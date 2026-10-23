using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 介面用的圖示用程式畫出來存成 PNG：箭頭、房子、麥克風、橡皮擦、圖片、齒輪、放大鏡、數數、播放。
    /// 全部是白色剪影，放到 Image 上用顏色染；風格一致、不用等 AI 生圖也不用找素材包。
    /// 另外也畫顏色卡片組的色球。
    /// </summary>
    public static class BabyAppUiSprites
    {
        /// <summary>大圓角底圖的圓角半徑（像素）。所有方塊、卡片、面板都用這張九宮格拉。</summary>
        public const float RoundRectRadius = 72f;

        public static Sprite EnsureArrow() => EnsureIcon("arrow");

        /// <summary>白色大圓角方形，九宮格切，用來當所有方塊的底，角才會又大又柔。</summary>
        public static Sprite EnsureRoundRect()
        {
            string path = BabyAppPaths.UiArt + "/roundrect.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(BabyAppPaths.UiArt);
            const int size = 256;
            float r = RoundRectRadius * 128f / size; // 畫布是 0～128 座標
            var canvas = new Canvas(size);
            canvas.Fill((x, y) => Inside.RoundRect(x, y, 0f, 0f, 128f, 128f, r));
            File.WriteAllBytes(path, canvas.ToPng(Color.white));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// 程式畫的蜂蜜小熊五張表情，存到 Art/Mascot 當佔位；AI 圖生好後同名覆蓋。
        /// 圓頭圓身、小耳朵、奶油色肚子與嘴巴、抱著藍色蜂蜜罐。已存在的檔跳過。
        /// </summary>
        public static void EnsureMascotPlaceholders()
        {
            Directory.CreateDirectory(BabyAppPaths.MascotArt);
            bool any = false;
            foreach (var pose in BabyAppPaths.MascotPoses)
            {
                string path = BabyAppPaths.MascotArt + "/" + pose + ".png";
                if (File.Exists(path)) continue;
                File.WriteAllBytes(path, DrawBear(pose.Replace("mascot_", "")));
                any = true;
            }
            if (any) AssetDatabase.Refresh();
        }

        private static byte[] DrawBear(string pose)
        {
            const int size = 512;
            var outline = new Canvas(size);   // 深色描邊（每個形狀放大一點）
            var fur = new Canvas(size);       // 金黃色
            var cream = new Canvas(size);     // 肚子、嘴巴、耳朵內側
            var pot = new Canvas(size);       // 藍色蜂蜜罐
            var ink = new Canvas(size);       // 眼睛、鼻子、嘴
            var white = new Canvas(size);     // 眼睛反光

            bool happy = pose == "happy", surprised = pose == "surprised", think = pose == "think", sleep = pose == "sleep";
            float armY = happy ? 76f : 50f;              // 開心時雙手舉高
            float rightArmX = think ? 86f : 96f;         // 思考時右手靠近下巴
            float rightArmY = think ? 68f : armY;
            float headTilt = sleep ? 6f : 0f;

            void Blob(float cx, float cy, float r, Canvas layer)
            {
                outline.Fill((x, y) => Inside.Circle(x, y, cx, cy, r + 2.2f));
                layer.Fill((x, y) => Inside.Circle(x, y, cx, cy, r));
            }

            // 腿、身體、手臂、頭、耳朵（後面的先畫）
            Blob(48f, 18f, 11f, fur);
            Blob(80f, 18f, 11f, fur);
            Blob(64f, 44f, 29f, fur);
            Blob(32f, armY, 10f, fur);
            Blob(rightArmX, rightArmY, 10f, fur);
            Blob(64f + headTilt * 0.3f, 84f, 31f, fur);
            Blob(40f, 109f, 11f, fur);
            Blob(88f, 109f, 11f, fur);
            cream.Fill((x, y) => Inside.Circle(x, y, 40f, 109f, 5.5f));
            cream.Fill((x, y) => Inside.Circle(x, y, 88f, 109f, 5.5f));

            // 肚子與嘴巴那塊
            cream.Fill((x, y) => Inside.Circle(x, y, 64f, 40f, 17f));
            cream.Fill((x, y) => Inside.Circle(x, y, 64f, 75f, 12f));

            // 蜂蜜罐：抱在身前偏右
            if (!happy)
            {
                outline.Fill((x, y) => Inside.RoundRect(x, y, 70f, 26f, 102f, 54f, 6f + 2.2f));
                pot.Fill((x, y) => Inside.RoundRect(x, y, 72f, 28f, 100f, 52f, 6f));
                outline.Fill((x, y) => Inside.RoundRect(x, y, 68f, 50f, 104f, 58f, 3f + 2.2f));
                pot.Fill((x, y) => Inside.RoundRect(x, y, 70f, 52f, 102f, 56f, 3f));
            }
            else
            {
                outline.Fill((x, y) => Inside.RoundRect(x, y, 96f, 4f, 124f, 28f, 6f + 2.2f));
                pot.Fill((x, y) => Inside.RoundRect(x, y, 98f, 6f, 122f, 26f, 6f));
            }

            // 鼻子
            ink.Fill((x, y) => Inside.Circle(x, y, 64f, 79f, 4.5f));

            // 眼睛
            float eyeY = 91f;
            if (sleep)
            {
                ink.Fill((x, y) => Inside.RoutedLine(x, y, 46f, eyeY, 58f, eyeY, 2.2f));
                ink.Fill((x, y) => Inside.RoutedLine(x, y, 70f, eyeY, 82f, eyeY, 2.2f));
            }
            else if (happy)
            {
                // 瞇瞇眼：上弧
                ink.Fill((x, y) => Inside.Circle(x, y, 52f, eyeY - 2f, 5.5f) && !Inside.Circle(x, y, 52f, eyeY - 4.5f, 5.5f));
                ink.Fill((x, y) => Inside.Circle(x, y, 76f, eyeY - 2f, 5.5f) && !Inside.Circle(x, y, 76f, eyeY - 4.5f, 5.5f));
            }
            else
            {
                float r = surprised ? 6f : 4.2f;
                float dx = think ? 1.5f : 0f, dy = think ? 1.5f : 0f;
                ink.Fill((x, y) => Inside.Circle(x, y, 52f + dx, eyeY + dy, r));
                ink.Fill((x, y) => Inside.Circle(x, y, 76f + dx, eyeY + dy, r));
                white.Fill((x, y) => Inside.Circle(x, y, 53.5f + dx, eyeY + 1.5f + dy, 1.6f));
                white.Fill((x, y) => Inside.Circle(x, y, 77.5f + dx, eyeY + 1.5f + dy, 1.6f));
            }

            // 嘴
            if (surprised) ink.Fill((x, y) => Inside.Circle(x, y, 64f, 70f, 3.5f));
            else if (happy) ink.Fill((x, y) => Inside.Circle(x, y, 64f, 71f, 5f) && y < 71f);
            else if (!sleep) ink.Fill((x, y) => Inside.Circle(x, y, 64f, 72f, 4.5f) && !Inside.Circle(x, y, 64f, 73.8f, 4.5f) && y < 72f);

            // 思考時頭上一個小問號（兩個點湊的）
            if (think)
            {
                ink.Fill((x, y) => Inside.Circle(x, y, 100f, 118f, 4f) && !Inside.Circle(x, y, 100f, 118f, 2f) && !(x < 100f && y < 118f));
                ink.Fill((x, y) => Inside.RoutedLine(x, y, 100f, 114f, 100f, 110f, 1.8f));
                ink.Fill((x, y) => Inside.Circle(x, y, 100f, 105f, 1.8f));
            }

            return Canvas.ComposeMany(
                (outline, new Color32(0x3A, 0x3A, 0x5A, 0xFF)),
                (fur, new Color32(0xFF, 0xC9, 0x4D, 0xFF)),
                (cream, new Color32(0xFF, 0xF0, 0xC8, 0xFF)),
                (pot, new Color32(0x5A, 0xA9, 0xE6, 0xFF)),
                (ink, new Color32(0x3A, 0x3A, 0x5A, 0xFF)),
                (white, new Color32(0xFF, 0xFF, 0xFF, 0xFF)));
        }

        public static Sprite EnsureIcon(string name)
        {
            string path = BabyAppPaths.UiArt + "/icon_" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(BabyAppPaths.UiArt);
            var canvas = new Canvas(128);
            Draw(name, canvas);
            File.WriteAllBytes(path, canvas.ToPng(Color.white));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>顏色卡片組的八顆色球，存到 Art/Cards/Colors，檔名就是卡片代號。</summary>
        public static void EnsureColorCards()
        {
            var colors = new (string id, Color32 color)[]
            {
                ("color_red", new Color32(0xFF, 0x3B, 0x30, 0xFF)),
                ("color_orange", new Color32(0xFF, 0x95, 0x00, 0xFF)),
                ("color_yellow", new Color32(0xFF, 0xD6, 0x0A, 0xFF)),
                ("color_green", new Color32(0x34, 0xC7, 0x59, 0xFF)),
                ("color_blue", new Color32(0x0A, 0x84, 0xFF, 0xFF)),
                ("color_purple", new Color32(0xBF, 0x5A, 0xF2, 0xFF)),
                ("color_pink", new Color32(0xFF, 0x6F, 0xB5, 0xFF)),
                ("color_black", new Color32(0x2B, 0x2B, 0x2B, 0xFF))
            };

            string folder = BabyAppPaths.CardArt + "/Colors";
            Directory.CreateDirectory(folder);
            bool any = false;

            foreach (var (id, color) in colors)
            {
                string path = folder + "/" + id + ".png";
                if (File.Exists(path)) continue;

                // 一顆圓球加左上角一點反光，像玻璃彈珠
                var ball = new Canvas(512);
                ball.Fill((x, y) => Inside.Circle(x, y, 256, 256, 210));
                var highlight = new Canvas(512);
                highlight.Fill((x, y) => Inside.Circle(x, y, 190, 330, 58));

                File.WriteAllBytes(path, Canvas.Compose(ball, color, highlight, new Color(1f, 1f, 1f, 0.45f)));
                any = true;
            }
            if (any) AssetDatabase.Refresh();
        }

        // ---------- 每個圖示的畫法（座標 0～128，y 往上）----------

        private static void Draw(string name, Canvas c)
        {
            switch (name)
            {
                case "arrow":
                    c.Fill((x, y) => Inside.Polygon(x, y, (26, 14), (26, 114), (110, 64)));
                    break;

                case "house":
                    c.Fill((x, y) => Inside.Rect(x, y, 84, 70, 96, 104));              // 煙囪
                    c.Fill((x, y) => Inside.Rect(x, y, 30, 22, 98, 72));               // 房身
                    c.Fill((x, y) => Inside.Polygon(x, y, (16, 68), (64, 114), (112, 68))); // 屋頂
                    c.Cut((x, y) => Inside.Rect(x, y, 54, 22, 74, 54));                // 門
                    break;

                case "mic":
                    c.Fill((x, y) => Inside.Circle(x, y, 64, 56, 28));                 // 外弧
                    c.Cut((x, y) => Inside.Circle(x, y, 64, 56, 20));
                    c.Cut((x, y) => Inside.Rect(x, y, 0, 56, 128, 128));               // 只留下半圈
                    c.Fill((x, y) => Inside.Rect(x, y, 50, 56, 78, 100));              // 麥克風頭
                    c.Fill((x, y) => Inside.Circle(x, y, 64, 100, 14));
                    c.Fill((x, y) => Inside.Circle(x, y, 64, 56, 14));
                    c.Fill((x, y) => Inside.Rect(x, y, 60, 24, 68, 36));               // 桿
                    c.Fill((x, y) => Inside.Rect(x, y, 44, 18, 84, 26));               // 底座
                    break;

                case "eraser":
                    c.Fill((x, y) => Inside.RotatedRect(x, y, 64, 64, 88, 42, 40f));
                    c.Cut((x, y) => Inside.RotatedRect(x, y, 76, 76, 6, 46, 40f));     // 一條縫，看得出是兩截
                    break;

                case "picture":
                    c.Fill((x, y) => Inside.Rect(x, y, 14, 22, 114, 106));             // 框
                    c.Cut((x, y) => Inside.Rect(x, y, 26, 34, 102, 94));
                    c.Fill((x, y) => Inside.Polygon(x, y, (28, 36), (56, 76), (72, 58), (100, 36))); // 山
                    c.Fill((x, y) => Inside.Circle(x, y, 86, 80, 9));                  // 太陽
                    break;

                case "gear":
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 45f;
                        float rad = angle * Mathf.Deg2Rad;
                        float cx = 64 + Mathf.Cos(rad) * 42f;
                        float cy = 64 + Mathf.Sin(rad) * 42f;
                        c.Fill((x, y) => Inside.RotatedRect(x, y, cx, cy, 22, 18, angle));
                    }
                    c.Fill((x, y) => Inside.Circle(x, y, 64, 64, 36));
                    c.Cut((x, y) => Inside.Circle(x, y, 64, 64, 14));
                    break;

                case "magnifier":
                    c.Fill((x, y) => Inside.Circle(x, y, 54, 74, 32));
                    c.Cut((x, y) => Inside.Circle(x, y, 54, 74, 22));
                    c.Fill((x, y) => Inside.RotatedRect(x, y, 92, 36, 44, 16, 45f));  // 把手
                    break;

                case "count":
                    c.Fill((x, y) => Inside.Circle(x, y, 26, 64, 11));
                    c.Fill((x, y) => Inside.Circle(x, y, 62, 64, 15));
                    c.Fill((x, y) => Inside.Circle(x, y, 102, 64, 19));
                    break;

                case "play":
                    c.Fill((x, y) => Inside.Polygon(x, y, (40, 22), (40, 106), (108, 64)));
                    break;

                default:
                    c.Fill((x, y) => Inside.Circle(x, y, 64, 64, 40));
                    break;
            }
        }

        // ---------- 小小的點陣繪圖器 ----------

        /// <summary>一張覆蓋率圖：每個像素 0～1，用 3×3 超取樣讓邊緣不鋸齒。</summary>
        private sealed class Canvas
        {
            private const int Samples = 3;
            private readonly int _size;
            private readonly float[] _coverage;

            public Canvas(int size)
            {
                _size = size;
                _coverage = new float[size * size];
            }

            public void Fill(Func<float, float, bool> inside) => Apply(inside, union: true);
            public void Cut(Func<float, float, bool> inside) => Apply(inside, union: false);

            private void Apply(Func<float, float, bool> inside, bool union)
            {
                float scale = 128f / _size; // 圖示的畫法都用 0～128 座標，大張的畫布照比例縮放
                for (int y = 0; y < _size; y++)
                {
                    for (int x = 0; x < _size; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < Samples; sy++)
                        {
                            for (int sx = 0; sx < Samples; sx++)
                            {
                                float px = (x + (sx + 0.5f) / Samples) * scale;
                                float py = (y + (sy + 0.5f) / Samples) * scale;
                                if (inside(px, py)) hits++;
                            }
                        }
                        float cov = hits / (float)(Samples * Samples);
                        int i = y * _size + x;
                        _coverage[i] = union ? Mathf.Max(_coverage[i], cov) : _coverage[i] * (1f - cov);
                    }
                }
            }

            public byte[] ToPng(Color tint)
            {
                var texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false);
                var pixels = new Color32[_size * _size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32((byte)(tint.r * 255), (byte)(tint.g * 255), (byte)(tint.b * 255),
                        (byte)(_coverage[i] * tint.a * 255));
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                var png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                return png;
            }

            /// <summary>底層一個顏色、上層一個顏色（含透明度）疊起來。</summary>
            public static byte[] Compose(Canvas bottom, Color bottomColor, Canvas top, Color topColor)
            {
                return ComposeMany((bottom, bottomColor), (top, topColor));
            }

            /// <summary>好幾層由下往上疊，每層一個顏色（含透明度），像剪紙。</summary>
            public static byte[] ComposeMany(params (Canvas layer, Color color)[] layers)
            {
                int size = layers[0].layer._size;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    var result = new Color(0f, 0f, 0f, 0f);
                    foreach (var (layer, color) in layers)
                    {
                        float a = layer._coverage[i] * color.a;
                        if (a <= 0f) continue;
                        // 一般的 over 合成
                        float outA = a + result.a * (1f - a);
                        var rgb = (new Color(color.r, color.g, color.b) * a + new Color(result.r, result.g, result.b) * result.a * (1f - a)) / Mathf.Max(outA, 1e-5f);
                        result = new Color(rgb.r, rgb.g, rgb.b, outA);
                    }
                    pixels[i] = result;
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                var png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                return png;
            }
        }

        private static class Inside
        {
            public static bool Circle(float x, float y, float cx, float cy, float r)
            {
                float dx = x - cx, dy = y - cy;
                return dx * dx + dy * dy <= r * r;
            }

            public static bool Rect(float x, float y, float x0, float y0, float x1, float y1)
            {
                return x >= x0 && x <= x1 && y >= y0 && y <= y1;
            }

            public static bool RotatedRect(float x, float y, float cx, float cy, float w, float h, float degrees)
            {
                float rad = -degrees * Mathf.Deg2Rad;
                float dx = x - cx, dy = y - cy;
                float lx = dx * Mathf.Cos(rad) - dy * Mathf.Sin(rad);
                float ly = dx * Mathf.Sin(rad) + dy * Mathf.Cos(rad);
                return Mathf.Abs(lx) <= w * 0.5f && Mathf.Abs(ly) <= h * 0.5f;
            }

            public static bool RoundRect(float x, float y, float x0, float y0, float x1, float y1, float r)
            {
                if (x < x0 || x > x1 || y < y0 || y > y1) return false;
                float cx = Mathf.Clamp(x, x0 + r, x1 - r);
                float cy = Mathf.Clamp(y, y0 + r, y1 - r);
                float dx = x - cx, dy = y - cy;
                return dx * dx + dy * dy <= r * r;
            }

            /// <summary>兩點之間一條有寬度、圓頭的線。</summary>
            public static bool RoutedLine(float x, float y, float ax, float ay, float bx, float by, float halfWidth)
            {
                float vx = bx - ax, vy = by - ay;
                float len2 = vx * vx + vy * vy;
                float t = len2 > 0f ? Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / len2) : 0f;
                float px = ax + vx * t, py = ay + vy * t;
                float dx = x - px, dy = y - py;
                return dx * dx + dy * dy <= halfWidth * halfWidth;
            }

            public static bool Polygon(float x, float y, params (float x, float y)[] points)
            {
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    var a = points[i];
                    var b = points[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                return inside;
            }
        }
    }
}
