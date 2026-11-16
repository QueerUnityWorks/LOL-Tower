using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class MechanismPortraitDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public MechanismCoefficientDebug owner;
        public UnitRecord unit;
        public ItemRecord item;
        public string category;
        public RawImage art;
        float pressedAt;
        bool held, scrolling, dragged;
        PointerEventData pointer;
        RectTransform ghost;
        public void OnPointerDown(PointerEventData e) { if (owner.Busy) return; pressedAt = Time.unscaledTime; held = true; dragged = scrolling = false; pointer = e; }
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = true; owner.RosterScroll.OnInitializePotentialDrag(e); }
        void Update() { if (held && !scrolling && ghost == null && Time.unscaledTime - pressedAt >= .28f) BeginPortraitDrag(pointer); }
        public void BeginPortraitDrag(PointerEventData e)
        {
            if (owner.Busy || ghost != null) return;
            ghost = new GameObject("Dragged Portrait", typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup)).GetComponent<RectTransform>();
            ghost.SetParent(owner.DragLayer, false); ghost.sizeDelta = new Vector2(164, 246); ghost.anchorMin = ghost.anchorMax = new Vector2(.5f, .5f); ghost.pivot = new Vector2(.5f, .5f);
            var image = ghost.GetComponent<RawImage>(); image.texture = art.texture; image.uvRect = art.uvRect; image.raycastTarget = false;
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false; ghost.GetComponent<CanvasGroup>().alpha = .85f;
            dragged = true; owner.RosterScroll.StopMovement(); Move(e);
        }
        void Move(PointerEventData e)
        {
            if (ghost == null) return; Vector2 point;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.DragLayer, e.position, e.pressEventCamera, out point)) ghost.localPosition = point;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (owner.Busy) return;
            if (ghost != null || held && Time.unscaledTime - pressedAt >= .28f) BeginPortraitDrag(e);
            else { scrolling = true; dragged = true; owner.RosterScroll.OnBeginDrag(e); }
        }
        public void OnDrag(PointerEventData e) { if (ghost != null) Move(e); else if (scrolling) owner.RosterScroll.OnDrag(e); }
        public void OnPointerUp(PointerEventData e) { held = false; if (ghost != null) Finish(e); }
        public void OnEndDrag(PointerEventData e) { held = false; if (ghost != null) Finish(e); else if (scrolling) owner.RosterScroll.OnEndDrag(e); scrolling = false; }
        void Select() { if (item != null) owner.Select(item); else owner.Select(unit, category); }
        public void OnPointerClick(PointerEventData e) { if (!dragged) Select(); }
        void Finish(PointerEventData e)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(owner.DropSlot, e.position, e.pressEventCamera)) Select();
            RemoveGhost();
        }
        void RemoveGhost() { if (ghost != null) { if (Application.isPlaying) Destroy(ghost.gameObject); else DestroyImmediate(ghost.gameObject); } ghost = null; }
        void OnDisable() { held = scrolling = false; RemoveGhost(); }
    }
}
