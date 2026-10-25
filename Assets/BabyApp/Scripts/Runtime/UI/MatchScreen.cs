using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 配對：左邊一張大卡當「樣子」，右邊三張選一張一樣的。小熊問「一樣的在哪裡？」。
    /// 跟找一找一樣沒有錯：選到不一樣的，那張淡掉、對的亮起來、小熊說「在這裡！」然後換題。
    /// 干擾項從同一組挑，分辨「狗 vs 貓」才有意思。
    /// </summary>
    public class MatchScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private DraggableWidget mascotHandle;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private Image sampleImage;
        [SerializeField] private RectTransform sampleTile;
        [SerializeField] private RectTransform[] choiceTiles;
        [SerializeField] private Image[] choiceImages;
        [SerializeField] private Image[] choiceFills;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Pulse[] choicePulses;

        [SerializeField] private float nextRoundDelay = 1.8f;
        [SerializeField] private float helpedRoundDelay = 2.6f;

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
            int slots = choiceTiles != null ? choiceTiles.Length : 0;
            if (_pool.Count < slots || slots == 0)
            {
                Debug.LogWarning("[BabyApp] 配對至少要三張有圖的卡片。");
                return;
            }

            _target = _pool[Random.Range(0, _pool.Count)];

            var sameDeck = new List<Entry>();
            var others = new List<Entry>();
            foreach (var e in _pool)
            {
                if (e.Card == _target.Card) continue;
                if (e.Deck == _target.Deck) sameDeck.Add(e);
                else others.Add(e);
            }
            Shuffle(sameDeck);
            Shuffle(others);

            // 干擾項先從同一組拿，不夠再從別組補
            var distractors = new List<Entry>();
            foreach (var e in sameDeck) { if (distractors.Count < slots - 1) distractors.Add(e); }
            foreach (var e in others) { if (distractors.Count < slots - 1) distractors.Add(e); }

            _targetSlot = Random.Range(0, slots);
            int d = 0;
            for (int i = 0; i < slots; i++)
            {
                SetTile(i, i == _targetSlot ? _target.Card : distractors[d++].Card);
            }

            if (sampleImage != null)
            {
                sampleImage.sprite = _target.Card.image;
                sampleImage.enabled = sampleImage.sprite != null;
            }
            if (sampleTile != null) sampleTile.localScale = Vector3.one;

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
            if (choiceFills != null && slot < choiceFills.Length && choiceFills[slot] != null) choiceFills[slot].color = _fillColor;
            if (choiceTiles != null && slot < choiceTiles.Length && choiceTiles[slot] != null) choiceTiles[slot].localScale = Vector3.one;
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
            VoiceService.Instance.PlaySequence(TargetName(), PromptLibrary.Instance.Same);
            StartCoroutine(BounceSample());
        }

        private void RepeatPrompt()
        {
            if (_busy) return;
            if (mascot != null) mascot.React(Mascot.Pose.Think, 1.2f);
            PlayPrompt();
        }

        private IEnumerator BounceSample()
        {
            const float duration = 0.5f;
            float t = 0f;
            while (t < duration && sampleTile != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                sampleTile.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            if (sampleTile != null) sampleTile.localScale = Vector3.one;
        }

        // ---------- 作答 ----------

        private void OnChoice(int slot)
        {
            if (_busy || _target.Card == null) return;
            _busy = true;

            if (slot == _targetSlot)
            {
                if (choicePulses != null && slot < choicePulses.Length && choicePulses[slot] != null) choicePulses[slot].Play();
                StartCoroutine(BounceSample());
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
                StartCoroutine(HelpFind(slot));
            }
        }

        private IEnumerator HelpFind(int wrongSlot)
        {
            var wrongGroup = wrongSlot < _groups.Count ? _groups[wrongSlot] : null;
            var rightTile = choiceTiles != null && _targetSlot < choiceTiles.Length ? choiceTiles[_targetSlot] : null;
            var rightFill = choiceFills != null && _targetSlot < choiceFills.Length ? choiceFills[_targetSlot] : null;

            if (mascot != null) mascot.React(Mascot.Pose.Surprised, 0.8f, jump: false);

            const float fade = 0.35f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                if (wrongGroup != null) wrongGroup.alpha = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(t / fade));
                yield return null;
            }

            if (rightFill != null) rightFill.color = Glow;
            if (VoiceService.Instance != null && PromptLibrary.Instance != null)
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.Here, TargetName());
            }
            if (mascot != null) mascot.React(Mascot.Pose.Happy, helpedRoundDelay);

            const float bounce = 0.9f;
            t = 0f;
            while (t < bounce)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / bounce);
                float s = 1f + 0.08f * Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2f));
                if (rightTile != null) rightTile.localScale = Vector3.one * s;
                if (sampleTile != null) sampleTile.localScale = Vector3.one * s;
                yield return null;
            }
            if (rightTile != null) rightTile.localScale = Vector3.one;
            if (sampleTile != null) sampleTile.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, helpedRoundDelay - fade - bounce));
            NewRound();
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
