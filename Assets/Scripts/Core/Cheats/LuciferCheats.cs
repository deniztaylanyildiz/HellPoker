using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// Gaze (minor, from the deal): this hand the House answers raises knowing the player's cards — it always re-raises a hand
    /// that would lose, and still re-raises a winning one half the time (a bluff), so its raise is a threat, never a certainty.
    /// </summary>
    public sealed class GazeCheat : ICheat
    {
        public string Id => CheatIds.Gaze;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.AfterDeal;

        public bool CanApply(CheatTable table) => true;

        public CheatResult Apply(CheatTable table)
        {
            table.Marks.Gaze = true;
            return new CheatResult(Id, CheatOutcome.Played);
        }
    }

    /// <summary>
    /// Rewrite (minor, after the draw): a card of the player's hand turns into one that drops the hand to the next category
    /// down where it can (a full house to three of a kind, two pair to one pair...). The new card comes out of the deck.
    /// </summary>
    public sealed class RewriteCheat : ICheat
    {
        public string Id => CheatIds.Rewrite;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.AfterDraw;

        public bool CanApply(CheatTable table) => Options(table).Count > 0;

        public CheatResult Apply(CheatTable table)
        {
            List<(int index, Card into)> options = Options(table);
            if (options.Count == 0) return CheatResult.Fizzled(Id);

            var (i, into) = options[table.Random.Next(options.Count)];
            Card lost = table.PlayerHand[i];
            table.TurnPlayerCardInto(i, into);
            return new CheatResult(Id, CheatOutcome.Played, new[] { i }, lost: lost, gained: into);
        }

        /// <summary>The changes that drop the hand the least far — one category down when possible.</summary>
        private static List<(int, Card)> Options(CheatTable table)
        {
            HandCategory now = table.Evaluator.Evaluate(table.PlayerHand).Category;
            var best = new List<(int, Card)>();
            HandCategory? bestCategory = null;
            foreach (int i in table.PlayerTargets())
            {
                foreach (Card card in table.Deck.Remaining)
                {
                    if (CheatRules.IsImmune(card)) continue;
                    HandCategory after = table.Evaluator.Evaluate(table.PlayerHand.With(i, card)).Category;
                    if (after >= now) continue;
                    if (bestCategory == null || after > bestCategory)
                    {
                        bestCategory = after;
                        best.Clear();
                    }
                    if (after == bestCategory) best.Add((i, card));
                }
            }
            return best;
        }
    }

    /// <summary>
    /// Burning Card (minor, before the draw): the highest card of the player's best combination (the pair, the trips...; any
    /// card of a straight or flush) catches fire — the highest card when there is no combination — and becomes the next card
    /// of the deck. That card is pure chance: the fire may backfire.
    /// </summary>
    public sealed class BurningCardCheat : ICheat
    {
        public string Id => CheatIds.BurningCard;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => table.Deck.Count > 0 && Target(table) >= 0;

        public CheatResult Apply(CheatTable table)
        {
            int index = Target(table);
            if (index < 0) return CheatResult.Fizzled(Id);
            Card lost = table.PlayerHand[index];
            if (!table.RedealPlayerCard(index, out Card ash)) return CheatResult.Fizzled(Id);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index }, lost: lost, gained: ash);
        }

        private static int Target(CheatTable table)
        {
            int index = CheatTable.Highest(table.PlayerHand, table.CombinationCards().Where(i => !CheatTable.IsImmune(table.PlayerHand[i])));
            return index >= 0 ? index : CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
        }
    }

    /// <summary>
    /// The Fall (major, at the showdown; once per attempt, only at 150 years or less): when the player would win, the highest
    /// card of each hand is dealt again from the deck and the hands are judged anew. Announced at the start of the hand
    /// ("THE FALL AWAITS"), so the player may fold.
    /// </summary>
    public sealed class TheFallCheat : ICheat
    {
        public string Id => CheatIds.TheFall;
        public CheatTier Tier => CheatTier.Major;
        public CheatTiming Timing => CheatTiming.BeforeShowdown;

        public bool CanApply(CheatTable table) =>
            table.Showdown != null && table.Showdown.Outcome == ShowdownOutcome.PlayerWins && table.Deck.Count >= 2 &&
            table.PlayerTargets().Any();

        public CheatResult Apply(CheatTable table)
        {
            if (!CanApply(table)) return CheatResult.Fizzled(Id);

            int mine = CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
            int theirs = CheatTable.Highest(table.HouseHand, table.HouseTargets());
            Card lost = table.PlayerHand[mine];
            table.RedealPlayerCard(mine, out Card gained);
            if (theirs >= 0)
                table.RedealHouseCard(theirs, out _);

            table.Showdown = ShowdownResult.Resolve(table.Evaluator.Evaluate(table.PlayerHand), table.Evaluator.Evaluate(table.HouseHand));
            return new CheatResult(Id, CheatOutcome.Played, new[] { mine }, theirs >= 0 ? new[] { theirs } : null, lost, gained);
        }
    }
}
