using System;
using BabyApp.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 對講機式的錄音鈕：按住就錄、放開就存。錄的時候面變紅、外圈跟著錄音長度長出來。
    /// </summary>
    public class RecordButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image progressRing;
        [SerializeField] private Image face;
        [SerializeField] private Color idleColor = new Color32(0xFF, 0x9F, 0x68, 0xFF);
        [SerializeField] private Color recordingColor = new Color32(0xFF, 0x3B, 0x30, 0xFF);

        /// <summary>開始錄了（用來把正在播的聲音停掉）。</summary>
        public event Action Started;

        /// <summary>錄完了，這是切好的聲音。</summary>
        public event Action<AudioClip> Recorded;

        private bool _holding;

        private void OnEnable()
        {
            if (face != null) face.color = idleColor;
            if (progressRing != null) progressRing.fillAmount = 0f;
        }

        private void Update()
        {
            if (!_holding) return;

            float progress = VoiceRecorder.Instance != null ? VoiceRecorder.Instance.Progress : 0f;
            if (progressRing != null) progressRing.fillAmount = progress;
            if (progress >= 1f) Finish();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (VoiceRecorder.Instance == null || !VoiceRecorder.Instance.StartRecording()) return;
            _holding = true;
            if (face != null) face.color = recordingColor;
            Started?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => Finish();
        public void OnPointerExit(PointerEventData eventData) => Finish();

        private void Finish()
        {
            if (!_holding) return;
            _holding = false;

            var clip = VoiceRecorder.Instance != null ? VoiceRecorder.Instance.StopRecording() : null;

            if (face != null) face.color = idleColor;
            if (progressRing != null) progressRing.fillAmount = 0f;

            if (clip != null) Recorded?.Invoke(clip);
        }
    }
}
