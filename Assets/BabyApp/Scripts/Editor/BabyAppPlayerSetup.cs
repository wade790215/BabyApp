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

            // Unity 已把 24 標成 obsolete（下一版會變 error）；26 是 Android 8.0，平板早就都過了
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
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
            // 錄音功能要跟 iOS 說明為什麼要用麥克風，不然一按錄音 App 會被系統關掉
            // 家長也會錄家人的稱呼，用途要寫實
            PlayerSettings.iOS.microphoneUsageDescription = "錄下家人和小朋友的聲音，念出卡片上的名稱。";

            // 家人相片卡：NativeGallery 在 iOS 用 PHPicker 本身不需要權限，
            // 但它會連結 Photos.framework，用途字串留著並寫成真實說明，免得上傳時被 ITMS-90683 擋下。
            // 這些值寫進 ProjectSettings/NativeGallery.json，Xcode 匯出時由套件自己套用。
            var gallery = NativeGalleryNamespace.Settings.Instance;
            gallery.AutomatedSetup = true; // 要靠它連結 PhotosUI.framework
            gallery.PhotoLibraryUsageDescription = "讓家長從相簿挑一張家人的照片做成卡片；照片只存在這台裝置上。";
            gallery.PhotoLibraryAdditionsUsageDescription = "本 App 不會把照片存進你的相簿。";
            gallery.DontAskLimitedPhotosPermissionAutomaticallyOnIos14 = true;
            gallery.Save();

            // 這支 App 只有 UI，不需要為了 3D 吃掉平板的電
            QualitySettings.vSyncCount = 1;

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[BabyApp] 已套用播放器設定：橫向鎖定、套件名稱 " + AppId + "、Android ARM64 + IL2CPP、iOS 只給 iPad、相簿用途說明。\n" +
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
