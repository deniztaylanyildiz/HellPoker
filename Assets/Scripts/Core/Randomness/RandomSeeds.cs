using System;

namespace HellPoker.Core.Randomness
{
    /// <summary>
    /// One master seed per game, and a separate seed derived from it for every stream of dice (the deck, the House's temper,
    /// the demon's cheats). <c>new Random()</c> seeds itself from the clock, so several made in the same tick would roll
    /// the same numbers; deriving from one master seed never does.
    /// </summary>
    public static class RandomSeeds
    {
        private static readonly object Lock = new object();
        private static readonly Random Master = new Random(Guid.NewGuid().GetHashCode());

        /// <summary>A fresh master seed, different on every call.</summary>
        public static int Fresh()
        {
            lock (Lock)
                return Master.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// The seed of stream <paramref name="stream"/> under <paramref name="master"/>: a well-mixed hash (SplitMix32), so
        /// neighbouring masters or streams give unrelated seeds (master 5's stream 1 is not master 6's stream 0).
        /// </summary>
        public static int Derive(int master, int stream)
        {
            unchecked
            {
                uint z = (uint)master + 0x9E3779B9u * (uint)(stream + 1);
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return (int)(z ^ (z >> 16));
            }
        }
    }
}
