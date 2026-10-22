using System;
using System.Collections.Generic;
using BabyApp.Core;
using BabyApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 首頁：把 Resources/Decks 裡的每個卡片組排成一排大按鈕，最後再接一顆「畫畫」。
    /// 角落一顆要長按的「設」給家長進設定。
    /// 新增主題不用改這支程式，也不用改場景。
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private DeckButton buttonPrefab;
        [SerializeField] private Button paintButton;
        [SerializeField] private HoldButton settingsButton;

        public event Action<CardDeck> DeckChosen;
        public event Action PaintChosen;
        public event Action SettingsChosen;

        private readonly List<DeckButton> _spawned = new List<DeckButton>();
        private bool _built;

        private void Awake()
        {
            if (paintButton != null) paintButton.onClick.AddListener(() => PaintChosen?.Invoke());
            if (settingsButton != null) settingsButton.Triggered += () => SettingsChosen?.Invoke();
        }

        private void OnEnable()
        {
            if (!_built) Build();
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
                button.Setup(deck, d => DeckChosen?.Invoke(d));
                _spawned.Add(button);
            }

            // 畫畫按鈕固定排最後
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
