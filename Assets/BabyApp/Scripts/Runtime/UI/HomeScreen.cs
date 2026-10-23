using System;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 首頁：每個卡片組一格，後面接「找一找」「數數」「畫畫」三格，排成正方形格子。
    /// 角落一顆要長按的「設」給家長進設定，小熊在旁邊陪著。
    /// 新增主題不用改這支程式，也不用改場景。
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private DeckButton buttonPrefab;
        [SerializeField] private Button findButton;
        [SerializeField] private Button countButton;
        [SerializeField] private Button paintButton;
        [SerializeField] private HoldButton settingsButton;
        [SerializeField] private Mascot mascot;

        public event Action<CardDeck> DeckChosen;
        public event Action FindChosen;
        public event Action CountChosen;
        public event Action PaintChosen;
        public event Action SettingsChosen;

        private readonly List<DeckButton> _spawned = new List<DeckButton>();
        private bool _built;

        private void Awake()
        {
            Hook(findButton, () => FindChosen?.Invoke());
            Hook(countButton, () => CountChosen?.Invoke());
            Hook(paintButton, () => PaintChosen?.Invoke());
            if (settingsButton != null) settingsButton.Triggered += () => SettingsChosen?.Invoke();
        }

        private void OnEnable()
        {
            if (!_built) Build();
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

        private void Build()
        {
            if (buttonContainer == null || buttonPrefab == null)
            {
                Debug.LogError("[BabyApp] HomeScreen 還沒接好 buttonContainer / buttonPrefab。");
                return;
            }

            foreach (var old in _spawned)
            {
                if (old != null) Destroy(old.gameObject);
            }
            _spawned.Clear();

            foreach (var deck in DeckLibrary.All)
            {
                var button = Instantiate(buttonPrefab, buttonContainer);
                button.gameObject.SetActive(true);
                button.name = $"DeckButton_{deck.deckId}";
                button.Setup(deck, d =>
                {
                    if (mascot != null) mascot.React(Mascot.Pose.Happy, 0.8f);
                    DeckChosen?.Invoke(d);
                });
                _spawned.Add(button);
            }

            // 固定的三格排在卡片組後面
            if (findButton != null) findButton.transform.SetAsLastSibling();
            if (countButton != null) countButton.transform.SetAsLastSibling();
            if (paintButton != null) paintButton.transform.SetAsLastSibling();

            _built = true;
        }

        /// <summary>素材更新後重建按鈕（Editor 測試用）。</summary>
        public void Rebuild()
        {
            DeckLibrary.Invalidate();
            _built = false;
            Build();
        }
    }
}
