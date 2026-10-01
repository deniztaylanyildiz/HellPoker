namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The demon at the table: portrait, name and what they say.</summary>
    public interface IDealerView
    {
        void SetDealer(DealerCard dealer);

        /// <summary>The dealer speaks a line; the tone tells the view how it feels (gloating, annoyed...).</summary>
        void Say(string line, Tone tone);
    }
}
