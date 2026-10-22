using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 手指塗鴉的畫布：拖哪裡哪裡就上色。粗筆刷、邊緣微軟化，像麥克筆。
    /// 畫在一張透明的 Texture2D 上，底下可以墊別的圖（淡淡的卡片圖）給他塗。
    ///
    /// 效能做法：CPU 端維護一份像素陣列，每一筆只更新筆刷那一小塊，
    /// 一幀最多 Apply 一次，平板上連續塗也不會卡。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class PaintCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("筆刷半徑（畫布像素）。2～3 歲手指粗、動作大，要粗一點才有成就感")]
        [SerializeField] private int brushRadius = 26;

        public event Action StrokeStarted;

        private RawImage _raw;
        private RectTransform _rt;
        private Texture2D _texture;
        private Color32[] _buffer;
        private Color32[] _scratch;
        private float[] _mask;
        private int _width;
        private int _height;
        private int _diameter;
        private Color32 _color = new Color32(0xFF, 0x3B, 0x30, 0xFF);
        private Vector2 _last;
        private bool _hasLast;
        private bool _dirty;

        private void Awake()
        {
            _raw = GetComponent<RawImage>();
            _rt = (RectTransform)transform;
        }

        private void Start() => EnsureTexture();

        private void LateUpdate()
        {
            if (!_dirty) return;
            _texture.Apply(false);
            _dirty = false;
        }

        public void SetColor(Color color) => _color = color;

        /// <summary>換筆刷粗細（半徑，畫布像素）。</summary>
        public void SetBrushRadius(int radius)
        {
            brushRadius = Mathf.Max(2, radius);
            BuildBrush();
        }

        /// <summary>整張擦乾淨（變回透明）。</summary>
        public void Clear()
        {
            EnsureTexture();
            var transparent = new Color32(0, 0, 0, 0);
            for (int i = 0; i < _buffer.Length; i++) _buffer[i] = transparent;
            _texture.SetPixels32(_buffer);
            _texture.Apply(false);
            _dirty = false;
            _hasLast = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var p = ToTexture(eventData);
            StrokeStarted?.Invoke();
            Stamp(p);
            _last = p;
            _hasLast = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var p = ToTexture(eventData);
            if (_hasLast) StampLine(_last, p);
            else Stamp(p);
            _last = p;
            _hasLast = true;
        }

        public void OnPointerUp(PointerEventData eventData) => _hasLast = false;

        // ---------- 內部 ----------

        private void EnsureTexture()
        {
            if (_texture != null) return;

            // 畫布像素跟版面單位 1:1，在不同解析度的平板上由 Canvas 自己縮放
            var rect = _rt.rect;
            _width = Mathf.Max(8, Mathf.RoundToInt(rect.width));
            _height = Mathf.Max(8, Mathf.RoundToInt(rect.height));

            _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _buffer = new Color32[_width * _height];
            _raw.texture = _texture;

            BuildBrush();
            Clear();
        }

        private void BuildBrush()
        {
            _diameter = brushRadius * 2 + 1;
            _mask = new float[_diameter * _diameter];
            _scratch = new Color32[_diameter * _diameter];

            for (int y = 0; y < _diameter; y++)
            {
                for (int x = 0; x < _diameter; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(brushRadius, brushRadius));
                    // 邊緣 1.5 像素漸淡，不會有鋸齒但也不會糊
                    _mask[y * _diameter + x] = Mathf.Clamp01((brushRadius - distance) / 1.5f + 1f);
                }
            }
        }

        private Vector2 ToTexture(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, eventData.position, eventData.pressEventCamera, out var local);
            var rect = _rt.rect;
            return new Vector2(
                (local.x - rect.xMin) / rect.width * _width,
                (local.y - rect.yMin) / rect.height * _height);
        }

        private void StampLine(Vector2 from, Vector2 to)
        {
            float step = Mathf.Max(1f, brushRadius * 0.45f);
            int count = Mathf.CeilToInt(Vector2.Distance(from, to) / step);
            for (int i = 1; i <= count; i++)
            {
                Stamp(Vector2.Lerp(from, to, (float)i / count));
            }
        }

        private void Stamp(Vector2 point)
        {
            int originX = Mathf.RoundToInt(point.x) - brushRadius;
            int originY = Mathf.RoundToInt(point.y) - brushRadius;

            int x0 = Mathf.Max(0, originX);
            int y0 = Mathf.Max(0, originY);
            int x1 = Mathf.Min(_width, originX + _diameter);
            int y1 = Mathf.Min(_height, originY + _diameter);
            if (x1 <= x0 || y1 <= y0) return;

            int blockWidth = x1 - x0;
            int blockHeight = y1 - y0;

            for (int by = 0; by < blockHeight; by++)
            {
                int maskY = y0 + by - originY;
                int row = (y0 + by) * _width;
                for (int bx = 0; bx < blockWidth; bx++)
                {
                    int maskX = x0 + bx - originX;
                    float a = _mask[maskY * _diameter + maskX];
                    int index = row + x0 + bx;
                    var dst = _buffer[index];

                    if (a > 0f)
                    {
                        // 透明處直接上色、已有顏色處混色，不然邊緣會出現黑邊
                        float mix = dst.a == 0 ? 1f : a;
                        var mixed = Color32.Lerp(dst, _color, mix);
                        mixed.a = (byte)Mathf.Max(dst.a, Mathf.RoundToInt(a * 255f));
                        dst = mixed;
                        _buffer[index] = dst;
                    }
                    _scratch[by * blockWidth + bx] = dst;
                }
            }

            _texture.SetPixels32(x0, y0, blockWidth, blockHeight, _scratch);
            _dirty = true;
        }
    }
}
