namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The year counter. It is only fed while the soul is safe; with the soul on the table the soul bar takes over.</summary>
    public interface ISentenceView
    {
        /// <summary>The current demon's soul line: the bar fills toward it.</summary>
        void SetSoulLine(int years);

        /// <summary>Any other line the bar fills toward, with its own words under it (Lucifer's: cast down above 250).</summary>
        void SetLimit(int years, string text);

        /// <summary>The words under the counter ("YEARS LEFT IN HELL", or at Lucifer's table the attempt).</summary>
        void SetLabel(string text);

        void SetYears(int years, bool animate);
    }
}
