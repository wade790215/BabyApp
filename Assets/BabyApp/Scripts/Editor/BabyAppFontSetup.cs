using System.IO;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 準備一個能顯示中文的字體。Unity 內建的 Arial 沒有中文字，不處理的話
    /// 卡片名稱會變成一排空白方塊。
    ///
    /// 開發期直接借用 Windows 內建的標楷體（筆畫標準，適合給小孩看字）。
    /// 注意：標楷體不能隨 APK 散佈，如果以後要上架或給別人安裝，
    /// 請換成 Google Noto Sans TC（開源字體授權，可自由散佈），
    /// 檔案換掉同名的 BabyApp_Chinese.ttf 就好，場景不用重做。
    /// </summary>
    public static class BabyAppFontSetup
    {
        private static readonly string[] CandidateSystemFonts =
        {
            @"C:\Windows\Fonts\kaiu.ttf",   // 標楷體
            @"C:\Windows\Fonts\msjh.ttc",   // 微軟正黑體
            @"C:\Windows\Fonts\mingliu.ttc" // 細明體
        };

        public static Font EnsureFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Font>(BabyAppPaths.DevFont);
            if (existing != null) return existing;

            // 使用者自己放的字體優先（任何 ttf/otf 都接受）
            var userFont = FindUserFont();
            if (userFont != null)
            {
                Debug.Log($"[BabyApp] 使用你放的字體：{AssetDatabase.GetAssetPath(userFont)}");
                return userFont;
            }

            string source = null;
            foreach (var candidate in CandidateSystemFonts)
            {
                if (File.Exists(candidate)) { source = candidate; break; }
            }

            if (source == null)
            {
                Debug.LogError(
                    "[BabyApp] 找不到中文字體。請下載 Noto Sans TC 的 .ttf，" +
                    $"改名成 BabyApp_Chinese.ttf 放進 {BabyAppPaths.Fonts}。");
                return null;
            }

            Directory.CreateDirectory(BabyAppPaths.Fonts);
            File.Copy(source, BabyAppPaths.DevFont, true);
            AssetDatabase.ImportAsset(BabyAppPaths.DevFont, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(BabyAppPaths.DevFont) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            Debug.LogWarning(
                $"[BabyApp] 已從 {source} 複製一份字體當開發用。" +
                "要上架或分享給別人前，請換成 Noto Sans TC 等可散佈的字體。");

            return AssetDatabase.LoadAssetAtPath<Font>(BabyAppPaths.DevFont);
        }

        private static Font FindUserFont()
        {
            var guids = AssetDatabase.FindAssets("t:Font", new[] { BabyAppPaths.Fonts });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null) return font;
            }
            return null;
        }
    }
}
