"""Shared helpers for the content generation tools."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONTENT_JSON = ROOT / "Assets" / "Resources" / "Content" / "cards.json"
CARDS_DIR = ROOT / "Assets" / "Resources" / "Cards"
AUDIO_DIR = ROOT / "Assets" / "Resources" / "Audio"


def load_catalog():
    with CONTENT_JSON.open(encoding="utf-8") as f:
        return json.load(f)


def iter_cards(catalog, category_filter=None):
    for category in catalog["categories"]:
        if category_filter and category["id"] not in category_filter:
            continue
        for card in category["cards"]:
            yield category, card
