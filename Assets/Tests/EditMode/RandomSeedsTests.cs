using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>One master seed per game, a separate stream for each kind of dice — never the same clock tick twice.</summary>
    public class RandomSeedsTests
    {
        [Test]
        public void EveryStream_GetsItsOwnSeed_AndNeighboursDoNotOverlap()
        {
            var seeds = new HashSet<int>();
            for (int master = 0; master < 200; master++)
            for (int stream = HellPokerGameFactory.DeckStream; stream <= HellPokerGameFactory.CheatStream; stream++)
                seeds.Add(RandomSeeds.Derive(master, stream));

            Assert.AreEqual(600, seeds.Count, "Master 5's house stream is not master 6's deck stream.");
        }

        [Test]
        public void Deriving_IsReproducible()
        {
            Assert.AreEqual(RandomSeeds.Derive(666, 1), RandomSeeds.Derive(666, 1));
        }

        [Test]
        public void FreshSeeds_MadeTogether_AreAllDifferent()
        {
            int[] fresh = Enumerable.Range(0, 50).Select(_ => RandomSeeds.Fresh()).ToArray();

            Assert.AreEqual(50, fresh.Distinct().Count());
        }

        [Test]
        public void GamesMadeInTheSameInstant_ShuffleDifferently()
        {
            var first = HellPokerGameFactory.Create();
            var second = HellPokerGameFactory.Create();
            first.PlaceBet();
            second.PlaceBet();

            Assert.AreNotEqual(first.PlayerHand.ToString() + first.HouseHand, second.PlayerHand.ToString() + second.HouseHand);
        }
    }
}
