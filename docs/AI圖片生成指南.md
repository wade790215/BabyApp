# AI 圖片生成指南

這份文件是給「我會認東西」這支 App 產素材用的。照著做，36 張卡片圖會長得像同一套教具，而不是 36 種風格拼起來。

---

## 1. 最重要的三件事

1. **檔名就是接線的鑰匙。** 檔名必須完全等於下面表格的卡片代號，例如 `animal_dog.png`。程式靠檔名自動接圖，名字錯一個字母就接不上。
2. **風格段落每次都要貼。** 下面那段英文提示詞不要改，只換 `[SUBJECT]`。這是 36 張圖看起來像一套的唯一原因。
3. **同一個對話連續生完一個主題。** 圖片模型會參考前文，同一串對話裡生出來的風格最接近。換主題（動物 → 水果）時再開新對話。

---

## 2. 風格段落（固定不要改）

把 `[SUBJECT]` 換成表格裡的 Subject，整段貼給圖片模型：

```
Flat vector illustration of [SUBJECT], drawn as a flashcard for a 2-year-old child.
Single subject only, centered, facing the viewer, entire shape visible and filling
most of the frame. Chunky rounded forms like a wooden toy, thick bold black outlines,
bold saturated primary colors, simple flat shading. Friendly and calm. Plain pure
white background. No text, no letters, no props, no background scenery, no drop
shadows, no photorealism. Square 1:1 composition, 1024x1024, sturdy toy-block style.
```

**規格**：1024×1024 正方形、PNG、白底（透明底更好）。

### 為什麼這樣寫

- **單一主體、置中、佔滿畫面** — 2～3 歲的視覺注意力很窄，畫面裡多一隻蝴蝶他就會指蝴蝶而不是狗。
- **粗黑邊、積木感、飽和原色** — 這是為了跟 App 的「工程車工地」介面對上：深藍底、粗黑框的卡片、工程黃的按鈕。圖本身還是乾淨的單一主體，不要讓狗戴安全帽——主題在介面上，不在圖裡。
- **輪廓清楚** — 在平板上離遠一點也認得出來。
- **白底 / 透明底** — 卡片底板是白色圓角卡，白底才不會出現一個灰色方塊。
- **不要文字** — 圖上的英文字會干擾中文名稱，而且 AI 生成的文字幾乎都是亂碼。
- **不要擬人化**（不要讓動物穿衣服、站起來） — 這一版的目的是認得「真實世界的狗」。

---

## 3. 卡片清單（36 張）

### 動物 — 存到 `Assets/BabyApp/Art/Cards/Animals/`

| 檔名 | 中文名 | Subject |
|---|---|---|
| `animal_dog.png` | 狗狗 | a friendly brown puppy, sitting, tail up |
| `animal_cat.png` | 貓咪 | a cute orange tabby cat, sitting, tail curled |
| `animal_rabbit.png` | 兔子 | a white rabbit with long ears, sitting |
| `animal_elephant.png` | 大象 | a grey elephant with a raised trunk, standing side view |
| `animal_lion.png` | 獅子 | a golden lion with a full mane, sitting, gentle face |
| `animal_monkey.png` | 猴子 | a brown monkey sitting, holding its tail |
| `animal_bear.png` | 熊 | a round brown bear, sitting, front view |
| `animal_pig.png` | 豬 | a pink pig standing side view, curly tail |
| `animal_duck.png` | 鴨子 | a yellow duck standing side view, orange beak |
| `animal_fish.png` | 魚 | a single orange fish, side view, simple fins |
| `animal_bird.png` | 小鳥 | a small blue bird perched, side view |
| `animal_cow.png` | 牛 | a white cow with black patches, standing side view |

### 水果 — 存到 `Assets/BabyApp/Art/Cards/Fruits/`

