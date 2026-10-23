using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BabyApp.UI
{
    /// <summary>
    /// 可以拖到別處的小東西（吉祥物）：擋到畫面就拖走，位置會記住。
    /// 沒拖、只是點一下的話發 Tapped（找一找、數數裡用來再聽一次題目）。
    /// </summary>
    public class DraggableWidget : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Tooltip("記位置用的名字，空的話用 父物件名/自己名")]
        [SerializeField] private string saveKey;

        public event Action Tapped;

        private RectTransform _rt;
        private RectTransform _parent;
        private Vector2 _grabOffset;
        private bool _dragged;

        private string Key => "widget." + (string.IsNullOrEmpty(saveKey)
            ? (transform.parent != null ? transform.parent.name + "/" : "") + name
            : saveKey);

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _parent = _rt.parent as RectTransform;
        }

        private void OnEnable()
        {
            // 有存過就放回上次的位置
            if (PlayerPrefs.HasKey(Key + ".x"))
            {
                _rt.anchoredPosition = Clamp(new Vector2(PlayerPrefs.GetFloat(Key + ".x"), PlayerPrefs.GetFloat(Key + ".y")));
            }
        }

        public void OnPointerDown(PointerEventData eventData) => _dragged = false;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragged = true;
            if (ToLocal(eventData, out var local)) _grabOffset = _rt.anchoredPosition - AnchoredFromLocal(local);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!ToLocal(eventData, out var local)) return;
            _rt.anchoredPosition = Clamp(AnchoredFromLocal(local) + _grabOffset);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            PlayerPrefs.SetFloat(Key + ".x", _rt.anchoredPosition.x);
            PlayerPrefs.SetFloat(Key + ".y", _rt.anchoredPosition.y);
            PlayerPrefs.Save();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragged) return;
            Tapped?.Invoke();
        }

        // ---------- 座標 ----------

        private bool ToLocal(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            return _parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out local);
        }

        /// <summary>父物件的本地座標 → 以自己錨點為準的 anchoredPosition。</summary>
        private Vector2 AnchoredFromLocal(Vector2 local)
        {
            var rect = _parent.rect;
            var anchor = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, _rt.anchorMin.x),
                Mathf.Lerp(rect.yMin, rect.yMax, _rt.anchorMin.y));
            return local - anchor;
        }

        /// <summary>不要讓它被拖出畫面外。</summary>
        private Vector2 Clamp(Vector2 anchored)
        {
            if (_parent == null) return anchored;
            var rect = _parent.rect;
            var size = _rt.rect.size;
            var anchor = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, _rt.anchorMin.x),
                Mathf.Lerp(rect.yMin, rect.yMax, _rt.anchorMin.y));
            var local = anchored + anchor;
            local.x = Mathf.Clamp(local.x, rect.xMin + size.x * 0.5f, rect.xMax - size.x * 0.5f);
            local.y = Mathf.Clamp(local.y, rect.yMin + size.y * 0.5f, rect.yMax - size.y * 0.5f);
            return local - anchor;
        }
    }
}
