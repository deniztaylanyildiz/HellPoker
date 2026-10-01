namespace HellPoker.Presentation.Abstractions
{
    public interface ISentenceView
    {
        void SetDamnationLimit(int years);
        void SetYears(int years, bool animate);
    }
}
