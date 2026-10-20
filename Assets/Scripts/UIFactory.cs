using UnityEngine;
using UnityEngine.UI;

namespace BabyApp
{
    /// Builds uGUI elements from code so the whole app lives in scripts and no
    /// prefab or scene wiring is needed. Sizes are tuned for toddlers on a
    /// tablet: everything tappable is big.
    public static class UIFactory
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1200f;

        static Font font;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                // Prefer an OS font that has Chinese glyphs; fall back to the built-in Latin font.
                string[] candidates =
                {
                    "PingFang TC", "Heiti TC", "Apple SD Gothic Neo",      // iOS / macOS
                    "Noto Sans CJK TC", "Noto Sans TC", "Droid Sans Fallback", // Android / Linux
                    "Microsoft JhengHei", "Microsoft YaHei",              // Windows
                };
                font = Font.CreateDynamicFontFromOSFont(candidates, 64);
                if (font == null || font.fontNames.Length == 0)
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string value, int size, Color color, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            return button;
        }

        public static void Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
