#!/usr/bin/env python3
"""Generate spoken audio for every card in cards.json.

Writes Assets/Resources/Audio/<card id>_en.mp3 (English word) and
<card id>_zh.mp3 (Chinese word) using the OpenAI text-to-speech API.
Existing files are skipped unless --force is given.

Requires OPENAI_API_KEY. There is no placeholder mode: the app plays a
short beep when a clip is missing, so you can test without audio.
"""
import argparse
import os
import sys

from common import AUDIO_DIR, iter_cards, load_catalog

LANGS = {
    "en": lambda card: card["en"],
    "zh": lambda card: card["zh"],
}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--force", action="store_true", help="regenerate clips that already exist")
    parser.add_argument("--category", action="append", help="only generate this category id (repeatable)")
    parser.add_argument("--lang", action="append", choices=list(LANGS), help="only this language (repeatable)")
    parser.add_argument("--model", default=os.environ.get("TTS_MODEL", "gpt-4o-mini-tts"))
    parser.add_argument("--voice", default=os.environ.get("TTS_VOICE", "nova"))
    args = parser.parse_args()

    if not os.environ.get("OPENAI_API_KEY"):
        sys.exit("OPENAI_API_KEY is not set.")
    from openai import OpenAI
    client = OpenAI()

    catalog = load_catalog()
    AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    langs = args.lang or list(LANGS)

    done = skipped = failed = 0
    for category, card in iter_cards(catalog, args.category):
        for lang in langs:
            path = AUDIO_DIR / f"{card['id']}_{lang}.mp3"
            if path.exists() and not args.force:
                skipped += 1
                continue
            text = LANGS[lang](card)
            print(f"[{category['id']}] {card['id']} ({lang}: {text}) ... ", end="", flush=True)
            try:
                with client.audio.speech.with_streaming_response.create(
                    model=args.model,
                    voice=args.voice,
                    input=text,
                    instructions="Speak slowly, warmly and clearly, as if teaching a toddler a single word.",
                    response_format="mp3",
                ) as response:
                    response.stream_to_file(path)
                done += 1
                print("ok")
            except Exception as exc:
                failed += 1
                print(f"FAILED: {exc}")

    print(f"\ngenerated {done}, skipped {skipped}, failed {failed}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
