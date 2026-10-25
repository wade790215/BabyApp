using BabyApp.Core;
using UnityEngine;

namespace BabyApp.UI
{
    /// <summary>
    /// 格子上的文字只給家長看：設定裡「顯示文字」關著（預設）時整塊藏起來，
    /// 上面的圖往下長滿整格；打開時圖讓出下面一條放名稱。
    /// 掛在格子上，接線交給 Builder。
    /// </summary>
    public class TileLabelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject labelGroup;
        [SerializeField] private RectTransform content;
        [SerializeField] private Vector2 anchorMinWithLabel = new Vector2(0.25f, 0.36f);
        [SerializeField] private Vector2 anchorMaxWithLabel = new Vector2(0.75f, 0.88f);
        [SerializeField] private Vector2 anchorMinNoLabel = new Vector2(0.18f, 0.18f);
        [SerializeField] private Vector2 anchorMaxNoLabel = new Vector2(0.82f, 0.82f);

        /// <summary>Builder 用：一次把兩組錨點設好。</summary>
        public void Configure(GameObject label, RectTransform target, Vector2 withMin, Vector2 withMax, Vector2 noMin, Vector2 noMax)
        {
            labelGroup = label;
            content = target;
            anchorMinWithLabel = withMin;
            anchorMaxWithLabel = withMax;
            anchorMinNoLabel = noMin;
            anchorMaxNoLabel = noMax;
        }

        private void OnEnable()
        {
            Apply();
            AppSettings.Changed += Apply;
        }

        private void OnDisable() => AppSettings.Changed -= Apply;

        private void Apply()
        {
            bool show = AppSettings.ShowLabels;
            if (labelGroup != null) labelGroup.SetActive(show);
            if (content != null)
            {
                content.anchorMin = show ? anchorMinWithLabel : anchorMinNoLabel;
                content.anchorMax = show ? anchorMaxWithLabel : anchorMaxNoLabel;
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            }
        }
    }
}
