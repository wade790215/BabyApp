using System.Collections.Generic;

namespace BabyApp.Core
{
    /// <summary>
    /// 介面上會出現的所有文字，中英各一份。卡片名稱不在這裡，在 CardManifest.csv。
    /// 要加新文字就加一行；取用時 Strings.Get("key")。
    /// </summary>
    public static class Strings
    {
        private static readonly Dictionary<string, (string zh, string en)> Table =
            new Dictionary<string, (string, string)>
            {
                ["title"] = ("我會認東西", "Look and Learn"),
                ["paint"] = ("畫畫", "Paint"),
                ["settings"] = ("設定", "Settings"),
                ["sfx"] = ("音效", "Sounds"),
                ["voice"] = ("語音", "Voice"),
                ["music"] = ("音樂", "Music"),
                ["background"] = ("背景", "Background"),
                ["language"] = ("語言", "Language"),
                ["language_name"] = ("中文", "English"),
                ["back_home"] = ("回首頁", "Home"),
                ["on"] = ("開", "On"),
                ["off"] = ("關", "Off"),
                ["home"] = ("家", "Home"),
                ["set"] = ("設", "Set"),
                ["clear"] = ("擦", "Clear"),
                ["pic"] = ("圖", "Pic"),
                ["blank"] = ("空", "Blank")
            };

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!Table.TryGetValue(key, out var pair)) return key;
            return AppSettings.IsEnglish ? pair.en : pair.zh;
        }
    }
}
