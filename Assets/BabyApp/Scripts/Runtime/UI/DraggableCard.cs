using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BabyApp.UI
{
    /// <summary>
    /// 讓整張卡跟著手指走：拖的時候微微傾斜，放手超過門檻就往那邊飛出去、
    /// 下一張從另一邊滑進來；沒超過就彈回原位。箭頭按鈕也走同一套動畫。
    ///
    /// 掛在卡片的外層容器上，裡面那張卡的彈跳動畫（TapReaction）動的是另一個物件，
    /// 兩邊不會互相打架。
    /// </summary>
    public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("放手時要離原位多遠才算換卡（版面單位）")]
        [SerializeField] private float swipeThreshold = 160f;
        [SerializeField] private float flyDistance = 1400f;
        [SerializeField] private float flySeconds = 0.22f;
        [SerializeField] private float slideInSeconds = 0.28f;
        [SerializeField] private float springBackSeconds = 0.25f;
        [Tooltip("每拖 1 單位傾斜幾度")]
        [SerializeField] private float tiltPerUnit = 0.02f;

        /// <summary>卡片開始飛出去的那一刻（用來馬上把聲音停掉）。</summary>
        public event Action FlipStarted;

        /// <summary>卡片已經飛出去，該把下一張（或上一張）綁上來了。</summary>
        public event Action SwipedToNext;
        public event Action SwipedToPrevious;

        public bool IsAnimating { get; private set; }
        public float LastDragEndTime { get; private set; } = -10f;
        public bool LastDragMoved { get; private set; }

        private RectTransform _rt;
        private RectTransform _parent;
        private Vector2 _home;
        private Vector2 _pointerStart;
        private Vector2 _offset;
        private bool _dragging;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _parent = _rt.parent as RectTransform;
            _home = _rt.anchoredPosition;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsAnimating = false;
            _dragging = false;
            _rt.anchoredPosition = _home;
            _rt.localRotation = Quaternion.identity;
        }

        /// <summary>用按鈕換卡：-1 往左飛（下一張），+1 往右飛（上一張）。</summary>
        public void Flip(int direction)
        {
            if (IsAnimating || _dragging) return;
            StartCoroutine(FlipRoutine(direction < 0 ? -1 : 1));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsAnimating) return;
            if (!ToLocal(eventData, out _pointerStart)) return;
            _dragging = true;
            _offset = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (!ToLocal(eventData, out var local)) return;

            _offset = local - _pointerStart;
            // 橫向跟手、縱向只跟一點點，重點是左右滑
            _rt.anchoredPosition = _home + new Vector2(_offset.x, _offset.y * 0.15f);
            _rt.localRotation = Quaternion.Euler(0f, 0f, -_offset.x * tiltPerUnit);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            LastDragEndTime = Time.unscaledTime;
            LastDragMoved = Mathf.Abs(_offset.x) > 20f;

            if (_offset.x < -swipeThreshold) StartCoroutine(FlipRoutine(-1));
            else if (_offset.x > swipeThreshold) StartCoroutine(FlipRoutine(1));
            else StartCoroutine(SpringBack());
        }

        // ---------- 動畫 ----------

        private IEnumerator FlipRoutine(int direction)
        {
            IsAnimating = true;
            FlipStarted?.Invoke();

            // 飛出去：越飛越斜
            var target = _home + new Vector2(direction * flyDistance, 0f);
            var targetRotation = Quaternion.Euler(0f, 0f, -direction * 18f);
            yield return Animate(target, targetRotation, flySeconds, easeOut: false);

            if (direction < 0) SwipedToNext?.Invoke();
            else SwipedToPrevious?.Invoke();

            // 新卡從另一邊進來
            _rt.anchoredPosition = _home + new Vector2(-direction * flyDistance * 0.5f, 0f);
            _rt.localRotation = Quaternion.Euler(0f, 0f, direction * 8f);
            yield return Animate(_home, Quaternion.identity, slideInSeconds, easeOut: true);

            IsAnimating = false;
        }

        private IEnumerator SpringBack()
        {
            IsAnimating = true;
            yield return Animate(_home, Quaternion.identity, springBackSeconds, easeOut: true);
            IsAnimating = false;
        }

        private IEnumerator Animate(Vector2 toPosition, Quaternion toRotation, float seconds, bool easeOut)
        {
            var fromPosition = _rt.anchoredPosition;
            var fromRotation = _rt.localRotation;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                float eased = easeOut ? 1f - Mathf.Pow(1f - p, 3f) : p * p;
                _rt.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
                _rt.localRotation = Quaternion.Slerp(fromRotation, toRotation, eased);
                yield return null;
            }

            _rt.anchoredPosition = toPosition;
            _rt.localRotation = toRotation;
        }

        private bool ToLocal(PointerEventData eventData, out Vector2 local)
        {
            if (_parent == null)
            {
                local = Vector2.zero;
                return false;
            }
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out local);
        }
    }
}
