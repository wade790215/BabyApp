using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.Core
{
    /// <summary>
    /// 點卡片時的回饋：四種動畫隨機挑一種（不會連續兩次一樣），再噴一圈星星。
    /// 2～3 歲會把同一張卡連點二十次，每次反應不同他才會一直期待「這次會怎樣」。
    /// </summary>
    public class TapReaction : MonoBehaviour
    {
        private enum Kind { Bounce, Wiggle, Spin, Squash }

        [Header("星星")]
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private Color sparkColor = new Color32(0xFF, 0xC1, 0x07, 0xFF);
        [SerializeField] private int sparkCount = 8;
        [SerializeField] private float sparkStartRadius = 300f;

        private RectTransform _rt;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Coroutine _running;
        private Kind _last;
        private bool _hasLast;

        private readonly List<GameObject> _sparks = new List<GameObject>();

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _baseScale = _rt.localScale;
            _baseRotation = _rt.localRotation;
        }

        private void OnDisable()
        {
            // 畫面被關掉時把飛到一半的星星清掉，不然回來會看到一堆凍住的黃點
            foreach (var spark in _sparks)
            {
                if (spark != null) Destroy(spark);
            }
            _sparks.Clear();
            ResetState();
        }

        public void Play()
        {
            Kind kind;
            do
            {
                kind = (Kind)Random.Range(0, 4);
            } while (_hasLast && kind == _last);
            _last = kind;
            _hasLast = true;

            ResetState();
            _running = StartCoroutine(Animate(kind));
            SpawnSparks();
        }

        /// <summary>換卡時重設，避免動畫中途切換造成殘留的縮放或旋轉。</summary>
        public void ResetState()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
            if (_rt != null)
            {
                _rt.localScale = _baseScale;
                _rt.localRotation = _baseRotation;
            }
        }

        private IEnumerator Animate(Kind kind)
        {
            float duration;
            switch (kind)
            {
                case Kind.Bounce: duration = 0.3f; break;
                case Kind.Wiggle: duration = 0.45f; break;
                case Kind.Spin: duration = 0.5f; break;
                default: duration = 0.4f; break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);

                switch (kind)
                {
                    case Kind.Bounce:
                        _rt.localScale = _baseScale * (1f + 0.14f * Mathf.Sin(p * Mathf.PI));
                        break;

                    case Kind.Wiggle:
                        // 左右搖頭，幅度隨時間收斂
                        float angle = Mathf.Sin(p * Mathf.PI * 4f) * 8f * (1f - p);
                        _rt.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
                        break;

                    case Kind.Spin:
                        float eased = 1f - Mathf.Pow(1f - p, 3f);
                        _rt.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, 360f * eased);
                        break;

                    case Kind.Squash:
                        // 先壓扁再拉長再回正，像果凍
                        float wave = Mathf.Sin(p * Mathf.PI * 2f) * 0.15f * (1f - p);
                        _rt.localScale = new Vector3(
                            _baseScale.x * (1f + wave),
                            _baseScale.y * (1f - wave),
                            _baseScale.z);
                        break;
                }
                yield return null;
            }

            _rt.localScale = _baseScale;
            _rt.localRotation = _baseRotation;
            _running = null;
        }

        private void SpawnSparks()
        {
            if (sparkSprite == null) return;
            var parent = _rt.parent as RectTransform;
            if (parent == null) return;

            for (int i = 0; i < sparkCount; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.SetAsLastSibling(); // 星星要蓋在卡片上面
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(40f, 40f);

                var image = go.GetComponent<Image>();
                image.sprite = sparkSprite;
                image.color = sparkColor;
                image.raycastTarget = false;

                float angle = (360f / sparkCount) * i + Random.Range(-15f, 15f);
                var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var start = _rt.anchoredPosition + dir * sparkStartRadius;
                var velocity = dir * Random.Range(260f, 420f);

                _sparks.Add(go);
                StartCoroutine(FlySpark(rt, image, start, velocity));
            }
        }

        private IEnumerator FlySpark(RectTransform rt, Image image, Vector2 start, Vector2 velocity)
        {
            const float duration = 0.5f;
            rt.anchoredPosition = start;
            var color = image.color;

            float t = 0f;
            while (t < duration)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / duration);

                rt.anchoredPosition += velocity * dt;
                rt.localScale = Vector3.one * (1f - p);
                color.a = 1f - p;
                image.color = color;
                yield return null;
            }

            _sparks.Remove(rt.gameObject);
            Destroy(rt.gameObject);
        }
    }
}
