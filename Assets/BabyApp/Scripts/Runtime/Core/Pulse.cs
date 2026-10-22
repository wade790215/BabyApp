using System.Collections;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 被點到時放大再彈回來的回饋動畫。2～3 歲需要很明確的「我有按到」訊號。
    /// 不依賴任何動畫套件，連點時會從頭重播而不是互相打斷。
    /// </summary>
    public class Pulse : MonoBehaviour
    {
        [SerializeField] private float scaleUp = 1.12f;
        [SerializeField] private float duration = 0.28f;

        private Coroutine _running;
        private Vector3 _baseScale = Vector3.one;
        private bool _captured;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _captured = true;
        }

        public void Play()
        {
            if (!_captured) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                // 先彈出去再回來的曲線
                float curve = Mathf.Sin(p * Mathf.PI);
                transform.localScale = _baseScale * (1f + (scaleUp - 1f) * curve);
                yield return null;
            }
            transform.localScale = _baseScale;
            _running = null;
        }

        /// <summary>卡片換圖後重設，避免動畫中途切換造成殘留縮放。</summary>
        public void ResetScale()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
            if (_captured) transform.localScale = _baseScale;
        }
    }
}
