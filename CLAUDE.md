# BabyApp — 給 2～3 歲小孩的認知學習 App（Unity）

每次開新對話先讀這份，不用重講架構。細節看 README.md 與 docs/。開發流程與驗收回報格式見 [docs/DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md)。

## 一句話

單場景 Unity 6000.5 / URP / 新版 Input System 的 iPad+Android 橫向 App。首頁七格：卡片（進去再選主題）、摸一摸、找一找、數數、配對、拼圖、畫畫；角落長按「設」進家長設定。風格「軟圓淡色」（淡粉彩背景、白邊圓角方塊、柔和陰影、圓形圖示按鈕、蜂蜜小熊吉祥物）。中英文可切換。

## 鐵律

- **沒有分數、計時、關卡、答錯懲罰**。2～3 歲只要「我按下去，世界有反應」。選錯不是錯：那張淡掉、對的亮起來、小熊說「在這裡！」。
- **按鈕用圖示不用字**（小孩不認字）。卡片／格子上的名稱預設藏著，設定「顯示文字」才出現（`AppSettings.ShowLabels`、`TileLabelToggle`）。
- **場景是程式建的**：`Assets/BabyApp/Scripts/Editor/BabyAppSceneBuilder.cs`，改版面改那支然後跑「BabyApp → 3. 重建場景」。不要手動在 Inspector 接線。新畫面要接 `AppRoot` + `HomeScreen`（或 `DeckPickScreen`）+ Builder 三處。
- **素材靠檔名接線**：`{cardId}.png` 圖、`{cardId}.wav` 中文名、`{cardId}_en.wav` 英文名、`{cardId}_sound.*` 叫聲、`{deckId}_sound.*` 整組共用叫聲、`cover_{deckId}.png` 封面。跑「2. 匯入卡片素材」就接上。
- **卡片內容在 `Assets/BabyApp/CardManifest.csv`**（8 欄：deckId,deckName,deckNameEn,sortOrder,themeColor,cardId,cardName,cardNameEn）。加卡、加主題只改 CSV。
- **版面全部用錨點百分比**，不用固定像素；`EvenStrip`（一條線平均排、不夠就縮）、`ResponsiveGrid`（正方形格子自動排）、`SafeAreaFitter`（瀏海）。
- 介面文字走 `Strings.Get(key)` + `LocalizedText`；小熊台詞走 `PromptLibrary`（`tools/prompts.csv` → `make-prompts.ps1`）；設定走 `AppSettings`（PlayerPrefs）。
- 我（Claude）無法跑 Unity（編輯器開著、batchmode 會搶 lock）；用 scratchpad 的 `typecheck.csproj`（netstandard2.1，參照 `D:\UnityEditor\6000.5.1f1\Editor\Data\Managed\UnityEngine\*Module.dll` + `Library/ScriptAssemblies` 的 UI/InputSystem/URP）做型別檢查，然後請使用者在 Unity 裡跑選單。**每次交付結尾用 DEVELOPMENT_WORKFLOW.md 的驗收回報格式，「編譯過」只能寫 NOT_RUN 不能寫 PASS。**

## 選單（Unity 上方 BabyApp）

1 一鍵初始化 ｜ 2 匯入卡片素材（改 CSV、丟素材後）｜ 3 重建場景（改 Builder 後）｜ 4 套用播放器設定 ｜ 5/6 切平台 Android/iOS ｜ 7 出 APK ｜ 8 匯出 Xcode 專案 ｜ 檢查還缺哪些素材

## 程式地圖（Assets/BabyApp/Scripts）

- Runtime/Data：`CardEntry`（`LocalizedName/LocalizedVoice`、`HasImage`）、`CardDeck`（`PlayableCards()` 有圖優先、整組沒圖退回純文字；`fallbackSound`）、`DeckLibrary`（Resources/Decks）、`ColorPalette`（顏色卡整張填色，Editor 畫色球共用）
- Runtime/Core：`AppRoot`（畫面切換）、`AppSettings`（音效/語音/音樂/背景/語言/顯示文字）、`Strings`、`LocalizedText`、`VoiceService`（語音/音效/音樂三軌，`PlaySequence` 串播、每段最多 2.5 秒；音效：boop/swoosh/tada/sparkle/chew）、`PromptLibrary`（在哪裡／這是誰的聲音／答對了／再試試看／全部數完了／在這裡／好吃／一樣的在哪裡／來玩吧／拼好了／一～五）、`VoiceRecorder`+`WavFile`+`RecordingStore`（錄音存 persistentDataPath/recordings）、`UsageStats`、`ThemedBackground`、`SafeAreaFitter`、`HoldButton`（長按）、`Pulse`、`TapReaction`（四種隨機動畫+星星）
- Runtime/UI：
  - 導覽：`HomeScreen`（七格）、`DeckPickScreen`（選主題，每組一格封面）、`DeckButton`、`TileLabelToggle`、`SettingsScreen`、`ToggleRow`
  - 卡片：`DeckScreen`（新主題前 `coveredCardsPerDeck`=3 張蓋「？」，之後直接看；滑到底自動接下一組 `chainDecks`；拖曳換卡、錄音、自動播放）、`CardView`、`CardDoor`（蓋子，`Dismiss()` 直接不蓋）、`DraggableCard`、`RecordButton`
  - 遊戲：`TouchScreen`（摸一摸：6 格點誰誰叫，點小熊換頁）、`FindScreen`（兩選一，名稱或叫聲出題）、`CountScreen`（點一個念一個數，東西飛進小熊嘴裡咔滋）、`MatchScreen`（左一張樣子、右三張選一樣的）、`PuzzleScreen`+`PuzzlePiece`（2×2 拼圖，前兩回合 2 片，寬鬆吸附，底下淡淡完整圖）、`PaintScreen`+`PaintCanvas`+`PictureCell`（24 色色盤、三種粗細、選底圖）
  - 共用：`Mascot`（表情、呼吸、跳）、`DraggableWidget`（小熊可拖、點一下發 Tapped）、`EvenStrip`、`ResponsiveGrid`
