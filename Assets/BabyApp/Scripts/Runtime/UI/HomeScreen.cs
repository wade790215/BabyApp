using System;
using BabyApp.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 首頁：七個大格——卡片、摸一摸、找一找、數數、配對、拼圖、畫畫，自動排 4×2。
    /// 主題（動物／水果…）不在這裡選，按「卡片」進去再選，首頁才不會塞滿格子讓 2 歲半選擇困難。
    /// 角落一顆要長按的「設」給家長進設定，小熊在旁邊陪著。
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] private Button cardsButton;
        [SerializeField] private Button touchButton;
        [SerializeField] private Button findButton;
        [SerializeField] private Button countButton;
        [SerializeField] private Button matchButton;
        [SerializeField] private Button puzzleButton;
        [SerializeField] private Button paintButton;
        [SerializeField] private HoldButton settingsButton;
        [SerializeField] private Mascot mascot;

        public event Action CardsChosen;
        public event Action TouchChosen;
        public event Action FindChosen;
        public event Action CountChosen;
        public event Action MatchChosen;
        public event Action PuzzleChosen;
        public event Action PaintChosen;
        public event Action SettingsChosen;

        private void Awake()
        {
            Hook(cardsButton, () => CardsChosen?.Invoke());
            Hook(touchButton, () => TouchChosen?.Invoke());
            Hook(findButton, () => FindChosen?.Invoke());
            Hook(countButton, () => CountChosen?.Invoke());
            Hook(matchButton, () => MatchChosen?.Invoke());
            Hook(puzzleButton, () => PuzzleChosen?.Invoke());
            Hook(paintButton, () => PaintChosen?.Invoke());
            if (settingsButton != null) settingsButton.Triggered += () => SettingsChosen?.Invoke();
        }

        private void Hook(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                if (VoiceService.Instance != null) VoiceService.Instance.PlayTap();
                if (mascot != null) mascot.React(Mascot.Pose.Happy, 0.8f);
                action();
            });
        }
    }
}
