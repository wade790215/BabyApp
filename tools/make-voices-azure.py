# 用 Azure AI Speech 的 neural 聲音產生名稱語音與小熊台詞，取代 Windows SAPI。
# 純 Python 標準庫，不裝套件。金鑰只從環境變數讀，不要寫進任何檔案。
#
#   set AZURE_SPEECH_KEY=...        （PowerShell：$env:AZURE_SPEECH_KEY = "..."）
#   set AZURE_SPEECH_REGION=eastasia（資源所在區域，建 Speech 資源時選的）
#
#   python tools/make-voices-azure.py --list-voices          列出 zh-TW 有哪些聲音
#   python tools/make-voices-azure.py --trial                 兩個女聲各念幾句到 tools/azure-trial/，先聽
#   python tools/make-voices-azure.py --voice zh-TW-HsiaoChenNeural --cards --prompts
#                                                             整批產出到 Assets 底下（會蓋掉同名 .wav）
#
# 產出是 24kHz 16-bit mono WAV，頭尾靜音會剪掉（留 60ms），不會再有 SAPI 那種墊 1.3 秒的事。
# 語速預設 -10%（2～3 歲聽的，慢一點點），音高不動；要調就 --rate -20% --pitch +5%。
import argparse, csv, io, json, os, struct, sys, time, urllib.request, urllib.error, wave

# Windows 的 console 預設 cp950，印繁中會變亂碼；sys.exit 的訊息走 stderr，兩個都要設
for stream in (sys.stdout, sys.stderr):
    if hasattr(stream, "reconfigure"):
        stream.reconfigure(encoding="utf-8")

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(PROJECT, "Assets", "BabyApp", "CardManifest.csv")
PROMPTS = os.path.join(PROJECT, "tools", "prompts.csv")
CARD_AUDIO = os.path.join(PROJECT, "Assets", "BabyApp", "Audio", "Cards")
PROMPT_AUDIO = os.path.join(PROJECT, "Assets", "BabyApp", "Audio", "Prompts")
TRIAL_DIR = os.path.join(PROJECT, "tools", "azure-trial")

ZH_VOICES = ["zh-TW-HsiaoChenNeural", "zh-TW-HsiaoYuNeural"]
EN_VOICE = "en-US-AvaMultilingualNeural"   # 英文名稱用；Ava 是 multilingual，也能念 zh-TW，但中文還是用台灣的聲音
OUTPUT_FORMAT = "riff-24khz-16bit-mono-pcm"

# 試聽用的句子：三個會露出「墊靜音」問題的短詞、兩句縮短後的台詞
TRIAL_LINES = [
    ("dog", "狗狗"),
    ("apple", "蘋果"),
    ("three", "三"),
    ("where", "在哪裡呀？"),
    ("correct", "答對了！好棒！"),
]


def need_env(name):
    v = os.environ.get(name, "").strip()
    if not v:
        sys.exit("缺環境變數 %s。建好 Azure 的 Speech 資源後，到「金鑰與端點」頁抄 KEY 1 與 Region。" % name)
    return v


def list_voices(key, region):
    # 聲音清單 API 走 <region>.tts.speech.microsoft.com，跟合成同一台主機
    url = "https://%s.tts.speech.microsoft.com/cognitiveservices/voices/list" % region
    req = urllib.request.Request(url, headers={"Ocp-Apim-Subscription-Key": key})
    with urllib.request.urlopen(req, timeout=30) as r:
        voices = json.loads(r.read().decode("utf-8"))
    rows = [v for v in voices if v.get("Locale") == "zh-TW" or "zh-TW" in str(v.get("SecondaryLocaleList", ""))]
    print("zh-TW 相關的聲音（%d）：" % len(rows))
    for v in rows:
        print("  %-32s %-7s %s" % (v["ShortName"], v.get("Gender", ""), v.get("LocalName", "")))


def synthesize(key, region, voice, text, rate, pitch, lang):
    prosody = '<prosody rate="%s" pitch="%s">%s</prosody>' % (rate, pitch, xml_escape(text))
    ssml = ('<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" '
            'xmlns:mstts="https://www.w3.org/2001/mstts" xml:lang="%s">'
            '<voice name="%s">%s</voice></speak>') % (lang, voice, prosody)
    url = "https://%s.tts.speech.microsoft.com/cognitiveservices/v1" % region
    req = urllib.request.Request(url, data=ssml.encode("utf-8"), method="POST", headers={
        "Ocp-Apim-Subscription-Key": key,
        "Content-Type": "application/ssml+xml",
        "X-Microsoft-OutputFormat": OUTPUT_FORMAT,
        "User-Agent": "BabyApp-voices",
    })
    for attempt in range(3):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read()
        except urllib.error.HTTPError as e:
            body = e.read().decode("utf-8", "replace")[:300]
            if e.code == 429 and attempt < 2:
                time.sleep(2 + attempt * 2)
                continue
            sys.exit("Azure 回 HTTP %d：%s" % (e.code, body))


