using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Data
{
    /// <summary>
    /// 一組卡片（一個學習主題），例如「動物」。
    /// 資產放在 Assets/BabyApp/Resources/Decks 之下，執行時用 Resources 一次載入。
    /// </summary>
    [CreateAssetMenu(fileName = "Deck_", menuName = "BabyApp/卡片組 (CardDeck)")]
    public class CardDeck : ScriptableObject
    {
        [Tooltip("唯一代號，例如 animals")]
        public string deckId;

        [Tooltip("首頁顯示的主題名稱，例如 動物")]
        public string displayName;

        [Tooltip("英文主題名稱，例如 Animals")]
        public string displayNameEn;

        /// <summary>依目前語言的主題名稱；英文缺的話退回中文。</summary>
        public string LocalizedName =>
            Core.AppSettings.IsEnglish && !string.IsNullOrEmpty(displayNameEn) ? displayNameEn : displayName;

        [Tooltip("首頁大按鈕上的封面圖")]
        public Sprite cover;

        [Tooltip("這個主題的主題色，用在背景與按鈕")]
        public Color themeColor = new Color(1f, 0.85f, 0.4f);

        [Tooltip("首頁按鈕順序，數字小的排前面")]
        public int sortOrder;

        public List<CardEntry> cards = new List<CardEntry>();

        /// <summary>
        /// 有圖的卡片優先：一旦這組有任何一張圖，就只顯示有圖的卡，
        /// 避免補素材的過程中混進沒圖的空白卡。
        ///
        /// 整組都還沒有圖時則退回純文字卡，這樣在生圖之前就能先玩過整個流程，
        /// 而不是開起來看到一片空白。
        /// </summary>
        public List<CardEntry> PlayableCards()
        {
            var named = new List<CardEntry>();
            var withImage = new List<CardEntry>();

            foreach (var card in cards)
            {
                if (card == null || !card.IsValid) continue;
                named.Add(card);
                if (card.HasImage) withImage.Add(card);
            }

            return withImage.Count > 0 ? withImage : named;
        }

        /// <summary>這組是不是還在「沒有任何圖」的預覽狀態。</summary>
        public bool IsArtPending()
        {
            foreach (var card in cards)
            {
                if (card != null && card.HasImage) return false;
            }
            return true;
        }
    }
}
