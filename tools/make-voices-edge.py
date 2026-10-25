"""
用 Microsoft Edge 的神經網路語音（edge-tts，免費、免金鑰）產生卡片名稱與小熊台詞，
取代原本平板的 Windows SAPI 聲音。中文用 zh-TW 曉雨（活潑、正向），英文用 Ana（卡通童聲）。
語速稍快、音調稍高，聽起來像在跟小孩玩，不像在念稿。

用法：
  pip install edge-tts
  python tools/make-voices-edge.py            # 只補缺的
  python tools/make-voices-edge.py --force    # 全部重生（換掉 SAPI 的舊檔）
  python tools/make-voices-edge.py --prompts  # 只做小熊台詞
  python tools/make-voices-edge.py --cards    # 只做卡片名稱

輸出 WAV（Unity 直接吃）。edge-tts 吐的是 MP3，用 ffmpeg 轉；沒有 ffmpeg 就直接存 MP3（Unity 也能吃）。
"""
import asyncio
import csv
import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARD_AUDIO = os.path.join(ROOT, "Assets", "BabyApp", "Audio", "Cards")
PROMPT_AUDIO = os.path.join(ROOT, "Assets", "BabyApp", "Audio", "Prompts")

VOICES = {
    "zh": dict(voice="zh-TW-HsiaoYuNeural", rate="+15%", pitch="+45Hz"),
    "en": dict(voice="en-US-AnaNeural", rate="+5%", pitch="+10Hz"),
}

FFMPEG = shutil.which("ffmpeg")


def jobs(force: bool, do_cards: bool, do_prompts: bool):
    """產生 (輸出路徑不含副檔名, 語言, 文字) 的清單。"""
    out = []
    if do_cards:
        with open(os.path.join(ROOT, "Assets", "BabyApp", "CardManifest.csv"), encoding="utf-8-sig") as f:
            for row in csv.DictReader(f):
                folder = os.path.join(CARD_AUDIO, row["deckId"][:1].upper() + row["deckId"][1:])
                out.append((os.path.join(folder, row["cardId"]), "zh", row["cardName"]))
                out.append((os.path.join(folder, row["cardId"] + "_en"), "en", row["cardNameEn"]))
    if do_prompts:
        with open(os.path.join(ROOT, "tools", "prompts.csv"), encoding="utf-8-sig") as f:
            for row in csv.DictReader(f):
                out.append((os.path.join(PROMPT_AUDIO, row["file"]), row["lang"], row["text"]))
    if not force:
        out = [j for j in out if not (os.path.exists(j[0] + ".wav") or os.path.exists(j[0] + ".mp3"))]
    return out


async def synth(base: str, lang: str, text: str):
    import edge_tts  # noqa: PLC0415

    cfg = VOICES[lang]
    os.makedirs(os.path.dirname(base), exist_ok=True)
    mp3 = base + ".tmp.mp3"
    communicate = edge_tts.Communicate(text, cfg["voice"], rate=cfg["rate"], pitch=cfg["pitch"])
    await communicate.save(mp3)

    if FFMPEG:
        wav = base + ".wav"
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", mp3, "-ar", "22050", "-ac", "1", wav], check=True)
        os.remove(mp3)
        # 以前 SAPI 留下的同名 mp3 不會有，但保險起見
        return wav
    final = base + ".mp3"
    os.replace(mp3, final)
    # 舊的 SAPI wav 要拿掉，不然 Unity 匯入器會先找到 wav
    if os.path.exists(base + ".wav"):
        os.remove(base + ".wav")
    return final


async def main_async(force, do_cards, do_prompts):
    todo = jobs(force, do_cards, do_prompts)
    print(f"要產生 {len(todo)} 段（ffmpeg：{'有，輸出 WAV' if FFMPEG else '沒有，輸出 MP3'}）")
    sem = asyncio.Semaphore(4)
    done = 0

    async def one(job):
        nonlocal done
        async with sem:
            for attempt in range(3):
                try:
                    path = await synth(*job)
                    done += 1
                    print(f"  [{done}/{len(todo)}] {os.path.relpath(path, ROOT)}")
                    return
                except Exception as e:  # noqa: BLE001
                    print(f"  重試 {job[0]}：{e}")
                    await asyncio.sleep(2 + attempt * 2)
            print(f"  失敗：{job[0]}")

    await asyncio.gather(*(one(j) for j in todo))


def main():
    force = "--force" in sys.argv
    do_cards = "--cards" in sys.argv or "--prompts" not in sys.argv
    do_prompts = "--prompts" in sys.argv or "--cards" not in sys.argv
    try:
        import edge_tts  # noqa: F401
    except ImportError:
        sys.exit("請先 pip install edge-tts")
    asyncio.run(main_async(force, do_cards, do_prompts))


if __name__ == "__main__":
    main()
