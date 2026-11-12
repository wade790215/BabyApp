# BabyApp — 給 2～3 歲小孩的認知學習 App（Unity）

每次開新對話先讀這份，不用重講架構。細節看 README.md 與 docs/。開發流程與驗收回報格式見 [docs/DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md)。

## 一句話

單場景 Unity 6000.5 / URP / 新版 Input System 的 iPad+Android 橫向 App。首頁七格：卡片（進去再選主題）、摸一摸、找一找、數數、配對、拼圖、畫畫；角落長按「設」進家長設定。風格「軟圓淡色」（淡粉彩背景、白邊圓角方塊、柔和陰影、圓形圖示按鈕、蜂蜜小熊吉祥物）。中英文可切換。

## 鐵律

- **沒有分數、計時、關卡、答錯懲罰**。2～3 歲只要「我按下去，世界有反應」。選錯不是錯：那張淡掉、對的亮起來、小熊說「在這裡！」。
- **按鈕用圖示不用字**（小孩不認字）。卡片／格子上的名稱預設藏著，設定「顯示文字」才出現（`AppSettings.ShowLabels`、`TileLabelToggle`）。
- **場景是程式建的**：`Assets/BabyApp/Scripts/Editor/BabyAppSceneBuilder.cs`，改版面改那支然後跑「BabyApp → 3. 重建場景」。不要手動在 Inspector 接線。新畫面要接 `AppRoot` + `HomeScreen`（或 `DeckPickScreen`）+ Builder 三處。
- **改了 Editor 下的腳本就要跑對應選單**，程式改了不等於專案吃到：`BabyAppSceneBuilder.cs` → 跑「3. 重建場景」；`CardManifest.csv` 或素材 → 跑「2. 匯入卡片素材」；`BabyAppPlayerSetup.cs` → 跑「4. 套用播放器設定」（它寫的是 `ProjectSettings/ProjectSettings.asset`）。漏跑選單 4 的代價是真的：`microphoneUsageDescription` 曾經一直空著，iOS 上空白會被 App Store 退件，裝置上 `VoiceRecorder` 要麥克風權限時會當場 crash。
- **素材靠檔名接線**：`{cardId}.png` 圖、`{cardId}.wav` 中文名、`{cardId}_en.wav` 英文名、`{cardId}_sound.*` 叫聲、`{deckId}_sound.*` 整組共用叫聲、`cover_{deckId}.png` 封面。跑「2. 匯入卡片素材」就接上。
- **卡片內容在 `Assets/BabyApp/CardManifest.csv`**（8 欄：deckId,deckName,deckNameEn,sortOrder,themeColor,cardId,cardName,cardNameEn）。加卡、加主題只改 CSV。
- **版面全部用錨點百分比**，不用固定像素；`EvenStrip`（一條線平均排、太寬或太高都整條縮、子物件 SetActive 會自動重排）、`ResponsiveGrid`（正方形格子自動排）、`SafeAreaFitter`（瀏海）。**UGUI 的 Best Fit 只為寬度縮字、不管高度**：文字框的高一定要 ≥ 最大字級，不然字會撐出框疊到別的東西上（家人卡頁踩過）。
- 介面文字走 `Strings.Get(key)` + `LocalizedText`；小熊台詞走 `PromptLibrary`（`tools/prompts.csv` → `python tools/make-voices-azure.py --voice zh-TW-HsiaoYuNeural --prompts`）；設定走 `AppSettings`（PlayerPrefs）。
- **我（Claude）可以直接操作 Unity**：`UnityMCP` MCP server（CoplayDev MCP for Unity，套件釘在 `Packages/manifest.json` 的 `com.coplaydev.unity-mcp`）打進你開著的編輯器，不是 batchmode、不搶 lock。常用：`read_console` 看編譯錯誤、`execute_menu_item` 跑「BabyApp → 2」、重建場景用 `execute_code` 呼叫 `BabyApp.EditorTools.BabyAppSceneBuilder.BuildScene(false)`（選單 3 會跳確認框卡住 MCP；同一次 domain reload 內第二次重建曾炸在 Input System 的 `AssignDefaultActions`，`CreateEventSystem()` 已繞掉，炸了先看那裡；Play 模式中兩種都會被擋，先 `manage_editor stop`）、`manage_scene`／`manage_gameobject` 檢查 Builder 建出來的階層、`unity_reflect`／`unity_docs` 查證 Unity API（別憑記憶猜，容易幻想出不存在的 API）。
- **Unity 內建的 `com.unity.ai.assistant` 已經移除**（2026-11-05）。它和 CoplayDev 的套件各起一支 relay、搶同一條路，當初 MCP 一直接不上就是這樣來的。不要「順手裝回去」——要裝回去是一個要先想清楚的決定，不是補遺漏。
- **MCP 的前提**：Unity 要開著，且 MCP 視窗（`Window → MCP for Unity → Toggle MCP Window`，`Ctrl+Shift+M`）的本機 HTTP server 在 8080 跑著；`MCPForUnity.AutoStartOnLoad` 打開就會隨編輯器自動起。接不上時依序查：8080 有沒有人聽 → `Library/MCPForUnity/Logs/server-launch-8080.log` 有沒有 `Plugin registered: BabyApp` → `~/.claude.json` 的專案層有沒有 `"UnityMCP": {"type": "http", "url": "http://127.0.0.1:8080/mcp"}`。注意 MCP 視窗裡按 Configure 只會寫給當下選到的那個 client，選錯會把 Claude Code 的註冊清掉。
- Unity 或 MCP 連不上時的退路：scratchpad 的 `typecheck.csproj`（netstandard2.1，參照 `D:\UnityEditor\6000.5.1f1\Editor\Data\Managed\UnityEngine\*Module.dll` + `Library/ScriptAssemblies` 的 UI/InputSystem/URP）做型別檢查，然後請使用者在 Unity 裡跑選單。
- 角色分工（`.claude/agents/`）：**新功能動手前先叫 `researcher`（sonnet，唯讀、可上網）**查熱門幼兒 App 怎麼做這類功能，報告存 `docs/research/<日期>-<主題>.md`，拿去跟使用者討論、由使用者決定做不做、怎麼做；定案後叫 `planner`（opus，唯讀）出方案、實作完叫 `evaluator`（sonnet，唯讀）獨立驗收，**Generator 一律留在主對話自己做**——改程式最需要完整上下文，交接成本高，而且它要獨佔 Unity。小任務（改一兩行、修錯字）不要叫，直接做完比較快。同一時間只能有一個寫入者碰 Unity。
- **同一組角色放了兩份定義，因為兩個環境吃的不一樣**：`.claude/skills/{researcher,planner,evaluator}/SKILL.md`（`context: fork` 的 skill，桌面 App Code tab 用這個，文件明寫專案 skills 會進技能清單）與 `.claude/agents/{researcher,planner,evaluator}.md`（`claude` CLI 終端機用這個）。桌面 App 對 `.claude/agents/` 的支援不穩定：早先 `subagent_type: "evaluator"` 一直回 `Agent type 'evaluator' not found`，但 2026-11-05 新增 `researcher.md` 後，同一個 session 裡剛建檔時找不到、幾分鐘後就自動註冊成 Agent type 了。所以兩邊都放、兩份內容要一起改；叫不到 agent 就改用 skill（`/researcher`）。researcher 第一次試跑：41 次工具呼叫、7 分鐘、約 13 萬 token，產出 `docs/research/2026-11-05-家人相片卡.md`。
- **「唯讀」只擋得住一半**：兩種定義都保留 `Bash`（要跑 git／grep／`check-meta.py`），而 Bash 本身能寫檔，所以唯讀是靠角色指引約束，不是工具層強制。`Edit`／`Write` 是真的擋掉了。
- **每次交付結尾用 DEVELOPMENT_WORKFLOW.md 的驗收回報格式。**「編譯過」要拿 `read_console` 的實際輸出當證據才能寫 PASS，拿不到證據就寫 NOT_RUN；「畫面長這樣」「聲音聽起來對」永遠是 NOT_RUN。

