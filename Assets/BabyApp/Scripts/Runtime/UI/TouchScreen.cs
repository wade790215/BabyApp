using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 摸一摸會叫：整頁 6 格，點誰誰叫（叫聲 → 名稱）、跳一下。
    /// 不出題、不換頁、不等誰，最適合 2 歲半的「我按下去，世界有反應」。
    /// 一次都從同一組挑（動物一頁、車子一頁），點小熊換一頁。
    /// </summary>
    public class TouchScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private DraggableWidget mascotHandle;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RectTransform field;
        [SerializeField] private Button itemTemplate;

        [SerializeField] private int itemsPerPage = 6;

        public event System.Action HomeRequested;

        private struct Entry
        {
            public CardEntry Card;
            public AudioClip Sound;
        }

        private readonly List<Button> _items = new List<Button>();
        private readonly List<Entry> _page = new List<Entry>();
        private readonly List<CardDeck> _decks = new List<CardDeck>();
        private int _deckCursor = -1;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            if (mascotHandle != null) mascotHandle.Tapped += NextPage;
        }

        private void OnEnable()
        {
            BuildDeckList();
            NextPage();
        }

        private void OnDisable()
        {
            ClearItems();
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        /// <summary>只收有叫聲可以放的組：卡片自己有聲音，或整組有共用聲音。</summary>
        private void BuildDeckList()
        {
            _decks.Clear();
            foreach (var deck in DeckLibrary.All)
            {
                if (deck.deckId == "colors") continue;
                int usable = 0;
                foreach (var card in deck.cards)
                {
                    if (card != null && card.HasImage && (card.sound != null || deck.fallbackSound != null)) usable++;
                }
                if (usable >= 2) _decks.Add(deck);
            }
            // 沒有任何叫聲的話退回有圖就好，至少還能聽名稱
            if (_decks.Count == 0)
            {
                foreach (var deck in DeckLibrary.All)
                {
                    if (deck.deckId != "colors" && !deck.IsArtPending()) _decks.Add(deck);
                }
            }
        }

        private void NextPage()
        {
            if (_decks.Count == 0 || itemTemplate == null || field == null) return;

            _deckCursor = (_deckCursor + 1) % _decks.Count;
            var deck = _decks[_deckCursor];

            var pool = new List<Entry>();
            foreach (var card in deck.cards)
            {
                if (card == null || !card.HasImage) continue;
                pool.Add(new Entry { Card = card, Sound = card.sound != null ? card.sound : deck.fallbackSound });
            }
            Shuffle(pool);

            _page.Clear();
            for (int i = 0; i < pool.Count && i < itemsPerPage; i++) _page.Add(pool[i]);

            ClearItems();
            for (int i = 0; i < _page.Count; i++)
            {
                var item = Instantiate(itemTemplate, field);
                item.gameObject.SetActive(true);

                foreach (var img in item.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name != "Thumb") continue;
                    img.sprite = _page[i].Card.image;
                    img.enabled = img.sprite != null;
                    break;
                }

                int index = i;
                item.onClick.AddListener(() => OnItemTapped(index));
                _items.Add(item);
            }

            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();
            if (mascot != null) mascot.React(Mascot.Pose.Happy, 1.0f);
        }

        private void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _items.Clear();
        }

        private void OnItemTapped(int index)
        {
            if (index < 0 || index >= _page.Count) return;
            var entry = _page[index];

            var pulse = _items[index].GetComponent<Pulse>();
            if (pulse != null) pulse.Play();
            StartCoroutine(Wiggle((RectTransform)_items[index].transform));

            if (VoiceService.Instance != null)
            {
                var recorded = RecordingStore.TryGet(entry.Card.id);
                VoiceService.Instance.PlayCard(entry.Sound, recorded != null ? recorded : entry.Card.LocalizedVoice);
            }
            if (mascot != null) mascot.React(Mascot.Pose.Surprised, 0.9f, jump: false);
            UsageStats.CardViewed(entry.Card.id);
        }

        private static IEnumerator Wiggle(RectTransform rt)
        {
            const float duration = 0.4f;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 3f) * 7f * (1f - p));
                yield return null;
            }
            if (rt != null) rt.localRotation = Quaternion.identity;
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
