using System;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Peasant: no tricks to speak of — the easy way down. Starts at 1000 years. Power (a full charge): an honest heart —
    /// walk away from this hand for nothing (the fold costs no years), wherever folding is possible.
    /// </summary>
    public sealed class Peasant : SinnerClass
    {
        public const string ClassId = "peasant";

        private readonly int _startingYears;

        public Peasant(int startingYears = 1000)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            _startingYears = startingYears;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override SinnerAbility Ability => SinnerAbility.FreeFold;
    }
}