## 選單（Unity 上方 BabyApp）

1 一鍵初始化 ｜ 2 匯入卡片素材（改 CSV、丟素材後）｜ 3 重建場景（改 Builder 後）｜ 4 套用播放器設定（改 BabyAppPlayerSetup 後）｜ 5/6 切平台 Android/iOS ｜ 7 出 APK ｜ 8 匯出 Xcode 專案 ｜ 檢查還缺哪些素材

## 程式地圖（Assets/BabyApp/Scripts）

- Runtime/Data：`FamilyLibrary`（家人相片卡：8 個槽位、persistentDataPath/family 的 jpg+wav、組出執行時的 `CardDeck`，**不進 `DeckLibrary`**，只有 `DeckPickScreen` 會多加它）、`CardEntry`（`LocalizedName/LocalizedVoice`、`HasImage`）、`CardDeck`（`PlayableCards()` 有圖優先、整組沒圖退回純文字；`fallbackSound`）、`DeckLibrary`（Resources/Decks）、`ColorPalette`（顏色卡整張填色，Editor 畫色球共用）
- Runtime/Core：`PhotoPicker`（包 NativeGallery 從相簿挑圖＋GPU 逐半縮圖到長邊 1024；Android 設免權限模式）、`AppRoot`（畫面切換）、`AppSettings`（音效/語音/背景/語言/顯示文字；**沒有音樂**，2026-11-13 整個拿掉，Pok Pok 也不放配樂）、`Strings`、`LocalizedText`、`VoiceService`（語音/音效兩軌，`PlaySequence` 串播：語音一定念完、叫聲用 `VoiceService.Sound(clip)` 包才砍 2.5 秒；畫面換題前 `yield return VoiceService.WaitForSpeech(0.5f)` 等小熊講完，不猜秒數；音效五個 `Audio/SFX/sfx_{tap,page,door,correct,chew}`，程式合成的木頭聲（低短圓），`PlayCorrect` 一回合只響一次；要換實物錄音就丟同基底名的 .wav/.ogg/.mp3 並刪掉合成檔；想重新合成就刪 `sfx_*.wav` 再跑選單 3，檔案在就不會重做）、`PromptLibrary`（在哪裡／這是誰的聲音／答對了／再試試看／全部數完了／在這裡／好吃／一樣的在哪裡／來玩吧／拼好了／一～五）、`VoiceRecorder`+`WavFile`+`RecordingStore`（錄音存 persistentDataPath/recordings）、`UsageStats`、`ThemedBackground`、`SafeAreaFitter`、`HoldButton`（長按）、`Pulse`、`TapReaction`（三種輕微動畫隨機挑、無星星、無旋轉）
- Runtime/UI：
  - 家長：`FamilyEditScreen`（家人卡 8 格 4×2，從設定頁「家人卡」進、關閉回設定頁）、`FamilySlotView`（一格：選照片／按住錄稱呼／聽／按住刪）
  - 導覽：`HomeScreen`（七格）、`DeckPickScreen`（選主題，每組一格封面）、`DeckButton`、`TileLabelToggle`、`SettingsScreen`、`ToggleRow`
  - 卡片：`DeckScreen`（新主題前 `coveredCardsPerDeck`=3 張蓋「？」，之後直接看；滑到底自動接下一組 `chainDecks`；拖曳換卡、錄音、自動播放）、`CardView`、`CardDoor`（蓋子，`Dismiss()` 直接不蓋）、`DraggableCard`、`RecordButton`
  - 遊戲：`TouchScreen`（摸一摸：6 格點誰誰叫，點小熊換頁）、`FindScreen`（兩選一，名稱或叫聲出題）、`CountScreen`（點一個念一個數，東西飛進小熊嘴裡咔滋）、`MatchScreen`（左一張樣子、右三張選一樣的）、`PuzzleScreen`+`PuzzlePiece`（2×2 拼圖，前兩回合 2 片，寬鬆吸附，底下淡淡完整圖）、`PaintScreen`+`PaintCanvas`+`PictureCell`（24 色色盤、三種粗細、選底圖）
  - 共用：`Mascot`（表情、呼吸、跳）、`DraggableWidget`（小熊可拖、點一下發 Tapped）、`EvenStrip`、`ResponsiveGrid`
