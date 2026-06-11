using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JemaaGame.NPC
{
    /// <summary>
    /// Halka orchestrator: manages the end-of-cycle flow.
    /// Responsibility: detect cycle end, trigger composition UI, broadcast completion.
    /// Singleton pattern. Follows project SRP guidelines.
    /// </summary>
    public class HalkaOrchestrator : MonoBehaviour
    {
        private static HalkaOrchestrator _instance;
        public static HalkaOrchestrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HalkaOrchestrator>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [SerializeField] private HalkaCompositionEngine compositionEngine;
        [SerializeField] private GameObject halkaCompositionPanelPrefab;

        private HalkaCompositionPanel _currentPanel;
        private bool _halkaInProgress;

        // Events for UI and downstream systems
        public event Action<HalkaCompositionEngine.CompositionResult> OnHalkaCompleted;
        public event Action OnHalkaStartRequested;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (compositionEngine == null)
                compositionEngine = GetComponent<HalkaCompositionEngine>();

            if (compositionEngine == null)
            {
                Debug.LogError("[HalkaOrchestrator] HalkaCompositionEngine not found. Halka system disabled.");
                enabled = false;
                return;
            }
        }

        // ─── Cycle Integration ──────────────────────────────────────────────────
        /// <summary>
        /// Called by InputHandler.EndConversation() to trigger Halka flow (after all NPCs done).
        /// Conditions:
        /// - No conversation active
        /// - End-of-day detected (optional: via GameManager or explicit flag)
        /// - At least 1 fragment collected
        /// </summary>
        public void TriggerEndOfCycleHalka()
        {
            if (_halkaInProgress)
            {
                Debug.LogWarning("[HalkaOrchestrator] Halka already in progress. Ignoring request.");
                return;
            }

            List<HalkaCompositionEngine.FragmentEntry> available = compositionEngine.GetAvailableFragments();
            if (available.Count == 0)
            {
                Debug.Log("[HalkaOrchestrator] No fragments collected. Skipping Halka.");
                return;
            }

            _halkaInProgress = true;
            OnHalkaStartRequested?.Invoke();

            // Si pas de prefab de panel assigné : lancer directement avec tous les fragments
            if (halkaCompositionPanelPrefab == null)
            {
                Debug.Log("[HalkaOrchestrator] No composition panel prefab — running Halka with all fragments.");
                var allIds = available.ConvertAll(f => f.fragmentId);
                StartCoroutine(ExecuteHalka(allIds));
                return;
            }

            ShowCompositionPanel();
        }

        // ─── UI Panel Management ─────────────────────────────────────────────────
        private void ShowCompositionPanel()
        {
            if (halkaCompositionPanelPrefab == null)
            {
                Debug.LogError("[HalkaOrchestrator] HalkaCompositionPanel prefab not assigned.");
                _halkaInProgress = false;
                return;
            }

            GameObject panelInstance = Instantiate(halkaCompositionPanelPrefab);
            _currentPanel = panelInstance.GetComponent<HalkaCompositionPanel>();

            if (_currentPanel == null)
            {
                Debug.LogError("[HalkaOrchestrator] HalkaCompositionPanel component not found on prefab.");
                Destroy(panelInstance);
                _halkaInProgress = false;
                return;
            }

            _currentPanel.Initialize(compositionEngine);
            _currentPanel.OnCompositionSubmitted += HandleCompositionSubmitted;
            _currentPanel.OnCancelled += HandleCompositionCancelled;
        }

        private void HandleCompositionSubmitted(List<string> selectedFragmentIds)
        {
            if (_currentPanel != null)
            {
                _currentPanel.OnCompositionSubmitted -= HandleCompositionSubmitted;
                _currentPanel.OnCancelled -= HandleCompositionCancelled;
            }

            StartCoroutine(ExecuteHalka(selectedFragmentIds));
        }

        private void HandleCompositionCancelled()
        {
            if (_currentPanel != null)
            {
                _currentPanel.OnCompositionSubmitted -= HandleCompositionSubmitted;
                _currentPanel.OnCancelled -= HandleCompositionCancelled;
                Destroy(_currentPanel.gameObject);
            }

            _halkaInProgress = false;
            Debug.Log("[HalkaOrchestrator] Halka composition cancelled.");
        }

        // ─── Halka Execution ────────────────────────────────────────────────────
        public static HalkaCompositionEngine.CompositionResult LastResult { get; private set; }

        private IEnumerator ExecuteHalka(List<string> selectedFragmentIds)
        {
            // Score composition (pure local logic)
            var result = compositionEngine.ScoreComposition(selectedFragmentIds);

            // Generate narration (LLM call)
            string narration = null;

            yield return StartCoroutine(compositionEngine.GenerateNarration(
                result,
                text => { narration = text; },
                error => { }
            ));

            result.narration = narration ?? BuildFallbackNarration(result);
            LastResult = result;

            // Broadcast completion
            OnHalkaCompleted?.Invoke(result);

            // Charger la scène de fin de cycle (HalkaScene)
            UnityEngine.SceneManagement.SceneManager.LoadScene("HalkaScene");

            _halkaInProgress = false;
        }

        private string BuildFallbackNarration(HalkaCompositionEngine.CompositionResult result)
        {
            if (result.selectedFragmentIds.Count == 0)
                return "Le silence retombe. Le cercle attend une histoire qui ne vient pas... mais la nuit est jeune.";

            string label = result.reactionLabel;
            return $"La Halka se déploie devant le cercle. {label}";
        }

        // ─── Audience Signal Control ─────────────────────────────────────────────
        public void SetMoussaAllied(bool value) => compositionEngine.MoussaAllied = value;
        public void SetYoussefBavard(bool value) => compositionEngine.YoussefBavard = value;
        public void SetOmarPresent(bool value) => compositionEngine.OmarPresent = value;

        // ─── Cycle Reset ────────────────────────────────────────────────────────
        public void ResetCycle()
        {
            _halkaInProgress = false;
            if (_currentPanel != null)
            {
                Destroy(_currentPanel.gameObject);
                _currentPanel = null;
            }
            compositionEngine.Reset();
        }

        private void OnDestroy()
        {
            if (_currentPanel != null)
                Destroy(_currentPanel.gameObject);
        }
    }
}
