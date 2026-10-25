using System.Collections;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 拼圖：一張卡片圖切成 2×2 四片，底盤上淡淡地印著完整的圖當提示，
    /// 四片散在下面的托盤，拖到大概的位置就會吸進去「咔」一聲。全部拼好小熊說「拼好了！」再播那張卡。
    /// 吸附範圍很寬（半片），2 歲半只要「放得差不多」就會成功；沒有錯、沒有計時。
    /// 拼好的瞬間碎片淡掉、底下的完整圖浮上來，看到的是一張完整的圖，不是四塊有縫的。
    /// 前兩回合只切 2 片（左右），之後 4 片。點小熊再聽一次這是什麼。
    /// </summary>
    public class PuzzleScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private DraggableWidget mascotHandle;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform boardInner;
        [SerializeField] private Image ghost;
        [SerializeField] private RectTransform tray;
        [SerializeField] private PuzzlePiece pieceTemplate;

        [Tooltip("拼好幾回合之後從 2 片變 4 片")]
        [SerializeField] private int easyRounds = 2;
        [SerializeField] private float nextRoundDelay = 2.4f;

        public event System.Action HomeRequested;

        private struct Entry
        {
            public CardEntry Card;
            public CardDeck Deck;
        }

        private readonly List<Entry> _pool = new List<Entry>();
        private readonly List<PuzzlePiece> _pieces = new List<PuzzlePiece>();
        private Entry _current;
        private int _placed;
        private int _roundsDone;
        private bool _busy;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            if (mascotHandle != null) mascotHandle.Tapped += SayName;
        }

        private void OnEnable()
        {
            BuildPool();
            _busy = false;
            StartCoroutine(StartAfter(0.3f));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearPieces();
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        private void BuildPool()
        {
            _pool.Clear();
            foreach (var deck in DeckLibrary.All)
            {
                if (deck.deckId == "colors") continue; // 一顆色球切四片沒意思
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
            if (_pool.Count == 0 || pieceTemplate == null || boardInner == null || tray == null) return;

            ClearPieces();
            _current = _pool[Random.Range(0, _pool.Count)];
            _placed = 0;
            _busy = false;

            if (ghost != null)
            {
                ghost.sprite = _current.Card.image;
                ghost.enabled = ghost.sprite != null;
            }

            // 底盤內緣是正方形，整張圖就鋪滿它
            var fullSize = boardInner.rect.size;
            // 前幾回合只切左右兩片，之後 2×2
            int cols = 2;
            int rows = _roundsDone < easyRounds ? 1 : 2;
            var pieceSize = new Vector2(fullSize.x / cols, fullSize.y / rows);

            // 每片的正確位置（在 pieces 的父物件座標裡），要把底盤的位置換算過來
            var parent = (RectTransform)pieceTemplate.transform.parent;
            Vector2 boardCenter = ToParent(parent, boardInner);
            Vector2 trayCenter = ToParent(parent, tray);
            float trayWidth = tray.rect.width;

            var order = new List<int>();
            for (int i = 0; i < cols * rows; i++) order.Add(i);
            Shuffle(order);

            int count = cols * rows;
            float trayScale = Mathf.Min(1f,
                (trayWidth - 40f) / (count * pieceSize.x * 1.1f),
                tray.rect.height / pieceSize.y);
            float trayStep = pieceSize.x * 1.1f * trayScale;
            float trayStart = -(count - 1) * trayStep * 0.5f;

            for (int k = 0; k < count; k++)
            {
                int index = order[k];
                int col = index % cols;
                int row = index / cols;

                var piece = Instantiate(pieceTemplate, parent);
                piece.gameObject.SetActive(true);

                var target = boardCenter + new Vector2(
                    -(cols - 1) * 0.5f * pieceSize.x + col * pieceSize.x,
                    (rows - 1) * 0.5f * pieceSize.y - row * pieceSize.y);
                var rest = trayCenter + new Vector2(trayStart + k * trayStep, 0f);

                piece.Setup(_current.Card.image, fullSize, col, row, cols, rows, target, rest,
                    snapDistance: pieceSize.x * 0.5f, restScale: trayScale);
                piece.Placed += OnPiecePlaced;
                _pieces.Add(piece);
            }

            if (mascot != null) mascot.React(Mascot.Pose.Think, 1.2f, jump: false);
            SayName();
        }

        private static Vector2 ToParent(RectTransform parent, RectTransform target)
        {
            var local = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            return new Vector2(local.x, local.y);
        }

        private void ClearPieces()
        {
            foreach (var piece in _pieces)
            {
                if (piece != null) Destroy(piece.gameObject);
            }
            _pieces.Clear();
        }

        private void SayName()
        {
            if (_current.Card == null || VoiceService.Instance == null) return;
            var recorded = RecordingStore.TryGet(_current.Card.id);
            VoiceService.Instance.PlaySequence(recorded != null ? recorded : _current.Card.LocalizedVoice);
        }

        // ---------- 拼 ----------

        private void OnPiecePlaced()
        {
            if (_busy) return;
            _placed++;
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
            if (mascot != null) mascot.Jump();

            if (_placed < _pieces.Count) return;

            _busy = true;
            _roundsDone++;
            UsageStats.CountRoundFinished();
            if (VoiceService.Instance != null)
            {
                VoiceService.Instance.PlaySparkle();
                var sound = _current.Card.sound != null ? _current.Card.sound : (_current.Deck != null ? _current.Deck.fallbackSound : null);
                var recorded = RecordingStore.TryGet(_current.Card.id);
                VoiceService.Instance.PlaySequence(
                    PromptLibrary.Instance != null ? PromptLibrary.Instance.Done : null,
                    sound,
                    recorded != null ? recorded : _current.Card.LocalizedVoice);
            }
            if (mascot != null) mascot.React(Mascot.Pose.Happy, nextRoundDelay);
            StartCoroutine(Celebrate());
        }

        private IEnumerator Celebrate()
        {
            // 先把碎片的白邊與縫隙淡掉、底下的完整圖浮上來：拼好的那一刻是一張完整的圖，不是四塊拼起來的
            var groups = new List<CanvasGroup>();
            foreach (var piece in _pieces)
            {
                if (piece == null) continue;
                var group = piece.GetComponent<CanvasGroup>();
                if (group == null) group = piece.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }
            Color ghostFrom = ghost != null ? ghost.color : Color.white;
            Color ghostTo = new Color(1f, 1f, 1f, 1f);

            const float merge = 0.4f;
            float t = 0f;
            while (t < merge)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / merge);
                foreach (var g in groups) g.alpha = 1f - p;
                if (ghost != null) ghost.color = Color.Lerp(ghostFrom, ghostTo, p);
                yield return null;
            }
            foreach (var piece in _pieces)
            {
                if (piece != null) piece.gameObject.SetActive(false);
            }

            // 整塊底盤彈一下
            const float duration = 0.6f;
            t = 0f;
            while (t < duration && board != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                board.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            if (board != null) board.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, nextRoundDelay - duration - merge));

            if (ghost != null) ghost.color = ghostFrom;
            NewRound();
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
