# 從維基共享資源抓卡片的叫聲，存成 {cardId}_sound.{ext}；授權與出處寫到 tools/commons-sounds-credits.json。
# 需要重抓時直接再跑一次；已存在的檔會跳過。
import json, os, re, sys, time, urllib.error, urllib.parse, urllib.request
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
AUDIO = os.path.join(ROOT, "Assets", "BabyApp", "Audio", "Cards")
UA = {"User-Agent": "BabyAppFetcher/1.0 (personal toddler learning app; Unity project; one-off asset fetch)"}

PICKS = [
    ("animal_dog",        "Animals",  "Barking of a dog.ogg"),
    ("animal_cat",        "Animals",  "Meow of a pleading cat.oga"),
    ("animal_elephant",   "Animals",  "Elephant voice - trumpeting.ogg"),
    ("animal_lion",       "Animals",  "Lion Mad.ogg"),
    ("animal_monkey",     "Animals",  "Pant-hoot call made by a male chimpanzee.ogg"),
    ("animal_bear",       "Animals",  "Bear growl.ogg"),
    ("animal_pig",        "Animals",  "Mudchute pig 1.ogg"),
    ("animal_duck",       "Animals",  "Pekin duck & mallard.ogg"),
    ("animal_fish",       "Animals",  "Bubble 01 nevit.ogg"),
    ("animal_bird",       "Animals",  "House Sparrows chirping.ogg"),
    ("animal_cow",        "Animals",  "Single Cow Moo.ogg"),
    ("fruits",            "Fruits",   "Crunch (Gravity Sound).wav"),   # 整組共用
    ("vehicle_car",       "Vehicles", "Car Horn.wav"),
    ("vehicle_bus",       "Vehicles", "WWS CityBusMANSG220horn.ogg"),
    ("vehicle_truck",     "Vehicles", "WWS FSRTarpan239Dvanstarting.ogg"),
    ("vehicle_train",     "Vehicles", "WWS SteamWhistle.ogg"),
    ("vehicle_airplane",  "Vehicles", "WWS SaabJ35DDraken8211passingtwotimes.ogg"),
    ("vehicle_boat",      "Vehicles", "Cruise ship Albatros ship horn.ogg"),
    ("vehicle_bicycle",   "Vehicles", "Ding Dong Bicycle Bell A.ogg"),
    ("vehicle_motorcycle","Vehicles", "Kawasaki ER-5.oga"),
    ("vehicle_firetruck", "Vehicles", "WWS Firedepartmentbell.ogg"),
    ("vehicle_ambulance", "Vehicles", "AmbulanceSound1.wav"),
    ("vehicle_police",    "Vehicles", "American police siren i.ogg"),
    ("construction",      "Construction", "WWS FSRTarpan239Dvanstarting.ogg"),   # 整組共用柴油引擎
    ("construction_forklift", "Construction", "WWS Forklifttruck.ogg"),
]

def fetch(url, retries=4):
    """帶 User-Agent 的請求；被限流（429）就等一下再試。"""
    for attempt in range(retries):
        try:
            return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60)
        except urllib.error.HTTPError as e:
            if e.code == 429 and attempt < retries - 1:
                time.sleep(15 * (attempt + 1)); continue
            raise

def download(url, out):
    with fetch(url) as r, open(out, "wb") as f:
        while True:
            chunk = r.read(65536)
            if not chunk: break
            f.write(chunk)

def api(title):
    url = ("https://commons.wikimedia.org/w/api.php?action=query&titles=" + urllib.parse.quote("File:" + title)
           + "&prop=imageinfo&iiprop=url|size|extmetadata&iiextmetadatafilter=LicenseShortName|Artist|LicenseUrl&format=json")
    with fetch(url) as r:
        d = json.load(r)
    page = next(iter(d["query"]["pages"].values()))
    return page["imageinfo"][0]

credits, ok, fail = [], 0, 0
for card_id, folder, title in PICKS:
    ext = os.path.splitext(title)[1].lower()
    if ext == ".oga": ext = ".ogg"   # Unity 只認 .ogg
    out = os.path.join(AUDIO, folder, f"{card_id}_sound{ext}")
    try:
        info = api(title)
        meta = info.get("extmetadata", {})
        artist = re.sub(r"<[^>]+>", "", meta.get("Artist", {}).get("value", "")).strip() or "unknown"
        lic = meta.get("LicenseShortName", {}).get("value", "?")
        if not os.path.exists(out):
            download(info["url"], out)
        kb = os.path.getsize(out) // 1024
        credits.append({"cardId": card_id, "file": os.path.basename(out), "title": title,
                        "author": artist, "license": lic, "source": info["descriptionurl"]})
        ok += 1
        print(f"ok   {card_id:20s} {kb:5d}KB  {lic:16s} {title}")
    except Exception as e:
        fail += 1
        print(f"FAIL {card_id:20s} {title}  ({str(e)[:60]})")
    time.sleep(3)

with open(os.path.join(ROOT, "tools", "commons-sounds-credits.json"), "w", encoding="utf-8") as f:
    json.dump(credits, f, ensure_ascii=False, indent=2)
print(f"---\n抓到 {ok} 個，失敗 {fail} 個")
