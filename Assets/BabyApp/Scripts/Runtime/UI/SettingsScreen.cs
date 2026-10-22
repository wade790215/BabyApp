using System;
using BabyApp.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 家長用的設定頁：三個開關（音效、語音、音樂）、語言切換、五個背景色塊。
    /// 改了就立刻存起來，不需要「儲存」按鈕。
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private ToggleRow sfxToggle;
        [SerializeField] private ToggleRow voiceToggle;
        [SerializeField] private ToggleRow musicToggle;
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageLabel;
        [SerializeField] private Button[] backgroundButtons;
        [SerializeField] private Image[] backgroundRings;

        public event Action CloseRequested;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());

            if (sfxToggle != null) sfxToggle.Changed += v => { AppSettings.SfxEnabled = v; Tap(); };
            if (voiceToggle != null) voiceToggle.Changed += v => { AppSettings.VoiceEnabled = v; Tap(); };
            if (musicToggle != null) musicToggle.Changed += v => { AppSettings.MusicEnabled = v; Tap(); };

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(() =>
                {
                    AppSettings.IsEnglish = !AppSettings.IsEnglish;
                    Tap();
                });
            }

            if (backgroundButtons != null)
            {
                for (int i = 0; i < backgroundButtons.Length; i++)
                {
                    int index = i;
                    if (backgroundButtons[i] != null)
                    {
                        backgroundButtons[i].onClick.AddListener(() =>
                        {
                            AppSettings.BackgroundIndex = index;
                            Tap();
                        });
                    }
                }
            }
        }

        private void OnEnable()
        {
            Refresh();
            AppSettings.Changed += Refresh;
        }

        private void OnDisable() => AppSettings.Changed -= Refresh;

        private void Refresh()
        {
            if (sfxToggle != null) sfxToggle.Set(AppSettings.SfxEnabled, notify: false);
            if (voiceToggle != null) voiceToggle.Set(AppSettings.VoiceEnabled, notify: false);
            if (musicToggle != null) musicToggle.Set(AppSettings.MusicEnabled, notify: false);
            if (languageLabel != null) languageLabel.text = Strings.Get("language_name");

            if (backgroundRings == null) return;
            int selected = AppSettings.BackgroundIndex;
            for (int i = 0; i < backgroundRings.Length; i++)
            {
                if (backgroundRings[i] != null) backgroundRings[i].enabled = i == selected;
            }
        }

        private static void Tap()
        {
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }
    }
}
