using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class MapViewport : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public RectTransform World { get; private set; }
        public float Zoom => zoom;
        public bool IsDragging { get; private set; }
        private RectTransform viewport, miniWindow;
        private Text zoomLabel;
        private Rect bounds;
        private Vector2 focus;
        private float zoom = 1, minimum = .5f, pinchDistance, pinchScale;
        private Vector2 pinchWorld;
        private float fitPadding = 140, miniScale = .11f;
        private Vector2 miniCenter, miniExtent = Vector2.one * 1060;
        private bool pinching;
        private Camera EventCamera => GetComponentInParent<Canvas>().worldCamera;
        public void Initialize(RectTransform world, Rect mapBounds, Vector2 location, float padding = 140)
        {
            viewport = (RectTransform)transform;
            World = world; bounds = mapBounds; focus = location; fitPadding = padding;
            Fit();
        }
        public void SetControls(Text label, RectTransform window, float scale = .11f, Vector2 center = default(Vector2), Vector2 extent = default(Vector2))
        {
            zoomLabel = label; miniWindow = window; miniScale = scale; miniCenter = center;
            miniExtent = extent == Vector2.zero ? Vector2.one * 1060 : extent;
            Apply();
        }
        public void Fit()
        {
            minimum = Mathf.Min(viewport.rect.width / (bounds.width + fitPadding), viewport.rect.height / (bounds.height + fitPadding));
            zoom = minimum;
            World.anchoredPosition = -bounds.center * zoom;
            Apply();
        }
        public void Locate()
        {
            zoom = Mathf.Max(zoom, 1.05f);
            World.anchoredPosition = -focus * zoom;
            Apply();
        }
        public void ZoomBy(float factor) { ZoomAt(zoom * factor, Vector2.zero); }
        private void ZoomAt(float value, Vector2 anchor)
        {
            float next = Mathf.Clamp(value, minimum, Mathf.Max(2, minimum * 3));
            World.anchoredPosition = anchor - (anchor - World.anchoredPosition) * (next / zoom);
            zoom = next; Apply();
        }
        private void Apply()
        {
            World.localScale = Vector3.one * zoom;
            Vector2 position = World.anchoredPosition;
            float margin = 90;
            position.x = Mathf.Clamp(position.x, -viewport.rect.width / 2 + margin - bounds.xMax * zoom, viewport.rect.width / 2 - margin - bounds.xMin * zoom);
            position.y = Mathf.Clamp(position.y, -viewport.rect.height / 2 + margin - bounds.yMax * zoom, viewport.rect.height / 2 - margin - bounds.yMin * zoom);
            World.anchoredPosition = position;
            if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(zoom / minimum * 100) + "%";
            if (miniWindow != null)
            {
                miniWindow.anchoredPosition = (-position / zoom - miniCenter) * miniScale;
                miniWindow.sizeDelta = new Vector2(Mathf.Min(miniExtent.x, viewport.rect.width / zoom), Mathf.Min(miniExtent.y, viewport.rect.height / zoom)) * miniScale;
            }
        }
        public void OnBeginDrag(PointerEventData eventData) { IsDragging = true; eventData.eligibleForClick = false; }
        public void OnDrag(PointerEventData eventData)
        {
            if (pinching) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 point);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 before);
            World.anchoredPosition += point - before; Apply();
        }
        public void OnEndDrag(PointerEventData eventData) { IsDragging = false; }
        public void OnScroll(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.enterEventCamera, out Vector2 point);
            ZoomAt(zoom * Mathf.Exp(eventData.scrollDelta.y * .12f), point);
        }
        private void Update()
        {
            if (World == null || Input.touchCount < 2) { pinching = false; return; }
            var first = Input.GetTouch(0); var second = Input.GetTouch(1);
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, first.position, EventCamera)
                || !RectTransformUtility.RectangleContainsScreenPoint(viewport, second.position, EventCamera)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, (first.position + second.position) / 2, EventCamera, out Vector2 middle);
            float distance = Vector2.Distance(first.position, second.position);
            if (!pinching)
            {
                pinching = true; pinchDistance = Mathf.Max(1, distance); pinchScale = zoom;
                pinchWorld = (middle - World.anchoredPosition) / zoom;
            }
            zoom = Mathf.Clamp(pinchScale * distance / pinchDistance, minimum, Mathf.Max(2, minimum * 3));
            World.anchoredPosition = middle - pinchWorld * zoom; Apply();
        }
    }
}
