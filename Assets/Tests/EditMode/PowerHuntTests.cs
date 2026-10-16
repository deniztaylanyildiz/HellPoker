using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Demo 2.2's hunt through the class powers: each finding pinned by a test (the Summoner's paid imp after the Legion, a seizure's
    /// pick that outlived the draw, Lucifer's mirror told wrongly, a chapter's tables writing nothing to the run log).
    /// </summary>
    public class PowerHuntTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        [TearDown]
        public void English() => Lang.Set(Language.English);

        private static HellPokerGame Stacked(string cards, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private sealed class Always : IRandomSource
        {
            private readonly int _value;
            public Always(int value) { _value = value; }
            public int Next(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        [Test]
        public void TheSummoner_AfterTheLegionsFreeImp_PaysForTheNext_AndIsRefusedWithoutTheCoins()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Summoner));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            var imps = new SummonPowers(kit, new Always(99), 0) { Purse = new CoinPurse(6) };
            game.UseHooks(imps);
            game.PlaceBet();
            Assert.IsTrue(imps.CallLegion(game));
            Assert.IsTrue(imps.Eye(game), "the Legion's free imp");
            Assert.AreEqual(6, imps.Purse.Coins);
            Assert.AreEqual(SummonPowers.CoinPrices[(int)SummonAct.Whisper], imps.PriceOf(SummonAct.Whisper), "the Legion's one free imp is spent");
            Assert.AreEqual(KitRefusal.NoTaxes, imps.WhyNot(game, SummonAct.Whisper), "eight coins with six in the purse: refused");
            Assert.IsFalse(imps.Whisper(game));
            Assert.AreEqual(6, imps.Purse.Coins);
        }

        [Test]
        public void ASeizuresPick_ClosesWithTheDraw()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, 31);
            journey.Kit.AddTax(10);
            HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
            presenter.SitAtBoss(boss, journey.Run, journey.Run.BossBarStart);
            presenter.PerformAction();   // the deal
            presenter.PressKitButton(2);   // SEIZE: the pick of the card to give opens
            Assert.AreEqual(UiText.SeizePrompt, view.Message);
            Assert.IsTrue(view.Kit.Waiting);
            if (boss.Phase == GamePhase.PlayerReveal) presenter.CheckToDraw();
            Assert.AreEqual(GamePhase.Drawing, boss.Phase);
            presenter.PerformAction();   // DRAW, no card given
            Assert.IsTrue(boss.IsAfterDraw || !boss.HandInPlay);
            Assert.IsFalse(view.Kit.Waiting, "the pick closed with the draw");
            presenter.ToggleDiscard(0);
            Assert.AreNotEqual(UiText.SeizePrompt, view.Message, "a card clicked after the draw is no seizure");
            presenter.Dispose();
        }

        private sealed class OnlyCheat : HellPoker.Core.Cheats.ICheatPolicy
        {
            private readonly HellPoker.Core.Cheats.ICheat _cheat;
            public OnlyCheat(HellPoker.Core.Cheats.ICheat cheat) { _cheat = cheat; }
            public System.Collections.Generic.IReadOnlyList<HellPoker.Core.Cheats.ICheat> Cheats => new[] { _cheat };
            public HellPoker.Core.Cheats.CheatPick Choose(HellPoker.Core.Cheats.CheatContext context, IRandomSource random) =>
                new HellPoker.Core.Cheats.CheatPick(_cheat);
            public HellPoker.Core.Cheats.ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        [Test]
        public void TheSatchel_TheCollateralChain_AndTheDraw_EachCardLandsWhereItShould()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            kit.Keep(TestCards.Card("AS"));
            kit.Keep(TestCards.Card("AD"));
            var random = new Always(0);
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            var game = new HellPokerGame(rules, TestDecks.Stacked("7C 8D 2H 3S 9C  QS 9H 8S 5C 3H  6H 10C JH 5D 2C 3D 4H"), Evaluator,
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), null,
                new HellPoker.Core.Cheats.CheatSession(new OnlyCheat(new HellPoker.Core.Cheats.CollateralCheat()), 1, random, null, 0), random, kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            // The demon's gauge fills over a hand or two (folded): the chain comes as the draw begins.
            for (int hand = 0; hand < 4; hand++)
            {
                game.PlaceBet();
                game.CheckToDraw();
                if (Enumerable.Range(0, Hand.Size).Any(game.IsPlayerCardChained)) break;
                game.Draw(new int[0]);
                while (game.Phase != GamePhase.RoundOver && !game.IsGameOver)
                    game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
                game.NextRound();
            }
            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            int chained = Enumerable.Range(0, Hand.Size).Single(game.IsPlayerCardChained);
            Hand before = game.PlayerHand;
            int[] throwing = Enumerable.Range(0, Hand.Size).Where(i => i != chained).OrderBy(i => before[i].Rank).Take(2).OrderBy(i => i).ToArray();
            Card top = game.Deck.Remaining.Last();   // the deck draws from its end
            var fill = new System.Collections.Generic.Dictionary<int, Card> { [throwing[0]] = TestCards.Card("AS") };
            Assert.IsTrue(game.CanDraw(throwing, fill, out string why), why);
            game.Draw(throwing, fill);

            Hand after = game.PlayerHand;
            Assert.AreEqual(before[chained], after[chained], "the chained card stays where it was");
            Assert.AreEqual(TestCards.Card("AS"), after[throwing[0]], "the satchel's card in its place");
            Assert.AreEqual(top, after[throwing[1]], "the deck's top card in the other");
            foreach (int i in Enumerable.Range(0, Hand.Size).Where(i => !throwing.Contains(i)))
                Assert.AreEqual(before[i], after[i], $"card {i} untouched");
            CollectionAssert.AreEqual(new[] { TestCards.Card("AD") }, kit.Satchel, "the ace taken is out of the satchel");
            var everywhere = after.Concat(game.HouseHand).Concat(game.Deck.Remaining).Concat(kit.Satchel).ToList();
            Assert.AreEqual(everywhere.Count, everywhere.Distinct().Count(), "no card in two places");
            Assert.IsTrue(hooks.Satchel.SeenThisHand, "the demon saw the satchel opened");
        }

        [Test]
        public void LucifersMirror_AgainstTheSummoner_SpeaksOfHisEye_NotOfJokers()
        {
            var summoner = new ClassKit(new Sinner(SinnerRoster.Summoner));
            var mirror = new LuciferMirror(1000, new Always(0), summoner);
            string line = UiText.DemonRuleLine(mirror);
            StringAssert.Contains("Eye", line);
            StringAssert.DoesNotContain("jokers", line);
            var two = new LuciferMirror(1000, new Always(0), summoner, LuciferMirror.SecondFor(KitKind.Summon));
            StringAssert.Contains("satchel", UiText.DemonRuleLine(two), "the fifth circle tells its second copy");
            Lang.Set(Language.Turkish);
            StringAssert.Contains("Göz", UiText.DemonRuleLine(mirror));
        }

        [Test]
        public void LucifersMirror_SaysWhatItDid_TheEye_TheSatchel_TheTopCard()
        {
            var summoner = new ClassKit(new Sinner(SinnerRoster.Summoner));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", summoner.Sinner);
            var mirror = new LuciferMirror(1000, new Always(0), summoner);
            game.UseHooks(mirror);
            for (int hand = 1; hand <= LuciferMirror.EyeEveryHands; hand++)
            {
                if (hand > 1)
                {
                    game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", summoner.Sinner);
                    game.UseHooks(mirror);
                }
                game.PlaceBet();
            }
            Assert.IsTrue(mirror.EyeSaw.HasValue, "the third hand: his eye");
            Assert.IsFalse(mirror.DrewTop || mirror.UsedBag || mirror.SentDown, "nothing else: no 'top card' line");

            // Against the Oracle, standing pat on a full house: the ace on top makes it four, he draws it.
            var oracle = new ClassKit(new Sinner(SinnerRoster.Warlock));
            HellPokerGame pat = Stacked("2C 7D 9S 4H 3D  AS AD AC KS KD  6S AH 8H 9C", oracle.Sinner);
            var seer = new LuciferMirror(1000, new Always(0), oracle);
            pat.UseHooks(seer);
            pat.PlaceBet();
            pat.CheckToDraw();
            pat.Draw(new[] { 0 });
            Assert.IsTrue(seer.DrewTop, "he drew the top card");
            Assert.IsFalse(seer.UsedBag || seer.SentDown || seer.EyeSaw.HasValue);
            Assert.AreEqual(HandCategory.FourOfAKind, Evaluator.Evaluate(pat.HouseHand).Category);
        }

        [Test]
        public void AChaptersTable_WritesItsHandsAndTheRefusedK_IntoTheRunLog()
        {
            var map = new FakeChapterMapView();
            var panel = new FakeChapterPanelView();
            var view = new FakeTableView();
            var table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            var chapters = new ChapterPresenter(map, panel, () => (table, view), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242);
            try
            {
                chapters.Start(SinnerRoster.Warlock);
                panel.Press(0);
                map.Press(map.State.Choices.FirstOrDefault(n => n.Kind == NodeKind.Table) ?? map.State.Choices.First());
                Assert.IsTrue(table.AtChapterTable);
                table.UsePower();   // K before the deal: refused
                int guard = 0;
                while (table.AtChapterTable && table.Game.RoundNumber < 2 && guard++ < 50) table.PerformAction();
                string[] lines = chapters.Feedback.LogLines.ToArray();
                Assert.IsTrue(lines.Any(l => l.StartsWith("#1 ")), string.Join("\n", lines));
                Assert.IsTrue(lines.Any(l => l.Contains("K refused")), string.Join("\n", lines));
            }
            finally
            {
                chapters.Dispose();
            }
        }
    }
}
