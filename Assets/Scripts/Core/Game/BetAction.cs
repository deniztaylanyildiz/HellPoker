namespace HellPoker.Core.Game
{
    /// <summary>Choice offered after a card is turned.</summary>
    public enum BetAction
    {
        /// <summary>Add the ante to the stake again.</summary>
        Raise,

        /// <summary>Continue without raising. Forbidden near the end of the sentence.</summary>
        Pass,

        /// <summary>Give up the hand and pay a penalty.</summary>
        Fold
    }
}
