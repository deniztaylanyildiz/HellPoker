using System;

namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Jester: no power and no charge — his trick is the deck. Every table he sits at (Lucifer's too) deals with jokers: the run
    /// starts with two, every won hand adds one (from the next hand on), and above <see cref="JokerLossLine"/> a lost hand takes one
    /// away again (never below the start). A single joker at the showdown becomes any card its owner does not hold; two or more
    /// lose the hand. The demon draws from the same deck. At <see cref="JokerJackpot"/> (20) jokers the deck is cleared of them, the
    /// count starts again at two, and the run earns the Jester's Rattle (once a run).
    /// </summary>
    public sealed class Jester : SinnerClass
    {
        public const string ClassId = "jester";

        private readonly int _startingYears;
        private readonly int _startingJokers;
        private readonly int _lossLine;
        private readonly int _jackpot;

        /// <param name="jackpot">At this many jokers the deck is cleared and the run earns the Jester's Rattle (once).</param>
        /// <param name="startingYears">750: a lighter sentence for the wildest deck (the balance simulation's suggestion, the designer's choice).</param>
        public Jester(int startingYears = 750, int startingJokers = 2, int lossLine = 10, int jackpot = 20)
        {
            if (jackpot != 0 && jackpot <= startingJokers) throw new ArgumentOutOfRangeException(nameof(jackpot));
            _jackpot = jackpot;
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (startingJokers <= 0) throw new ArgumentOutOfRangeException(nameof(startingJokers));
            if (lossLine < startingJokers) throw new ArgumentOutOfRangeException(nameof(lossLine));
            _startingYears = startingYears;
            _startingJokers = startingJokers;
            _lossLine = lossLine;
        }

        public override string Id => ClassId;
        public override int StartingYears => _startingYears;
        public override int StartingJokers => _startingJokers;
        public override int JokerLossLine => _lossLine;
        public override int JokerJackpot => _jackpot;
    }
}