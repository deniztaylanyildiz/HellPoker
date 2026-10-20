using System;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's camp and bag: five worn, five in the bag, the four actions, the forge, the fourth circle's two, the save.</summary>
    public class CampTests
    {
        private static string[] Relics(int n) => ChapterRelics.All.Where(id => !RelicRoster.IsReward(id)).Take(n).ToArray();

        /// <summary>A run standing at a camp (its first floor's node made one).</summary>
        private static (ChapterJourney journey, ChapterRun run) AtCamp(Difficulty difficulty = Difficulty.Easy, SinnerClass cls = null)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls ?? SinnerRoster.Peasant, 9, difficulty);
            ChapterRun run = journey.Run;
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.Current)).SetValue(run, new MapNode(3, 0, NodeKind.PurgatoryFire, new[] { 0 }));
            return (journey, run);
        }

        [Test]
        public void APhaseTwoRun_WearsFive_TheBagHoldsFive_ThenItIsFull()
        {
            var (_, run) = AtCamp();
            RunEffects e = run.Effects;
            Assert.AreEqual(5, e.MaxCarried);
            Assert.AreEqual(5, e.MaxBag);
            string[] ids = Relics(11);
            for (int i = 0; i < 10; i++) Assert.IsTrue(e.Give(ids[i]), ids[i]);
            Assert.AreEqual(5, e.Relics.Count, "the first five worn");
            CollectionAssert.AreEqual(ids.Skip(5).Take(5), e.Bag, "the rest in the bag");
            Assert.IsTrue(run.RelicsFull);
            Assert.IsFalse(e.Give(ids[10]), "both full");
            Assert.IsTrue(run.GiveRelic(ids[10]));
            Assert.AreEqual(ids[10], run.WardenRelicWaiting, "it waits for one to be left behind");
            Assert.IsTrue(run.SwapForWardenRelic(ids[7]));
            CollectionAssert.Contains(e.Bag, ids[10]);
            CollectionAssert.DoesNotContain(e.Bag, ids[7]);
        }

        [Test]
        public void TheBag_OnlyTheWornAct_TheyChangePlaces()
        {
            var (_, run) = AtCamp();
            RunEffects e = run.Effects;
            e.MaxCarried = 1;
            Assert.IsTrue(e.Give(RelicIds.RustyCrown));
            Assert.IsTrue(e.Give(RelicIds.FerrymansCoin));
            CollectionAssert.AreEqual(new[] { RelicIds.FerrymansCoin }, e.Bag);
            Assert.AreEqual(1, e.CombinedRelics.MaliceExtraPerHand, "the crown acts");
            Assert.AreEqual(100, e.CombinedRelics.AntePercent, "the coin waits in the bag");
            Assert.IsTrue(e.Swap(RelicIds.RustyCrown, RelicIds.FerrymansCoin));
            Assert.AreEqual(80, e.CombinedRelics.AntePercent);
            Assert.AreEqual(0, e.CombinedRelics.MaliceExtraPerHand);
            Assert.IsTrue(e.Unequip(RelicIds.FerrymansCoin));
            Assert.IsEmpty(e.Relics);
            Assert.IsTrue(e.Equip(RelicIds.RustyCrown));
            Assert.IsTrue(e.Drop(RelicIds.FerrymansCoin));
            Assert.IsFalse(e.Holds(RelicIds.FerrymansCoin), "left behind for good");
        }

        [Test]
        public void TheMarket_SellsWhileAnythingIsFree_NotOnlyToTwo()
        {
            var (_, run) = AtCamp();
            foreach (string id in Relics(4)) run.Effects.Give(id);
            run.Purse.Add(500);
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.Current)).SetValue(run, new MapNode(3, 0, NodeKind.BlackMarket, new[] { 0 }));
            ChapterTuning.MarketVendor = MarketVendor.Smuggler;   // Demo 2.0's market: three relics
            BlackMarket market;
            try { market = run.OpenMarket(); }
            finally { ChapterTuning.Reset(); }
            Assert.IsFalse(market.SellsNoMore);
            Assert.IsTrue(market.BuyRelic(market.Relics[0].RelicId));
            Assert.AreEqual(5, run.Effects.Relics.Count);
            Assert.IsTrue(market.BuyRelic(market.Relics[0].RelicId), "into the bag");
            Assert.AreEqual(1, run.Effects.Bag.Count);
        }

        [Test]
        public void Rest_TheSentenceFivePercentShorter_TheNextBarThree()
        {
            var (journey, run) = AtCamp();
            int sentence = journey.Sentence.Years;
            Assert.IsTrue(run.TendFire(FireChoice.RestByTheFire));
            Assert.AreEqual(325, run.RestYears, "the Peasant: 5% of 6500");
            Assert.AreEqual(sentence - 325, journey.Sentence.Years);
            Assert.AreEqual(-3, run.CampBarPercent);
            Assert.AreEqual(run.Years * 97 / 100 + 0, run.BarAfterGate(10000), "the bar three percent shorter (the tribute paid)");
        }

        [Test]
        public void Whet_AnUpgrade_TheNextBarTenPercentLonger_TheCampsAddUp()
        {
            var (_, run) = AtCamp();
            Assert.IsTrue(run.TendFire(FireChoice.WhetSoul));
            Assert.IsNotNull(run.FireUpgrade);
            Assert.AreEqual(10, run.CampBarPercent);
            Assert.IsTrue(run.TendFire(FireChoice.RestByTheFire), "a second camp");
            Assert.AreEqual(7, run.CampBarPercent);
            run.PayTribute();
            int years = run.Years;
            run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(years * 107 / 100, run.BossBarStart);
            Assert.AreEqual(0, run.CampBarPercent, "set back as the table opens");
        }

        [Test]
        public void TheRite_BreaksTheFirstCheat_OrSilencesACurse()
        {
            var (_, run) = AtCamp();
            Assert.IsFalse(run.CanTend(FireChoice.SilenceCurse), "nothing worn");
            Assert.IsTrue(run.TendFire(FireChoice.BreakFirstCheat));
            Assert.IsTrue(run.BreaksFirstCheat);
            run.Effects.Give(RelicIds.RustyCrown);
            Assert.IsTrue(run.TendFire(FireChoice.SilenceCurse, RelicIds.RustyCrown));
            Assert.AreEqual(0, run.Effects.CombinedRelics.MaliceExtraPerHand);
        }

        [Test]
        public void TheForge_AWornRelicsGiftHalfAgain_OnceARelic()
        {
            var (_, run) = AtCamp();
            run.Effects.Give(RelicIds.FerrymansCoin);
            Assert.IsTrue(run.CanTend(FireChoice.Forge));
            Assert.IsTrue(run.TendFire(FireChoice.Forge, RelicIds.FerrymansCoin));
            CollectionAssert.Contains(run.Effects.Forged, RelicIds.FerrymansCoin);
            Assert.AreEqual(80 * 90 / 100, run.Effects.CombinedRelics.AntePercent, "a fifth off the ante, and half of it again");
            Assert.AreEqual(-1, run.Effects.CombinedRelics.HouseCardsDelta, "the curse stays as it was");
            Assert.IsFalse(run.CanTend(FireChoice.Forge), "once");
            Assert.AreEqual(90, new RelicEffects(antePercent: 80).Partial(50).AntePercent);
            Assert.AreEqual(1, new RelicEffects(redrawsPerTable: 1).Partial(50).RedrawsPerTable, "a single redraw stays one");
        }

        [Test]
        public void EveryCamp_OffersAllFourActions_AtEveryDifficulty()
        {
            // Demo 3.4: the fourth circle's two of four went with the circles.
            for (Difficulty d = Difficulty.Easy; d <= DifficultyRules.Hardest; d++)
                Assert.AreEqual(4, AtCamp(d).run.CampOffers.Count, d.ToString());
        }

        [Test]
        public void TheBagTheForgeAndTheCampsSum_AreSaved_AnOlderSavesRelicsAreWorn()
        {
            var (journey, run) = AtCamp();
            string[] ids = Relics(7);
            foreach (string id in ids) run.Effects.Give(id);
            run.Effects.Forge(ids[0]);
            run.TendFire(FireChoice.RestByTheFire);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            ChapterJourney back = ChapterJourney.Restore(read);
            CollectionAssert.AreEqual(run.Effects.Relics, back.Effects.Relics);
            CollectionAssert.AreEqual(run.Effects.Bag, back.Effects.Bag);
            CollectionAssert.Contains(back.Effects.Forged, ids[0]);
            Assert.AreEqual(-3, back.Run.CampBarPercent);

            string old = string.Join("\n", journey.Capture().Encode().Split('\n')
                .Where(l => !l.StartsWith("bag=", StringComparison.Ordinal) && !l.StartsWith("camp.bar=", StringComparison.Ordinal)));
            ChapterJourney older = ChapterJourney.Restore(ChapterSave.TryDecode(old, out ChapterSave o) ? o : null);
            Assert.IsEmpty(older.Effects.Bag, "an older save has no bag: its relics are worn");
            Assert.AreEqual(5, older.Effects.Relics.Count);
        }
    }
}
