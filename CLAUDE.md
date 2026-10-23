# BabyApp — 給 2～3 歲小孩的認知學習 App（Unity）

每次開新對話先讀這份，不用重講架構。細節看 README.md 與 docs/。

## 一句話

單場景 Unity 6000.5 / URP / 新版 Input System 的 iPad+Android 橫向 App：認知卡片（看圖、聽叫聲+名稱、錄自己的聲音）、找一找、數數、塗鴉、家長設定。風格「軟圓淡色」（淡粉彩背景、白邊圓角方塊、柔和陰影、圓形圖示按鈕、蜂蜜小熊吉祥物）。中英文可切換。

## 鐵律

- **沒有分數、計時、關卡、答錯懲罰**。2～3 歲只要「我按下去，世界有反應」。
- **按鈕用圖示不用字**（小孩不認字），文字只給家長看。
- **場景是程式建的**：`Assets/BabyApp/Scripts/Editor/BabyAppSceneBuilder.cs`，改版面改那支然後跑「BabyApp → 3. 重建場景」。不要手動在 Inspector 接線。
- **素材靠檔名接線**：`{cardId}.png` 圖、`{cardId}.wav` 中文名、`{cardId}_en.wav` 英文名、`{cardId}_sound.*` 叫聲、`{deckId}_sound.*` 整組共用叫聲、`cover_{deckId}.png` 封面。跑「2. 匯入卡片素材」就接上。
- **卡片內容在 `Assets/BabyApp/CardManifest.csv`**（8 欄：deckId,deckName,deckNameEn,sortOrder,themeColor,cardId,cardName,cardNameEn）。加卡、加主題只改 CSV。
- **版面全部用錨點百分比**，不用固定像素；`EvenStrip`（一條線平均排、不夠就縮）、`ResponsiveGrid`（正方形格子自動排）、`SafeAreaFitter`（瀏海）。
- 介面文字走 `Strings.Get(key)` + `LocalizedText`；設定走 `AppSettings`（PlayerPrefs）。
- 我（Claude）無法跑 Unity（編輯器開著、batchmode 會搶 lock）；用 scratchpad 的 `typecheck.csproj`（netstandard2.1，參照 `D:\UnityEditor\6000.5.1f1\Editor\Data\Managed\UnityEngine\*Module.dll` + `Library/ScriptAssemblies` 的 UI/InputSystem/URP）做型別檢查，然後請使用者在 Unity 裡跑選單。每次回報都要說清楚「編譯過、沒實際跑過」。

## 選單（Unity 上方 BabyApp）

1 一鍵初始化 ｜ 2 匯入卡片素材（改 CSV、丟素材後）｜ 3 重建場景（改 Builder 後）｜ 4 套用播放器設定 ｜ 5/6 切平台 Android/iOS ｜ 7 出 APK ｜ 8 匯出 Xcode 專案 ｜ 檢查還缺哪些素材

## 程式地圖（Assets/BabyApp/Scripts）

- Runtime/Data：`CardEntry`（含 `LocalizedName/LocalizedVoice`）、`CardDeck`（`PlayableCards()` 有圖優先、整組沒圖退回純文字；`fallbackSound`）、`DeckLibrary`（Resources/Decks）
- Runtime/Core：`AppRoot`（畫面切換）、`AppSettings`、`Strings`、`LocalizedText`、`VoiceService`（語音/音效/音樂三軌，`PlaySequence` 串播、每段最多 2.5 秒）、`PromptLibrary`（小熊的台詞，Audio/Prompts）、`VoiceRecorder`+`WavFile`+`RecordingStore`（錄音存 persistentDataPath/recordings）、`UsageStats`、`ThemedBackground`、`SafeAreaFitter`、`HoldButton`（長按）、`Pulse`、`TapReaction`（四種隨機動畫+星星）
- Runtime/UI：`HomeScreen`、`DeckButton`、`DeckScreen`（蓋子→叫聲→名稱、拖曳換卡、錄音、自動播放）、`CardView`、`CardDoor`（「？」蓋子）、`DraggableCard`、`FindScreen`（兩選一，名稱或叫聲出題）、`CountScreen`（點一個念一個數，1～5）、`PaintScreen`+`PaintCanvas`（24 色色盤、三種粗細、選底圖）、`PictureCell`、`SettingsScreen`、`ToggleRow`、`Mascot`、`EvenStrip`、`ResponsiveGrid`、`RecordButton`
- Editor：`BabyAppSceneBuilder`（建場景）、`BabyAppDeckImporter`（CSV→Deck 資產）、`BabyAppUiSprites`（程式畫圖示/色球）、`BabyAppSfxGenerator`（合成介面音效）、`BabyAppFontSetup`（標楷體→發佈前換 Noto Sans TC）、`BabyAppTextureImportRules`（Art 下自動設 Sprite）、`BabyAppPlayerSetup`、`BabyAppBuilder`、`BabyAppMenu`、`BabyAppPaths`

## 素材現況

- 卡片圖：**全部 Gemini 生成並去背**（2026-10-23）：動物 12、水果 12、交通工具 11、工程車 8（挖土機已搬到工程車組）、4 張封面；顏色組 8 顆色球程式畫。OpenMoji 已不再使用（tools/openmoji-* 只是歷史）。
- 叫聲：25 個來自 Wikimedia Commons（出處在 docs/素材授權.md）。卡車＝柴油引擎、消防車＝消防鐘（跟救護車／警車鳴笛區分）、工程車組共用柴油、堆高機專屬。
- Gemini 批次生圖的坑：同一個對話連續生、每張 ~35 秒；頁面裡的圖是 blob，用 JS 畫到 canvas → `<a download>`；**連續多檔下載會先落成 .tmp 亂數名，幾十秒後才改回正確檔名**，所以搬檔前 sleep 幾秒、按檔名複製，不要用時間排序對應。
- 名稱語音：Windows SAPI 合成（Hanhan zh / Zira en），`tools/make-voices.ps1`；提示語音 `tools/make-prompts.ps1` + `tools/prompts.csv`。
- 吉祥物：五張蜂蜜小熊 **已用 Google Gemini 生好並去背**（Art/Mascot，2026-10-23）。`BabyAppUiSprites.EnsureMascotPlaceholders()` 只在檔案缺時畫程式佔位。小熊可拖動（`DraggableWidget`，位置存 PlayerPrefs，點一下發 Tapped）。
- 去白底：`python tools/knockout-white.py 圖.png ...`（純 Python，從四邊淹白、邊緣半透明），AI 生的卡片圖也用它。
- 用內建瀏覽器開 gemini.google.com 生圖的流程：「+」→「建立圖像」→ 貼 prompt；圖是 blob，用 javascript_tool 畫到 canvas → `<a download>` 存到 C:\Users\Bachider\Downloads，再 cp 進專案。
- 方塊底圖：`Art/UI/roundrect.png` 程式畫的大圓角（半徑 72，九宮格邊由 `BabyAppTextureImportRules` 設），所有 `CreateTile` 共用。首頁沒有標題。
- 發佈前要處理：字體授權、OpenMoji 換掉、CC BY 標註、合成語音換掉。

## 待辦 / 想法

- 配對遊戲、家人相片卡（要接相機/相簿）— 第二階段
- iPad 安裝路線（有 Mac 用 Xcode／沒 Mac 出 WebGL）使用者還沒定
- tools/ 下的腳本都可重跑，已存在的檔會跳過

## 語言

對使用者一律繁體中文；程式註解繁中；識別字英文。
