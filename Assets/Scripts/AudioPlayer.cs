using System.Collections;
using UnityEngine;

namespace BabyApp
{
    /// Plays card audio one clip after another. If a clip is missing a short
    /// generated chime is played instead so taps always give feedback.
    public class AudioPlayer : MonoBehaviour
    {
        public static AudioPlayer Instance { get; private set; }

        AudioSource source;
        AudioClip chime;
        AudioClip success;
        AudioClip wrong;
        AudioClip celebration;
        Coroutine current;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            chime = MakeTone("chime", 0.35f, 660f, 880f);
            success = MakeTone("success", 0.5f, 523f, 659f, 784f);
            wrong = MakeTone("wrong", 0.3f, 220f, 180f);
            celebration = MakeTone("celebration", 1.2f, 523f, 659f, 784f, 1047f, 784f, 1047f);
        }

        public void PlayCard(CardItem card)
        {
            var en = ContentLoader.LoadClip(card, "en");
            var zh = ContentLoader.LoadClip(card, "zh");
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(PlaySequence(en, zh));
        }

        public void PlayChime() => source.PlayOneShot(chime, 0.5f);
        public void PlaySuccess() => source.PlayOneShot(success, 0.6f);
        public void PlayWrong() => source.PlayOneShot(wrong, 0.4f);
        public void PlayCelebration() => source.PlayOneShot(celebration, 0.6f);

        IEnumerator PlaySequence(params AudioClip[] clips)
        {
            source.Stop();
            bool playedAny = false;
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                playedAny = true;
                source.clip = clip;
                source.Play();
                yield return new WaitForSeconds(clip.length + 0.35f);
            }
            if (!playedAny) PlayChime();
            current = null;
        }

        // Simple melodies synthesised at runtime, so no audio asset is needed.
        // The notes are played one after another, each fading out, over `length` seconds.
        static AudioClip MakeTone(string name, float length, params float[] notes)
        {
            const int rate = 44100;
            int samples = (int)(rate * length);
            float noteLength = length / notes.Length;
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                int note = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float noteT = t - note * noteLength;
                float envelope = 1f - noteT / noteLength;
                data[i] = Mathf.Sin(2 * Mathf.PI * notes[note] * t) * envelope * 0.6f;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
