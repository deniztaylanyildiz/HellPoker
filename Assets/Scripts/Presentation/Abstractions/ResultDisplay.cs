namespace HellPoker.Presentation.Abstractions
{
    /// <summary>How a hand ended, as the table shows it: won, lost, a push, a fold.</summary>
    public enum HandOutcome
    {
        Win,
        Loss,
        Push,
        Fold
    }

    /// <summary>
    /// A Phase 2 table's result band (2.1.1): one big word across the middle ("YOU WIN" gold, "YOU LOSE" ember red, "PUSH" / "FOLDED"
    /// grey) and the amount under it ("+12 coins"; at a demon's table his bar's and the sentence's change). It stays until the next hand.
    /// </summary>
    public sealed class ResultBand
    {
        public HandOutcome Outcome { get; }
        public string Title { get; }
        public string Amount { get; }

        public ResultBand(HandOutcome outcome, string title, string amount)
        {
            Outcome = outcome;
            Title = title ?? "";
            Amount = amount ?? "";
        }
    }

    /// <summary>One of the last hands by the counter (2.1.1): its colour and what the hover tells (the hand's name and amount).</summary>
    public sealed class HandPip
    {
        public HandOutcome Outcome { get; }
        public string Hint { get; }

        public HandPip(HandOutcome outcome, string hint)
        {
            Outcome = outcome;
            Hint = hint ?? "";
        }
    }
}
