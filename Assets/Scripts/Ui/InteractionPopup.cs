using UnityEngine;
using TMPro;
using System.Collections;

namespace JemaaGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionPopup : MonoBehaviour
    {
        public static InteractionPopup Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI npcInfoText;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeSpeed = 5f;
        
        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;
        private bool isVisible = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            isVisible = false;
        }

        public void Show(string npcName, string npcRole)
        {
            if (npcInfoText != null)
            {
                npcInfoText.text = $"{npcName} ({npcRole})";
            }
            
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Fade(1f));
            isVisible = true;
        }

        public void Hide()
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Fade(0f));
            isVisible = false;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
