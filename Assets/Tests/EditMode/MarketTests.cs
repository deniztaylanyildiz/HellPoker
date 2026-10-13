using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's black markets: six vendors (never the same twice in a chapter), each one's wares, the haggle, the stall turned over.</summary>
    public class MarketTests
    {
        [TearDown]
        public void Reset() => ChapterTuning.Reset();

        private static ChapterRun MarketRun(int seed = 4, SinnerClass cls = null, int coins = 1000)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls ?? SinnerRoster.Peasant, seed);
            ChapterRun run = journey.Run;
            run.Purse.Add(coins - run.Purse.Coins);
            Stand(run, 3);
            return run;
        }

        private static void Stand(ChapterRun run, int floor)
        {
            run.GetType().GetProperty(nameof(ChapterRun.Current)).SetValue(run, new MapNode(floor, 0, NodeKind.BlackMarket, new[] { 0 }));
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.MarketHere)).SetValue(run, null);
        }

        private static BlackMarket Open(MarketVendor vendor, ChapterRun run = null)
        {
            ChapterTuning.MarketVendor = vendor;
            return (run ?? MarketRun()).OpenMarket();
        }

        [Test]
        public void TheVendor_IsRolled_NeverTheSameTwiceAChapter_TheSameOnTheSameNode()
        {
            ChapterRun run = MarketRun();
            var met = new List<MarketVendor>();
            for (int i = 0; i < 6; i++)
            {
                Stand(run, 3 + i);
                BlackMarket market = run.OpenMarket();
                Assert.AreEqual(market.Vendor, run.OpenMarket().Vendor, "the same market, the same vendor");
                met.Add(market.Vendor);
            }
            CollectionAssert.AllItemsAreUnique(met, "six markets, six vendors");
            Assert.AreEqual(6, run.VendorsSeen.Count);
        }

        [Test]
        public void TheSmuggler_DemoTwosMarket_AndTheNextWardensPurse()
        {
            ChapterRun run = MarketRun();
            run.NextWardenPursePercent = 150;
            BlackMarket market = Open(MarketVendor.Smuggler, run);
            Assert.AreEqual(3, market.Relics.Count);
            Assert.IsTrue(market.OffersEye);
            Assert.AreEqual(run.Rules.WardenCoins * 3 / 2, market.NextWardenPurse);
        }

        [Test]
        public void ThePawnshop_SellsForHalf_ThePawnedSoulIsYearsAtTheGate()
        {
            ChapterRun run = MarketRun();
            run.Effects.Give(RelicIds.RustyCrown);
            BlackMarket market = Open(MarketVendor.Pawnshop, run);
            Assert.AreEqual(2, market.Relics.Count);
            int coins = run.Purse.Coins, price = market.SellPrice(RelicIds.RustyCrown);
            Assert.AreEqual(run.PriceOf(ChapterRelics.PriceOf(RelicIds.RustyCrown)) / 2, price);
            Assert.IsTrue(market.SellRelic(RelicIds.RustyCrown));
            Assert.AreEqual(coins + price, run.Purse.Coins);
            Assert.IsFalse(run.Effects.Holds(RelicIds.RustyCrown));
            int sentence = run.Sentence.Years;
            Assert.IsTrue(market.Pawn());
            Assert.IsFalse(market.Pawn(), "once");
            Assert.AreEqual(80, run.YearsOwed);
            Assert.AreEqual(sentence + 80, run.Sentence.Years);
        }

        [Test]
        public void TheBoneMerchant_RareAndCheaper_EveryRelicYearsOnTheSentence()
        {
            ChapterRun run = MarketRun();
            BlackMarket market = Open(MarketVendor.BoneMerchant, run);
            Assert.AreEqual(3, market.Relics.Count);
            RelicOffer offer = market.Relics[0];
            Assert.AreEqual(run.PriceOf(ChapterRelics.PriceOf(offer.RelicId)) * 70 / 100, offer.Price, "30% cheaper");
            int sentence = run.Sentence.Years;
            Assert.IsTrue(market.BuyRelic(offer.RelicId));
            Assert.AreEqual(sentence + 40, run.Sentence.Years);
            int rare = 0;
            for (int seed = 0; seed < 60; seed++)
                rare += Open(MarketVendor.BoneMerchant, MarketRun(seed)).Relics.Count(o => ChapterRelics.RarityOf(o.RelicId) != RelicRarity.Common);
            Assert.Greater(rare, 60 * 3 * 7 / 10, "mostly rare and legendary");
        }

        [Test]
        public void TheAlchemist_TwoForOneOfTheNextRarity_ACurseBurnedForGood()
        {
            ChapterRun run = MarketRun();
            string[] commons = ChapterRelics.OfRarity(RelicRarity.Common).Where(id => id != RelicIds.RustyCrown).Take(2).ToArray();
            foreach (string id in commons) run.Effects.Give(id);
            run.Effects.Give(RelicIds.RustyCrown);
            BlackMarket market = Open(MarketVendor.Alchemist, run);
            Assert.AreEqual(1, market.Relics.Count);
            CollectionAssert.Contains(market.Transmutable, RelicRarity.Common);
            string made = market.Transmute(commons[0], commons[1]);
            Assert.AreEqual(RelicRarity.Rare, ChapterRelics.RarityOf(made));
            Assert.IsFalse(run.Effects.Holds(commons[0]) || run.Effects.Holds(commons[1]));
            int malice = run.Effects.CombinedRelics.MaliceExtraPerHand, win = run.Effects.CombinedRelics.WinPercent;
            Assert.IsTrue(market.Cleanse(RelicIds.RustyCrown));
            Assert.AreEqual(malice - 1, run.Effects.CombinedRelics.MaliceExtraPerHand, "the crown's curse burned away");
            Assert.AreEqual(win, run.Effects.CombinedRelics.WinPercent, "its gift stays");
            run.EndChapter();
            Assert.AreEqual(malice - 1, run.Effects.CombinedRelics.MaliceExtraPerHand, "for good");
        }

        [Test]
        public void TheGamblersStall_ABoxOfRolledRarity_DoubleOrNothing()
        {
            int doubled = 0, lost = 0;
            for (int seed = 0; seed < 60; seed++)
            {
                ChapterRun run = MarketRun(seed);
                BlackMarket market = Open(MarketVendor.GamblersStall, run);
                Assert.IsEmpty(market.Relics);
                Assert.IsTrue(market.BuyBox());
                Assert.IsNotNull(market.Box);
                Assert.IsFalse(market.CanBuyBox, "one box at a time");
                int before = run.Effects.AllHeld.Count();
                if (seed % 2 == 0)
                {
                    Assert.IsNotNull(market.OpenBox());
                    Assert.AreEqual(before + 1, run.Effects.AllHeld.Count());
                    continue;
                }
                IReadOnlyList<string> won = market.DoubleOrNothing();
                if (won.Count == 0) lost++;
                else
                {
                    doubled++;
                    Assert.AreEqual(2, won.Count);
                }
            }
            Assert.Greater(doubled, 5);
            Assert.Greater(lost, 5);
        }

        [Test]
        public void TheTailor_ThreeUpgradesToChooseFrom_OneAVisit()
        {
            ChapterRun run = MarketRun();
            BlackMarket market = Open(MarketVendor.Tailor, run);
            Assert.AreEqual(3, market.Upgrades.Count);
            string upgrade = market.Upgrades[1];
            int coins = run.Purse.Coins;
            Assert.IsTrue(market.BuyUpgrade(upgrade));
            Assert.AreEqual(coins - run.PriceOf(BlackMarket.UpgradePrice), run.Purse.Coins);
            CollectionAssert.Contains(run.Kit.Upgrades, upgrade);
            Assert.IsEmpty(market.Upgrades, "one a visit");
            Assert.IsTrue(market.OffersEye);
        }

        [Test]
        public void TheHaggle_OnceAFifthLessOrMore_TheStallTurnedOverOnce()
        {
            int won = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                BlackMarket market = Open(MarketVendor.Smuggler, MarketRun(seed));
                int price = market.Relics[0].Price;
                bool w = market.Haggle();
                Assert.IsFalse(market.Haggle(), "once");
                Assert.AreEqual(w ? price * 80 / 100 : price * 120 / 100, market.Relics[0].Price, 1);
                if (w) won++;
                string[] before = market.Relics.Select(o => o.RelicId).ToArray();
                Assert.IsTrue(market.Reroll());
                Assert.IsFalse(market.Reroll(), "once");
                Assert.AreEqual(3, market.Relics.Count);
            }
            Assert.AreEqual(20, won, 9, "half the time");
        }

        [Test]
        public void TheVendors_AreSaved()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 6);
            ChapterRun run = journey.Run;
            Stand(run, 3);
            BlackMarket market = run.OpenMarket();
            run.Effects.Give(RelicIds.RustyCrown);
            run.Effects.Cleanse(RelicIds.RustyCrown);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            ChapterJourney back = ChapterJourney.Restore(read);
            CollectionAssert.AreEqual(run.VendorsSeen, back.Run.VendorsSeen);
            CollectionAssert.Contains(back.Effects.Cleansed, RelicIds.RustyCrown);
            Assert.AreEqual(market.Vendor, back.Run.MarketHere);
        }
    }
}
