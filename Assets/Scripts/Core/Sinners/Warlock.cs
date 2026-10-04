using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Warlock: sees through a demon's lie the moment it is told — and through a hidden hand: where the House shows fewer
    /// than two cards (Belial), he sees two — and once at every table wards off a minor cheat about to
    /// strike (the guard refuses it: it is spent, the gauge empties, nothing happens). Major cheats — The Fall among them — get
    /// through. A cheat that would come to nothing anyway is not worth a ward.
    /// </summary>
    public sealed class Warlock : SinnerClass
    {
        public const string ClassId = "warlock";

        private readonly int _startingYears;
        private readonly int _wardsPerTable;

        public Warlock(int startingYears = 1000, int wardsPerTable = 1, int seenHouseCards = 2)
        {
            if (seenHouseCards < 0) throw new ArgumentOutOfRangeException(nameof(seenHouseCards));
            SeenHouseCards = seenHouseCards;
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (wardsPerTable < 0) throw new ArgumentOutOfRangeException(nameof(wardsPerTable));
            _startingYears = startingYears;
            _wardsPerTable = wardsPerTable;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override SinnerAbility Ability => SinnerAbility.Ward;
        public override int ChargesPerTable => _wardsPerTable;
        public override bool SeesLies => true;

        /// <summary>
        /// The Warlock sees through a demon who hides his hand: at an ordinary table showing fewer than
        /// <see cref="SeenHouseCards"/> House cards (Belial shows one) he sees that many. The final table keeps its darkness.
        /// </summary>
        public override int HouseCardsShownAt(Game.GameRules rules) =>
            rules.IsFinalTable ? rules.HouseCardsShown : Math.Max(rules.HouseCardsShown, Math.Min(SeenHouseCards, Cards.Hand.Size - 1));

        /// <summary>How many House cards the Warlock sees before his last decision at a table that hides them.</summary>
        public int SeenHouseCards { get; }

        public override bool Allows(ICheat cheat, CheatTable table, Sinner sinner)
        {
            if (cheat == null || cheat.Tier != CheatTier.Minor) return true;
            if (!cheat.CanApply(table)) return true;   // it will come to nothing: keep the ward
            return !sinner.TrySpend(SinnerAbility.Ward);
        }
    }
}
