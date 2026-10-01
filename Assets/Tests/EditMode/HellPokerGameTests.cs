using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class HellPokerGameTests
    {
        private static GameRules Rules(int startingYears = 1000, int damnationYears = 2000, int forcedRaiseYears = 250, int houseRevealDecisions = 3)
        {
            return new GameRules(startingYears, damnationYears, minStake: 10, maxStake: 200,
                forcedRaiseYears: forcedRaiseYears, houseRevealDecisions: houseRevealDecisions);
        }

        /// <summary>Deals the player's five, then the house's five, then the replacement cards in order.</summary>
        private static HellPokerGame CreateGame(string player, string house, string rest = "", GameRules rules = null)
        {
            return new HellPokerGame(
                rules ?? Rules(),
                TestDecks.Stacked($"{player} {house} {rest}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                PayoutTable.CreateDefault());
        }

        private static void PassUntil(HellPokerGame game, GamePhase phase)
        {
            while (game.Phase != phase)
                game.Bet(BetAction.Pass);
        }

        /// <summary>Bets the ante, passes every decision, keeps all cards.</summary>
        private static RoundResult PlayPassively(HellPokerGame game, int stake = 50)
        {
            game.PlaceBet(stake);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
            return game.LastRound;
        }

        [Test]
        public void NewGame_StartsInBettingWithFullSentence()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            Assert.AreEqual(GamePhase.Betting, game.Phase);
            Assert.AreEqual(1000, game.Years);
        }

        [Test]
        public void PlaceBet_DealsBothHands_AndTurnsFirstPlayerCard()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            game.PlaceBet(100);

            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
            Assert.AreEqual(1, game.PlayerCardsRevealed);
            Assert.AreEqual(0, game.HouseCardsRevealed);
            Assert.AreEqual(100, game.Ante);
            Assert.AreEqual(100, game.CurrentStake);
            Assert.AreEqual(TestCards.Hand("2C 3C 4C 5C 7D").ToString(), game.PlayerHand.ToString());
            Assert.AreEqual(TestCards.Hand("2D 3D 4D 5D 7H").ToString(), game.HouseHand.ToString());
        }

        [TestCase(5)]
        [TestCase(201)]
        public void PlaceBet_OutOfRange_Throws(int stake)
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            Assert.Throws<ArgumentOutOfRangeException>(() => game.PlaceBet(stake));
        }

        [Test]
        public void WrongPhaseActions_Throw()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            Assert.Throws<InvalidOperationException>(() => game.Draw(new int[0]));
            Assert.Throws<InvalidOperationException>(() => game.Bet(BetAction.Pass));

            game.PlaceBet(50);
            Assert.Throws<InvalidOperationException>(() => game.Draw(new int[0]));
        }

        [Test]
        public void PlayerReveal_EachDecisionTurnsNextCard_ThenDrawing()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");
            game.PlaceBet(50);

            for (int revealed = 1; revealed <= Hand.Size; revealed++)
            {
                Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
                Assert.AreEqual(revealed, game.PlayerCardsRevealed);
                game.Bet(BetAction.Pass);
            }

            Assert.AreEqual(GamePhase.Drawing, game.Phase);
        }

        [Test]
        public void Raise_AddsAnteEachTime()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");
            game.PlaceBet(50);

            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Pass);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(150, game.CurrentStake);
        }

        [Test]
        public void Draw_MovesToHouseReveal_WithFirstHouseCardTurned()
        {
            // The house holds four diamonds and draws one card for the flush.
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H", "KH");
            game.PlaceBet(50);
            PassUntil(game, GamePhase.Drawing);

            game.Draw(new int[0]);

            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
            Assert.AreEqual(1, game.HouseCardsRevealed);
        }

        [Test]
        public void HouseReveal_EndsInShowdown_AfterConfiguredDecisions()
        {
            var game = CreateGame("2C 9C JC 4C KC", "2D 2H 5S 7H 9D", "3S 4S 6D", Rules(houseRevealDecisions: 3));
            game.PlaceBet(50);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Pass);
            game.Bet(BetAction.Pass);
            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
            Assert.AreEqual(3, game.HouseCardsRevealed);

            game.Bet(BetAction.Pass);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.AreEqual(Hand.Size, game.HouseCardsRevealed);
        }

        [Test]
        public void NoHouseDecisions_DrawGoesStraightToShowdown()
        {
            var game = CreateGame("2C 9C JC 4C KC", "2D 2H 5S 7H 9D", "3S 4S 6D", Rules(houseRevealDecisions: 0));
            game.PlaceBet(50);
            PassUntil(game, GamePhase.Drawing);

            game.Draw(new int[0]);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
        }

        [Test]
        public void PlayerWin_ForgivesTotalStakeTimesMultiplier()
        {
            // Player stands on a flush; house holds a pair of twos and draws three blanks.
            var game = CreateGame("2C 9C JC 4C KC", "2D 2H 5S 7H 9D", "3S 4S 6D");
            game.PlaceBet(50);
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.RoundOver);

            RoundResult result = game.LastRound;
            Assert.AreEqual(ShowdownOutcome.PlayerWins, result.Showdown.Outcome);
            Assert.AreEqual(HandCategory.Flush, result.Showdown.Player.Category);
            Assert.AreEqual(150, result.Stake);
            Assert.AreEqual(1000 - 150 * 5, game.Years);
            Assert.IsFalse(result.Folded);
        }

        [Test]
        public void HouseWin_AddsTotalStake()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            game.PlaceBet(50);
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            PassUntil(game, GamePhase.RoundOver);

            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(1100, game.Years);
        }

        [Test]
        public void Push_ChangesNothing()
        {
            var game = CreateGame("10S 9H 8D 7C 6H", "10C 9D 8S 7H 6C");

            RoundResult result = PlayPassively(game);

            Assert.AreEqual(ShowdownOutcome.Push, result.Showdown.Outcome);
            Assert.AreEqual(1000, game.Years);
        }

        [Test]
        public void FoldBeforeDraw_CostsHalfTheStake()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            game.PlaceBet(50);
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Fold);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.IsTrue(game.LastRound.Folded);
            Assert.IsNull(game.LastRound.Showdown);
            Assert.AreEqual(1050, game.Years);
        }

        [Test]
        public void FoldDuringHouseReveal_RoundsPenaltyUp()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            game.PlaceBet(25);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Fold);

            Assert.IsTrue(game.LastRound.Folded);
            Assert.AreEqual(1013, game.Years);
        }

        [Test]
        public void FinalStretch_ForbidsPassing_ButAllowsRaiseAndFold()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 250, damnationYears: 2000, forcedRaiseYears: 250));
            game.PlaceBet(10);

            Assert.IsTrue(game.IsRaiseForced);
            Assert.IsFalse(game.CanBet(BetAction.Pass, out string reason));
            Assert.IsNotNull(reason);
            Assert.Throws<InvalidOperationException>(() => game.Bet(BetAction.Pass));
            Assert.IsTrue(game.CanBet(BetAction.Raise, out _));
            Assert.IsTrue(game.CanBet(BetAction.Fold, out _));
        }

        [Test]
        public void AboveFinalStretch_PassingIsAllowed()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 251, forcedRaiseYears: 250));
            game.PlaceBet(10);

            Assert.IsFalse(game.IsRaiseForced);
            Assert.IsTrue(game.CanBet(BetAction.Pass, out _));
        }

        [Test]
        public void StakeOnTable_ComesOffTheYearsOffTable()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            game.PlaceBet(200);
            Assert.AreEqual(800, game.YearsOffTable);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(400, game.CurrentStake);
            Assert.AreEqual(600, game.YearsOffTable);
            Assert.AreEqual(1000, game.Years, "The sentence itself only changes when the hand is settled.");
        }

        [Test]
        public void TotalStake_CannotExceedTheSentence()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            game.PlaceBet(200);

            for (int i = 0; i < 4; i++) game.Bet(BetAction.Raise);

            Assert.AreEqual(1000, game.CurrentStake);
            Assert.AreEqual(0, game.RaiseAmount);
            Assert.IsFalse(game.CanBet(BetAction.Raise, out string reason));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void LastRaise_GoesAllInWithWhatIsLeft()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 130));
            game.PlaceBet(50);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(30, game.RaiseAmount);
            game.Bet(BetAction.Raise);
            Assert.AreEqual(130, game.CurrentStake);
        }

        [Test]
        public void AnteAboveSentence_IsNotAllowed()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 80));

            Assert.IsTrue(game.IsValidStake(50));
            Assert.IsFalse(game.IsValidStake(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.PlaceBet(100));
        }

        [Test]
        public void MinimumAnte_GoesAllIn_WhenLessIsLeft()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 6));

            Assert.IsTrue(game.IsValidStake(10));
            game.PlaceBet(10);

            Assert.AreEqual(6, game.CurrentStake);
            Assert.AreEqual(0, game.YearsOffTable);
        }

        [Test]
        public void FinalStretch_AllIn_AllowsPassingAgain()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 100, forcedRaiseYears: 250));
            game.PlaceBet(50);
            Assert.IsFalse(game.CanBet(BetAction.Pass, out _));

            game.Bet(BetAction.Raise);

            Assert.AreEqual(100, game.CurrentStake);
            Assert.IsTrue(game.CanBet(BetAction.Pass, out _));
        }

        [Test]
        public void AllInLoss_DoublesTheSentence()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 100, damnationYears: 300, forcedRaiseYears: 0));
            game.PlaceBet(100);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            PassUntil(game, GamePhase.RoundOver);

            Assert.AreEqual(200, game.Years);
        }

        [Test]
        public void DrawingIntoDeadMansHand_AbsolvesEverything()
        {
            var game = CreateGame("AS AC 8S 2D 3H", "AH AD KS KC 4D", "8C 9D QH");
            game.PlaceBet(10);
            PassUntil(game, GamePhase.Drawing);

            game.Draw(new[] { 3, 4 });
            PassUntil(game, GamePhase.Absolved);

            Assert.AreEqual(HandCategory.DeadMansHand, game.LastRound.Showdown.Player.Category);
            Assert.AreEqual(0, game.Years);
            Assert.IsTrue(game.IsGameOver);
        }

        [Test]
        public void ReachingDamnationLimit_EndsGame()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H", rules: Rules(startingYears: 100, damnationYears: 150, forcedRaiseYears: 0));

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Damned, game.Phase);
            Assert.Throws<InvalidOperationException>(() => game.NextRound());
        }

        [Test]
        public void NextRound_ClearsHand_AndRestartResets()
        {
            var game = CreateGame("2C 5D 7H 9S JC", "KS KH KD 4C 4H");
            PlayPassively(game);

            game.NextRound();
            Assert.AreEqual(GamePhase.Betting, game.Phase);
            Assert.IsNull(game.PlayerHand);
            Assert.AreEqual(0, game.CurrentStake);

            game.Restart();
            Assert.AreEqual(1000, game.Years);
            Assert.AreEqual(0, game.RoundNumber);
        }

        [Test]
        public void DefaultGame_PlaysManyRoundsWithoutErrors()
        {
            var game = HellPokerGameFactory.Create(seed: 1234);
            var strategy = new HouseDrawStrategy();
            var random = new Random(99);

            for (int round = 0; round < 500; round++)
            {
                if (game.IsGameOver) game.Restart();

                int stake = game.Rules.MaxStake;
                while (!game.IsValidStake(stake)) stake--;
                game.PlaceBet(stake);
                while (game.Phase == GamePhase.PlayerReveal || game.Phase == GamePhase.HouseReveal || game.Phase == GamePhase.Drawing)
                {
                    if (game.Phase == GamePhase.Drawing)
                    {
                        game.Draw(strategy.ChooseDiscards(game.PlayerHand));
                        continue;
                    }

                    var action = (BetAction)random.Next(3);
                    if (!game.CanBet(action, out _)) action = game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Pass;
                    game.Bet(action);
                    if (!game.IsGameOver && game.Phase != GamePhase.RoundOver)
                        Assert.LessOrEqual(game.CurrentStake, game.Years, "Stake must never exceed the sentence.");
                }

                if (!game.IsGameOver) game.NextRound();
            }

            Assert.Pass();
        }
    }
}
