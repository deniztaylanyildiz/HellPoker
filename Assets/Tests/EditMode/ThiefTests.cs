using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.2's sixth class, the Thief: the glance, the blind steal, being caught and fined, his upgrades, Lucifer's mirror,
    /// and every class open from the start.</summary>
    public class ThiefTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();
        private static Card C(string token) => TestCards.Card(token);

        private sealed class Always : IRandomSource
        {
            private readonly int _value;
            public Always(int value) { _value = value; }
            public int Next(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        private static HellPokerGame Stacked(string cards, Sinner sinner)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private const string Cards = "2C 7D 9S 4H 3D  AS KD QC JH 5C  6H 10C JS 5D 2S 3S 4S";

        [Test]
        public void TheThief_IsPhaseTwos_OpenFromTheStart_WithHisPurseAndKit()
        {
            Assert.AreSame(SinnerRoster.Thief, SinnerRoster.Find(Thief.ClassId));
            CollectionAssert.DoesNotContain(SinnerRoster.All.Select(c => c.Id), Thief.ClassId, "the Boss Simulator never offers him");
            Assert.AreEqual(KitKind.Theft, ClassKit.KindOf(SinnerRoster.Thief));
            Assert.AreEqual(140, ChapterRules.StartingCoinsFor(Thief.ClassId));
            Assert.AreEqual(4, ClassUpgrades.For(Thief.ClassId).Count);
            foreach (SinnerClass c in SinnerRoster.PhaseTwo)
                Assert.IsTrue(ChapterUnlocks.IsClassOpen(c.Id, new ChapterRecords()), $"{c.Id} open from the start");
            // One switch brings the old lock back: the Summoner waits for the first freedom.
            ChapterUnlocks.AllClassesOpen = false;
            try
            {
                Assert.IsFalse(ChapterUnlocks.IsClassOpen(Summoner.ClassId, new ChapterRecords()));
                Assert.IsTrue(ChapterUnlocks.IsClassOpen(Thief.ClassId, new ChapterRecords()), "only the Summoner was ever locked");
                var freed = new ChapterRecords();
                freed.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 30);
                Assert.IsTrue(ChapterUnlocks.IsClassOpen(Summoner.ClassId, freed), "the first freedom opens him");
            }
            finally
            {
                ChapterUnlocks.AllClassesOpen = true;
            }
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Thief, 3);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            Assert.AreEqual(Thief.ClassId, ChapterJourney.Restore(read).Sinner.Id);
            Assert.AreEqual("THIEF", UiText.SinnerName(Thief.ClassId));
        }

        [Test]
        public void TheGlance_OpensOneOfTheDemonsCards_TheNightEyeTwo()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            HellPokerGame game = Stacked(Cards, kit.Sinner);
            ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            Assert.GreaterOrEqual(game.HouseCardsRevealed, 1);
            kit.AddUpgrade(UpgradeIds.NightEye);
            HellPokerGame night = Stacked(Cards, kit.Sinner);
            ChapterHooks.Attach(night, kit, null, 0, 1);
            night.PlaceBet();
            Assert.GreaterOrEqual(night.HouseCardsRevealed, 2);
        }

        private static (HellPokerGame game, TheftPowers theft, CoinPurse purse) AtTheDraw(int dice, int barStart = 0, ClassKit kit = null)
        {
            kit = kit ?? new ClassKit(new Sinner(SinnerRoster.Thief));
            HellPokerGame game = Stacked(Cards, kit.Sinner);
            var theft = new TheftPowers(kit, new Always(dice), barStart) { Purse = new CoinPurse(100) };
            game.UseHooks(theft);
            game.PlaceBet();
            game.CheckToDraw();
            return (game, theft, theft.Purse);
        }

        [Test]
        public void TheSteal_TakesOneOfTheDemonsFaceDownCards_ForOneOfHis_OnceAHand()
        {
            var (game, theft, purse) = AtTheDraw(99);   // never caught
            Hand mine = game.PlayerHand, his = game.HouseHand;
            int open = game.HouseCardsRevealed;
            Assert.AreEqual(KitRefusal.None, theft.WhyNoSteal(game));
            Assert.IsTrue(theft.Steal(game, 0));
            Assert.IsFalse(theft.CaughtThisHand);
            Assert.AreEqual(mine[0], theft.Given);
            Assert.AreEqual(theft.Taken, game.PlayerHand[0]);
            int at = Enumerable.Range(0, Hand.Size).Single(i => game.HouseHand[i].Equals(mine[0]));
            Assert.GreaterOrEqual(at, open, "a face-down card of his");
            Assert.AreEqual(his[at], theft.Taken);
            Assert.AreEqual(KitRefusal.AlreadyUsed, theft.WhyNoSteal(game), "once a hand");
            Assert.AreEqual(100, purse.Coins, "no fine");
            var all = game.PlayerHand.Concat(game.HouseHand).ToList();
            Assert.AreEqual(10, all.Distinct().Count(), "no card in two places");
        }

        [Test]
        public void Caught_NothingChangesHands_AndTheFineIsPaid_CoinsOnAFloor_BarAtADemons()
        {
            var (game, theft, purse) = AtTheDraw(0);   // always caught
            Hand mine = game.PlayerHand;
            Assert.IsTrue(theft.Steal(game, 0));
            Assert.IsTrue(theft.CaughtThisHand);
            CollectionAssert.AreEqual(mine, game.PlayerHand, "nothing changed hands");
            Assert.AreEqual(System.Math.Min(game.Ante, 99), theft.Fine, "an ante's coins, never the purse's last");
            Assert.AreEqual(100 - theft.Fine, purse.Coins);

            var (boss, bossTheft, _) = AtTheDraw(0, barStart: 1000);
            Assert.IsTrue(bossTheft.Steal(boss, 0));
            Assert.AreEqual(1000 * TheftPowers.FineBarPercent / 100, bossTheft.YearsAfterHand(boss, null, false, false), "3% of the bar");
        }

        [Test]
        public void LightFingers_ALevelDown_TheLootSack_Coins()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            Assert.AreEqual(ClassKit.CaughtStart, kit.ThiefCaughtPercent, "Demo 2.3: caught 40% at the start");
            kit.AddUpgrade(UpgradeIds.LightFingers);
            Assert.AreEqual(ClassKit.CaughtStart - ClassKit.CaughtStep, kit.ThiefCaughtPercent, "Light Fingers: a level");
            kit.AddUpgrade(UpgradeIds.LootSack);
            var (game, theft, purse) = AtTheDraw(ClassKit.CaughtStart - ClassKit.CaughtStep, kit: kit);   // caught at the start, not a level up
            Assert.IsTrue(theft.Steal(game, 0));
            Assert.IsFalse(theft.CaughtThisHand);
            Assert.AreEqual(100 + TheftPowers.LootCoins, purse.Coins);
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(ChapterRelics.ClassChain(Thief.ClassId));
            Assert.AreEqual(ClassKit.CaughtStart - ClassKit.MaxLevel * ClassKit.CaughtStep, kit.ThiefCaughtPercent, "at most the highest level");
        }

        /// <summary>A hook that answers every raise with a re-raise and keeps the demon's hand as he decided on it.</summary>
        private sealed class ReRaiser : TableHooks
        {
            public Hand Seen;
            public override bool HouseReRaises(HellPokerGame game, bool wants)
            {
                Seen = game.HouseHand;
                return true;
            }
        }

        [Test]
        public void TheSteal_AfterTheDraw_AtTheBetAndAtTheReRaise_TheDemonPlaysAndIsJudgedOnHisNewHand_RefusedOnceItIsOver()
        {
            // After the draw, at the bet.
            var (game, theft, _) = AtTheDraw(99);
            game.Draw(new int[0]);
            Assert.AreEqual(GamePhase.DrawReveal, game.Phase);
            Assert.AreEqual(KitRefusal.None, theft.WhyNoSteal(game), "Demo 2.3: after the draw too");
            Card given = game.PlayerHand[0];
            Assert.IsTrue(theft.Steal(game, 0));
            CollectionAssert.Contains(game.HouseHand, given);
            Assert.AreEqual(1, theft.Kit.StealsAfterDraw);
            while (game.Phase != GamePhase.RoundOver && !game.IsGameOver) game.Bet(BetAction.Pass);
            Assert.AreEqual(Evaluator.Evaluate(game.HouseHand).Category, game.LastRound.Showdown.House.Category, "judged on his hand with the stolen-for card");
            CollectionAssert.Contains(game.HouseHand, given);
            Assert.AreEqual(KitRefusal.NotNow, theft.WhyNoSteal(game), "the showdown is over: no steal");

            // At the re-raise: the demon decided on his hand before it; the steal changes what he is judged on.
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            HellPokerGame raised = Stacked(Cards, kit.Sinner);
            var steal = new TheftPowers(kit, new Always(99), 0) { Purse = new CoinPurse(100) };
            var reRaiser = new ReRaiser();
            raised.UseHooks(new HookChain(new TableHooks[] { steal, reRaiser }));
            raised.PlaceBet();
            raised.CheckToDraw();
            raised.Draw(new int[0]);
            raised.Bet(BetAction.Raise);
            Assert.AreEqual(GamePhase.HouseReRaise, raised.Phase);
            Assert.AreEqual(KitRefusal.None, steal.WhyNoSteal(raised), "a re-raise to answer: still in the hand");
            Card mine = raised.PlayerHand[1];
            Assert.IsTrue(steal.Steal(raised, 1));
            Assert.AreEqual(KitRefusal.AlreadyUsed, steal.WhyNoSteal(raised), "once a hand");
            raised.Bet(BetAction.Call);
            while (raised.Phase != GamePhase.RoundOver && !raised.IsGameOver)
            {
                if (raised.Phase == GamePhase.HouseReRaise) raised.Bet(BetAction.Call);
                else raised.Bet(BetAction.Pass);
            }
            CollectionAssert.Contains(raised.HouseHand, mine);
            Assert.AreEqual(Evaluator.Evaluate(raised.HouseHand).Category, raised.LastRound.Showdown.House.Category);
        }

        [Test]
        public void TheSteal_BeforeTheDeal_IsRefused()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            HellPokerGame game = Stacked(Cards, kit.Sinner);
            var theft = new TheftPowers(kit, new Always(99), 0);
            game.UseHooks(theft);
            Assert.AreEqual(KitRefusal.NotNow, theft.WhyNoSteal(game));
        }

        [Test]
        public void TheMasterThief_TakesTheBestOpenCard()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            kit.AddUpgrade(UpgradeIds.MasterThief);
            HellPokerGame game = Stacked("2C 7D 9S 4H 3D  2S KD QC JH 5C  6H 10C JS 5D 2H 3S 4S", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 99);   // the glance opens his first card, the 2S
            game.PlaceBet();
            game.CheckToDraw();
            Assert.IsTrue(game.HouseCardsRevealed >= 1);
            TheftPowers theft = hooks.Theft;
            for (int tries = 0; tries < 20 && !theft.Steal(game, 1); tries++) { }
            if (theft.CaughtThisHand) Assert.Pass("caught this time: nothing to look at");
            Assert.AreEqual(C("2S"), game.PlayerHand[1], "the open two pairs his two");
        }

        [Test]
        public void LucifersMirror_StealsEveryThirdHand()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Thief));
            var mirror = new LuciferMirror(1000, new Always(0), kit);
            StringAssert.Contains("steal", UiText.DemonRuleLine(mirror));
            int stolen = 0;
            for (int hand = 0; hand < 3; hand++)
            {
                HellPokerGame game = Stacked("AS AD 9S 4H 3D  AC 7C 8D 2H 5C  6H 10C JS 5D 2S 3S 4S", kit.Sinner);
                game.UseHooks(mirror);
                game.PlaceBet();
                game.CheckToDraw();
                if (mirror.Seized.HasValue) stolen++;
            }
            Assert.AreEqual(1, stolen, "the third hand");
        }

        [Test]
        public void AtTheTable_KOpensThePick_TheCardClickedIsGiven()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Thief, 77);
            HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
            presenter.SitAtBoss(boss, journey.Run, journey.Run.BossBarStart);
            Assert.AreEqual(UiText.KitThiefTitle, view.Kit.Title);
            presenter.PerformAction();   // the deal
            presenter.UsePower();
            Assert.AreEqual(UiText.StealPrompt, view.Message);
            Card given = boss.PlayerHand[0];
            presenter.ToggleDiscard(0);
            Assert.IsTrue(presenter.Kit.Theft.TriedThisHand);
            if (presenter.Kit.Theft.CaughtThisHand) StringAssert.StartsWith(UiText.CaughtBarFormat.Substring(0, 6), view.Message);
            else Assert.AreEqual(string.Format(UiText.StolenFormat, presenter.Kit.Theft.Taken.Value, given), view.Message);
            presenter.Dispose();
        }
    }
}
