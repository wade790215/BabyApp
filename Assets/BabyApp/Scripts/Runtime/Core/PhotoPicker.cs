using System;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 從相簿挑一張照片，縮到卡片用的大小。包著 NativeGallery：
    /// iOS 走系統的 PHPicker（行程外、不用相簿權限），Android 走相簿選擇器並設成免權限模式，
    /// Editor 裡會跳出檔案視窗，所以 Play 模式就能測整個流程。
    /// 第一版只有相簿、沒有相機。
    /// </summary>
    public static class PhotoPicker
    {
        /// <summary>縮圖的長邊上限。卡片在 iPad 上最大也不到這個尺寸。</summary>
        public const int MaxSide = 1024;

        private static bool _androidConfigured;

        public static bool IsBusy => NativeGallery.IsMediaPickerBusy();

        /// <summary>
        /// 跳出挑圖器。選好就回傳一張可讀的 Texture2D（長邊 ≤ MaxSide，RGB24），
        /// 取消或讀不到就回 null。回傳的 texture 由呼叫端負責 Destroy。
        /// </summary>
        public static void Pick(Action<Texture2D> done)
        {
            ConfigureAndroid();

            NativeGallery.GetImageFromGallery(path =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    done?.Invoke(null);
                    return;
                }

                // 實機上原生端就先縮圖並套用 EXIF 方向，不會整張 4000×3000 解進記憶體
                var loaded = NativeGallery.LoadImageAtPath(path, MaxSide, markTextureNonReadable: false, generateMipmaps: false);
                if (loaded == null)
                {
                    Debug.LogWarning("[BabyApp] 讀不到挑選的照片：" + path);
                    done?.Invoke(null);
                    return;
                }

                // Editor 的 LoadImageAtPath 不縮圖，這裡再保險縮一次；已經夠小就原樣回傳
                var fitted = FitWithin(loaded, MaxSide);
                if (fitted != loaded) UnityEngine.Object.Destroy(loaded);
                done?.Invoke(fitted);
            }, Strings.Get("family_pick_title"));
        }

        /// <summary>
        /// 等比縮到長邊 ≤ maxSide。用 GPU 一路縮一半、縮一半，避免一次大比例的雙線性縮圖出現鋸齒。
        /// 專案是 Linear 色彩空間，RenderTexture 要用 sRGB 讀回來顏色才不會跑掉。
        /// </summary>
        public static Texture2D FitWithin(Texture2D source, int maxSide)
        {
            if (source == null) return null;
            int w = source.width, h = source.height;
            if (Mathf.Max(w, h) <= maxSide) return source;

            float scale = (float)maxSide / Mathf.Max(w, h);
            int targetW = Mathf.Max(1, Mathf.RoundToInt(w * scale));
            int targetH = Mathf.Max(1, Mathf.RoundToInt(h * scale));

            Texture current = source;
            RenderTexture previousRt = null;
            var prevActive = RenderTexture.active;

            try
            {
                while (w > targetW || h > targetH)
                {
                    int nextW = Mathf.Max(targetW, w / 2);
                    int nextH = Mathf.Max(targetH, h / 2);
                    var rt = RenderTexture.GetTemporary(nextW, nextH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    rt.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(current, rt);
                    if (previousRt != null) RenderTexture.ReleaseTemporary(previousRt);
                    previousRt = rt;
                    current = rt;
                    w = nextW; h = nextH;
                }

                RenderTexture.active = previousRt;
                var result = new Texture2D(w, h, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                result.Apply(false, false);
                result.wrapMode = TextureWrapMode.Clamp;
                return result;
            }
            finally
            {
                // 不管中間有沒有出錯，active 要還原、暫存的 RT 要放掉
                RenderTexture.active = prevActive;
                if (previousRt != null) RenderTexture.ReleaseTemporary(previousRt);
            }
        }

        /// <summary>
        /// Android：NativeGallery 預設會在 Android 12 以下跳存取權限詢問；
        /// 改成免權限模式 + ACTION_GET_CONTENT，系統選擇器會把那一張的讀取權限交給 App，不用整個相簿權限。
        /// </summary>
        private static void ConfigureAndroid()
        {
            if (_androidConfigured) return;
            _androidConfigured = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var gallery = new AndroidJavaClass("com.yasirkula.unity.NativeGallery"))
                    gallery.SetStatic("PermissionFreeMode", true);
                using (var fragment = new AndroidJavaClass("com.yasirkula.unity.NativeGalleryMediaPickerFragment"))
                    fragment.SetStatic("preferGetContent", true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BabyApp] 設定 NativeGallery 免權限模式失敗：" + e.Message);
            }
#endif
        }
    }
}
