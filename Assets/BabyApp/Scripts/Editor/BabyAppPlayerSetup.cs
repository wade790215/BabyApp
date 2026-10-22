using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 套用給小小孩用的平板設定：鎖橫向、不要有多餘的畫面旋轉、
    /// 以及 Android 出包需要的最低設定。
    /// </summary>
    public static class BabyAppPlayerSetup
    {
        private const string AppId = "com.yile.babyapp";

        public static void Apply()
        {
            PlayerSettings.companyName = "Yile";
            PlayerSettings.productName = "我會認東西";
            PlayerSettings.bundleVersion = "0.1.0";

            // 鎖橫向。2～3 歲拿平板很容易轉來轉去，畫面跟著轉會打斷他看圖
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            // 瀏海與圓角螢幕的平板上也佔滿整個畫面
            PlayerSettings.Android.renderOutsideSafeArea = true;

            // 現在的 Android 裝置多半只跑 64 位元，ARM64 + IL2CPP 是能裝得起來的組合
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // iOS：只給 iPad、藏狀態列、佔滿全螢幕。真正編譯與安裝要在 Mac 上用 Xcode。
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            // 這支 App 只有 UI，不需要為了 3D 吃掉平板的電
            QualitySettings.vSyncCount = 1;

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[BabyApp] 已套用播放器設定：橫向鎖定、套件名稱 " + AppId + "、Android ARM64 + IL2CPP、iOS 只給 iPad。\n" +
                "出 APK 前請確認 Unity Hub 裡 Android Build Support 的 SDK / NDK / OpenJDK 都有裝；\n" +
                "出 iPad 版要先切到 iOS 平台匯出 Xcode 專案，再在 Mac 上用 Xcode 編譯安裝。");
        }

        public static void SwitchToIos()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                Debug.Log("[BabyApp] 目前已經是 iOS 平台。");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "切換到 iOS",
                    "切換平台會重新壓縮一次所有素材，第一次可能要等幾分鐘。\n" +
                    "注意：Windows 上只能匯出 Xcode 專案，編譯和裝到 iPad 要在 Mac 上做。要現在切嗎？",
                    "切換", "稍後"))
            {
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS);
        }

        public static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[BabyApp] 目前已經是 Android 平台。");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "切換到 Android",
                    "切換平台會重新壓縮一次所有素材，第一次可能要等幾分鐘。要現在切嗎？",
                    "切換", "稍後"))
            {
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
        }
    }
}
