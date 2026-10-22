# 我會認東西（BabyApp）

給 2～3 歲小孩的認知卡片 App。圖片與語音素材全部用 AI 生成，Unity 製作，目標裝置是 Android 平板。

- Unity 6000.5.1f1ㆍURPㆍ新版 Input System
- 三個主題：動物、水果、交通工具，共 36 張卡
- 操作只有「看圖、聽名稱、換下一張」——沒有計時、沒有分數、沒有答錯

---

## 第一次打開要做的事

在 Unity 上方選單按 **BabyApp → 1. 一鍵初始化專案**。

它會依序做完這些：準備中文字體、產生介面圖示、建立卡片資產、套用橫向平板設定、把 `Main` 場景整個建出來並接好線。跑完按 Play 就能玩，只是卡片還沒有圖。

接著照 [docs/AI圖片生成指南.md](docs/AI圖片生成指南.md) 生圖。**不用一次生完 36 張**，先生 3～4 張動物、匯入、讓小孩玩一下，再決定要不要繼續。

---

## 日常流程

```
生圖 → 丟進 Assets/BabyApp/Art/Cards/<主題>/ → BabyApp → 2. 匯入卡片素材 → 按 Play
```

圖片丟進去會自動被設成 Sprite（這個專案是 3D 範本起家的，預設不會，已經用匯入規則處理掉）。程式靠**檔名**自動接圖，所以檔名一定要跟卡片代號一樣。

缺哪些圖隨時可以按 **BabyApp → 檢查還缺哪些素材** 查。

---

## 選單對照

| 選單 | 什麼時候用 |
|---|---|
| 1. 一鍵初始化專案 | 只有第一次，或想整個重來 |
| 2. 匯入卡片素材 | 每次加了新圖、新語音，或改了 CSV |
| 3. 重建場景 | 改了版面的程式想重新套用（**會蓋掉場景裡的手動調整**） |
| 4. 套用播放器設定 | 設定被改掉想還原 |
| 5. 切換平台到 Android | 要出 APK 之前 |
| 檢查還缺哪些素材 | 想知道還有幾張圖沒生 |

---

## 資料夾

```
Assets/BabyApp/
  CardManifest.csv        卡片內容清單 ← 要加卡片就改這個
  Art/Cards/<主題>/       卡片圖（檔名 = 卡片代號）
  Art/Covers/             首頁主題封面圖（可選）
  Art/UI/                 程式生成的介面圖示
  Audio/Cards/<主題>/      卡片語音（檔名 = 卡片代號）
  Audio/SFX/              介面音效（可選）
  Fonts/                  中文字體
  Resources/Decks/        自動生成的卡片組資產，不用手改
  Scenes/Main.unity       唯一的場景，自動生成
  Scripts/Runtime/        遊戲程式
  Scripts/Editor/         上面那些選單工具
docs/                     素材製作指南
```

---

## 程式結構

單一場景，首頁與卡片畫面用顯示開關切換，所以按下去立刻有反應，沒有載入畫面。

- `AppRoot` — 入口，負責首頁與卡片畫面之間的切換
- `DeckLibrary` — 從 `Resources/Decks` 載入所有卡片組，**新增主題不用改程式**
- `HomeScreen` — 依卡片組自動長出首頁按鈕
- `DeckScreen` — 一次顯示一張卡，處理換頁與重播
- `VoiceService` — 播聲音：一張卡是「先叫聲、再名稱」，哪段沒接上就略過
- `CardDoor` — 蓋在圖上的工地門，每張卡進場都是關著的，要自己點開（躲貓貓機制）
- `TapReaction` — 點卡片的回饋：四種動畫隨機挑、不連續重複，外加一圈星星
- `DraggableCard` — 卡片跟著手指走、微傾斜，甩過門檻就飛出去換下一張，沒過就彈回；箭頭也走同一套動畫
- `PaintScreen` / `PaintCanvas` — 手指塗鴉：六個大色塊、三種粗細、「圖」打開圖片牆自己選底圖（含空白）；長按「擦」清掉
- `SettingsScreen` / `AppSettings` — 家長設定（首頁右上長按「設」）：音效／語音／音樂開關、中／英文切換、五種背景色，存在裝置上
- `Strings` / `LocalizedText` — 介面文字的中英對照表；卡片與主題的英文名在 CSV 的 `cardNameEn` / `deckNameEn` 欄
- `ThemedBackground` — 每個畫面的背景跟著設定的背景色走
- `HoldButton` — 要按住 0.8 秒才觸發，用在「回首頁」與「擦掉」防誤觸
- `Pulse` — 按鈕用的簡單彈跳

幾個刻意的設計決定：

- **沒有素材的卡片會被跳過**，所以素材可以慢慢補，中途不會出現空白卡。
- **鎖橫向**。小小孩拿平板會轉來轉去，畫面跟著轉會打斷他看圖。
- **回首頁要長按**。小孩亂點不會跳離當前主題。
- **場景用程式建**（`BabyAppSceneBuilder`）。手動在 Inspector 拉幾十個欄位容易漏接，而且改版面重跑一次就好。

---

## 出 APK

1. **BabyApp → 5. 切換平台到 Android**（第一次要等素材重新壓縮，可能幾分鐘）
2. File → Build Settings → Build

設定已經套好：套件名稱 `com.yile.babyapp`、最低 Android 7.0、ARM64 + IL2CPP、橫向鎖定。

需要 Unity Hub 裡 Android Build Support 的 **SDK / NDK / OpenJDK** 三項都有裝（這台已經有 AndroidPlayer 模組）。

---

## 介面風格

軟圓淡色的幼兒 App 風：淡粉彩背景（設定裡可換五種）、白色圓角邊加柔和陰影、首頁是自動排的正方形格子、按鈕都是圓的。卡片進場先蓋一張「？」的淡黃蓋子，點了縮小淡出。顏色與邊框寬度集中在 `BabyAppSceneBuilder.cs` 最上面那幾個常數，改完跑「3. 重建場景」。

三個主題各有一個主題色（在 `CardManifest.csv` 的 `themeColor` 欄）：動物暖橘、水果珊瑚粉、交通工具天藍。主題色打在格子與箭頭上，背景跟設定走。

在 Editor 試玩請把 Game 視窗的解析度設成 **2360×1640（iPad）**；用自由比例的小視窗看，右欄按鈕會擠在一起，那不是裝置上的樣子。

## 素材授權

目前的 35 張卡片圖是 OpenMoji 佔位圖（CC BY-SA 4.0），之後會被 AI 圖同名覆蓋。細節與發佈前的注意事項在 [docs/素材授權.md](docs/素材授權.md)。

## 字體授權注意

初始化時會從 `C:\Windows\Fonts\kaiu.ttf`（標楷體）複製一份當開發用字體。標楷體是 Windows 隨附的商用字體，**不能隨 APK 散佈**。

自己家裡裝著玩沒問題；要上架或給別人安裝之前，請下載 [Noto Sans TC](https://fonts.google.com/noto/specimen/Noto+Sans+TC)（開源字體授權），改名成 `BabyApp_Chinese.ttf` 蓋掉 `Assets/BabyApp/Fonts/` 裡那個檔案。場景不用重建。
