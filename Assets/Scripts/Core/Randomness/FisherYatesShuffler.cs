using System;
using System.Collections.Generic;

namespace HellPoker.Core.Randomness
{
    public sealed class FisherYatesShuffler : IShuffler
    {
        private readonly IRandomSource _random;

        public FisherYatesShuffler(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public void Shuffle<T>(IList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}
