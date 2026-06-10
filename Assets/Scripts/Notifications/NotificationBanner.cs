using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JemaaGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class NotificationBanner : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image bgImage;
        [SerializeField] private Image sidebarImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float scaleAmount = 0.95f;

        private RectTransform rectTransform;
        private Coroutine activeAnimationCoroutine;
        private System.Action<NotificationBanner> releaseCallback;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            
            // Set initial hidden state
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * scaleAmount;
        }

        /// <summary>
        /// Configures the banner visuals and texts.
        /// </summary>
        public void Configure(string title, string description, NotificationType type, Sprite iconSprite = null)
        {
            // Assign texts
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            // Apply Gilded Codex base background color (#1B1106 with Alpha 180)
            if (bgImage != null)
            {
                bgImage.color = new Color32(27, 17, 6, 180);
            }

            // Apply specific accent color on the left sidebar
            if (sidebarImage != null)
            {
                sidebarImage.color = GetColorForType(type);
            }

            // Configure icon if available
            if (iconImage != null)
            {
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.gameObject.SetActive(true);
                    iconImage.color = GetColorForType(type); // tint icon
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Plays the full life-cycle animation: Fade In, Display, Fade Out, and Release.
        /// </summary>
        public void Play(float displayDuration, System.Action<NotificationBanner> onRelease)
        {
            releaseCallback = onRelease;
            
            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
            }
            
            activeAnimationCoroutine = StartCoroutine(AnimateLifecycle(displayDuration));
        }

        private IEnumerator AnimateLifecycle(float displayDuration)
        {
            // --- Phase 1: Fade In & Scale Up ---
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * scaleAmount;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                
                // Ease out cubic
                float tEase = 1f - Mathf.Pow(1f - t, 3);
                
                canvasGroup.alpha = tEase;
                transform.localScale = Vector3.Lerp(Vector3.one * scaleAmount, Vector3.one, tEase);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;

            // --- Phase 2: Stay Visible ---
            yield return new WaitForSeconds(displayDuration);

            // --- Phase 3: Fade Out & Scale Down ---
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                
                // Ease in cubic
                float tEase = t * t * t;
                
                canvasGroup.alpha = 1f - tEase;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * scaleAmount, tEase);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * scaleAmount;

            // --- Phase 4: Recycle back to pool ---
            activeAnimationCoroutine = null;
            releaseCallback?.Invoke(this);
        }

        private Color GetColorForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Friendship:
                    return new Color32(16, 185, 129, 255); // Emerald Green
                case NotificationType.Reputation:
                    return new Color32(99, 102, 241, 255);  // Indigo
                case NotificationType.Fragment:
                    return new Color32(245, 158, 11, 255);  // Gold/Amber
                case NotificationType.System:
                    return new Color32(148, 163, 184, 255); // Slate Grey
                case NotificationType.Warning:
                    return new Color32(239, 68, 68, 255);   // Red
                default:
                    return Color.white;
            }
        }
        
        // Manual assignment helper for programmatic creation
        public void SetupComponents(CanvasGroup cg, Image bg, Image sidebar, Image icon, TextMeshProUGUI title, TextMeshProUGUI desc)
        {
            canvasGroup = cg;
            bgImage = bg;
            sidebarImage = sidebar;
            iconImage = icon;
            titleText = title;
            descriptionText = desc;
        }
    }
}