- Editor：`BabyAppSceneBuilder`（建場景）、`BabyAppDeckImporter`（CSV→Deck 資產）、`BabyAppUiSprites`（程式畫圖示/色球/圓角底圖/小熊佔位）、`BabyAppSfxGenerator`（合成介面音效）、`BabyAppFontSetup`（標楷體→發佈前換 Noto Sans TC）、`BabyAppTextureImportRules`（Art 下自動設 Sprite、roundrect 九宮格邊）、`BabyAppPlayerSetup`、`BabyAppBuilder`、`BabyAppMenu`、`BabyAppPaths`

## 素材現況（2026-11-04）

- 主題 7 組：動物 12、水果 12、交通工具 11、工程車 8、顏色 8（程式畫色球）、生活用品 10（Gemini，2026-11-04）、身體 8（Gemini，2026-11-04，小熊指著該部位）。**七組全部有圖，69 張卡的圖與語音都已接進 Deck 資產**（2026-11-05 跑完選單 2 確認 69/69）。注意「有 PNG」不等於「Deck 接上了」：丟完素材沒跑選單 2 的話，Deck 裡的 `image` 會留在 `{fileID: 0}`，整組就退回純文字卡——身體與生活用品兩組就這樣空轉過一段時間。要查就 grep `Assets/BabyApp/Resources/Decks/Deck_*.asset` 還有沒有 `image: {fileID: 0}`。身體卡的 prompt：「Same style, same honey bear character. Subject: the honey bear cub <動作>, front view, whole body visible.」，第一張要先貼小熊角色設定。
- **2026-11-12 起改成 Pok Pok 風（平塗色塊、無框線、無陰影，見 docs/research/2026-11-12-PokPok風格.md 第 9 節）**，動物 12 張與小熊 5 張已換成新風格並去背；水果換了 3 張（蘋果／香蕉／橘子，還在 `tools/gemini-incoming/`，整組換齊才搬進 Assets，不要一組裡兩種風格）；交通工具、工程車、生活用品、身體與 4 張封面還是舊的粗黑邊風格，待換。舊圖全部 Gemini 生成並去背（2026-10-23）。OpenMoji 已不再使用（tools/openmoji-* 只是歷史）。
- 叫聲：25 個來自 Wikimedia Commons（出處在 docs/素材授權.md）。卡車＝柴油引擎、消防車＝消防鐘、工程車組共用柴油、堆高機專屬。兔子沒叫聲。
- 名稱語音與小熊台詞（2026-11-05 換成 **Azure AI Speech**）：中文 `zh-TW-HsiaoYuNeural`（曉雨，使用者試聽三個聲音後選的）、英文 `en-US-AvaMultilingualNeural`，語速 -10%，全部是剪掉頭尾靜音的 24kHz `.wav`（卡名 0.4～0.8 秒，以前 edge-tts 每個都墊到 1.87 秒）。工具 `python tools/make-voices-azure.py --voice zh-TW-HsiaoYuNeural --cards --prompts`（`--trial` 試聽到 `tools/azure-trial/`、`--list-voices`），**會蓋掉同名檔**。金鑰放 `tools/azure.key`（第一行 KEY 1、第二行區域 `eastasia`，已 gitignore，記事本存檔會多個 `.txt` 要改掉）；資源是 Free F0 層、每月 50 萬字免費，F0 商用授權不明，**發佈前改 S0 重跑一次整批**。台詞清單在 `tools/prompts.csv`，2026-11-05 全部縮短過（「哇～答對了！你好棒喔！」→「答對了！好棒！」），有逗號的句子要加引號不然 csv 會切掉。舊的 edge-tts `.mp3` 已全刪，`make-voices-edge.py` 只是歷史。**不要跑 `make-prompts.ps1`／`make-voices.ps1`**（舊的 SAPI 版）：它們只檢查 `.wav` 存不存在，會把全部台詞重生成 SAPI 的 `.wav`，而 Builder 的 `Prompt()` 優先吃 `.wav`，小熊的聲音會整個被換掉。
- 吉祥物：五張蜂蜜小熊 Gemini 生成並去背（Art/Mascot，2026-11-13 換成 Pok Pok 風、無框線）。`EnsureMascotPlaceholders()` 只在檔案缺時畫程式佔位。
- 去白底：`python tools/knockout-white.py 圖.png ...`（純 Python，從四邊淹白、邊緣半透明）。
- 丟完素材要查 `.meta` 有沒有一起進版控：`python tools/check-meta.py`（純 Python，發現漏的回傳 1；`--fix-cmd` 會印出補齊用的 `git add` 指令）。`.meta` 裡的 GUID 是 Unity 唯一的資產參照，圖 commit 了但 `.meta` 沒 commit，別台機器 clone 下來 GUID 會重生、參照全斷。
- 方塊底圖：`Art/UI/roundrect.png` 程式畫的大圓角（半徑 72，九宮格），所有 `CreateTile` 共用。
- 家人相片卡（2026-11-05）：預設稱呼語音 `Audio/Prompts/family_{dad,mom,grandpa,grandma}{,_en}.mp3`；相簿挑圖靠第三方套件 **NativeGallery 1.9.5**（MIT，`Packages/manifest.json` 的 scopedRegistry `package.openupm.com` + `com.yasirkula.nativegallery`，版本釘死）。iOS 用途字串由 `BabyAppPlayerSetup` 寫進 `ProjectSettings/NativeGallery.json`。Android 走的是 `ACTION_GET_CONTENT` 不是 Google 的 Photo Picker，AAR manifest 帶 `READ/WRITE_EXTERNAL_STORAGE`，上架前要用 Custom Main Manifest 拿掉。WebGL 整個功能不可用（入口已藏）。規格與驗收清單在 `docs/specs/2026-11-05-家人相片卡.md`。
- 發佈前要處理：字體授權、CC BY 標註、Azure 語音用 S0 重跑、NativeGallery 的 Android 權限清理、App Privacy／Data safety 誠實勾選（家人照片是兒童相關內容，只存本機）。

