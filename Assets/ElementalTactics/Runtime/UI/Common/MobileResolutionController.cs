using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    /// <summary>Fits the portrait UI inside the device safe area at native screen resolution.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    [DefaultExecutionOrder(100)]
    public sealed class MobileResolutionController : MonoBehaviour
    {
        [Header("界面设计尺寸")]
        [Tooltip("现有场景按 900 × 1600 排版。修改此值时，应同步调整内容布局。")]
        public Vector2 referenceResolution = new Vector2(900, 1600);
        [Header("手机适配")]
        [Tooltip("将交互内容放入安全区，避开刘海、挖孔与底部手势区域。")]
        public bool respectSafeArea = true;
        [Tooltip("仅在手机运行时锁定竖屏，不影响编辑器或桌面窗口。")]
        public bool lockPortraitOnMobile = true;
        [Tooltip("覆盖整个屏幕，包括安全区外和比例不同产生的留白。")]
        public Color backgroundColor = new Color(.035f, .075f, .13f, 1);
        [Header("画布节点（留空时从 PortraitLayout 自动获取）")]
        public RectTransform safeArea;
        public RectTransform content;

        private CanvasScaler scaler;
        private PortraitLayout layout;
        private Image backdrop;
        private RectTransform preparedContent;

        private void OnEnable()
        {
            scaler = GetComponent<CanvasScaler>();
            layout = GetComponent<PortraitLayout>();
            if (Application.isMobilePlatform && lockPortraitOnMobile)
                Screen.orientation = ScreenOrientation.Portrait;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        /// <summary>Also callable after changing layout settings at runtime.</summary>
        public void Refresh()
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            if (layout == null) layout = GetComponent<PortraitLayout>();
            // Builders assign these references after AddComponent invokes OnEnable.
            if (layout != null)
            {
                if (safeArea == null) safeArea = layout.safeArea;
                if (content == null) content = layout.content;
            }
            if (safeArea == null || content == null || scaler == null) return;
            if (backdrop == null)
            {
                var existing = transform.Find("Backdrop");
                backdrop = existing == null ? null : existing.GetComponent<Image>();
                if (backdrop == null)
                    backdrop = PrototypeUI.Panel("Resolution Background", transform, Vector2.zero, Vector2.zero, backgroundColor);
                backdrop.transform.SetAsFirstSibling();
                backdrop.raycastTarget = false;
                backdrop.rectTransform.anchorMin = Vector2.zero;
                backdrop.rectTransform.anchorMax = Vector2.one;
                backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
            }
            backdrop.color = backgroundColor;
            if (preparedContent != content)
            {
                // Artwork can extend beyond its authored rect. Keep it out of the margins.
                if (content.GetComponent<RectMask2D>() == null) content.gameObject.AddComponent<RectMask2D>();
                preparedContent = content;
            }
            var reference = ValidReference(referenceResolution);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            ApplyLayout(safeArea, content, reference, new Vector2Int(Screen.width, Screen.height),
                respectSafeArea ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height));
        }

        private static Vector2 ValidReference(Vector2 value)
        {
            return new Vector2(value.x > 0 && !float.IsInfinity(value.x) ? value.x : 900,
                value.y > 0 && !float.IsInfinity(value.y) ? value.y : 1600);
        }

        // The shared calculation also supports edit-mode layout and simulated device tests.
        public static void ApplyLayout(RectTransform safe, RectTransform body, Vector2 reference,
            Vector2Int screenSize, Rect deviceSafeArea)
        {
            if (safe == null || body == null || screenSize.x <= 0 || screenSize.y <= 0) return;
            reference = ValidReference(reference);
            float left = Mathf.Clamp(deviceSafeArea.xMin, 0, screenSize.x);
            float bottom = Mathf.Clamp(deviceSafeArea.yMin, 0, screenSize.y);
            float right = Mathf.Clamp(deviceSafeArea.xMax, 0, screenSize.x);
            float top = Mathf.Clamp(deviceSafeArea.yMax, 0, screenSize.y);
            // Some devices briefly report an empty area during resume or rotation.
            if (!(right > left && top > bottom))
            {
                left = bottom = 0;
                right = screenSize.x;
                top = screenSize.y;
            }
            safe.anchorMin = new Vector2(left / screenSize.x, bottom / screenSize.y);
            safe.anchorMax = new Vector2(right / screenSize.x, top / screenSize.y);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, .5f);
            body.anchoredPosition = Vector2.zero;
            body.sizeDelta = reference;
            float scale = Mathf.Min(safe.rect.width / reference.x, safe.rect.height / reference.y);
            body.localScale = Vector3.one * Mathf.Max(0, scale);
        }
    }
}
