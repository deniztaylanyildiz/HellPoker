namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The year counter. It is only fed while the soul is safe; with the soul on the table the soul bar takes over.</summary>
    public interface ISentenceView
    {
        /// <summary>The current demon's soul line: the bar fills toward it.</summary>
        void SetSoulLine(int years);

        void SetYears(int years, bool animate);
    }
}
