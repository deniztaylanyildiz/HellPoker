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
    /// <summary>Phase 2's progression: the Summoner and his imps, the circles of Hell, the locks and their milestones.</summary>
    public class ChapterProgressTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        private sealed class Always : IRandomSource
        {
            private readonly int _value;
            public Always(int value) { _value = value; }
            public int Next(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        private static HellPokerGame Stacked(string cards, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private static Card C(string token) => TestCards.Card(token);

        // ------------------------------------------------------------------ the Summoner

        [Test]
        public void TheSummoner_IsPhaseTwosOnly_WithHisPurseAndSentence()
        {
            CollectionAssert.DoesNotContain(SinnerRoster.All.Select(c => c.Id), Summoner.ClassId, "the Boss Simulator never offers him");
            CollectionAssert.Contains(SinnerRoster.PhaseTwo.Select(c => c.Id), Summoner.ClassId);
            Assert.AreSame(SinnerRoster.Summoner, SinnerRoster.Find(Summoner.ClassId));
            Assert.AreEqual(200, ChapterRules.StartingCoinsFor(Summoner.ClassId), "tuned from 140 by the whole-run simulation");
            Assert.AreEqual(5800, BossShares.Total(Summoner.ClassId), "tuned from 6500");
            Assert.AreEqual(KitKind.Summon, ClassKit.KindOf(SinnerRoster.Summoner));
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Summoner, 3);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            Assert.AreEqual(Summoner.ClassId, ChapterJourney.Restore(read).Sinner.Id);
        }

        /// <summary>A floor's game with the Summoner's imps on it (the purse given), dealt and at the draw.</summary>
        private static (HellPokerGame game, SummonPowers imps, CoinPurse purse) AtTheDraw(string cards, int coins = 100, int barStart = 0,
            IRandomSource dice = null, int charge = 0, int steps = 2)
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Summoner, charge));
            // Demo 2.3: the Hand and the Whisper open with the Summoner's relics — here two steps of them, with no curse.
            kit.Relics = () => new HellPoker.Core.Relics.RelicEffects(extras: HellPoker.Core.Relics.RelicEffects.Marks((HellPoker.Core.Relics.RelicExtras.ClassStep, steps)));
            HellPokerGame game = Stacked(cards, kit.Sinner);
            var imps = new SummonPowers(kit, dice ?? new Always(99), barStart);
            var purse = new CoinPurse(coins);
            imps.Purse = purse;
            game.UseHooks(imps);
            game.PlaceBet();
            game.CheckToDraw();
            return (game, imps, purse);
        }

        [Test]
        public void TheEye_TurnsADemonsCard_TheLoyalImpMakesTheFirstFree_OnceAHand()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
            int open = game.HouseCardsRevealed;
            Assert.AreEqual(0, imps.PriceOf(SummonAct.Eye), "the passive LOYAL IMP: the hand's first imp is free");
            Assert.AreEqual(5, imps.FullPriceOf(SummonAct.Eye));
            Assert.IsTrue(imps.Eye(game));
            Assert.AreEqual(open + 1, game.HouseCardsRevealed);
            Assert.AreEqual(100, purse.Coins);
            Assert.AreEqual(SummonAct.Eye, imps.FreedThisHand);
            Assert.IsTrue(imps.PassiveThisHand);
            Assert.AreEqual(KitRefusal.AlreadyUsed, imps.WhyNot(game, SummonAct.Eye));
            Assert.AreEqual(8, imps.PriceOf(SummonAct.Whisper), "only the first");
        }

        [Test]
        public void TheHand_TradesACardForTheDemonsBestOpenOne_ForFifteen()
        {
            var (game, imps, purse) = AtTheDraw("KH KD 7C 4S 2D  KC 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
            Assert.IsTrue(imps.Eye(game), "an open card to take (free)");
            Card? taken = imps.Swap(game, 4);
            Assert.AreEqual(C("KC"), taken, "the king makes three (the dead man's aces would be beyond an imp)");
            Assert.AreEqual(HandCategory.ThreeOfAKind, Evaluator.Evaluate(game.PlayerHand).Category);
            CollectionAssert.Contains(game.HouseHand, C("2D"));
            Assert.AreEqual(100 - 15, purse.Coins);
        }

        [Test]
        public void TheWhisper_TellsTheTruth_AndTheImpsNeverTakeTheLastCoin()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", coins: 8);
            Assert.IsTrue(imps.Eye(game), "the free one");
            Assert.AreEqual(KitRefusal.NoTaxes, imps.WhyNot(game, SummonAct.Whisper), "8 coins do not pay an 8-coin imp");
            purse.Add(1);
            Assert.IsTrue(imps.Whisper(game));
            Assert.IsTrue(imps.Whispered);
            Assert.AreEqual(game.PendingCheatTruth, imps.WhisperedCheat);
            Assert.AreEqual(1, purse.Coins);
        }

        [Test]
        public void AtADemonsTable_TheImpsGrowTheBar_AsTheHandIsSettled_TwoLoyalImpsFreeTwo()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", barStart: 1000);
            Assert.AreEqual(20, imps.FullPriceOf(SummonAct.Eye));
            Assert.AreEqual(50, imps.FullPriceOf(SummonAct.Hand));
            Assert.AreEqual(30, imps.FullPriceOf(SummonAct.Whisper));
            imps.Eye(game);
            imps.Whisper(game);
            Assert.AreEqual(100, purse.Coins, "no coins at a demon's table");
            Assert.AreEqual(30, imps.YearsAfterHand(game, null, false, false), "the Eye free, the Whisper's 3% of the bar's start");

            var (game2, pair, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", barStart: 1000);
            pair.Kit.AddUpgrade(UpgradeIds.LoyalPair);
            pair.HandDealt(game2);   // the upgrade counts from the next deal
            pair.Eye(game2);
            pair.Whisper(game2);
            Assert.AreEqual(0, pair.YearsAfterHand(game2, null, false, false), "Two Loyal Imps: both free");
        }

        [Test]
        public void TheHandAndTheWhisper_WaitForTheSummonersRelics_AndTheThirdStepHalvesThePrices()
        {
            var (game, imps, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", steps: 0);
            Assert.AreEqual(1, imps.Kit.ImpsOpen, "the Eye alone");
            Assert.AreEqual(KitRefusal.Locked, imps.WhyNot(game, SummonAct.Hand));
            Assert.AreEqual(KitRefusal.Locked, imps.WhyNot(game, SummonAct.Whisper));
            var (game1, one, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", steps: 1);
            Assert.IsTrue(one.IsOpen(SummonAct.Hand));
            Assert.AreEqual(KitRefusal.Locked, one.WhyNot(game1, SummonAct.Whisper));
            var (_, three, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", steps: 3);
            Assert.IsTrue(three.Kit.ImpsHalfPrice);
            Assert.AreEqual(8, three.FullPriceOf(SummonAct.Hand), "half of fifteen, rounded up");
        }

        [Test]
        public void AnImpRunningWild_LowersOneOfTheSummonersCards()
        {
            var (game, imps, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H QH", dice: new Always(0));
            Assert.IsTrue(imps.Eye(game));
            Assert.AreEqual(C("4S"), imps.WildFrom, "the only card whose lower one is still in the deck");
            Assert.AreEqual(C("3S"), imps.WildTo);
            Assert.AreEqual(C("3S"), game.PlayerHand[3]);
            CollectionAssert.DoesNotContain(game.Deck.Remaining, C("3S"), "out of the deck");
            Assert.AreEqual(1, imps.Kit.WildImps);
        }

        [Test]
        public void LucifersMirror_AgainstTheSummoner_HisEyeEveryThirdHand()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Summoner));
            var mirror = new LuciferMirror(1000, new Always(1), kit);
            int seen = 0;
            for (int hand = 1; hand <= 6; hand++)
            {
                HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
                game.UseHooks(mirror);
                game.PlaceBet();
                if (mirror.EyeSaw.HasValue) seen++;
                Assert.AreEqual(hand % 3 == 0, mirror.EyeSaw.HasValue, "hand " + hand);
            }
            Assert.AreEqual(2, seen);
        }

        // ------------------------------------------------------------------ the records, the locks

        [Test]
        public void TheLocks_FourteenRelicsAndFourClassesOpen_TheMilestonesOpenTheRest()
        {
            var book = new ChapterRecords();
            Assert.AreEqual(37, ChapterUnlocks.OpenRelics(book).Count(), "Demo 2.1: 39 relics, 15 locked; Demo 2.3: the thirteen class relics open");
            Assert.AreEqual(16, ChapterUnlocks.Relics.Count, "2.1.1: the compass is locked too");
            Assert.IsTrue(SinnerRoster.PhaseTwo.All(c => ChapterUnlocks.IsClassOpen(c.Id, book)), "Demo 2.2: every class open");

            ChapterRecords before = ChapterRecords.Decode(book.Encode());
            book.WardenBeaten();
            book.TributePaidInFull();
            book.BossBeaten(DealerRoster.BelialId);
            book.RunEnded(JourneyEnd.Freed, 4, King.ClassId, 120);
            CollectionAssert.IsSubsetOf(new[] { ChapterRelicIds.SaltCircle, ChapterRelicIds.SilverBell, ChapterRelicIds.RavenFeather,
                ChapterRelicIds.LilithsTear, ChapterRelicIds.AbyssCoin }, ChapterUnlocks.Opened(before, book));
            ChapterRecords read = ChapterRecords.Decode(book.Encode());
            Assert.AreEqual(1, read.WardensBeaten);
            Assert.AreEqual(1, read.TributesPaid);
            Assert.AreEqual(1, read.Beaten(DealerRoster.BelialId));
        }

        [Test]
        public void ALockedRelic_NeverComesFromTheMarketOrAWarden()
        {
            var book = new ChapterRecords();
            var journey = ChapterJourney.Begin(SinnerRoster.Peasant, 4);
            journey.RelicAvailable = id => ChapterUnlocks.IsRelicOpen(id, book);
            var random = new SystemRandomSource(2);
            for (int i = 0; i < 300; i++)
                foreach (string id in ChapterRelics.Draw(random, RelicWeights.Warden, 3, null, journey.Run.RelicAvailable))
                    Assert.IsTrue(ChapterUnlocks.IsRelicOpen(id, book), id);
        }
    }
}
