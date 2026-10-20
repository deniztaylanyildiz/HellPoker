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
        DeadMansHand,

        /// <summary>The table is full: "THE PACT IS SEALED" flares up, and the rest of the hand plays out on its own.</summary>
        PactSealed,

        /// <summary>A demon's cheat helped the player: the text ("BACKFIRE") blinks over the player's cards it touched.</summary>
        Backfire,

        /// <summary>The Warlock warded off a cheat: the text ("WARD") flares over the player's cards.</summary>
        Ward,

        /// <summary>The demon's two jokers turned up at the showdown: a fool's laugh flares up (the text), the demon fumes.</summary>
        JokerLaugh,

        /// <summary>A hand won (Phase 2): the screen's edge blinks gold, twice.</summary>
        WinGlow,

        /// <summary>A hand lost (Phase 2): the screen's edge blinks red, twice.</summary>
        LossGlow,

        /// <summary>Demo 3.4: a demon enraged at half his bar: the text ("ENRAGED") flares up.</summary>
        Rage
    }
}
