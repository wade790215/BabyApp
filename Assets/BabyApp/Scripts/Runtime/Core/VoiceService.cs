using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 統一的播音入口。語音、音效、音樂分三軌，點卡片時語音會立刻覆蓋上一句，
    /// 不會像小孩連點時那樣疊成一團。
    ///
    /// 語音軌一次播一串（叫聲 → 名稱，或 名稱 → 「在哪裡？」），每段最多幾秒就接下一段。
    /// 三軌各自看設定裡的開關；音樂欄位留空就什麼都不會播。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VoiceService : MonoBehaviour
    {
        public static VoiceService Instance { get; private set; }

        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("介面音效（可留空）")]
        [SerializeField] private AudioClip tapSfx;
        [SerializeField] private AudioClip pageSfx;
        [SerializeField] private AudioClip doorSfx;
        [SerializeField] private AudioClip sparkleSfx;

        [Header("背景音樂（可留空）")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;

        [Tooltip("一段最多播幾秒就接下一段，避免太長的叫聲讓小孩等")]
        [SerializeField] private float maxClipSeconds = 2.5f;

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
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

            voiceSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.6f;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

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

        /// <summary>播一串聲音，null 的段落自動跳過。再叫一次會從頭換成新的一串。</summary>
        public void PlaySequence(params AudioClip[] clips)
        {
            StopSequence();
            if (!AppSettings.VoiceEnabled || clips == null) return;

            var list = new List<AudioClip>();
            foreach (var clip in clips)
            {
                if (clip != null) list.Add(clip);
            }
            if (list.Count == 0) return;

            _sequence = StartCoroutine(Sequence(list));
        }

        /// <summary>播一張卡：先叫聲，再名稱。</summary>
        public void PlayCard(AudioClip sound, AudioClip voice) => PlaySequence(sound, voice);

        /// <summary>只念名稱，沒有叫聲。</summary>
        public void SpeakCard(AudioClip voice) => PlaySequence(voice);

        /// <summary>把正在播的那一串整個停掉（換卡、開始錄音時用）。音效不受影響。</summary>
        public void StopVoice() => StopSequence();

        public void PlayTap() => PlaySfx(tapSfx);
        public void PlayPage() => PlaySfx(pageSfx);
        public void PlayDoor() => PlaySfx(doorSfx);
        public void PlaySparkle() => PlaySfx(sparkleSfx);

        private void ApplySettings()
        {
            musicSource.volume = musicVolume;
            bool wantMusic = AppSettings.MusicEnabled && musicClip != null;

            if (wantMusic && !musicSource.isPlaying)
            {
                musicSource.clip = musicClip;
                musicSource.Play();
            }
            else if (!wantMusic && musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            if (!AppSettings.VoiceEnabled) StopSequence();
        }

        private IEnumerator Sequence(List<AudioClip> clips)
        {
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                voiceSource.Stop();
                voiceSource.clip = clip;
                voiceSource.Play();

                bool last = i == clips.Count - 1;
                float wait = Mathf.Min(clip.length, maxClipSeconds) + (last ? 0f : 0.12f);
                yield return new WaitForSecondsRealtime(wait);
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
