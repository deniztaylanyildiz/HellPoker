using System.Collections.Generic;
using System.IO;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Phase 2's build variety: the relic pool by rarity, what the relics change in a chapter, the class upgrades, the fire's whetted soul.</summary>
    public class ChapterBuildTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        private static HellPokerGame Stacked(string cards, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private static Card C(string token) => TestCards.Card(token);

        private static ChapterRun Run(SinnerClass cls = null, int coins = 100, int chapter = 1)
        {
            var effects = new RunEffects { MaxCarried = ChapterRelics.Slots };
            return new ChapterRun(ChapterRules.For(chapter), new Sinner(cls ?? SinnerRoster.Peasant), effects, 1000, coins, 7);
        }

        private static void Stand(ChapterRun run, NodeKind kind) =>
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.Current)).SetValue(run, new MapNode(2, 0, kind, new[] { 0 }));

        // ------------------------------------------------------------------ the pool

        [Test]
        public void ThePool_ThirtyNineRelics_ByRarity_AndTwoOfEachLootingDemonsOwn()
        {
            Assert.AreEqual(40, ChapterRelics.All.Count, "Demo 2.1: fifteen more; 2.1.1: the Devil's Compass");
            Assert.AreEqual(12, ChapterRelics.OfRarity(RelicRarity.Common).Count());
            Assert.AreEqual(18, ChapterRelics.OfRarity(RelicRarity.Rare).Count(), "2.1.1: the compass is rare");
            Assert.AreEqual(6, ChapterRelics.OfRarity(RelicRarity.Legendary).Count());
            Assert.AreEqual(4, ChapterRelics.OfRarity(RelicRarity.Boss).Count());
            Assert.AreEqual(2, ChapterRelics.BossLoot(DealerRoster.MammonId).Count);
            Assert.AreEqual(2, ChapterRelics.BossLoot(DealerRoster.BelialId).Count);
            Assert.IsEmpty(ChapterRelics.BossLoot(DealerRoster.LilithId), "Lilith's fall leads straight to Lucifer: no loot");
            Assert.AreEqual(RelicRarity.Reward, ChapterRelics.RarityOf(RelicIds.JestersRattle));
            foreach (string id in ChapterRelics.All) Assert.IsNotNull(RelicRoster.Find(id), id);
            Assert.AreEqual(RelicRoster.All.Count + ChapterRelics.New.Count, RelicRoster.IconOrder.Count);
            Assert.AreEqual(RelicRoster.IconOrder.Count, RelicRoster.IconOrder.Distinct().Count());
        }

        [Test]
        public void TheDemosRelics_AreUntouched()
        {
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.FerrymansCoin, RelicIds.ThornedRosary },
                RelicRoster.Offered.Select(r => r.Id), "a demo event offers the four it always did");
            Assert.AreEqual(2, RelicRoster.MaxCarried);
            Assert.AreEqual(2, new RunEffects().MaxCarried, "a demo run carries two");
        }

        [Test]
        public void TheIconStrip_HasAnIconForEveryRelic()
        {
            string path = Path.Combine("Assets", "Resources", "Art", "Ui", "relic_icons.png");
            byte[] header = File.ReadAllBytes(path).Take(24).ToArray();
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            Assert.AreEqual(RelicRoster.IconOrder.Count * 16, width);
        }

        [Test]
        public void TheDraw_FollowsTheWeights_AndNeverGivesWhatIsCarried()
        {
            var random = new SystemRandomSource(11);
            var counts = new Dictionary<RelicRarity, int>();
            const int draws = 6000;
            for (int i = 0; i < draws; i++)
            {
                string id = ChapterRelics.Draw(random, RelicWeights.Market, 1).Single();
                RelicRarity r = ChapterRelics.RarityOf(id);
                counts[r] = (counts.TryGetValue(r, out int n) ? n : 0) + 1;
            }
            Assert.AreEqual(67, 100.0 * counts[RelicRarity.Common] / draws, 3);
            Assert.AreEqual(25, 100.0 * counts[RelicRarity.Rare] / draws, 3);
            Assert.AreEqual(8, 100.0 * counts[RelicRarity.Legendary] / draws, 2);
            Assert.IsFalse(counts.ContainsKey(RelicRarity.Boss), "a demon's own never comes from a draw");

            var warden = new Dictionary<RelicRarity, int>();
            for (int i = 0; i < draws; i++)
            {
                RelicRarity r = ChapterRelics.RarityOf(ChapterRelics.Draw(random, RelicWeights.Warden, 1).Single());
                warden[r] = (warden.TryGetValue(r, out int n) ? n : 0) + 1;
            }
            Assert.AreEqual(40, 100.0 * warden[RelicRarity.Rare] / draws, 3);
            Assert.AreEqual(15, 100.0 * warden[RelicRarity.Legendary] / draws, 3);

            var carried = ChapterRelics.OfRarity(RelicRarity.Legendary).ToList();
            for (int i = 0; i < 200; i++)
                CollectionAssert.DoesNotContain(carried, ChapterRelics.Draw(random, RelicWeights.Warden, 1, carried).Single());
            Assert.AreEqual(3, ChapterRelics.Draw(random, RelicWeights.Market, 3).Distinct().Count(), "three different");
            Assert.IsEmpty(ChapterRelics.Draw(random, RelicWeights.Market, 1, available: _ => false), "nothing unlocked: nothing drawn");
        }

        // ------------------------------------------------------------------ what the relics change in a chapter

        [Test]
        public void TheRelics_ChangeTheChaptersNumbers()
        {
            ChapterRun run = Run();
            int tribute = run.Tribute, treasure = run.TreasureCoins, imp = run.ImpCoinsAt(1), price = run.PriceOf(45);
            run.Effects.AddRelic(ChapterRelicIds.MammonsSeal);    // tribute 75%
            run.Effects.AddRelic(ChapterRelicIds.SaltCircle);     // treasure ×2, imps 115%
            run.Effects.AddRelic(ChapterRelicIds.OldMap);         // market 75%, tribute 110%
            Assert.AreEqual(tribute * 75 / 100 * 110 / 100, run.Tribute, 1);
            Assert.AreEqual(treasure * 2, run.TreasureCoins);
            Assert.AreEqual(imp * 115 / 100, run.ImpCoinsAt(1));
            Assert.AreEqual(price * 75 / 100, run.PriceOf(45));
            Stand(run, NodeKind.Table);
            Assert.AreEqual(run.ImpCoinsAt(2), run.OpenTable().HousePurse.Coins, "the imp's purse at the table");
        }

        [Test]
        public void ARelicOfTheBar_ChangesWhereTheDemonsBarBegins()
        {
            ChapterRun run = Run(coins: 1000);
            run.Effects.AddRelic(ChapterRelicIds.RavenFeather);   // 92%
            Assert.AreEqual(1000 * 92 / 100, run.BarAfterGate(run.Purse.Coins), "the gate tells it");
            run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(920, run.BossBarStart);
            HellPokerGame again = run.ReopenBossTable(GameRules.Default, 920, 900, 3);
            Assert.AreEqual(920, run.BossBarStart, "a saved table is not shortened twice");
            Assert.AreEqual(900, again.Years);
        }

        [Test]
        public void APhaseTwoRun_WearsFiveRelics()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, 3);
            Assert.IsTrue(journey.Effects.AddRelic(RelicIds.BoneDie));
            Assert.IsTrue(journey.Effects.AddRelic(ChapterRelicIds.BlackBook));
            Assert.IsTrue(journey.Effects.AddRelic(ChapterRelicIds.IronVow));
            Assert.IsTrue(journey.Effects.AddRelic(RelicIds.RustyCrown));
            Assert.IsTrue(journey.Effects.AddRelic(RelicIds.FerrymansCoin));
            Assert.IsFalse(journey.Effects.AddRelic(ChapterRelicIds.OldMap), "five worn at most (Demo 2.1; the rest goes in the bag)");
            Assert.IsTrue(journey.Effects.RelicSlotsFull);
            Assert.IsTrue(journey.Effects.AddRelic(RelicIds.JestersRattle), "an earned one is beyond the limit");
        }

        // ------------------------------------------------------------------ the class upgrades

        [Test]
        public void EveryClass_HasFourUpgrades_OnlyItsOwn_OnceEach()
        {
            foreach (SinnerClass cls in SinnerRoster.All)
            {
                Assert.AreEqual(4, ClassUpgrades.For(cls.Id).Count, cls.Id);
                var kit = new ClassKit(new Sinner(cls));
                foreach (string id in ClassUpgrades.For(cls.Id))
                {
                    Assert.AreEqual(cls.Id, ClassUpgrades.ClassOf(id));
                    Assert.IsTrue(kit.AddUpgrade(id));
                    Assert.IsFalse(kit.AddUpgrade(id), "once a run");
                }
                Assert.IsEmpty(kit.UpgradesLeft);
                string other = ClassUpgrades.All.First(u => ClassUpgrades.ClassOf(u) != cls.Id);
                Assert.IsFalse(kit.AddUpgrade(other), "another class's");
            }
            Assert.AreEqual(16, ClassUpgrades.All.Distinct().Count());
        }

        [Test]
        public void ThePeasantsUpgrades_AWiderSatchel_TwoKept_AQuieterOne()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            Assert.AreEqual(3, kit.SatchelCapacity);
            kit.AddUpgrade(UpgradeIds.WideSatchel);
            foreach (string c in new[] { "2C", "3C", "4C", "5C", "6C" }) kit.Keep(C(c));
            CollectionAssert.AreEqual(new[] { C("3C"), C("4C"), C("5C"), C("6C") }, kit.Satchel, "four at most");

            Assert.AreEqual(ClassKit.SatchelReRaisePercent, kit.SatchelRisk);
            kit.AddUpgrade(UpgradeIds.QuietSatchel);
            Assert.AreEqual(ClassKit.SatchelReRaisePercent / 2, kit.SatchelRisk);

            var abundance = new ClassKit(new Sinner(SinnerRoster.Peasant));
            abundance.AddUpgrade(UpgradeIds.Abundance);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", abundance.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, abundance, null, 0, 1);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new[] { 2, 3, 4 });
            Assert.IsTrue(hooks.Satchel.Keep(game, C("4S")));
            Assert.IsTrue(hooks.Satchel.Keep(game, C("2D")), "Abundance: a second card");
            Assert.IsFalse(hooks.Satchel.Keep(game, C("7C")), "two a hand");
            Assert.AreEqual(2, abundance.Satchel.Count);
        }

        [Test]
        public void TheSeed_PutsATenOrBetterInTheSatchel_AsAChapterBegins()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 4);
            journey.Kit.AddUpgrade(UpgradeIds.Seed);
            BeatTheDemon(journey);
            journey.Run.TakeLootCoins();
            journey.NextChapter();
            Assert.IsTrue(journey.Sown.HasValue);
            Assert.GreaterOrEqual((int)journey.Sown.Value.Rank, (int)Rank.Ten);
            CollectionAssert.Contains(journey.Kit.Satchel, journey.Sown.Value);
            Assert.IsNull(new ClassKit(new Sinner(SinnerRoster.Peasant)).Sow(new SystemRandomSource(1)), "only with the upgrade");
        }

        private static void BeatTheDemon(ChapterJourney journey)
        {
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
        }

        [Test]
        public void TheOraclesUpgrades_ThirdEye_FateWeaver_ABlindMirror()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock, charge: 5));
            kit.AddUpgrade(UpgradeIds.DeepProphecy);
            kit.AddUpgrade(UpgradeIds.Thrice);
            Assert.AreEqual(3, kit.ProphecyCards, "the Third Eye: one more");
            kit.EnterLucifer();
            Assert.AreEqual(3, kit.ProphecyCards);
            kit.EnterChapter(1);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            Assert.IsTrue(hooks.Prophecy.StartRearranging(game));
            Assert.AreEqual(2, hooks.Prophecy.RearrangesLeft, "Thrice: one more (2.1.1: one a use)");

            var blind = new ClassKit(new Sinner(SinnerRoster.Warlock));
            blind.AddUpgrade(UpgradeIds.BlindMirror);
            int known = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                HellPokerGame g = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", blind.Sinner);
                ChapterHooks h = ChapterHooks.Attach(g, blind, null, 0, RandomSeeds.Derive(77, seed));
                g.PlaceBet();
                if (h.Prophecy.KnownCard.HasValue) known++;
            }
            Assert.AreEqual(100, known, 25, "about half the hands");
        }

        [Test]
        public void AWarlocksSave_OpensAsTheOracle_WithItsUpgrades()
        {
            // A 2.1.1 save: the class id and the upgrade ids are the Warlock's (Demo 2.2 kept them).
            ChapterJourney old = ChapterJourney.Begin(SinnerRoster.Warlock, 5);
            old.Kit.AddUpgrade(UpgradeIds.DeepProphecy);
            old.Kit.AddUpgrade(UpgradeIds.EyeOfTruth);
            string text = old.Capture().Encode();
            StringAssert.Contains("warlock", text);
            Assert.IsTrue(ChapterSave.TryDecode(text, out ChapterSave read));
            ChapterJourney back = ChapterJourney.Restore(read);
            Assert.AreEqual(Warlock.ClassId, back.Sinner.Id);
            Assert.AreEqual(KitKind.Prophecy, back.Kit.Kind);
            CollectionAssert.AreEqual(new[] { UpgradeIds.DeepProphecy, UpgradeIds.EyeOfTruth }, back.Kit.Upgrades);
            Assert.AreEqual(3, back.Kit.ProphecyCards);
            Assert.AreEqual("ORACLE", HellPoker.Presentation.Ui.UiText.SinnerName(Warlock.ClassId));
        }

        [Test]
        public void TheProphecySeal_PaysForASwindlerWalkedAwayFrom_NotForAnHonestOffer()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Warlock, 3);
            ChapterRun run = journey.Run;
            int purse = run.Purse.Coins;
            EventOutcome passed = new FakePardon().Choose(run, EventChoices.Pass);
            Assert.AreEqual("pass", passed.Result, "without the seal: nothing");
            Assert.AreEqual(purse, run.Purse.Coins);

            run.Kit.AddUpgrade(UpgradeIds.EyeOfTruth);
            EventOutcome sealedOne = new SoulInsurance().Choose(run, EventChoices.Pass);
            Assert.AreEqual("sealed", sealedOne.Result);
            Assert.AreEqual(ClassKit.SealCoins, sealedOne.Coins);
            Assert.AreEqual(purse + ClassKit.SealCoins, run.Purse.Coins);
            Assert.AreEqual(ClassKit.SealCoins, run.Kit.SealCoinsEarned);

            new ChainedSoul().Choose(run, EventChoices.Pass);
            Assert.AreEqual(purse + ClassKit.SealCoins, run.Purse.Coins, "a soul asking for help is no swindler");
            new FakePardon().Choose(run, "buy");
            Assert.AreEqual(ClassKit.SealCoins, run.Kit.SealCoinsEarned, "taken, it pays nothing");
        }

        [Test]
        public void TheKingsUpgrades_ABiggerTreasury_CheaperActs_AGuardedCrown()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            kit.AddTax(20);
            Assert.AreEqual(10, kit.Taxes);
            kit.AddUpgrade(UpgradeIds.OverflowingTreasury);
            kit.AddTax(20);
            Assert.AreEqual(15, kit.Taxes);

            Assert.AreEqual(4, kit.PriceOf(TaxAct.Seize));
            Assert.AreEqual(2, kit.PriceOf(TaxAct.FreeRaise));
            kit.AddUpgrade(UpgradeIds.CheapSeizure);
            kit.AddUpgrade(UpgradeIds.RoyalDecree);
            Assert.AreEqual(3, kit.PriceOf(TaxAct.Seize));
            Assert.AreEqual(1, kit.PriceOf(TaxAct.FreeRaise));
            Assert.AreEqual(1, kit.PriceOf(TaxAct.Reveal));

            kit.Restore(null, 8, null, 0, kit.Upgrades.ToList());
            kit.HalveTaxes();
            Assert.AreEqual(4, kit.Taxes, "half without the guard");
            kit.AddUpgrade(UpgradeIds.CrownGuard);
            kit.Restore(null, 8, null, 0, kit.Upgrades.ToList());
            kit.HalveTaxes();
            Assert.AreEqual(6, kit.Taxes, "a quarter with it");
        }

        [Test]
        public void TheJestersUpgrades_TheDoubleLaugh_ThrowsASecondJokerBack()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Find(Jester.ClassId)));
            kit.AddUpgrade(UpgradeIds.DoubleLaugh);
            // The Jester holds a joker; the draw brings the second, then a plain card waits on top.
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            // The jokers placed by hand (the Jester's deck puts its own at the bottom as a hand is dealt).
            game.SetHands(TestCards.Hand("JK1 KD 7C 4S 2D"), game.HouseHand);
            game.Deck.Restore(TestCards.Cards("JK2 10C JH 5D 2C 3S 4H 6S 6D").ToList());
            game.CheckToDraw();
            game.Draw(new[] { 4 });
            Assert.AreEqual(1, JokerResolver.CountJokers(game.PlayerHand), "one joker left");
            Assert.IsTrue(hooks.Upgrades.LaughedThisHand);
            CollectionAssert.Contains(game.Deck.Remaining, C("JK2"), "the joker went back under the deck");
        }

        [Test]
        public void TheJestersUpgrades_ControlledChaos_ALossTakesAJoker_TheBuffoonCalmsTheDemon()
        {
            var sinner = new Sinner(SinnerRoster.Find(Jester.ClassId), jokers: 5);
            var kit = new ClassKit(sinner);
            kit.AddUpgrade(UpgradeIds.ControlledChaos);
            HellPokerGame loss = Stacked("QS 9H 8D 5C 3H  AH AD 7C 4S 2D  6H 10C JH 5D 2C 3S 4H", sinner);
            ChapterHooks.Attach(loss, kit, null, 0, 1);
            loss.PlaceBet();
            loss.CheckToDraw();
            loss.Draw(new int[0]);
            while (loss.Phase == GamePhase.DrawReveal || loss.Phase == GamePhase.HouseReveal) loss.Bet(BetAction.Pass);
            Assert.AreEqual(4, sinner.Jokers, "a lost hand took one (below the line of ten)");

            var buffoon = new ClassKit(new Sinner(SinnerRoster.Find(Jester.ClassId)));
            buffoon.AddUpgrade(UpgradeIds.Buffoon);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", buffoon.Sinner);
            game.PlaceBet();
            game.SetHands(TestCards.Hand("JK1 KD 7C 4S 2D"), game.HouseHand);
            Assert.IsTrue(buffoon.Has(UpgradeIds.Buffoon));
            Assert.AreEqual(1, JokerResolver.CountJokers(game.PlayerHand), string.Join(" ", game.PlayerHand.Select(c => c.ToString())));
            int yes = 0;
            var calm = new UpgradeHooks(buffoon, new SystemRandomSource(5));
            for (int i = 0; i < 200; i++)
                if (calm.HouseReRaises(game, true)) yes++;
            Assert.IsFalse(new UpgradeHooks(buffoon, new SystemRandomSource(1)).HouseReRaises(game, false), "never makes him raise");
            Assert.AreEqual(100, yes, 25, "half of what he would");
        }

        [Test]
        public void TheJokerPurse_HalvesTheMarketsJokerService()
        {
            ChapterRun run = Run(SinnerRoster.Find(Jester.ClassId));
            Stand(run, NodeKind.BlackMarket);
            int full = run.OpenMarket().JokerServicePrice;
            run.Kit.AddUpgrade(UpgradeIds.JokerPurse);
            Assert.AreEqual(full / 2, run.OpenMarket().JokerServicePrice);
        }

        // ------------------------------------------------------------------ the fire's whetted soul

        [Test]
        public void TheFire_WhetTheSoul_AnUpgradeNow_ALongerBar()
        {
            ChapterRun run = Run(coins: 1000);
            Stand(run, NodeKind.PurgatoryFire);
            Assert.IsTrue(run.CanTend(FireChoice.WhetSoul));
            Assert.IsTrue(run.TendFire(FireChoice.WhetSoul));
            CollectionAssert.Contains(ClassUpgrades.For(Peasant.ClassId), run.FireUpgrade);
            CollectionAssert.AreEqual(new[] { run.FireUpgrade }, run.Kit.Upgrades);
            Assert.AreEqual(1100, run.BarAfterGate(run.Purse.Coins), "the gate tells it");
            run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(1100, run.BossBarStart, "10% longer");

            ChapterRun full = Run();
            foreach (string id in ClassUpgrades.For(Peasant.ClassId)) full.Kit.AddUpgrade(id);
            Stand(full, NodeKind.PurgatoryFire);
            Assert.IsFalse(full.CanTend(FireChoice.WhetSoul), "nothing left to give");
            Assert.IsFalse(full.TendFire(FireChoice.WhetSoul));
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void TheSave_KeepsTheUpgrades_TheLootsUpgrade_AndAWhettedSoul()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, 12);
            journey.Kit.AddUpgrade(UpgradeIds.CheapSeizure);
            journey.Effects.AddRelic(ChapterRelicIds.BlackBook);
            journey.Effects.AddRelic(ChapterRelicIds.SilverBell);
            journey.Effects.AddRelic(RelicIds.BoneDie);
            BeatTheDemon(journey);
            ChapterSave save = journey.Capture();
            save.Whetted = true;
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            CollectionAssert.AreEqual(new[] { UpgradeIds.CheapSeizure }, read.Upgrades);
            Assert.AreEqual(journey.Run.LootUpgrade, read.LootUpgrade);
            Assert.IsTrue(read.Whetted);

            ChapterJourney back = ChapterJourney.Restore(read);
            CollectionAssert.AreEqual(new[] { UpgradeIds.CheapSeizure }, back.Kit.Upgrades);
            Assert.AreEqual(3, back.Effects.Relics.Count, "three relics come back");
            Assert.AreEqual(journey.Run.LootUpgrade, back.Run.LootUpgrade);
            Assert.IsTrue(back.Run.WhettedSoul);

            Assert.IsTrue(ChapterSave.TryDecode(save.Encode().Replace("kit.upgrades=cheap_seizure", "kit.upgrades=nonsense,wide_satchel"), out ChapterSave odd));
            CollectionAssert.AreEqual(new[] { UpgradeIds.WideSatchel }, odd.Upgrades, "the unknown goes");
            Assert.IsEmpty(ChapterJourney.Restore(odd).Kit.Upgrades, "another class's goes too");
        }
    }
}
