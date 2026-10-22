using System;
using BabyApp.Core;
using UnityEngine;

namespace BabyApp.Data
{
    /// <summary>
    /// 一張認知卡片：一張圖、中英文名稱、念出名稱的語音（中英各一段）、這個東西自己的叫聲。
    /// 語音可以留空（還沒錄 / 還沒生成），程式會自動略過不播。
    /// </summary>
    [Serializable]
    public class CardEntry
    {
        [Tooltip("唯一代號，對應素材檔名，例如 animal_dog")]
        public string id;

        [Tooltip("中文名稱，例如 狗狗")]
        public string displayName;

        [Tooltip("英文名稱，例如 Dog")]
        public string displayNameEn;

        [Tooltip("AI 生成的卡片圖（1024x1024、透明或白底）")]
        public Sprite image;

        [Tooltip("念出中文名稱的語音，可留空")]
        public AudioClip voice;

        [Tooltip("念出英文名稱的語音（檔名 _en），可留空")]
        public AudioClip voiceEn;

        [Tooltip("這個東西自己的聲音（狗的汪汪、消防車的鳴笛），會先於名稱播放，可留空")]
        public AudioClip sound;

        public bool HasImage => image != null;

        public bool IsValid => !string.IsNullOrEmpty(displayName);

        /// <summary>依目前語言的名稱；英文缺的話退回中文。</summary>
        public string LocalizedName =>
            AppSettings.IsEnglish && !string.IsNullOrEmpty(displayNameEn) ? displayNameEn : displayName;

        /// <summary>依目前語言的名稱語音；英文沒錄就不播（不會退回中文，免得小孩混淆）。</summary>
        public AudioClip LocalizedVoice => AppSettings.IsEnglish ? voiceEn : voice;
    }
}
