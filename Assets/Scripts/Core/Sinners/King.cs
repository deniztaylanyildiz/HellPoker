using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The King: a heavier sin (starts at 1250), but a crown that pays — a won hand forgives a share of the ante more
    /// (<c>crownPercent</c>, 25: a quarter ante; tuned with the balance simulation, see CLAUDE.md) — and once at every table a card
    /// put under the crown's protection before the draw: no cheat may touch it that hand (as the Dead Man's Hand cards are beyond them).
    /// </summary>
    public sealed class King : SinnerClass
    {
        public const string ClassId = "king";

        private readonly int _startingYears;
        private readonly int _crownPercent;
        private readonly int _protects;
        private readonly bool _protectsPerRun;

        /// <param name="crownPercent">A won hand forgives this share of the ante more (100: the ante multiplier +1).</param>
        /// <param name="protects">Charges of the protection, at each table (or for the whole run with <paramref name="perRun"/>).</param>
        public King(int startingYears = 1250, int crownPercent = 25, int protects = 1, bool perRun = false)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (crownPercent < 0) throw new ArgumentOutOfRangeException(nameof(crownPercent));
            if (protects < 0) throw new ArgumentOutOfRangeException(nameof(protects));
            _startingYears = startingYears;
            _crownPercent = crownPercent;
            _protects = protects;
            _protectsPerRun = perRun;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override int WinAntePercent => _crownPercent;
        public override SinnerAbility Ability => SinnerAbility.Protect;
        public override int ChargesPerTable => _protectsPerRun ? 0 : _protects;
        public override int ChargesPerRun => _protectsPerRun ? _protects : 0;
    }
}
