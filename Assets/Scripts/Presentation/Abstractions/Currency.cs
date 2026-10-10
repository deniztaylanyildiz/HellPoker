namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What the table's numbers count: years of the sentence (every demon's table), or coins (Phase 2's floors).</summary>
    public enum Currency
    {
        Years,

        /// <summary>The floors' coins: the counter may go below zero (a debt, shown red).</summary>
        Coins
    }
}
