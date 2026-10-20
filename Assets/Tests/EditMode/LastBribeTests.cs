using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.5.1, the Last Bribe: at Lilith's gate every coin left after the tribute goes to her and her bar shrinks — one percent a
    /// five coins, at most fifteen; nowhere else; once; Mammon's Gold Tooth still counts the coins.</summary>
    public class LastBribeTests
    {
        /// <summary>A run at Lilith's gate (Mammon and Belial beaten the chapters' own way), its purse set to <paramref name="coins"/>.</summary>
        private static ChapterJourney AtLilithsGate(int coins, int seed = 11)
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, seed);
            for (int chapter = 1; chapter < ChapterRules.BribeChapter; chapter++)
            {
                journey.Run.PayTribute();
                journey.Run.LeaveBossTable(0);
                journey.Run.TakeLootCoins();
                journey.NextChapter();
            }
            ChapterRun run = journey.Run;
            run.Purse.Add(coins - run.Purse.Coins);
            return journey;
        }

        [TestCase(0, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(42, 8)]
        [TestCase(75, 15)]
        [TestCase(400, 15)]
        public void TheBribe_IsOnePercentAFiveCoins_AtMostFifteen(int coins, int percent) =>
            Assert.AreEqual(percent, ChapterRun.BribePercentFor(coins));

        [Test]
        public void AtLilithsGate_TheCoinsLeftGoToHer_AndHerBarShrinks()
        {
            ChapterJourney journey = AtLilithsGate(1000);
            ChapterRun run = journey.Run;
            Assert.AreEqual(ChapterRules.BribeChapter, run.Rules.Number);
            int left = 1000 - run.TributePaid(1000);
            int unbribed = run.BarAfterGate(1000, bribe: false);
            int predicted = run.BarAfterGate(1000);
            GateToll toll = run.PayTribute();
            int percent = ChapterRun.BribePercentFor(left);
            Assert.AreEqual(left, toll.BribeCoins);
            Assert.AreEqual(percent, toll.BribePercent);
            Assert.AreEqual(0, run.Purse.Coins, "every coin left goes to her");
            Assert.AreEqual(left, run.BribeCoins);
            Assert.AreEqual(unbribed * (100 - percent) / 100, run.Years, "her bar shrinks before the table");
            Assert.AreEqual(predicted, run.Years, "the gate's panel foretold it");
        }

        [Test]
        public void TheBribe_StopsAtFifteenPercent()
        {
            ChapterRun run = AtLilithsGate(5000).Run;
            int unbribed = run.BarAfterGate(5000, bribe: false);
            GateToll toll = run.PayTribute();
            Assert.AreEqual(ChapterRules.BribeMostPercent, toll.BribePercent);
            Assert.AreEqual(unbribed * (100 - ChapterRules.BribeMostPercent) / 100, run.Years);
        }

        [Test]
        public void FewCoinsLeft_TheBarStaysAsItWas()
        {
            ChapterRun run = AtLilithsGate(0).Run;
            run.Purse.Add(run.Tribute + 3 - run.Purse.Coins);   // three coins left after the tribute
            int unbribed = run.BarAfterGate(run.Purse.Coins, bribe: false);
            GateToll toll = run.PayTribute();
            Assert.AreEqual(0, toll.BribePercent);
            Assert.AreEqual(unbribed, run.Years);
        }

        [Test]
        public void MammonsAndBelialsGates_TakeNoBribe()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, 11);
            journey.Run.Purse.Add(1000);
            int coins = journey.Run.Purse.Coins;
            GateToll toll = journey.Run.PayTribute();
            Assert.AreEqual(0, toll.BribeCoins);
            Assert.AreEqual(coins - toll.Paid, journey.Run.Purse.Coins, "the purse goes on");
        }

        [Test]
        public void TheGoldTooth_CountsTheCoinsBeforeTheBribe()
        {
            ChapterRun run = AtLilithsGate(400).Run;
            run.PayTribute();
            Assert.AreEqual(400, run.GateCoins, "Lilith's table: the coins brought to the gate");
            Assert.AreEqual(400 - run.TributePaid(400), run.Purse.Coins + run.BribeCoins, "Lucifer's: the coins after the tribute, the bribe's counted");
        }

        [Test]
        public void ASave_PaysTheBribeOnce()
        {
            // Closed at the gate (before paying): the bribe comes once, as without the save.
            ChapterJourney straight = AtLilithsGate(300);
            straight.Run.PayTribute();
            ChapterJourney saved = AtLilithsGate(300);
            Assert.IsTrue(ChapterSave.TryDecode(saved.Capture().Encode(), out ChapterSave atGate));
            ChapterJourney back = ChapterJourney.Restore(atGate);
            back.Run.PayTribute();
            Assert.AreEqual(straight.Run.Years, back.Run.Years);
            Assert.AreEqual(straight.Run.BribeCoins, back.Run.BribeCoins);

            // Saved after paying: nothing is paid again, the bribe's coins are kept.
            Assert.IsTrue(ChapterSave.TryDecode(straight.Capture().Encode(), out ChapterSave paid));
            ChapterJourney after = ChapterJourney.Restore(paid);
            Assert.AreEqual(straight.Run.Years, after.Run.Years);
            Assert.AreEqual(straight.Run.BribeCoins, after.Run.BribeCoins);
            Assert.AreEqual(0, after.Run.Purse.Coins);
        }
    }
}
