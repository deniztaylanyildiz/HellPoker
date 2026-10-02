using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Cheats
{
    /// <summary>Collateral (minor, before the draw): a gold chain on the player's highest card — it cannot be thrown back this hand.</summary>
    public sealed class CollateralCheat : ICheat
    {
        public string Id => CheatIds.Collateral;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => CheatTable.Highest(table.PlayerHand, table.PlayerTargets()) >= 0;

        public CheatResult Apply(CheatTable table)
        {
            int index = CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
            if (index < 0) return CheatResult.Fizzled(Id);
            table.Marks.Chained.Add(table.PlayerHand[index]);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index }, lost: table.PlayerHand[index]);
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

    /// <summary>Buyout (major, before the draw): the player's highest card is traded for the House's lowest.</summary>
    public sealed class BuyoutCheat : ICheat
    {
        public string Id => CheatIds.Buyout;
        public CheatTier Tier => CheatTier.Major;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table)
        {
            int mine = CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
            int theirs = CheatTable.Lowest(table.HouseHand, table.HouseTargets());
            return mine >= 0 && theirs >= 0 && table.PlayerHand[mine].Rank > table.HouseHand[theirs].Rank;
        }

        public CheatResult Apply(CheatTable table)
        {
            if (!CanApply(table)) return CheatResult.Fizzled(Id);
            int mine = CheatTable.Highest(table.PlayerHand, table.PlayerTargets());
            int theirs = CheatTable.Lowest(table.HouseHand, table.HouseTargets());
            var taken = table.PlayerHand[mine];
            var given = table.HouseHand[theirs];
            table.PlayerHand = table.PlayerHand.With(mine, given);
            table.HouseHand = table.HouseHand.With(theirs, taken);
            return new CheatResult(Id, CheatOutcome.Played, new[] { mine }, new[] { theirs }, taken, given);
        }
    }
}
