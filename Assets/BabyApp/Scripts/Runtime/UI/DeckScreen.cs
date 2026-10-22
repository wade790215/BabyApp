using System;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 卡片畫面：一次只看一張卡。每張卡進場都被工地門蓋著，點開才看得到、才有聲音；
    /// 再點就是彈跳反應加重播。拖著卡片甩出去或按箭頭換卡。
    /// 刻意沒有計時、沒有分數、沒有答錯，2～3 歲只要「打開、看、聽、再一次」。
    /// </summary>
    public class DeckScreen : MonoBehaviour
    {
        [SerializeField] private CardView cardView;
        [SerializeField] private DraggableCard dragger;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private HoldButton homeButton;

        [Tooltip("會跟著主題色變色的介面元件（箭頭按鈕、回家按鈕的底）")]
        [SerializeField] private Image[] themedAccents;

        [Tooltip("剛拖完卡片後這段時間內忽略點擊，避免拖曳尾端被當成點卡片")]
        [SerializeField] private float tapLockAfterDrag = 0.3f;

        public event Action HomeRequested;

        private readonly List<CardEntry> _cards = new List<CardEntry>();
        private int _index;

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(() => Flip(1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Flip(-1));

            if (cardView != null)
            {
                cardView.Tapped += OnCardTapped;
                cardView.Revealed += PlayCurrentCard;
            }

            if (dragger != null)
            {
                dragger.SwipedToNext += AdvanceNext;
                dragger.SwipedToPrevious += AdvancePrevious;
            }

            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
        }

        public void Open(CardDeck deck)
        {
            _cards.Clear();
            _cards.AddRange(deck.PlayableCards());

            // 背景跟著設定走，主題色只打在按鈕上，每個主題一眼就不一樣又不會糊成一片
            if (themedAccents != null)
            {
                foreach (var accent in themedAccents)
                {
                    if (accent != null) accent.color = deck.themeColor;
                }
            }

            _index = 0;
            if (_cards.Count == 0)
            {
                Debug.LogWarning($"[BabyApp] 卡片組「{deck.displayName}」沒有可用的卡片。");
                return;
            }
            Show();
        }

        /// <summary>按鈕換卡：卡片飛出去再滑新的進來，跟用拖的一樣。</summary>
        private void Flip(int direction)
        {
            if (_cards.Count == 0) return;

            if (dragger != null)
            {
                dragger.Flip(direction);
                return;
            }

            // 沒接拖曳元件就直接換
            if (direction < 0) AdvanceNext();
            else AdvancePrevious();
        }

        private void AdvanceNext()
        {
            if (_cards.Count == 0) return;
            _index = (_index + 1) % _cards.Count;
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();
            Show();
        }

        private void AdvancePrevious()
        {
            if (_cards.Count == 0) return;
            _index = (_index - 1 + _cards.Count) % _cards.Count;
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();
            Show();
        }

        /// <summary>換卡只是把關著門的新卡放上來，聲音要等他自己把門打開。</summary>
        private void Show()
        {
            if (cardView != null) cardView.Bind(_cards[_index]);
        }

        private void OnCardTapped()
        {
            if (_cards.Count == 0 || cardView == null) return;

            if (dragger != null)
            {
                if (dragger.IsAnimating) return;
                bool justDragged = dragger.LastDragMoved &&
                                   Time.unscaledTime - dragger.LastDragEndTime < tapLockAfterDrag;
                if (justDragged) return;
            }

            if (cardView.IsCovered)
            {
                cardView.Reveal();
                return;
            }

            cardView.PlayReaction();
            if (VoiceService.Instance != null) VoiceService.Instance.PlaySparkle();
            PlayCurrentCard();
        }

        private void PlayCurrentCard()
        {
            if (_cards.Count == 0 || VoiceService.Instance == null) return;
            var card = _cards[_index];
            VoiceService.Instance.PlayCard(card.sound, card.LocalizedVoice);
        }
    }
}
