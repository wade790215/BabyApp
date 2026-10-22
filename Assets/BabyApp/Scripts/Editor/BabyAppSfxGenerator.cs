using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 用程式合成幾個可愛的介面音效存成 WAV：
    /// 點擊「嘟～嘀」、翻頁「咻～」、開蓋子「噹噹噹」、點卡片「叮鈴」。
    /// 可愛的公式：高音、正弦波（圓潤）、很短、兩三個往上走的音。
    /// 這樣不用等找音效素材，App 現在就有手感。想換成真的音效，同名檔案蓋掉即可。
    /// </summary>
    public static class BabyAppSfxGenerator
    {
        private const int Rate = 22050;

        // 以前的機械音，看到就清掉
        private static readonly string[] Obsolete =
        {
            BabyAppPaths.SfxFolder + "/ui_pop.wav",
            BabyAppPaths.SfxFolder + "/ui_whoosh.wav",
            BabyAppPaths.SfxFolder + "/door_clack.wav"
        };

        public static (AudioClip tap, AudioClip page, AudioClip door, AudioClip sparkle) EnsureAll()
        {
            Directory.CreateDirectory(BabyAppPaths.SfxFolder);
            foreach (var old in Obsolete)
            {
                if (File.Exists(old)) AssetDatabase.DeleteAsset(old);
            }

            var tap = Ensure(BabyAppPaths.SfxTap, Boop);
            var page = Ensure(BabyAppPaths.SfxPage, Swoosh);
            var door = Ensure(BabyAppPaths.SfxDoor, TaDa);
            var sparkle = Ensure(BabyAppPaths.SfxSparkle, Sparkle);
            return (tap, page, door, sparkle);
        }

        private static AudioClip Ensure(string path, Func<float[]> synth)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing != null) return existing;

            WriteWav(path, synth());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------- 四種聲音 ----------

        /// <summary>嘟～嘀：兩個往上的短音，像小動物應了一聲。</summary>
        private static float[] Boop()
        {
            var buffer = NewBuffer(0.22f);
            Tone(buffer, 880f, 0.00f, 0.10f, 0.55f);  // A5
            Tone(buffer, 1175f, 0.09f, 0.11f, 0.55f); // D6
            return buffer;
        }

        /// <summary>咻～：音高一路往上滑的圓潤滑音，翻頁時像東西飛過去。</summary>
        private static float[] Swoosh()
        {
            var buffer = NewBuffer(0.24f);
            Sweep(buffer, 500f, 1500f, 0.0f, 0.22f, 0.45f);
            return buffer;
        }

        /// <summary>噹噹噹：三個往上的音加一點餘韻，打開蓋子時的小小「登場」。</summary>
        private static float[] TaDa()
        {
            var buffer = NewBuffer(0.5f);
            Tone(buffer, 784f, 0.00f, 0.10f, 0.5f);   // G5
            Tone(buffer, 988f, 0.09f, 0.10f, 0.5f);   // B5
            Tone(buffer, 1319f, 0.18f, 0.30f, 0.55f); // E6，拉長一點當餘韻
            Tone(buffer, 2637f, 0.18f, 0.25f, 0.12f); // 高八度薄薄一層，亮一點
            return buffer;
        }

        /// <summary>叮鈴：很快的高音琶音，點卡片噴星星時用。</summary>
        private static float[] Sparkle()
        {
            var buffer = NewBuffer(0.36f);
            Tone(buffer, 1568f, 0.00f, 0.09f, 0.4f);  // G6
            Tone(buffer, 1976f, 0.06f, 0.09f, 0.4f);  // B6
            Tone(buffer, 2349f, 0.12f, 0.09f, 0.4f);  // D7
            Tone(buffer, 3136f, 0.18f, 0.16f, 0.35f); // G7
            return buffer;
        }

        // ---------- 合成工具 ----------

        private static float[] NewBuffer(float seconds) => new float[(int)(Rate * seconds)];

        /// <summary>
        /// 疊一個正弦音進緩衝：很快的起音、指數衰減，加一點二倍頻讓它不那麼空。
        /// 音與音之間用疊加而不是接續，重疊的部分會自然融在一起。
        /// </summary>
        private static void Tone(float[] buffer, float frequency, float start, float duration, float amplitude)
        {
            int begin = (int)(start * Rate);
            int count = (int)(duration * Rate);
            double phase = 0.0;
            double step = 2.0 * Math.PI * frequency / Rate;

            for (int i = 0; i < count && begin + i < buffer.Length; i++)
            {
                float t = (float)i / Rate;
                float attack = Mathf.Clamp01(t / 0.006f);
                float decay = Mathf.Exp(-t * (3.5f / duration));
                float envelope = attack * decay;

                phase += step;
                float sample = (float)(Math.Sin(phase) + 0.25 * Math.Sin(phase * 2.0));
                buffer[begin + i] += sample * envelope * amplitude;
            }
        }

        /// <summary>從一個音高滑到另一個，中間最響，像「咻」。</summary>
        private static void Sweep(float[] buffer, float from, float to, float start, float duration, float amplitude)
        {
            int begin = (int)(start * Rate);
            int count = (int)(duration * Rate);
            double phase = 0.0;

            for (int i = 0; i < count && begin + i < buffer.Length; i++)
            {
                float p = (float)i / count;
                float frequency = Mathf.Lerp(from, to, p * p); // 越後面滑越快
                phase += 2.0 * Math.PI * frequency / Rate;
                float envelope = Mathf.Sin(p * Mathf.PI);
                float sample = (float)(Math.Sin(phase) + 0.2 * Math.Sin(phase * 2.0));
                buffer[begin + i] += sample * envelope * amplitude;
            }
        }

        // ---------- WAV 寫檔（16-bit 單聲道 PCM）----------

        private static void WriteWav(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;

                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });

                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);           // fmt 區塊長度
                writer.Write((short)1);     // PCM
                writer.Write((short)1);     // 單聲道
                writer.Write(Rate);
                writer.Write(Rate * 2);     // 每秒位元組
                writer.Write((short)2);     // 每個取樣的位元組
                writer.Write((short)16);    // 位元深度

                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                foreach (var sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }
            }
        }
    }
}