def xml_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def trim_silence(wav_bytes, threshold=300, pad_ms=60):
    """剪掉 16-bit mono WAV 頭尾的靜音。threshold 是 int16 絕對值（300 ≈ 滿刻度 1%）。"""
    with wave.open(io.BytesIO(wav_bytes)) as w:
        params = w.getparams()
        frames = w.readframes(w.getnframes())
    if params.sampwidth != 2 or params.nchannels != 1:
        return wav_bytes  # 不是預期格式就原樣存，不要亂剪
    n = len(frames) // 2
    samples = struct.unpack("<%dh" % n, frames)
    first = next((i for i, s in enumerate(samples) if abs(s) > threshold), None)
    if first is None:
        return wav_bytes
    last = next(i for i in range(n - 1, -1, -1) if abs(samples[i]) > threshold)
    pad = int(params.framerate * pad_ms / 1000)
    a, b = max(0, first - pad), min(n, last + 1 + pad)
    out = io.BytesIO()
    with wave.open(out, "wb") as w:
        w.setparams(params)
        w.writeframes(struct.pack("<%dh" % (b - a), *samples[a:b]))
    return out.getvalue()


def write_wav(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(data)
    with wave.open(path) as w:
        return w.getnframes() / w.getframerate()


def run_trial(key, region, rate, pitch):
    print("試聽檔寫到 %s" % TRIAL_DIR)
    for voice in ZH_VOICES:
        short = voice.split("-")[2].replace("Neural", "")
        for tag, text in TRIAL_LINES:
            data = trim_silence(synthesize(key, region, voice, text, rate, pitch, "zh-TW"))
            path = os.path.join(TRIAL_DIR, "%s_%s.wav" % (short, tag))
            print("  %5.2fs  %-24s %s" % (write_wav(path, data), os.path.basename(path), text))


def run_cards(key, region, voice, rate, pitch, en_voice):
    with open(MANIFEST, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    made = 0
    for row in rows:
        folder = row["deckId"][:1].upper() + row["deckId"][1:]
        base = os.path.join(CARD_AUDIO, folder, row["cardId"])
        for suffix, text, v, lang in (("", row["cardName"], voice, "zh-TW"), ("_en", row["cardNameEn"], en_voice, "en-US")):
            data = trim_silence(synthesize(key, region, v, text, rate, pitch, lang))
            secs = write_wav(base + suffix + ".wav", data)
            made += 1
            print("  %5.2fs  %s%s.wav  %s" % (secs, row["cardId"], suffix, text))
    print("卡片名稱：%d 個檔" % made)


def run_prompts(key, region, voice, rate, pitch, en_voice):
    with open(PROMPTS, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    made = 0
    for row in rows:
        en = row["lang"] == "en"
        data = trim_silence(synthesize(key, region, en_voice if en else voice, row["text"], rate, pitch,
                                       "en-US" if en else "zh-TW"))
        secs = write_wav(os.path.join(PROMPT_AUDIO, row["file"] + ".wav"), data)
        made += 1
        print("  %5.2fs  %s.wav  %s" % (secs, row["file"], row["text"]))
    print("台詞：%d 個檔" % made)


def main():
    ap = argparse.ArgumentParser(description="用 Azure neural TTS 產生 BabyApp 的語音")
    ap.add_argument("--list-voices", action="store_true")
    ap.add_argument("--trial", action="store_true", help="兩個女聲各念幾句到 tools/azure-trial/")
    ap.add_argument("--cards", action="store_true", help="整批產出卡片名稱（中英）")
    ap.add_argument("--prompts", action="store_true", help="整批產出小熊台詞（中英）")
    ap.add_argument("--voice", default=ZH_VOICES[0], help="中文聲音，預設 %s" % ZH_VOICES[0])
    ap.add_argument("--en-voice", default=EN_VOICE)
    ap.add_argument("--rate", default="-10%", help='語速，例如 -10%%、slow；預設 -10%%')
    ap.add_argument("--pitch", default="+0%", help='音高，例如 +5%%；預設不動')
    a = ap.parse_args()
    if not (a.list_voices or a.trial or a.cards or a.prompts):
        ap.print_help()
        return 1

    key, region = need_env("AZURE_SPEECH_KEY"), need_env("AZURE_SPEECH_REGION")
    if a.list_voices:
        list_voices(key, region)
    if a.trial:
        run_trial(key, region, a.rate, a.pitch)
    if a.cards:
        run_cards(key, region, a.voice, a.rate, a.pitch, a.en_voice)
    if a.prompts:
        run_prompts(key, region, a.voice, a.rate, a.pitch, a.en_voice)
    if a.cards or a.prompts:
        print("\n接著：把同名的舊 .mp3 刪掉（不然一張卡會有兩個候選），再跑 Unity 選單「2. 匯入卡片素材」。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
