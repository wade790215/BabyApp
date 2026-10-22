using System.IO;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 介面用的小圖示用程式畫出來存成 PNG，這樣就不用等 AI 生圖也不用找素材包。
    /// 目前只有左右換頁的三角箭頭，圓形底用 Unity 內建的 Knob。
    /// </summary>
    public static class BabyAppUiSprites
    {
        private const int Size = 128;

        public static Sprite EnsureArrow()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.ArrowSprite);
            if (existing != null) return existing;

            Directory.CreateDirectory(BabyAppPaths.UiArt);

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];

            // 指向右邊的三角形，用 4x4 超取樣讓邊緣不要有鋸齒
            Vector2 a = new Vector2(26f, 14f);
            Vector2 b = new Vector2(26f, Size - 14f);
            Vector2 c = new Vector2(Size - 18f, Size * 0.5f);

            const int samples = 4;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            var p = new Vector2(
                                x + (sx + 0.5f) / samples,
                                y + (sy + 0.5f) / samples);
                            if (InTriangle(p, a, b, c)) hits++;
                        }
                    }
                    byte alpha = (byte)(255 * hits / (samples * samples));
                    pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(BabyAppPaths.ArrowSprite, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(BabyAppPaths.ArrowSprite, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.ArrowSprite);
        }

        /// <summary>
        /// 工地警示條紋：黃黑 45° 斜紋，64x64 一片，週期 32 所以平鋪起來是無縫的。
        /// 用在首頁與卡片畫面底部那條帶子。
        /// </summary>
        public static Sprite EnsureStripe()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.StripeSprite);
            if (existing != null) return existing;

            Directory.CreateDirectory(BabyAppPaths.UiArt);

            const int size = 64;
            const int period = 32;
            var yellow = new Color32(0xFF, 0xC1, 0x07, 0xFF);
            var black = new Color32(0x1A, 0x1A, 0x1A, 0xFF);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isYellow = ((x + y) % period) < period / 2;
                    pixels[y * size + x] = isYellow ? yellow : black;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(BabyAppPaths.StripeSprite, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(BabyAppPaths.StripeSprite, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.StripeSprite);
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p - a, b - a);
            float d2 = Cross(p - b, c - b);
            float d3 = Cross(p - c, a - c);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
    }
}
