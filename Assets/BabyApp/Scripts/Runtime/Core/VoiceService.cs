using System.Collections;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 統一的播音入口。語音、音效、音樂分三軌，點卡片時語音會立刻覆蓋上一句，
    /// 不會像小孩連點時那樣疊成一團。
    ///
    /// 一張卡的聲音是「先叫聲、再名稱」：汪汪！…狗狗。
    /// 叫聲是他最愛的部分，名稱接在後面才是語言學習。
    ///
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

        [Tooltip("叫聲最多播幾秒就接名稱，避免太長的音檔讓小孩等")]
        [SerializeField] private float maxSoundSeconds = 2.5f;

        private Coroutine _sequence;

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
        }

        private void OnEnable()
        {
            ApplySettings();
            AppSettings.Changed += ApplySettings;
        }

        private void OnDisable() => AppSettings.Changed -= ApplySettings;

        /// <summary>播一張卡：先叫聲，再名稱。任一段為 null 就略過那段。再點會從頭重來。</summary>
        public void PlayCard(AudioClip sound, AudioClip voice)
        {
            StopSequence();
            if (!AppSettings.VoiceEnabled) return;
            if (sound == null && voice == null) return;
            _sequence = StartCoroutine(CardSequence(sound, voice));
        }

        /// <summary>只念名稱，沒有叫聲。</summary>
        public void SpeakCard(AudioClip voice) => PlayCard(null, voice);

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

        private IEnumerator CardSequence(AudioClip sound, AudioClip voice)
        {
            if (sound != null)
            {
                voiceSource.Stop();
                voiceSource.clip = sound;
                voiceSource.Play();

                float wait = Mathf.Min(sound.length, maxSoundSeconds) + 0.12f;
                yield return new WaitForSecondsRealtime(wait);
            }

            if (voice != null)
            {
                voiceSource.Stop();
                voiceSource.clip = voice;
                voiceSource.Play();
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
