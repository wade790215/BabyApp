using System.Collections;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 點卡片時的回饋：三種輕微動畫（彈一下、搖一搖、壓一壓）隨機挑一種，不會連續兩次一樣，不噴星星。
    /// 2～3 歲會把同一張卡連點二十次，每次反應不同他才會一直期待「這次會怎樣」；
    /// 但幅度要小、速度要慢，是「東西回應了我」，不是「放煙火獎勵我」（Pok Pok 的安靜手感）。
    /// </summary>
    public class TapReaction : MonoBehaviour
    {
        private enum Kind { Bounce, Wiggle, Squash }

        private RectTransform _rt;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Coroutine _running;
        private Kind _last;
        private bool _hasLast;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _baseScale = _rt.localScale;
            _baseRotation = _rt.localRotation;
        }

        private void OnDisable() => ResetState();

        public void Play()
        {
            Kind kind;
            do
            {
                kind = (Kind)Random.Range(0, 3);
            } while (_hasLast && kind == _last);
            _last = kind;
            _hasLast = true;

            ResetState();
            _running = StartCoroutine(Animate(kind));
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
                case Kind.Bounce: duration = 0.45f; break;
                case Kind.Wiggle: duration = 0.6f; break;
                default: duration = 0.55f; break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);

                switch (kind)
                {
                    case Kind.Bounce:
                        _rt.localScale = _baseScale * (1f + 0.06f * Mathf.Sin(p * Mathf.PI));
                        break;

                    case Kind.Wiggle:
                        // 左右搖頭，幅度隨時間收斂
                        float angle = Mathf.Sin(p * Mathf.PI * 3f) * 4f * (1f - p);
                        _rt.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
                        break;

                    case Kind.Squash:
                        // 先壓扁再拉長再回正，像果凍
                        float wave = Mathf.Sin(p * Mathf.PI * 2f) * 0.06f * (1f - p);
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
    }
}
