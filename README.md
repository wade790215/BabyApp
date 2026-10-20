# BabyApp

給 2–3 歲小孩的平板學習 App：看圖、聽發音（英文＋中文）、滑動換卡。
所有圖片和語音都用 AI **事先產生**後打包進 App，離線可用，家長可以先檢查內容。

- Unity 6 (6000.5)、URP、Input System，橫向平板。
- 所有 UI 都用程式碼建立，場景裡只有一個 `App` 物件，不需要在 Editor 裡拉 UI。

## 專案結構

```
Assets/
  Scenes/Main.unity              主場景（只有一個掛著 AppBootstrap 的 App 物件）
  Scripts/
    AppBootstrap.cs              進入點：建立相機、EventSystem、選單畫面、字卡畫面
    CardData.cs                  cards.json 對應的資料類別
    ContentLoader.cs             從 Resources 讀取 json / 圖片 / 聲音，缺檔時自動用替代品
    AudioPlayer.cs               依序播放英文、中文發音；沒有聲音檔時播提示音
    UIFactory.cs                 用程式碼建 uGUI 元件（大按鈕、字型選擇）
    SwipeDetector.cs             左右滑動換卡
    MatchGame.cs                 「聽聲音點圖」配對小遊戲
  Resources/
    Content/cards.json           ★ 內容來源：分類、字卡、中英文、AI 提示詞
    Cards/<id>.png               AI 產生的圖片（由 Tools 產生）
    Audio/<id>_en.mp3, <id>_zh.mp3   AI 產生的發音（由 Tools 產生）
Tools/
  generate_images.py             用 OpenAI Images API 依 cards.json 產圖
  generate_audio.py              用 OpenAI TTS 產英文 / 中文發音
```

## 內容產生流程

```bash
cd Tools
pip install -r requirements.txt
export OPENAI_API_KEY=sk-...

# 1. 圖片（每張卡一張，已存在的會跳過；--force 重做）
python generate_images.py
python generate_images.py --category animals      # 只做一個分類
python generate_images.py --placeholder           # 不用 API，畫簡單的替代圖來測試

# 2. 發音（每張卡英文 + 中文各一個 mp3）
python generate_audio.py
python generate_audio.py --lang en                # 只做英文
```

`cards.json` 裡的 `stylePrompt` 是所有圖片共用的畫風描述，每張卡的 `prompt` 只描述主體。
想加卡片就在 `cards.json` 加一筆，再跑一次兩個腳本（只會產生新的）。

目前 `Resources/Cards` 裡的是 `--placeholder` 產生的替代圖，拿到 API key 後用 `--force` 換成 AI 圖。

## 在 Unity 裡執行

1. 用 Unity 6000.5.x 開啟專案。
2. 打開 `Assets/Scenes/Main.unity`，按 Play。
3. 建置平板：File → Build Profiles，選 Android 或 iOS。畫面方向已設定為只允許橫向。

## 操作方式

- 選單：點分類大按鈕。
- 字卡：顯示時自動唸英文再唸中文；點圖片再聽一次；左右滑動或按兩側大箭頭換卡。
- 左上角小按鈕回選單（故意做小一點，減少小孩誤觸）。
- 字卡右上角的「?」進入配對小遊戲：App 唸一個字，出現三張同分類的圖，點對的那張。
  點錯不扣分，只會把那張淡掉；每答對 5 題出現星星慶祝畫面，點一下繼續。右上角「))」可以再聽一次。
