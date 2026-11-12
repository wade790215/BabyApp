using System;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 家長用的設定頁：三個開關（音效、語音、顯示文字）、語言切換、六個背景色塊、清除錄音，
    /// 還有一行小統計（今天翻了幾張、最愛哪張、錄了幾段、找對幾次、數了幾回）。
    /// 改了就立刻存起來，不需要「儲存」按鈕。
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private ToggleRow sfxToggle;
        [SerializeField] private ToggleRow voiceToggle;
        [SerializeField] private ToggleRow labelsToggle;
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageLabel;
        [SerializeField] private Button clearRecordingsButton;
        [SerializeField] private Button[] backgroundButtons;
        [SerializeField] private Image[] backgroundRings;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Button familyButton;

        public event Action CloseRequested;

        /// <summary>家長要去編輯家人相片卡。</summary>
        public event Action FamilyRequested;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());

            if (familyButton != null)
            {
                familyButton.onClick.AddListener(() => { Tap(); FamilyRequested?.Invoke(); });
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL 沒有相簿挑圖也沒有麥克風，整個功能用不了，乾脆不露出入口
                familyButton.gameObject.SetActive(false);
#endif
            }

            if (sfxToggle != null) sfxToggle.Changed += v => { AppSettings.SfxEnabled = v; Tap(); };
            if (voiceToggle != null) voiceToggle.Changed += v => { AppSettings.VoiceEnabled = v; Tap(); };
            if (labelsToggle != null) labelsToggle.Changed += v => { AppSettings.ShowLabels = v; Tap(); };

            if (languageButton != null)
            {
                languageButton.onClick.AddListener(() =>
                {
                    AppSettings.IsEnglish = !AppSettings.IsEnglish;
                    Tap();
                });
            }

            if (clearRecordingsButton != null)
            {
                clearRecordingsButton.onClick.AddListener(() =>
                {
                    RecordingStore.DeleteAll();
                    Tap();
                    Refresh();
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
            if (labelsToggle != null) labelsToggle.Set(AppSettings.ShowLabels, notify: false);
            if (languageLabel != null) languageLabel.text = Strings.Get("language_name");

            if (backgroundRings != null)
            {
                int selected = AppSettings.BackgroundIndex;
                for (int i = 0; i < backgroundRings.Length; i++)
                {
                    if (backgroundRings[i] != null) backgroundRings[i].enabled = i == selected;
                }
            }

            if (statsLabel != null)
            {
                statsLabel.text = string.Format(Strings.Get("stats"),
                    UsageStats.TodayViews, TopCardName(), UsageStats.Recordings,
                    UsageStats.FindCorrect, UsageStats.CountRounds);
            }
        }

        private static string TopCardName()
        {
            string id = UsageStats.TopCardId();
            if (string.IsNullOrEmpty(id)) return Strings.Get("none");
            foreach (var deck in DeckLibrary.All)
            {
                foreach (var card in deck.cards)
                {
                    if (card != null && card.id == id) return card.LocalizedName;
                }
            }
            // 最愛的可能是家人卡（不在 DeckLibrary 裡）
            var family = FamilyLibrary.Instance != null ? FamilyLibrary.Instance.Deck : null;
            if (family != null)
            {
                foreach (var card in family.cards)
                {
                    if (card != null && card.id == id) return card.LocalizedName;
                }
            }
            return Strings.Get("none");
        }

        private static void Tap()
        {
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }
    }
}
