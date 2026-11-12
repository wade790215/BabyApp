using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 統一的播音入口。語音、音效分兩軌，點卡片時語音會立刻覆蓋上一句，
    /// 不會像小孩連點時那樣疊成一團。
    ///
    /// 語音軌一次播一串（叫聲 → 名稱，或 名稱 → 「在哪裡？」）。語音一定念完才接下一段；
    /// 叫聲用 <see cref="Sound"/> 包起來，最多播 <see cref="maxClipSeconds"/> 秒就接下一段（鴨子那種 80 秒的不能讓小孩等）。
    /// 畫面要等小熊講完再換題，用 <see cref="WaitForSpeech"/>，不要猜秒數。
    /// 兩軌各自看設定裡的開關。沒有背景音樂：Pok Pok 也不放配樂，而且音樂會蓋掉名稱語音。
    /// 音效只有五個：tap（點按鈕）、page（翻卡）、door（掀蓋）、correct（答對／拼完／數完）、chew（小熊咔滋），都是低、短、圓的木頭聲。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VoiceService : MonoBehaviour
    {
        public static VoiceService Instance { get; private set; }

        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("介面音效（可留空）")]
        [SerializeField] private AudioClip tapSfx;
        [SerializeField] private AudioClip pageSfx;
        [SerializeField] private AudioClip doorSfx;
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip chewSfx;

        [Tooltip("叫聲最多播幾秒就接下一段，避免太長的叫聲讓小孩等；語音不受限，一定念完")]
        [SerializeField] private float maxClipSeconds = 2.5f;

        /// <summary>一串裡的一段：語音（念完為止）或叫聲（最多 maxClipSeconds 秒）。</summary>
        public readonly struct Segment
        {
            public readonly AudioClip Clip;
            public readonly bool Capped;

            public Segment(AudioClip clip, bool capped)
            {
                Clip = clip;
                Capped = capped;
            }

            /// <summary>沒特別說的都當語音，一定念完。</summary>
            public static implicit operator Segment(AudioClip clip) => new Segment(clip, false);
        }

        /// <summary>把叫聲標成「最多播 maxClipSeconds 秒」，串在語音裡用：PlaySequence(Correct, Sound(bark), name)。</summary>
        public static Segment Sound(AudioClip clip) => new Segment(clip, true);

        private Coroutine _sequence;

        /// <summary>語音軌是不是還在播一串。</summary>
        public bool IsSpeaking => _sequence != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            voiceSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.45f;

            // 場景裡沒有耳朵的話什麼都聽不到；自己補一個，省得場景少接一個元件就全靜音
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.LogWarning("[BabyApp] 場景裡沒有 AudioListener，已自動加在 AppRoot 上。");
            }
        }

        private void OnEnable()
        {
            ApplySettings();
            AppSettings.Changed += ApplySettings;
        }

        private void OnDisable() => AppSettings.Changed -= ApplySettings;

        /// <summary>播一串語音，每段都念完，null 的段落自動跳過。再叫一次會從頭換成新的一串。</summary>
        public void PlaySequence(params AudioClip[] clips)
        {
            if (clips == null)
            {
                StopSequence();
                return;
            }
            var segments = new Segment[clips.Length];
            for (int i = 0; i < clips.Length; i++) segments[i] = clips[i];
            PlaySequence(segments);
        }

        /// <summary>播一串，裡面可以混叫聲（用 <see cref="Sound"/> 包）與語音。</summary>
        public void PlaySequence(params Segment[] segments)
        {
            StopSequence();
            if (!AppSettings.VoiceEnabled || segments == null) return;

            var list = new List<Segment>();
            foreach (var segment in segments)
            {
                if (segment.Clip != null) list.Add(segment);
            }
            if (list.Count == 0) return;

            _sequence = StartCoroutine(Sequence(list));
        }

        /// <summary>播一張卡：先叫聲（最多幾秒），再名稱。</summary>
        public void PlayCard(AudioClip sound, AudioClip voice) => PlaySequence(Sound(sound), voice);

        /// <summary>
        /// 給畫面的 coroutine 用：小熊還在講就等它講完（最多 timeout 秒），講完再停 pauseAfter 秒。
        /// 一開始就沒在講的話立刻回來，不多等。用法：yield return VoiceService.WaitForSpeech(0.5f);
        /// </summary>
        public static IEnumerator WaitForSpeech(float pauseAfter = 0f, float timeout = 8f)
        {
            if (Instance == null || !Instance.IsSpeaking) yield break;
            float waited = 0f;
            while (Instance != null && Instance.IsSpeaking && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (pauseAfter > 0f) yield return new WaitForSecondsRealtime(pauseAfter);
        }

        /// <summary>只念名稱，沒有叫聲。</summary>
        public void SpeakCard(AudioClip voice) => PlaySequence(voice);

        /// <summary>把正在播的那一串整個停掉（換卡、開始錄音時用）。音效不受影響。</summary>
        public void StopVoice() => StopSequence();

        public void PlayTap() => PlaySfx(tapSfx);
        public void PlayPage() => PlaySfx(pageSfx);
        public void PlayDoor() => PlaySfx(doorSfx);
        /// <summary>答對、拼完、數完：一聲木琴。每個畫面一回合只響一次，不要每點一下都響。</summary>
        public void PlayCorrect() => PlaySfx(correctSfx);
        public void PlayChew() => PlaySfx(chewSfx);

        private void ApplySettings()
        {
            if (!AppSettings.VoiceEnabled) StopSequence();
        }

        private IEnumerator Sequence(List<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var clip = segments[i].Clip;
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.Play();

                bool last = i == segments.Count - 1;
                // 語音檔頭尾靜音都剪掉了，念多久就等多久；叫聲才砍
                float wait = segments[i].Capped ? Mathf.Min(clip.length, maxClipSeconds) : clip.length;
                yield return new WaitForSecondsRealtime(wait + (last ? 0f : 0.12f));
            }
            _sequence = null;
        }

        private void StopSequence()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }
            voiceSource.Stop();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || !AppSettings.SfxEnabled) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}
