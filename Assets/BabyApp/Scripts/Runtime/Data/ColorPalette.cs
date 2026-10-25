using System.Collections.Generic;
using UnityEngine;

namespace BabyApp.Data
{
    /// <summary>
    /// 顏色卡的實際顏色。一張顏色卡不只是一顆色球，整張卡會被這個顏色填滿，
    /// 2 歲半看到的是「一整片紅」而不是「一顆紅球」。
    /// Editor 的 BabyAppUiSprites 畫色球也用同一張表，兩邊才會一致。
    /// </summary>
    public static class ColorPalette
    {
        private static readonly Dictionary<string, Color32> Table = new Dictionary<string, Color32>
        {
            ["color_red"] = new Color32(0xFF, 0x3B, 0x30, 0xFF),
            ["color_orange"] = new Color32(0xFF, 0x95, 0x00, 0xFF),
            ["color_yellow"] = new Color32(0xFF, 0xD6, 0x0A, 0xFF),
            ["color_green"] = new Color32(0x34, 0xC7, 0x59, 0xFF),
            ["color_blue"] = new Color32(0x0A, 0x84, 0xFF, 0xFF),
            ["color_purple"] = new Color32(0xBF, 0x5A, 0xF2, 0xFF),
            ["color_pink"] = new Color32(0xFF, 0x6F, 0xB5, 0xFF),
            ["color_black"] = new Color32(0x2B, 0x2B, 0x2B, 0xFF)
        };

        public static IEnumerable<KeyValuePair<string, Color32>> All => Table;

        public static bool TryGet(string cardId, out Color color)
        {
            if (!string.IsNullOrEmpty(cardId) && Table.TryGetValue(cardId, out var c))
            {
                color = c;
                return true;
            }
            color = Color.white;
            return false;
        }

        /// <summary>深色底上用白字，淺色底上用深字。</summary>
        public static bool IsDark(Color color) => color.r * 0.299f + color.g * 0.587f + color.b * 0.114f < 0.5f;
    }
}
