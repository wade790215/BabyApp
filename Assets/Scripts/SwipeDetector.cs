using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BabyApp
{
    /// Reports horizontal swipes on the UI element it is attached to.
    /// Direction is -1 for a swipe to the left (next card) and +1 to the right.
    public class SwipeDetector : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public Action<int> OnSwipe;
        public float minDistance = 80f;

        Vector2 start;

        public void OnBeginDrag(PointerEventData eventData)
        {
            start = eventData.position;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            var delta = eventData.position - start;
            if (Mathf.Abs(delta.x) < minDistance || Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return;
            OnSwipe?.Invoke(delta.x < 0 ? -1 : 1);
        }
    }
}
