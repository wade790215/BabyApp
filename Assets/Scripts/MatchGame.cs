using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BabyApp
{
    /// "Listen and tap" game: the app says a word, three pictures from the
    /// same category are shown, the child taps the matching one. Wrong taps
    /// are never punished - the tile just fades out so the choice gets easier.
    /// Every few rounds a star celebration plays, then the game continues.
    public class MatchGame : MonoBehaviour
    {
        const int Choices = 3;
        const int RoundsPerCelebration = 5;

        static readonly Color Ink = new Color(0.20f, 0.18f, 0.22f);

        RectTransform screen;
        Image background;
        Text promptEn;
        Text promptZh;
        Text stars;
        RectTransform celebration;
        readonly List<Button> tiles = new List<Button>();
        readonly List<Image> tileImages = new List<Image>();

        CardCategory category;
        CardItem target;
        CardItem previousTarget;
        int roundsWon;
        bool roundOver;
        Action onExit;

        public void Build(Transform parent, Color backgroundColor)
        {
            screen = UIFactory.CreatePanel(parent, "GameScreen", backgroundColor);
            background = screen.GetComponent<Image>();

            promptEn = UIFactory.CreateText(screen, "PromptEn", "", 96, Ink);
            UIFactory.Place(promptEn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1400, 120));
            promptZh = UIFactory.CreateText(screen, "PromptZh", "", 56, Ink, FontStyle.Normal);
            UIFactory.Place(promptZh.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(1400, 70));

            // Speaker button: hear the word again.
            var replay = UIFactory.CreateButton(screen, "Replay", new Color(1, 1, 1, 0.85f), () => AudioPlayer.Instance.PlayCard(target));
            UIFactory.Place(replay.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(160, 160));
            UIFactory.CreateText(replay.transform, "Icon", "))", 80, Ink).rectTransform.sizeDelta = new Vector2(160, 160);

            // Three big tiles in a row.
            var row = new GameObject("Tiles", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            row.SetParent(screen, false);
            UIFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(1800, 520));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 60;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < Choices; i++)
            {
                int index = i;
                var tile = UIFactory.CreateButton(row, "Tile" + i, Color.white, () => OnTileTapped(index));
                tile.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 520);
                var picture = UIFactory.CreateImage(tile.transform, "Picture");
                UIFactory.Stretch(picture.rectTransform, 30);
                picture.raycastTarget = false;
                tiles.Add(tile);
                tileImages.Add(picture);
            }

            // Celebration overlay: covers everything, tap to continue.
            celebration = UIFactory.CreatePanel(screen, "Celebration", new Color(1f, 0.95f, 0.6f, 0.95f));
            celebration.gameObject.AddComponent<Button>().onClick.AddListener(() => { celebration.gameObject.SetActive(false); NextRound(); });
            stars = UIFactory.CreateText(celebration, "Stars", "* * * * *", 200, new Color(1f, 0.6f, 0.1f));
            UIFactory.Place(stars.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1600, 260));
            var bravo = UIFactory.CreateText(celebration, "Bravo", "好棒！ Great job!", 90, Ink);
            UIFactory.Place(bravo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(1600, 120));
            celebration.gameObject.SetActive(false);

            var home = UIFactory.CreateButton(screen, "Home", new Color(1, 1, 1, 0.6f), () => { Close(); onExit?.Invoke(); });
            UIFactory.Place(home.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(30, -30), new Vector2(120, 120));
            UIFactory.CreateText(home.transform, "Icon", "X", 72, Ink).rectTransform.sizeDelta = new Vector2(120, 120);

            screen.gameObject.SetActive(false);
        }

        public void Open(CardCategory category, Color backgroundColor, Action onExit)
        {
            this.category = category;
            this.onExit = onExit;
            background.color = backgroundColor;
            roundsWon = 0;
            previousTarget = null;
            celebration.gameObject.SetActive(false);
            screen.gameObject.SetActive(true);
            NextRound();
        }

        public void Close()
        {
            StopAllCoroutines();
            screen.gameObject.SetActive(false);
        }

        void NextRound()
        {
            if (category.cards.Length < 2)
            {
                Close();
                onExit?.Invoke();
                return;
            }

            // Pick a target that differs from last round, then distractors from the rest.
            var pool = new List<CardItem>(category.cards);
            if (previousTarget != null && pool.Count > 1) pool.Remove(previousTarget);
            target = pool[UnityEngine.Random.Range(0, pool.Count)];
            previousTarget = target;

            var others = new List<CardItem>(category.cards);
            others.Remove(target);
            Shuffle(others);
            var shown = new List<CardItem> { target };
            for (int i = 0; i < Choices - 1 && i < others.Count; i++) shown.Add(others[i]);
            Shuffle(shown);

            var fallback = ContentLoader.ParseColor(category.color, Color.gray);
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < shown.Count;
                tiles[i].gameObject.SetActive(used);
                if (!used) continue;
                tiles[i].interactable = true;
                tiles[i].image.color = Color.white;
                tiles[i].transform.localScale = Vector3.one;
                tileImages[i].sprite = ContentLoader.LoadSprite(shown[i], fallback);
                tileImages[i].color = Color.white;
                tiles[i].name = shown[i].id;
            }

            promptEn.text = target.en;
            promptZh.text = target.zh;
            roundOver = false;
            AudioPlayer.Instance.PlayCard(target);
        }

        void OnTileTapped(int index)
        {
            if (roundOver) return;
            var tile = tiles[index];
            if (tile.name == target.id)
            {
                roundOver = true;
                roundsWon++;
                AudioPlayer.Instance.PlaySuccess();
                StartCoroutine(WinRoutine(tile.GetComponent<RectTransform>()));
            }
            else
            {
                tile.interactable = false;
                AudioPlayer.Instance.PlayWrong();
                StartCoroutine(FadeOut(tile.image, tileImages[index]));
            }
        }

        IEnumerator WinRoutine(RectTransform tile)
        {
            // Dim the other tiles, bounce the winner twice, then say the word once more.
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].transform != tile) tiles[i].interactable = false;

            for (int n = 0; n < 2; n++)
            {
                const float duration = 0.3f;
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    float s = 1f + 0.18f * Mathf.Sin(t / duration * Mathf.PI);
                    tile.localScale = new Vector3(s, s, 1);
                    yield return null;
                }
            }
            tile.localScale = Vector3.one;

            AudioPlayer.Instance.PlayCard(target);
            yield return new WaitForSeconds(1.6f);

            if (roundsWon % RoundsPerCelebration == 0)
            {
                celebration.gameObject.SetActive(true);
                AudioPlayer.Instance.PlayCelebration();
                StartCoroutine(PulseStars());
            }
            else
            {
                NextRound();
            }
        }

        IEnumerator PulseStars()
        {
            while (celebration.gameObject.activeSelf)
            {
                float s = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
                stars.rectTransform.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            stars.rectTransform.localScale = Vector3.one;
        }

        static IEnumerator FadeOut(Image frame, Image picture)
        {
            const float duration = 0.25f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float a = Mathf.Lerp(1f, 0.25f, t / duration);
                frame.color = new Color(1, 1, 1, a);
                picture.color = new Color(1, 1, 1, a);
                yield return null;
            }
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
