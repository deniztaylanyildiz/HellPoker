using System.Collections;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static HellPoker.PlayMode.Tests.CheatRig;
using static HellPoker.PlayMode.Tests.HellPokerScreenshots;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// The demons cheat on the real table, played with its own buttons on stacked decks: the blow lands on the cards, the
    /// sign comes down, the hand plays on with the cheat's mark — and a card kept from the player never shows its face on
    /// screen, in any frame, before the showdown.
    /// </summary>
    public class CheatJourneyTests
    {
        /// <summary>Four clubs and a 9♥; the 9♣ is still in the deck (after the House's draw).</summary>
        private const string FourClubs = "9H 2C 5C JC KC  2D 2H 5S 7H 4D  3S 6D 10S 9C 2S QD QC 8H 7C 3H";

        /// <summary>A pair of kings against the House's aces: the player loses.</summary>
        private const string KingsLoseToAces = "KC KD 7H 4S 2D AS AH 5D 6C 9H 3S 6D 8S 2S 3H 10D 10C 8H 7C 6H JS 5S";

        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.3f);
        }

        private static IEnumerator Deal()
        {
            Press("ActionButton");
            yield return WaitForTable();
        }

        private static IEnumerator ToTheDraw()
        {
            Press("CheckToDrawButton");
            yield return WaitForTable();
            Assert.AreEqual(GamePhase.Drawing, Presenter.Game.Phase);
        }

        private static IEnumerator Draw(params int[] discards)
        {
            foreach (int i in discards)
                Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[i]);
            Press("ActionButton");
            yield return WaitForTable();
        }

        private static CheatResult Played => Presenter.Game.CheatsThisHand.Single(r => r.Outcome == CheatOutcome.Played);

        private static HandCategory PlayerCategory => Evaluator.Evaluate(Presenter.Game.PlayerHand).Category;

        // ------------------------------------------------------------------ Mammon

        [UnityTest]
        public IEnumerator Mammon_ChainsACard_AndItCannotBeThrown()
        {
            yield return SitWith(0);
            Force(new CollateralCheat());   // a flush: nothing to throw, so the lowest card, the 2♣
            yield return Deal();
            Assert.AreEqual(CheatIds.Collateral, Intent?.Id, "The sign is up before the draw.");
            yield return ToTheDraw();

            Assert.IsNull(Intent, "Played out: the sign comes down.");
            Assert.IsTrue(Presenter.Game.IsPlayerCardChained(0));
            Assert.IsTrue(PlayerCard(0).HasMark, "The chain is on the card.");

            yield return Draw(0);   // refused: nothing is picked, the 2♣ stays
            Assert.AreEqual(new Card(Rank.Two, Suit.Clubs), Presenter.Game.PlayerHand[0]);
            yield return PlayToShowdown();
        }

        // ------------------------------------------------------------------ Belial

        [UnityTest]
        public IEnumerator Belial_Lies_AndHisTongueBreaksTheFlush()
        {
            yield return SitWith(1);
            Force(new ForkedTongueCheat(), shown: new FalseFaceCheat());
            yield return Deal();
            Assert.AreEqual(CheatIds.FalseFace, Intent?.Id, "He announces a false face.");
            yield return ToTheDraw();
            yield return Draw();   // stand pat on the flush: the tongue strikes after the draw

            CheatResult played = Played;
            Assert.AreEqual(CheatIds.ForkedTongue, played.CheatId);
            Assert.IsTrue(played.WasLie);
            Assert.IsFalse(played.Backfired);
            Assert.AreNotEqual(HandCategory.Flush, PlayerCategory, "Aimed: the flush is broken.");
            Assert.IsNull(Intent, "The truth showed, then the sign came down.");
            yield return PlayToShowdown();
        }

        [UnityTest]
        public IEnumerator Belial_HisTongueSlips_AndHandsThePlayerAFlush()
        {
            yield return SitWith(1);
            Force(new ForkedTongueCheat(), deck: FourClubs, backfirePercent: 100, random: new FirstChoice());
            yield return Deal();
            yield return ToTheDraw();
            yield return Draw();

            Assert.IsTrue(Played.Backfired, "The 9♥ slipped into the 9♣.");
            Assert.AreEqual(HandCategory.Flush, PlayerCategory);
            Assert.AreEqual("BACKFIRE", Object.FindFirstObjectByType<CheatEffects>().LastBackfire);
            yield return PlayToShowdown();
            Assert.AreEqual(1, Presenter.Records.BackfiresSeen);
        }

        // ------------------------------------------------------------------ Lilith

        [UnityTest]
        public IEnumerator Lilith_AVeiledCard_NeverShowsItsFace_UntilTheShowdown()
        {
            yield return SitWith(2);
            Force(new NightVeilCheat());
            DarkWatch watch = WatchTheDark();
            yield return Deal();

            int veiled = Played.PlayerCards.Single();
            Assert.GreaterOrEqual(veiled, 2, "A card still to turn.");
            yield return ToTheDraw();
            Assert.IsTrue(PlayerCard(veiled).HasMark, "It turned under its veil.");
            Assert.IsFalse(PlayerCard(veiled).IsFaceUp);
            yield return Draw();
            yield return PlayToShowdown();

            yield return AssertTheDarkHeld(watch);
        }

        [UnityTest]
        public IEnumerator Lilith_MoonlessCards_NeverShowTheirFaces_UntilTheShowdown()
        {
            yield return SitWith(2);
            Force(new MoonlessCheat());
            DarkWatch watch = WatchTheDark();
            yield return Deal();
            yield return ToTheDraw();
            yield return Draw(0, 2);

            Assert.IsTrue(Presenter.Game.IsPlayerCardHidden(0) && Presenter.Game.IsPlayerCardHidden(2), "The new cards came in the dark.");
            yield return PlayToShowdown();

            yield return AssertTheDarkHeld(watch);
        }

        // ------------------------------------------------------------------ Lucifer

        private static IEnumerator Summon(int years = 150)
        {
            yield return SitWith(0);
            SetSentence(Presenter, years);
            yield return WaitForTable();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(Presenter.IsAtFinalTable, "Summoned.");
        }

        [UnityTest]
        public IEnumerator Lucifer_Gaze_HeReRaisesAHandHeKnowsWillLose()
        {
            // 240 years: after a raise to the table's 150 there are still years left to re-raise.
            yield return Summon(240);
            Force(new GazeCheat(), deck: KingsLoseToAces);
            yield return Deal();
            Assert.AreEqual(CheatIds.Gaze, Played.CheatId);
            yield return ToTheDraw();
            yield return Draw();

            Press("RaiseButton");
            yield return WaitForTable();

            Assert.AreEqual(GamePhase.HouseReRaise, Presenter.Game.Phase, "He saw the kings would lose to his aces.");
            Assert.IsTrue(IsActive("CallButton"));
            yield return PlayToShowdown();
        }

        [UnityTest]
        public IEnumerator Lucifer_TheFall_TakesTheWinningHand()
        {
            yield return Summon();
            Force(new TheFallCheat());
            yield return Deal();
            Assert.AreEqual("THE FALL AWAITS", Intent.Name);
            yield return ToTheDraw();
            Press("ActionButton");   // stand pat on the flush
            yield return BetUntil(GamePhase.RoundOver);
            yield return WaitForTable();

            Assert.AreEqual(CheatIds.TheFall, Played.CheatId);
            Assert.IsTrue(Presenter.Game.MajorCheatUsed, "Once per attempt.");
            Assert.AreNotEqual(ShowdownOutcome.PlayerWins, Presenter.Game.LastRound.Showdown.Outcome, "The flush fell.");
        }
    }
}
