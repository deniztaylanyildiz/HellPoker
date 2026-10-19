using System.Collections;
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
            HellPokerBootstrap.ForceSimulator = null;
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
        private static IEnumerator PlayUntilTheFanfare(string player)
        {
            var table = (TablePresenter)ChapterJourneyTests.Chapters.Table;
            TableView view = ChapterJourneyTests.ChapterTableView;
            var head = CardCodes.TryParseDeck(string.Join(",", (player + " " + HouseNothing + Draws).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))).ToList();
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
            [Values("AS AD 7C 3C JS", "AS AD 7C 7H JS", "9S 8D 7C 6H 5S", "KS KD KC 5S 5C", "QS QD QC QH JS", "AS KS QS JS 10S")] string player)
        {
            HandTier expected = player.StartsWith("AS AD 7C 3C") ? HandTier.Plain : player.StartsWith("AS AD 7C 7H") ? HandTier.Good
                : player.StartsWith("9S") ? HandTier.Strong : player.StartsWith("KS") ? HandTier.Full : player.StartsWith("QS") ? HandTier.Quads : HandTier.Blaze;
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
    }
}
