using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BabyApp.EditorTools
{
    /// <summary>
    /// 用程式合成五個介面音效存成 WAV，走 Pok Pok 那種「安靜不花俏」的手感：
    /// 低、短、圓，像木頭輕輕敲一下，不是電子琴的「叮咚」「登登登」。
    /// tap 點按鈕「叩」、page 翻卡「紙滑過」、door 掀蓋「啵」、correct 答對「一聲木琴」、chew 小熊「咔滋」。
    ///
    /// 想換成實物錄音：把 `sfx_tap.wav`／`.ogg`／`.mp3` 丟進 Audio/SFX 就直接用（先刪掉同名合成檔），不用改程式。
    /// 想重新合成：刪掉 Audio/SFX 裡的 `sfx_*.wav` 再跑「3. 重建場景」；檔案在就不會重做。
    /// </summary>
    public static class BabyAppSfxGenerator
    {
        private const int Rate = 22050;

        private static readonly string[] Extensions = { ".wav", ".ogg", ".mp3" };

        // 以前的聲音（機械音、電子琴琶音），看到就清掉；.meta 跟著 DeleteAsset 一起走
        private static readonly string[] Obsolete =
        {
            BabyAppPaths.SfxFolder + "/ui_pop.wav",
            BabyAppPaths.SfxFolder + "/ui_whoosh.wav",
            BabyAppPaths.SfxFolder + "/door_clack.wav",
            BabyAppPaths.SfxFolder + "/ui_boop.wav",
            BabyAppPaths.SfxFolder + "/ui_swoosh.wav",
            BabyAppPaths.SfxFolder + "/ui_tada.wav",
            BabyAppPaths.SfxFolder + "/ui_sparkle.wav",
            BabyAppPaths.SfxFolder + "/ui_chew.wav"
        };

        public static (AudioClip tap, AudioClip page, AudioClip door, AudioClip correct, AudioClip chew) EnsureAll()
        {
            Directory.CreateDirectory(BabyAppPaths.SfxFolder);
            foreach (var old in Obsolete)
            {
                if (File.Exists(old)) AssetDatabase.DeleteAsset(old);
            }

            var tap = Ensure(BabyAppPaths.SfxTap, Tap);
            var page = Ensure(BabyAppPaths.SfxPage, Page);
            var door = Ensure(BabyAppPaths.SfxDoor, Door);
            var correct = Ensure(BabyAppPaths.SfxCorrect, Correct);
            var chew = Ensure(BabyAppPaths.SfxChew, Chew);
            return (tap, page, door, correct, chew);
        }

        /// <summary>basePath 不帶副檔名。先找現成的 .wav/.ogg/.mp3（實物錄音），都沒有才合成一個 .wav。</summary>
        private static AudioClip Ensure(string basePath, Func<float[]> synth)
        {
            foreach (var extension in Extensions)
            {
                var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + extension);
                if (existing != null) return existing;
            }

            string path = basePath + ".wav";
            WriteWav(path, synth());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------- 五種聲音（峰值都壓在 0.35 以下） ----------

        /// <summary>叩：木塊輕敲一下，很快就沒了。點按鈕、放好一片拼圖。</summary>
        private static float[] Tap()
        {
            var buffer = NewBuffer(0.14f);
            Knock(buffer, 520f, 0f, 0.03f, 0.32f);
            return buffer;
        }

        /// <summary>紙滑過：一小段低通雜音，中間最響。翻卡、換頁，是最常發生的動作所以要最不起眼。</summary>
        private static float[] Page()
        {
            var buffer = NewBuffer(0.16f);
            var random = new System.Random(11);
            float previous = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float p = (float)i / buffer.Length;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                previous = Mathf.Lerp(previous, noise, 0.12f); // 低通很重，偏「沙」不偏「嘶」
                buffer[i] += previous * Mathf.Sin(p * Mathf.PI) * 0.10f;
            }
            return buffer;
        }

        /// <summary>啵：音高往下掉的一聲軟軟的「啵」，像拔開軟木塞。掀開蓋子。</summary>
        private static float[] Door()
        {
            var buffer = NewBuffer(0.12f);
            int count = (int)(0.09f * Rate);
            double phase = 0.0;
            for (int i = 0; i < count && i < buffer.Length; i++)
            {
                float t = (float)i / Rate;
                float p = (float)i / count;
                float frequency = Mathf.Lerp(620f, 320f, p);
                phase += 2.0 * Math.PI * frequency / Rate;
                float envelope = Mathf.Clamp01(t / 0.002f) * Mathf.Exp(-t / 0.03f);
                buffer[i] += (float)Math.Sin(phase) * envelope * 0.28f;
            }
            return buffer;
        }

        /// <summary>木琴單音 G4，餘韻稍長一點，上面薄薄一層泛音。答對、拼完、數完各一聲，不是琶音。</summary>
        private static float[] Correct()
        {
            var buffer = NewBuffer(0.45f);
            Knock(buffer, 392f, 0f, 0.12f, 0.30f);   // G4
            Knock(buffer, 1568f, 0f, 0.05f, 0.06f);  // 高兩個八度的泛音，讓它亮一點但不尖
            return buffer;
        }

        /// <summary>咔滋咔滋：兩下短促的雜音脆響，小熊把東西吃掉時用。比以前小聲、低通更重。</summary>
        private static float[] Chew()
        {
            var buffer = NewBuffer(0.4f);
            Crunch(buffer, 0.00f, 0.10f, 0.30f);
            Crunch(buffer, 0.18f, 0.10f, 0.24f);
            Tone(buffer, 220f, 0.00f, 0.08f, 0.12f);  // 低音墊一下，有「咬下去」的厚度
            Tone(buffer, 196f, 0.18f, 0.08f, 0.10f);
            return buffer;
        }

        // ---------- 合成工具 ----------

        private static float[] NewBuffer(float seconds) => new float[(int)(Rate * seconds)];

        /// <summary>
        /// 木頭敲一下（模態合成）：基頻加一個不成整數倍的泛音（2.76 倍，木塊的味道），
        /// 2 毫秒起音、指數衰減（tau 秒衰到 1/e），開頭疊 3 毫秒低通雜音當「敲到」的那一下。跑到 5 倍 tau 就停。
        /// </summary>
        private static void Knock(float[] buffer, float frequency, float start, float tau, float amplitude)
        {
            int begin = (int)(start * Rate);
            int count = Mathf.Min((int)(5f * tau * Rate), buffer.Length - begin);
            double phase = 0.0;
            double step = 2.0 * Math.PI * frequency / Rate;
            var random = new System.Random(begin + 3);
            float previous = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float envelope = Mathf.Clamp01(t / 0.002f) * Mathf.Exp(-t / tau);
                phase += step;
                float sample = (float)(Math.Sin(phase) + 0.25 * Math.Sin(phase * 2.76));
                float value = sample * envelope * amplitude;

                if (t < 0.003f)
                {
                    float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    previous = Mathf.Lerp(previous, noise, 0.3f);
                    value += previous * 0.06f;
                }
                buffer[begin + i] += value;
            }
        }

        /// <summary>一下帶濾波的雜音脆響：很快衰減，像咬到脆的東西。</summary>
        private static void Crunch(float[] buffer, float start, float duration, float amplitude)
        {
            int begin = (int)(start * Rate);
            int count = (int)(duration * Rate);
            var random = new System.Random(begin + 7);
            float previous = 0f;

            for (int i = 0; i < count && begin + i < buffer.Length; i++)
            {
                float t = (float)i / Rate;
                float envelope = Mathf.Exp(-t * (6f / duration));
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                // 低通重一點，讓它偏「咔滋」不偏「嘶」
                previous = Mathf.Lerp(previous, noise, 0.30f);
                buffer[begin + i] += previous * envelope * amplitude;
            }
        }

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
