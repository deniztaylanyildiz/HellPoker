using System;
using HellPoker.Core.Betting;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The soul: past the dealer's soul line the soul (worth 1000 years) goes on the table. Bets are measured on the soul,
    /// losses cost half again, and when it is gone the player is damned. Wins can buy it back.
    /// </summary>
    public class SoulTests
    {
        // Player: high card. House: kings and fours that draw one blank. Player flush vs a pair of twos for wins.
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseTwoPair = "KS KH 4D 4C 9H";
        private const string Blanks = "3S 8D QD 6C";
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";

        private static GameRules Rules(int soulThreshold = 2000) =>
            new GameRules(1000, soulThreshold, forcedRaiseYears: 250, soulWorthYears: 1000, soulLossPercent: 150);

        private static HellPokerGame Game(string player, string house, int years, IHouseBettingStrategy houseBetting = null,
            int soulThreshold = 2000)
        {
            var game = new HellPokerGame(
                Rules(soulThreshold),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                PayoutTable.CreateDefault(),
                houseBetting);
            game.TakeOver(years, roundsPlayed: 3);
            return game;
        }

        private static void PassUntil(HellPokerGame game, GamePhase phase)
        {
            while (game.Phase != phase)
                game.Bet(BetAction.Pass);
        }

        private static void PlayPassively(HellPokerGame game)
        {
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ the line

        [Test]
        public void SoulGoesOnTheTable_AtTheLine()
        {
            Assert.IsFalse(Game(Nothing, HouseTwoPair, 1999).IsSoulAtStake);

            var game = Game(Nothing, HouseTwoPair, 2000);
            Assert.IsTrue(game.IsSoulAtStake);
            Assert.AreEqual(1000, game.SoulRemaining, "The whole soul, right at the line.");
        }

        [Test]
        public void SoulRemaining_ShrinksPastTheLine()
        {
            Assert.AreEqual(700, Game(Nothing, HouseTwoPair, 2300).SoulRemaining);
            Assert.AreEqual(1000, Game(Nothing, HouseTwoPair, 1200).SoulRemaining, "Below the line the soul is whole.");
        }

        [Test]
        public void Damnation_ComesWhenTheSoulIsGone()
        {
            Assert.AreEqual(3000, Rules().DamnationYears);
            Assert.AreEqual(GamePhase.Damned, Game(Nothing, HouseTwoPair, 3000).Phase);
            Assert.AreEqual(GamePhase.Betting, Game(Nothing, HouseTwoPair, 2999).Phase);
        }

        // ------------------------------------------------------------------ bets on the soul

        [Test]
        public void SoulHand_BetsInTenthsOfTheSoul_CappedAtThirtyPercent()
        {
            var game = Game(Nothing, HouseTwoPair, 2600);
            Assert.AreEqual(100, game.UpcomingAnte, "A tenth of the soul — not of 2600 years.");

            game.PlaceBet();

            Assert.IsTrue(game.IsSoulHand);
            Assert.AreEqual(100, game.Unit);
            Assert.AreEqual(100, game.Ante);
            Assert.AreEqual(300, game.TableCap);
        }

        [Test]
        public void SoulHand_NeverStakesMoreThanIsLeft()
        {
            var game = Game(Nothing, HouseTwoPair, 2850);   // 150 of the soul left

            game.PlaceBet();
            Assert.AreEqual(150, game.TableCap);
            Assert.AreEqual(50, game.WagerLeft);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(150, game.CurrentStake, "All of it.");
            Assert.AreEqual(0, game.WagerLeft);
            Assert.AreEqual(0, game.RaiseAmount);
        }

        [Test]
        public void SoulHand_SmallRemainder_IsAnAllInAnte()
        {
            var game = Game(Nothing, HouseTwoPair, 2960);   // 40 left

            Assert.AreEqual(40, game.UpcomingAnte);
            game.PlaceBet();
            Assert.AreEqual(40, game.CurrentStake);
            Assert.AreEqual(0, game.WagerLeft);
        }

        [Test]
        public void HouseReRaise_InTheSoulZone_MayPassTheCap_ButNotTheSoul()
        {
            var game = Game(Nothing, HouseTwoPair, 2000, new HellPokerGameTests.FixedHouseBetting(true));
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(300, game.CurrentStake, "The player's raise stops at the cap.");
            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
            Assert.AreEqual(100, game.HouseReRaiseAmount, "The house goes past the cap.");
        }

        [Test]
        public void HouseReRaise_InTheSoulZone_StopsWhenTheSoulIsAllOnTheTable()
        {
            var game = Game(Nothing, HouseTwoPair, 2700, new HellPokerGameTests.FixedHouseBetting(true));   // 300 left
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(300, game.CurrentStake);
            Assert.AreNotEqual(GamePhase.HouseReRaise, game.Phase, "Nothing left to wager.");
        }

        // ------------------------------------------------------------------ settling on the soul

        [Test]
        public void SoulLoss_CostsHalfAgain()
        {
            var safe = Game(Nothing, HouseTwoPair, 1500);
            PlayPassively(safe);
            Assert.AreEqual(1500 + 200, safe.Years, "Two pair ×2 on a 100 ante.");

            var soul = Game(Nothing, HouseTwoPair, 2000);
            PlayPassively(soul);
            Assert.AreEqual(2000 + 300, soul.Years, "The same loss ×1.5 with the soul at stake.");
            Assert.AreEqual(700, soul.SoulRemaining);
        }

        [Test]
        public void SoulFold_CostsHalfAgain()
        {
            var safe = Game(Nothing, HouseTwoPair, 1500);
            safe.PlaceBet();
            safe.Bet(BetAction.Raise);
            safe.Bet(BetAction.Fold);
            Assert.AreEqual(1500 + 100, safe.Years, "Half of 200 before the draw.");

            var soul = Game(Nothing, HouseTwoPair, 2000);
            soul.PlaceBet();
            soul.Bet(BetAction.Raise);
            soul.Bet(BetAction.Fold);
            Assert.AreEqual(2000 + 150, soul.Years, "The same fold ×1.5 with the soul at stake.");
        }

        [Test]
        public void SoulFold_RoundsUpOnce()
        {
            Assert.AreEqual(19, PayoutTable.CreateDefault().GetFoldPenalty(25, afterDraw: false, surchargePercent: 150), "25 × 50% × 1.5 = 18.75.");
            Assert.AreEqual(38, PayoutTable.CreateDefault().GetFoldPenalty(25, afterDraw: true, surchargePercent: 150), "25 × 1.5 = 37.5.");
        }

        [Test]
        public void SoulLoss_CombinesWithTheDealersLossPercent_RoundedUpOnce()
        {
            var payouts = new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125);
            Assert.AreEqual((int)Math.Ceiling(200 * 1.25 * 1.5), payouts.GetYearsAdded(HandCategory.TwoPair, 100, 100, 150));
        }

        [Test]
        public void LosingTheLastOfTheSoul_Damns()
        {
            var game = Game(Nothing, HouseTwoPair, 2850);

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Damned, game.Phase);
            Assert.AreEqual(0, game.SoulRemaining);
            Assert.IsTrue(game.IsGameOver);
        }

        [Test]
        public void SoulLoss_ThatLeavesSomething_PlaysOn()
        {
            var game = Game(Nothing, HouseTwoPair, 2600);

            PlayPassively(game);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.AreEqual(2900, game.Years);
            Assert.AreEqual(100, game.SoulRemaining);
        }

        [Test]
        public void Win_InTheSoulZone_MendsTheSoul()
        {
            var game = Game(Flush, HouseTwos, 2700);

            PlayPassively(game);

            Assert.AreEqual(2700 - 500, game.Years, "Flush ×5 on the ante.");
            Assert.AreEqual(800, game.SoulRemaining);
            Assert.IsTrue(game.IsSoulAtStake);
        }

        [Test]
        public void Win_BelowTheLine_GivesTheSoulBack()
        {
            var game = Game(Flush, HouseTwos, 2300);

            PlayPassively(game);
            Assert.IsTrue(game.IsSoulHand, "The hand was dealt with the soul on the table.");

            game.NextRound();

            Assert.AreEqual(1800, game.Years);
            Assert.IsFalse(game.IsSoulAtStake);
            Assert.IsFalse(game.IsSoulHand);
            Assert.AreEqual(100, game.UpcomingAnte, "Back to years: a tenth of 1800, in hundreds.");
        }

        // ------------------------------------------------------------------ changing tables

        [Test]
        public void LeavingTheTable_IsAllowedBetweenHands()
        {
            var game = Game(Nothing, HouseTwoPair, 1200);

            Assert.IsTrue(game.CanLeaveTable(out string reason));
            Assert.IsNull(reason);
        }

        [Test]
        public void LeavingTheTable_IsNotAllowedDuringAHand()
        {
            var game = Game(Nothing, HouseTwoPair, 1200);
            game.PlaceBet();

            Assert.IsFalse(game.CanLeaveTable(out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void LeavingTheTable_IsLocked_WhileTheSoulIsOnIt()
        {
            var game = Game(Nothing, HouseTwoPair, 2000);

            Assert.IsFalse(game.CanLeaveTable(out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TakeOver_CarriesTheSentenceAndTheRoundCount()
        {
            var game = Game(Nothing, HouseTwoPair, 1650);

            Assert.AreEqual(1650, game.Years);
            Assert.AreEqual(3, game.RoundNumber);
            Assert.AreEqual(GamePhase.Betting, game.Phase);
        }

        [Test]
        public void TakeOver_PastTheNewDealersLine_PutsTheSoulOnTheTableAtOnce()
        {
            // 1600 years is safe with Mammon (2000) but past Lilith's line (1500).
            var game = Game(Nothing, HouseTwoPair, 1600, soulThreshold: DealerRoster.Lilith.SoulThreshold);

            Assert.IsTrue(game.IsSoulAtStake);
            Assert.AreEqual(900, game.SoulRemaining);
            Assert.IsFalse(game.CanLeaveTable(out _));
        }

        [Test]
        public void TakeOver_OnlyBetweenHands()
        {
            var game = Game(Nothing, HouseTwoPair, 1200);
            game.PlaceBet();

            Assert.Throws<InvalidOperationException>(() => game.TakeOver(1300, 4));
        }

        [Test]
        public void TakeOver_OfZero_IsFreedom()
        {
            Assert.AreEqual(GamePhase.Absolved, Game(Nothing, HouseTwoPair, 0).Phase);
        }

        [Test]
        public void Factory_SwitchingDealers_KeepsTheSentence()
        {
            var table = new GameRules(1000, 2000);
            HellPokerGame mammon = HellPokerGameFactory.Create(table, DealerRoster.Mammon, seed: 3);
            mammon.TakeOver(1600, 7);
            Assert.IsFalse(mammon.IsSoulAtStake);

            HellPokerGame lilith = HellPokerGameFactory.Create(table, DealerRoster.Lilith, seed: 3);
            lilith.TakeOver(mammon.Years, mammon.RoundNumber);

            Assert.AreEqual(1600, lilith.Years);
            Assert.AreEqual(7, lilith.RoundNumber);
            Assert.IsTrue(lilith.IsSoulAtStake, "Lilith takes the soul at 1500.");
        }
    }
}
