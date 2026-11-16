using System;
using System.Collections;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 餵小熊畫面裡盤子上的一樣食物：點一下，或拖起來放開，都只是把事情回報給 FeedScreen，
    /// 「有沒有餵到」由 FeedScreen 判斷。沒餵到就滑回盤子，不出聲、不算錯。
    /// 拖曳與滑回的寫法照 PuzzlePiece；手抖造成的小拖曳（不到 tapSlop）一律當成點一下，
    /// 不然兩歲的手指一抖，點擊被判成拖曳、東西滑回去，看起來就是「按了沒反應」。
    /// </summary>
    public class FoodItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image image;

        [Tooltip("拖不到這麼遠（版面單位）就放手，算點一下")]
        [SerializeField] private float tapSlop = 40f;

        public CardEntry Card { get; private set; }
        public int Slot { get; private set; }

        /// <summary>已經在飛向小熊（或正在被收走），不再吃任何輸入。</summary>
        public bool IsEating { get; private set; }

        public RectTransform Rect => _rt != null ? _rt : _rt = (RectTransform)transform;

        public event Action<FoodItem> Tapped;
        public event Action<FoodItem> DragStarted;

        /// <summary>拖起來之後放手了；第二個參數是放手時手指的螢幕座標。</summary>
        public event Action<FoodItem, Vector2> Dropped;

        private RectTransform _rt;
        private RectTransform _parent;
        private Vector2 _home;
        private Vector2 _grabOffset;
        private Vector2 _dragStart;
        private bool _dragged;
        private bool _dragging;
        private Coroutine _anim;

        public void Setup(CardEntry card, int slot, Vector2 home, float size)
        {
            Card = card;
            Slot = slot;
            _home = home;
            _parent = Rect.parent as RectTransform;
            IsEating = false;

            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.sizeDelta = new Vector2(size, size);
            Rect.anchoredPosition = home;

            if (image != null)
            {
                image.sprite = card != null ? card.image : null;
                image.enabled = image.sprite != null;
                image.raycastTarget = true;
            }
        }

        /// <summary>從盤子上「啵」地冒出來。</summary>
        public void PopIn()
        {
            StopAnim();
            _anim = StartCoroutine(PopRoutine());
        }

        /// <summary>沒餵到：輕輕滑回盤子。途中可以再被抓起來。</summary>
        public void ReturnHome()
        {
            if (IsEating) return;
            StopAnim();
            _anim = StartCoroutine(MoveHome(0.25f));
        }

        /// <summary>要被吃掉或收走了，之後所有輸入都忽略。</summary>
        public void Lock()
        {
            IsEating = true;
            _dragging = false;
            StopAnim();
            if (image != null) image.raycastTarget = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _anim = null;
        }

        // ---------- 輸入 ----------

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsEating) return;
            _dragged = false;
            StopAnim();
            // 手指一碰就有反應（100ms 內），還沒放開就知道「我按到了」
            Rect.localScale = Vector3.one * 1.08f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsEating || _dragging) return;
            Rect.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 拖過的那一次由 OnEndDrag 處理，這裡只管沒拖的單純點擊
            if (_dragged || IsEating) return;
            Tapped?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEating) return;
            StopAnim();
            _dragged = true;
            _dragging = true;
            _dragStart = Rect.anchoredPosition;
            Rect.SetAsLastSibling();
            if (ToLocal(eventData, out var local)) _grabOffset = Rect.anchoredPosition - local;
            Rect.localScale = Vector3.one * 1.1f;
            DragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || IsEating) return;
            if (ToLocal(eventData, out var local)) Rect.anchoredPosition = local + _grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || IsEating) return;
            _dragging = false;

            if (Vector2.Distance(Rect.anchoredPosition, _dragStart) < tapSlop) Tapped?.Invoke(this);
            else Dropped?.Invoke(this, eventData.position);
        }

        // ---------- 動畫 ----------

        private void StopAnim()
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = null;
        }

        private IEnumerator PopRoutine()
        {
            const float seconds = 0.25f;
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float t = 0f;
            Rect.localScale = Vector3.zero;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds) - 1f;
                // ease-out-back：稍微衝過頭再彈回來
                Rect.localScale = Vector3.one * (1f + c3 * p * p * p + c1 * p * p);
                yield return null;
            }
            Rect.localScale = Vector3.one;
            _anim = null;
        }

        private IEnumerator MoveHome(float seconds)
        {
            var from = Rect.anchoredPosition;
            var fromScale = Rect.localScale;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                Rect.anchoredPosition = Vector2.LerpUnclamped(from, _home, eased);
                Rect.localScale = Vector3.LerpUnclamped(fromScale, Vector3.one, eased);
                yield return null;
            }
            Rect.anchoredPosition = _home;
            Rect.localScale = Vector3.one;
            _anim = null;
        }

        private bool ToLocal(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            if (_parent == null) return false;
            // 父物件（Field）貼滿畫面、中心是錨點，所以本地座標直接等於 anchoredPosition
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out local);
        }
    }
}
