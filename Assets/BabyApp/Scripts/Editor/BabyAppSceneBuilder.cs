using System.Collections.Generic;
using System.IO;
using BabyApp.Core;
using BabyApp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 一鍵把整個 Main 場景與 DeckButton 預置物建出來並接好線。
    /// 手動在 Inspector 拉幾十個欄位很容易漏，交給程式做比較不會錯，
    /// 而且之後想改版面重跑一次就好。
    ///
    /// 視覺風格是「軟圓淡色」的幼兒 App：淡粉彩背景（可在設定換）、白色圓角邊、柔和陰影、
    /// 正方形格子按鈕、圓形圖示按鈕（不用文字，2～3 歲不認字）、每個畫面角落一隻蜂蜜小熊。
    ///
    /// 版面全部用錨點百分比，不用固定像素，iPad 和各種平板都不會有東西跑到畫面外。
    /// </summary>
    public static class BabyAppSceneBuilder
    {
        // 設計基準解析度，橫向平板
        private static readonly Vector2 Reference = new Vector2(1600f, 900f);

        private static readonly Color Ink = new Color32(0x3A, 0x3A, 0x5A, 0xFF);        // 柔和的深藍灰，所有文字與圖示
        private static readonly Color Frame = Color.white;                                // 白色圓角邊
        private static readonly Color Peach = new Color32(0xFF, 0x9F, 0x68, 0xFF);       // 長按進度環、錄音
        private static readonly Color CoverYellow = new Color32(0xFF, 0xE8, 0xA3, 0xFF); // 「？」蓋子
        private static readonly Color Lavender = new Color32(0xA5, 0x8C, 0xFF, 0xFF);    // 畫畫
        private static readonly Color Mint = new Color32(0x7E, 0xD9, 0xA8, 0xFF);        // 一般按鈕的面
        private static readonly Color Sky = new Color32(0x6F, 0xCB, 0xFF, 0xFF);         // 找一找
        private static readonly Color Sun = new Color32(0xFF, 0xD1, 0x66, 0xFF);         // 數數
        private static readonly Color Rose = new Color32(0xFF, 0x9E, 0xC4, 0xFF);        // 摸一摸
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.16f);

        // 白邊寬度，卡片與按鈕共用，整體才像同一套
        private const float Border = 10f;

        private static Font _font;
        private static Sprite _rounded;
        private static Sprite _circle;
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
        private static AudioClip _tapSfx;
        private static AudioClip _pageSfx;
        private static AudioClip _doorSfx;
        private static AudioClip _sparkleSfx;
        private static AudioClip _chewSfx;

        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(BabyAppPaths.MainScene) &&
                !EditorUtility.DisplayDialog(
                    "重建場景",
                    BabyAppPaths.MainScene + " 已經存在，重建會覆蓋掉你在場景裡的手動調整。要繼續嗎？",
                    "重建", "取消"))
            {
                return;
            }

            _font = BabyAppFontSetup.EnsureFont();
            Icons.Clear();
            foreach (var name in new[] { "arrow", "house", "mic", "eraser", "picture", "gear", "magnifier", "count", "play", "cards", "paw", "match", "puzzle" })
            {
                Icons[name] = BabyAppUiSprites.EnsureIcon(name);
            }
            (_tapSfx, _pageSfx, _doorSfx, _sparkleSfx, _chewSfx) = BabyAppSfxGenerator.EnsureAll();
            // 大圓角底圖（程式畫的，九宮格），所有方塊共用；圓形用內建的 Knob
            _rounded = BabyAppUiSprites.EnsureRoundRect();
            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            BabyAppUiSprites.EnsureMascotPlaceholders();

            Directory.CreateDirectory(BabyAppPaths.Scenes);
            Directory.CreateDirectory(BabyAppPaths.Prefabs);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            var canvas = CreateCanvas();
            var safeRoot = CreateSafeRoot(canvas.transform);
            var home = CreateHomeScreen(safeRoot);
            var deckPick = CreateDeckPickScreen(safeRoot);
            var deck = CreateDeckScreen(safeRoot);
            var touch = CreateTouchScreen(safeRoot);
            var find = CreateFindScreen(safeRoot);
            var count = CreateCountScreen(safeRoot);
            var match = CreateMatchScreen(safeRoot);
            var puzzle = CreatePuzzleScreen(safeRoot);
            var paint = CreatePaintScreen(safeRoot);
            var settings = CreateSettingsScreen(safeRoot);

            var deckButton = BuildDeckButtonPrefab();
            Wire(deckPick, ("buttonPrefab", deckButton));

            CreateAppRoot(home, deckPick, deck, touch, find, count, match, puzzle, paint, settings);

            deckPick.gameObject.SetActive(false);
            deck.gameObject.SetActive(false);
            touch.gameObject.SetActive(false);
            find.gameObject.SetActive(false);
            count.gameObject.SetActive(false);
            match.gameObject.SetActive(false);
            puzzle.gameObject.SetActive(false);
            paint.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BabyAppPaths.MainScene);

            RegisterSceneInBuildSettings();

            Debug.Log("[BabyApp] 場景建好了：" + BabyAppPaths.MainScene + "。按 Play 就能試玩。");
        }

        // ---------- 場景骨架 ----------

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AppSettings.BackgroundColor;
            cam.orthographic = true;
            cam.cullingMask = 0; // 畫面全是 UI Overlay，相機不用畫任何東西
            go.AddComponent<UniversalAdditionalCameraData>();
            // 用選單建相機會自動附耳朵，用程式建不會；沒有 AudioListener 整個 App 都是無聲的
            go.AddComponent<AudioListener>();
            go.tag = "MainCamera";
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<InputSystemUIInputModule>();
            // 用 AddComponent 加上去不會自己帶預設操作設定，沒接的話整個畫面都點不動
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // 0.5 讓版面在細長手機與方正平板上都不會被切掉
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateAppRoot(HomeScreen home, DeckPickScreen deckPick, DeckScreen deck, TouchScreen touch,
            FindScreen find, CountScreen count, MatchScreen match, PuzzleScreen puzzle, PaintScreen paint, SettingsScreen settings)
        {
            var go = new GameObject("AppRoot");
            var voiceSource = go.AddComponent<AudioSource>();
            var sfxSource = go.AddComponent<AudioSource>();
            var musicSource = go.AddComponent<AudioSource>();
            voiceSource.playOnAwake = sfxSource.playOnAwake = musicSource.playOnAwake = false;

            var voice = go.AddComponent<VoiceService>();
            Wire(voice,
                ("voiceSource", voiceSource),
                ("sfxSource", sfxSource),
                ("musicSource", musicSource),
                ("tapSfx", _tapSfx),
                ("pageSfx", _pageSfx),
                ("doorSfx", _doorSfx),
                ("sparkleSfx", _sparkleSfx),
                ("chewSfx", _chewSfx));

            go.AddComponent<VoiceRecorder>();

            // 小熊說的話：從 Audio/Prompts 接上，缺的就是 null（那句會被跳過）
            var prompts = go.AddComponent<PromptLibrary>();
            Wire(prompts,
                ("whereZh", Prompt("prompt_where")),
                ("whoseSoundZh", Prompt("prompt_whose_sound")),
                ("correctZh", Prompt("prompt_correct")),
                ("tryAgainZh", Prompt("prompt_try_again")),
                ("allCountedZh", Prompt("prompt_all_counted")),
                ("hereZh", Prompt("prompt_here")),
                ("yummyZh", Prompt("prompt_yummy")),
                ("sameZh", Prompt("prompt_same")),
                ("comeOnZh", Prompt("prompt_come_on")),
                ("doneZh", Prompt("prompt_done")),
                ("whereEn", Prompt("prompt_where_en")),
                ("whoseSoundEn", Prompt("prompt_whose_sound_en")),
                ("correctEn", Prompt("prompt_correct_en")),
                ("tryAgainEn", Prompt("prompt_try_again_en")),
                ("allCountedEn", Prompt("prompt_all_counted_en")),
                ("hereEn", Prompt("prompt_here_en")),
                ("yummyEn", Prompt("prompt_yummy_en")),
                ("sameEn", Prompt("prompt_same_en")),
                ("comeOnEn", Prompt("prompt_come_on_en")),
                ("doneEn", Prompt("prompt_done_en")));
            WireArray(prompts, "numbersZh", Prompt("num_1"), Prompt("num_2"), Prompt("num_3"), Prompt("num_4"), Prompt("num_5"));
            WireArray(prompts, "numbersEn", Prompt("num_1_en"), Prompt("num_2_en"), Prompt("num_3_en"), Prompt("num_4_en"), Prompt("num_5_en"));

            var root = go.AddComponent<AppRoot>();
            Wire(root,
                ("homeScreen", home),
                ("deckPickScreen", deckPick),
                ("deckScreen", deck),
                ("touchScreen", touch),
                ("findScreen", find),
                ("countScreen", count),
                ("matchScreen", match),
                ("puzzleScreen", puzzle),
                ("paintScreen", paint),
                ("settingsScreen", settings));
        }

        private static AudioClip Prompt(string name)
        {
            // edge-tts 出的是 mp3、SAPI 出的是 wav，兩種都接受
            AudioClip clip = null;
            foreach (var ext in new[] { ".wav", ".mp3", ".ogg" })
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BabyAppPaths.PromptAudio + "/" + name + ext);
                if (clip != null) break;
            }
            if (clip == null) Debug.LogWarning("[BabyApp] 找不到提示語音 " + name + "，請跑 python tools/make-voices-edge.py --prompts。");
            return clip;
        }

        // ---------- 首頁 ----------

        private static HomeScreen CreateHomeScreen(Transform parent)
        {
            var screen = CreateScreen("HomeScreen", parent);
            var home = screen.gameObject.AddComponent<HomeScreen>();

            // 首頁不放標題：小孩不認字，空間留給格子
            // 七個大格自動排 4×2：卡片、摸一摸、找一找、數數、配對、拼圖、畫畫。主題選擇在「卡片」裡面
            var grid = CreateChild("TileGrid", screen);
            Anchor(grid, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.9f));
            grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.gameObject.AddComponent<ResponsiveGrid>();

            var cardsButton = CreateFeatureTile("CardsButton", grid, "cards", "cards", Peach);
            var touchButton = CreateFeatureTile("TouchButton", grid, "paw", "touch", Rose);
            var findButton = CreateFeatureTile("FindButton", grid, "magnifier", "find", Sky);
            var countButton = CreateFeatureTile("CountButton", grid, "count", "count", Sun);
            var matchButton = CreateFeatureTile("MatchButton", grid, "match", "match", Mint);
            var puzzleButton = CreateFeatureTile("PuzzleButton", grid, "puzzle", "puzzle", CoverYellow);
            var paintButton = CreatePaintButton(grid);

            var (settingsButton, _) = CreateHoldButton("SettingsButton", screen, "gear", Mint,
                new Vector2(1f, 1f), new Vector2(-90f, -90f), 130f);
            var (mascot, _) = CreateMascot(screen, new Vector2(0f, 1f), new Vector2(130f, -100f), 190f);

            Wire(home,
                ("cardsButton", cardsButton),
                ("touchButton", touchButton),
                ("findButton", findButton),
                ("countButton", countButton),
                ("matchButton", matchButton),
                ("puzzleButton", puzzleButton),
                ("paintButton", paintButton),
                ("settingsButton", settingsButton),
                ("mascot", mascot));
            return home;
        }

        // ---------- 選主題 ----------

        private static DeckPickScreen CreateDeckPickScreen(Transform parent)
        {
            var screen = CreateScreen("DeckPickScreen", parent);
            var pick = screen.gameObject.AddComponent<DeckPickScreen>();

            var grid = CreateChild("DeckGrid", screen);
            Anchor(grid, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.9f));
            grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.gameObject.AddComponent<ResponsiveGrid>();

            var (home, _) = CreateHomeButton(screen);
            var (mascot, _) = CreateMascot(screen, new Vector2(0f, 1f), new Vector2(130f, -100f), 190f);

            Wire(pick,
                ("buttonContainer", grid),
                ("homeButton", home),
                ("mascot", mascot));
            return pick;
        }

        private static DeckButton BuildDeckButtonPrefab()
        {
            var (root, _, fill) = CreateTile("DeckButton", null, new Vector2(400f, 400f), Lavender);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            // 顏色由主題色決定，按下的回饋交給 Pulse 的彈跳，別讓內建變色蓋掉主題色
            button.transition = Selectable.Transition.None;

            var pulse = root.gameObject.AddComponent<Pulse>();
            var deckButton = root.gameObject.AddComponent<DeckButton>();

            var cover = CreateImage("Cover", root, null, Color.white);
            cover.preserveAspect = true;
            Anchor(cover.rectTransform, new Vector2(0.14f, 0.3f), new Vector2(0.86f, 0.92f));

            var (label, labelGroup) = CreateTileLabel(root, "主題");
            AddLabelToggle(root, labelGroup, cover.rectTransform,
                new Vector2(0.14f, 0.3f), new Vector2(0.86f, 0.92f),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));

            Wire(deckButton,
                ("cover", cover),
                ("panel", fill),
                ("label", label),
                ("button", button),
                ("pulse", pulse));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, BabyAppPaths.DeckButtonPrefab);
            Object.DestroyImmediate(root.gameObject);

            return prefab.GetComponent<DeckButton>();
        }

        /// <summary>
        /// 首頁的功能格：一個大圖示加名稱（名稱預設藏起來，設定裡打開才顯示）。
        /// 有 Art/UI/tile_{name}.png（AI 畫的彩色小圖，例如拿放大鏡的小熊）就用它、不染色；沒有就用程式畫的深色剪影。
        /// </summary>
        private static Button CreateFeatureTile(string name, Transform parent, string iconName, string labelKey, Color color)
        {
            var (root, _, fill) = CreateTile(name, parent, new Vector2(400f, 400f), color);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<Pulse>();

            var art = AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.UiArt + "/tile_" + labelKey + ".png");
            var icon = CreateImage("Icon", root, art != null ? art : Icon(iconName), art != null ? Color.white : Ink);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var withMin = new Vector2(0.25f, 0.36f);
            var withMax = new Vector2(0.75f, 0.88f);
            Anchor(icon.rectTransform, withMin, withMax);

            var (label, labelGroup) = CreateTileLabel(root, Strings.Get(labelKey));
            Localize(label, labelKey);
            AddLabelToggle(root, labelGroup, icon.rectTransform, withMin, withMax,
                art != null ? new Vector2(0.1f, 0.1f) : new Vector2(0.2f, 0.2f),
                art != null ? new Vector2(0.9f, 0.9f) : new Vector2(0.8f, 0.8f));
            return button;
        }

        /// <summary>首頁的「畫畫」格：薰衣草紫，上面三顆顏料點。</summary>
        private static Button CreatePaintButton(Transform parent)
        {
            var (root, _, fill) = CreateTile("PaintButton", parent, new Vector2(400f, 400f), Lavender);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<Pulse>();

            // 三顆點放在一個容器裡，文字藏起來時整個容器往中間移
            var dotsRt = CreateChild("Dots", root);
            var withMin = new Vector2(0f, 0.3f);
            var withMax = new Vector2(1f, 0.92f);
            Anchor(dotsRt, withMin, withMax);

            var dots = new[] { PaintScreen.DefaultPalette[0], PaintScreen.DefaultPalette[5], PaintScreen.DefaultPalette[12] };
            for (int i = 0; i < dots.Length; i++)
            {
                float x = 0.22f + 0.28f * i;
                var ring = CreateImage("DotRing" + i, dotsRt, _circle, Frame);
                ring.raycastTarget = false;
                ring.preserveAspect = true;
                Anchor(ring.rectTransform, new Vector2(x - 0.12f, 0.3f), new Vector2(x + 0.12f, 0.7f));

                var dot = CreateImage("Dot" + i, dotsRt, _circle, dots[i]);
                dot.raycastTarget = false;
                dot.preserveAspect = true;
                Anchor(dot.rectTransform, new Vector2(x - 0.1f, 0.33f), new Vector2(x + 0.1f, 0.67f));
            }

            var (label, labelGroup) = CreateTileLabel(root, Strings.Get("paint"));
            Localize(label, "paint");
            AddLabelToggle(root, labelGroup, dotsRt, withMin, withMax, new Vector2(0f, 0.1f), new Vector2(1f, 0.9f));
            return button;
        }

        /// <summary>
        /// 格子下方的名稱：白色小圓角底、深色字，字會依格子大小自動縮放。
        /// 兩個都包在一個 LabelGroup 裡，方便整塊藏起來。回傳 (文字, 整塊)。
        /// </summary>
        private static (Text, GameObject) CreateTileLabel(Transform tile, string text)
        {
            var group = CreateChild("LabelGroup", tile);
            Stretch(group);

            var pill = CreateImage("LabelPill", group, _rounded, new Color(1f, 1f, 1f, 0.85f));
            pill.type = Image.Type.Sliced;
            pill.raycastTarget = false;
            Anchor(pill.rectTransform, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.26f));

            var label = CreateText("Label", group, text, 64, Ink);
            Anchor(label.rectTransform, new Vector2(0.12f, 0.07f), new Vector2(0.88f, 0.25f));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.resizeTextMaxSize = 64;
            return (label, group.gameObject);
        }

        /// <summary>文字只給家長看：接上 TileLabelToggle，設定關著時整塊藏起來、圖長滿格子。</summary>
        private static void AddLabelToggle(RectTransform tile, GameObject labelGroup, RectTransform content,
            Vector2 withMin, Vector2 withMax, Vector2 noMin, Vector2 noMax)
        {
            var toggle = tile.gameObject.AddComponent<TileLabelToggle>();
            toggle.Configure(labelGroup, content, withMin, withMax, noMin, noMax);
            // Configure 只是改記憶體裡的值，要寫回序列化欄位場景才存得住
            EditorUtility.SetDirty(toggle);
        }

        // ---------- 卡片畫面 ----------

        private static DeckScreen CreateDeckScreen(Transform parent)
        {
            var screen = CreateScreen("DeckScreen", parent);
            var deck = screen.gameObject.AddComponent<DeckScreen>();

            var (cardView, dragger) = CreateCardPanel(screen);
            var (previous, previousFace) = CreateArrowButton("PreviousButton", screen, left: true);
            var (next, nextFace) = CreateArrowButton("NextButton", screen, left: false);
            var (homeButton, homeFace) = CreateHomeButton(screen);
            var record = CreateRecordButton(screen);
            var (autoplay, autoplayFace, _) = CreateRoundButtonBase("AutoplayButton", screen, Mint,
                new Vector2(0f, 0f), new Vector2(250f, 100f), 120f);
            AddIcon(autoplay.transform, "play", 0.5f);
            var (mascot, _) = CreateMascot(screen, new Vector2(0f, 1f), new Vector2(130f, -100f), 190f);

            Wire(deck,
                ("cardView", cardView),
                ("dragger", dragger),
                ("previousButton", previous),
                ("nextButton", next),
                ("homeButton", homeButton),
                ("recordButton", record),
                ("autoplayButton", autoplay),
                ("autoplayFace", autoplayFace),
                ("mascot", mascot));
            WireArray(deck, "themedAccents", previousFace, nextFace, homeFace);

            return deck;
        }

        /// <summary>
        /// 外層 CardHolder 負責被拖著走（DraggableCard），裡層 CardPanel 負責彈跳（TapReaction），
        /// 兩個動畫動不同的物件才不會互相蓋掉。
        /// </summary>
        private static (CardView, DraggableCard) CreateCardPanel(Transform parent)
        {
            // 高度跟著螢幕走，寬度固定 900；在最窄的 4:3 平板上兩邊還留得下箭頭
            var holder = CreateChild("CardHolder", parent);
            holder.anchorMin = new Vector2(0.5f, 0.1f);
            holder.anchorMax = new Vector2(0.5f, 0.93f);
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(900f, 0f);
            holder.anchoredPosition = Vector2.zero;
            var dragger = holder.gameObject.AddComponent<DraggableCard>();

            var (root, _, fill) = CreateTile("CardPanel", holder, Vector2.zero, Color.white);
            Inset(root, 0f);

            var tap = root.gameObject.AddComponent<Button>();
            tap.targetGraphic = fill;
            tap.transition = Selectable.Transition.None;

            var reaction = root.gameObject.AddComponent<TapReaction>();
            Wire(reaction, ("sparkSprite", _circle));

            var cardView = root.gameObject.AddComponent<CardView>();

            var image = CreateImage("CardImage", root, null, Color.white);
            image.preserveAspect = true;
            Anchor(image.rectTransform, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.96f));

            var label = CreateText("NameLabel", root, "名稱", 130, Ink);
            Anchor(label.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.28f));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 40;
            label.resizeTextMaxSize = 130;

            // 蓋子最後建，才會畫在圖的上面
            var door = CreateCover(root);

            Wire(cardView,
                ("image", image),
                ("fill", fill),
                ("nameLabel", label),
                ("tapTarget", tap),
                ("door", door),
                ("reaction", reaction));

            return (cardView, dragger);
        }

        /// <summary>蓋在卡片上的淡黃蓋子，中間一個大大的「？」。點了縮小淡出。</summary>
        private static CardDoor CreateCover(Transform parent)
        {
            var coverRt = CreateChild("Cover", parent);
            Inset(coverRt, Border);
            var group = coverRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; // 點擊穿過去給卡片的 Button，由 DeckScreen 決定要開蓋子
            var door = coverRt.gameObject.AddComponent<CardDoor>();

            var face = coverRt.gameObject.AddComponent<Image>();
            face.sprite = _rounded;
            face.type = Image.Type.Sliced;
            face.color = CoverYellow;
            face.raycastTarget = false;

            var mark = CreateText("Mark", coverRt, "？", 260, Ink);
            Anchor(mark.rectTransform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            mark.resizeTextForBestFit = true;
            mark.resizeTextMinSize = 80;
            mark.resizeTextMaxSize = 260;

            Wire(door, ("cover", coverRt), ("group", group));
            return door;
        }

        /// <summary>圓形的換頁按鈕：白邊、主題色的面、深色箭頭。回傳面的 Image 讓主題色能套上。</summary>
        private static (Button, Image) CreateArrowButton(string name, Transform parent, bool left)
        {
            var anchor = left ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            var (button, face, rt) = CreateRoundButtonBase(name, parent, Mint, anchor,
                new Vector2(left ? 110f : -110f, 0f), 170f);

            var arrow = AddIcon(rt, "arrow", 0.45f);
            if (left) arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            return (button, face);
        }

        private static (HoldButton, Image) CreateHomeButton(Transform parent)
        {
            return CreateHoldButton("HomeButton", parent, "house", Mint,
                new Vector2(1f, 1f), new Vector2(-100f, -100f), 150f);
        }

        /// <summary>要按住才觸發的圓形按鈕：外圈桃色進度環、白邊、彩色面、一個圖示。</summary>
        private static (HoldButton, Image) CreateHoldButton(string name, Transform parent, string iconName,
            Color faceColor, Vector2 anchor, Vector2 position, float size)
        {
            var rt = CreateChild(name, parent);
            Place(rt, anchor, anchor, position, new Vector2(size, size));

            // 整塊都要能按到，所以根物件自己也放一張全透明的圖當接收範圍
            var hitArea = rt.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            var hold = rt.gameObject.AddComponent<HoldButton>();

            // 按住的進度是最外圈那一圈，從上方順時針長出來
            var ring = CreateImage("Ring", rt, _circle, Peach);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            ring.raycastTarget = false;
            Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size, size));

            float inner = size - 22f;
            var border = CreateImage("Border", rt, _circle, Frame);
            border.raycastTarget = false;
            AddShadow(border.gameObject);
            Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(inner, inner));

            var face = CreateImage("Face", rt, _circle, faceColor);
            face.raycastTarget = false;
            Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(inner - Border * 2f, inner - Border * 2f));

            AddIcon(rt, iconName, 0.42f);

            Wire(hold, ("progressRing", ring));
            return (hold, face);
        }

        /// <summary>普通一點就觸發的圓形按鈕，中間一個圖示。</summary>
        private static Button CreateRoundButton(string name, Transform parent, string iconName, Color faceColor,
            Vector2 anchor, Vector2 position, float size)
        {
            var (button, _, rt) = CreateRoundButtonBase(name, parent, faceColor, anchor, position, size);
            AddIcon(rt, iconName, 0.46f);
            return button;
        }

        /// <summary>圓形按鈕的底：白邊 + 陰影 + 彩色面。回傳 (按鈕, 面, 根物件)。</summary>
        private static (Button, Image, RectTransform) CreateRoundButtonBase(string name, Transform parent,
            Color faceColor, Vector2 anchor, Vector2 position, float size)
        {
            var rt = CreateChild(name, parent);
            Place(rt, anchor, anchor, position, new Vector2(size, size));

            var border = rt.gameObject.AddComponent<Image>();
            border.sprite = _circle;
            border.color = Frame;
            AddShadow(rt.gameObject);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<Pulse>();

            var face = CreateImage("Face", rt, _circle, faceColor);
            face.raycastTarget = false;
            Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size - Border * 2f, size - Border * 2f));

            return (button, face, rt);
        }

        /// <summary>左下角的錄音鈕：按住錄、放開存。外圈跟著錄音長度長出來，錄的時候面變紅。</summary>
        private static RecordButton CreateRecordButton(Transform parent)
        {
            const float size = 150f;
            var rt = CreateChild("RecordButton", parent);
            Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(100f, 100f), new Vector2(size, size));

            var hitArea = rt.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            var record = rt.gameObject.AddComponent<RecordButton>();

            var ring = CreateImage("Ring", rt, _circle, new Color32(0xFF, 0x3B, 0x30, 0xFF));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            ring.raycastTarget = false;
            Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size, size));

            float inner = size - 22f;
            var border = CreateImage("Border", rt, _circle, Frame);
            border.raycastTarget = false;
            AddShadow(border.gameObject);
            Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(inner, inner));

            var face = CreateImage("Face", rt, _circle, Peach);
            face.raycastTarget = false;
            Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(inner - Border * 2f, inner - Border * 2f));

            AddIcon(rt, "mic", 0.42f);

            Wire(record, ("progressRing", ring), ("face", face));
            return record;
        }

        // ---------- 摸一摸 ----------

        private static TouchScreen CreateTouchScreen(Transform parent)
        {
            var screen = CreateScreen("TouchScreen", parent);
            var touch = screen.gameObject.AddComponent<TouchScreen>();

            // 六格正方形自動排 3×2，底下留一條給小熊
            var field = CreateChild("Field", screen);
            Anchor(field, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.96f));
            field.gameObject.AddComponent<GridLayoutGroup>();
            field.gameObject.AddComponent<ResponsiveGrid>();

            var (cell, _, cellFill) = CreateTile("ItemTemplate", field, new Vector2(300f, 300f), Color.white);
            var cellButton = cell.gameObject.AddComponent<Button>();
            cellButton.targetGraphic = cellFill;
            cellButton.transition = Selectable.Transition.None;
            cell.gameObject.AddComponent<Pulse>();
            var thumb = CreateImage("Thumb", cell, null, Color.white);
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;
            Inset(thumb.rectTransform, 24f);
            cell.gameObject.SetActive(false);

            var (home, _) = CreateHomeButton(screen);
            var (mascot, mascotHandle) = CreateMascot(screen, new Vector2(0.5f, 0f), new Vector2(0f, 100f), 190f);

            Wire(touch,
                ("mascot", mascot),
                ("mascotHandle", mascotHandle),
                ("homeButton", home),
                ("field", field),
                ("itemTemplate", cellButton));
            return touch;
        }

        // ---------- 找一找 ----------

        private static FindScreen CreateFindScreen(Transform parent)
        {
            var screen = CreateScreen("FindScreen", parent);
            var find = screen.gameObject.AddComponent<FindScreen>();

            var tiles = new RectTransform[2];
            var images = new Image[2];
            var fills = new Image[2];
            var buttons = new Button[2];
            var pulses = new Pulse[2];
            for (int i = 0; i < 2; i++)
            {
                var (root, _, fill) = CreateTile("Choice" + i, screen, Vector2.zero, Color.white);
                Anchor(root, new Vector2(i == 0 ? 0.07f : 0.53f, 0.24f), new Vector2(i == 0 ? 0.47f : 0.93f, 0.9f));

                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.transition = Selectable.Transition.None;
                var pulse = root.gameObject.AddComponent<Pulse>();

                var thumb = CreateImage("Thumb", root, null, Color.white);
                thumb.preserveAspect = true;
                thumb.raycastTarget = false;
                Inset(thumb.rectTransform, 34f);

                tiles[i] = root; images[i] = thumb; fills[i] = fill; buttons[i] = button; pulses[i] = pulse;
            }

            var (home, _) = CreateHomeButton(screen);
            var (mascot, mascotHandle) = CreateMascot(screen, new Vector2(0.5f, 0f), new Vector2(0f, 100f), 190f);

            Wire(find, ("mascot", mascot), ("mascotHandle", mascotHandle), ("homeButton", home));
            WireArray(find, "choiceTiles", tiles);
            WireArray(find, "choiceImages", images);
            WireArray(find, "choiceFills", fills);
            WireArray(find, "choiceButtons", buttons);
            WireArray(find, "choicePulses", pulses);
            return find;
        }

        // ---------- 數數 ----------

        private static CountScreen CreateCountScreen(Transform parent)
        {
            var screen = CreateScreen("CountScreen", parent);
            var count = screen.gameObject.AddComponent<CountScreen>();

            var number = CreateText("NumberLabel", screen, "", 170, Ink, shadowed: true);
            Anchor(number.rectTransform, new Vector2(0.3f, 0.8f), new Vector2(0.7f, 0.98f));

            var field = CreateChild("Field", screen);
            Anchor(field, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.8f));

            // 樣板：白色格、一張圖，關著；CountScreen 每一題複製幾個出來
            var (cell, _, cellFill) = CreateTile("ItemTemplate", field, new Vector2(200f, 200f), Color.white);
            var cellButton = cell.gameObject.AddComponent<Button>();
            cellButton.targetGraphic = cellFill;
            cellButton.transition = Selectable.Transition.None;
            cell.gameObject.AddComponent<Pulse>();
            var thumb = CreateImage("Thumb", cell, null, Color.white);
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;
            Inset(thumb.rectTransform, 18f);
            cell.gameObject.SetActive(false);

            var (home, _) = CreateHomeButton(screen);
            var (mascot, mascotHandle) = CreateMascot(screen, new Vector2(0.5f, 0f), new Vector2(0f, 100f), 190f);

            Wire(count,
                ("mascot", mascot),
                ("mascotHandle", mascotHandle),
                ("homeButton", home),
                ("field", field),
                ("itemTemplate", cellButton),
                ("numberLabel", number));
            return count;
        }

        // ---------- 配對 ----------

        private static MatchScreen CreateMatchScreen(Transform parent)
        {
            var screen = CreateScreen("MatchScreen", parent);
            var match = screen.gameObject.AddComponent<MatchScreen>();

            // 左邊一張大的「樣子」，右邊三張一排選一樣的
            var (sample, _, _) = CreateTile("Sample", screen, Vector2.zero, CoverYellow);
            Anchor(sample, new Vector2(0.05f, 0.3f), new Vector2(0.37f, 0.94f));
            var sampleImage = CreateImage("Thumb", sample, null, Color.white);
            sampleImage.preserveAspect = true;
            sampleImage.raycastTarget = false;
            Inset(sampleImage.rectTransform, 34f);

            const int slots = 3;
            var tiles = new RectTransform[slots];
            var images = new Image[slots];
            var fills = new Image[slots];
            var buttons = new Button[slots];
            var pulses = new Pulse[slots];
            for (int i = 0; i < slots; i++)
            {
                float x0 = 0.42f + 0.18f * i;
                var (root, _, fill) = CreateTile("Choice" + i, screen, Vector2.zero, Color.white);
                Anchor(root, new Vector2(x0, 0.42f), new Vector2(x0 + 0.165f, 0.82f));

                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.transition = Selectable.Transition.None;
                var pulse = root.gameObject.AddComponent<Pulse>();
                root.gameObject.AddComponent<CanvasGroup>();

                var thumb = CreateImage("Thumb", root, null, Color.white);
                thumb.preserveAspect = true;
                thumb.raycastTarget = false;
                Inset(thumb.rectTransform, 26f);

                tiles[i] = root; images[i] = thumb; fills[i] = fill; buttons[i] = button; pulses[i] = pulse;
            }

            var (home, _) = CreateHomeButton(screen);
            var (mascot, mascotHandle) = CreateMascot(screen, new Vector2(0.5f, 0f), new Vector2(0f, 100f), 190f);

            Wire(match,
                ("mascot", mascot),
                ("mascotHandle", mascotHandle),
                ("homeButton", home),
                ("sampleImage", sampleImage),
                ("sampleTile", sample));
            WireArray(match, "choiceTiles", tiles);
            WireArray(match, "choiceImages", images);
            WireArray(match, "choiceFills", fills);
            WireArray(match, "choiceButtons", buttons);
            WireArray(match, "choicePulses", pulses);
            return match;
        }

        // ---------- 拼圖 ----------

        private static PuzzleScreen CreatePuzzleScreen(Transform parent)
        {
            var screen = CreateScreen("PuzzleScreen", parent);
            var puzzle = screen.gameObject.AddComponent<PuzzleScreen>();

            // 底盤：正方形、高度跟著螢幕走；裡面淡淡一張完整的圖當提示
            var (board, _, boardFill) = CreateTile("Board", screen, Vector2.zero, Color.white);
            board.anchorMin = new Vector2(0.5f, 0.3f);
            board.anchorMax = new Vector2(0.5f, 0.97f);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.sizeDelta = new Vector2(600f, 0f);
            board.anchoredPosition = Vector2.zero;
            boardFill.color = new Color(1f, 1f, 1f, 1f);

            var inner = CreateChild("Inner", board);
            Inset(inner, Border + 6f);
            var ghost = CreateImage("Ghost", inner, null, new Color(1f, 1f, 1f, 0.28f));
            ghost.preserveAspect = false;
            ghost.raycastTarget = false;
            Stretch(ghost.rectTransform);

            // 托盤：底下一條，散放拼圖片
            var tray = CreateChild("Tray", screen);
            Anchor(tray, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.27f));

            // 拼圖片的父層：鋪滿整個畫面，片才能從托盤拖到底盤
            var pieces = CreateChild("Pieces", screen);
            Stretch(pieces);

            // 樣板：白邊 + 遮罩 + 裡面整張圖
            var pieceRt = CreateChild("PieceTemplate", pieces);
            Size(pieceRt, new Vector2(200f, 200f));
            var frame = pieceRt.gameObject.AddComponent<Image>();
            frame.sprite = _rounded;
            frame.type = Image.Type.Sliced;
            frame.color = Frame;
            AddShadow(pieceRt.gameObject);

            var mask = CreateChild("Mask", pieceRt);
            Inset(mask, Border * 0.6f);
            mask.gameObject.AddComponent<RectMask2D>();
            var maskImage = mask.gameObject.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0f);
            maskImage.raycastTarget = false;

            var pic = CreateImage("Pic", mask, null, Color.white);
            pic.raycastTarget = false;

            var piece = pieceRt.gameObject.AddComponent<PuzzlePiece>();
            Wire(piece, ("frame", pieceRt), ("picture", pic.rectTransform));
            pieceRt.gameObject.SetActive(false);

            var (home, _) = CreateHomeButton(screen);
            var (mascot, mascotHandle) = CreateMascot(screen, new Vector2(0f, 1f), new Vector2(130f, -100f), 190f);

            Wire(puzzle,
                ("mascot", mascot),
                ("mascotHandle", mascotHandle),
                ("homeButton", home),
                ("board", board),
                ("boardInner", inner),
                ("ghost", ghost),
                ("tray", tray),
                ("pieceTemplate", piece));
            return puzzle;
        }

        // ---------- 著色畫面 ----------

        private static PaintScreen CreatePaintScreen(Transform parent)
        {
            var screen = CreateScreen("PaintScreen", parent);
            var paint = screen.gameObject.AddComponent<PaintScreen>();

            // 畫板貼邊留白：右邊留給一欄按鈕
            var (board, _, _) = CreateTile("Board", screen, Vector2.zero, Color.white);
            Anchor(board, Vector2.zero, Vector2.one, new Vector2(40f, 40f), new Vector2(-210f, -40f));

            var backdrop = CreateImage("Backdrop", board, null, new Color(1f, 1f, 1f, 0.22f));
            backdrop.preserveAspect = true;
            backdrop.raycastTarget = false;
            Anchor(backdrop.rectTransform, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.92f));

            var canvasRt = CreateChild("PaintCanvas", board);
            Inset(canvasRt, Border);
            var raw = canvasRt.gameObject.AddComponent<RawImage>();
            raw.color = Color.white;
            var paintCanvas = canvasRt.gameObject.AddComponent<PaintCanvas>();

            var center = new Vector2(0.5f, 0.5f);

            // 右欄由上到下：家、目前顏色、三種粗細、圖、擦。
            // 交給 EvenStrip 平均分配：高螢幕拉開間距，矮螢幕整欄等比縮小，永遠不重疊。
            var column = CreateChild("RightColumn", screen);
            column.anchorMin = new Vector2(1f, 0f);
            column.anchorMax = new Vector2(1f, 1f);
            column.pivot = new Vector2(1f, 0.5f);
            column.sizeDelta = new Vector2(170f, -30f);
            column.anchoredPosition = new Vector2(-15f, 0f);
            column.gameObject.AddComponent<EvenStrip>().Configure(isHorizontal: false, min: 12f, max: 70f);

            var (home, _) = CreateHoldButton("HomeButton", column, "house", Mint, center, Vector2.zero, 150f);

            // 顯示目前顏色的圓鈕，點開色盤
            var palette = PaintScreen.DefaultPalette;
            var (colorButton, colorFace, _) = CreateRoundButtonBase("ColorButton", column, palette[0],
                center, Vector2.zero, 136f);

            var sizes = PaintScreen.BrushSizes;
            var sizeButtons = new Button[sizes.Length];
            var sizeRings = new Image[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                var (button, ring) = CreateSizeButton("Size" + i, column, sizes[i], center, Vector2.zero);
                sizeButtons[i] = button;
                sizeRings[i] = ring;
            }

            var pick = CreateRoundButton("PickButton", column, "picture", Mint, center, Vector2.zero, 120f);
            var (clear, _) = CreateHoldButton("ClearButton", column, "eraser", Peach, center, Vector2.zero, 130f);

            var (colorPicker, colorButtons, colorRings) = CreateColorPicker(screen, palette);
            var (picturePicker, pickerContent, cellTemplate) = CreatePicturePicker(screen);

            Wire(paint,
                ("paintCanvas", paintCanvas),
                ("backdrop", backdrop),
                ("homeButton", home),
                ("clearButton", clear),
                ("pickButton", pick),
                ("colorButton", colorButton),
                ("colorFace", colorFace),
                ("colorPicker", colorPicker),
                ("picturePicker", picturePicker),
                ("pickerContent", pickerContent),
                ("cellTemplate", cellTemplate));
            WireArray(paint, "colorButtons", colorButtons);
            WireArray(paint, "colorRings", colorRings);
            WireArray(paint, "sizeButtons", sizeButtons);
            WireArray(paint, "sizeRings", sizeRings);

            return paint;
        }

        /// <summary>色盤：暗掉的底、白色面板、裡面一格一格的顏色圓鈕，8 顆一排。</summary>
        private static (GameObject, Button[], Image[]) CreateColorPicker(Transform parent, Color[] palette)
        {
            var dim = CreateSolid("ColorPicker", parent, new Color(0.2f, 0.2f, 0.3f, 0.5f));
            Stretch(dim.rectTransform);

            var (panel, _, _) = CreateTile("Panel", dim.transform, Vector2.zero, Color.white);
            Anchor(panel, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));

            var grid = CreateChild("Grid", panel);
            Inset(grid, Border + 12f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(124f, 124f);
            layout.spacing = new Vector2(14f, 14f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 8;

            var buttons = new Button[palette.Length];
            var rings = new Image[palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                // 白色的面用一點點灰，不然在白邊裡看不見
                var face = palette[i] == Color.white ? new Color32(0xF2, 0xF2, 0xF2, 0xFF) : (Color32)palette[i];
                var (button, ring) = CreateColorButton("Color" + i, grid, face, new Vector2(0.5f, 0.5f), Vector2.zero);
                buttons[i] = button;
                rings[i] = ring;
            }

            dim.gameObject.SetActive(false);
            return (dim.gameObject, buttons, rings);
        }

        /// <summary>一顆圓形色塊：白邊、陰影；被選中時外面多一圈深色。</summary>
        private static (Button, Image) CreateColorButton(string name, Transform parent, Color color,
            Vector2 anchor, Vector2 position)
        {
            const float size = 100f;
            var rt = CreateChild(name, parent);
            Place(rt, anchor, anchor, position, new Vector2(size, size));

            var ring = CreateImage("Ring", rt, _circle, Ink);
            ring.raycastTarget = false;
            ring.enabled = false;
            Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size + 14f, size + 14f));

            var border = CreateImage("Border", rt, _circle, Frame);
            AddShadow(border.gameObject);
            Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size, size));

            var face = CreateImage("Face", rt, _circle, color);
            face.raycastTarget = false;
            Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size - Border * 2f, size - Border * 2f));

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<Pulse>();

            return (button, ring);
        }

        /// <summary>粗細按鈕：白色圓面上一顆對應大小的深色點。</summary>
        private static (Button, Image) CreateSizeButton(string name, Transform parent, int radius,
            Vector2 anchor, Vector2 position)
        {
            var (button, ring) = CreateColorButton(name, parent, Color.white, anchor, position);

            var dot = CreateImage("Dot", button.transform, _circle, Ink);
            dot.raycastTarget = false;
            float size = Mathf.Clamp(radius * 1.4f, 14f, 64f);
            Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size, size));

            return (button, ring);
        }

        /// <summary>
        /// 選圖牆：暗掉的底、白色面板、裡面一個直向捲動的格子牆。
        /// 格子由 PaintScreen 執行時從樣板複製出來，這裡只放一個關著的樣板。
        /// </summary>
        private static (GameObject, Transform, PictureCell) CreatePicturePicker(Transform parent)
        {
            var dim = CreateSolid("PicturePicker", parent, new Color(0.2f, 0.2f, 0.3f, 0.5f));
            Stretch(dim.rectTransform);
            var pickerGo = dim.gameObject;

            var (panel, _, _) = CreateTile("Panel", dim.transform, Vector2.zero, Color.white);
            Anchor(panel, Vector2.zero, Vector2.one, new Vector2(90f, 70f), new Vector2(-90f, -70f));

            var viewport = CreateChild("Viewport", panel);
            Inset(viewport, Border);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f); // 透明但要吃到拖曳

            var content = CreateChild("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200f, 200f);
            grid.spacing = new Vector2(24f, 24f);
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            // 樣板：淡色格、縮圖、一個字（給「空白」用），關著
            var (cell, _, cellFill) = CreateTile("CellTemplate", content, new Vector2(200f, 200f),
                AppSettings.Backgrounds[0].Color);
            var cellButton = cell.gameObject.AddComponent<Button>();
            cellButton.targetGraphic = cellFill;
            cellButton.transition = Selectable.Transition.None;
            cell.gameObject.AddComponent<Pulse>();

            var thumb = CreateImage("Thumb", cell, null, Color.white);
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;
            Inset(thumb.rectTransform, 22f);

            var cellLabel = CreateText("Label", cell, "空", 72, Ink);
            Inset(cellLabel.rectTransform, 22f);

            var pictureCell = cell.gameObject.AddComponent<PictureCell>();
            Wire(pictureCell, ("thumb", thumb), ("label", cellLabel), ("button", cellButton));
            cell.gameObject.SetActive(false);

            pickerGo.SetActive(false);
            return (pickerGo, content, pictureCell);
        }

        // ---------- 設定畫面 ----------

        private static SettingsScreen CreateSettingsScreen(Transform parent)
        {
            var screen = CreateScreen("SettingsScreen", parent);
            var settings = screen.gameObject.AddComponent<SettingsScreen>();

            var title = CreateText("Title", screen, Strings.Get("settings"), 76, Ink, shadowed: true);
            Anchor(title.rectTransform, new Vector2(0.2f, 0.86f), new Vector2(0.8f, 0.98f));
            Localize(title, "settings");

            // 面板用百分比，上面留標題、下面留「回首頁」，矮螢幕上三者也不會疊
            var (panel, _, _) = CreateTile("Panel", screen, Vector2.zero, Color.white);
            Anchor(panel, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.84f));

            // 每一列用面板高度的百分比定位，面板矮一點列就靠近一點，不會疊
            var sfx = CreateToggleRow(panel, "sfx", 0.90f);
            var voice = CreateToggleRow(panel, "voice", 0.77f);
            var music = CreateToggleRow(panel, "music", 0.64f);
            var labels = CreateToggleRow(panel, "labels", 0.51f);

            CreateRowLabel(panel, "language", 0.37f);
            var (languageButton, languageName) = CreateTextButton("LanguageButton", panel, "language_name", Mint,
                new Vector2(0.78f, 0.37f), Vector2.zero, new Vector2(240f, 80f));

            CreateRowLabel(panel, "background", 0.21f);
            var options = AppSettings.Backgrounds;
            var swatchButtons = new Button[options.Length];
            var swatchRings = new Image[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                var (button, ring) = CreateColorButton("Background" + i, panel, options[i].Color,
                    new Vector2(0.42f + 0.085f * i, 0.21f), Vector2.zero);
                swatchButtons[i] = button;
                swatchRings[i] = ring;
            }

            // 給家長看的一行小統計
            var stats = CreateText("Stats", panel, "", 32, Ink);
            Anchor(stats.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.09f));
            FitText(stats, 18);

            var (close, _) = CreateTextButton("CloseButton", screen, "back_home", Mint,
                new Vector2(0.5f, 0.085f), Vector2.zero, new Vector2(300f, 96f));
            var (clearRecordings, _) = CreateTextButton("ClearRecordingsButton", screen, "clear_recordings", Peach,
                new Vector2(0.2f, 0.085f), Vector2.zero, new Vector2(340f, 96f));

            Wire(settings,
                ("closeButton", close),
                ("sfxToggle", sfx),
                ("voiceToggle", voice),
                ("musicToggle", music),
                ("labelsToggle", labels),
                ("languageButton", languageButton),
                ("languageLabel", languageName),
                ("clearRecordingsButton", clearRecordings),
                ("statsLabel", stats));
            WireArray(settings, "backgroundButtons", swatchButtons);
            WireArray(settings, "backgroundRings", swatchRings);

            return settings;
        }

        /// <summary>設定頁一列左邊的名稱，y 是面板高度的百分比。</summary>
        private static Text CreateRowLabel(Transform parent, string key, float y)
        {
            var label = CreateText(key + "Label", parent, Strings.Get(key), 56, Ink);
            Place(label.rectTransform, new Vector2(0.22f, y), new Vector2(0.22f, y),
                Vector2.zero, new Vector2(320f, 76f));
            FitText(label, 24);
            Localize(label, key);
            return label;
        }

        /// <summary>設定頁一列：左邊一個名稱，右邊一顆開／關。</summary>
        private static ToggleRow CreateToggleRow(Transform parent, string key, float y)
        {
            CreateRowLabel(parent, key, y);

            var (root, _, fill) = CreateTile(key + "Toggle", parent, new Vector2(200f, 80f), Color.gray);
            Place(root, new Vector2(0.78f, y), new Vector2(0.78f, y), Vector2.zero, new Vector2(200f, 80f));

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<Pulse>();

            var state = CreateText("State", root, Strings.Get("on"), 52, Color.white);
            Inset(state.rectTransform, Border);
            FitText(state, 20);

            var toggle = root.gameObject.AddComponent<ToggleRow>();
            Wire(toggle, ("button", button), ("face", fill), ("stateLabel", state));
            return toggle;
        }

        /// <summary>長方形的文字按鈕。回傳 (按鈕, 文字) 讓需要的人改字。</summary>
        private static (Button, Text) CreateTextButton(string name, Transform parent, string key, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var (root, _, fill) = CreateTile(name, parent, size, color);
            Place(root, anchor, anchor, position, size);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<Pulse>();

            var label = CreateText("Label", root, Strings.Get(key), 48, Ink);
            Inset(label.rectTransform, Border);
            FitText(label, 20);
            Localize(label, key);

            return (button, label);
        }

        // ---------- 吉祥物 ----------

        /// <summary>
        /// 蜂蜜小熊。五張表情從 Art/Mascot 接（程式畫的佔位或 AI 圖），缺的用待機圖。
        /// 整隻可以拖到別處（擋到畫面就拖走，位置記住），點一下不算拖，發 Tapped（找一找、數數用來再聽一次題目）。
        /// </summary>
        private static (Mascot, DraggableWidget) CreateMascot(Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            var rt = CreateChild("Mascot", parent);
            Place(rt, anchor, anchor, position, new Vector2(size, size));

            var image = rt.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;

            var sprites = new Sprite[BabyAppPaths.MascotPoses.Length];
            Sprite fallback = AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.CardArt + "/Animals/animal_bear.png");
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(BabyAppPaths.MascotArt + "/" + BabyAppPaths.MascotPoses[i] + ".png");
            }
            if (sprites[0] == null)
            {
                sprites[0] = fallback;
                Debug.LogWarning("[BabyApp] 還沒有吉祥物的圖（Art/Mascot/mascot_idle.png），先用動物卡的熊頂著。生圖方式見 docs/AI圖片生成指南.md。");
            }
            image.sprite = sprites[0];
            image.enabled = sprites[0] != null;

            var mascot = rt.gameObject.AddComponent<Mascot>();
            Wire(mascot,
                ("image", image),
                ("idle", sprites[0]),
                ("happy", sprites[1]),
                ("surprised", sprites[2]),
                ("think", sprites[3]),
                ("sleep", sprites[4]));

            var handle = rt.gameObject.AddComponent<DraggableWidget>();
            return (mascot, handle);
        }

        // ---------- 軟圓風格的積木 ----------

        /// <summary>
        /// 一張鋪滿整個螢幕、跟著設定變色的背景，再加一個縮到安全區裡的容器。
        /// 所有畫面都放在容器裡，所以瀏海跟圓角切不到任何按鈕；背景則照樣鋪到螢幕邊。
        /// </summary>
        private static Transform CreateSafeRoot(Transform canvas)
        {
            var background = CreateSolid("Background", canvas, AppSettings.BackgroundColor);
            Stretch(background.rectTransform);
            background.raycastTarget = false;
            background.gameObject.AddComponent<ThemedBackground>();

            var safe = CreateChild("SafeArea", canvas);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        /// <summary>一個畫面的根：貼滿安全區。背景不在這裡，在外面那張共用的。</summary>
        private static RectTransform CreateScreen(string name, Transform parent)
        {
            var screen = CreateChild(name, parent);
            Stretch(screen);
            return screen;
        }

        /// <summary>
        /// 白邊圓角方塊：外層一張白色圓角圖當邊框（帶柔和陰影），裡面縮進 Border 再鋪一層顏色。
        /// 卡片、格子按鈕、面板都用這個，看起來才像同一套。
        /// 回傳 (根物件, 邊框 Image, 內層 Image)。
        /// </summary>
        private static (RectTransform, Image, Image) CreateTile(
            string name, Transform parent, Vector2 size, Color fillColor)
        {
            var root = CreateChild(name, parent);
            Size(root, size);

            var frame = root.gameObject.AddComponent<Image>();
            frame.sprite = _rounded;
            frame.type = Image.Type.Sliced;
            frame.color = Frame;
            AddShadow(root.gameObject);

            var fill = CreateImage("Fill", root, _rounded, fillColor);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            Inset(fill.rectTransform, Border);

            return (root, frame, fill);
        }

        /// <summary>柔和的下方陰影，讓白邊方塊從淡色背景浮出來。</summary>
        private static void AddShadow(GameObject go)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;
        }

        private static Sprite Icon(string name)
        {
            return Icons.TryGetValue(name, out var sprite) ? sprite : BabyAppUiSprites.EnsureIcon(name);
        }

        /// <summary>在圓形按鈕中間放一個深色圖示，大小是按鈕的 fraction。</summary>
        private static Image AddIcon(Transform button, string iconName, float fraction)
        {
            var icon = CreateImage("Icon", button, Icon(iconName), Ink);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            float half = 0.5f - fraction * 0.5f;
            Anchor(icon.rectTransform, new Vector2(half, half), new Vector2(1f - half, 1f - half));
            return icon;
        }

        // ---------- 小工具 ----------

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var rt = CreateChild(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite == null) image.enabled = false; // 素材還沒接上時不要顯示白方塊
            return image;
        }

        /// <summary>
        /// 單純的色塊。背景不能用內建的圓角圖去拉滿全螢幕，
        /// 那個圓角會被放大成誇張的大弧形；不給 sprite 的 Image 畫出來就是乾淨的純色。
        /// </summary>
        private static Image CreateSolid(string name, Transform parent, Color color)
        {
            var rt = CreateChild(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color,
            bool shadowed = false)
        {
            var rt = CreateChild(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            if (shadowed)
            {
                // 標題在淡色底上加一道白色的柔邊，像貼紙；在夜間深色底上也還看得清楚
                var outline = rt.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
            return text;
        }

        /// <summary>文字跟著語言設定走。</summary>
        private static void Localize(Text text, string key)
        {
            text.gameObject.AddComponent<LocalizedText>().SetKey(key);
        }

        /// <summary>英文比中文長，字放不下就自動縮小。</summary>
        private static void FitText(Text text, int minSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        /// <summary>貼滿父物件但四邊往內縮 inset。</summary>
        private static void Inset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>用父物件的百分比定位，可再加左下／右上的像素留白。這是整個版面自適應的關鍵。</summary>
        private static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void Size(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 用 SerializedObject 把 [SerializeField] 的私有欄位接起來。
        /// 這樣程式裡的欄位可以保持私有，不必為了建場景把它們全開成 public。
        /// </summary>
        private static void Wire(Object target, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var pair in values)
            {
                var property = so.FindProperty(pair.field);
                if (property == null)
                {
                    Debug.LogError("[BabyApp] " + target.GetType().Name +
                                   " 上找不到欄位「" + pair.field + "」，接線失敗。");
                    continue;
                }
                property.objectReferenceValue = pair.value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[BabyApp] " + target.GetType().Name +
                               " 上找不到陣列欄位「" + field + "」，接線失敗。");
                return;
            }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (scenes.Exists(s => s.path == BabyAppPaths.MainScene))
            {
                foreach (var s in scenes) s.enabled = s.path == BabyAppPaths.MainScene;
            }
            else
            {
                // Main 要排第一個，打包出來才會從首頁開始
                foreach (var s in scenes) s.enabled = false;
                scenes.Insert(0, new EditorBuildSettingsScene(BabyAppPaths.MainScene, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
