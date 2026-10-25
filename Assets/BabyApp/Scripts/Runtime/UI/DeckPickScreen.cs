using System;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;

namespace BabyApp.UI
{
    /// <summary>
    /// 選主題：首頁按「卡片」之後進來，每個卡片組一格（封面圖 + 主題色），排成正方形格子。
    /// 首頁只放四大功能，主題多了也不會把首頁擠滿；新增主題不用改程式。
    /// </summary>
    public class DeckPickScreen : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private DeckButton buttonPrefab;
        [SerializeField] private HoldButton homeButton;
        [SerializeField] private Mascot mascot;

        public event Action<CardDeck> DeckChosen;
        public event Action HomeRequested;

        private readonly List<DeckButton> _spawned = new List<DeckButton>();
        private bool _built;

        private void Awake()
        {
            if (homeButton != null) homeButton.Triggered += () => HomeRequested?.Invoke();
        }

        private void OnEnable()
        {
            if (!_built) Build();
        }

        private void Build()
        {
            if (buttonContainer == null || buttonPrefab == null)
            {
                Debug.LogError("[BabyApp] DeckPickScreen 還沒接好 buttonContainer / buttonPrefab。");
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
