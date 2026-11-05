using System;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 家長編輯家人卡的畫面（從設定頁的「家人卡」進來）：標題、一行提醒（只存在這台平板），
    /// 8 格排成 4×2，每格自己管自己（FamilySlotView）。關閉回到設定頁，不是回首頁。
    /// </summary>
    public class FamilyEditScreen : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private FamilySlotView[] slots;

        public event Action CloseRequested;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null) slots[i].Bind(i);
                }
            }
        }

        private void OnEnable()
        {
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot != null) slot.Refresh();
            }
        }
    }
}
