using System.Collections;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static HellPoker.PlayMode.Tests.CheatRig;
using static HellPoker.PlayMode.Tests.HellPokerScreenshots;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Each demon cheats once on the real table, played with its own buttons on a stacked deck: the sign goes up at the deal,
    /// the blow lands on the cards, the sign comes down, and the hand plays on with the cheat's mark.
    /// </summary>
    public class CheatJourneyTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>Deals with the cheat forced; the sign must name it before anything strikes.</summary>
        private static IEnumerator DealUnder(ICheat cheat, ICheat shown = null, string deck = null)
        {
            Force(cheat, shown, deck);
            Press("ActionButton");
            yield return WaitForTable();
            Assert.IsNotNull(Intent, "The sign goes up at the deal.");
            Assert.AreEqual((shown ?? cheat).Id, Intent.Id);
        }

        private static IEnumerator ToTheDraw()
        {
            Press("CheckToDrawButton");
            yield return WaitForTable();
            Assert.AreEqual(GamePhase.Drawing, Presenter.Game.Phase);
        }

        private static CheatResult Played => Presenter.Game.CheatsThisHand.Single(r => r.Outcome == CheatOutcome.Played);

        [UnityTest]
        public IEnumerator Mammon_ChainsTheBestCard_AndItCannotBeThrown()
        {
            yield return SitWith(0);
            yield return DealUnder(new CollateralCheat());
            yield return ToTheDraw();

            Assert.IsNull(Intent, "Played out: the sign comes down.");
            Assert.IsTrue(Presenter.Game.IsPlayerCardChained(4), "The K♣ is chained.");
            Assert.IsTrue(PlayerCard(4).HasMark, "The chain is on the card.");

            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[4]);
            Press("ActionButton");   // draw: nothing picked, the K♣ stays
            yield return WaitForTable();
            Assert.AreEqual(new Card(Rank.King, Suit.Clubs), Presenter.Game.PlayerHand[4]);
            yield return PlayToShowdown();
        }

        [UnityTest]
        public IEnumerator Belial_Lies_AndTheTruthComesOut()
        {
            yield return SitWith(1);
            yield return DealUnder(new ForkedTongueCheat(), shown: new FalseFaceCheat());
            yield return ToTheDraw();
            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[0]);
            Press("ActionButton");   // draw one: the forked tongue strikes after the draw
            yield return WaitForTable();

            CheatResult played = Played;
            Assert.AreEqual(CheatIds.ForkedTongue, played.CheatId);
            Assert.IsTrue(played.WasLie, "He said False Face.");
            Assert.IsNull(Intent, "The truth showed, then the sign came down.");
            yield return PlayToShowdown();
        }

        [UnityTest]
        public IEnumerator Lilith_VeilsACard_AndItIsThrownBlind()
        {
            yield return SitWith(2);
            yield return DealUnder(new NightVeilCheat());
            yield return ToTheDraw();

            int veiled = Enumerable.Range(0, Hand.Size).Single(Presenter.Game.IsPlayerCardHidden);
            Assert.IsTrue(PlayerCard(veiled).HasMark, "The veil is on the card.");
            CollectionAssert.IsEmpty(Presenter.Game.SuggestedDiscards(), "No hint may give the veiled card away.");

            Card under = Presenter.Game.PlayerHand[veiled];
            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[veiled]);
            Press("ActionButton");
            yield return WaitForTable();
            Assert.AreNotEqual(under, Presenter.Game.PlayerHand[veiled], "Thrown blind, a new card came.");
            Assert.IsFalse(Presenter.Game.IsPlayerCardHidden(veiled));
            yield return PlayToShowdown();
        }

        [UnityTest]
        public IEnumerator Lucifer_TheFall_TakesTheWinningHand()
        {
            yield return SitWith(0);
            SetSentence(Presenter, 150);
            yield return WaitForTable();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(Presenter.IsAtFinalTable, "Summoned.");

            yield return DealUnder(new TheFallCheat());
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
