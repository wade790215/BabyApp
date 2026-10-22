#!/usr/bin/env bash
# 抓 OpenMoji 的 618x618 PNG 當卡片佔位圖，檔名直接用卡片代號，丟進對應資料夾。
# 對照表在 openmoji-map.txt。之後 AI 圖用同名覆蓋即可。
# 授權：CC BY-SA 4.0，見 docs/素材授權.md
set -euo pipefail
cd "$(dirname "$0")/.."
BASE="https://cdn.jsdelivr.net/gh/hfg-gmuend/openmoji/color/618x618"
ok=0; fail=0
while read -r id cp folder; do
  case "$id" in \#*|"") continue;; esac
  out="Assets/BabyApp/Art/Cards/$folder/$id.png"
  if curl -fsSL "$BASE/$cp.png" -o "$out"; then
    ok=$((ok+1)); echo "ok   $id  ($cp)"
  else
    fail=$((fail+1)); echo "FAIL $id  ($cp)"; rm -f "$out"
  fi
done < tools/openmoji-map.txt
echo "---"; echo "下載 $ok 張，失敗 $fail 張"
