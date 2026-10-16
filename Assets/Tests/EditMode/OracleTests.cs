using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.2: the Warlock became the Oracle (FORESIGHT, the Mirror Curse); the id stays. Demo 2.3: BEND FATE and its arrows are
    /// gone — FORESIGHT only shows, the passive sees the truth.</summary>
    public class OracleTests
    {
        [TearDown]
        public void English() => Lang.Set(Language.English);

        private static (TablePresenter presenter, FakeTableView view, ChapterJourney journey) AtTheBoss(int seed = 77)
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Warlock, seed);
            HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
            presenter.SitAtBoss(boss, journey.Run, journey.Run.BossBarStart);
            return (presenter, view, journey);
        }

        [Test]
        public void Foresight_ShowsTheDecksTop_NothingMovesIt_AndKOnlyTellsThePassive()
        {
            var (presenter, view, journey) = AtTheBoss();
            Assert.AreEqual(0, view.Kit.Cards.Count, "shut before the deal");
            presenter.PerformAction();   // the deal
            HellPokerGame game = (HellPokerGame)presenter.Game;
            Assert.AreEqual(ClassKit.ForesightStart, view.Kit.Cards.Count, "Demo 2.3: FORESIGHT starts with one card");
            Card top = game.Deck.Remaining[game.Deck.Remaining.Count - 1];
            Assert.AreEqual(top, view.Kit.Cards[0].Card, "the deck's top card");

            presenter.PressKitCard(0);
            Assert.AreEqual(top, game.Deck.Remaining[game.Deck.Remaining.Count - 1], "a click on the card moves nothing");
            presenter.UsePower();
            Assert.AreEqual(UiText.ClassPassiveInfo(Warlock.ClassId, journey.Kit), view.Message, "K: what the class does, nothing pressed");
            StringAssert.Contains("SEES THE TRUTH", view.Message);
            presenter.Dispose();
        }

        [Test]
        public void TheFateWeaver_ShowsTheCardTheDemonKnows()
        {
            var (presenter, view, journey) = AtTheBoss(78);
            journey.Kit.AddUpgrade(UpgradeIds.Thrice);
            presenter.PerformAction();
            Card? known = presenter.Kit.Prophecy.KnownCard;
            Assert.IsTrue(known.HasValue, "the Mirror Curse: the demon knows a card every hand");
            StringAssert.Contains(string.Format(UiText.DemonKnowsFormat, known.Value), view.Kit.Title);
            presenter.Dispose();
        }

        [Test]
        public void TheOracle_IsNamedSo_InBothLanguages_TheSimulatorsPowerUntouched()
        {
            Assert.AreEqual("ORACLE", UiText.SinnerName(Warlock.ClassId));
            StringAssert.Contains("FORESIGHT", UiText.ChapterSinnerAbility(Warlock.ClassId));
            StringAssert.Contains("MIRROR CURSE", UiText.ChapterSinnerDetail(Warlock.ClassId));
            StringAssert.Contains("ward", UiText.SinnerAbility(Warlock.ClassId), "the Boss Simulator's Warlock keeps his ward");
            Lang.Set(Language.Turkish);
            Assert.AreEqual("KÂHİN", UiText.SinnerName(Warlock.ClassId));
            StringAssert.Contains("ÖNSEZİ", UiText.ChapterSinnerAbility(Warlock.ClassId));
            Assert.AreEqual("KÖR FALCI", UiText.OfferTitle(ChoiceEventIds.BlindSeer), "the Blind Seer is no Oracle");
            Assert.AreEqual("ÜÇÜNCÜ GÖZ", UiText.UpgradeName(UpgradeIds.DeepProphecy));
            Assert.AreEqual("KADER DOKUMACISI", UiText.UpgradeName(UpgradeIds.Thrice));
            Assert.AreEqual("KEHANET MÜHRÜ", UiText.UpgradeName(UpgradeIds.EyeOfTruth));
        }

        [Test]
        public void TheLanguageChange_RewritesThePanel()
        {
            var (presenter, view, _) = AtTheBoss();
            presenter.PerformAction();
            Assert.AreEqual("FORESIGHT", view.Kit.Title);
            Lang.Set(Language.Turkish);
            Assert.AreEqual("ÖNSEZİ", view.Kit.Title);
            presenter.Dispose();
        }
    }
}
