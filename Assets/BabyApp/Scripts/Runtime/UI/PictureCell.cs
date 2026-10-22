using System;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>選圖牆上的一格：一張縮圖（或「空」字），點了就選它。</summary>
    public class PictureCell : MonoBehaviour
    {
        [SerializeField] private Image thumb;
        [SerializeField] private Text label;
        [SerializeField] private Button button;

        public void Setup(Sprite sprite, string text, Action onChosen)
        {
            if (thumb != null)
            {
                thumb.sprite = sprite;
                thumb.enabled = sprite != null;
            }
            if (label != null)
            {
                label.text = text ?? string.Empty;
                label.enabled = !string.IsNullOrEmpty(text);
            }
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onChosen?.Invoke());
            }
        }
    }
}
