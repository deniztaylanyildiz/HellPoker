using System.Linq;

namespace HellPoker.Core.Cheats
{
    /// <summary>Night Veil (minor, before the draw): one of the player's cards goes dark to them until the showdown (it may still be thrown back blind).</summary>
    public sealed class NightVeilCheat : ICheat
    {
        public string Id => CheatIds.NightVeil;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => table.PlayerTargets(i => !table.Marks.HiddenFromPlayer.Contains(table.PlayerHand[i])).Any();

        public CheatResult Apply(CheatTable table)
        {
            int index = table.Pick(table.PlayerTargets(i => !table.Marks.HiddenFromPlayer.Contains(table.PlayerHand[i])));
            if (index < 0) return CheatResult.Fizzled(Id);
            table.Marks.HiddenFromPlayer.Add(table.PlayerHand[index]);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index });
        }
    }

    /// <summary>Thorn (minor, before the draw): a thorn in one card — throwing it back costs a betting unit, at once.</summary>
    public sealed class ThornCheat : ICheat
    {
        public string Id => CheatIds.Thorn;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => table.Unit > 0 && table.PlayerTargets().Any();

        public CheatResult Apply(CheatTable table)
        {
            int index = table.Pick(table.PlayerTargets());
            if (index < 0) return CheatResult.Fizzled(Id);
            table.Marks.Thorned.Add(table.PlayerHand[index]);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index }, years: table.Unit);
        }
    }

    /// <summary>Moonless (major, at the draw): the cards the player draws stay dark to them until the showdown.</summary>
    public sealed class MoonlessCheat : ICheat
    {
        public string Id => CheatIds.Moonless;
        public CheatTier Tier => CheatTier.Major;
        public CheatTiming Timing => CheatTiming.AfterDraw;

        /// <summary>Nothing to darken when the player stands pat.</summary>
        public bool CanApply(CheatTable table) => table.DrawnIndices.Any(i => !CheatTable.IsImmune(table.PlayerHand[i]));

        public CheatResult Apply(CheatTable table)
        {
            int[] dark = table.DrawnIndices.Where(i => !CheatTable.IsImmune(table.PlayerHand[i])).ToArray();
            if (dark.Length == 0) return CheatResult.Fizzled(Id);
            foreach (int i in dark)
                table.Marks.HiddenFromPlayer.Add(table.PlayerHand[i]);
            return new CheatResult(Id, CheatOutcome.Played, dark);
        }
    }
}
