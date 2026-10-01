using System;

namespace HellPoker.Core.Randomness
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource()
        {
            _random = new Random();
        }

        public SystemRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive)
        {
            return _random.Next(maxExclusive);
        }
    }
}
