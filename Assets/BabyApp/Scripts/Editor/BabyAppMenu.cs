using System.Text;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// Unity 上方選單的 BabyApp 項目。日常只會用到前兩項：
    /// 丟新素材進來 → 「2. 匯入卡片素材」→ 按 Play。
    /// </summary>
    public static class BabyAppMenu
    {
        [MenuItem("BabyApp/1. 一鍵初始化專案", priority = 10)]
        public static void InitialiseEverything()
        {
            BabyAppFontSetup.EnsureFont();
            ImportCards();
            BabyAppPlayerSetup.Apply();
            BabyAppSceneBuilder.BuildScene();
        }

        [MenuItem("BabyApp/2. 匯入卡片素材", priority = 20)]
        public static void ImportCards()
        {
            // 顏色卡片組的圖是程式畫的，先確定它們在，匯入才接得上
            BabyAppUiSprites.EnsureColorCards();
            BabyAppDeckImporter.Import();
        }

        [MenuItem("BabyApp/3. 重建場景", priority = 30)]
        public static void RebuildScene() => BabyAppSceneBuilder.BuildScene();

        [MenuItem("BabyApp/4. 套用播放器設定", priority = 40)]
        public static void ApplyPlayerSettings() => BabyAppPlayerSetup.Apply();

        [MenuItem("BabyApp/5. 切換平台到 Android", priority = 50)]
        public static void SwitchToAndroid() => BabyAppPlayerSetup.SwitchToAndroid();

        [MenuItem("BabyApp/6. 切換平台到 iOS (iPad)", priority = 60)]
        public static void SwitchToIos() => BabyAppPlayerSetup.SwitchToIos();

        [MenuItem("BabyApp/7. 出 Android APK", priority = 70)]
        public static void BuildAndroid() => BabyAppBuilder.BuildAndroid();

        [MenuItem("BabyApp/8. 匯出 iOS Xcode 專案", priority = 80)]
        public static void BuildIos() => BabyAppBuilder.BuildIos();

        [MenuItem("BabyApp/檢查還缺哪些素材", priority = 100)]
        public static void ReportMissingAssets()
        {
            var missing = BabyAppDeckImporter.MissingImageIds();

            if (missing.Count == 0)
            {
                Debug.Log("[BabyApp] 清單上每張卡都有圖了。");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("[BabyApp] 還缺 " + missing.Count + " 張圖，檔名要跟下面的代號一樣（.png）：");
            foreach (var id in missing) builder.AppendLine("  " + id);
            builder.AppendLine("生圖的提示詞在 docs/AI圖片生成指南.md。");

            Debug.Log(builder.ToString());
        }
    }
}
