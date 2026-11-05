using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 家長頁裡的一格：照片（或佔位圖示）、稱呼、狀態一行字，下面四顆鈕：
    /// 選照片、按住錄稱呼、聽一次、按住刪除。改了就直接存，沒有「儲存」鍵。
    /// </summary>
    public class FamilySlotView : MonoBehaviour
    {
        [SerializeField] private Image photo;
        [SerializeField] private Image placeholder;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button pickButton;
        [SerializeField] private RecordButton recordButton;
        [SerializeField] private Button playButton;
        [SerializeField] private HoldButton deleteButton;

        private int _index = -1;
        private bool _wired;

        public void Bind(int index)
        {
            _index = index;
            WireOnce();
            Refresh();
        }

        private void OnEnable()
        {
            AppSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => AppSettings.Changed -= Refresh;

        private void WireOnce()
        {
            if (_wired) return;
            _wired = true;

            if (pickButton != null) pickButton.onClick.AddListener(PickPhoto);
            if (playButton != null) playButton.onClick.AddListener(PlayVoice);

            if (recordButton != null)
            {
                recordButton.Started += () =>
                {
                    if (VoiceService.Instance != null) VoiceService.Instance.StopVoice();
                };
                recordButton.Recorded += OnRecorded;
            }

            if (deleteButton != null)
            {
                deleteButton.Triggered += () =>
                {
                    if (Library == null) return;
                    Library.Delete(_index);
                    Tap();
                    Refresh();
                };
            }
        }

        private static FamilyLibrary Library => FamilyLibrary.Instance;

        private void PickPhoto()
        {
            if (Library == null || PhotoPicker.IsBusy) return;
            Tap();
            PhotoPicker.Pick(texture =>
            {
                if (texture == null || this == null) return;
                Library.SavePhoto(_index, texture);
                Destroy(texture);
                Refresh();
            });
        }

        /// <summary>錄完就存起來並馬上播給家長聽，確認錄到了。</summary>
        private void OnRecorded(AudioClip clip)
        {
            if (Library == null || clip == null) return;
            Library.SaveVoice(_index, clip);
            if (VoiceService.Instance != null) VoiceService.Instance.SpeakCard(clip);
            Refresh();
        }

        private void PlayVoice()
        {
            if (Library == null || VoiceService.Instance == null) return;
            var clip = Library.Voice(_index);
            if (clip == null) clip = Library.FallbackVoice(_index);
            if (clip == null) return;
            VoiceService.Instance.SpeakCard(clip);
        }

        public void Refresh()
        {
            if (_index < 0 || Library == null) return;

            bool hasPhoto = Library.HasPhoto(_index);
            bool hasVoice = Library.HasVoice(_index);
            bool fixedSlot = _index < FamilyLibrary.FixedSlots;

            if (photo != null)
            {
                photo.sprite = Library.Photo(_index);
                photo.enabled = hasPhoto;
            }
            if (placeholder != null) placeholder.enabled = !hasPhoto;

            if (nameLabel != null) nameLabel.text = Library.SlotLabel(_index);

            if (statusLabel != null)
            {
                string key;
                if (!hasPhoto) key = "family_need_photo";
                else if (hasVoice) key = "family_ready";
                else key = fixedSlot ? "family_builtin_voice" : "family_need_voice";
                statusLabel.text = Strings.Get(key);
            }

            // 空格子沒東西可刪、也沒東西可聽（自訂槽沒錄音就沒聲音）
            if (deleteButton != null) deleteButton.gameObject.SetActive(hasPhoto || hasVoice);
            if (playButton != null) playButton.gameObject.SetActive(hasVoice || fixedSlot);
        }

        private static void Tap()
        {
            if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
        }
    }
}
