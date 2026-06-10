using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JemaaGame.UI
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [System.Serializable]
        public struct NotificationData
        {
            public string Title;
            public string Description;
            public NotificationType Type;
            public Sprite Icon;
            public float Duration;

            public NotificationData(string title, string description, NotificationType type, Sprite icon, float duration)
            {
                Title = title;
                Description = description;
                Type = type;
                Icon = icon;
                Duration = duration;
            }
        }

        [Header("UI References")]
        [Tooltip("The parent container (box) that will hold the notification banners. Must have a VerticalLayoutGroup.")]
        [SerializeField] private RectTransform containerRect;
        
        [Tooltip("Optional prefab for the notification banner. If null, a default visual banner will be constructed dynamically.")]
        [SerializeField] private GameObject bannerPrefab;

        [Header("System Settings")]
        [SerializeField] private int maxVisibleNotifications = 3;
        [SerializeField] private int maxQueuedNotifications = 5;
        [SerializeField] private int initialPoolSize = 5;

        private List<NotificationBanner> pool = new List<NotificationBanner>();
        private Queue<NotificationData> queue = new Queue<NotificationData>();
        private List<NotificationBanner> activeBanners = new List<NotificationBanner>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Optional: prevent destruction on scene load if desired, but here we let it live with the UI scene
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureContainerExists();
            InitializePool();
        }

        /// <summary>
        /// Displays a notification. If the visible limit is reached, it queues the notification.
        /// If the queue overflows, the oldest pending notification is discarded.
        /// </summary>
        public void Show(string title, string description, NotificationType type, Sprite icon = null, float duration = 3.5f)
        {
            NotificationData data = new NotificationData(title, description, type, icon, duration);

            if (activeBanners.Count < maxVisibleNotifications)
            {
                DisplayNotification(data);
            }
            else
            {
                // Capping Queue: Drop oldest if full
                if (queue.Count >= maxQueuedNotifications)
                {
                    queue.Dequeue(); // Silent drop
                }
                
                queue.Enqueue(data);
            }
        }

        private void DisplayNotification(NotificationData data)
        {
            NotificationBanner banner = GetPooledBanner();
            if (banner == null) return;

            banner.gameObject.SetActive(true);
            banner.transform.SetAsLastSibling(); // Ensure it stacks correctly at the bottom of the container
            banner.Configure(data.Title, data.Description, data.Type, data.Icon);
            
            activeBanners.Add(banner);

            banner.Play(data.Duration, OnBannerReleased);
        }

        private void OnBannerReleased(NotificationBanner banner)
        {
            banner.gameObject.SetActive(false);
            activeBanners.Remove(banner);

            // Process next in queue
            if (queue.Count > 0 && activeBanners.Count < maxVisibleNotifications)
            {
                DisplayNotification(queue.Dequeue());
            }
        }

        private NotificationBanner GetPooledBanner()
        {
            // Search for an inactive banner in the pool
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf && !activeBanners.Contains(pool[i]))
                {
                    return pool[i];
                }
            }

            // If none are inactive, create a new one to dynamically grow the pool
            NotificationBanner newBanner = CreateNewBannerInstance();
            if (newBanner != null)
            {
                pool.Add(newBanner);
                return newBanner;
            }

            return null;
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                NotificationBanner banner = CreateNewBannerInstance();
                if (banner != null)
                {
                    banner.gameObject.SetActive(false);
                    pool.Add(banner);
                }
            }
        }

        private void EnsureContainerExists()
        {
            if (containerRect != null) return;

            // Try to find a canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                // Create a fallback canvas
                GameObject canvasObj = new GameObject("NotificationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            }

            // Create container GameObject
            GameObject containerObj = new GameObject("NotificationContainer", typeof(RectTransform));
            containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.SetParent(canvas.transform, false);

            // Position at top right
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-20f, -20f);
            containerRect.sizeDelta = new Vector2(400f, 600f);

            // Add Vertical Layout Group for automatic stacking
            VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Add Content Size Fitter to auto-fit height
            ContentSizeFitter fitter = containerObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private NotificationBanner CreateNewBannerInstance()
        {
            if (bannerPrefab != null)
            {
                GameObject obj = Instantiate(bannerPrefab, containerRect);
                return obj.GetComponent<NotificationBanner>();
            }

            // Fallback: Dynamically build a stunning visual banner representing the "Gilded Codex" aesthetic
            GameObject bannerObj = new GameObject("NotificationBanner", typeof(RectTransform), typeof(CanvasGroup));
            bannerObj.transform.SetParent(containerRect, false);

            RectTransform rt = bannerObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 85f);

            // 1. Background image (Gilded Codex: Dark chocolate brown #1B1106 with Alpha 180)
            Image bgImg = bannerObj.AddComponent<Image>();
            bgImg.color = new Color32(27, 17, 6, 180);

            // 2. Left side color accent stripe
            GameObject stripeObj = new GameObject("Sidebar", typeof(RectTransform));
            stripeObj.transform.SetParent(bannerObj.transform, false);
            RectTransform stripeRt = stripeObj.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0f);
            stripeRt.anchorMax = new Vector2(0f, 1f);
            stripeRt.pivot = new Vector2(0f, 0.5f);
            stripeRt.anchoredPosition = Vector2.zero;
            stripeRt.sizeDelta = new Vector2(8f, 0f); // 8px wide stripe
            Image stripeImg = stripeObj.AddComponent<Image>();

            // 3. Gold border / outline (fine outline)
            GameObject borderObj = new GameObject("GoldBorder", typeof(RectTransform));
            borderObj.transform.SetParent(bannerObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero; // stretch to match parent
            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = new Color32(212, 175, 55, 100); // Gilded gold, transparent
            outline.effectDistance = new Vector2(1f, -1f);
            borderObj.AddComponent<Image>().color = Color.clear; // outline needs a graphic but transparent background

            // 4. Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(bannerObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(20f, 0f); // offset from left stripe
            iconRt.sizeDelta = new Vector2(32f, 32f);
            Image iconImg = iconObj.AddComponent<Image>();

            // 5. Title text (TMP)
            GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform));
            titleTextObj.transform.SetParent(bannerObj.transform, false);
            RectTransform titleRt = titleTextObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(64f, -10f); // offset from icon
            titleRt.sizeDelta = new Vector2(-74f, 25f);
            TextMeshProUGUI titleTxt = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 15;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color32(245, 235, 215, 255); // Cream/parchment light color
            titleTxt.alignment = TextAlignmentOptions.Left;

            // 6. Description text (TMP)
            GameObject descTextObj = new GameObject("DescText", typeof(RectTransform));
            descTextObj.transform.SetParent(bannerObj.transform, false);
            RectTransform descRt = descTextObj.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 1f);
            descRt.pivot = new Vector2(0f, 0f);
            descRt.anchoredPosition = new Vector2(64f, 8f);
            descRt.sizeDelta = new Vector2(-74f, -40f);
            TextMeshProUGUI descTxt = descTextObj.AddComponent<TextMeshProUGUI>();
            descTxt.fontSize = 11;
            descTxt.color = new Color32(200, 190, 175, 255); // muted warm light color
            descTxt.alignment = TextAlignmentOptions.TopLeft;
            descTxt.textWrappingMode = TextWrappingModes.Normal;
            descTxt.overflowMode = TextOverflowModes.Ellipsis;

            // 7. Attach and setup banner script
            NotificationBanner banner = bannerObj.AddComponent<NotificationBanner>();
            banner.SetupComponents(bannerObj.GetComponent<CanvasGroup>(), bgImg, stripeImg, iconImg, titleTxt, descTxt);

            return banner;
        }
    }
}
