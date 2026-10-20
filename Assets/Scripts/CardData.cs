using System;

namespace BabyApp
{
    // Mirrors Assets/Resources/Content/cards.json. Field names must match the
    // JSON keys because JsonUtility maps them by name.

    [Serializable]
    public class CardCatalog
    {
        public string stylePrompt;
        public CardCategory[] categories;
    }

    [Serializable]
    public class CardCategory
    {
        public string id;
        public string zh;
        public string en;
        public string color;
        public CardItem[] cards;
    }

    [Serializable]
    public class CardItem
    {
        public string id;
        public string zh;
        public string en;
        public string prompt;
    }
}
