using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1: the Jester's twenty jokers in Phase 2 fall to ten (the demo's still go back to two); after the Rattle, a reward each time.</summary>
    public class JesterJackpotTests
    {
        private static void WinUntilJackpot(Sinner sinner)
        {
            for (int i = 0; i < 40 && !sinner.HitJokerJackpot; i++) sinner.HandSettled(false, ShowdownOutcome.PlayerWins);
        }

        [Test]
        public void InPhaseTwo_TwentyJokersFallToTen_TheDemoStillGoesBackToTwo()
        {
            var demo = new Sinner(SinnerRoster.Jester);
            WinUntilJackpot(demo);
            Assert.AreEqual(2, demo.Jokers, "the demo's rule stands");

            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Jester, 3);
            Sinner jester = journey.Sinner;
            WinUntilJackpot(jester);
            Assert.IsTrue(jester.HitJokerJackpot);
            Assert.AreEqual(10, jester.Jokers, "Phase 2: down to ten");
            jester.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(10, jester.Jokers, "the loss line (ten) still stands: at ten a loss takes nothing");
        }

        [Test]
        public void AfterTheRattle_EveryTwentyIsARelicOrCoins()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Jester, 4);
            ChapterRun run = journey.Run;
            Sinner jester = journey.Sinner;
            WinUntilJackpot(jester);
            Assert.AreEqual(0, jester.JackpotRewardsWaiting, "the first is the Rattle's");
            jester.HandSettled(false, ShowdownOutcome.PlayerWins);
            WinUntilJackpot(jester);
            Assert.AreEqual(2, jester.JokerJackpots);
            Assert.AreEqual(1, jester.JackpotRewardsWaiting);
            string relic = run.TakeJackpotRelic();
            Assert.AreEqual(RelicRarity.Common, ChapterRelics.RarityOf(relic));
            Assert.IsTrue(run.Effects.Holds(relic));
            Assert.AreEqual(0, jester.JackpotRewardsWaiting);
            Assert.IsFalse(run.TakeJackpotCoins(), "nothing waits");

            jester.HandSettled(false, ShowdownOutcome.PlayerWins);
            WinUntilJackpot(jester);
            int coins = run.Purse.Coins;
            Assert.IsTrue(run.TakeJackpotCoins());
            Assert.AreEqual(coins + ChapterRun.JackpotCoins, run.Purse.Coins);

            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            Sinner back = ChapterJourney.Restore(read).Sinner;
            Assert.AreEqual(3, back.JokerJackpots);
            Assert.AreEqual(0, back.JackpotRewardsWaiting, "the rewards taken are saved");
            Assert.AreEqual(10, back.JackpotFallsTo);
        }
    }
}
