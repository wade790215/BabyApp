namespace BabyApp.EditorTools
{
    /// <summary>專案裡所有固定路徑集中在這裡，之後要搬資料夾只改這一支。</summary>
    public static class BabyAppPaths
    {
        public const string Root = "Assets/BabyApp";

        public const string CardArt = Root + "/Art/Cards";
        public const string CoverArt = Root + "/Art/Covers";
        public const string MascotArt = Root + "/Art/Mascot";
        public const string UiArt = Root + "/Art/UI";
        public const string CardAudio = Root + "/Audio/Cards";
        public const string PromptAudio = Root + "/Audio/Prompts";
        public const string Fonts = Root + "/Fonts";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";

        public const string Resources = Root + "/Resources";
        public const string DeckAssets = Resources + "/Decks";

        public const string Manifest = Root + "/CardManifest.csv";
        public const string DevFont = Fonts + "/BabyApp_Chinese.ttf";
        public const string MainScene = Scenes + "/Main.unity";
        public const string DeckButtonPrefab = Prefabs + "/DeckButton.prefab";

        public const string SfxFolder = Root + "/Audio/SFX";
        public const string SfxTap = SfxFolder + "/ui_boop.wav";
        public const string SfxPage = SfxFolder + "/ui_swoosh.wav";
        public const string SfxDoor = SfxFolder + "/ui_tada.wav";
        public const string SfxSparkle = SfxFolder + "/ui_sparkle.wav";

        /// <summary>吉祥物五張表情圖的檔名（放在 Art/Mascot），缺的會退回第一張卡的熊。</summary>
        public static readonly string[] MascotPoses = { "mascot_idle", "mascot_happy", "mascot_surprised", "mascot_think", "mascot_sleep" };

        public const string BuildsFolder = "Builds";
    }
}
