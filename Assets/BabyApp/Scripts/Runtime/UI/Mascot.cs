using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 蜂蜜小熊：每個畫面角落都有牠，平常慢慢呼吸，有事發生就換表情、跳一下，過一會兒回到待機。
    /// 小孩是在跟牠互動，不是在跟一個軟體互動。
    /// 沒有的表情圖會退回待機圖，所以五張圖可以慢慢補。
    /// </summary>
    public class Mascot : MonoBehaviour
    {
        public enum Pose { Idle, Happy, Surprised, Think, Sleep }

        [SerializeField] private Image image;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite happy;
        [SerializeField] private Sprite surprised;
        [SerializeField] private Sprite think;
        [SerializeField] private Sprite sleep;

        [SerializeField] private float breathAmount = 0.025f;

        private RectTransform _rt;
        private Coroutine _hold;
        private Coroutine _jump;
        private float _breathPhase;

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable() => Show(Pose.Idle);

        private void OnDisable()
        {
            StopAllCoroutines();
            _hold = null;
            _jump = null;
        }

        private void Update()
        {
            // 慢慢呼吸，活著的感覺
            _breathPhase += Time.unscaledDeltaTime * 2.2f;
            float s = 1f + Mathf.Sin(_breathPhase) * breathAmount;
            _rt.localScale = new Vector3(s, 1f + (s - 1f) * 0.6f, 1f);
        }

        /// <summary>換個表情，seconds 秒後回到待機；順便跳一下。</summary>
        public void React(Pose pose, float seconds = 1.4f, bool jump = true)
        {
            Show(pose);
            if (_hold != null) StopCoroutine(_hold);
            _hold = StartCoroutine(BackToIdle(seconds));
            if (jump) Jump();
        }

        public void Jump()
        {
            if (_jump != null) StopCoroutine(_jump);
            _jump = StartCoroutine(JumpRoutine());
        }

        private void Show(Pose pose)
        {
            if (image == null) return;
            Sprite sprite;
            switch (pose)
            {
                case Pose.Happy: sprite = happy; break;
                case Pose.Surprised: sprite = surprised; break;
                case Pose.Think: sprite = think; break;
                case Pose.Sleep: sprite = sleep; break;
                default: sprite = idle; break;
            }
            if (sprite == null) sprite = idle;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private IEnumerator BackToIdle(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Show(Pose.Idle);
            _hold = null;
        }

        private IEnumerator JumpRoutine()
        {
            // 從現在的位置跳起來再回到現在的位置（牠可能被拖到別處了）
            const float duration = 0.4f;
            const float height = 40f;
            var home = _rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                _rt.anchoredPosition = home + Vector2.up * (Mathf.Sin(p * Mathf.PI) * height);
                yield return null;
            }
            _rt.anchoredPosition = home;
            _jump = null;
        }
    }
}
