using System;
using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 給家長看的小統計：今天翻了幾張、最常點哪張、錄了幾段、找對幾次。
    /// 存在裝置上，完全不影響小孩看到的畫面。
    /// </summary>
    public static class UsageStats
    {
        private const string DateKey = "stats.date";
        private const string TodayViewsKey = "stats.today.views";
        private const string TotalViewsKey = "stats.total.views";
        private const string RecordingsKey = "stats.recordings";
        private const string FindCorrectKey = "stats.find.correct";
        private const string CountRoundsKey = "stats.count.rounds";
        private const string CardsKey = "stats.cards"; // id=次數;id=次數

        public static int TodayViews { get { RollDay(); return PlayerPrefs.GetInt(TodayViewsKey, 0); } }
        public static int TotalViews => PlayerPrefs.GetInt(TotalViewsKey, 0);
        public static int Recordings => PlayerPrefs.GetInt(RecordingsKey, 0);
        public static int FindCorrect => PlayerPrefs.GetInt(FindCorrectKey, 0);
        public static int CountRounds => PlayerPrefs.GetInt(CountRoundsKey, 0);

        public static void CardViewed(string cardId)
        {
            RollDay();
            Bump(TodayViewsKey);
            Bump(TotalViewsKey);

            if (string.IsNullOrEmpty(cardId)) return;
            var counts = LoadCards();
            counts.TryGetValue(cardId, out int n);
            counts[cardId] = n + 1;
            SaveCards(counts);
        }

        public static void RecordingMade() => Bump(RecordingsKey);
        public static void FindAnsweredCorrectly() => Bump(FindCorrectKey);
        public static void CountRoundFinished() => Bump(CountRoundsKey);

        /// <summary>點最多次的卡片代號；還沒有就是 null。</summary>
        public static string TopCardId()
        {
            string best = null;
            int bestCount = 0;
            foreach (var pair in LoadCards())
            {
                if (pair.Value > bestCount)
                {
                    best = pair.Key;
                    bestCount = pair.Value;
                }
            }
            return best;
        }

        // ---------- 內部 ----------

        private static void RollDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(DateKey, "") == today) return;
            PlayerPrefs.SetString(DateKey, today);
            PlayerPrefs.SetInt(TodayViewsKey, 0);
        }

        private static void Bump(string key)
        {
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
            PlayerPrefs.Save();
        }

        private static Dictionary<string, int> LoadCards()
        {
            var result = new Dictionary<string, int>();
            foreach (var entry in PlayerPrefs.GetString(CardsKey, "").Split(';'))
            {
                int eq = entry.IndexOf('=');
                if (eq <= 0) continue;
                if (int.TryParse(entry.Substring(eq + 1), out int n)) result[entry.Substring(0, eq)] = n;
            }
            return result;
        }

        private static void SaveCards(Dictionary<string, int> counts)
        {
            var parts = new List<string>();
            foreach (var pair in counts) parts.Add(pair.Key + "=" + pair.Value);
            PlayerPrefs.SetString(CardsKey, string.Join(";", parts));
            PlayerPrefs.Save();
        }
    }
}
