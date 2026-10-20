using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BabyApp
{
    /// The only component that needs to be in the scene. On start it creates
    /// the camera, event system and the two screens (category menu and
    /// flashcards) entirely from code.
    public class AppBootstrap : MonoBehaviour
    {
        static readonly Color Background = new Color(0.98f, 0.96f, 0.90f);
        static readonly Color Ink = new Color(0.20f, 0.18f, 0.22f);

        CardCatalog catalog;
        Canvas canvas;
        RectTransform menuScreen;
        RectTransform cardScreen;

        // Flashcard screen state
        CardCategory currentCategory;
        int currentIndex;
        Image cardBackground;
        Image cardImage;
        Text wordEn;
        Text wordZh;
        Coroutine bounce;

        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            catalog = ContentLoader.LoadCatalog();
            EnsureCamera();
            EnsureEventSystem();
            new GameObject("AudioPlayer", typeof(AudioPlayer));

            canvas = UIFactory.CreateCanvas();
            BuildMenuScreen();
            BuildCardScreen();
            ShowMenu();
        }

        // ---------- scene plumbing ----------

        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.orthographic = true;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
#if ENABLE_INPUT_SYSTEM
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
        }

        // ---------- menu screen ----------

        void BuildMenuScreen()
        {
            menuScreen = UIFactory.CreatePanel(canvas.transform, "MenuScreen", Background);

            var title = UIFactory.CreateText(menuScreen, "Title", "BabyApp", 96, Ink);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1200, 140));

            var grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            grid.SetParent(menuScreen, false);
            UIFactory.Stretch(grid, 80);
            grid.offsetMax = new Vector2(-80, -220);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(400, 400);
            layout.spacing = new Vector2(50, 50);
            layout.childAlignment = TextAnchor.MiddleCenter;

            foreach (var category in catalog.categories)
            {
                var c = category;
                var color = ContentLoader.ParseColor(category.color, Color.gray);
                var button = UIFactory.CreateButton(grid, "Category_" + category.id, color, () => OpenCategory(c));

                var preview = UIFactory.CreateImage(button.transform, "Preview");
                if (category.cards.Length > 0)
                    preview.sprite = ContentLoader.LoadSprite(category.cards[0], Color.white);
                UIFactory.Stretch(preview.rectTransform, 40);
                preview.rectTransform.offsetMin = new Vector2(40, 110);
                preview.raycastTarget = false;

                var label = UIFactory.CreateText(button.transform, "Label", category.zh, 56, Color.white);
                UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(380, 80));
            }
        }

        // ---------- flashcard screen ----------

        void BuildCardScreen()
        {
            cardScreen = UIFactory.CreatePanel(canvas.transform, "CardScreen", Background);
            cardBackground = cardScreen.GetComponent<Image>();

            // White card in the middle holding the picture. Tap to hear it again, swipe to change card.
            var cardButton = UIFactory.CreateButton(cardScreen, "Card", Color.white, Replay);
            UIFactory.Place(cardButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 900));
            cardButton.gameObject.AddComponent<SwipeDetector>().OnSwipe = dir => Step(-dir);

            cardImage = UIFactory.CreateImage(cardButton.transform, "Picture");
            UIFactory.Stretch(cardImage.rectTransform, 40);
            cardImage.raycastTarget = false;

            wordEn = UIFactory.CreateText(cardScreen, "WordEn", "", 110, Ink);
            UIFactory.Place(wordEn.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1400, 140));

            wordZh = UIFactory.CreateText(cardScreen, "WordZh", "", 64, Ink, FontStyle.Normal);
            UIFactory.Place(wordZh.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 80));

            var prev = UIFactory.CreateButton(cardScreen, "Prev", new Color(1, 1, 1, 0.85f), () => Step(-1));
            UIFactory.Place(prev.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(60, 40), new Vector2(220, 220));
            UIFactory.CreateText(prev.transform, "Arrow", "<", 120, Ink).rectTransform.sizeDelta = new Vector2(220, 220);

            var next = UIFactory.CreateButton(cardScreen, "Next", new Color(1, 1, 1, 0.85f), () => Step(1));
            UIFactory.Place(next.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-60, 40), new Vector2(220, 220));
            UIFactory.CreateText(next.transform, "Arrow", ">", 120, Ink).rectTransform.sizeDelta = new Vector2(220, 220);

            // Home is deliberately smaller and tucked in a corner so a toddler rarely hits it by accident.
            var home = UIFactory.CreateButton(cardScreen, "Home", new Color(1, 1, 1, 0.6f), ShowMenu);
            UIFactory.Place(home.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(30, -30), new Vector2(120, 120));
            UIFactory.CreateText(home.transform, "Icon", "X", 72, Ink).rectTransform.sizeDelta = new Vector2(120, 120);
        }

        void ShowMenu()
        {
            menuScreen.gameObject.SetActive(true);
            cardScreen.gameObject.SetActive(false);
        }

        void OpenCategory(CardCategory category)
        {
            currentCategory = category;
            currentIndex = 0;
            cardBackground.color = Color.Lerp(ContentLoader.ParseColor(category.color, Background), Color.white, 0.55f);
            menuScreen.gameObject.SetActive(false);
            cardScreen.gameObject.SetActive(true);
            ShowCard();
        }

        void Step(int delta)
        {
            int count = currentCategory.cards.Length;
            currentIndex = (currentIndex + delta + count) % count;
            ShowCard();
        }

        void ShowCard()
        {
            var card = currentCategory.cards[currentIndex];
            var fallback = ContentLoader.ParseColor(currentCategory.color, Color.gray);
            cardImage.sprite = ContentLoader.LoadSprite(card, fallback);
            wordEn.text = card.en;
            wordZh.text = card.zh;
            AudioPlayer.Instance.PlayCard(card);
            Bounce();
        }

        void Replay()
        {
            AudioPlayer.Instance.PlayCard(currentCategory.cards[currentIndex]);
            Bounce();
        }

        void Bounce()
        {
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(BounceRoutine(cardImage.rectTransform));
        }

        static IEnumerator BounceRoutine(RectTransform target)
        {
            const float duration = 0.3f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float s = 1f + 0.12f * Mathf.Sin(t / duration * Mathf.PI);
                target.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
