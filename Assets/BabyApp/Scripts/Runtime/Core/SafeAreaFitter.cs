using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 把自己的範圍縮到螢幕的安全區裡：避開 iPhone 的瀏海、iPad 底部那條、Android 的鏡頭洞。
    /// 所有畫面都放在這底下，背景則留在外面鋪滿整個螢幕。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _applied;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            // 轉向或分割畫面時安全區會變，每幀比一下很便宜
            if (Screen.safeArea != _applied) Apply();
        }

        private void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;

            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0f || h <= 0f) return;

            var min = new Vector2(area.xMin / w, area.yMin / h);
            var max = new Vector2(area.xMax / w, area.yMax / h);

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
