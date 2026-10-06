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

        /// <summary>Once before each hand.</summary>
        AlreadyShuffled,

        /// <summary>Below <see cref="GameRules.ShuffleMinYears"/>.</summary>
        TooFewYears,

        /// <summary>The soul is on the table — or the shuffle's years would put it there.</summary>
        SoulAtStake
    }
}