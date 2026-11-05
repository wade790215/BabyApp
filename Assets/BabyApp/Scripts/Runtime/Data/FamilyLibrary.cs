using System;
using System.IO;
using BabyApp.Core;
using UnityEngine;

namespace BabyApp.Data
{
    /// <summary>
    /// 家人相片卡的資料：固定 8 個槽位（爸爸、媽媽、阿公、阿嬤 + 4 個自訂），
    /// 每格一張照片（JPG）加一段家長錄的稱呼（WAV），全部只存在這台裝置的 persistentDataPath/family/。
    /// 執行時把可玩的槽位組成一個 CardDeck 給 DeckPickScreen／DeckScreen 用，走跟內建卡完全一樣的流程；
    /// 刻意不放進 DeckLibrary，所以找一找、配對那些遊戲第一版不會抓到家人卡。
    /// 掛在 AppRoot 上，預設稱呼語音由 Builder 從 Audio/Prompts 接上。
    /// </summary>
    public class FamilyLibrary : MonoBehaviour
    {
        public static FamilyLibrary Instance { get; private set; }

        public const int SlotCount = 8;
        public const int FixedSlots = 4;
        public const string DeckId = "family";

        /// <summary>卡片代號，也是檔名。固定以 family_ 開頭，跟 CSV 裡的卡不會撞。</summary>
        public static readonly string[] Ids =
        {
            "family_dad", "family_mom", "family_grandpa", "family_grandma",
            "family_custom1", "family_custom2", "family_custom3", "family_custom4"
        };

        // 卡名是卡片內容（相當於 CSV），中英都要填進 CardEntry，所以不走 Strings
        private static readonly (string zh, string en)[] Names =
        {
            ("爸爸", "Daddy"), ("媽媽", "Mommy"), ("阿公", "Grandpa"), ("阿嬤", "Grandma"),
            ("家人", "Family"), ("家人", "Family"), ("家人", "Family"), ("家人", "Family")
        };

        [Tooltip("固定四格沒錄稱呼時的預設語音（中文），順序：爸爸、媽媽、阿公、阿嬤")]
        [SerializeField] private AudioClip[] fallbackZh;

        [Tooltip("同上，英文")]
        [SerializeField] private AudioClip[] fallbackEn;

        [SerializeField] private Color themeColor = new Color32(0xFF, 0x9E, 0xC4, 0xFF);

        /// <summary>資料每改一次就 +1，DeckPickScreen 用它判斷要不要重建格子。</summary>
        public int Version { get; private set; }

        public event Action Changed;

        private readonly Sprite[] _photos = new Sprite[SlotCount];
        private readonly AudioClip[] _voices = new AudioClip[SlotCount];
        private CardDeck _deck;

        private static string Folder => Path.Combine(Application.persistentDataPath, "family");
        private static string PhotoPath(int i) => Path.Combine(Folder, Ids[i] + ".jpg");
        private static string VoicePath(int i) => Path.Combine(Folder, Ids[i] + ".wav");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        // 啟動時就把 8 格讀完，免得第一次進選主題頁時讀 JPG 卡一下
        private void Start() => LoadAll();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- 查詢 ----------

        public bool HasPhoto(int i) => InRange(i) && _photos[i] != null;
        public bool HasVoice(int i) => InRange(i) && _voices[i] != null;

        /// <summary>固定槽有照片就能玩（沒錄音用預設稱呼）；自訂槽要照片加錄音都有。</summary>
        public bool IsPlayable(int i) => HasPhoto(i) && (i < FixedSlots || HasVoice(i));

        public Sprite Photo(int i) => InRange(i) ? _photos[i] : null;
        public AudioClip Voice(int i) => InRange(i) ? _voices[i] : null;

        /// <summary>沒錄音時會播的預設稱呼（只有固定四格有）。</summary>
        public AudioClip FallbackVoice(int i)
        {
            if (i < 0 || i >= FixedSlots) return null;
            var table = AppSettings.IsEnglish ? fallbackEn : fallbackZh;
            return table != null && i < table.Length ? table[i] : null;
        }

        /// <summary>家長頁上這一格的名字：固定槽是稱呼，自訂槽是「自訂 1」。</summary>
        public string SlotLabel(int i)
        {
            if (!InRange(i)) return string.Empty;
            if (i < FixedSlots) return AppSettings.IsEnglish ? Names[i].en : Names[i].zh;
            return string.Format(Strings.Get("family_custom"), i - FixedSlots + 1);
        }

