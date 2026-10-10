namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What the table's numbers count: years of the sentence (every demon's table), or coins (Phase 2's floors).</summary>
    public enum Currency
    {
        Years,

        /// <summary>The floors' coins (never below zero: an empty purse ends the run).</summary>
        Coins,

        /// <summary>A chapter demon's health bar (Phase 2): the counter shows the bar alone, no number.</summary>
        Bar
    }
}
