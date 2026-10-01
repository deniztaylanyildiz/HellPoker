namespace HellPoker.Core.Game
{
    /// <summary>Choice offered at a bet decision.</summary>
    public enum BetAction
    {
        /// <summary>Add betting units to the stake (one before the draw, two after), up to the table cap.</summary>
        Raise,

        /// <summary>Continue without raising. Forbidden near the end of the sentence until the cap or all in.</summary>
        Pass,

        /// <summary>Give up the hand and pay a penalty.</summary>
        Fold,

        /// <summary>Match the house's re-raise and continue. Only when the house has re-raised.</summary>
        Call
    }
}
