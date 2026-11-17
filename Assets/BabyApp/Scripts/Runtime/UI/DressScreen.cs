using System;
using System.Collections;
using BabyApp.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 換裝（紙娃娃第一版）：小熊大大地在左邊，右邊六格配件。點一下，配件就飛到小熊頭上戴好，
    /// 小熊開心地跳一下、念出名稱、「好好看！」。再點一次同一個就脫掉、飛回格子；點同部位的另一個就換。
    /// 也可以拖：拖起來放手，不管放在哪都會飛到頭上，沒有「放錯」這回事。點小熊頭上的東西也能脫掉。
    /// 頭頂跟臉各戴一樣。造型記在 Outfit 裡，回首頁、去別的遊戲小熊都還戴著。
    ///
    /// 造型是「按下去的當下」就存好的，飛行只是演給小孩看：這樣連點、飛到一半離開畫面，
    /// 狀態都不會亂。飛上去的途中先請小熊把那個部位藏著（HoldSlot），落地才露出來。
    /// </summary>
    public class DressScreen : MonoBehaviour
    {
        [SerializeField] private Mascot mascot;
        [SerializeField] private Button mascotButton;
        [Tooltip("小熊頭上、臉上那兩張配件圖上的按鈕：點戴著的東西就脫掉")]
        [SerializeField] private Button headWearButton;
        [SerializeField] private Button faceWearButton;
        [SerializeField] private HoldButton homeButton;
        [Tooltip("飛來飛去的複製圖都生在這層；它自己沒有圖，不會擋到點擊")]
        [SerializeField] private RectTransform field;
        [Tooltip("順序跟 Outfit.All 一樣")]
        [SerializeField] private OutfitTile[] tiles;
        [SerializeField] private AudioClip[] namesZh;
        [SerializeField] private AudioClip[] namesEn;

        [SerializeField] private float flySeconds = 0.4f;
        [SerializeField] private float returnSeconds = 0.3f;

        public event Action HomeRequested;

        // 每樣配件最多一張複製圖在外面飛（或被拖著）
        private Image[] _ghosts = new Image[0];
        private Coroutine[] _moves = new Coroutine[0];
        private bool[] _dragging = new bool[0];
        private Vector2[] _grabOffset = new Vector2[0];
        // 每個部位正在飛上去的是哪一個（-1 = 沒有）
        private readonly int[] _incoming = new int[Outfit.SlotCount];

        private void Awake()
        {
            int count = Outfit.All.Length;
            _ghosts = new Image[count];
            _moves = new Coroutine[count];
            _dragging = new bool[count];
            _grabOffset = new Vector2[count];
            for (int s = 0; s < _incoming.Length; s++) _incoming[s] = -1;

            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            // 點小熊：開心地跳一下
            if (mascotButton != null && mascot != null)
            {
                mascotButton.onClick.AddListener(() => mascot.React(Mascot.Pose.Happy, 0.8f));
            }
            if (headWearButton != null) headWearButton.onClick.AddListener(() => TakeOff(Outfit.Worn(Outfit.Slot.Head)));
            if (faceWearButton != null) faceWearButton.onClick.AddListener(() => TakeOff(Outfit.Worn(Outfit.Slot.Face)));

            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].Setup(i);
                tiles[i].Tapped += OnTileTapped;
                tiles[i].DragStarted += OnDragStarted;
                tiles[i].DragMoved += OnDragMoved;
                tiles[i].DragEnded += OnDragEnded;
            }
        }

        private void OnEnable()
        {
            Outfit.Changed += RefreshTiles;
            RefreshTiles();
            StartCoroutine(Begin());
        }

        private void OnDisable()
        {
            Outfit.Changed -= RefreshTiles;
            StopAllCoroutines();
            for (int i = 0; i < _ghosts.Length; i++)
            {
                if (_ghosts[i] != null) Destroy(_ghosts[i].gameObject);
                _ghosts[i] = null;
                _moves[i] = null;
                _dragging[i] = false;
            }
            for (int s = 0; s < _incoming.Length; s++) _incoming[s] = -1;
            // 造型早就存好了，這裡只要把藏著的部位放出來
            if (mascot != null)
            {
                mascot.ReleaseSlot(Outfit.Slot.Head);
                mascot.ReleaseSlot(Outfit.Slot.Face);
            }
            if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
        }

        private IEnumerator Begin()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            if (VoiceService.Instance != null && PromptLibrary.Instance != null)
            {
                VoiceService.Instance.PlaySequence(PromptLibrary.Instance.ComeOn);
            }
        }

        private bool Valid(int index)
        {
            return index >= 0 && index < _ghosts.Length && tiles != null && index < tiles.Length &&
                   tiles[index] != null && mascot != null && field != null;
        }

        /// <summary>格子裡的東西在不在：戴在頭上、正在飛、被拖著，都只留影子。</summary>
        private void RefreshTiles()
        {
            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                bool flying = i < _ghosts.Length && _ghosts[i] != null;
                tiles[i].SetAway(Outfit.IsWorn(i) || flying);
            }
        }

        // ---------- 輸入 ----------

        private void OnTileTapped(OutfitTile tile)
        {
            int index = tile.Index;
            if (!Valid(index) || _dragging[index]) return;
            if (Outfit.IsWorn(index)) TakeOff(index);
            else Wear(index);
        }

        private void OnDragStarted(OutfitTile tile, Vector2 screenPosition)
        {
            int index = tile.Index;
            if (!Valid(index)) return;
            // 已經戴在頭上，格子裡只剩影子，沒有東西可以拿起來
            if (Outfit.IsWorn(index)) return;

            var ghost = Ghost(index, atHead: false);
            StopMove(index);
            _dragging[index] = true;
            var rt = ghost.rectTransform;
            rt.SetAsLastSibling();
            rt.localScale = Vector3.one * 1.1f;
            _grabOffset[index] = ToField(screenPosition, out var local) ? rt.anchoredPosition - local : Vector2.zero;
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }

        private void OnDragMoved(OutfitTile tile, Vector2 screenPosition)
        {
            int index = tile.Index;
            if (!Valid(index) || !_dragging[index] || _ghosts[index] == null) return;
            if (ToField(screenPosition, out var local)) _ghosts[index].rectTransform.anchoredPosition = local + _grabOffset[index];
        }

        private void OnDragEnded(OutfitTile tile, Vector2 screenPosition)
        {
            int index = tile.Index;
            if (!Valid(index)) return;
            if (!_dragging[index])
            {
                // 拖的是已經戴著的那格：沒東西可拿，但不能「按了沒反應」，小熊跳一下
                mascot.React(Mascot.Pose.Happy, 0.8f);
                return;
            }
            _dragging[index] = false;
            if (_ghosts[index] != null) _ghosts[index].rectTransform.localScale = Vector3.one;
            // 放手一律是「戴上」，不管放在哪：從放手的地方飛到頭上
            Wear(index, playTap: false);
        }

        private bool ToField(Vector2 screenPosition, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(field, screenPosition, null, out local);
        }

        // ---------- 戴、脫 ----------

        private void Wear(int index, bool playTap = true)
        {
            if (!Valid(index) || Outfit.IsWorn(index)) return;
            var item = Outfit.All[index];
            int s = (int)item.Slot;

            // 同部位原本戴的（或還在飛上去的）那個，飛回自己的格子
            int previous = Outfit.Worn(item.Slot);
            if (previous >= 0) SendHome(previous);

            // 先藏、再存：存的那一刻每隻小熊都會換上新的圖，這一隻要等複製圖落地才露出來
            _incoming[s] = index;
            mascot.HoldSlot(item.Slot);
            Outfit.Wear(index);

            var ghost = Ghost(index, atHead: false);
            StopMove(index);
            _moves[index] = StartCoroutine(FlyToHead(index, ghost));
            if (playTap && VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }

        private void TakeOff(int index)
        {
            if (!Valid(index) || !Outfit.IsWorn(index)) return;
            var slot = Outfit.All[index].Slot;
            int s = (int)slot;

            SendHome(index);
            if (_incoming[s] == index) _incoming[s] = -1;
            Outfit.TakeOff(slot);
            mascot.ReleaseSlot(slot);

            mascot.React(Mascot.Pose.Surprised, 0.6f, jump: false);
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();
        }

        /// <summary>讓 index 那樣東西飛回格子：已經有複製圖在外面就從它現在的位置飛，沒有就從頭上生一張。</summary>
        private void SendHome(int index)
        {
            var ghost = Ghost(index, atHead: true);
            StopMove(index);
            _dragging[index] = false;
            ghost.rectTransform.localScale = Vector3.one;
            _moves[index] = StartCoroutine(FlyHome(index, ghost));
        }

        private IEnumerator FlyToHead(int index, Image ghost)
        {
            var item = Outfit.All[index];
            var rt = ghost.rectTransform;
            rt.SetAsLastSibling();
            var from = rt.anchoredPosition;
            float fromSide = rt.sizeDelta.x;
            float fromAngle = rt.localEulerAngles.z;

            float t = 0f;
            while (t < flySeconds && ghost != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / flySeconds);
                float eased = p * p * (3f - 2f * p);
                // 小熊在呼吸、在跳，終點每幀重算
                HeadPose(index, out var to, out float toSide);
                rt.anchoredPosition = Vector2.Lerp(from, to, eased) + Vector2.up * (Mathf.Sin(p * Mathf.PI) * 60f);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(fromSide, toSide, eased);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(fromAngle, item.Tilt, eased));
                yield return null;
            }

            _moves[index] = null;
            DropGhost(index);

            int s = (int)item.Slot;
            if (_incoming[s] == index)
            {
                _incoming[s] = -1;
                mascot.ReleaseSlot(item.Slot);
            }
            if (!Outfit.IsWorn(index)) yield break;

            mascot.React(Mascot.Pose.Happy, 1.2f);
            if (VoiceService.Instance != null)
            {
                // 每次整串重播，新的會取代舊的，連點不會排隊
                VoiceService.Instance.PlaySequence(NameClip(index),
                    PromptLibrary.Instance != null ? PromptLibrary.Instance.Pretty : null);
            }
        }

        private IEnumerator FlyHome(int index, Image ghost)
        {
            var rt = ghost.rectTransform;
            var from = rt.anchoredPosition;
            float fromSide = rt.sizeDelta.x;
            float fromAngle = rt.localEulerAngles.z;

            float t = 0f;
            while (t < returnSeconds && ghost != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / returnSeconds);
                float eased = 1f - (1f - p) * (1f - p);
                TilePose(index, out var to, out float toSide);
                rt.anchoredPosition = Vector2.Lerp(from, to, eased);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(fromSide, toSide, eased);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(fromAngle, 0f, eased));
                yield return null;
            }

            _moves[index] = null;
            DropGhost(index);
        }

        // ---------- 複製圖 ----------

        /// <summary>拿 index 的複製圖；外面沒有就生一張，放在格子上（或頭上）。</summary>
        private Image Ghost(int index, bool atHead)
        {
            if (_ghosts[index] != null) return _ghosts[index];

            var go = new GameObject("Ghost_" + Outfit.All[index].Id, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(field, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            var image = go.GetComponent<Image>();
            image.sprite = tiles[index].Icon.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            Vector2 position;
            float side;
            if (atHead)
            {
                HeadPose(index, out position, out side);
                rt.localEulerAngles = new Vector3(0f, 0f, Outfit.All[index].Tilt);
            }
            else
            {
                TilePose(index, out position, out side);
            }
            rt.anchoredPosition = position;
            rt.sizeDelta = Vector2.one * side;

            _ghosts[index] = image;
            RefreshTiles();
            return image;
        }

        private void DropGhost(int index)
        {
            if (_ghosts[index] != null) Destroy(_ghosts[index].gameObject);
            _ghosts[index] = null;
            RefreshTiles();
        }

        private void StopMove(int index)
        {
            if (_moves[index] != null) StopCoroutine(_moves[index]);
            _moves[index] = null;
        }

        /// <summary>格子裡那張圖在 Field 座標的位置與邊長。</summary>
        private void TilePose(int index, out Vector2 position, out float side)
        {
            var icon = tiles[index].Icon.rectTransform;
            position = field.InverseTransformPoint(icon.TransformPoint(icon.rect.center));
            side = Mathf.Min(icon.rect.width, icon.rect.height);
        }

        /// <summary>戴在小熊頭上時，在 Field 座標的位置與邊長。</summary>
        private void HeadPose(int index, out Vector2 position, out float side)
        {
            mascot.WearPose(index, out var world, out side);
            position = field.InverseTransformPoint(world);
        }

        private AudioClip NameClip(int index)
        {
            // 播的當下才看語言，不先存起來
            var set = AppSettings.IsEnglish ? namesEn : namesZh;
            return set != null && index >= 0 && index < set.Length ? set[index] : null;
        }
    }
}
