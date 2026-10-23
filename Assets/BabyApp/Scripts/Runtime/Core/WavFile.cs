using System;
using System.IO;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>把 AudioClip 存成 16-bit PCM 的 WAV，或從 WAV 讀回來。只處理自己存的格式。</summary>
    public static class WavFile
    {
        public static void Save(string path, AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                foreach (var sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }
            }
        }

        public static AudioClip Load(string path, string name)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < 44) return null;

                int channels = BitConverter.ToInt16(bytes, 22);
                int frequency = BitConverter.ToInt32(bytes, 24);
                int bits = BitConverter.ToInt16(bytes, 34);
                if (bits != 16 || channels < 1) return null;

                // 找 data 區塊（自己存的 WAV 固定在 36，但保險起見還是掃一下）
                int offset = 12;
                int dataLength = 0;
                while (offset + 8 <= bytes.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
                    int size = BitConverter.ToInt32(bytes, offset + 4);
                    if (id == "data")
                    {
                        offset += 8;
                        dataLength = Mathf.Min(size, bytes.Length - offset);
                        break;
                    }
                    offset += 8 + size;
                }
                if (dataLength <= 0) return null;

                int count = dataLength / 2;
                var samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    samples[i] = BitConverter.ToInt16(bytes, offset + i * 2) / (float)short.MaxValue;
                }

                var clip = AudioClip.Create(name, count / channels, channels, frequency, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BabyApp] 讀錄音檔失敗：" + path + "，" + e.Message);
                return null;
            }
        }
    }
}
