using System;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Warlock. Passive: sees through a demon's lie the moment it is told — and through a hidden hand: where the House shows
    /// fewer than two cards (Belial), he sees two. Power (a full charge): a ward against the minor cheat announced — it is refused
    /// when it comes (spent, the gauge empties, nothing happens). Major cheats — The Fall among them — cannot be warded.
    /// </summary>
    public sealed class Warlock : SinnerClass
    {
        public const string ClassId = "warlock";

        private readonly int _startingYears;

        public Warlock(int startingYears = 1000, int seenHouseCards = 2)
        {
            if (seenHouseCards < 0) throw new ArgumentOutOfRangeException(nameof(seenHouseCards));
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            SeenHouseCards = seenHouseCards;
            _startingYears = startingYears;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override SinnerAbility Ability => SinnerAbility.Ward;
        public override bool SeesLies => true;

        /// <summary>
        /// The Warlock sees through a demon who hides his hand: at an ordinary table showing fewer than
        /// <see cref="SeenHouseCards"/> House cards (Belial shows one) he sees that many. The final table keeps its darkness.
        /// </summary>
        public override int HouseCardsShownAt(Game.GameRules rules) =>
            rules.IsFinalTable ? rules.HouseCardsShown : Math.Max(rules.HouseCardsShown, Math.Min(SeenHouseCards, Cards.Hand.Size - 1));

        /// <summary>How many House cards the Warlock sees before his last decision at a table that hides them.</summary>
        public int SeenHouseCards { get; }
    }
}
