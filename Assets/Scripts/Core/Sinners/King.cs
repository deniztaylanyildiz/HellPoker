using System;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The King: a heavier sin (starts at 1250). Passive: a crown that pays — a won hand forgives a share of the ante more
    /// (<c>crownPercent</c>, 25: a quarter ante; tuned with the balance simulation, see CLAUDE.md). Power (a full charge): before
    /// the draw a card goes under the crown's protection — no cheat may touch it that hand (as the Dead Man's Hand cards are beyond them).
    /// </summary>
    public sealed class King : SinnerClass
    {
        public const string ClassId = "king";

        private readonly int _startingYears;
        private readonly int _crownPercent;

        /// <param name="crownPercent">A won hand forgives this share of the ante more (100: the ante multiplier +1).</param>
        public King(int startingYears = 1250, int crownPercent = 25)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (crownPercent < 0) throw new ArgumentOutOfRangeException(nameof(crownPercent));
            _startingYears = startingYears;
            _crownPercent = crownPercent;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override int WinAntePercent => _crownPercent;
        public override SinnerAbility Ability => SinnerAbility.Protect;
    }
}
