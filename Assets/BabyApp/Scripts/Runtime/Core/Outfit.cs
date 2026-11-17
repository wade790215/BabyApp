using System;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 紙娃娃：小熊現在戴著什麼。第一版只有頭上的配件，兩個部位（頭頂、臉）各戴一樣。
    /// 造型存在裝置上（PlayerPrefs），改了會發 Changed，每個畫面的小熊（Mascot）自己去換，
    /// 所以在換裝頁戴上帽子，回首頁、去別的遊戲牠都還戴著。
    /// 沒有解鎖、沒有商店：清單裡的東西全部一開始就能戴。
    /// </summary>
    public static class Outfit
    {
        public enum Slot { Head, Face }

        public readonly struct Item
        {
            /// <summary>代號：圖是 Art/Outfits/outfit_{id}.png，語音是 Audio/Prompts/outfit_{id}{,_en}.wav，名稱是 Strings 的 outfit_{id}。</summary>
            public readonly string Id;
            public readonly Slot Slot;
            /// <summary>戴上去時，配件圖的中心在小熊圖（正方形）裡的位置：0～1，原點左下。</summary>
            public readonly Vector2 Center;
            /// <summary>配件圖的邊長佔小熊圖邊長的比例。</summary>
            public readonly float Size;
            /// <summary>歪一點比較俏皮；度，逆時針為正。</summary>
            public readonly float Tilt;

            public string LabelKey => "outfit_" + Id;

            public Item(string id, Slot slot, float centerX, float centerY, float size, float tilt = 0f)
            {
                Id = id;
                Slot = slot;
                Center = new Vector2(centerX, centerY);
                Size = size;
                Tilt = tilt;
            }
        }

        // 位置是照 Art/Mascot 的五張小熊圖量的（1024 見方，y 從上往下算：頭頂 y≈125、
        // 兩眼 (405,318)(612,318)、兩耳 (333,170)(668,152)）。這套做法成立的前提是
        // 「五張表情圖的頭都在同一個位置」；換了小熊的圖、頭的位置不一樣，就要回來改這裡。
        //
        // 配件整張圖（含歪掉之後的四個角）要留在小熊圖的範圍裡：首頁的小熊貼著螢幕上緣，
        // 帽子高出去的部分會被螢幕邊切掉。重建場景時 Builder 會檢查，超出去會在 Console 警告。
        // think 那張右上角的「？」在 x 0.74～0.81、y 0.77～0.90，頭頂的東西別往那邊放。
        public static readonly Item[] All =
        {
            new Item("hat", Slot.Head, 0.505f, 0.895f, 0.21f, -8f),
            new Item("crown", Slot.Head, 0.500f, 0.890f, 0.22f),
            new Item("bow", Slot.Head, 0.600f, 0.845f, 0.21f, -16f),
            new Item("flower", Slot.Head, 0.345f, 0.830f, 0.18f),
            new Item("glasses", Slot.Face, 0.497f, 0.689f, 0.42f),
            new Item("sunglasses", Slot.Face, 0.497f, 0.687f, 0.40f)
        };

        public const int SlotCount = 2;

        /// <summary>有人換了造型。</summary>
        public static event Action Changed;

        private static string Key(Slot slot) => slot == Slot.Head ? "babyapp.outfit.head" : "babyapp.outfit.face";

        public static int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return i;
            }
            return -1;
        }

        /// <summary>這個部位現在戴的是 All 裡的第幾個；沒戴（或存的代號已經不在清單裡）回 -1。</summary>
        public static int Worn(Slot slot)
        {
            int index = IndexOf(PlayerPrefs.GetString(Key(slot), ""));
            return index >= 0 && All[index].Slot == slot ? index : -1;
        }

        public static bool IsWorn(int index)
        {
            return index >= 0 && index < All.Length && Worn(All[index].Slot) == index;
        }

        /// <summary>戴上 All[index]；同一個部位原本戴的會被換掉。已經戴著就不動、不發事件。</summary>
        public static void Wear(int index)
        {
            if (index < 0 || index >= All.Length || IsWorn(index)) return;
            PlayerPrefs.SetString(Key(All[index].Slot), All[index].Id);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void TakeOff(Slot slot)
        {
            if (Worn(slot) < 0) return;
            PlayerPrefs.DeleteKey(Key(slot));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
