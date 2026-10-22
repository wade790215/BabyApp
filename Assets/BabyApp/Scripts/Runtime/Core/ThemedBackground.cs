using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.Core
{
    /// <summary>掛在每個畫面的背景 Image 上，顏色跟著設定裡選的背景色走。</summary>
    [RequireComponent(typeof(Image))]
    public class ThemedBackground : MonoBehaviour
    {
        private Image _image;

        private void Awake() => _image = GetComponent<Image>();

        private void OnEnable()
        {
            Apply();
            AppSettings.Changed += Apply;
        }

        private void OnDisable() => AppSettings.Changed -= Apply;

        private void Apply()
        {
            if (_image != null) _image.color = AppSettings.BackgroundColor;
        }
    }
}
