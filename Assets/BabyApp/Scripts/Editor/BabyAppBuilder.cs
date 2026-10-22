using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 一鍵出包。兩個平台都放在專案根目錄的 Builds/ 底下：
    ///   Builds/Android/BabyApp.apk   → 直接傳到 Android 平板裝
    ///   Builds/iOS/                  → Xcode 專案，拿到 Mac 上用 Xcode 開、簽、裝到 iPad
    /// 出包前會先套一次播放器設定，確保兩邊的 Bundle ID、方向鎖定都是對的。
    /// </summary>
    public static class BabyAppBuilder
    {
        public static void BuildAndroid()
        {
            BabyAppPlayerSetup.Apply();

            string output = Path.Combine(BabyAppPaths.BuildsFolder, "Android", "BabyApp.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            EditorUserBuildSettings.buildAppBundle = false; // 自己裝用 APK，上 Play 商店才需要 AAB
            Run(new BuildPlayerOptions
            {
                scenes = new[] { BabyAppPaths.MainScene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            }, "Android APK");
        }

        public static void BuildIos()
        {
            BabyAppPlayerSetup.Apply();

            string output = Path.Combine(BabyAppPaths.BuildsFolder, "iOS");
            Directory.CreateDirectory(output);

            Run(new BuildPlayerOptions
            {
                scenes = new[] { BabyAppPaths.MainScene },
                locationPathName = output,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            }, "iOS Xcode 專案");
        }

        private static void Run(BuildPlayerOptions options, string label)
        {
            if (!File.Exists(BabyAppPaths.MainScene))
            {
                Debug.LogError("[BabyApp] 還沒有場景，請先跑 BabyApp → 1. 一鍵初始化專案。");
                return;
            }

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[BabyApp] " + label + " 出包完成：" + summary.outputPath +
                          "（" + (summary.totalSize / (1024 * 1024)) + " MB）");
                EditorUtility.RevealInFinder(summary.outputPath);
            }
            else
            {
                Debug.LogError("[BabyApp] " + label + " 出包失敗：" + summary.result +
                               "。往上捲 Console 看第一個紅字，通常是 SDK/NDK 沒裝或簽章問題。");
            }
        }
    }
}
