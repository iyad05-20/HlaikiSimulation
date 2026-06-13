using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace JemaaGame.NPC
{
    /// <summary>
    /// UI panel for Halka composition: displays fragments, allows selection/ordering.
    /// Responsibility: user interaction, fragment display, order management.
    /// No logic coupling beyond composition engine queries.
    /// </summary>
    public class HalkaCompositionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect fragmentScrollArea;
        [SerializeField] private Transform fragmentContainerTransform;
        [SerializeField] private GameObject fragmentButtonPrefab;
        [SerializeField] private TextMeshProUGUI selectedOrderText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;

        [Header("Result Display")]
        [SerializeField] private GameObject resultPanelPrefab;
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private TextMeshProUGUI reactionText;
        [SerializeField] private Button resultCloseButton;

        private HalkaCompositionEngine _engine;
        private List<HalkaCompositionEngine.FragmentEntry> _availableFragments;
        private List<string> _selectedOrder = new List<string>();
        private Dictionary<string, Button> _fragmentButtons = new Dictionary<string, Button>();

        public event Action<List<string>> OnCompositionSubmitted;
        public event Action OnCancelled;

        // ─── Initialization ──────────────────────────────────────────────────────
        public void Initialize(HalkaCompositionEngine engine)
        {
            _engine = engine;
            _availableFragments = engine.GetAvailableFragments();

            if (_availableFragments.Count == 0)
            {
                Debug.LogWarning("[HalkaCompositionPanel] No fragments available.");
                ClosePanel();
                return;
            }

            SetupUI();
        }

        private void SetupUI()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            PopulateFragmentButtons();
            UpdateSelectedOrderDisplay();
        }

        // ─── Fragment Display ────────────────────────────────────────────────────
        private void PopulateFragmentButtons()
        {
            if (fragmentContainerTransform == null || fragmentButtonPrefab == null)
            {
                Debug.LogError("[HalkaCompositionPanel] Fragment container or prefab not assigned.");
                return;
            }

            foreach (var fragment in _availableFragments)
            {
                GameObject buttonObj = Instantiate(fragmentButtonPrefab, fragmentContainerTransform);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (button == null || label == null)
                {
                    Debug.LogError("[HalkaCompositionPanel] Fragment button prefab missing Button or TextMeshProUGUI.");
                    continue;
                }

                label.text = $"[{fragment.npcName}] — {fragment.title}";
                _fragmentButtons[fragment.fragmentId] = button;

                button.onClick.AddListener(() => OnFragmentToggled(fragment.fragmentId));
            }
        }

        // ─── Fragment Selection Logic ────────────────────────────────────────────
        private void OnFragmentToggled(string fragmentId)
        {
            if (_selectedOrder.Contains(fragmentId))
            {
                _selectedOrder.Remove(fragmentId);
            }
            else
            {
                _selectedOrder.Add(fragmentId);
            }

            UpdateButtonStates();
            UpdateSelectedOrderDisplay();
        }

        private void UpdateButtonStates()
        {
            foreach (var kvp in _fragmentButtons)
            {
                bool isSelected = _selectedOrder.Contains(kvp.Key);
                ColorBlock colors = kvp.Value.colors;
                colors.normalColor = isSelected ? Color.yellow : Color.white;
                kvp.Value.colors = colors;
            }
        }

        private void UpdateSelectedOrderDisplay()
        {
            if (selectedOrderText == null)
                return;

            if (_selectedOrder.Count == 0)
            {
                selectedOrderText.text = "Aucun fragment sélectionné. Cliquez sur un fragment pour commencer.";
                return;
            }

            List<string> names = _selectedOrder
                .Select(id => _availableFragments.FirstOrDefault(f => f.fragmentId == id)?.npcName ?? "???")
                .ToList();

            selectedOrderText.text = $"Ordre sélectionné:\n{string.Join(" → ", names)}";
        }

        // ─── Submission ──────────────────────────────────────────────────────────
        private void OnSubmitClicked()
        {
            if (_selectedOrder.Count == 0)
            {
                Debug.Log("[HalkaCompositionPanel] No fragments selected. Use default (all available).");
                _selectedOrder = _availableFragments.Select(f => f.fragmentId).ToList();
            }

            OnCompositionSubmitted?.Invoke(new List<string>(_selectedOrder));
            Destroy(gameObject);
        }

        private void OnCancelClicked()
        {
            OnCancelled?.Invoke();
            Destroy(gameObject);
        }

        private void ClosePanel()
        {
            Destroy(gameObject);
        }

        // ─── Result Display ──────────────────────────────────────────────────────
        public void DisplayResult(HalkaCompositionEngine.CompositionResult result)
        {
            // Replace composition panel with result panel
            if (resultPanelPrefab != null)
            {
                GameObject resultObj = Instantiate(resultPanelPrefab);
                var resultPanel = resultObj.GetComponent<HalkaResultPanel>();

                if (resultPanel != null)
                {
                    resultPanel.Display(result);
                }
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Separate panel to display Halka result (narration, reaction, scores).
    /// </summary>
    public class HalkaResultPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private TextMeshProUGUI reactionText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        public void Display(HalkaCompositionEngine.CompositionResult result)
        {
            if (narrationText != null)
                narrationText.text = result.narration ?? "Narration unavailable.";

            if (reactionText != null)
                reactionText.text = result.reactionLabel ?? "No reaction.";

            if (scoreText != null)
            {
                scoreText.text = $@"Cohérence: {result.coherenceScore}
Profondeur: {result.depthScore}
Audience: {result.audienceSize}
Total: {result.totalScore}

{(result.exposesSensitiveStory ? "⚠️ Une histoire intime a été révélée." : "")}";
            }
        }
    }
}