## 用 Gemini 批次生圖（用使用者的 Chrome，不用內建瀏覽器）

**走 Claude in Chrome（`mcp__claude-in-chrome__*`），不要走內建瀏覽器。** 2026-11-12 把內建瀏覽器的路全試過了，都死：每個 `<a download>` 跳一次「另存新檔」；`showDirectoryPicker` 連唯讀都回 AbortError（Electron 不給網頁檔案權限）；`navigator.clipboard.write` 被拒；fetch 打本機 HTTP 伺服器被 Gemini 的 CSP `connect-src` 擋掉（寫過 `tools/image-drop-server.py`，就是這樣死的，已刪）；圖沒有公開 URL（重新載入對話後是 lh3 的 cookie 保護網址，curl 會 302）。使用者的 Chrome 則**零點擊**：網頁觸發的下載直接落地、檔名照 `a.download`。

流程：`list_connected_browsers` 先認清哪台是**這台電腦**（`onThisComputer: true`；使用者另一台電腦的 Chrome 也會連著，選錯檔案會存到那台去，2026-11-12 就發生過）→ `select_browser` → `tabs_context_mcp` → 開 gemini.google.com/app → 「+」→「建立圖像」→ 貼 prompt → Enter。生好的圖在頁面上是 `blob:` 的 `<img>`，用 javascript_tool 畫到 canvas → `toBlob` → `<a download="animal_dog.png">`，檔案落在**這個 Chrome 設定檔的下載資料夾**（Profile 1 是 `C:\Users\Bachider\Desktop`，從 `%LOCALAPPDATA%\Google\Chrome\User Data\*\Preferences` 的 `download.default_directory` 查），再 `mv` 進 `tools/gemini-incoming/`（gitignored）→ `python tools/knockout-white.py` 在副本上去背 → cp 進 `Assets/BabyApp/Art/...` → 跑選單 2。

