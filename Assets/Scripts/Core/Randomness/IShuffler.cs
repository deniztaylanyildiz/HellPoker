using System.Collections.Generic;

namespace HellPoker.Core.Randomness
{
    public interface IShuffler
    {
        void Shuffle<T>(IList<T> items);
    }
}
