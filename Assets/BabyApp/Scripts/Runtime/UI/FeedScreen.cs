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
    /// 餵小熊：小熊大大地在左邊，右邊三個盤子各放一樣水果。點一下水果，或把它拖到小熊身上，
    /// 水果就飛進嘴裡——咔滋、小熊嚼一嚼、念出名稱、「好好吃！」，盤子再冒出新的。
    /// 只有一步，沒有切、煮（多步驟拖曳對兩歲太難）。拖偏了水果滑回盤子，不出聲、不算錯。
    /// 吃了幾口之後小熊說「我吃飽了！」坐下來拍拍肚子，然後又可以繼續餵：是收尾，不是關卡。
    /// </summary>
    public class FeedScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private Button mascotButton;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RectTransform field;
        [SerializeField] private FoodItem foodTemplate;
        [SerializeField] private RectTransform[] slots;

        [Tooltip("哪幾組卡片可以拿來餵；不管總共有幾組主題，只挑這裡列的")]
        [SerializeField] private string[] foodDeckIds = { "fruits" };

        [Tooltip("吃幾口之後小熊說吃飽了；0 = 不做這個收尾")]
        [SerializeField] private int fullAfter = 5;

        [Tooltip("點一下：水果飛進嘴裡要幾秒")]
        [SerializeField] private float flySeconds = 0.45f;

        [Tooltip("拖到小熊身上放手：已經在嘴邊了，很快吸進去")]
        [SerializeField] private float dropFlySeconds = 0.18f;

        [SerializeField] private float refillDelay = 0.5f;
        [SerializeField] private float fullHoldSeconds = 2.4f;

        [Tooltip("嘴在小熊框中心的哪裡，以框的短邊為單位（從圖上量的）")]
        [SerializeField] private Vector2 mouthOffset = new Vector2(0f, 0.12f);

        [Tooltip("水果佔盤子的比例")]
        [SerializeField] private float foodScale = 0.85f;

        public event Action HomeRequested;

        private readonly List<CardEntry> _pool = new List<CardEntry>();
        private readonly List<FoodItem> _live = new List<FoodItem>();
        private FoodItem[] _onPlate = new FoodItem[0];
        private string _lastEatenId;
        private int _bites;
        private bool _full;
        private float _lastChew = -10f;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            // 點小熊：張嘴、跳一下，不出聲
            if (mascotButton != null && mascot != null)
            {
                mascotButton.onClick.AddListener(() => mascot.React(Mascot.Pose.Surprised, 0.8f));
            }
        }

        private void OnEnable()
        {
            _onPlate = new FoodItem[slots != null ? slots.Length : 0];
            _bites = 0;
            _full = false;
            _lastEatenId = null;
            BuildPool();
            StartCoroutine(Begin());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (var item in _live)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _live.Clear();
            for (int i = 0; i < _onPlate.Length; i++) _onPlate[i] = null;
            _bites = 0;
            _full = false;
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        private void BuildPool()
        {
            _pool.Clear();
            foreach (var deck in DeckLibrary.All)
            {
                if (deck == null || Array.IndexOf(foodDeckIds, deck.deckId) < 0) continue;
                foreach (var card in deck.cards)
                {
                    if (card != null && card.HasImage) _pool.Add(card);
                }
            }
            if (_pool.Count == 0) Debug.LogWarning("[BabyApp] 餵小熊找不到有圖的食物卡（foodDeckIds）。");
        }

        private IEnumerator Begin()
        {
            // 等版面算好大小，盤子的位置才是對的
            yield return new WaitForSecondsRealtime(0.3f);
            yield return FillPlates();
            if (VoiceService.Instance != null && PromptLibrary.Instance != null)
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.ComeOn);
            }
        }

        private IEnumerator FillPlates()
        {
            for (int i = 0; i < _onPlate.Length; i++)
            {
                SpawnInto(i);
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }

        // ---------- 盤子 ----------

        private void SpawnInto(int slot)
        {
            if (_pool.Count == 0 || foodTemplate == null || field == null) return;
            if (slot < 0 || slot >= _onPlate.Length || slots[slot] == null || _onPlate[slot] != null) return;

            var card = PickCard();
            var item = Instantiate(foodTemplate, field);
            item.gameObject.SetActive(true);

            var rect = slots[slot].rect;
            float side = Mathf.Min(rect.width, rect.height) * foodScale;
            item.Setup(card, slot, SlotCenter(slot), side);
            item.Tapped += OnFoodTapped;
            item.DragStarted += OnFoodDragStarted;
            item.Dropped += OnFoodDropped;
            item.PopIn();

            _onPlate[slot] = item;
            _live.Add(item);
        }

        /// <summary>挑一樣跟其他盤子、跟剛吃掉那個都不一樣的；池子太小才允許重複。</summary>
        private CardEntry PickCard()
        {
            var candidates = new List<CardEntry>();
            foreach (var card in _pool)
            {
                if (card.id == _lastEatenId) continue;
                bool shown = false;
                foreach (var plate in _onPlate)
                {
                    if (plate != null && plate.Card == card) { shown = true; break; }
                }
                if (!shown) candidates.Add(card);
            }
            var from = candidates.Count > 0 ? candidates : _pool;
            return from[UnityEngine.Random.Range(0, from.Count)];
        }

        private Vector2 SlotCenter(int slot)
        {
            return field.InverseTransformPoint(slots[slot].position);
        }

        /// <summary>嘴在 Field 座標裡的位置。小熊在呼吸、在跳，所以每次都重算。</summary>
        private Vector2 MouthPoint()
        {
            var bear = (RectTransform)mascot.transform;
            float side = Mathf.Min(bear.rect.width, bear.rect.height);
            var world = bear.TransformPoint(new Vector3(mouthOffset.x * side, mouthOffset.y * side, 0f));
            return field.InverseTransformPoint(world);
        }

        // ---------- 餵 ----------

        private void OnFoodDragStarted(FoodItem item)
        {
            // 張嘴等：圓眼、o 型嘴那張
            if (mascot != null && !_full) mascot.React(Mascot.Pose.Surprised, 30f, jump: false);
        }

        private void OnFoodTapped(FoodItem item)
        {
            if (item == null || item.IsEating || mascot == null) return;
            StartCoroutine(Eat(item, flySeconds));
        }

        private void OnFoodDropped(FoodItem item, Vector2 screenPosition)
        {
            if (item == null || item.IsEating || mascot == null) return;

            // 判定很寬：手指放在小熊整個框裡，或水果離嘴夠近，都算餵到
            var bear = (RectTransform)mascot.transform;
            float side = Mathf.Min(bear.rect.width, bear.rect.height);
            bool onBear = RectTransformUtility.RectangleContainsScreenPoint(bear, screenPosition, null);
            bool nearMouth = Vector2.Distance(item.Rect.anchoredPosition, MouthPoint()) <= side * 0.35f;

            if (onBear || nearMouth)
            {
                StartCoroutine(Eat(item, dropFlySeconds));
            }
            else
            {
                item.ReturnHome();
                if (!_full) mascot.React(Mascot.Pose.Idle, 0f, jump: false);
            }
        }

        /// <summary>水果縮小著飛進小熊嘴裡（飛行寫法改自 CountScreen.FeedBear，終點換成嘴）。</summary>
        private IEnumerator Eat(FoodItem item, float seconds)
        {
            int slot = item.Slot;
            var card = item.Card;
            item.Lock();
            if (slot >= 0 && slot < _onPlate.Length && _onPlate[slot] == item) _onPlate[slot] = null;

            var rt = item.Rect;
            rt.SetAsLastSibling();
            var from = rt.anchoredPosition;
            var fromScale = rt.localScale;
            // 點一下的才往上拋一道弧線；已經拖到嘴邊的直接吸進去
            float arc = seconds >= 0.3f ? 80f : 0f;

            float t = 0f;
            while (t < seconds && item != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                float eased = p * p;
                rt.anchoredPosition = Vector2.Lerp(from, MouthPoint(), eased) + Vector2.up * (Mathf.Sin(p * Mathf.PI) * arc);
                rt.localScale = fromScale * Mathf.Lerp(1f, 0.15f, eased);
                yield return null;
            }

            _live.Remove(item);
            if (item != null) Destroy(item.gameObject);

            // 連點時咔滋會疊在一起變刺耳，太近的就不播
            if (VoiceService.Instance != null && Time.unscaledTime - _lastChew >= 0.15f)
            {
                _lastChew = Time.unscaledTime;
                VoiceService.Instance.PlayChew();
            }
            mascot.Chew(0.6f);
            _lastEatenId = card != null ? card.id : null;

            // 已經在「吃飽了」的收尾裡：這一口照樣吃掉，但不搶小熊的話、也不補盤子（收尾完會一起補）
            if (_full) yield break;

            mascot.React(Mascot.Pose.Happy, 1.0f, jump: false);
            _bites++;

            if (fullAfter > 0 && _bites >= fullAfter)
            {
                StartCoroutine(FullRoutine(card));
                yield break;
            }

            if (VoiceService.Instance != null)
            {
                // 每一口整串重播，新的會取代舊的，所以連點不會排隊；名稱一定聽得到
                VoiceService.Instance.PlaySequence(NameClip(card),
                    PromptLibrary.Instance != null ? PromptLibrary.Instance.Yummy : null);
            }

            yield return new WaitForSecondsRealtime(refillDelay);
            if (!_full) SpawnInto(slot);
        }

        /// <summary>吃飽了：小熊坐下來、肚子慢慢動，盤子收走；講完再上新的三盤，可以繼續餵。</summary>
        private IEnumerator FullRoutine(CardEntry lastCard)
        {
            _full = true;

            if (VoiceService.Instance != null)
            {
                VoiceService.Instance.PlayCorrect();
                VoiceService.Instance.PlaySequence(NameClip(lastCard),
                    PromptLibrary.Instance != null ? PromptLibrary.Instance.Full : null);
            }

            for (int i = 0; i < _onPlate.Length; i++)
            {
                if (_onPlate[i] == null) continue;
                StartCoroutine(ShrinkAway(_onPlate[i]));
                _onPlate[i] = null;
            }

            mascot.React(Mascot.Pose.Sleep, fullHoldSeconds, jump: false);
            mascot.Chew(1.2f);

            yield return new WaitForSecondsRealtime(fullHoldSeconds);
            // 台詞長短不一，等小熊講完，不猜秒數
            yield return VoiceService.WaitForSpeech(0.6f);

            _bites = 0;
            _full = false;
            yield return FillPlates();
        }

        private IEnumerator ShrinkAway(FoodItem item)
        {
            item.Lock();
            var rt = item.Rect;
            var fromScale = rt.localScale;
            float t = 0f;
            while (t < 0.25f && item != null)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = fromScale * (1f - Mathf.Clamp01(t / 0.25f));
                yield return null;
            }
            _live.Remove(item);
            if (item != null) Destroy(item.gameObject);
        }

        private static AudioClip NameClip(CardEntry card)
        {
            if (card == null) return null;
            var recorded = RecordingStore.TryGet(card.id);
            return recorded != null ? recorded : card.LocalizedVoice;
        }
    }
}
