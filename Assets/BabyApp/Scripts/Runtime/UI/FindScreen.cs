using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 找一找：小熊問「狗狗在哪裡？」（或先播汪汪再問「這是誰的聲音？」），兩張大圖選一張。
    /// 永遠只有兩個選項、選錯沒有懲罰也不說「再試試看」：錯的那張淡掉、對的那張發光，
    /// 小熊說「在這裡！狗狗！」等於幫他找到，然後換下一題。沒有分數、沒有關卡。點小熊可以再聽一次題目。
    /// 干擾項一開始都從同一組挑（狗 vs 貓）；找對幾次之後才偶爾混進別組的（狗 vs 蘋果）。
    /// </summary>
    public class FindScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private DraggableWidget mascotHandle;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RectTransform[] choiceTiles;
        [SerializeField] private Image[] choiceImages;
        [SerializeField] private Image[] choiceFills;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Pulse[] choicePulses;

        [Tooltip("答對後等幾秒換下一題")]
        [SerializeField] private float nextRoundDelay = 1.8f;

        [Tooltip("選錯被「幫忙找到」後等幾秒換下一題")]
        [SerializeField] private float helpedRoundDelay = 2.6f;

        [Tooltip("這次玩找對幾次之後，才開始偶爾出跨主題的干擾項")]
        [SerializeField] private int crossDeckAfter = 8;

        private static readonly Color Glow = new Color32(0xFF, 0xE8, 0xA3, 0xFF);

        public event System.Action HomeRequested;

        private struct Entry
        {
            public CardEntry Card;
            public CardDeck Deck;
        }

        private readonly List<Entry> _pool = new List<Entry>();
        private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
        private Entry _target;
        private int _targetSlot;
        private bool _soundMode;
        private bool _busy;
        private int _streak;
        private Color _fillColor = Color.white;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            if (mascotHandle != null) mascotHandle.Tapped += RepeatPrompt;

            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    int slot = i;
                    if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => OnChoice(slot));
                }
            }
            if (choiceFills != null && choiceFills.Length > 0 && choiceFills[0] != null) _fillColor = choiceFills[0].color;

            // 淡出用的 CanvasGroup，場景裡沒有就補
            if (choiceTiles != null)
            {
                foreach (var tile in choiceTiles)
                {
                    if (tile == null) { _groups.Add(null); continue; }
                    var group = tile.GetComponent<CanvasGroup>();
                    if (group == null) group = tile.gameObject.AddComponent<CanvasGroup>();
                    _groups.Add(group);
                }
            }
        }

        private void OnEnable()
        {
            BuildPool();
            _busy = false;
            _streak = 0;
            StartCoroutine(StartAfter(0.4f));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        private void BuildPool()
        {
            _pool.Clear();
            foreach (var deck in DeckLibrary.All)
            {
                foreach (var card in deck.cards)
                {
                    if (card != null && card.HasImage) _pool.Add(new Entry { Card = card, Deck = deck });
                }
            }
        }

        private IEnumerator StartAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            NewRound();
        }

        // ---------- 出題 ----------

        private void NewRound()
        {
            if (_pool.Count < 2)
            {
                Debug.LogWarning("[BabyApp] 找一找至少要兩張有圖的卡片。");
                return;
            }

            _target = _pool[Random.Range(0, _pool.Count)];

            // 干擾項盡量從同一組挑，分辨「狗」和「貓」比分辨「狗」和「蘋果」更有學習價值；
            // 找對夠多次之後才偶爾混進別組的，當作一點點變化
            var sameDeck = new List<Entry>();
            var otherDeck = new List<Entry>();
            foreach (var e in _pool)
            {
                if (e.Card == _target.Card) continue;
                if (e.Deck == _target.Deck) sameDeck.Add(e);
                else otherDeck.Add(e);
            }
            bool allowCross = _streak >= crossDeckAfter && otherDeck.Count > 0 && Random.value < 0.3f;
            var candidates = allowCross || sameDeck.Count == 0 ? otherDeck : sameDeck;
            if (candidates.Count == 0) candidates = sameDeck.Count > 0 ? sameDeck : otherDeck;
            var other = candidates[Random.Range(0, candidates.Count)];

            _targetSlot = Random.Range(0, 2);
            SetTile(_targetSlot, _target.Card);
            SetTile(1 - _targetSlot, other.Card);

            // 有叫聲的卡一半機率出「這是誰的聲音」
            _soundMode = TargetSound() != null && Random.value < 0.5f;

            _busy = false;
            if (mascot != null) mascot.React(Mascot.Pose.Think, 1.6f, jump: false);
            PlayPrompt();
        }

        private void SetTile(int slot, CardEntry card)
        {
            if (choiceImages != null && slot < choiceImages.Length && choiceImages[slot] != null)
            {
                choiceImages[slot].sprite = card.image;
                choiceImages[slot].enabled = card.image != null;
            }
            if (choiceFills != null && slot < choiceFills.Length && choiceFills[slot] != null)
            {
                choiceFills[slot].color = _fillColor;
            }
            if (choiceTiles != null && slot < choiceTiles.Length && choiceTiles[slot] != null)
            {
                choiceTiles[slot].localRotation = Quaternion.identity;
                choiceTiles[slot].localScale = Vector3.one;
            }
            if (slot < _groups.Count && _groups[slot] != null) _groups[slot].alpha = 1f;
        }

        private AudioClip TargetSound()
        {
            if (_target.Card == null) return null;
            if (_target.Card.sound != null) return _target.Card.sound;
            return _target.Deck != null ? _target.Deck.fallbackSound : null;
        }

        private AudioClip TargetName()
        {
            var recorded = RecordingStore.TryGet(_target.Card.id);
            return recorded != null ? recorded : _target.Card.LocalizedVoice;
        }

        private void PlayPrompt()
        {
            if (VoiceService.Instance == null || PromptLibrary.Instance == null || _target.Card == null) return;

            if (_soundMode)
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.WhoseSound, TargetSound());
            }
            else
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.WhereIs(TargetName()));
            }
        }

        /// <summary>點小熊：再聽一次題目。</summary>
        private void RepeatPrompt()
        {
            if (_busy) return;
            if (mascot != null) mascot.React(Mascot.Pose.Think, 1.2f);
            PlayPrompt();
        }

        // ---------- 作答 ----------

        private void OnChoice(int slot)
        {
            if (_busy || _target.Card == null) return;
            _busy = true;

            if (slot == _targetSlot)
            {
                _streak++;
                if (choicePulses != null && slot < choicePulses.Length && choicePulses[slot] != null) choicePulses[slot].Play();
                if (VoiceService.Instance != null)
                {
                    VoiceService.Instance.PlaySparkle();
                    VoiceService.Instance.PlaySequence(PromptLibrary.Instance != null ? PromptLibrary.Instance.Correct : null,
                        TargetSound(), TargetName());
                }
                if (mascot != null) mascot.React(Mascot.Pose.Happy, nextRoundDelay);
                UsageStats.FindAnsweredCorrectly();
                StartCoroutine(StartAfter(nextRoundDelay));
            }
            else
            {
                // 選錯不是錯：錯的那張悄悄淡掉，對的那張亮起來，小熊幫他指出來「在這裡！狗狗！」
                StartCoroutine(HelpFind(slot));
            }
        }

        private IEnumerator HelpFind(int wrongSlot)
        {
            var wrongGroup = wrongSlot < _groups.Count ? _groups[wrongSlot] : null;
            var rightTile = choiceTiles != null && _targetSlot < choiceTiles.Length ? choiceTiles[_targetSlot] : null;
            var rightFill = choiceFills != null && _targetSlot < choiceFills.Length ? choiceFills[_targetSlot] : null;

            if (mascot != null) mascot.React(Mascot.Pose.Surprised, 0.8f, jump: false);

            // 錯的淡掉
            const float fade = 0.35f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                if (wrongGroup != null) wrongGroup.alpha = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(t / fade));
                yield return null;
            }

            // 對的發光、輕輕跳兩下，同時說「在這裡！」+ 名稱
            if (rightFill != null) rightFill.color = Glow;
            if (VoiceService.Instance != null && PromptLibrary.Instance != null)
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.Here, TargetSound(), TargetName());
            }
            if (mascot != null) mascot.React(Mascot.Pose.Happy, helpedRoundDelay);

            const float bounce = 0.9f;
            t = 0f;
            while (t < bounce)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / bounce);
                if (rightTile != null) rightTile.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2f)));
                yield return null;
            }
            if (rightTile != null) rightTile.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, helpedRoundDelay - fade - bounce));
            NewRound();
        }
    }
}