**節奏（會咬人）**：一張 ~40～65 秒。**圖還沒出現就送下一句，前一句的回覆會變成空的**，之後那個對話就壞了（兩句都空）；所以每送一句就輪詢 `img` 數量（naturalWidth ≥ 512、排除 gstatic）確認多了一張才送下一句，一句最多等 ~2 分鐘，沒圖就重送同一句一次。每一組卡用**新對話**，第一句貼完整風格段落 + 主體，之後只貼「Same style exactly (flat hand-cut paper, solid fills, NO outlines, …). Subject: …」。一次 browser_batch 最多 25 個動作；視窗窄時用 `find` 拿 ref 而不是座標。**額度**：免費層一天約 25～30 張就會開始偶爾空回覆，最後 Gemini 直接回「我的程式設計裡沒有這樣的功能」= 今天沒了，等隔天同一時間。

## 待辦 / 想法

- 家人相片卡第二階段：相機（NativeCamera）、家人卡進找一找／配對（要動 `DeckLibrary` 來源並做方形裁切給拼圖）、自訂槽打字取名、超過 8 格。
- iPad 安裝路線（有 Mac 用 Xcode／沒 Mac 出 WebGL）使用者還沒定。

## 語言

對使用者一律繁體中文；程式註解繁中；識別字英文。
