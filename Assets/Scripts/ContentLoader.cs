using System.Collections.Generic;
using UnityEngine;

namespace BabyApp
{
    /// Loads the card catalog and its generated assets from Resources.
    /// Every lookup degrades gracefully so the app runs before any AI content
    /// has been generated: a missing image becomes a coloured placeholder and
    /// a missing clip returns null.
    public static class ContentLoader
    {
        const string CatalogPath = "Content/cards";
        const string CardsFolder = "Cards/";
        const string AudioFolder = "Audio/";

        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static CardCatalog LoadCatalog()
        {
            var json = Resources.Load<TextAsset>(CatalogPath);
            if (json == null)
            {
                Debug.LogError($"Missing Resources/{CatalogPath}.json");
                return new CardCatalog { categories = new CardCategory[0] };
            }
            return JsonUtility.FromJson<CardCatalog>(json.text);
        }

        public static Sprite LoadSprite(CardItem card, Color fallbackColor)
        {
            if (spriteCache.TryGetValue(card.id, out var cached))
                return cached;

            Sprite sprite;
            var texture = Resources.Load<Texture2D>(CardsFolder + card.id);
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                Debug.LogWarning($"No image for card '{card.id}'. Run Tools/generate_images.py.");
                sprite = MakeSolidSprite(fallbackColor);
            }
            spriteCache[card.id] = sprite;
            return sprite;
        }

        public static AudioClip LoadClip(CardItem card, string lang)
        {
            return Resources.Load<AudioClip>($"{AudioFolder}{card.id}_{lang}");
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        static Sprite MakeSolidSprite(Color color)
        {
            var texture = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
