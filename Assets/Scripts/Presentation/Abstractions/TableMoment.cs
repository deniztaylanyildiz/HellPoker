namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A moment the table should make the player feel. The view picks the effect; all of them can be skipped.</summary>
    public enum TableMoment
    {
        /// <summary>A heavy loss: the screen shakes and the sentence counter flashes red.</summary>
        BigLoss,

        /// <summary>A winning hand of Two Pair or better: its name flares up big for a moment.</summary>
        GoodHand,

        /// <summary>The Dead Man's Hand wins: the screen goes dark and its four cards light up one by one.</summary>
        DeadMansHand
    }
}
