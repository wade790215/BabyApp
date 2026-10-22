using System;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 家長可以調的設定，存在裝置上（PlayerPrefs）。
    /// 改了會發 Changed，背景、文字與播音那邊自己去更新，不用誰去通知誰。
    /// </summary>
    public static class AppSettings
    {
        public readonly struct BackgroundOption
        {
            public readonly string Name;
            public readonly Color Color;

            public BackgroundOption(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        /// <summary>可選的背景色，都是淡粉彩，白邊方塊放上去才會浮出來。第一個是預設。</summary>
        public static readonly BackgroundOption[] Backgrounds =
        {
            new BackgroundOption("淡藍", new Color32(0xBE, 0xE7, 0xFF, 0xFF)),
            new BackgroundOption("薄荷", new Color32(0xC9, 0xF2, 0xD9, 0xFF)),
            new BackgroundOption("奶油", new Color32(0xFF, 0xF3, 0xB0, 0xFF)),
            new BackgroundOption("櫻花", new Color32(0xFF, 0xD6, 0xE7, 0xFF)),
            new BackgroundOption("薰衣草", new Color32(0xE3, 0xD7, 0xFF, 0xFF))
        };

        private const string SfxKey = "babyapp.sfx";
        private const string VoiceKey = "babyapp.voice";
        private const string MusicKey = "babyapp.music";
        private const string BackgroundKey = "babyapp.background";
        private const string LanguageKey = "babyapp.language";

        public static event Action Changed;

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set => Set(SfxKey, value ? 1 : 0);
        }

        public static bool VoiceEnabled
        {
            get => PlayerPrefs.GetInt(VoiceKey, 1) == 1;
            set => Set(VoiceKey, value ? 1 : 0);
        }

        /// <summary>預設關：背景音樂會蓋掉名稱語音，這個年紀語音比音樂重要。</summary>
        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt(MusicKey, 0) == 1;
            set => Set(MusicKey, value ? 1 : 0);
        }

        public static int BackgroundIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(BackgroundKey, 0), 0, Backgrounds.Length - 1);
            set => Set(BackgroundKey, Mathf.Clamp(value, 0, Backgrounds.Length - 1));
        }

        public static Color BackgroundColor => Backgrounds[BackgroundIndex].Color;

        /// <summary>false = 中文（預設），true = English。</summary>
        public static bool IsEnglish
        {
            get => PlayerPrefs.GetInt(LanguageKey, 0) == 1;
            set => Set(LanguageKey, value ? 1 : 0);
        }

        private static void Set(string key, int value)
        {
            if (PlayerPrefs.GetInt(key, int.MinValue) == value) return;
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
