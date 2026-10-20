using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.1: a chapter table's hand ending by its strength, on the real table. Each tier from a stacked deck at Mammon's table: the right
    /// tier plays, the demon's bar waits for it (the hit comes after, never over it), a skip ends it at once and leaves nothing on the screen —
    /// the band and the message readable; with SCREEN SHAKE off the four of a kind plays without its white flash.
    /// </summary>
    public class HandFanfarePlayTests
    {
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string Draws = " 3S 5H 7D 8H JD";

        [TearDown]
        public void Restore()
        {
            ScreenShake.Set(true);
        }

        private static IEnumerator AtMammons()
        {
            yield return ChapterJourneyTests.LoadWithSave(new ChapterSave
            {
                ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                BossBar = 1600, NodeDone = false
            });
            yield return ChapterJourneyTests.WaitForChapterTable();
        }

        /// <summary>Plays a stacked hand, every step waited for, until its fanfare begins (or the table rests with none).</summary>
        private static IEnumerator PlayUntilTheFanfare(string player, string house = HouseNothing)
        {
            var table = (TablePresenter)ChapterJourneyTests.Chapters.Table;
            TableView view = ChapterJourneyTests.ChapterTableView;
            var head = CardCodes.TryParseDeck(string.Join(",", (player + " " + house + Draws).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))).ToList();
            Assert.AreEqual(15, head.Distinct().Count(), "fifteen different cards: a stacked deck with a card twice is refused");
            ((HellPokerGame)table.Game).StackTheDeal(head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            int guard = 0;
            while (guard++ < 30)
            {
                table.PerformAction();
                float started = Time.time;
                yield return null;
                while (view.IsBusy && view.Fanfare.Playing == HandTier.None && Time.time - started < 15f) yield return null;
                if (view.Fanfare.Playing != HandTier.None || table.Game.Phase == GamePhase.RoundOver || table.Game.IsGameOver) yield break;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EachTier_PlaysForItsHand_TheBarWaits_AndASkipLeavesNothing(
            [Values("AS AD 7C 3C JS", "AS AD 7C 7H JS", "QS QD QC 4C 3H", "9S 8D 7C 6H 5S", "2H 6H 9H JH KH", "KS KD KC 5S 5C", "QS QD QC QH JS",
                "AS KS QS JS 10S")] string player)
        {
            var tiers = new Dictionary<string, HandTier>
            {
                ["AS AD 7C 3C JS"] = HandTier.Plain, ["AS AD 7C 7H JS"] = HandTier.TwoPair, ["QS QD QC 4C 3H"] = HandTier.Trips,
                ["9S 8D 7C 6H 5S"] = HandTier.Straight, ["2H 6H 9H JH KH"] = HandTier.Flush, ["KS KD KC 5S 5C"] = HandTier.Full,
                ["QS QD QC QH JS"] = HandTier.Quads, ["AS KS QS JS 10S"] = HandTier.Blaze
            };
            HandTier expected = tiers[player];
            yield return AtMammons();
            TableView view = ChapterJourneyTests.ChapterTableView;
            int barBefore = view.BossBar.Shown.Value;

            yield return PlayUntilTheFanfare(player);
            Assert.AreEqual(expected, view.Fanfare.Playing, "the tier of " + player);
            Assert.AreEqual(barBefore, view.BossBar.Shown.Value, "the demon's bar waits for the hand's animation");
            yield return null;
            Assert.IsTrue(view.Fanfare.AnythingShown || expected == HandTier.Plain, "something on the screen while it plays");

            view.SkipAnimations();
            yield return null;
            Assert.AreEqual(HandTier.None, view.Fanfare.Playing, "a skip ends it");
            Assert.IsFalse(view.Fanfare.AnythingShown, "and leaves nothing on the screen");
            Assert.AreEqual(expected, view.Fanfare.LastPlayed);
            Assert.AreNotEqual(barBefore, view.BossBar.Shown.Value, "then the bar takes the hit");
            Assert.IsNotNull(view.Band, "the result band stays");
            Assert.IsNotEmpty(view.MessageShown, "the message stays readable");
            Assert.AreEqual(Vector2.zero, HoverTip.ScreenOf(view.PlayerPanel.transform).anchoredPosition, "the screen back in its place");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AFullTier_PlaysToItsEnd_OnItsOwn()
        {
            yield return AtMammons();
            TableView view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("KS KD KC 5S 5C");
            Assert.AreEqual(HandTier.Full, view.Fanfare.Playing);
            float started = Time.time;
            while (view.Fanfare.Playing != HandTier.None && Time.time - started < 10f) yield return null;
            float took = Time.time - started;
            Assert.AreEqual(HandTier.None, view.Fanfare.Playing, "it ends");
            Assert.Less(took, HandFanfare.SecondsOf(HandTier.Full) + 1.5f, "about as long as its tier");
            Assert.IsFalse(view.Fanfare.AnythingShown, "nothing of it stays");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator WithScreenShakeOff_FourOfAKind_HasNoFlash()
        {
            yield return AtMammons();
            ScreenShake.Set(false);   // after the scene: its settings apply their own value on load
            TableView view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("QS QD QC QH JS");
            Assert.AreEqual(HandTier.Quads, view.Fanfare.Playing);
            float started = Time.time;
            while (view.Fanfare.Playing != HandTier.None && Time.time - started < 10f)
            {
                Assert.AreEqual(Vector2.zero, HoverTip.ScreenOf(view.PlayerPanel.transform).anchoredPosition, "no shake");
                yield return null;
            }
            Assert.IsFalse(view.Fanfare.FlashShown, "no white flash");
            Assert.AreEqual(HandTier.Quads, view.Fanfare.LastPlayed, "everything else played");
        }

        /// <summary>Plays the fanfare already begun to its end (no skip), watching that the screen never moves when SCREEN SHAKE is off.</summary>
        private static IEnumerator ToItsEnd(TableView view, bool shakeOff = false)
        {
            float started = Time.time;
            while (view.Fanfare.Playing != HandTier.None && Time.time - started < 10f)
            {
                if (shakeOff) Assert.AreEqual(Vector2.zero, HoverTip.ScreenOf(view.PlayerPanel.transform).anchoredPosition, "no shake, no knock");
                yield return null;
            }
            Assert.IsFalse(view.Fanfare.AnythingShown, "nothing of it stays");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EveryHand_HasItsOwnMotion()
        {
            yield return AtMammons();
            TableView view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("QS QD QC 4C 3H");
            yield return ToItsEnd(view);
            Assert.IsTrue(view.Fanfare.SparksShown, "trips: gold sparks round the three");
            Assert.IsTrue(view.Fanfare.ShookOrKnocked, "and the screen knocks a pixel");

            yield return AtMammons();
            view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("9S 8D 7C 6H 5S");
            yield return ToItsEnd(view);
            Assert.IsTrue(view.Fanfare.Waved, "a straight: the wave");
            Assert.IsFalse(view.Fanfare.SuitsRained);

            yield return AtMammons();
            view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("2H 6H 9H JH KH");
            yield return ToItsEnd(view);
            Assert.IsTrue(view.Fanfare.SuitsRained, "a flush: its suit rains");
            Assert.IsTrue(view.Fanfare.ToneFlashed, "and the cards flash in its tone");
            Assert.IsFalse(view.Fanfare.Waved);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ABigLoss_FlickersTheEdgeRed_AfterTheHousesCards_AndWithShakeOffItDoesNotShake()
        {
            yield return AtMammons();
            TableView view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("3D 5C 7H JC KD", "9C 9D 9H 9S 2C");
            Assert.AreEqual(HandTier.HouseStrong, view.Fanfare.Playing, "the House's cards hit first");
            Assert.IsFalse(view.Fanfare.RedEdgeShown, "the edge waits for them");
            yield return ToItsEnd(view);
            Assert.IsTrue(view.Fanfare.RedEdgeShown, "then the edge flickers red");
            Assert.IsTrue(view.Fanfare.ShookOrKnocked, "with a short shake");

            yield return AtMammons();
            ScreenShake.Set(false);
            view = ChapterJourneyTests.ChapterTableView;
            yield return PlayUntilTheFanfare("3D 5C 7H JC KD", "9C 9D 9H 9S 2C");
            yield return ToItsEnd(view, shakeOff: true);
            Assert.IsTrue(view.Fanfare.RedEdgeShown, "SCREEN SHAKE off: the red edge still flickers");
            Assert.IsFalse(view.Fanfare.ShookOrKnocked, "but nothing shakes");
        }
    }
}
