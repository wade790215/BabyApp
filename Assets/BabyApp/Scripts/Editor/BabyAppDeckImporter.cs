using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BabyApp.Data;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 讀 CardManifest.csv，把每一組卡片做成 CardDeck 資產，
    /// 並自動把同名的圖片與語音檔接上去。
    ///
    /// 流程就是：改 CSV 或丟新素材 → 跑這個 → 進遊戲就看得到。
    /// 素材還沒生好的卡片會被跳過，不會讓 App 出現空白卡。
    /// </summary>
    public static class BabyAppDeckImporter
    {
        private class Row
        {
            public string DeckId;
            public string DeckName;
            public string DeckNameEn;
            public int SortOrder;
            public Color ThemeColor;
            public string CardId;
            public string CardName;
            public string CardNameEn;
        }

        public static void Import()
        {
            if (!File.Exists(BabyAppPaths.Manifest))
            {
                Debug.LogError($"[BabyApp] 找不到卡片清單 {BabyAppPaths.Manifest}。");
                return;
            }

            var rows = ReadManifest(BabyAppPaths.Manifest);
            if (rows.Count == 0)
            {
                Debug.LogError("[BabyApp] 卡片清單是空的，或格式不符。");
                return;
            }

            Directory.CreateDirectory(BabyAppPaths.DeckAssets);

            // 先把素材整個掃一次做成查表，比每張卡都去 FindAssets 快很多
            var sprites = BuildAssetLookup<Sprite>(BabyAppPaths.Root + "/Art");
            var clips = BuildAssetLookup<AudioClip>(BabyAppPaths.Root + "/Audio");

            int totalCards = 0, withImage = 0, withVoice = 0;
            var report = new StringBuilder();

            foreach (var group in rows.GroupBy(r => r.DeckId))
            {
                var first = group.First();
                string assetPath = $"{BabyAppPaths.DeckAssets}/Deck_{first.DeckId}.asset";

                var deck = AssetDatabase.LoadAssetAtPath<CardDeck>(assetPath);
                bool isNew = deck == null;
                if (isNew) deck = ScriptableObject.CreateInstance<CardDeck>();

                deck.deckId = first.DeckId;
                deck.displayName = first.DeckName;
                deck.displayNameEn = first.DeckNameEn;
                deck.sortOrder = first.SortOrder;
                deck.themeColor = first.ThemeColor;

                sprites.TryGetValue("cover_" + first.DeckId, out var cover);
                deck.cover = cover;

                deck.cards.Clear();
                int groupWithImage = 0;
                foreach (var row in group)
                {
                    sprites.TryGetValue(row.CardId, out var sprite);
                    clips.TryGetValue(row.CardId, out var clip);
                    // 英文名稱語音用 _en 後綴、叫聲用 _sound 後綴：animal_dog_en.wav、animal_dog_sound.wav
                    clips.TryGetValue(row.CardId + "_en", out var clipEn);
                    clips.TryGetValue(row.CardId + "_sound", out var sound);

                    deck.cards.Add(new CardEntry
                    {
                        id = row.CardId,
                        displayName = row.CardName,
                        displayNameEn = row.CardNameEn,
                        image = sprite,
                        voice = clip,
                        voiceEn = clipEn,
                        sound = sound
                    });

                    totalCards++;
                    if (sprite != null) { withImage++; groupWithImage++; }
                    if (clip != null) withVoice++;
                }

                if (isNew) AssetDatabase.CreateAsset(deck, assetPath);
                EditorUtility.SetDirty(deck);

                report.AppendLine(
                    $"  {first.DeckName} ({first.DeckId})：{group.Count()} 張卡，已有圖 {groupWithImage} 張");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DeckLibrary.Invalidate();

            Debug.Log(
                $"[BabyApp] 卡片匯入完成。共 {totalCards} 張，已接上圖片 {withImage} 張、語音 {withVoice} 段。\n"
                + report
                + (withImage == 0
                    ? "\n目前還沒有任何圖片，所以會先用純文字卡讓你試玩整個流程。"
                      + $"\n生圖方式見 docs/AI圖片生成指南.md，檔名用卡片代號，放進 {BabyAppPaths.CardArt} 的對應資料夾。"
                    : string.Empty));
        }

        private static List<Row> ReadManifest(string path)
        {
            var rows = new List<Row>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart('﻿').Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (i == 0 && line.StartsWith("deckId")) continue;

                var parts = line.Split(',');
                if (parts.Length < 8)
                {
                    Debug.LogWarning(
                        $"[BabyApp] 第 {i + 1} 行欄位不足（要 8 欄：deckId,deckName,deckNameEn,sortOrder,themeColor,cardId,cardName,cardNameEn），已跳過：{line}");
                    continue;
                }

                rows.Add(new Row
                {
                    DeckId = parts[0].Trim(),
                    DeckName = parts[1].Trim(),
                    DeckNameEn = parts[2].Trim(),
                    SortOrder = int.TryParse(parts[3].Trim(), out var order) ? order : 0,
                    ThemeColor = ParseColor(parts[4].Trim()),
                    CardId = parts[5].Trim(),
                    CardName = parts[6].Trim(),
                    CardNameEn = parts[7].Trim()
                });
            }
            return rows;
        }

        private static Color ParseColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color)) return color;
            Debug.LogWarning($"[BabyApp] 顏色「{hex}」看不懂，先用預設黃色。");
            return new Color(1f, 0.82f, 0.4f);
        }

        /// <summary>把資料夾裡的素材做成「檔名(不含副檔名) → 資產」的查表。</summary>
        private static Dictionary<string, T> BuildAssetLookup<T>(string folder) where T : Object
        {
            var lookup = new Dictionary<string, T>(System.StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(folder)) return lookup;

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null) continue;

                var key = Path.GetFileNameWithoutExtension(assetPath);
                if (lookup.ContainsKey(key))
                {
                    Debug.LogWarning(
                        $"[BabyApp] 有兩個同名素材「{key}」，會用先找到的那個：{assetPath}");
                    continue;
                }
                lookup[key] = asset;
            }
            return lookup;
        }

        /// <summary>給文件用：列出清單裡還缺圖的卡片代號。</summary>
        public static List<string> MissingImageIds()
        {
            var missing = new List<string>();
            if (!File.Exists(BabyAppPaths.Manifest)) return missing;

            var sprites = BuildAssetLookup<Sprite>(BabyAppPaths.Root + "/Art");
            foreach (var row in ReadManifest(BabyAppPaths.Manifest))
            {
                if (!sprites.ContainsKey(row.CardId)) missing.Add(row.CardId);
            }
            return missing;
        }
    }
}
