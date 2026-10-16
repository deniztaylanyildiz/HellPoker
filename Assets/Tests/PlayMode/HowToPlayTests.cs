using System.Collections;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>Demo 2.2's How to Play: every page opens in both languages, its lines fit beside the picture and above the pager, the
    /// pictures load, the arrows (and the keys' command) turn the pages; the Boss Simulator keeps its own pages.</summary>
    public class HowToPlayTests
    {
        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

        private static IEnumerator OpenHowToPlay(string language)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", language);
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return null;
        }

        private static bool Overlap(RectTransform a, RectTransform b)
        {
            Rect ra = Box(a), rb = Box(b);
            return ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax;
        }

        private static Rect Box(RectTransform r) =>
            new Rect(r.anchoredPosition.x, -r.anchoredPosition.y, r.sizeDelta.x, r.sizeDelta.y);

        [UnityTest]
        public IEnumerator EveryPage_InBothLanguages_FitsAndTurns()
        {
            foreach (string language in new[] { "English", "Turkish" })
            {
                yield return OpenHowToPlay(language);
                var menu = Object.FindFirstObjectByType<MainMenuView>();
                HowToPlayView pages = menu.HowTo;
                Assert.IsTrue(menu.IsShowingRules);
                Assert.IsTrue(pages.gameObject.activeInHierarchy, "the game's paged How to Play");
                Assert.AreEqual(0, pages.Page);
                int pagerY = Mathf.RoundToInt(-pages.Pager.rectTransform.anchoredPosition.y);
                for (int page = 0; page < UiText.HowToPages; page++)
                {
                    Assert.AreEqual(page, pages.Page);
                    Assert.AreEqual(string.Format(UiText.HowToPagerFormat, page + 1, UiText.HowToPages), pages.Pager.text);
                    if (page < UiText.HowToPages - 1)
                    {
                        Assert.IsNotEmpty(pages.Title.text, $"{language} page {page + 1}: a title");
                        Assert.IsNotEmpty(pages.Lines[0].text, $"{language} page {page + 1}: its lines");
                        Assert.LessOrEqual(UiText.HowToLines(page).Length, 5, "five lines at most");
                        Assert.LessOrEqual(pages.TextBottom, pagerY - 2, $"{language} page {page + 1}: the lines stay above the pager");
                        bool made = HowToPlayView.ImageOf(page) != null;
                        Assert.AreEqual(made, pages.ImageShown, $"{language} page {page + 1}: the picture, when it was made");
                        foreach (Text line in pages.Lines)
                        {
                            if (string.IsNullOrEmpty(line.text)) continue;
                            Assert.LessOrEqual(line.preferredHeight, line.rectTransform.sizeDelta.y + 0.5f, $"{language} page {page + 1}: '{line.text}' is cut");
                            if (pages.ImageShown) Assert.IsFalse(Overlap(line.rectTransform, pages.ImageRect), $"{language} page {page + 1}: a line over the picture");
                        }
                    }
                    ChapterJourneyTests.Menu.TurnRulesPage(1);
                    yield return null;
                }
                Assert.AreEqual(0, pages.Page, "round to the first page");
                ChapterJourneyTests.Menu.TurnRulesPage(-1);
                Assert.AreEqual(UiText.HowToPages - 1, pages.Page, "back from the first: the last");
                HellPokerScreenshots.Press("HowToNext");
                Assert.AreEqual(0, pages.Page);
                ChapterJourneyTests.Menu.GoBack();
                yield return null;
                Assert.IsFalse(menu.IsShowingRules, "Esc on the first page closes How to Play");
            }
        }

        [UnityTest]
        public IEnumerator TheBossSimulator_KeepsItsOwnPages()
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.ForceSimulator = true;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuView>();
            Assert.IsFalse(menu.HowTo.gameObject.activeInHierarchy, "no paged How to Play in the simulator");
            Assert.IsTrue(HellPokerScreenshots.Find<UnityEngine.UI.Button>("HandRanksPageButton").gameObject.activeInHierarchy);
        }
    }
}
