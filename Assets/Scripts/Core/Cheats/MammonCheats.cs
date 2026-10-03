using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// Collateral (minor, before the draw): a gold chain on a card the player would want to throw back — the highest of the
    /// cards a sensible draw would toss — so it must stay this hand. With nothing worth tossing (a made hand), the lowest card.
    /// </summary>
    public sealed class CollateralCheat : ICheat
    {
        public string Id => CheatIds.Collateral;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => Target(table) >= 0;

        public CheatResult Apply(CheatTable table)
        {
            int index = Target(table);
            if (index < 0) return CheatResult.Fizzled(Id);
            table.Marks.Chained.Add(table.PlayerHand[index]);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index }, lost: table.PlayerHand[index]);
        }

        private static int Target(CheatTable table)
        {
            IReadOnlyCollection<int> toss = table.AdvisedDiscards();
            int index = CheatTable.Highest(table.PlayerHand, table.PlayerTargets(toss.Contains));
            return index >= 0 ? index : CheatTable.Lowest(table.PlayerHand, table.PlayerTargets());
        }
    }

    /// <summary>Tithe (minor, at the showdown): a win this hand forgives one betting unit less (never below nothing).</summary>
    public sealed class TitheCheat : ICheat
    {
        public string Id => CheatIds.Tithe;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeShowdown;

        /// <summary>Only a won hand has anything to tithe — and never the Dead Man's Hand.</summary>
        public bool CanApply(CheatTable table) =>
            table.Showdown != null && table.Showdown.Outcome == ShowdownOutcome.PlayerWins &&
            table.Showdown.Player.Category != HandCategory.DeadMansHand && table.Unit > 0;

        public CheatResult Apply(CheatTable table)
        {
            if (!CanApply(table)) return CheatResult.Fizzled(Id);
            table.Marks.Tithe = true;
            return new CheatResult(Id, CheatOutcome.Played, years: table.Unit);
        }
    }

    /// <summary>
    /// Buyout (major, before the draw): the player's highest card is traded for one of the House's lowest — never a card that
    /// would leave the player better off (a low card that pairs up with theirs is passed over).
    /// </summary>
    public sealed class BuyoutCheat : ICheat
    {
        public string Id => CheatIds.Buyout;
        public CheatTier Tier => CheatTier.Major;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => Trade(table).theirs >= 0;

        public CheatResult Apply(CheatTable table)
        {
            var (mine, theirs) = Trade(table);
            if (theirs < 0) return CheatResult.Fizzled(Id);
            var taken = table.PlayerHand[mine];
            var given = table.HouseHand[theirs];
            table.PlayerHand = table.PlayerHand.With(mine, given);
            table.HouseHand = table.HouseHand.With(theirs, taken);
            return new CheatResult(Id, CheatOutcome.Played, new[] { mine }, new[] { theirs }, taken, given);
        }

        /// <summary>The player's highest card and the House's lowest lower card that does not help the player; -1 for none.</summary>
        private static (int mine, int theirs) Trade(CheatTable table)
        {
            int mine = CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
            if (mine < 0) return (-1, -1);
            Card taken = table.PlayerHand[mine];
            int theirs = table.HouseTargets(i => table.HouseHand[i].Rank < taken.Rank && !table.Improves(table.PlayerHand.With(mine, table.HouseHand[i])))
                .OrderBy(i => table.HouseHand[i].Rank).DefaultIfEmpty(-1).First();
            return (mine, theirs);
        }
    }
}
