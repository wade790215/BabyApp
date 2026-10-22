using System;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 著色畫面：一張淡淡的卡片圖墊在底下，上面隨便塗。
    /// 右欄：目前顏色（點開一面 24 色的色盤）、三種粗細、選圖牆、長按才會擦掉的按鈕。
    /// 沒有「塗出線外」這回事，2～3 歲要的是顏色跟著手指跑。
    /// </summary>
    public class PaintScreen : MonoBehaviour
    {
        /// <summary>色盤上的 24 色，建場景時也用這組塗色塊，兩邊才會一致。白色塗在白板上等於擦掉。</summary>
        public static readonly Color[] DefaultPalette =
        {
            new Color32(0xFF, 0x3B, 0x30, 0xFF), // 紅
            new Color32(0xC0, 0x39, 0x2B, 0xFF), // 深紅
            new Color32(0xFF, 0x7F, 0x50, 0xFF), // 珊瑚
            new Color32(0xFF, 0x95, 0x00, 0xFF), // 橘
            new Color32(0xFF, 0xC3, 0x00, 0xFF), // 金黃
            new Color32(0xFF, 0xD6, 0x0A, 0xFF), // 黃
            new Color32(0xC6, 0xFF, 0x00, 0xFF), // 萊姆
            new Color32(0xA6, 0xE2, 0x2E, 0xFF), // 黃綠
            new Color32(0x34, 0xC7, 0x59, 0xFF), // 綠
            new Color32(0x1E, 0x84, 0x49, 0xFF), // 深綠
            new Color32(0x2E, 0xC4, 0xB6, 0xFF), // 青
            new Color32(0x5A, 0xC8, 0xFA, 0xFF), // 天藍
            new Color32(0x0A, 0x84, 0xFF, 0xFF), // 藍
            new Color32(0x3F, 0x51, 0xB5, 0xFF), // 靛
            new Color32(0xBF, 0x5A, 0xF2, 0xFF), // 紫
            new Color32(0x7B, 0x1F, 0xA2, 0xFF), // 深紫
            new Color32(0xE9, 0x1E, 0x63, 0xFF), // 桃紅
            new Color32(0xFF, 0x6F, 0xB5, 0xFF), // 粉紅
            new Color32(0xFF, 0xC1, 0xE3, 0xFF), // 淺粉
            new Color32(0xF5, 0xDE, 0xB3, 0xFF), // 米
            new Color32(0x8D, 0x55, 0x24, 0xFF), // 棕
            new Color32(0x9E, 0x9E, 0x9E, 0xFF), // 灰
            new Color32(0x2B, 0x2B, 0x2B, 0xFF), // 黑
            new Color32(0xFF, 0xFF, 0xFF, 0xFF)  // 白
        };

        /// <summary>三種筆刷半徑：細、中、粗。</summary>
        public static readonly int[] BrushSizes = { 12, 26, 44 };

        [Header("畫布")]
        [SerializeField] private PaintCanvas paintCanvas;
        [SerializeField] private Image backdrop;

        [Header("右欄按鈕")]
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private HoldButton clearButton;
        [SerializeField] private Button pickButton;
        [SerializeField] private Button colorButton;
        [SerializeField] private Image colorFace;
        [SerializeField] private Button[] sizeButtons;
        [SerializeField] private Image[] sizeRings;

        [Header("色盤")]
        [SerializeField] private GameObject colorPicker;
        [SerializeField] private Button[] colorButtons;
        [SerializeField] private Image[] colorRings;

        [Header("選圖牆")]
        [SerializeField] private GameObject picturePicker;
        [SerializeField] private Transform pickerContent;
        [SerializeField] private PictureCell cellTemplate;

        [SerializeField] private Color[] palette = DefaultPalette;

        public event Action HomeRequested;

        private List<Sprite> _pictures;
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _hasPicture;
        private int _selectedColor = -1;
        private int _selectedSize = -1;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
            if (clearButton != null) clearButton.Triggered += ClearCanvas;
            if (pickButton != null) pickButton.onClick.AddListener(() => OpenPopup(picturePicker));
            if (colorButton != null) colorButton.onClick.AddListener(() => OpenPopup(colorPicker));

            HookButtons(colorButtons, SelectColor);
            HookButtons(sizeButtons, SelectSize);
        }

        private void OnEnable()
        {
            if (_pictures == null) LoadPictures();
            if (_cells.Count == 0) BuildPicker();
            ClosePopups();

            if (_selectedColor < 0) SelectColor(0, silent: true);
            if (_selectedSize < 0) SelectSize(1, silent: true);

            // 第一次進來先墊第一張圖，之後保留上次選的
            if (!_hasPicture && _pictures.Count > 0) SetPicture(_pictures[0], silent: true);

            AppSettings.Changed += OnSettingsChanged;
        }

        private void OnDisable() => AppSettings.Changed -= OnSettingsChanged;

        /// <summary>語言換了，「空白」那格的字要跟著換，重建一次最省事。</summary>
        private void OnSettingsChanged() => BuildPicker();

        // ---------- 彈出面板 ----------

        private void OpenPopup(GameObject popup)
        {
            if (popup == null) return;
            ClosePopups();
            popup.SetActive(true);
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }

        private void ClosePopups()
        {
            if (colorPicker != null) colorPicker.SetActive(false);
            if (picturePicker != null) picturePicker.SetActive(false);
        }

        // ---------- 圖片 ----------

        private void LoadPictures()
        {
            _pictures = new List<Sprite>();
            foreach (var deck in DeckLibrary.All)
            {
                foreach (var card in deck.cards)
                {
                    if (card != null && card.HasImage) _pictures.Add(card.image);
                }
            }
        }

        private void BuildPicker()
        {
            if (pickerContent == null || cellTemplate == null) return;

            foreach (var cell in _cells)
            {
                if (cell != null) Destroy(cell);
            }
            _cells.Clear();

            // 第一格永遠是空白
            SpawnCell(null, Strings.Get("blank"), () => SetPicture(null, silent: false));
            foreach (var sprite in _pictures)
            {
                var captured = sprite;
                SpawnCell(captured, null, () => SetPicture(captured, silent: false));
            }
        }

        private void SpawnCell(Sprite sprite, string text, Action onChosen)
        {
            var cell = Instantiate(cellTemplate, pickerContent);
            cell.gameObject.SetActive(true);
            cell.Setup(sprite, text, onChosen);
            _cells.Add(cell.gameObject);
        }

        /// <summary>換底圖並擦乾淨；sprite 為 null 就是空白畫布。</summary>
        private void SetPicture(Sprite sprite, bool silent)
        {
            if (paintCanvas != null) paintCanvas.Clear();
            if (backdrop != null)
            {
                backdrop.sprite = sprite;
                backdrop.enabled = sprite != null;
            }
            _hasPicture = true;
            ClosePopups();
            if (!silent && VoiceService.Instance != null) VoiceService.Instance.PlayPage();
        }

        private void ClearCanvas()
        {
            if (paintCanvas != null) paintCanvas.Clear();
            if (VoiceService.Instance != null) VoiceService.Instance.PlayPage();
        }

        // ---------- 顏色與粗細 ----------

        private void SelectColor(int index) => SelectColor(index, silent: false);

        private void SelectColor(int index, bool silent)
        {
            if (palette == null || palette.Length == 0) return;
            index = Mathf.Clamp(index, 0, palette.Length - 1);
            _selectedColor = index;

            if (paintCanvas != null) paintCanvas.SetColor(palette[index]);
            if (colorFace != null) colorFace.color = palette[index];
            ShowRing(colorRings, index);
            ClosePopups();
            if (!silent && VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }

        private void SelectSize(int index) => SelectSize(index, silent: false);

        private void SelectSize(int index, bool silent)
        {
            index = Mathf.Clamp(index, 0, BrushSizes.Length - 1);
            _selectedSize = index;

            if (paintCanvas != null) paintCanvas.SetBrushRadius(BrushSizes[index]);
            if (!silent && VoiceService.Instance != null) VoiceService.Instance.PlayTap();
            ShowRing(sizeRings, index);
        }

        // ---------- 小工具 ----------

        private static void HookButtons(Button[] buttons, Action<int> onClick)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => onClick(index));
            }
        }

        /// <summary>選中的那顆外面亮出深色圈，其他關掉。</summary>
        private static void ShowRing(Image[] rings, int selected)
        {
            if (rings == null) return;
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] != null) rings[i].enabled = i == selected;
            }
        }
    }
}
