using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Data
{
    /// <summary>
    /// 從 Resources/Decks 載入所有卡片組，依 sortOrder 排序。
    /// 新增主題只要丟一個 CardDeck 資產進那個資料夾，不需要改程式。
    /// </summary>
    public static class DeckLibrary
    {
        public const string ResourcesFolder = "Decks";

        private static List<CardDeck> _cache;

        public static List<CardDeck> All
        {
            get
            {
                if (_cache != null) return _cache;

                var loaded = Resources.LoadAll<CardDeck>(ResourcesFolder);
                _cache = new List<CardDeck>();
                foreach (var deck in loaded)
                {
                    if (deck != null && deck.PlayableCards().Count > 0) _cache.Add(deck);
                }
                _cache.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

                if (_cache.Count == 0)
                {
                    Debug.LogWarning(
                        "[BabyApp] 還沒有可用的卡片組。請先放入卡片圖，再執行選單 BabyApp/2. 匯入卡片素材。");
                }
                return _cache;
            }
        }

        /// <summary>素材換過之後重新載入（Editor 用）。</summary>
        public static void Invalidate() => _cache = null;
    }
}