        public bool AnyPlayable()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (IsPlayable(i)) return true;
            }
            return false;
        }

        /// <summary>可玩槽位組成的卡片組；一張都沒有就回 null（選主題頁就不會出現「家人」格）。</summary>
        public CardDeck Deck
        {
            get
            {
                if (!AnyPlayable()) return null;
                if (_deck == null) _deck = BuildDeck();
                return _deck;
            }
        }

        // ---------- 修改 ----------

        /// <summary>存一張照片（已經縮好的 texture）。舊照片會被覆蓋並釋放。</summary>
        public void SavePhoto(int i, Texture2D texture)
        {
            if (!InRange(i) || texture == null) return;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(PhotoPath(i), ImageConversion.EncodeToJPG(texture, 85));
            ReleasePhoto(i);
            _photos[i] = LoadPhoto(i);
            Bump();
        }

        public void SaveVoice(int i, AudioClip clip)
        {
            if (!InRange(i) || clip == null) return;
            Directory.CreateDirectory(Folder);
            WavFile.Save(VoicePath(i), clip);
            _voices[i] = clip;
            Bump();
        }

        /// <summary>整格清掉：照片、家長錄音、還有小孩在這張卡上錄的聲音。</summary>
        public void Delete(int i)
        {
            if (!InRange(i)) return;
            if (File.Exists(PhotoPath(i))) File.Delete(PhotoPath(i));
            if (File.Exists(VoicePath(i))) File.Delete(VoicePath(i));
            ReleasePhoto(i);
            _voices[i] = null;
            RecordingStore.Delete(Ids[i]);
            Bump();
        }

        // ---------- 內部 ----------

        private static bool InRange(int i) => i >= 0 && i < SlotCount;

        private void LoadAll()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                ReleasePhoto(i);
                _photos[i] = LoadPhoto(i);
                _voices[i] = File.Exists(VoicePath(i)) ? WavFile.Load(VoicePath(i), Ids[i]) : null;
            }
            Bump();
        }

        private static Sprite LoadPhoto(int i)
        {
            string path = PhotoPath(i);
            if (!File.Exists(path)) return null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                // 讀完就設成不可讀，記憶體只留 GPU 那一份
                if (!ImageConversion.LoadImage(texture, bytes, true))
                {
                    Destroy(texture);
                    return null;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                // texture 已經不可讀，網格要用 FullRect（Tight 要讀像素算外框）
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
                sprite.name = Ids[i];
                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BabyApp] 讀家人照片失敗：" + path + "，" + e.Message);
                return null;
            }
        }

        private void ReleasePhoto(int i)
        {
            var sprite = _photos[i];
            if (sprite == null) return;
            if (sprite.texture != null) Destroy(sprite.texture);
            Destroy(sprite);
            _photos[i] = null;
        }

        private CardDeck BuildDeck()
        {
            var deck = ScriptableObject.CreateInstance<CardDeck>();
            deck.hideFlags = HideFlags.DontSave;
            deck.deckId = DeckId;
            deck.displayName = "家人";
            deck.displayNameEn = "Family";
            deck.themeColor = themeColor;
            deck.sortOrder = int.MaxValue;
            deck.cover = null;         // DeckButton 沒封面時自動用第一張可玩卡（爸爸的照片）
            deck.fallbackSound = null;

            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsPlayable(i)) continue;
                var voice = _voices[i];
                deck.cards.Add(new CardEntry
                {
                    id = Ids[i],
                    displayName = Names[i].zh,
                    displayNameEn = Names[i].en,
                    image = _photos[i],
                    // 家長錄的稱呼放在「叫聲」的位置；有它就不再接預設稱呼，免得念兩次
                    sound = voice,
                    voice = voice == null ? FallbackClip(fallbackZh, i) : null,
                    voiceEn = voice == null ? FallbackClip(fallbackEn, i) : null
                });
            }
            return deck;
        }

        private static AudioClip FallbackClip(AudioClip[] table, int i)
        {
            return i < FixedSlots && table != null && i < table.Length ? table[i] : null;
        }

        private void Bump()
        {
            Version++;
            if (_deck != null)
            {
                Destroy(_deck);
                _deck = null;
            }
            Changed?.Invoke();
        }
    }
}
