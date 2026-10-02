using System;
using HellPoker.Core.Betting;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The sealed pact: once the table is full (or the player all in) the hand plays out without bet decisions — no passing
    /// to click through, no folding — but the draw is still the player's. Also CHECK TO DRAW, and a hand left unfinished.
    /// </summary>
    public class PactTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";

        private static GameRules Rules(int startingYears = 1000, int tableCapPercent = 30, int forcedRaiseYears = 250) =>
            new GameRules(startingYears, 2000, forcedRaiseYears: forcedRaiseYears, stakes: new StakeScale(tableCapPercent: tableCapPercent));

        private static HellPokerGame CreateGame(GameRules rules = null, IHouseBettingStrategy houseBetting = null, IPayoutTable payouts = null)
        {
            return new HellPokerGame(
                rules ?? Rules(),
                TestDecks.Stacked($"{Nothing} {HouseFullHouse} 3S 8D QD 6C"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                payouts ?? PayoutTable.CreateDefault(),
                houseBetting);
        }

        /// <summary>Unit 100, ante 100, cap 300: two raises before the draw fill the table on the fourth card.</summary>
        private static HellPokerGame SealedBeforeTheDraw()
        {
            var game = CreateGame();
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Raise);
            return game;
        }

        private static void PassUntil(HellPokerGame game, GamePhase phase)
        {
            while (game.Phase != phase)
                game.Bet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ sealing

        [Test]
        public void ReachingTheCap_SealsThePact_AndTheRestOfTheCardsTurnOnTheirOwn()
        {
            HellPokerGame game = SealedBeforeTheDraw();

            Assert.AreEqual(300, game.CurrentStake);
            Assert.IsTrue(game.IsCommitted);
            Assert.AreEqual(GamePhase.Drawing, game.Phase, "The fifth card was not asked about.");
            Assert.AreEqual(5, game.PlayerCardsRevealed);
            Assert.AreEqual(1, game.DecisionsSkipped);
        }

        [Test]
        public void BelowTheCap_NothingIsSealed()
        {
            var game = CreateGame();
            game.PlaceBet();
            game.Bet(BetAction.Raise);

            Assert.IsFalse(game.IsCommitted);
            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
            Assert.AreEqual(0, game.DecisionsSkipped);
            Assert.IsTrue(game.CanBet(BetAction.Fold, out _));
        }

        [Test]
        public void WhileSealed_PassAndFold_AreRefused()
        {
            HellPokerGame game = SealedBeforeTheDraw();

            foreach (BetAction action in new[] { BetAction.Pass, BetAction.Fold, BetAction.Raise, BetAction.Call })
            {
                Assert.IsFalse(game.CanBet(action, out string reason), action.ToString());
                Assert.IsNotNull(reason);
                Assert.Throws<InvalidOperationException>(() => game.Bet(action));
            }
        }

        [Test]
        public void WhileSealed_TheDrawIsStillAsked_ThenTheHandPlaysOutToTheShowdown()
        {
            HellPokerGame game = SealedBeforeTheDraw();

            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.IsTrue(game.CanDraw(new[] { 0, 1 }, out _), "The cards are still the player's choice.");

            game.Draw(new int[0]);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "No decision after the draw, none on the House's cards.");
            Assert.AreEqual(3, game.DecisionsSkipped);
            Assert.IsNotNull(game.LastRound.Showdown);
            Assert.IsFalse(game.LastRound.Folded);
            Assert.AreEqual(300, game.LastRound.Stake);
        }

        [Test]
        public void AllIn_AtTheDeal_SealsAtOnce()
        {
            var game = CreateGame(Rules(startingYears: 6));

            game.PlaceBet();

            Assert.IsTrue(game.IsCommitted);
            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.AreEqual(3, game.DecisionsSkipped, "Cards 3, 4 and 5 turned without a question.");
        }

        [Test]
        public void TheSeal_LastsUntilTheNextDeal()
        {
            HellPokerGame game = SealedBeforeTheDraw();
            game.Draw(new int[0]);
            Assert.IsTrue(game.IsCommitted, "Still sealed while the result shows.");

            game.NextRound();

            Assert.IsFalse(game.IsCommitted);
            Assert.AreEqual(0, game.DecisionsSkipped);
        }

        // ------------------------------------------------------------------ the house re-raise

        [Test]
        public void AHouseReRaise_AfterARaiseToTheCap_IsStillAnswered()
        {
            var game = CreateGame(houseBetting: new HellPokerGameTests.FixedHouseBetting(true));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);   // 100 + 200 = 300: the cap

            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase, "The House may still raise past the cap.");
            Assert.IsFalse(game.IsCommitted, "A re-raise is a new bet: call or fold.");
            Assert.IsTrue(game.CanBet(BetAction.Call, out _));
            Assert.IsTrue(game.CanBet(BetAction.Fold, out _));
        }

        [Test]
        public void CallingAReRaise_PastTheCap_SealsThePact()
        {
            var game = CreateGame(houseBetting: new HellPokerGameTests.FixedHouseBetting(true));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Call);

            Assert.IsTrue(game.IsCommitted);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "The House's cards turned without a last decision.");
            Assert.AreEqual(400, game.LastRound.Stake);
            Assert.AreEqual(1, game.DecisionsSkipped);
        }

        [Test]
        public void CallingAReRaise_BelowTheCap_LeavesTheLastDecision()
        {
            // Cap 500: 100 + 200 raise + 100 re-raise = 400.
            var game = CreateGame(Rules(tableCapPercent: 50), new HellPokerGameTests.FixedHouseBetting(true));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Call);

            Assert.IsFalse(game.IsCommitted);
            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
        }

        // ------------------------------------------------------------------ the final stretch and the soul

        [Test]
        public void FinalStretch_RaisesToTheCap_ThenNothingMoreIsAsked()
        {
            // 250 years: unit 25, ante 25, cap 75. Passing is forbidden until the cap — after it, no decision is left.
            var game = CreateGame(Rules(startingYears: 250));
            game.PlaceBet();
            Assert.IsFalse(game.CanBet(BetAction.Pass, out _));

            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Raise);
            game.Draw(new int[0]);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.AreEqual(75, game.LastRound.Stake);
        }

        [Test]
        public void SoulAllIn_SealsThePact()
        {
            var game = CreateGame();
            game.TakeOver(2850, 3);   // 150 of the soul left
            game.PlaceBet();

            game.Bet(BetAction.Raise);

            Assert.IsTrue(game.IsSoulHand);
            Assert.AreEqual(0, game.WagerLeft);
            Assert.IsTrue(game.IsCommitted);
            Assert.AreEqual(GamePhase.Drawing, game.Phase);
        }

        // ------------------------------------------------------------------ CHECK TO DRAW

        [Test]
        public void CheckToDraw_PassesEveryCardUntilTheDraw()
        {
            var game = CreateGame();
            game.PlaceBet();

            Assert.IsTrue(game.CanCheckToDraw(out _));
            game.CheckToDraw();

            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.AreEqual(5, game.PlayerCardsRevealed);
            Assert.AreEqual(100, game.CurrentStake, "Nothing was raised.");
            Assert.IsFalse(game.IsCommitted);
        }

        [Test]
        public void CheckToDraw_AfterARaise_StillOnlyPasses()
        {
            var game = CreateGame();
            game.PlaceBet();
            game.Bet(BetAction.Raise);

            game.CheckToDraw();

            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.AreEqual(200, game.CurrentStake);
        }

        [Test]
        public void CheckToDraw_IsOnlyForTheCardsBeforeTheDraw()
        {
            var game = CreateGame();
            Assert.IsFalse(game.CanCheckToDraw(out _), "Not between hands.");

            game.PlaceBet();
            game.CheckToDraw();
            Assert.IsFalse(game.CanCheckToDraw(out string reason), "Not while drawing.");
            Assert.IsNotNull(reason);
            Assert.Throws<InvalidOperationException>(() => game.CheckToDraw());

            game.Draw(new int[0]);
            Assert.IsFalse(game.CanCheckToDraw(out _), "Not after the draw.");
        }

        [Test]
        public void CheckToDraw_IsLocked_InTheFinalStretch()
        {
            var game = CreateGame(Rules(startingYears: 250));
            game.PlaceBet();

            Assert.IsFalse(game.CanCheckToDraw(out string reason));
            Assert.IsNotNull(reason);
            Assert.Throws<InvalidOperationException>(() => game.CheckToDraw());
            Assert.AreEqual(3, game.PlayerCardsRevealed, "Nothing moved.");
        }

        // ------------------------------------------------------------------ a hand left behind

        [Test]
        public void CurrentHand_DescribesTheHandInPlay_AndIsNullBetweenHands()
        {
            var game = CreateGame();
            Assert.IsNull(game.CurrentHand);

            game.PlaceBet();
            game.Bet(BetAction.Raise);
            HandInProgress hand = game.CurrentHand;
            Assert.AreEqual(200, hand.Stake);
            Assert.AreEqual(100, hand.Ante);
            Assert.IsFalse(hand.IsAfterDraw);
            Assert.IsFalse(hand.IsSoulHand);
            Assert.IsFalse(hand.IsSealed);

            game.Bet(BetAction.Raise);
            Assert.IsTrue(game.CurrentHand.IsSealed);
            game.Draw(new int[0]);
            Assert.IsNull(game.CurrentHand, "Settled: nothing left to save.");
        }

        [Test]
        public void ForfeitHand_BeforeTheDraw_CostsLikeAFold()
        {
            var game = CreateGame();

            RoundResult round = game.ForfeitHand(new HandInProgress(200, 100, false, false, false));

            Assert.AreEqual(1100, game.Years, "Half the stake, as a fold before the draw.");
            Assert.IsTrue(round.Folded);
            Assert.AreEqual(100, round.YearsChange);
            Assert.AreEqual(GamePhase.Betting, game.Phase);
            Assert.AreSame(round, game.LastRound);
        }

        [Test]
        public void ForfeitHand_AfterTheDraw_CostsTheWholeStake()
        {
            var game = CreateGame();

            game.ForfeitHand(new HandInProgress(200, 100, true, false, false));

            Assert.AreEqual(1200, game.Years);
        }

        [Test]
        public void ForfeitHand_OnTheSoul_CostsHalfAgain()
        {
            var game = CreateGame();
            game.TakeOver(2600, 4);

            game.ForfeitHand(new HandInProgress(200, 100, false, true, false));

            Assert.AreEqual(2600 + 150, game.Years, "200 × 50% × 150%.");
        }

        [Test]
        public void ForfeitHand_Sealed_LosesTheWholeWager_AtTheDealersLossPercent()
        {
            var payouts = new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125);
            var game = CreateGame(payouts: payouts);

            game.ForfeitHand(new HandInProgress(300, 100, false, false, true));

            Assert.AreEqual(1000 + 375, game.Years, "A sealed hand cannot be folded: 300 lost, × 1.25 at this table.");
        }

        [Test]
        public void ForfeitHand_SealedOnTheSoul_AddsTheSoulSurcharge()
        {
            var payouts = new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125);
            var game = CreateGame(payouts: payouts);
            game.TakeOver(2100, 4);

            game.ForfeitHand(new HandInProgress(300, 100, true, true, true));

            Assert.AreEqual(2100 + 563, game.Years, "300 × 1.25 × 1.5 = 562.5, rounded up once.");
        }

        [Test]
        public void ForfeitHand_CanDamn()
        {
            var game = CreateGame();
            game.TakeOver(2900, 9);

            game.ForfeitHand(new HandInProgress(100, 100, true, true, true));

            Assert.AreEqual(GamePhase.Damned, game.Phase);
        }

        [Test]
        public void ForfeitHand_OnlyBetweenHands()
        {
            var game = CreateGame();
            game.PlaceBet();

            Assert.Throws<InvalidOperationException>(() => game.ForfeitHand(new HandInProgress(100, 100, false, false, false)));
            Assert.Throws<ArgumentNullException>(() => CreateGame().ForfeitHand(null));
        }
    }
}
