using UnityEngine;
using UnityEngine.EventSystems;

namespace ElementalTactics
{
    public sealed class FormationCardDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FormationController controller;
        private CardView card;
        private int index;
        private bool reserve;
        private int pointerId = int.MinValue;
        private float pressedAt;
        private bool scrolling;
        private bool movingCard;
        private PointerEventData lastEvent;

        public void Initialize(FormationController owner, CardView view, int characterOrSlot, bool fromReserve)
        {
            controller = owner;
            card = view;
            index = characterOrSlot;
            reserve = fromReserve;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || pointerId != int.MinValue
                || controller == null || !controller.CanDrag) return;
            pointerId = eventData.pointerId;
            pressedAt = Time.unscaledTime;
            lastEvent = eventData;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            if (movingCard)
            {
                ResetPointer();
                controller.EndCardDrag(eventData);
            }
            else if (!scrolling) ResetPointer();
        }

        private void Update()
        {
            if (!reserve || pointerId < 0 || movingCard || scrolling || lastEvent == null
                || Time.unscaledTime - pressedAt < 0.3f || !controller.CanDrag) return;
            lastEvent.eligibleForClick = false;
            movingCard = controller.BeginCardDrag(card, index, lastEvent);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId && reserve)
                controller.rosterScroll.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            eventData.eligibleForClick = false;
            lastEvent = eventData;
            if (movingCard) return;
            if (!controller.CanDrag) return;
            scrolling = reserve && eventData.pointerId >= 0 && Time.unscaledTime - pressedAt < 0.3f;
            if (scrolling) controller.rosterScroll.OnBeginDrag(eventData);
            else movingCard = controller.BeginCardDrag(card, reserve ? index : controller.State[index], eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            lastEvent = eventData;
            if (scrolling && !controller.CanDrag) CancelInteraction();
            else if (scrolling) controller.rosterScroll.OnDrag(eventData);
            else if (movingCard) controller.UpdateCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            bool completeCard = movingCard;
            if (scrolling) controller.rosterScroll.OnEndDrag(eventData);
            ResetPointer();
            if (completeCard) controller.EndCardDrag(eventData);
        }

        private void OnDisable()
        {
            bool cancelCard = movingCard;
            if (scrolling && controller != null && lastEvent != null)
                controller.rosterScroll.OnEndDrag(lastEvent);
            ResetPointer();
            if (cancelCard && controller != null) controller.CancelCardDrag();
        }

        private void ResetPointer()
        {
            pointerId = int.MinValue;
            scrolling = false;
            movingCard = false;
            lastEvent = null;
        }

        public void CancelInteraction()
        {
            if (scrolling && controller != null && lastEvent != null)
            {
                controller.rosterScroll.OnEndDrag(lastEvent);
                controller.rosterScroll.StopMovement();
            }
            ResetPointer();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !movingCard) CancelInteraction();
        }
    }
}
