namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Which table the screen lays out: the Boss Simulator's (the old demo's) or a chapter's.</summary>
    public enum TableLayoutKind
    {
        /// <summary>The demon on the left, the counter and the payouts on the right (the demo, as it always was).</summary>
        Simulator,

        /// <summary>Demo 3.2: the demon top right, the counter top left, the player's panel bottom left.</summary>
        Chapter
    }
}
