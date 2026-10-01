using System;
using System.Linq;
using System.Text;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Not a real test: plays thousands of runs against every dealer with a simple, sensible player and reports
    /// how often runs end absolved or damned. Explicit, so normal runs skip it. Run with:
    ///   -testPlatform EditMode -testFilter HellPoker.Core.Tests.BalanceSimulation
    /// The report is in the test's output (results.xml).
    /// </summary>
    [Explicit, Category("Simulation")]
    public class BalanceSimulation
    {
        private const int Runs = 2000;
        private const int MaxHandsPerRun = 400;

        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        [Test]
        public void EveryDealer()
        {
            var report = new StringBuilder();
            report.AppendLine($"{Runs} runs per dealer, at most {MaxHandsPerRun} hands each.");
            report.AppendLine("dealer   absolved  damned  unfinished  avg hands  dead man's hand wins");

            foreach (Dealer dealer in DealerRoster.All)
            {
                int absolved = 0, damned = 0, hands = 0, deadMan = 0;
                for (int run = 0; run < Runs; run++)
                {
                    HellPokerGame game = HellPokerGameFactory.Create(GameRules.Default, dealer, seed: run * 7919 + 13);
                    var player = new HouseDrawStrategy(game.Rules.MaxDiscards);
                    int played = 0;

                    while (!game.IsGameOver && played < MaxHandsPerRun)
                    {
                        PlayHand(game, player);
                        played++;
                        if (game.LastRound.Showdown?.Player.Category == HandCategory.DeadMansHand &&
                            game.LastRound.Showdown.Outcome == ShowdownOutcome.PlayerWins)
                            deadMan++;
                        if (!game.IsGameOver) game.NextRound();
                    }

                    hands += played;
                    if (game.Phase == GamePhase.Absolved) absolved++;
                    else if (game.Phase == GamePhase.Damned) damned++;
                }

                report.AppendLine($"{dealer.Id,-8} {Percent(absolved),7}  {Percent(damned),6}  {Percent(Runs - absolved - damned),10}  " +
                                  $"{hands / (double)Runs,9:0.0}  {deadMan,6}");
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static string Percent(int count) => (100.0 * count / Runs).ToString("0.0") + "%";

        /// <summary>
        /// A plain player: raises on a visible pair before the draw and on two pair or better after it, never folds a
        /// made hand, calls re-raises with a pair or better. Uses only cards that are face up.
        /// </summary>
        private static void PlayHand(HellPokerGame game, IDrawStrategy drawing)
        {
            game.PlaceBet();
            while (!game.IsGameOver && game.Phase != GamePhase.RoundOver)
            {
                switch (game.Phase)
                {
                    case GamePhase.Drawing:
                        game.Draw(drawing.ChooseDiscards(game.PlayerHand));
                        break;

                    case GamePhase.HouseReRaise:
                        game.Bet(Strength(game) >= HandCategory.OnePair ? BetAction.Call : BetAction.Fold);
                        break;

                    default:
                        bool strong = game.IsAfterDraw ? Strength(game) >= HandCategory.TwoPair : VisiblePair(game);
                        BetAction action = strong && game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Pass;
                        if (!game.CanBet(action, out _))
                            action = game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Fold;
                        game.Bet(action);
                        break;
                }
            }
        }

        private static HandCategory Strength(HellPokerGame game) => Evaluator.Evaluate(game.PlayerHand).Category;

        private static bool VisiblePair(HellPokerGame game)
        {
            return game.PlayerHand.Take(game.PlayerCardsRevealed).GroupBy(card => card.Rank).Any(group => group.Count() >= 2);
        }
    }
}
