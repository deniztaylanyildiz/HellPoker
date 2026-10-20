using System.Collections;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>Demo 3.5: the spoils' scene in the real game — the deal, a press that hurries it, the words and cards in place, the choice.</summary>
    public class BossLootPlayTests
    {
        [TearDown]
        public void English() => Lang.Set(Language.English);

        /// <summary>A run at <paramref name="chapter"/>'s spoils (1 Mammon, 2 Belial), beaten there the chapters' own way.</summary>
        internal static ChapterSave Spoils(int chapter, SinnerClass cls = null, int seed = 11)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls ?? SinnerRoster.King, seed);
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            if (chapter == 2)
            {
                journey.Run.TakeLootCoins();
                journey.NextChapter();
                journey.Run.PayTribute();
                journey.Run.LeaveBossTable(0);
            }
            ChapterSave save = journey.Capture();
            save.NodeDone = true;
            save.Pending = "loot";
            return save;
        }

        private static IEnumerator Load(ChapterSave save, bool turkish)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ContinueButton");
            yield return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheDeal_PlaysOut_ThenTheChoiceOpens()
        {
            yield return Load(Spoils(1), false);
            var loot = Object.FindFirstObjectByType<BossLootView>();
            Assert.IsTrue(loot.IsOpen, "the spoils' scene");
            Assert.IsTrue(loot.IsAnimating, "the cards come in");
            float started = Time.time;
            while (loot.IsAnimating && Time.time - started < 6f) yield return null;
            Assert.IsFalse(loot.IsAnimating, "the deal ends on its own");
            Assert.IsTrue(loot.Cards.All(c => !c.BackShown && c.gameObject.activeSelf), "every card face up");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator APress_HurriesTheDeal_TheWordsAndCardsFit_OnTheGrid_TheChoiceLeadsOn_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            foreach (int chapter in new[] { 1, 2 })
            {
                string what = $"chapter {chapter}" + (turkish ? " (tr)" : " (en)");
                yield return Load(Spoils(chapter), turkish);
                var loot = Object.FindFirstObjectByType<BossLootView>();
                Assert.IsTrue(loot.IsAnimating, what);
                ChapterJourneyTests.Chapters.Confirm();   // a press while the cards come in
                Assert.IsFalse(loot.IsAnimating, what + ": hurried at once");
                Assert.IsTrue(loot.Cards.All(c => !c.BackShown), what + ": every card face up, the choice open");
                Assert.IsNull(ChapterJourneyTests.Chapters.Run.LootTaken ? "taken" : null, what + ": the press took nothing");
                Canvas.ForceUpdateCanvases();

                Assert.LessOrEqual(loot.Title.preferredWidth, loot.Title.rectTransform.rect.width, what + ": the title fits");
                Assert.LessOrEqual(loot.Subtitle.preferredWidth, loot.Subtitle.rectTransform.rect.width, what + ": the subtitle fits");
                var reroll = (RectTransform)loot.RerollButton.transform;
                float rerollTop = -reroll.anchoredPosition.y;
                Assert.LessOrEqual(loot.RerollButton.GetComponentInChildren<Text>().preferredWidth, reroll.rect.width - 4, what + ": REROLL fits");
                foreach (RelicCardView card in loot.Cards)
                {
                    var r = (RectTransform)card.transform;
                    int x = Mathf.RoundToInt(r.anchoredPosition.x), y = -Mathf.RoundToInt(r.anchoredPosition.y);
                    Assert.AreEqual(0, x % 8, $"{what}: {card.name} on the grid");
                    Assert.GreaterOrEqual(x, 0, what);
                    Assert.LessOrEqual(x + RelicCardView.Width, PixelScreen.Width, $"{what}: {card.name} on the screen");
                    Assert.Greater(y, -loot.Subtitle.rectTransform.anchoredPosition.y + 9, $"{what}: {card.name} under the words");
                    Assert.LessOrEqual(y + RelicCardView.Height, rerollTop, $"{what}: {card.name} over REROLL");
                }
                for (int i = 1; i < loot.Cards.Count; i++)
                    Assert.GreaterOrEqual(loot.Cards[i].Place.x, loot.Cards[i - 1].Place.x + RelicCardView.Width, what + ": side by side");

                HellPokerScreenshots.Press("LootCard" + (loot.Cards.Count - 1));   // the coins
                Assert.IsTrue(loot.IsAnimating, what + ": the choice plays out");
                float started = Time.time;
                while (ChapterJourneyTests.Chapters.IsLootOpen && Time.time - started < 5f) yield return null;
                Assert.IsFalse(loot.IsOpen, what + ": the scene goes");
                Assert.AreEqual(chapter + 1, ChapterJourneyTests.Chapters.Journey.Chapter, what + ": the next chapter");
                Assert.IsNotNull(ChapterJourneyTests.Chapters.Panel, what + ": its opening words");
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Reroll_TurnsTheRelicCardsOver_Once()
        {
            yield return Load(Spoils(1), false);
            var loot = Object.FindFirstObjectByType<BossLootView>();
            loot.Skip();
            var before = loot.Cards.Select(c => c.Data.Name).ToList();
            HellPokerScreenshots.Press("LootReroll");
            Assert.IsTrue(loot.IsAnimating, "the new faces turn over");
            float started = Time.time;
            while (loot.IsAnimating && Time.time - started < 4f) yield return null;
            var after = loot.Cards.Select(c => c.Data.Name).ToList();
            CollectionAssert.AreNotEqual(before, after);
            Assert.AreEqual(before.Last(), after.Last(), "the coins stay");
            Assert.AreEqual(UiText.LootRerolled, loot.RerollButton.GetComponentInChildren<Text>().text);
            Assert.IsTrue(loot.RerollButton.GetComponent<ButtonFeel>().Locked, "spent: dim");
        }
    }
}
