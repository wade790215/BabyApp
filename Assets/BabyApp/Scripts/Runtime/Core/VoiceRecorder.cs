using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace BabyApp.Core
{
    /// <summary>
    /// 用麥克風錄一小段聲音。按住錄、放開停，最多幾秒；停下來時只保留真的錄到的長度。
    /// WebGL 沒有麥克風 API，會回報不可用而不是爆掉。
    /// </summary>
    public class VoiceRecorder : MonoBehaviour
    {
        public static VoiceRecorder Instance { get; private set; }

        [Tooltip("一段最多錄幾秒")]
        [SerializeField] private int maxSeconds = 4;

        private const int Rate = 22050;

        private AudioClip _buffer;
        private float _startTime;

        public bool IsRecording { get; private set; }
        public int MaxSeconds => maxSeconds;

        /// <summary>0～1，錄到最大長度就是 1。</summary>
        public float Progress => IsRecording ? Mathf.Clamp01((Time.unscaledTime - _startTime) / maxSeconds) : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public bool IsAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return Microphone.devices.Length > 0;
#endif
            }
        }

        /// <summary>開始錄。沒有麥克風或還沒拿到權限時回 false（Android 會順便跳出權限詢問）。</summary>
        public bool StartRecording()
        {
            if (IsRecording) return true;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                return false; // 這次先讓他按允許，下一次按就能錄
            }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            if (!IsAvailable)
            {
                Debug.LogWarning("[BabyApp] 找不到麥克風，無法錄音。");
                return false;
            }

            _buffer = Microphone.Start(null, false, maxSeconds, Rate);
            if (_buffer == null) return false;

            _startTime = Time.unscaledTime;
            IsRecording = true;
            return true;
#endif
        }

        /// <summary>停下來並把錄到的部分切出來。太短（不到 0.15 秒）視為誤觸，回 null。</summary>
        public AudioClip StopRecording()
        {
            if (!IsRecording) return null;
            IsRecording = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            return null;
#else
            int position = Microphone.GetPosition(null);
            Microphone.End(null);

            if (_buffer == null || position < Rate * 0.15f) return null;
            position = Mathf.Min(position, _buffer.samples);

            var data = new float[position * _buffer.channels];
            _buffer.GetData(data, 0);

            var clip = AudioClip.Create("recording", position, _buffer.channels, _buffer.frequency, false);
            clip.SetData(data, 0);
            return clip;
#endif
        }
    }
}
