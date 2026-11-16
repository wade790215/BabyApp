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

        [SerializeField] private float breathAmount = 0.015f;

        [Tooltip("嚼東西時上下壓扁的幅度與速度（餵小熊用）")]
        [SerializeField] private float chewAmount = 0.05f;
        [SerializeField] private float chewSpeed = 22f;

        [Tooltip("多久沒人碰螢幕就招手說「來玩吧」；0 = 不要。放到 60 秒：小孩發呆也是在玩，不要一直催")]
        [SerializeField] private float idleNudgeSeconds = 60f;

        private RectTransform _rt;
        private Coroutine _hold;
        private Coroutine _jump;
        private float _breathPhase;
        private float _idle;
        private float _chewLeft;
        private Vector2 _jumpHome;
        private Vector2 _jumpLastSet;

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable()
        {
            Show(Pose.Idle);
            _idle = 0f;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _hold = null;
            // 跳到一半被關掉，先放回地面，不然下次打開會浮在半空
            if (_jump != null && _rt != null) _rt.anchoredPosition = _jumpHome;
            _jump = null;
            _chewLeft = 0f;
        }

        private void Update()
        {
            // 慢慢呼吸，活著的感覺
            _breathPhase += Time.unscaledDeltaTime * 1.4f;
            float s = 1f + Mathf.Sin(_breathPhase) * breathAmount;
            float sx = s, sy = 1f + (s - 1f) * 0.6f;

            // 嚼：上下壓扁、左右微微鼓出去。這裡每幀都會覆寫縮放，所以嚼只能併在這裡做
            if (_chewLeft > 0f)
            {
                _chewLeft -= Time.unscaledDeltaTime;
                float k = Mathf.Abs(Mathf.Sin(_chewLeft * chewSpeed)) * chewAmount;
                sx *= 1f + k * 0.6f;
                sy *= 1f - k;
            }
            _rt.localScale = new Vector3(sx, sy, 1f);

            // 小孩分心太久，小熊跳一下、喊一聲把他叫回來
            if (idleNudgeSeconds <= 0f) return;
            if (AnyPointerDown()) _idle = 0f;
            else _idle += Time.unscaledDeltaTime;

            if (_idle >= idleNudgeSeconds)
            {
                _idle = 0f;
                React(Pose.Happy, 1.6f);
                if (Core.VoiceService.Instance != null && Core.PromptLibrary.Instance != null &&
                    !Core.VoiceService.Instance.IsSpeaking)
                {
                    Core.VoiceService.Instance.PlaySequence(Core.PromptLibrary.Instance.ComeOn);
                }
            }
        }

        private static bool AnyPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            var pointer = UnityEngine.InputSystem.Pointer.current;
            return pointer != null && pointer.press.isPressed;
#else
            return Input.GetMouseButton(0) || Input.touchCount > 0;
#endif
        }

        /// <summary>換個表情，seconds 秒後回到待機；順便跳一下。</summary>
        public void React(Pose pose, float seconds = 1.4f, bool jump = true)
        {
            _idle = 0f;
            Show(pose);
            if (_hold != null) StopCoroutine(_hold);
            _hold = StartCoroutine(BackToIdle(seconds));
            if (jump) Jump();
        }

        /// <summary>嚼幾秒（咔滋咔滋）。只動縮放，不換表情，可以跟 React 一起用。</summary>
        public void Chew(float seconds)
        {
            _idle = 0f;
            _chewLeft = Mathf.Max(_chewLeft, seconds);
        }

        public void Jump()
        {
            _idle = 0f;
            // 落地點只在「沒在跳」的時候記，連點時在半空被再叫一次才不會越跳越高
            if (_jump == null) _jumpHome = _rt.anchoredPosition;
            else StopCoroutine(_jump);
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
            // 從 _jumpHome 跳起來再回到 _jumpHome（牠可能被拖到別處了，所以不用固定座標）。
            // 小幅慢跳，像點頭，不是彈跳床。
            const float duration = 0.55f;
            const float height = 16f;
            _jumpLastSet = _rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                // 位置不是我們上一幀放的 → 正在被拖（DraggableWidget），放棄這一跳、不要把牠拉回原地
                if (_rt.anchoredPosition != _jumpLastSet)
                {
                    _jump = null;
                    yield break;
                }
                _jumpLastSet = _jumpHome + Vector2.up * (Mathf.Sin(p * Mathf.PI) * height);
                _rt.anchoredPosition = _jumpLastSet;
                yield return null;
            }
            _rt.anchoredPosition = _jumpHome;
            _jump = null;
        }
    }
}
