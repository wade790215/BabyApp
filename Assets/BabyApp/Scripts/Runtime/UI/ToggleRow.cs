using System;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.Core
{
    /// <summary>設定頁的一個開關：按一下切換，綠底「開」／灰底「關」。文字跟著語言走。</summary>
    public class ToggleRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image face;
        [SerializeField] private Text stateLabel;

        private static readonly Color OnColor = new Color32(0x34, 0xC7, 0x59, 0xFF);
        private static readonly Color OffColor = new Color32(0xBD, 0xBD, 0xBD, 0xFF);

        public event Action<bool> Changed;

        private bool _value;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Set(!_value, notify: true));
        }

        /// <summary>設定顯示狀態；notify 為 true 時會發 Changed。</summary>
        public void Set(bool value, bool notify)
        {
            _value = value;
            if (face != null) face.color = value ? OnColor : OffColor;
            if (stateLabel != null) stateLabel.text = Strings.Get(value ? "on" : "off");
            if (notify) Changed?.Invoke(value);
        }
    }
}
