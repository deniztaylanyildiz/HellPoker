namespace HellPoker.Core.Game
{
    /// <summary>Why the player may not SHUFFLE now (<see cref="IHellPokerGame.WhyNoShuffle"/>).</summary>
    public enum ShuffleRefusal
    {
        None,

        /// <summary>No deck to count: a fresh shuffle every hand (the Jester's jokers, or the rule switched off).</summary>
        NoDeckToCount,

        /// <summary>Only between hands, before the deal.</summary>
        NotBetweenHands,

        /// <summary>The soul is on the table.</summary>
        SoulOnTable,

        /// <summary>Once before each hand.</summary>
        AlreadyShuffled,

        /// <summary>All 52 are in the deck: nothing to shuffle back.</summary>
        DeckFull,

        /// <summary>The deck is too thin for the next hand: the demon shuffles it anyway, for nothing.</summary>
        ShuffleComing,

        /// <summary>Below <see cref="GameRules.ShuffleMinYears"/>.</summary>
        TooFewYears,

        /// <summary>The shuffle's years would reach the soul line: the soul would go on the table.</summary>
        WouldStakeSoul
    }
}