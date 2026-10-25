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
    /// 卡片畫面：一次只看一張卡。進一個新主題的前幾張被「？」蓋著，點開才看得到、才有聲音；
    /// 同一組看熟了（開過幾張）之後蓋子就不再出現，換卡直接看圖聽聲音，不會每張都要多點一下。
    /// 再點就是彈跳反應加重播。拖著卡片甩出去或按箭頭換卡，換卡的瞬間聲音就停；
    /// 滑過這組最後一張會自動接到下一個主題，小孩不用回首頁選。
    /// 左下角按住「錄」可以錄自己的聲音當這張卡的名稱；「播放」是給家長用的自動翻頁。
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

        [Tooltip("自動播放時，聲音播完後再停幾秒才翻頁")]
        [SerializeField] private float autoplayPauseSeconds = 1.5f;

        [Tooltip("同一組打開幾張蓋子之後，後面的卡就不再蓋")]
        [SerializeField] private int coveredCardsPerDeck = 3;

        [Tooltip("滑到最後一張再往後翻，要不要自動接下一個主題")]
        [SerializeField] private bool chainDecks = true;

        public event Action HomeRequested;

        private static readonly Color AutoplayOff = new Color32(0x7E, 0xD9, 0xA8, 0xFF);
        private static readonly Color AutoplayOn = new Color32(0xFF, 0x9F, 0x68, 0xFF);

        private readonly List<CardEntry> _cards = new List<CardEntry>();
        private CardDeck _deck;
        private int _index;
        private int _reveals;
        private Coroutine _autoplay;
        private Coroutine _delayedPlay;

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
            if (_delayedPlay != null) { StopCoroutine(_delayedPlay); _delayedPlay = null; }
        }

        public void Open(CardDeck deck) => Open(deck, startAtEnd: false);

        private void Open(CardDeck deck, bool startAtEnd)
        {
            _deck = deck;
            _cards.Clear();
            _cards.AddRange(deck.PlayableCards());
            _reveals = 0; // 新主題：蓋子重新出現

            // 背景跟著設定走，主題色只打在按鈕上，每個主題一眼就不一樣又不會糊成一片
            if (themedAccents != null)
            {
                foreach (var accent in themedAccents)
                {
                    if (accent != null) accent.color = deck.themeColor;
                }
            }

            if (_cards.Count == 0)
            {
                _index = 0;
                Debug.LogWarning($"[BabyApp] 卡片組「{deck.displayName}」沒有可用的卡片。");
                return;
            }
            _index = startAtEnd ? _cards.Count - 1 : 0;
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
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();

            if (_index + 1 >= _cards.Count && chainDecks && TryOpenNeighbourDeck(1)) return;

            _index = (_index + 1) % _cards.Count;
            Show();
        }

        private void AdvancePrevious()
        {
            if (_cards.Count == 0) return;
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();

            if (_index == 0 && chainDecks && TryOpenNeighbourDeck(-1)) return;

            _index = (_index - 1 + _cards.Count) % _cards.Count;
            Show();
        }

        /// <summary>翻過頭就接到隔壁的主題。只有一個主題時回 false，照舊繞圈。</summary>
        private bool TryOpenNeighbourDeck(int step)
        {
            var decks = DeckLibrary.All;
            if (_deck == null || decks.Count < 2) return false;

            int at = decks.IndexOf(_deck);
            if (at < 0) return false;
            var next = decks[(at + step + decks.Count) % decks.Count];

            // 往回翻是從上一組的最後一張接起
            Open(next, startAtEnd: step < 0);
            if (mascot != null) mascot.React(Mascot.Pose.Happy, 1.0f);
            return true;
        }

        /// <summary>
        /// 把目前這張卡放上來。前幾張蓋著，聲音要等他自己把蓋子打開；
        /// 看熟了之後直接露出來，等卡片滑進來就自己播。
        /// </summary>
        private void Show()
        {
            StopVoice();
            if (_delayedPlay != null) { StopCoroutine(_delayedPlay); _delayedPlay = null; }

            var card = _cards[_index];
            bool covered = _reveals < coveredCardsPerDeck;
            if (cardView != null) cardView.Bind(card, covered);
            UsageStats.CardViewed(card.id);

            if (!covered) _delayedPlay = StartCoroutine(PlayAfterSlideIn());
        }

        private IEnumerator PlayAfterSlideIn()
        {
            yield return new WaitForSecondsRealtime(0.15f);
            while (dragger != null && dragger.IsAnimating) yield return null;
            _delayedPlay = null;
            PlayCurrentCard();
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
            _reveals++;
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

        /// <summary>每張：掀開（如果蓋著）→ 等聲音播完 → 停一下 → 翻下一張。聲音長短不一，不用猜秒數。</summary>
        private IEnumerator Autoplay()
        {
            while (_cards.Count > 0)
            {
                if (cardView != null && cardView.IsCovered)
                {
                    yield return new WaitForSecondsRealtime(0.8f);
                    cardView.Reveal(); // Revealed 事件會播聲音
                }
                else
                {
                    // 沒蓋著的卡是滑進來後自己播的，等它開始
                    yield return new WaitForSecondsRealtime(0.3f);
                }

                float waited = 0f;
                while (VoiceService.Instance != null && VoiceService.Instance.IsSpeaking && waited < 8f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(autoplayPauseSeconds);

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
