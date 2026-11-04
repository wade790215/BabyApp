# 瀏覽器批次下載卡在 .tmp 亂數檔名時，用「圖片內容」把它們對回卡片名稱。
# 用法：python tools/match-downloads.py 指紋.json 目的資料夾規則
#   指紋.json：{ "card_id": [r,g,b × 64] }，在頁面裡把每張圖縮成 8×8 讀出的 RGB（見 CLAUDE.md）
#   每個 .tmp 算同樣的 8×8 色塊平均，取最接近的指紋；會印出次近距離，差距太小會標 !!
import sys, json, glob, os, shutil, math, importlib.util

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
spec = importlib.util.spec_from_file_location("k", os.path.join(ROOT, "tools", "knockout-white.py"))
k = importlib.util.module_from_spec(spec); spec.loader.exec_module(k)

def fingerprint(px, w, h, grid=8, step=8):
    v = []
    bw, bh = w // grid, h // grid
    for by in range(grid):
        for bx in range(grid):
            r = g = b = n = 0
            for y in range(by * bh, by * bh + bh, step):
                row = y * w
                for x in range(bx * bw, bx * bw + bw, step):
                    i = (row + x) * 4
                    r += px[i]; g += px[i + 1]; b += px[i + 2]; n += 1
            v += [r / n, g / n, b / n]
    return v

def folder_for(name):
    prefix = name.split("_")[0]
    return {"daily": "Daily", "body": "Body", "animal": "Animals", "fruit": "Fruits",
            "vehicle": "Vehicles", "construction": "Construction", "color": "Colors"}.get(prefix, prefix.title())

def main():
    fps = json.load(open(sys.argv[1], encoding="utf-8"))
    downloads = sys.argv[2] if len(sys.argv) > 2 else r"C:\Users\Bachider\Downloads"
    cands = {}
    for t in glob.glob(os.path.join(downloads, "*.tmp")):
        try:
            w, h, px = k.read_png(t)
        except Exception:
            continue
        if w < 1024:
            continue
        cands[t] = fingerprint(px, w, h)
    print(f"tmp PNG: {len(cands)} | 指紋: {len(fps)}")

    pairs = sorted((math.sqrt(sum((a - b) ** 2 for a, b in zip(fp, v))), name, t)
                   for name, fp in fps.items() for t, v in cands.items())
    used_n, used_t, match = set(), set(), {}
    for d, name, t in pairs:
        if name in used_n or t in used_t:
            continue
        match[name] = (t, d); used_n.add(name); used_t.add(t)

    for name, (t, d) in match.items():
        second = min((dd for dd, nn, tt in pairs if tt == t and nn != name), default=float("inf"))
        flag = "OK" if second > d * 1.5 else "!! 不夠明確"
        dst = os.path.join(ROOT, "Assets", "BabyApp", "Art", "Cards", folder_for(name), name + ".png")
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy(t, dst)
        print(f"{name:20s} <- {os.path.basename(t)[:8]}  d={d:6.1f}  次近={second:6.1f}  {flag}")
    print("未配對的 tmp:", len(cands) - len(used_t), "| 未配對的指紋:", len(fps) - len(used_n))

if __name__ == "__main__":
    main()
