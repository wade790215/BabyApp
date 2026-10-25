using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 數數：畫面上排 1～5 個一樣的東西，點一個念一個數（一、二、三…），那個東西就飛進小熊嘴裡，
    /// 咔滋一聲；點完全部小熊說「全部數完了！」。
    /// 「點一下念一個數」是 2～3 歲最自然的數數方式；點過的飛走了，看得出「這個算過了」。
    /// 點小熊可以再聽一次這是什麼。
    /// </summary>
    public class CountScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private DraggableWidget mascotHandle;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RectTransform field;
        [SerializeField] private Button itemTemplate;
        [SerializeField] private Text numberLabel;

        [SerializeField] private int maxCount = 5;
        [SerializeField] private float nextRoundDelay = 2.2f;
        [Tooltip("東西飛進小熊嘴裡要幾秒")]
        [SerializeField] private float flySeconds = 0.45f;

        public event System.Action HomeRequested;

        private readonly List<CardEntry> _pool = new List<CardEntry>();
        private readonly List<Button> _items = new List<Button>();
        private readonly List<bool> _counted = new List<bool>();
        private CardEntry _card;
        private int _total;
        private int _done;
        private bool _busy;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            if (mascotHandle != null) mascotHandle.Tapped += SayName;
        }

        private void OnEnable()
        {
            BuildPool();
            StartCoroutine(StartAfter(0.4f));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearItems();
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        private void BuildPool()
        {
            _pool.Clear();
            foreach (var deck in DeckLibrary.All)
            {
                // 顏色那組不適合拿來數，跳過
                if (deck.deckId == "colors") continue;
                foreach (var card in deck.cards)
                {
                    if (card != null && card.HasImage) _pool.Add(card);
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
            if (_pool.Count == 0 || itemTemplate == null || field == null) return;

            ClearItems();
            _card = _pool[Random.Range(0, _pool.Count)];
            _total = Random.Range(1, maxCount + 1);
            _done = 0;
            _busy = false;
            if (numberLabel != null) numberLabel.text = string.Empty;

            // 一排排開，數量多就小一點
            float width = field.rect.width;
            float size = Mathf.Min(field.rect.height * 0.8f, width / (_total + 0.6f));
            float spacing = size * 1.12f;
            float startX = -(_total - 1) * spacing * 0.5f;

            for (int i = 0; i < _total; i++)
            {
                var item = Instantiate(itemTemplate, field);
                item.gameObject.SetActive(true);
                var rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(startX + spacing * i, 0f);

                var image = item.GetComponentInChildren<Image>(true);
                foreach (var img in item.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name == "Thumb") { image = img; break; }
                }
                if (image != null)
                {
                    image.sprite = _card.image;
                    image.enabled = _card.image != null;
                }

                int index = i;
                item.onClick.AddListener(() => OnItemTapped(index));
                _items.Add(item);
                _counted.Add(false);
            }

            if (mascot != null) mascot.React(Mascot.Pose.Think, 1.2f, jump: false);
            SayName();
        }

        private void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _items.Clear();
            _counted.Clear();
        }

        private void SayName()
        {
            if (_card == null || VoiceService.Instance == null) return;
            var recorded = RecordingStore.TryGet(_card.id);
            VoiceService.Instance.PlaySequence(recorded != null ? recorded : _card.LocalizedVoice);
        }

        // ---------- 數 ----------

        private void OnItemTapped(int index)
        {
            if (_busy || index < 0 || index >= _counted.Count || _counted[index]) return;
            _counted[index] = true;
            _done++;

            var pulse = _items[index].GetComponent<Pulse>();
            if (pulse != null) pulse.Play();

            // 飛進小熊嘴裡：餵牠，不是只是「算過了」
            StartCoroutine(FeedBear((RectTransform)_items[index].transform));

            if (numberLabel != null) numberLabel.text = _done.ToString();

            if (VoiceService.Instance != null)
            {
                VoiceService.Instance.PlaySparkle();
                var number = PromptLibrary.Instance != null ? PromptLibrary.Instance.Number(_done) : null;

                if (_done >= _total)
                {
                    _busy = true;
                    VoiceService.Instance.PlaySequence(number,
                        PromptLibrary.Instance != null ? PromptLibrary.Instance.Yummy : null,
                        PromptLibrary.Instance != null ? PromptLibrary.Instance.AllCounted : null);
                    if (mascot != null) mascot.React(Mascot.Pose.Happy, nextRoundDelay);
                    UsageStats.CountRoundFinished();
                    StartCoroutine(StartAfter(nextRoundDelay));
                }
                else
                {
                    VoiceService.Instance.PlaySequence(number);
                }
            }
        }

        /// <summary>東西縮小著飛到小熊那裡，到了就咔滋一聲、小熊開心一下，東西消失。</summary>
        private IEnumerator FeedBear(RectTransform item)
        {
            if (item == null) yield break;
            item.SetAsLastSibling();
            var button = item.GetComponent<Button>();
            if (button != null) button.interactable = false;

            var from = item.anchoredPosition;
            var fromScale = item.localScale;
            // 小熊可能被拖到別處，每次都重新算牠在欄位裡的位置
            Vector2 to = from + Vector2.down * 300f;
            if (mascot != null && field != null)
            {
                var local = field.InverseTransformPoint(mascot.transform.position);
                to = new Vector2(local.x, local.y);
            }

            float t = 0f;
            while (t < flySeconds && item != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / flySeconds);
                float eased = p * p;
                // 弧線：先往上拋一點再落到嘴邊
                var pos = Vector2.Lerp(from, to, eased) + Vector2.up * (Mathf.Sin(p * Mathf.PI) * 80f);
                item.anchoredPosition = pos;
                item.localScale = fromScale * Mathf.Lerp(1f, 0.15f, eased);
                yield return null;
            }

            if (item != null) item.gameObject.SetActive(false);
            if (VoiceService.Instance != null) VoiceService.Instance.PlayChew();
            if (mascot != null && !_busy) mascot.React(Mascot.Pose.Happy, 0.7f);
        }
    }
}
