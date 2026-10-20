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
        Coroutine current;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            chime = MakeChime();
        }

        public void PlayCard(CardItem card)
        {
            var en = ContentLoader.LoadClip(card, "en");
            var zh = ContentLoader.LoadClip(card, "zh");
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(PlaySequence(en, zh));
        }

        public void PlayChime()
        {
            source.PlayOneShot(chime, 0.5f);
        }

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

        // A soft two-note chime synthesised at runtime, so no asset is needed.
        static AudioClip MakeChime()
        {
            const int rate = 44100;
            const float length = 0.35f;
            int samples = (int)(rate * length);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float freq = t < length / 2 ? 660f : 880f;
                float envelope = 1f - t / length;
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("chime", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
