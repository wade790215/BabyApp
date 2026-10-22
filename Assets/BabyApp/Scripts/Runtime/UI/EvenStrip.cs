using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.UI
{
    /// <summary>
    /// 把子物件沿一條線（直或橫）平均排開：空間夠就拉大間距（有上限），
    /// 空間不夠就把整條等比縮小，永遠不會重疊。子物件的大小用它們自己的 sizeDelta，不會被改。
    ///
    /// 縮的是這一條的 localScale 而不是每個子物件的，這樣子物件上的彈跳動畫（Pulse）
    /// 還是以 1 為基準，不會動完一次就把排版彈壞。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EvenStrip : MonoBehaviour
    {
        [SerializeField] private bool horizontal;
        [SerializeField] private float minSpacing = 12f;
        [SerializeField] private float maxSpacing = 70f;
        [SerializeField] private float padding = 10f;

        private RectTransform _rt;
        private readonly List<RectTransform> _items = new List<RectTransform>();

        public void Configure(bool isHorizontal, float min, float max)
        {
            horizontal = isHorizontal;
            minSpacing = min;
            maxSpacing = max;
        }

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable() => Layout();
        private void OnRectTransformDimensionsChange() => Layout();
        private void OnTransformChildrenChanged() => Layout();

        private void Layout()
        {
            if (_rt == null) _rt = (RectTransform)transform;

            _items.Clear();
            float total = 0f;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var item = child as RectTransform;
                if (item == null) continue;
                _items.Add(item);
                total += Extent(item);
            }

            int count = _items.Count;
            if (count == 0) return;

            float available = (horizontal ? _rt.rect.width : _rt.rect.height) - padding * 2f;
            if (available <= 0f) return;

            // 先假設不用縮：間距在上下限之間
            float gaps = Mathf.Max(0, count - 1);
            float spacing = gaps > 0 ? Mathf.Clamp((available - total) / gaps, minSpacing, maxSpacing) : 0f;
            float needed = total + spacing * gaps;

            // 連最小間距都塞不下，就整條等比縮小
            float scale = needed > available ? available / needed : 1f;
            _rt.localScale = Vector3.one * scale;

            // 在這一條的座標裡（未縮放）從頭排到尾，整體置中
            float cursor = -needed * 0.5f;
            foreach (var item in _items)
            {
                float size = Extent(item);
                item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                float center = cursor + size * 0.5f;
                // 直的從上往下排，所以 y 要反過來
                item.anchoredPosition = horizontal ? new Vector2(center, 0f) : new Vector2(0f, -center);
                cursor += size + spacing;
            }
        }

        private float Extent(RectTransform item) => horizontal ? item.sizeDelta.x : item.sizeDelta.y;
    }
}
