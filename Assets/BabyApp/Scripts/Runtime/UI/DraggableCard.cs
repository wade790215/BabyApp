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
        [Tooltip("放手時要離原位多遠才算換卡（版面單位）；實際門檻取這個與卡寬百分比的較小者")]
        [SerializeField] private float swipeThreshold = 160f;
        [Tooltip("位移超過卡寬的這個比例就算換卡；小小孩的手短，拖不遠")]
        [SerializeField, Range(0.1f, 0.6f)] private float swipeFraction = 0.25f;
        [Tooltip("放手前的橫向速度超過這個值（版面單位/秒）也算換卡，隨手一甩就會翻")]
        [SerializeField] private float flickSpeed = 900f;
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
        private float _velocityX;
        private float _lastDragTime;

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
            _velocityX = 0f;
            _lastDragTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (!ToLocal(eventData, out var local)) return;

            var previous = _offset;
            _offset = local - _pointerStart;

            // 平滑一下的橫向速度，放手時拿來判斷是不是「甩」
            float now = Time.unscaledTime;
            float dt = Mathf.Max(now - _lastDragTime, 1e-4f);
            _velocityX = Mathf.Lerp(_velocityX, (_offset.x - previous.x) / dt, 0.5f);
            _lastDragTime = now;

            // 橫向跟手、縱向只跟一點點，重點是左右滑（小小孩常常斜著拖，縱向不算）
            _rt.anchoredPosition = _home + new Vector2(_offset.x, _offset.y * 0.15f);
            _rt.localRotation = Quaternion.Euler(0f, 0f, -_offset.x * tiltPerUnit);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            LastDragEndTime = Time.unscaledTime;
            LastDragMoved = Mathf.Abs(_offset.x) > 20f;

            // 放手太久之後速度不算（停著想了一下才放開）
            if (Time.unscaledTime - _lastDragTime > 0.1f) _velocityX = 0f;

            float threshold = Mathf.Min(swipeThreshold, _rt.rect.width * swipeFraction);
            bool farEnough = Mathf.Abs(_offset.x) > threshold;
            bool flicked = Mathf.Abs(_velocityX) > flickSpeed && Mathf.Abs(_offset.x) > 40f &&
                           Mathf.Sign(_velocityX) == Mathf.Sign(_offset.x);

            if ((farEnough || flicked) && _offset.x < 0f) StartCoroutine(FlipRoutine(-1));
            else if (farEnough || flicked) StartCoroutine(FlipRoutine(1));
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
