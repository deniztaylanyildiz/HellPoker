using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Peasant: no ability to speak of — the easy way down. Starts at 1000 years, and an honest heart: the run's first fold
    /// costs nothing (once per run).
    /// </summary>
    public sealed class Peasant : SinnerClass
    {
        public const string ClassId = "peasant";

        private readonly int _startingYears;
        private readonly int _freeFolds;

        public Peasant(int startingYears = 1000, int freeFolds = 1)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (freeFolds < 0) throw new ArgumentOutOfRangeException(nameof(freeFolds));
            _startingYears = startingYears;
            _freeFolds = freeFolds;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override SinnerAbility Ability => _freeFolds > 0 ? SinnerAbility.FreeFold : SinnerAbility.None;
        public override int ChargesPerRun => _freeFolds;
    }
}
