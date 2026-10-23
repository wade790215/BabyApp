using BabyApp.Data;
using BabyApp.UI;
using UnityEngine;

namespace BabyApp.Core
{
    /// <summary>
    /// 整支 App 的入口。只有一個場景，各個畫面用開關顯示切換，
    /// 所以不會有載入畫面，小孩按下去就立刻有反應。
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        [SerializeField] private HomeScreen homeScreen;
        [SerializeField] private DeckScreen deckScreen;
        [SerializeField] private FindScreen findScreen;
        [SerializeField] private CountScreen countScreen;
        [SerializeField] private PaintScreen paintScreen;
        [SerializeField] private SettingsScreen settingsScreen;

        private void Awake()
        {
            // 平板上不要關螢幕，也不要讓螢幕變暗
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;

            if (homeScreen != null)
            {
                homeScreen.DeckChosen += OpenDeck;
                homeScreen.FindChosen += () => ShowOnly(findScreen);
                homeScreen.CountChosen += () => ShowOnly(countScreen);
                homeScreen.PaintChosen += () => ShowOnly(paintScreen);
                homeScreen.SettingsChosen += () => ShowOnly(settingsScreen);
            }
            if (deckScreen != null) deckScreen.HomeRequested += GoHome;
            if (findScreen != null) findScreen.HomeRequested += GoHome;
            if (countScreen != null) countScreen.HomeRequested += GoHome;
            if (paintScreen != null) paintScreen.HomeRequested += GoHome;
            if (settingsScreen != null) settingsScreen.CloseRequested += GoHome;
        }

        private void Start() => GoHome();

        private void OpenDeck(CardDeck deck)
        {
            if (deckScreen == null) return;
            ShowOnly(deckScreen);
            deckScreen.Open(deck);
        }

        private void GoHome() => ShowOnly(homeScreen);

        private void ShowOnly(Component screen)
        {
            if (screen == null) return;
            Toggle(homeScreen, screen);
            Toggle(deckScreen, screen);
            Toggle(findScreen, screen);
            Toggle(countScreen, screen);
            Toggle(paintScreen, screen);
            Toggle(settingsScreen, screen);
        }

        private static void Toggle(Component candidate, Component wanted)
        {
            if (candidate != null) candidate.gameObject.SetActive(candidate == wanted);
        }
    }
}
