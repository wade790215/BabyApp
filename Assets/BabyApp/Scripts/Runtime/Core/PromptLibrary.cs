using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 吉祥物說的那些話：「在哪裡？」「答對了！」「一、二、三」。中英各一套，照設定的語言拿。
    /// 音檔在 Assets/BabyApp/Audio/Prompts/，由 tools/make-prompts.ps1 產生，想換成自己錄的同名蓋掉。
    /// </summary>
    public class PromptLibrary : MonoBehaviour
    {
        public static PromptLibrary Instance { get; private set; }

        [Header("中文")]
        [SerializeField] private AudioClip whereZh;        // 「在哪裡？」（接在名稱後面）
        [SerializeField] private AudioClip whoseSoundZh;   // 「這是誰的聲音？」
        [SerializeField] private AudioClip correctZh;      // 「答對了！」
        [SerializeField] private AudioClip tryAgainZh;     // 「再試試看」
        [SerializeField] private AudioClip allCountedZh;   // 「全部數完了！」
        [SerializeField] private AudioClip[] numbersZh;    // 一～五

        [Header("English")]
        [SerializeField] private AudioClip whereEn;        // "Where is the"（放在名稱前面）
        [SerializeField] private AudioClip whoseSoundEn;
        [SerializeField] private AudioClip correctEn;
        [SerializeField] private AudioClip tryAgainEn;
        [SerializeField] private AudioClip allCountedEn;
        [SerializeField] private AudioClip[] numbersEn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private static bool En => AppSettings.IsEnglish;

        public AudioClip WhoseSound => En ? whoseSoundEn : whoseSoundZh;
        public AudioClip Correct => En ? correctEn : correctZh;
        public AudioClip TryAgain => En ? tryAgainEn : tryAgainZh;
        public AudioClip AllCounted => En ? allCountedEn : allCountedZh;

        /// <summary>「狗狗在哪裡？」：中文是 名稱→在哪裡，英文是 Where is the→名稱，語序不同。</summary>
        public AudioClip[] WhereIs(AudioClip name)
        {
            return En ? new[] { whereEn, name } : new[] { name, whereZh };
        }

        /// <summary>1～5 的數字語音；超出範圍回 null。</summary>
        public AudioClip Number(int n)
        {
            var set = En ? numbersEn : numbersZh;
            if (set == null || n < 1 || n > set.Length) return null;
            return set[n - 1];
        }
    }
}
