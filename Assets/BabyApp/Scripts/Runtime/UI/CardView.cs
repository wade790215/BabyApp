using System;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 畫面中央那張大卡：一張「？」蓋子蓋著一張圖和一個大大的名稱。
    /// 點蓋子 → 蓋子消失；點已經打開的卡 → 彈跳反應 + 重播聲音。
    /// 這裡只負責長相與動作，「點了要做什麼」由 DeckScreen 決定。
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Button tapTarget;
        [SerializeField] private CardDoor door;
        [SerializeField] private TapReaction reaction;

        /// <summary>卡片任何地方被點到（不管蓋子開著還是關著）。</summary>
        public event Action Tapped;

        /// <summary>蓋子消失完成，圖已經露出來。</summary>
        public event Action Revealed;

        public bool IsCovered => door != null && door.IsClosed;

        private CardEntry _card;

        // 名稱平常在卡片下方（上面是圖），沒有圖的時候要移到正中間
        private Vector2 _labelAnchorMin;
        private Vector2 _labelAnchorMax;
        private Vector2 _labelOffsetMin;
        private Vector2 _labelOffsetMax;

        private void Awake()
        {
            if (tapTarget != null) tapTarget.onClick.AddListener(() => Tapped?.Invoke());
            if (door != null) door.Opened += OnDoorOpened;

            if (nameLabel != null)
            {
                var rt = nameLabel.rectTransform;
                _labelAnchorMin = rt.anchorMin;
                _labelAnchorMax = rt.anchorMax;
                _labelOffsetMin = rt.offsetMin;
                _labelOffsetMax = rt.offsetMax;
            }
        }

        private void OnEnable() => AppSettings.Changed += RefreshName;
        private void OnDisable() => AppSettings.Changed -= RefreshName;

        public void Bind(CardEntry card)
        {
            _card = card;
            if (reaction != null) reaction.ResetState();

            bool hasImage = card.HasImage;

            if (image != null)
            {
                image.sprite = card.image;
                image.enabled = hasImage;
            }

            if (nameLabel != null)
            {
                RefreshName();

                // 還沒生圖的卡片讓名稱佔住整張卡，看起來才像一張卡而不是壞掉的畫面
                var rt = nameLabel.rectTransform;
                if (hasImage)
                {
                    rt.anchorMin = _labelAnchorMin;
                    rt.anchorMax = _labelAnchorMax;
                    rt.offsetMin = _labelOffsetMin;
                    rt.offsetMax = _labelOffsetMax;
                }
                else
                {
                    rt.anchorMin = new Vector2(0.1f, 0.3f);
                    rt.anchorMax = new Vector2(0.9f, 0.7f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                }
            }

            if (door != null)
            {
                // 每張卡都從蓋著開始，名稱也藏著，打開才一起出現
                door.Close();
                if (nameLabel != null) nameLabel.enabled = false;
            }
            else if (nameLabel != null)
            {
                nameLabel.enabled = true;
            }
        }

        /// <summary>把蓋子打開。已經開著就什麼都不做。</summary>
        public void Reveal()
        {
            if (!IsCovered) return;
            door.Open();
            if (VoiceService.Instance != null) VoiceService.Instance.PlayDoor();
        }

        /// <summary>播放「我被按到了」的隨機動畫與星星。</summary>
        public void PlayReaction()
        {
            if (reaction != null) reaction.Play();
        }

        private void RefreshName()
        {
            if (nameLabel != null && _card != null) nameLabel.text = _card.LocalizedName;
        }

        private void OnDoorOpened()
        {
            if (nameLabel != null) nameLabel.enabled = true;
            Revealed?.Invoke();
        }
    }
}
