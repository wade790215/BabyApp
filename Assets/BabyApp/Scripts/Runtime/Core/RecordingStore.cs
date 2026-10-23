using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 自己錄的卡片語音，存在裝置上（persistentDataPath/recordings），中英文各一份。
    /// 有錄過的卡片會優先用錄音，沒有的才用內建語音。
    /// </summary>
    public static class RecordingStore
    {
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        private static string Folder => Path.Combine(Application.persistentDataPath, "recordings");

        private static string Key(string cardId) => cardId + (AppSettings.IsEnglish ? "_en" : "_zh");

        private static string PathFor(string cardId) => Path.Combine(Folder, Key(cardId) + ".wav");

        /// <summary>這張卡在目前語言下有沒有錄音；有就回傳，沒有回 null。</summary>
        public static AudioClip TryGet(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            string key = Key(cardId);
            if (Cache.TryGetValue(key, out var cached)) return cached;

            string path = PathFor(cardId);
            var clip = File.Exists(path) ? WavFile.Load(path, key) : null;
            Cache[key] = clip;
            return clip;
        }

        public static void Save(string cardId, AudioClip clip)
        {
            if (string.IsNullOrEmpty(cardId) || clip == null) return;
            Directory.CreateDirectory(Folder);
            WavFile.Save(PathFor(cardId), clip);
            Cache[Key(cardId)] = clip;
        }

        public static void DeleteAll()
        {
            Cache.Clear();
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        }
    }
}
