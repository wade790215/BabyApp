using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 換裝頁右邊的一格配件。格子自己不會動（它在 GridLayoutGroup 底下，版面一重排位置就被蓋回去），
    /// 只負責把「被點了」「被拖起來、拖到哪、放手了」告訴 DressScreen；飛來飛去的是 DressScreen 生的複製圖。
    /// 東西被拿去戴的時候，格子裡只剩一個淡淡的影子。
    /// </summary>
    public class OutfitTile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image icon;

        [Tooltip("東西不在格子裡（戴在頭上、正在飛、被拖著）時，留在格子裡的影子有多淡")]
        [SerializeField] private float awayAlpha = 0.22f;

        /// <summary>Outfit.All 裡的第幾個。</summary>
        public int Index { get; private set; }
        public Image Icon => icon;

        public event Action<OutfitTile> Tapped;
        /// <summary>第二個參數是手指在螢幕上的位置。</summary>
        public event Action<OutfitTile, Vector2> DragStarted;
        public event Action<OutfitTile, Vector2> DragMoved;
        public event Action<OutfitTile, Vector2> DragEnded;

        private const int NoPointer = int.MinValue;
        private int _pointerId = NoPointer;
        private bool _dragged;

        public void Setup(int index) => Index = index;

        public void SetAway(bool away)
        {
            if (icon == null) return;
            var color = icon.color;
            color.a = away ? awayAlpha : 1f;
            icon.color = color;
        }

        private void OnDisable()
        {
            // 拖到一半畫面被關掉，收不到 OnEndDrag，自己收乾淨
            _pointerId = NoPointer;
            _dragged = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 手指一碰就有反應，還沒放開就知道「我按到了」
            transform.localScale = Vector3.one * 0.94f;
            // 一指正在拖的時候，另一指按下來不能把「拖過了」洗掉，不然放手會多算一次點擊
            if (_pointerId == NoPointer) _dragged = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 拖過的那一次由 OnEndDrag 處理，這裡只管沒拖的單純點擊
            if (_dragged) return;
            Tapped?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 同一格只認第一根手指
            if (_pointerId != NoPointer) return;
            _pointerId = eventData.pointerId;
            _dragged = true;
            DragStarted?.Invoke(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            DragMoved?.Invoke(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = NoPointer;
            DragEnded?.Invoke(this, eventData.position);
        }
    }
}
