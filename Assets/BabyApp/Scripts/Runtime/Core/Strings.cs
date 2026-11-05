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
                ["blank"] = ("空", "Blank"),
                ["record"] = ("錄", "Rec"),
                ["clear_recordings"] = ("清除錄音", "Clear recordings"),
                ["find"] = ("找一找", "Find It"),
                ["count"] = ("數數", "Count"),
                ["cards"] = ("卡片", "Cards"),
                ["touch"] = ("摸一摸", "Touch"),
                ["match"] = ("配對", "Match"),
                ["puzzle"] = ("拼圖", "Puzzle"),
                ["labels"] = ("顯示文字", "Show labels"),
                ["stats"] = ("今天翻了 {0} 張　最愛：{1}　錄了 {2} 段　找對 {3} 次　數了 {4} 回",
                             "Today: {0} cards · Favourite: {1} · {2} recordings · {3} found · {4} counted"),
                ["none"] = ("還沒有", "none"),
                // 家人相片卡（家長頁）
                ["family_cards"] = ("家人卡", "Family cards"),
                ["family_notice"] = ("照片和錄音只存在這台平板上，換平板或刪除 App 就會不見。\n按住麥克風錄稱呼，按住橡皮擦刪除。",
                                     "Photos and recordings stay on this tablet only; switching tablets or deleting the app loses them.\nHold the mic to record a name, hold the eraser to delete."),
                ["family_custom"] = ("自訂 {0}", "Custom {0}"),
                ["family_need_photo"] = ("還沒選照片", "Pick a photo"),
                ["family_need_voice"] = ("還沒錄稱呼", "Record a name"),
                ["family_builtin_voice"] = ("可以玩了（用內建稱呼）", "Ready (built-in name)"),
                ["family_ready"] = ("可以玩了", "Ready"),
                ["family_pick_title"] = ("選一張照片", "Choose a photo"),
                ["back_settings"] = ("回設定", "Back")
            };

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!Table.TryGetValue(key, out var pair)) return key;
            return AppSettings.IsEnglish ? pair.en : pair.zh;
        }
    }
}