- Editor：`BabyAppSceneBuilder`（建場景）、`BabyAppDeckImporter`（CSV→Deck 資產）、`BabyAppUiSprites`（程式畫圖示/色球/圓角底圖/小熊佔位）、`BabyAppSfxGenerator`（合成介面音效）、`BabyAppFontSetup`（標楷體→發佈前換 Noto Sans TC）、`BabyAppTextureImportRules`（Art 下自動設 Sprite、roundrect 九宮格邊）、`BabyAppPlayerSetup`、`BabyAppBuilder`、`BabyAppMenu`、`BabyAppPaths`

## 素材現況（2026-11-04）

- 主題 7 組：動物 12、水果 12、交通工具 11、工程車 8、顏色 8（程式畫色球）、生活用品 10（Gemini，2026-11-04）、身體 8（Gemini，2026-11-04，小熊指著該部位）。**七組全部有圖**。身體卡的 prompt：「Same style, same honey bear character. Subject: the honey bear cub <動作>, front view, whole body visible.」，第一張要先貼小熊角色設定。
- 有圖的卡片全部 Gemini 生成並去背（2026-10-23）+ 4 張封面。OpenMoji 已不再使用（tools/openmoji-* 只是歷史）。
- 叫聲：25 個來自 Wikimedia Commons（出處在 docs/素材授權.md）。卡車＝柴油引擎、消防車＝消防鐘、工程車組共用柴油、堆高機專屬。兔子沒叫聲。
- 名稱語音：Windows SAPI 合成（Hanhan zh / Zira en），`tools/make-voices.ps1`；小熊台詞 `tools/make-prompts.ps1` + `tools/prompts.csv`（已存在的檔會跳過）。
- 吉祥物：五張蜂蜜小熊 Gemini 生成並去背（Art/Mascot）。`EnsureMascotPlaceholders()` 只在檔案缺時畫程式佔位。
- 去白底：`python tools/knockout-white.py 圖.png ...`（純 Python，從四邊淹白、邊緣半透明）。
- 方塊底圖：`Art/UI/roundrect.png` 程式畫的大圓角（半徑 72，九宮格），所有 `CreateTile` 共用。
- 發佈前要處理：字體授權、CC BY 標註、合成語音換掉。

## 用 Gemini 批次生圖（內建瀏覽器）

gemini.google.com → 「+」→「建立圖像」→ 貼 prompt（風格句見 docs/AI圖片生成指南.md），同一個對話連續生、每張 ~35 秒。圖是 blob：用 javascript_tool 畫到 canvas → `<a download>` 存到 `C:\Users\Bachider\Downloads`，再 cp 進專案、跑去背。**坑**：連續多檔下載常落成 `.tmp` 亂數名，有時幾十秒後改回正確檔名、有時永遠不會。**不要用時間排序對應**（下載是平行啟動的）。正解：在頁面用 JS 把每張圖縮到 8×8 讀出 RGB 當指紋存成 json（**要逐步減半縮到 8×8 並開 imageSmoothingQuality='high'**，一次 drawImage 到 8×8 只是點取樣，距離會從 ~45 暴增到 ~400 而配錯），再跑 `python tools/match-downloads.py 指紋.json`，它對每個 .tmp 算同樣指紋、取最近的、搬進對應資料夾並印出次近距離（差距 < 1.5 倍會標 !!）。一次 batch 最多 25 個動作；視窗變窄時用 `find` 拿 ref 而不是座標。免費額度一天約 50 張，用完要等到隔天同一時間。

## 待辦 / 想法

- 家人相片卡（要接相機/相簿）— 第二階段。
- iPad 安裝路線（有 Mac 用 Xcode／沒 Mac 出 WebGL）使用者還沒定。

## 語言

對使用者一律繁體中文；程式註解繁中；識別字英文。
