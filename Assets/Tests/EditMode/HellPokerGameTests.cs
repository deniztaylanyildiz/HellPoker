using System;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class HellPokerGameTests
    {
        /// <summary>The house re-raises every time it can (or never).</summary>
        internal sealed class FixedHouseBetting : IHouseBettingStrategy
        {
            private readonly bool _reRaise;
            public int Asked { get; private set; }

            public FixedHouseBetting(bool reRaise) => _reRaise = reRaise;

            public bool WantsToReRaise(HandEvaluation houseHand)
            {
                Asked++;
                return _reRaise;
            }
        }

        // Player: high card. House: a full house that stands pat.
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";

        private static GameRules Rules(int startingYears = 1000, int soulThreshold = 2000, int forcedRaiseYears = 250, int houseCardsShown = 2,
            int tableCapPercent = 30, int soulWorthYears = 1000, int soulLossPercent = 150)
        {
            return new GameRules(startingYears, soulThreshold, forcedRaiseYears: forcedRaiseYears, houseCardsShown: houseCardsShown,
                stakes: new StakeScale(tableCapPercent: tableCapPercent), soulWorthYears: soulWorthYears, soulLossPercent: soulLossPercent);
        }

        /// <summary>A roomier table (cap 500 of 1000), so a raise after the draw still leaves room for a house re-raise.</summary>
        private static GameRules RoomyRules() => Rules(tableCapPercent: 50);

        /// <summary>Deals the player's five, then the house's five, then the replacement cards in order.</summary>
        private static HellPokerGame CreateGame(string player, string house, string rest = "", GameRules rules = null,
            IPayoutTable payouts = null, IHouseBettingStrategy houseBetting = null)
        {
            return new HellPokerGame(
                rules ?? Rules(),
                TestDecks.Stacked($"{player} {house} {rest}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                payouts ?? PayoutTable.CreateDefault(),
                houseBetting);
        }

        private static void PassUntil(HellPokerGame game, GamePhase phase)
        {
            while (game.Phase != phase)
                game.Bet(BetAction.Pass);
        }

        /// <summary>Deals, passes every decision, keeps all cards.</summary>
        private static RoundResult PlayPassively(HellPokerGame game)
        {
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
            return game.LastRound;
        }

        private static HellPokerGame GameAtDrawReveal(IHouseBettingStrategy houseBetting = null, GameRules rules = null)
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: rules, houseBetting: houseBetting);
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            return game;
        }

        // ------------------------------------------------------------------ deal and ante

        [Test]
        public void NewGame_StartsInBetting_WithAnteOfOneUnit()
        {
            var game = CreateGame(Nothing, HouseFullHouse);

            Assert.AreEqual(GamePhase.Betting, game.Phase);
            Assert.AreEqual(1000, game.Years);
            Assert.AreEqual(100, game.UpcomingAnte);
        }

        [Test]
        public void PlaceBet_PutsDownOneUnit_AndTurnsTheOpeningCardsPlusOne()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            game.PlaceBet();

            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
            Assert.AreEqual(100, game.Unit);
            Assert.AreEqual(100, game.Ante);
            Assert.AreEqual(100, game.CurrentStake);
            Assert.AreEqual(300, game.TableCap);
            Assert.AreEqual(3, game.PlayerCardsRevealed, "Two cards turn together, the third brings the first decision.");
            Assert.AreEqual(0, game.HouseCardsRevealed);
            Assert.AreEqual(TestCards.Hand("2C 3C 4C 5C 7D").ToString(), game.PlayerHand.ToString());
            Assert.AreEqual(TestCards.Hand("2D 3D 4D 5D 7H").ToString(), game.HouseHand.ToString());
        }

        [TestCase(650, 50)]
        [TestCase(340, 25)]
        [TestCase(180, 10)]
        [TestCase(6, 6)]
        public void Ante_FollowsTheSentence(int years, int ante)
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(startingYears: years));

            game.PlaceBet();

            Assert.AreEqual(ante, game.Ante);
        }

        [Test]
        public void WrongPhaseActions_Throw()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");

            Assert.Throws<InvalidOperationException>(() => game.Draw(new int[0]));
            Assert.Throws<InvalidOperationException>(() => game.Bet(BetAction.Pass));

            game.PlaceBet();
            Assert.Throws<InvalidOperationException>(() => game.PlaceBet());
            Assert.Throws<InvalidOperationException>(() => game.Draw(new int[0]));
            Assert.IsFalse(game.CanBet(BetAction.Call, out _), "Nothing to call without a house re-raise.");
        }

        // ------------------------------------------------------------------ the flow of a hand

        [Test]
        public void PlayerDecides_OnCardsThreeFourAndFive_ThenDraws()
        {
            var game = CreateGame("2C 3C 4C 5C 7D", "2D 3D 4D 5D 7H");
            game.PlaceBet();

            for (int revealed = 3; revealed <= Hand.Size; revealed++)
            {
                Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
                Assert.AreEqual(revealed, game.PlayerCardsRevealed);
                game.Bet(BetAction.Pass);
            }

            Assert.AreEqual(GamePhase.Drawing, game.Phase);
        }

        [Test]
        public void AfterTheDraw_OneDecision_ThenHouseShowsItsCards_ThenShowdown()
        {
            var game = GameAtDrawReveal();

            Assert.AreEqual(GamePhase.DrawReveal, game.Phase);
            Assert.IsTrue(game.IsAfterDraw);
            Assert.AreEqual(0, game.HouseCardsRevealed);

            game.Bet(BetAction.Pass);
            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
            Assert.AreEqual(2, game.HouseCardsRevealed);

            game.Bet(BetAction.Pass);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.AreEqual(Hand.Size, game.HouseCardsRevealed);
        }

        [Test]
        public void AHand_HasAtMostFiveDecisions()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();
            int decisions = 0;

            while (!game.IsGameOver && game.Phase != GamePhase.RoundOver)
            {
                if (game.Phase == GamePhase.Drawing)
                {
                    game.Draw(new int[0]);
                    continue;
                }
                game.Bet(BetAction.Pass);
                decisions++;
            }

            Assert.AreEqual(5, decisions);
        }

        [Test]
        public void NoHouseCardsShown_SkipsTheHouseDecision()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(houseCardsShown: 0));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Pass);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
        }

        [Test]
        public void HouseCardsShown_ComesFromTheRules()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(houseCardsShown: 1));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Pass);

            Assert.AreEqual(1, game.HouseCardsRevealed);
        }

        // ------------------------------------------------------------------ raising

        [Test]
        public void RaiseBeforeTheDraw_AddsOneUnit()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();

            Assert.AreEqual(100, game.RaiseAmount);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(200, game.CurrentStake);
        }

        [Test]
        public void RaiseAfterTheDraw_AddsTwoUnits()
        {
            var game = GameAtDrawReveal();

            Assert.AreEqual(200, game.RaiseAmount);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(300, game.CurrentStake);
        }

        [Test]
        public void StakeOnTable_ComesOffTheYearsOffTable()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();
            Assert.AreEqual(900, game.YearsOffTable);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(800, game.YearsOffTable);
            Assert.AreEqual(1000, game.Years, "The sentence itself only changes when the hand is settled.");
        }

        [Test]
        public void TableCap_IsThirtyPercentOfTheSentence_AndLocksRaising()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(300, game.CurrentStake);
            Assert.AreEqual(0, game.RaiseAmount);
            Assert.IsFalse(game.CanBet(BetAction.Raise, out string reason));
            Assert.IsNotNull(reason);
            Assert.IsTrue(game.IsCommitted, "A full table seals the pact.");
        }

        [Test]
        public void DoubleRaise_IsCutDown_ToWhatTheCapLeaves()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            Assert.AreEqual(100, game.RaiseAmount);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(300, game.CurrentStake);
        }

        [Test]
        public void TinySentence_GoesAllIn_WithNothingLeftToRaise()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(startingYears: 6));
            game.PlaceBet();

            Assert.AreEqual(6, game.CurrentStake);
            Assert.AreEqual(0, game.YearsOffTable);
            Assert.AreEqual(0, game.RaiseAmount);
        }

        // ------------------------------------------------------------------ the final stretch

        [Test]
        public void FinalStretch_ForbidsPassing_UntilTheTableIsFull()
        {
            // 250 years: unit 25, ante 25, cap 75.
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(startingYears: 250, forcedRaiseYears: 250));
            game.PlaceBet();

            Assert.IsTrue(game.IsRaiseForced);
            Assert.IsFalse(game.CanBet(BetAction.Pass, out string reason));
            Assert.IsNotNull(reason);
            Assert.Throws<InvalidOperationException>(() => game.Bet(BetAction.Pass));
            Assert.IsTrue(game.CanBet(BetAction.Fold, out _));

            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Raise);

            Assert.AreEqual(75, game.CurrentStake);
            Assert.IsTrue(game.IsCommitted, "At the cap there is nothing left to raise: the pact is sealed, nothing more is asked.");
            Assert.AreEqual(GamePhase.Drawing, game.Phase, "The last card turned by itself; the draw is still the player's.");
        }

        [Test]
        public void AboveFinalStretch_PassingIsAllowed()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(startingYears: 251, forcedRaiseYears: 250));
            game.PlaceBet();

            Assert.IsFalse(game.IsRaiseForced);
            Assert.IsTrue(game.CanBet(BetAction.Pass, out _));
        }

        // ------------------------------------------------------------------ the house re-raises

        [Test]
        public void RaiseAfterTheDraw_CanBeAnsweredByAHouseReRaise()
        {
            var game = GameAtDrawReveal(new FixedHouseBetting(true), RoomyRules());

            game.Bet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
            Assert.AreEqual(100, game.HouseReRaiseAmount, "One unit.");
            Assert.AreEqual(300, game.CurrentStake, "The re-raise is not on the table until it is called.");
            Assert.IsTrue(game.CanBet(BetAction.Call, out _));
            Assert.IsTrue(game.CanBet(BetAction.Fold, out _));
            Assert.IsFalse(game.CanBet(BetAction.Raise, out _));
            Assert.IsFalse(game.CanBet(BetAction.Pass, out _));
        }

        [Test]
        public void CallingTheReRaise_PutsItOnTheTable_AndPlayGoesOn()
        {
            var game = GameAtDrawReveal(new FixedHouseBetting(true), RoomyRules());
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Call);

            Assert.AreEqual(400, game.CurrentStake);
            Assert.AreEqual(0, game.HouseReRaiseAmount);
            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
        }

        [Test]
        public void ReRaiseAtTheLastDecision_GoesToShowdownWhenCalled()
        {
            var game = GameAtDrawReveal(new FixedHouseBetting(true), RoomyRules());
            game.Bet(BetAction.Pass);
            game.Bet(BetAction.Raise);
            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);

            game.Bet(BetAction.Call);

            Assert.IsNotNull(game.LastRound?.Showdown, "The hand went to the showdown.");
            Assert.AreEqual(400, game.LastRound.Stake);
        }

        [Test]
        public void FoldingToTheReRaise_CostsTheStakeBeforeIt()
        {
            var game = GameAtDrawReveal(new FixedHouseBetting(true), RoomyRules());
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Fold);

            Assert.IsTrue(game.LastRound.Folded);
            Assert.AreEqual(1300, game.Years, "After the draw folding costs the whole stake (300), not the uncalled re-raise.");
        }

        [Test]
        public void HouseNeverReRaises_BeforeTheDraw()
        {
            var betting = new FixedHouseBetting(true);
            var game = CreateGame(Nothing, HouseFullHouse, houseBetting: betting);
            game.PlaceBet();

            game.Bet(BetAction.Raise);

            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
            Assert.AreEqual(0, betting.Asked);
        }

        [Test]
        public void HouseReRaise_MayGoPastTheCap()
        {
            var game = GameAtDrawReveal(new FixedHouseBetting(true));

            game.Bet(BetAction.Raise);
            Assert.AreEqual(300, game.CurrentStake, "The player's raise stops at the cap.");
            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
            Assert.AreEqual(100, game.HouseReRaiseAmount);

            game.Bet(BetAction.Call);

            Assert.AreEqual(400, game.LastRound.Stake, "A called re-raise takes the table past the cap.");
            Assert.Greater(game.LastRound.Stake, game.TableCap);
            Assert.IsTrue(game.IsCommitted, "Past the cap the pact is sealed: the last decision was passed for the player.");
            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
        }

        [Test]
        public void HouseReRaise_NeverExceedsTheSentence()
        {
            // 15 years: unit 10, ante 10, cap 10 — after a raise nothing is left but 5.
            var rules = new GameRules(15, 2000, forcedRaiseYears: 0, stakes: new StakeScale(tableCapPercent: 100));
            var game = CreateGame(Nothing, HouseFullHouse, rules: rules, houseBetting: new FixedHouseBetting(true));
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(15, game.CurrentStake, "All in.");
            Assert.AreNotEqual(GamePhase.HouseReRaise, game.Phase, "Nothing left to re-raise with.");
        }

        [Test]
        public void HouseThatDeclines_LetsTheRaiseStand()
        {
            var betting = new FixedHouseBetting(false);
            var game = GameAtDrawReveal(betting, RoomyRules());

            game.Bet(BetAction.Raise);

            Assert.AreEqual(1, betting.Asked, "The house had room and was asked.");
            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
        }

        // ------------------------------------------------------------------ settling

        [Test]
        public void PlayerWin_ForgivesStakeTimesPlayersMultiplier()
        {
            // Player stands on a flush; house holds a pair of twos and draws three blanks.
            var game = CreateGame("2C 9C JC 4C KC", "2D 2H 5S 7H 9D", "3S 4S 6D");

            RoundResult result = PlayPassively(game);

            Assert.AreEqual(ShowdownOutcome.PlayerWins, result.Showdown.Outcome);
            Assert.AreEqual(HandCategory.Flush, result.Showdown.Player.Category);
            Assert.AreEqual(100, result.Stake);
            Assert.AreEqual(1000 - 100 * 5, game.Years);
            Assert.IsFalse(result.Folded);
        }

        [Test]
        public void HouseWin_AddsStakeTimesHousesMultiplier()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(soulThreshold: 5000));
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            PassUntil(game, GamePhase.RoundOver);

            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(1000 + 200 + 100 * 7, game.Years, "Full house ×8 on the ante, the raise one to one.");
        }

        [Test]
        public void PlayerWin_MultipliesTheAnte_RaisesPayOneToOne()
        {
            // Full house for the player (stands), a pair of twos for the house (draws three blanks).
            var game = CreateGame("QS QH QD 7C 7H", "2D 2H 5S 8H 9D", "3S 4S 6C");
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            PassUntil(game, GamePhase.Drawing);
            game.Draw(new int[0]);
            PassUntil(game, GamePhase.RoundOver);

            Assert.AreEqual(200, game.LastRound.Stake);
            Assert.AreEqual(1000 - (200 + 100 * 7), game.Years, "Full house ×8 on the 100 ante, the 100 raise one to one: 900 forgiven.");
        }

        [Test]
        public void HouseWin_UsesTheDealersLossPercent()
        {
            var payouts = new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 150);
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(soulThreshold: 5000), payouts: payouts);

            PlayPassively(game);

            Assert.AreEqual(1000 + 100 * 8 * 3 / 2, game.Years);
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
        public void FoldBeforeTheDraw_CostsHalfTheStake()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            game.PlaceBet();
            game.Bet(BetAction.Raise);

            game.Bet(BetAction.Fold);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
            Assert.IsTrue(game.LastRound.Folded);
            Assert.IsNull(game.LastRound.Showdown);
            Assert.AreEqual(1100, game.Years);
        }

        [Test]
        public void FoldAfterTheDraw_CostsTheWholeStake()
        {
            var game = GameAtDrawReveal();

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1100, game.Years);
        }

        [Test]
        public void DrawingIntoDeadMansHand_AbsolvesEverything()
        {
            var game = CreateGame("AS AC 8S 2D 3H", "AH AD KS KC 4D", "8C 9D QH");
            game.PlaceBet();
            PassUntil(game, GamePhase.Drawing);

            game.Draw(new[] { 3, 4 });
            PassUntil(game, GamePhase.Absolved);

            Assert.AreEqual(HandCategory.DeadMansHand, game.LastRound.Showdown.Player.Category);
            Assert.AreEqual(0, game.Years);
            Assert.IsTrue(game.IsGameOver);
        }

        [Test]
        public void DeadMansHand_BeatsARoyalFlush_AtTheTable()
        {
            var game = CreateGame("AS AC 8S 8C 2D", "10H JH QH KH AH");

            RoundResult result = PlayPassively(game);

            Assert.AreEqual(ShowdownOutcome.PlayerWins, result.Showdown.Outcome);
            Assert.AreEqual(GamePhase.Absolved, game.Phase);
            Assert.AreEqual(0, game.Years);
        }

        [TestCase("2D 3H 5S 9C JD")]
        [TestCase("KD KH 5S 9C JD")]
        [TestCase("KD KH 5S 5C JD")]
        [TestCase("KD KH KS 9C JD")]
        [TestCase("9D 10H JS QC KD")]
        [TestCase("2H 5H 9H JH KH")]
        [TestCase("KD KH KS 9C 9D")]
        [TestCase("KD KH KS KC 9D")]
        [TestCase("9H 10H JH QH KH")]
        [TestCase("10H JH QH KH AH")]
        public void DeadMansHand_IsUnbeatable(string house)
        {
            var evaluator = HandEvaluator.CreateDefault();
            HandEvaluation deadMan = evaluator.Evaluate(TestCards.Hand("AS AC 8S 8C 2D"));

            Assert.AreEqual(ShowdownOutcome.PlayerWins, ShowdownResult.Resolve(deadMan, evaluator.Evaluate(TestCards.Hand(house))).Outcome);
            Assert.AreEqual(ShowdownOutcome.HouseWins, ShowdownResult.Resolve(evaluator.Evaluate(TestCards.Hand(house)), deadMan).Outcome);
        }

        [Test]
        public void LosingTheWholeSoul_EndsGame()
        {
            // Soul line 1001, soul worth 500: damned at 1501. A passive loss to a full house adds 800.
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(startingYears: 1000, soulThreshold: 1001, soulWorthYears: 500));

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Damned, game.Phase);
            Assert.Throws<InvalidOperationException>(() => game.NextRound());
        }

        [Test]
        public void Outlook_ShowsTheLeastAWinOrLossCanMove()
        {
            var game = CreateGame(Nothing, HouseFullHouse);
            Assert.AreEqual(100, game.LeastYearsForgiven, "Between hands it is measured on the upcoming ante.");
            Assert.AreEqual(100, game.LeastYearsAdded);

            game.PlaceBet();
            game.Bet(BetAction.Raise);

            Assert.AreEqual(200, game.LeastYearsForgiven);
            Assert.AreEqual(200, game.LeastYearsAdded);
        }

        [Test]
        public void NextRound_ClearsHand_AndRestartResets()
        {
            var game = CreateGame(Nothing, HouseFullHouse, rules: Rules(soulThreshold: 5000));
            PlayPassively(game);

            game.NextRound();
            Assert.AreEqual(GamePhase.Betting, game.Phase);
            Assert.IsNull(game.PlayerHand);
            Assert.AreEqual(0, game.CurrentStake);
            Assert.AreEqual(0, game.TableCap);

            game.Restart();
            Assert.AreEqual(1000, game.Years);
            Assert.AreEqual(0, game.RoundNumber);
        }

        [Test]
        public void DefaultGame_PlaysManyRounds_WithinTheCap()
        {
            var game = HellPokerGameFactory.Create(seed: 1234, betting: new HouseBettingStyle(HandCategory.TwoPair, 70, 20));
            var strategy = new HouseDrawStrategy();
            var random = new Random(99);
            var actions = new[] { BetAction.Raise, BetAction.Pass, BetAction.Fold, BetAction.Call };

            for (int round = 0; round < 500; round++)
            {
                if (game.IsGameOver) game.Restart();

                int sentence = game.Years;
                game.PlaceBet();
                Assert.LessOrEqual(game.TableCap, Math.Max(game.Ante, sentence * 30 / 100));

                while (!game.IsGameOver && game.Phase != GamePhase.RoundOver)
                {
                    if (game.Phase == GamePhase.Drawing)
                    {
                        game.Draw(strategy.ChooseDiscards(game.PlayerHand));
                        continue;
                    }

                    BetAction action = actions[random.Next(actions.Length)];
                    if (!game.CanBet(action, out _))
                        action = game.CanBet(BetAction.Call, out _) ? BetAction.Call
                            : game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Pass;
                    game.Bet(action);

                    if (game.Phase != GamePhase.RoundOver && !game.IsGameOver)
                    {
                        Assert.LessOrEqual(game.CurrentStake + game.HouseReRaiseAmount, sentence, "Stake never exceeds the sentence.");
                        if (game.CurrentStake > game.TableCap)
                            Assert.AreEqual(0, game.RaiseAmount, "Only a house re-raise goes past the cap; the player cannot follow.");
                    }
                }

                if (!game.IsGameOver) game.NextRound();
            }

            Assert.Pass();
        }
    }
}
