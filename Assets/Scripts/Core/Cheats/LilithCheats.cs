using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// Night Veil (minor, at the deal): one of the player's cards that has not turned yet comes to them in the dark — it turns
    /// face down under its veil, is never seen until the showdown, and may only be thrown back blind.
    /// </summary>
    public sealed class NightVeilCheat : ICheat
    {
        public string Id => CheatIds.NightVeil;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.AfterDeal;

        public bool CanApply(CheatTable table) => Targets(table).Any();

        public CheatResult Apply(CheatTable table)
        {
            int index = table.Pick(Targets(table));
            if (index < 0) return CheatResult.Fizzled(Id);
            table.Marks.HiddenFromPlayer.Add(table.PlayerHand[index]);
            return new CheatResult(Id, CheatOutcome.Played, new[] { index });
        }

        /// <summary>Only cards the player has not seen yet: a card already looked at cannot be taken back into the dark.</summary>
        private static IEnumerable<int> Targets(CheatTable table) =>
            table.PlayerTargets(i => i >= table.PlayerCardsSeen && !table.Marks.HiddenFromPlayer.Contains(table.PlayerHand[i]));
    }

    /// <summary>
    /// Thorn (minor, before the draw): a thorn in a card the player would want to throw back (one the House's logic would
    /// toss; any card for a made hand) — throwing it back costs a betting unit, at once.
    /// </summary>
    public sealed class ThornCheat : ICheat
    {
        public string Id => CheatIds.Thorn;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.BeforeDraw;

        public bool CanApply(CheatTable table) => table.Unit > 0 && table.PlayerTargets().Any();

        public CheatResult Apply(CheatTable table)
        {
            IReadOnlyCollection<int> toss = table.AdvisedDiscards();
            int[] tempting = table.PlayerTargets(toss.Contains).ToArray();
            int index = table.Pick(tempting.Length > 0 ? tempting : table.PlayerTargets());
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
        public bool CanApply(CheatTable table) => table.DrawnIndices.Any(i => !table.IsUntouchable(i));

        public CheatResult Apply(CheatTable table)
        {
            int[] dark = table.DrawnIndices.Where(i => !table.IsUntouchable(i)).ToArray();
            if (dark.Length == 0) return CheatResult.Fizzled(Id);
            foreach (int i in dark)
                table.Marks.HiddenFromPlayer.Add(table.PlayerHand[i]);
            return new CheatResult(Id, CheatOutcome.Played, dark);
        }
    }
}
