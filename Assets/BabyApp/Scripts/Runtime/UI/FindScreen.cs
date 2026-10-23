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
    /// 永遠只有兩個選項、選錯沒有懲罰（錯的那張晃一下、小熊說「再試試看」、題目重播一次），
    /// 選對就慶祝然後換下一題。沒有分數、沒有關卡。點小熊可以再聽一次題目。
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

        public event System.Action HomeRequested;

        private struct Entry
        {
            public CardEntry Card;
            public CardDeck Deck;
        }

        private readonly List<Entry> _pool = new List<Entry>();
        private Entry _target;
        private int _targetSlot;
        private bool _soundMode;
        private bool _busy;
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
        }

        private void OnEnable()
        {
            BuildPool();
            _busy = false;
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

            // 干擾項盡量從同一組挑，分辨「狗」和「貓」比分辨「狗」和「蘋果」更有學習價值
            var sameDeck = new List<Entry>();
            foreach (var e in _pool)
            {
                if (e.Deck == _target.Deck && e.Card != _target.Card) sameDeck.Add(e);
            }
            Entry other;
            if (sameDeck.Count > 0) other = sameDeck[Random.Range(0, sameDeck.Count)];
            else
            {
                do { other = _pool[Random.Range(0, _pool.Count)]; } while (other.Card == _target.Card);
            }

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
            }
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

            if (slot == _targetSlot)
            {
                _busy = true;
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
                // 錯的那張晃一下、暗一下，沒有任何「錯誤音」
                StartCoroutine(Wiggle(slot));
                if (mascot != null) mascot.React(Mascot.Pose.Surprised, 1.0f, jump: false);
                if (VoiceService.Instance != null && PromptLibrary.Instance != null)
                {
                    VoiceService.Instance.PlaySequence(PromptLibrary.Instance.TryAgain);
                }
                StartCoroutine(RepeatAfter(1.4f));
            }
        }

        private IEnumerator RepeatAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (!_busy) PlayPrompt();
        }

        private IEnumerator Wiggle(int slot)
        {
            var tile = choiceTiles != null && slot < choiceTiles.Length ? choiceTiles[slot] : null;
            var fill = choiceFills != null && slot < choiceFills.Length ? choiceFills[slot] : null;
            if (fill != null) fill.color = Color.Lerp(_fillColor, Color.gray, 0.35f);

            const float duration = 0.45f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                if (tile != null) tile.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 6f * (1f - p));
                yield return null;
            }
            if (tile != null) tile.localRotation = Quaternion.identity;

            yield return new WaitForSecondsRealtime(0.5f);
            if (fill != null) fill.color = _fillColor;
        }
    }
}
