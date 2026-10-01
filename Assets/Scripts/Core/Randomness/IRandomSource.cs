namespace HellPoker.Core.Randomness
{
    public interface IRandomSource
    {
        /// <summary>Returns a value in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
