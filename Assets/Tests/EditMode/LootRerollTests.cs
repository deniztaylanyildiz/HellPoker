using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.5: the loot's reroll (ARAM's) — once, the same rules, nothing held or on offer before, the upgrade and the coins kept.</summary>
    public class LootRerollTests
    {
        private static string MapText(ChapterMap map) =>
            string.Join("|", map.Floors.Select(floor => string.Join(",", floor.Select(n => n.Lane + ":" + n.Kind))));

        private static ChapterJourney Beaten(int seed, SinnerClass cls = null)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls ?? SinnerRoster.King, seed);
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            return journey;
        }

        [Test]
        public void TheReroll_Once_EachRelicByAnotherOfItsRarity_NeverHeldNorOfferedBefore_TheUpgradeKept()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                ChapterJourney journey = Beaten(seed);
                ChapterRun run = journey.Run;
                var before = run.LootOffers.ToList();
                string upgrade = run.LootUpgrade;
                Assert.IsTrue(run.CanRerollLoot, "a fresh loot may be drawn again");
                Assert.IsTrue(run.RerollLoot());
                var after = run.LootOffers.ToList();
                Assert.AreEqual(before.Count, after.Count, "as many cards");
                Assert.AreEqual(upgrade, run.LootUpgrade, "the upgrade stays");
                for (int i = 0; i < after.Count; i++)
                {
                    Assert.IsFalse(before.Contains(after[i]) && after[i] != before[i], "never one offered before (in another place)");
                    if (after[i] == before[i]) continue;   // nothing else of its kind
                    Assert.AreEqual(ChapterRelics.RarityOf(before[i]), ChapterRelics.RarityOf(after[i]), $"seed {seed}: the same rarity");
                    Assert.IsFalse(run.Effects.Holds(after[i]), "never one held");
                    Assert.IsFalse(ChapterRelics.IsClassRelic(after[i]), "never a class relic");
                }
                Assert.AreEqual(after.Count, after.Distinct().Count());
                Assert.IsFalse(run.CanRerollLoot, "once");
                Assert.IsFalse(run.RerollLoot());
                CollectionAssert.AreEqual(after, run.LootOffers, "a second press changes nothing");
            }
        }

        [Test]
        public void MammonsOwn_AreRerolledToAnotherDemonsOwn_ThenTheChoiceTakesTheNewOnes()
        {
            ChapterRun run = Beaten(7).Run;
            CollectionAssert.AreEquivalent(ChapterRelics.BossLoot(Core.Dealers.DealerRoster.MammonId), run.LootOffers);
            run.RerollLoot();
            CollectionAssert.AreEquivalent(ChapterRelics.BossLoot(Core.Dealers.DealerRoster.BelialId), run.LootOffers,
                "the demons' own are four: Mammon's two become Belial's two");
            Assert.IsFalse(run.TakeLoot(ChapterRelicIds.MammonsSeal), "the old card is gone");
            Assert.IsTrue(run.TakeLoot(run.LootOffers[0]));
            Assert.IsFalse(run.CanRerollLoot, "nothing after the loot is taken");
        }

        [Test]
        public void TheReroll_IsKeptInTheSave_TheSameCards_TheRightSpent()
        {
            ChapterJourney journey = Beaten(11);
            journey.Run.RerollLoot();
            ChapterSave save = journey.Capture();
            save.Pending = "loot";
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            Assert.IsTrue(read.LootRerolled);
            ChapterJourney back = ChapterJourney.Restore(read);
            CollectionAssert.AreEqual(journey.Run.LootOffers, back.Run.LootOffers, "the same cards");
            Assert.IsFalse(back.Run.CanRerollLoot, "its reroll already spent");

            ChapterJourney fresh = Beaten(12);
            Assert.IsTrue(ChapterSave.TryDecode(fresh.Capture().Encode(), out ChapterSave freshSave));
            ChapterJourney freshBack = ChapterJourney.Restore(freshSave);
            Assert.IsTrue(freshBack.Run.CanRerollLoot, "an old save (or one not rerolled) keeps its reroll");
        }

        [Test]
        public void TheReroll_DrawsOnItsOwnDice_NothingElseInTheRunShifts()
        {
            ChapterJourney a = Beaten(21), b = Beaten(21);
            b.Run.RerollLoot();
            Assert.IsTrue(a.Run.TakeLootCoins());
            Assert.IsTrue(b.Run.TakeLootCoins());
            a.NextChapter();
            b.NextChapter();
            Assert.AreEqual(MapText(a.Run.Map), MapText(b.Run.Map), "the next chapter's map the same");
            Assert.AreEqual(a.Run.Purse.Coins, b.Run.Purse.Coins);
        }
    }
}
