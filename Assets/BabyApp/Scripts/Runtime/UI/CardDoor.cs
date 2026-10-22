using System;
using System.Collections;
using UnityEngine;

namespace BabyApp.UI
{
    /// <summary>
    /// 蓋在卡片上的「？」蓋子。每張卡進場都是蓋著的，要他自己點開才會看到裡面是什麼。
    /// 這是躲貓貓的機制：「翻頁」從被動變成主動，圖是他打開的，不是我們給他看的。
    ///
    /// 點了蓋子會縮小淡出。蓋著的時候每隔幾秒輕輕晃一下，暗示「這裡可以點」。
    /// </summary>
    public class CardDoor : MonoBehaviour
    {
        [SerializeField] private RectTransform cover;
        [SerializeField] private CanvasGroup group;

        [SerializeField] private float openSeconds = 0.3f;

        [Tooltip("蓋著時每隔幾秒晃一下提示可以點")]
        [SerializeField] private float nudgeEvery = 2.5f;

        public bool IsClosed { get; private set; }

        /// <summary>蓋子完全消失、圖已經整個露出來時觸發。</summary>
        public event Action Opened;

        private float _idle;

        private void Update()
        {
            if (!IsClosed) return;

            _idle += Time.unscaledDeltaTime;
            if (_idle >= nudgeEvery)
            {
                _idle = 0f;
                StartCoroutine(Nudge());
            }
        }

        public void Close()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            if (cover != null) cover.localScale = Vector3.one;
            if (group != null) group.alpha = 1f;
            IsClosed = true;
            _idle = 0f;
        }

        public void Open()
        {
            if (!IsClosed) return;
            IsClosed = false;
            StopAllCoroutines();
            StartCoroutine(Shrink());
        }

        private IEnumerator Shrink()
        {
            float t = 0f;
            while (t < openSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / openSeconds);
                // 先微微放大再縮到沒有，像泡泡破掉
                float scale = p < 0.25f ? 1f + 0.08f * (p / 0.25f) : 1.08f * (1f - (p - 0.25f) / 0.75f);
                if (cover != null) cover.localScale = Vector3.one * Mathf.Max(0f, scale);
                if (group != null) group.alpha = 1f - Mathf.Pow(p, 2f);
                yield return null;
            }

            // 先通知再關掉自己：關掉會停掉這個 coroutine，通知放後面就不會發出去
            Opened?.Invoke();
            gameObject.SetActive(false);
        }

        private IEnumerator Nudge()
        {
            const float duration = 0.3f;
            float t = 0f;
            while (t < duration && IsClosed)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                if (cover != null) cover.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            if (cover != null && IsClosed) cover.localScale = Vector3.one;
        }
    }
}
