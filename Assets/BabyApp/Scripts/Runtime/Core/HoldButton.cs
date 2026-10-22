using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyApp.Core
{
    /// <summary>
    /// 要按住一段時間才會觸發的按鈕，並用環形進度顯示還要按多久。
    /// 用在「回首頁」這種不希望小孩亂點就跳走的操作。
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float holdSeconds = 0.8f;
        [Tooltip("用來顯示進度的 Image，Image Type 要設成 Filled")]
        [SerializeField] private Image progressRing;

        public event Action Triggered;

        private bool _holding;
        private float _held;

        private void Update()
        {
            if (!_holding) return;

            _held += Time.unscaledDeltaTime;
            if (progressRing != null) progressRing.fillAmount = Mathf.Clamp01(_held / holdSeconds);

            if (_held >= holdSeconds)
            {
                Release();
                Triggered?.Invoke();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _holding = true;
            _held = 0f;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            _holding = false;
            _held = 0f;
            if (progressRing != null) progressRing.fillAmount = 0f;
        }
    }
}
