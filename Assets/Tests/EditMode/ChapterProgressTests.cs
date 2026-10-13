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
            IRandomSource dice = null, int charge = 0)
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Summoner, charge));
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
        public void TheEye_TurnsADemonsCard_ForFiveCoins_OnceAHand()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
            int open = game.HouseCardsRevealed;
            Assert.IsTrue(imps.Eye(game));
            Assert.AreEqual(open + 1, game.HouseCardsRevealed);
            Assert.AreEqual(95, purse.Coins);
            Assert.AreEqual(KitRefusal.AlreadyUsed, imps.WhyNot(game, SummonAct.Eye));
        }

        [Test]
        public void TheHand_TradesACardForTheDemonsBestOpenOne_ForFifteen()
        {
            var (game, imps, purse) = AtTheDraw("KH KD 7C 4S 2D  KC 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
            Assert.IsTrue(imps.Eye(game), "an open card to take");
            Card? taken = imps.Swap(game, 4);
            Assert.AreEqual(C("KC"), taken, "the king makes three (the dead man's aces would be beyond an imp)");
            Assert.AreEqual(HandCategory.ThreeOfAKind, Evaluator.Evaluate(game.PlayerHand).Category);
            CollectionAssert.Contains(game.HouseHand, C("2D"));
            Assert.AreEqual(100 - 5 - 15, purse.Coins);
        }

        [Test]
        public void TheWhisper_TellsTheTruth_AndTheImpsNeverTakeTheLastCoin()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", coins: 8);
            Assert.AreEqual(KitRefusal.NoTaxes, imps.WhyNot(game, SummonAct.Whisper), "8 coins do not pay an 8-coin imp");
            purse.Add(1);
            Assert.IsTrue(imps.Whisper(game));
            Assert.IsTrue(imps.Whispered);
            Assert.AreEqual(game.PendingCheatTruth, imps.WhisperedCheat);
            Assert.AreEqual(1, purse.Coins);
        }

        [Test]
        public void AtADemonsTable_TheImpsGrowTheBar_AsTheHandIsSettled_AndTheLegionMakesThemFree()
        {
            var (game, imps, purse) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", barStart: 1000);
            Assert.AreEqual(20, imps.PriceOf(SummonAct.Eye));
            Assert.AreEqual(50, imps.PriceOf(SummonAct.Hand));
            Assert.AreEqual(30, imps.PriceOf(SummonAct.Whisper));
            imps.Eye(game);
            imps.Whisper(game);
            Assert.AreEqual(100, purse.Coins, "no coins at a demon's table");
            Assert.AreEqual(50, imps.YearsAfterHand(game, null, false, false), "the bar grows 2% + 3% of its start");

            var (game2, legion, _) = AtTheDraw("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", barStart: 1000, charge: 5);
            Assert.IsTrue(legion.CallLegion(game2));
            Assert.AreEqual(0, legion.PriceOf(SummonAct.Hand));
            legion.Eye(game2);
            Assert.AreEqual(0, legion.YearsAfterHand(game2, null, false, false));
            Assert.AreEqual(KitRefusal.AlreadyUsed, legion.WhyNoLegion(game2));
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

        // ------------------------------------------------------------------ the circles of Hell

        [Test]
        public void TheCircles_AddUp_ImpsTributeBarsMarketFireMaliceAndTheMirror()
        {
            ChapterRules c0 = ChapterRules.For(1), c2 = c0.WithCircle(2), c4 = c0.WithCircle(4);
            Assert.AreEqual(c0.ImpCoins * 125 / 100, c2.ImpCoins);
            Assert.AreEqual(c0.FirstImpCoins * 125 / 100, c2.FirstImpCoins);
            Assert.AreEqual(c0.TributeOverStart + 20, c2.TributeOverStart);
            Assert.AreEqual(c0.PricePercent, c2.PricePercent, "the market only from the fourth");
            Assert.AreEqual(c0.PricePercent * 125 / 100, c4.PricePercent);
            Assert.AreEqual(0, c4.WithCircle(0).Circle);

            ChapterJourney three = ChapterJourney.Begin(SinnerRoster.Peasant, 9, circle: 3);
            three.Run.Purse.Add(10000);
            three.Run.PayTribute();
            three.Run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(1300 * 115 / 100, three.Run.BossBarStart, "the bars +15%");

            var effects = new RunEffects { MaxCarried = ChapterRelics.Slots };
            var fourth = new ChapterRun(ChapterRules.For(1).WithCircle(4), new Sinner(SinnerRoster.Peasant), effects, 1000, 100, 7);
            Assert.AreEqual(2, fourth.FireOffers.Count, "the fire offers two");
            CollectionAssert.AreEqual(fourth.FireOffers, fourth.FireOffers, "the same two every time");
            FireChoice missing = ((FireChoice[])System.Enum.GetValues(typeof(FireChoice))).First(c => !fourth.FireOffers.Contains(c));
            Assert.IsFalse(fourth.CanTend(missing));

            ChapterJourney five = ChapterJourney.Begin(SinnerRoster.Peasant, 9, circle: 5);
            five.Run.Purse.Add(10000);
            five.Run.PayTribute();
            HellPokerGame boss = five.Run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(DealerRoster.Mammon.MaliceMax - 1, boss.MaliceMax, "the gauge one shorter: every cheat a hand sooner");
            Assert.AreEqual(5, five.Capture().Circle);
        }

        [Test]
        public void EveryCircle_TakesFivePercentOffAWinAgainstADemon()
        {
            Assert.AreEqual(5, Circles.WinStepPercent);
            Assert.AreEqual(5, ChapterTuning.CircleWinStepPercent, "the game's value");
            Assert.AreEqual(100, Circles.WinPercentAt(0, 100));
            Assert.AreEqual(95, Circles.WinPercentAt(1, 100));
            Assert.AreEqual(90, Circles.WinPercentAt(2, 100));
            Assert.AreEqual(100 * 100 / 115 * 85 / 100, Circles.WinPercentAt(3, 100), "the thick bar and the circle's cut");
            Assert.AreEqual(100 * 100 / 115 * 75 / 100, Circles.WinPercentAt(5, 100));
            StringAssert.Contains("15%", Presentation.Ui.UiText.CircleNote(3), "the class choice says it");
            StringAssert.Contains("5%", Presentation.Ui.UiText.GameRulesPage(), "How to Play says it");
            StringAssert.DoesNotContain("{CIRCLES}", Presentation.Ui.UiText.GameRulesPage());
        }

        [Test]
        public void TheFifthCircle_LuciferCopiesTwoMechanics()
        {
            Assert.AreEqual(KitKind.Satchel, LuciferMirror.SecondFor(KitKind.Treasury));
            Assert.AreEqual(KitKind.Prophecy, LuciferMirror.SecondFor(KitKind.Satchel));
            var mirror = (LuciferMirror)DemonRule.For(DealerRoster.LuciferId, 1000, new ClassKit(new Sinner(SinnerRoster.King)), new Always(0), mirrorsTwo: true);
            Assert.IsTrue(mirror.Copies(KitKind.Treasury));
            Assert.IsTrue(mirror.Copies(KitKind.Satchel));
            Assert.IsFalse(mirror.Copies(KitKind.Prophecy));
            var single = (LuciferMirror)DemonRule.For(DealerRoster.LuciferId, 1000, new ClassKit(new Sinner(SinnerRoster.King)), new Always(0));
            Assert.IsFalse(single.Copies(KitKind.Satchel));
        }

        // ------------------------------------------------------------------ the records, the locks

        [Test]
        public void ACircleOpens_WithAFreedomInTheDeepestOpen_ClassByClass()
        {
            var book = new ChapterRecords();
            Assert.AreEqual(0, book.CirclesOpen(Peasant.ClassId));
            book.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 80, circle: 0);
            Assert.AreEqual(1, book.CirclesOpen(Peasant.ClassId));
            Assert.AreEqual(0, book.CirclesOpen(King.ClassId));
            book.RunEnded(JourneyEnd.CastDown, 4, Peasant.ClassId, 80, circle: 1);
            Assert.AreEqual(1, book.CirclesOpen(Peasant.ClassId), "a fall opens nothing");
            for (int c = 1; c <= 6; c++) book.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 80, circle: c);
            Assert.AreEqual(Circles.Deepest, book.CirclesOpen(Peasant.ClassId));
            ChapterRecords read = ChapterRecords.Decode(book.Encode());
            Assert.AreEqual(Circles.Deepest, read.BestCircle(Peasant.ClassId));
        }

        [Test]
        public void TheLocks_FourteenRelicsAndFourClassesOpen_TheMilestonesOpenTheRest()
        {
            var book = new ChapterRecords();
            Assert.AreEqual(14, ChapterUnlocks.OpenRelics(book).Count());
            Assert.AreEqual(10, ChapterUnlocks.Relics.Count);
            Assert.IsFalse(ChapterUnlocks.IsClassOpen(Summoner.ClassId, book));
            Assert.IsTrue(SinnerRoster.All.All(c => ChapterUnlocks.IsClassOpen(c.Id, book)));

            ChapterRecords before = ChapterRecords.Decode(book.Encode());
            book.WardenBeaten();
            book.TributePaidInFull();
            book.BossBeaten(DealerRoster.BelialId);
            book.RunEnded(JourneyEnd.Freed, 4, King.ClassId, 120);
            CollectionAssert.IsSubsetOf(new[] { Summoner.ClassId, ChapterRelicIds.SaltCircle, ChapterRelicIds.SilverBell, ChapterRelicIds.RavenFeather,
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