| 檔名 | 中文名 | Subject |
|---|---|---|
| `fruit_apple.png` | 蘋果 | one red apple with a green leaf |
| `fruit_banana.png` | 香蕉 | a bunch of three yellow bananas |
| `fruit_orange.png` | 橘子 | one whole orange with a green leaf |
| `fruit_grape.png` | 葡萄 | a cluster of purple grapes with a green leaf |
| `fruit_strawberry.png` | 草莓 | one red strawberry with green leaves on top |
| `fruit_watermelon.png` | 西瓜 | one triangular slice of watermelon, red flesh and green rind |
| `fruit_pineapple.png` | 鳳梨 | one whole pineapple with spiky green leaves |
| `fruit_pear.png` | 梨子 | one yellow-green pear with a small leaf |
| `fruit_peach.png` | 桃子 | one pink peach with a green leaf |
| `fruit_mango.png` | 芒果 | one yellow-orange mango with a green leaf |
| `fruit_lemon.png` | 檸檬 | one bright yellow lemon with a green leaf |
| `fruit_tomato.png` | 番茄 | one red tomato with a green stem on top |

### 交通工具 — 存到 `Assets/BabyApp/Art/Cards/Vehicles/`

| 檔名 | 中文名 | Subject |
|---|---|---|
| `vehicle_car.png` | 汽車 | a small red car, side view |
| `vehicle_bus.png` | 公車 | a yellow city bus, side view |
| `vehicle_truck.png` | 卡車 | a blue cargo truck, side view |
| `vehicle_train.png` | 火車 | a green steam train locomotive, side view |
| `vehicle_airplane.png` | 飛機 | a white passenger airplane, side view, wings visible |
| `vehicle_boat.png` | 船 | a small wooden boat with a white sail, side view |
| `vehicle_bicycle.png` | 腳踏車 | a blue bicycle, side view |
| `vehicle_motorcycle.png` | 機車 | a red scooter, side view |
| `vehicle_firetruck.png` | 消防車 | a red fire engine with a ladder, side view |
| `vehicle_ambulance.png` | 救護車 | a white ambulance with a red cross, side view |
| `vehicle_police.png` | 警車 | a white and blue police car, side view |
| `vehicle_excavator.png` | 挖土機 | a yellow excavator with its arm lowered, side view |

### 主題封面（3 張，可選）

首頁三個大按鈕上的圖。不做的話程式會自動拿該主題的第一張卡當封面，所以可以先跳過。

存到 `Assets/BabyApp/Art/Covers/`：

| 檔名 | Subject |
|---|---|
| `cover_animals.png` | a dog, a cat and a rabbit sitting together as a group |
| `cover_fruits.png` | an apple, a banana and a bunch of grapes grouped together |
| `cover_vehicles.png` | a car, a bus and an airplane grouped together |

> 封面是唯一允許多個主體的圖，所以貼風格段落時要把 `Single subject only` 那句改成 `A small group of objects`。

---

### 先從哪張開始

目前 35 張卡已經有 OpenMoji 的佔位圖，只有 **`vehicle_excavator`（挖土機）** 是空的（emoji 沒有挖土機）。所以第一張該生的就是它——生好丟進 `Vehicles/`，匯入後挖土機卡就會出現。

之後想替換哪張就生哪張，同名覆蓋即可，不用一次換完。

## 4. 生完圖之後

1. 把檔案存到上面指定的資料夾，**檔名照表格**。
2. 回到 Unity，等它跑完匯入（圖片會自動被設成 Sprite，不用手動改 Inspector）。
3. 上方選單 **BabyApp → 2. 匯入卡片素材**。
4. Console 會印出哪些卡有圖、哪些還缺。想單獨看缺哪些就按 **BabyApp → 檢查還缺哪些素材**。
5. 按 Play 試玩。

**不用一次生完 36 張。** 先生 3～4 張動物，跑一次匯入，確認小孩的反應再繼續。沒有圖的卡片會自動被跳過，不會出現空白卡。

---

## 5. 想加新卡片或新主題

編輯 `Assets/BabyApp/CardManifest.csv`（用 Excel 或記事本都可以，存成 UTF-8）：

```
deckId,deckName,sortOrder,themeColor,cardId,cardName
animals,動物,0,#FFD166,animal_sheep,綿羊
```

- `deckId` 一樣就是加到同一個主題；填新的 `deckId` 就會多一個首頁按鈕。
- `sortOrder` 決定首頁按鈕的左右順序，數字小的在前面。
- `themeColor` 是該主題的顏色，同一個 `deckId` 的每一行都填一樣的值。

存檔後跑 **BabyApp → 2. 匯入卡片素材**，不用改任何程式。
