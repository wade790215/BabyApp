using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 一片拼圖：跟著手指走，放手時夠靠近自己的位置就「咔」一聲吸進去、鎖住；不夠近就滑回原處。
    /// 每片是整張圖的一塊：用 RectMask2D 只露出自己那一格，所以不用真的切圖。
    /// </summary>
    public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform frame;
        [SerializeField] private RectTransform picture;

        /// <summary>吸進正確位置時觸發。</summary>
        public event Action Placed;

        public bool IsPlaced { get; private set; }

        private RectTransform _rt;
        private RectTransform _parent;
        private Vector2 _target;
        private float _snapDistance;
        private Vector2 _grabOffset;
        private Vector2 _restPosition;
        private float _restScale = 1f;
        private bool _dragging;
        private Coroutine _move;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _parent = _rt.parent as RectTransform;
        }

        /// <summary>
        /// 設定這片是哪一塊：整張圖 fullSize、這片的格子 (col,row)、幾欄幾列、正確位置 target、
        /// 托盤裡的位置 rest 與縮放 restScale（托盤放不下就縮小一點，拖起來會回到原寸）。
        /// </summary>
        public void Setup(Sprite sprite, Vector2 fullSize, int col, int row, int cols, int rows,
            Vector2 target, Vector2 rest, float snapDistance, float restScale = 1f)
        {
            _rt = (RectTransform)transform;
            _parent = _rt.parent as RectTransform;
            _target = target;
            _restPosition = rest;
            _restScale = restScale;
            _snapDistance = snapDistance;
            IsPlaced = false;

            var pieceSize = new Vector2(fullSize.x / cols, fullSize.y / rows);
            _rt.sizeDelta = pieceSize;
            _rt.anchoredPosition = rest;
            _rt.localScale = Vector3.one * restScale;

            if (picture != null)
            {
                var image = picture.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                    image.enabled = sprite != null;
                    image.preserveAspect = false;
                }
                // 整張圖放在遮罩裡，往反方向推，剛好露出自己那一格（row 0 在最上面）
                picture.anchorMin = picture.anchorMax = picture.pivot = new Vector2(0.5f, 0.5f);
                picture.sizeDelta = fullSize;
                float offsetX = (cols - 1) * 0.5f * pieceSize.x - col * pieceSize.x;
                float offsetY = -((rows - 1) * 0.5f * pieceSize.y) + row * pieceSize.y;
                picture.anchoredPosition = new Vector2(offsetX, offsetY);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsPlaced) return;
            if (_move != null) { StopCoroutine(_move); _move = null; }
            _dragging = true;
            _rt.SetAsLastSibling();
            if (ToLocal(eventData, out var local)) _grabOffset = _rt.anchoredPosition - local;
            _rt.localScale = Vector3.one * 1.06f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (ToLocal(eventData, out var local)) _rt.anchoredPosition = local + _grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            if (Vector2.Distance(_rt.anchoredPosition, _target) <= _snapDistance)
            {
                IsPlaced = true;
                _rt.localScale = Vector3.one;
                _move = StartCoroutine(MoveTo(_target, 0.12f, then: () =>
                {
                    _rt.SetAsFirstSibling(); // 放好的壓到最下面，不要擋住還在拖的
                    Placed?.Invoke();
                }));
            }
            else
            {
                _rt.localScale = Vector3.one * _restScale;
                _move = StartCoroutine(MoveTo(_restPosition, 0.25f, null));
            }
        }

        private IEnumerator MoveTo(Vector2 to, float seconds, Action then)
        {
            var from = _rt.anchoredPosition;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                _rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }
            _rt.anchoredPosition = to;
            _move = null;
            then?.Invoke();
        }

        private bool ToLocal(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            if (_parent == null) return false;
            // 父物件中心是錨點（Setup 時 anchoredPosition 直接等於本地座標）
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out local);
        }
    }
}
