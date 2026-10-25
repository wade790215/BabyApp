"""
用 Gemini 圖像 API 批次生卡片圖，再去白底，直接存進專案。
金鑰從環境變數 GEMINI_API_KEY 讀（不要寫進專案）。

用法：
  python tools/gen-images.py tools/gen-list.csv            # 全部缺的都生
  python tools/gen-images.py tools/gen-list.csv daily_cup  # 只生指定的幾個 id
  python tools/gen-images.py tools/gen-list.csv --force    # 已存在也重生

gen-list.csv 欄位：file,folder,subject,kind
  kind = card（單一主體、白底）或 tile（首頁功能格，可有小熊）
已存在的檔會跳過，所以可以重跑。
"""
import base64
import csv
import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODEL = os.environ.get("GEMINI_IMAGE_MODEL", "gemini-2.5-flash-image")

# 跟現有卡片一致的風格：可愛貼紙、粗深棕描邊、柔和平塗、純白底
STYLE_CARD = (
    "Cute kawaii cartoon sticker illustration of {subject}, drawn as a flashcard for a 2-year-old child. "
    "Single subject only, centered, facing the viewer, the whole shape visible and filling most of the frame. "
    "Chunky rounded friendly forms, thick dark brown outline, soft flat pastel-leaning saturated colors, "
    "very simple flat shading, tiny highlights. Calm and friendly. "
    "Plain pure white background (#FFFFFF), nothing else in the scene. "
    "No text, no letters, no props, no background scenery, no drop shadow, no photorealism. "
    "Square 1:1 composition."
)

BEAR = (
    "The mascot is a round honey-colored teddy bear with a cream belly and muzzle, small round ears, "
    "big friendly black eyes, a little dark nose, cute and simple, "
)

STYLE_TILE = (
    "Cute kawaii cartoon sticker illustration: {subject}. " + BEAR +
    "drawn in the same style as a children's flashcard: chunky rounded forms, thick dark brown outline, "
    "soft flat colors, very simple shading. Plain pure white background, nothing else. "
    "No text, no letters, no drop shadow, no photorealism. Square 1:1 composition."
)


def generate(prompt: str, key: str) -> bytes:
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={key}"
    body = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {"responseModalities": ["IMAGE"]},
    }
    req = urllib.request.Request(url, data=json.dumps(body).encode(), headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as resp:
        data = json.load(resp)
    for cand in data.get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            inline = part.get("inlineData") or part.get("inline_data")
            if inline and inline.get("data"):
                return base64.b64decode(inline["data"])
    raise RuntimeError("回應裡沒有圖片：" + json.dumps(data)[:400])


def main():
    key = os.environ.get("GEMINI_API_KEY")
    if not key:
        sys.exit("沒有 GEMINI_API_KEY 環境變數。")

    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv
    if not args:
        sys.exit(__doc__)
    list_path, only = args[0], set(args[1:])

    with open(list_path, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))

    made = skipped = failed = 0
    for row in rows:
        file_id = row["file"].strip()
        if only and file_id not in only:
            continue
        folder = os.path.join(ROOT, row["folder"].strip())
        os.makedirs(folder, exist_ok=True)
        out = os.path.join(folder, file_id + ".png")
        if os.path.exists(out) and not force:
            skipped += 1
            continue

        template = STYLE_TILE if row.get("kind", "card").strip() == "tile" else STYLE_CARD
        prompt = template.format(subject=row["subject"].strip())

        for attempt in range(1, 4):
            try:
                print(f"[{file_id}] 生成中（{MODEL}，第 {attempt} 次）…", flush=True)
                png = generate(prompt, key)
                with open(out, "wb") as f:
                    f.write(png)
                # 去白底：從四邊淹白、邊緣半透明
                subprocess.run([sys.executable, os.path.join(ROOT, "tools", "knockout-white.py"), out], check=False)
                print(f"[{file_id}] OK → {os.path.relpath(out, ROOT)}", flush=True)
                made += 1
                break
            except urllib.error.HTTPError as e:
                msg = e.read().decode(errors="replace")[:300]
                print(f"[{file_id}] HTTP {e.code}: {msg}", flush=True)
                if e.code == 429:
                    time.sleep(20 * attempt)
                elif attempt == 3:
                    failed += 1
            except Exception as e:  # noqa: BLE001
                print(f"[{file_id}] 失敗：{e}", flush=True)
                if attempt == 3:
                    failed += 1
                time.sleep(3)
        else:
            failed += 1

        time.sleep(2)  # 免費額度友善一點

    print(f"\n完成：生了 {made} 張、跳過 {skipped} 張（已存在）、失敗 {failed} 張")


if __name__ == "__main__":
    main()
