#!/usr/bin/env python3
"""Generate one illustration per card in cards.json.

Images are written to Assets/Resources/Cards/<card id>.png so Unity can load
them with Resources.Load. Cards that already have an image are skipped unless
--force is given.

Modes:
  default        call the OpenAI Images API (needs OPENAI_API_KEY)
  --placeholder  draw a simple coloured card with the English word, no API
                 needed - handy for testing the app before spending credits
"""
import argparse
import base64
import os
import sys

from common import CARDS_DIR, iter_cards, load_catalog


def hex_to_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def draw_placeholder(category, card, path, size):
    from PIL import Image, ImageDraw, ImageFont

    image = Image.new("RGB", (size, size), "white")
    draw = ImageDraw.Draw(image)
    color = hex_to_rgb(category["color"])
    margin = size // 8
    draw.ellipse((margin, margin, size - margin, size - margin), fill=color)
    try:
        font = ImageFont.truetype("DejaVuSans-Bold.ttf", size // 8)
    except OSError:
        font = ImageFont.load_default()
    text = card["en"]
    box = draw.textbbox((0, 0), text, font=font)
    w, h = box[2] - box[0], box[3] - box[1]
    draw.text(((size - w) / 2, (size - h) / 2 - box[1]), text, fill="white", font=font)
    image.save(path, "PNG")


def generate_with_openai(client, model, style, card, path, size):
    prompt = f"{card['prompt']}, {style}"
    result = client.images.generate(
        model=model,
        prompt=prompt,
        size=f"{size}x{size}",
        n=1,
    )
    data = result.data[0]
    if getattr(data, "b64_json", None):
        path.write_bytes(base64.b64decode(data.b64_json))
    else:
        import urllib.request
        urllib.request.urlretrieve(data.url, path)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--placeholder", action="store_true", help="draw placeholder images instead of calling the API")
    parser.add_argument("--force", action="store_true", help="regenerate images that already exist")
    parser.add_argument("--category", action="append", help="only generate this category id (repeatable)")
    parser.add_argument("--model", default=os.environ.get("IMAGE_MODEL", "gpt-image-1"))
    parser.add_argument("--size", type=int, default=1024)
    args = parser.parse_args()

    catalog = load_catalog()
    CARDS_DIR.mkdir(parents=True, exist_ok=True)

    client = None
    if not args.placeholder:
        if not os.environ.get("OPENAI_API_KEY"):
            sys.exit("OPENAI_API_KEY is not set. Export it, or run with --placeholder.")
        from openai import OpenAI
        client = OpenAI()

    done = skipped = failed = 0
    for category, card in iter_cards(catalog, args.category):
        path = CARDS_DIR / f"{card['id']}.png"
        if path.exists() and not args.force:
            skipped += 1
            continue
        print(f"[{category['id']}] {card['id']} ... ", end="", flush=True)
        try:
            if args.placeholder:
                draw_placeholder(category, card, path, args.size)
            else:
                generate_with_openai(client, args.model, catalog["stylePrompt"], card, path, args.size)
            done += 1
            print("ok")
        except Exception as exc:  # keep going so one bad prompt doesn't stop the batch
            failed += 1
            print(f"FAILED: {exc}")

    print(f"\ngenerated {done}, skipped {skipped}, failed {failed}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
