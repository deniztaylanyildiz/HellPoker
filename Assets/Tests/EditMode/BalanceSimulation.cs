using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Not a real test: plays thousands of runs against every dealer with a simple, sensible player and reports
    /// how often runs end absolved or damned, and how the demons cheat. Explicit, so normal runs skip it. Run with:
    ///   -testPlatform EditMode -testFilter HellPoker.Core.Tests.BalanceSimulation
    /// The report is in the test's output (results.xml).
    /// </summary>
    [Explicit, Category("Simulation")]
    public class BalanceSimulation
    {
        private const int Runs = 2000;
        private const int MaxHandsPerRun = 400;

        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        /// <summary>What happened at one demon's table over every run: hands, and each cheat's outcomes.</summary>
        private sealed class CheatTally
        {
            public int Hands;
            public int Played;
            public int Fizzled;
            public int Lies;
            public int Backfires;
            public readonly Dictionary<string, int> ById = new Dictionary<string, int>();
            public readonly Dictionary<string, int> BackfiresById = new Dictionary<string, int>();

            public void Count(HellPokerGame game)
            {
                Hands++;
                foreach (CheatResult result in game.CheatsThisHand)
                {
                    if (result.Outcome == CheatOutcome.Fizzled)
                    {
                        Fizzled++;
                        continue;
                    }
                    Played++;
                    if (result.WasLie) Lies++;
                    ById[result.CheatId] = ById.TryGetValue(result.CheatId, out int n) ? n + 1 : 1;
                    if (result.Backfired)
                    {
                        Backfires++;
                        BackfiresById[result.CheatId] = BackfiresById.TryGetValue(result.CheatId, out int b) ? b + 1 : 1;
                    }
                }
            }
        }

        [Test]
        public void EveryDealer()
        {
            // HELLPOKER_SOUL_WORTH / HELLPOKER_SOUL_LOSS try other soul numbers without touching the code;
            // HELLPOKER_LUCIFER_GATE (0: no Lucifer) / HELLPOKER_CAST_DOWN the final table;
            // HELLPOKER_MALICE ("mammon,belial,lilith,lucifer" gauge sizes), HELLPOKER_MALICE_WIN, HELLPOKER_MALICE_LOW (the ≤ 500 bonus)
            // the cheats' pace; HELLPOKER_CHEATS=0 plays without cheats.
            GameRules table = new GameRules(
                soulWorthYears: EnvInt("HELLPOKER_SOUL_WORTH", GameRules.Default.SoulWorthYears),
                soulLossPercent: EnvInt("HELLPOKER_SOUL_LOSS", GameRules.Default.SoulLossPercent),
                luciferGateYears: EnvInt("HELLPOKER_LUCIFER_GATE", GameRules.Default.LuciferGateYears),
                luciferCastDownYears: EnvInt("HELLPOKER_CAST_DOWN", GameRules.Default.LuciferCastDownYears),
                malicePerWin: EnvInt("HELLPOKER_MALICE_WIN", GameRules.Default.MalicePerWin),
                maliceLowSentenceBonus: EnvInt("HELLPOKER_MALICE_LOW", GameRules.Default.MaliceLowSentenceBonus));
            bool cheats = EnvInt("HELLPOKER_CHEATS", 1) != 0;
            int[] malice = (Environment.GetEnvironmentVariable("HELLPOKER_MALICE") ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            Dealer Tune(Dealer dealer, int slot)
            {
                if (!cheats) return dealer.WithoutCheats();
                return slot < malice.Length ? dealer.WithMaliceMax(malice[slot]) : dealer;
            }
            Dealer lucifer = Tune(DealerRoster.Lucifer, 3);

            var report = new StringBuilder();
            report.AppendLine($"{Runs} runs per dealer, at most {MaxHandsPerRun} hands each. " +
                              $"Soul worth {table.SoulWorthYears}, soul losses {table.SoulLossPercent}%. " +
                              (table.LuciferGateYears > 0
                                  ? $"Lucifer below {table.LuciferGateYears}, cast down to {table.LuciferCastDownYears}."
                                  : "No Lucifer."));
            report.AppendLine(cheats
                ? $"Cheats on. Malice: {string.Join(" / ", DealerRoster.All.Select((d, i) => $"{d.Id} {Tune(d, i).MaliceMax}"))} / lucifer {lucifer.MaliceMax}; " +
                  $"+{table.MalicePerHand} a hand, +{table.MalicePerWin} a win, +{table.MaliceLowSentenceBonus} at or below {table.MaliceLowSentenceYears}."
                : "Cheats off.");
            report.AppendLine("dealer   absolved  damned  unfinished  avg hands  re-raised hands  dead man's hand wins  soul staked  soul saved" +
                              "  | reached lucifer  beat him 1st try  avg attempts  cast downs  wild bill");

            var tallies = new Dictionary<string, CheatTally>();
            CheatTally TallyFor(string id) => tallies.TryGetValue(id, out CheatTally t) ? t : tallies[id] = new CheatTally();
            var luciferTally = new CheatTally();

            for (int slot = 0; slot < DealerRoster.All.Count; slot++)
            {
                Dealer dealer = Tune(DealerRoster.All[slot], slot);
                CheatTally tally = TallyFor(dealer.Id);
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
                                seat = lucifer;
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
                        (seat.IsFinalTable ? luciferTally : tally).Count(game);
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

            if (cheats)
            {
                report.AppendLine();
                report.AppendLine("table    hands   cheats/hand  fizzled  lies  backfires | cheat types (share of played cheats; backfire rate of that cheat)");
                foreach (Dealer dealer in DealerRoster.All)
                    AppendTally(report, dealer.Id, TallyFor(dealer.Id));
                AppendTally(report, lucifer.Id, luciferTally);
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static void AppendTally(StringBuilder report, string id, CheatTally tally)
        {
            string perHand = (tally.Played / (double)Math.Max(1, tally.Hands)).ToString("0.00");
            string types = string.Join("  ", tally.ById.OrderByDescending(p => p.Value)
                .Select(p => $"{p.Key} {100.0 * p.Value / Math.Max(1, tally.Played):0}%" +
                             (tally.BackfiresById.TryGetValue(p.Key, out int b) ? $" (bf {100.0 * b / p.Value:0.0}%)" : "")));
            string backfires = $"{tally.Backfires} ({100.0 * tally.Backfires / Math.Max(1, tally.Played):0.0}%)";
            report.AppendLine($"{id,-8} {tally.Hands,6}  {perHand,11}  {tally.Fizzled,7}  {tally.Lies,4}  {backfires,9} | {types}");
        }

        private static int EnvInt(string name, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;
        }

        private static string Percent(int count) => (100.0 * count / Runs).ToString("0.0") + "%";

        /// <summary>
        /// A sensible player who raises and folds, using only what is on the table for them to see (never a veiled card, never
        /// the real face behind Belial's false one), and who reads the demon's intent:
        /// before the draw raises on a visible pair and, with all five cards seen, folds plain high cards that have no draw
        /// (only where folding early is cheap — at a table where it costs the whole stake it plays on);
        /// after the draw raises with two pair or better, folds high card when the House shows a pair;
        /// calls a re-raise with a pair or better.
        /// Against the intent: never throws a thorned or chained card away; when The Fall awaits it does not raise (a big win
        /// is what The Fall takes) and folds a weak hand after the draw. Under Lucifer's Gaze a re-raise is no certainty (he
        /// bluffs half the time), so it is answered like any other: called with a pair or better.
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
                        game.Draw(Discards(game, drawing));
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

        /// <summary>
        /// The House's own logic on a hand the player can read; with veiled cards it keeps the visible pairs and throws the
        /// rest, veiled cards first. Thorned and chained cards stay.
        /// </summary>
        private static int[] Discards(HellPokerGame game, IDrawStrategy drawing)
        {
            var hidden = Enumerable.Range(0, Hand.Size).Where(game.IsPlayerCardHidden).ToList();
            IEnumerable<int> discards;
            if (hidden.Count == 0)
            {
                discards = drawing.ChooseDiscards(game.PlayerHand);
            }
            else
            {
                var visible = Enumerable.Range(0, Hand.Size).Except(hidden).ToList();
                var paired = new HashSet<Rank>(visible.GroupBy(i => game.PlayerHand[i].Rank).Where(g => g.Count() >= 2).Select(g => g.Key));
                discards = hidden.Concat(visible.Where(i => !paired.Contains(game.PlayerHand[i].Rank))
                    .OrderBy(i => game.PlayerHand[i].Rank));
            }
            return discards.Where(i => !game.IsPlayerCardChained(i) && !game.IsPlayerCardThorned(i))
                .Take(game.Rules.MaxDiscards).ToArray();
        }

        private static BetAction Choose(HellPokerGame game, IDrawStrategy drawing, IPayoutInfo payouts)
        {
            bool raise, fold;
            bool fallAwaits = game.PendingCheat?.Id == CheatIds.TheFall;
            if (!game.IsAfterDraw)
            {
                bool allSeen = game.PlayerCardsRevealed == Hand.Size && Enumerable.Range(0, Hand.Size).All(i => !game.IsPlayerCardHidden(i));
                bool hasDraw = allSeen && drawing.ChooseDiscards(game.PlayerHand).Count == 1;
                bool cheapFold = payouts.FoldPercentBeforeDraw < 100;
                raise = !fallAwaits && Strength(game) >= HandCategory.OnePair;
                fold = cheapFold && allSeen && Strength(game) == HandCategory.HighCard && !hasDraw;
            }
            else
            {
                HandCategory mine = Strength(game);
                raise = !fallAwaits && mine >= HandCategory.TwoPair;
                fold = mine == HandCategory.HighCard && (fallAwaits || HouseShowsPair(game));
            }

            if (raise && game.CanBet(BetAction.Raise, out _)) return BetAction.Raise;
            if (fold) return BetAction.Fold;
            if (game.CanBet(BetAction.Pass, out _)) return BetAction.Pass;
            return game.CanBet(BetAction.Raise, out _) ? BetAction.Raise : BetAction.Fold;
        }

        /// <summary>What the player can read of their hand: visible cards only.</summary>
        private static HandCategory Strength(HellPokerGame game) => game.PlayerHandNow ?? HandCategory.HighCard;

        /// <summary>A pair among the House's open cards, not counting a face marked false.</summary>
        private static bool HouseShowsPair(HellPokerGame game)
        {
            return Enumerable.Range(0, game.HouseCardsRevealed).Where(i => !game.IsHouseCardFalse(i))
                .GroupBy(i => game.HouseCardFace(i).Rank).Any(group => group.Count() >= 2);
        }
    }
}
