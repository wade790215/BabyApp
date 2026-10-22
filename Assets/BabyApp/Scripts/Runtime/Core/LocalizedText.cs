using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.Core
{
    /// <summary>掛在 Text 上，文字跟著設定裡的語言走。key 對應 Strings 裡的表。</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text _text;

        /// <summary>建場景時用來指定 key，順便先把文字填好。</summary>
        public void SetKey(string value)
        {
            key = value;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
            AppSettings.Changed += Apply;
        }

        private void OnDisable() => AppSettings.Changed -= Apply;

        private void Apply()
        {
            if (_text == null) _text = GetComponent<Text>();
            if (_text != null) _text.text = Strings.Get(key);
        }
    }
}
