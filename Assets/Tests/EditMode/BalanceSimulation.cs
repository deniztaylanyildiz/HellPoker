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
            // HELLPOKER_SOUL_WORTH / HELLPOKER_SOUL_LOSS try other soul numbers without touching the code;
            // HELLPOKER_LUCIFER_GATE (0: no Lucifer) / HELLPOKER_CAST_DOWN the final table.
            GameRules table = new GameRules(
                soulWorthYears: EnvInt("HELLPOKER_SOUL_WORTH", GameRules.Default.SoulWorthYears),
                soulLossPercent: EnvInt("HELLPOKER_SOUL_LOSS", GameRules.Default.SoulLossPercent),
                luciferGateYears: EnvInt("HELLPOKER_LUCIFER_GATE", GameRules.Default.LuciferGateYears),
                luciferCastDownYears: EnvInt("HELLPOKER_CAST_DOWN", GameRules.Default.LuciferCastDownYears));

            var report = new StringBuilder();
            report.AppendLine($"{Runs} runs per dealer, at most {MaxHandsPerRun} hands each. " +
                              $"Soul worth {table.SoulWorthYears}, soul losses {table.SoulLossPercent}%. " +
                              (table.LuciferGateYears > 0
                                  ? $"Lucifer below {table.LuciferGateYears}, cast down to {table.LuciferCastDownYears}."
                                  : "No Lucifer."));
            report.AppendLine("dealer   absolved  damned  unfinished  avg hands  re-raised hands  dead man's hand wins  soul staked  soul saved" +
                              "  | reached lucifer  beat him 1st try  avg attempts  cast downs  wild bill");

            foreach (Dealer dealer in DealerRoster.All)
            {
                int absolved = 0, damned = 0, hands = 0, deadMan = 0, reRaised = 0, soulStaked = 0, soulSaved = 0;
                int reached = 0, firstTry = 0, attempts = 0, castDowns = 0, wildBill = 0;
                for (int run = 0; run < Runs; run++)
                {
                    int seed = run * 7919 + 13;
                    int sittings = 0;
                    Dealer seat = dealer;
                    HellPokerGame game = HellPokerGameFactory.Create(table, seat, seed);
                    var gate = new LuciferGate(table);
                    int played = 0;
                    bool staked = false;

                    while (!game.IsGameOver && played < MaxHandsPerRun)
                    {
                        // Between hands the gate may move the player: summoned below it, cast down above it at Lucifer's table.
                        GateCall call = gate.Check(game.Years, game.Phase);
                        if (call != GateCall.Stay)
                        {
                            int years = game.Years;
                            if (call == GateCall.Summoned)
                            {
                                gate.Summon(seat.Id);
                                seat = DealerRoster.Lucifer;
                            }
                            else
                            {
                                years = gate.CastDown(years);
                                seat = dealer;
                            }
                            game = HellPokerGameFactory.Create(table, seat, seed + 100003 * ++sittings);
                            game.TakeOver(years, played);
                            if (game.IsGameOver) break;
                        }

                        if (PlayHand(game, new HouseDrawStrategy(game.Rules.MaxDiscards), seat.Payouts))
                            reRaised++;
                        played++;
                        if (game.LastRound.Showdown?.Player.Category == HandCategory.DeadMansHand &&
                            game.LastRound.Showdown.Outcome == ShowdownOutcome.PlayerWins)
                            deadMan++;
                        staked |= game.IsSoulAtStake || game.Phase == GamePhase.Damned;
                        if (!game.IsGameOver) game.NextRound();
                    }

                    hands += played;
                    if (game.Phase == GamePhase.Absolved) absolved++;
                    else if (game.Phase == GamePhase.Damned) damned++;
                    if (staked) soulStaked++;
                    if (staked && game.Phase == GamePhase.Absolved) soulSaved++;
                    if (gate.ReachedLucifer) reached++;
                    attempts += gate.Attempts;
                    castDowns += gate.CastDowns;
                    if (game.Phase == GamePhase.Absolved && gate.IsAtLucifer && gate.Attempts == 1) firstTry++;
                    if (game.Phase == GamePhase.Absolved && !gate.IsAtLucifer) wildBill++;
                }

                string reRaiseShare = (100.0 * reRaised / Math.Max(1, hands)).ToString("0.0") + "%";
                string firstTryShare = (100.0 * firstTry / Math.Max(1, reached)).ToString("0.0") + "%";
                report.AppendLine($"{dealer.Id,-8} {Percent(absolved),7}  {Percent(damned),6}  {Percent(Runs - absolved - damned),10}  " +
                                  $"{hands / (double)Runs,9:0.0}  {reRaiseShare,15}  {deadMan,6}  {Percent(soulStaked),11}  {Percent(soulSaved),10}" +
                                  $"  | {Percent(reached),15}  {firstTryShare,16}  {attempts / (double)Math.Max(1, reached),12:0.00}" +
                                  $"  {castDowns,10}  {wildBill,9}");
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static int EnvInt(string name, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;
        }

        private static string Percent(int count) => (100.0 * count / Runs).ToString("0.0") + "%";

        /// <summary>
        /// A sensible player who raises and folds, using only cards that are face up:
        /// before the draw raises on a visible pair and, with all five cards seen, folds plain high cards that have no draw
        /// (only where folding early is cheap — at a table where it costs the whole stake it plays on);
        /// after the draw raises with two pair or better, folds high card when the House shows a pair;
        /// calls a re-raise with a pair or better.
        /// </summary>
        /// <returns>True if the house re-raised during the hand.</returns>
        private static bool PlayHand(HellPokerGame game, IDrawStrategy drawing, IPayoutInfo payouts)
        {
            bool reRaised = false;
            game.PlaceBet();
            while (!game.IsGameOver && game.Phase != GamePhase.RoundOver)
            {
                switch (game.Phase)
                {
                    case GamePhase.Drawing:
                        game.Draw(drawing.ChooseDiscards(game.PlayerHand));
                        break;

                    case GamePhase.HouseReRaise:
                        reRaised = true;
                        game.Bet(Strength(game) >= HandCategory.OnePair ? BetAction.Call : BetAction.Fold);
                        break;

                    default:
                        game.Bet(Choose(game, drawing, payouts));
                        break;
                }
            }
            return reRaised;
        }

        private static BetAction Choose(HellPokerGame game, IDrawStrategy drawing, IPayoutInfo payouts)
        {
            bool raise, fold;
            if (!game.IsAfterDraw)
            {
                bool allSeen = game.PlayerCardsRevealed == Hand.Size;
                bool hasDraw = drawing.ChooseDiscards(game.PlayerHand).Count == 1;
                bool cheapFold = payouts.FoldPercentBeforeDraw < 100;
                raise = PairAmong(game.PlayerHand.Take(game.PlayerCardsRevealed));
                fold = cheapFold && allSeen && Strength(game) == HandCategory.HighCard && !hasDraw;
            }
            else
            {
                HandCategory mine = Strength(game);
                bool houseShowsPair = PairAmong(game.HouseHand.Take(game.HouseCardsRevealed));
                raise = mine >= HandCategory.TwoPair;
                fold = mine == HandCategory.HighCard && houseShowsPair;
            }

            if (raise && game.CanBet(BetAction.Raise, out _)) return BetAction.Raise;
            if (fold) return BetAction.Fold;
            if (game.CanBet(BetAction.Pass, out _)) return BetAction.Pass;
            return game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Fold;
        }

        private static HandCategory Strength(HellPokerGame game) => Evaluator.Evaluate(game.PlayerHand).Category;

        private static bool PairAmong(System.Collections.Generic.IEnumerable<Card> cards)
        {
            return cards.GroupBy(card => card.Rank).Any(group => group.Count() >= 2);
        }
    }
}
