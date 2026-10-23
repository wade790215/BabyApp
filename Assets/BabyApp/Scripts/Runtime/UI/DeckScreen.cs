using System;
using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 卡片畫面：一次只看一張卡。每張卡進場都被「？」蓋著，點開才看得到、才有聲音；
    /// 再點就是彈跳反應加重播。拖著卡片甩出去或按箭頭換卡，換卡的瞬間聲音就停。
    /// 左下角按住「錄」可以錄自己的聲音當這張卡的名稱；左上角「播放」是給家長用的自動翻頁。
    /// 刻意沒有計時、沒有分數、沒有答錯，2～3 歲只要「打開、看、聽、再一次」。
    /// </summary>
    public class DeckScreen : MonoBehaviour
    {
        [SerializeField] private CardView cardView;
        [SerializeField] private DraggableCard dragger;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RecordButton recordButton;
        [SerializeField] private Button autoplayButton;
        [SerializeField] private Image autoplayFace;
        [SerializeField] private Mascot mascot;

        [Tooltip("會跟著主題色變色的介面元件（箭頭按鈕、回家按鈕的底）")]
        [SerializeField] private Image[] themedAccents;

        [Tooltip("剛拖完卡片後這段時間內忽略點擊，避免拖曳尾端被當成點卡片")]
        [SerializeField] private float tapLockAfterDrag = 0.3f;

        [Tooltip("自動播放時每張卡停幾秒")]
        [SerializeField] private float autoplaySeconds = 4f;

        public event Action HomeRequested;

        private static readonly Color AutoplayOff = new Color32(0x7E, 0xD9, 0xA8, 0xFF);
        private static readonly Color AutoplayOn = new Color32(0xFF, 0x9F, 0x68, 0xFF);

        private readonly List<CardEntry> _cards = new List<CardEntry>();
        private CardDeck _deck;
        private int _index;
        private Coroutine _autoplay;

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(() => { StopAutoplay(); Flip(1); });
            if (nextButton != null) nextButton.onClick.AddListener(() => { StopAutoplay(); Flip(-1); });

            if (cardView != null)
            {
                cardView.Tapped += OnCardTapped;
                cardView.Revealed += OnRevealed;
            }

            if (dragger != null)
            {
                dragger.FlipStarted += StopVoice;
                dragger.SwipedToNext += AdvanceNext;
                dragger.SwipedToPrevious += AdvancePrevious;
            }

            if (recordButton != null)
            {
                recordButton.Started += () => { StopAutoplay(); StopVoice(); };
                recordButton.Recorded += OnRecorded;
            }

            if (autoplayButton != null) autoplayButton.onClick.AddListener(ToggleAutoplay);
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
        }

        private void OnDisable()
        {
            StopAutoplay();
            StopVoice();
        }

        public void Open(CardDeck deck)
        {
            _deck = deck;
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

        // ---------- 換卡 ----------

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

        /// <summary>換卡只是把蓋著的新卡放上來，聲音要等他自己把蓋子打開。</summary>
        private void Show()
        {
            StopVoice();
            var card = _cards[_index];
            if (cardView != null) cardView.Bind(card);
            UsageStats.CardViewed(card.id);
        }

        // ---------- 點卡 ----------

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

            StopAutoplay();

            if (cardView.IsCovered)
            {
                cardView.Reveal();
                return;
            }

            cardView.PlayReaction();
            if (VoiceService.Instance != null) VoiceService.Instance.PlaySparkle();
            if (mascot != null) mascot.React(Mascot.Pose.Happy, 1.0f);
            PlayCurrentCard();
        }

        private void OnRevealed()
        {
            if (mascot != null) mascot.React(Mascot.Pose.Surprised, 1.2f);
            PlayCurrentCard();
        }

        private void PlayCurrentCard()
        {
            if (_cards.Count == 0 || VoiceService.Instance == null) return;
            var card = _cards[_index];

            // 卡片自己沒有叫聲就用這組共用的（水果全部都是咬一口的咔滋）
            var sound = card.sound != null ? card.sound : (_deck != null ? _deck.fallbackSound : null);

            // 自己錄的聲音優先，沒錄過才用內建語音
            var voice = RecordingStore.TryGet(card.id);
            if (voice == null) voice = card.LocalizedVoice;

            VoiceService.Instance.PlayCard(sound, voice);
        }

        /// <summary>錄完就存起來並馬上播給他聽：汪汪！…（他自己的聲音）。</summary>
        private void OnRecorded(AudioClip clip)
        {
            if (_cards.Count == 0) return;
            var card = _cards[_index];
            RecordingStore.Save(card.id, clip);
            UsageStats.RecordingMade();
            if (mascot != null) mascot.React(Mascot.Pose.Happy, 1.4f);

            if (cardView != null && cardView.IsCovered) cardView.Reveal();
            else PlayCurrentCard();
        }

        // ---------- 自動播放（給家長用）----------

        private void ToggleAutoplay()
        {
            if (_autoplay != null) StopAutoplay();
            else
            {
                if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
                _autoplay = StartCoroutine(Autoplay());
                if (autoplayFace != null) autoplayFace.color = AutoplayOn;
            }
        }

        private void StopAutoplay()
        {
            if (_autoplay == null) return;
            StopCoroutine(_autoplay);
            _autoplay = null;
            if (autoplayFace != null) autoplayFace.color = AutoplayOff;
        }

        private IEnumerator Autoplay()
        {
            while (_cards.Count > 0)
            {
                if (cardView != null && cardView.IsCovered)
                {
                    yield return new WaitForSecondsRealtime(0.8f);
                    cardView.Reveal(); // Revealed 事件會播聲音
                }
                yield return new WaitForSecondsRealtime(autoplaySeconds);

                Flip(-1);
                // 等卡片飛完、新卡滑進來
                yield return new WaitForSecondsRealtime(0.1f);
                while (dragger != null && dragger.IsAnimating) yield return null;
            }
            _autoplay = null;
        }

        private static void StopVoice()
        {
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }
    }
}
