using System;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>首頁上的一個主題格子（動物 / 水果 / 交通工具）。名稱跟著語言設定走。</summary>
    public class DeckButton : MonoBehaviour
    {
        [SerializeField] private Image cover;
        [SerializeField] private Image panel;
        [SerializeField] private Text label;
        [SerializeField] private Button button;
        [SerializeField] private Pulse pulse;

        private CardDeck _deck;

        private void OnEnable() => AppSettings.Changed += RefreshLabel;
        private void OnDisable() => AppSettings.Changed -= RefreshLabel;

        public void Setup(CardDeck deck, Action<CardDeck> onChosen)
        {
            _deck = deck;
            RefreshLabel();

            if (cover != null)
            {
                // 沒有專門封面圖時，就用這組的第一張卡片當封面
                Sprite sprite = deck.cover;
                if (sprite == null)
                {
                    var playable = deck.PlayableCards();
                    if (playable.Count > 0) sprite = playable[0].image;
                }
                cover.sprite = sprite;
                cover.enabled = sprite != null;
            }

            if (panel != null) panel.color = deck.themeColor;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (pulse != null) pulse.Play();
                    if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
                    onChosen?.Invoke(deck);
                });
            }
        }

        private void RefreshLabel()
        {
            if (label != null && _deck != null) label.text = _deck.LocalizedName;
        }
    }
}
