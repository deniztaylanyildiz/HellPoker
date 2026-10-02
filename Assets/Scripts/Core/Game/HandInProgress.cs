using System;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A hand that was still being played, as saved: enough to settle it if the game is closed before it ends.
    /// The cards are not kept — a hand left behind is never played on, only forfeited (see <see cref="IHellPokerGame.ForfeitHand"/>).
    /// </summary>
    public sealed class HandInProgress
    {
        /// <summary>Everything on the table: ante, raises and called re-raises (an uncalled re-raise is not).</summary>
        public int Stake { get; }

        public int Ante { get; }

        /// <summary>True once the cards were exchanged: folding then costs more.</summary>
        public bool IsAfterDraw { get; }

        /// <summary>True for a hand dealt with the soul on the table: losses burn the soul faster.</summary>
        public bool IsSoulHand { get; }

        /// <summary>True when the pact was sealed (the table full or all in): there was no folding any more.</summary>
        public bool IsSealed { get; }

        public HandInProgress(int stake, int ante, bool isAfterDraw, bool isSoulHand, bool isSealed)
        {
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            if (ante <= 0 || ante > stake) throw new ArgumentOutOfRangeException(nameof(ante), "The ante is part of the stake.");
            Stake = stake;
            Ante = ante;
            IsAfterDraw = isAfterDraw;
            IsSoulHand = isSoulHand;
            IsSealed = isSealed;
        }
    }
}
