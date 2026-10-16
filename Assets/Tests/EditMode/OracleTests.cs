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
    /// <summary>Demo 2.2: the Warlock became the Oracle (FORESIGHT, BEND FATE with a card's arrows, the Mirror Curse); the id stays.</summary>
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
        public void TheArrows_ShowWhileFateMayBeBent_AndAnUpArrowBringsTheCardToTheTop()
        {
            var (presenter, view, journey) = AtTheBoss();
            Assert.IsFalse(view.Kit.Cards.Any(c => c.Arrows), "shut before the deal");
            presenter.PerformAction();   // the deal
            Assert.AreEqual(2, view.Kit.Cards.Count, "FORESIGHT: two cards");
            Assert.IsTrue(view.Kit.Cards.All(c => c.Arrows));
            Assert.IsFalse(view.Kit.Cards[0].CanTop, "the top card is on top already");
            Assert.IsTrue(view.Kit.Cards[1].CanTop);
            Assert.IsTrue(view.Kit.Cards.All(c => c.CanBottom));

            Card second = view.Kit.Cards[1].Card;
            presenter.PressKitArrow(0, true);
            Assert.AreEqual(UiText.BendAlreadyOnTop, view.Message);
            Assert.IsFalse(presenter.Kit.Prophecy.UsedThisHand, "a refused move does not spend the power");

            presenter.PressKitArrow(1, true);
            Assert.AreEqual(second, view.Kit.Cards[0].Card, "the second card is the top now");
            StringAssert.StartsWith(string.Format(UiText.BentUpFormat, second), view.Message);
            Assert.IsTrue(presenter.Kit.Prophecy.UsedThisHand);
            Assert.IsFalse(view.Kit.Cards.Any(c => c.Arrows), "the hand's bend is spent: the arrows go");

            presenter.PressKitArrow(1, false);
            Assert.AreEqual(UiText.KitUsedThisHand, view.Message, "once a hand");
            presenter.Dispose();
        }

        [Test]
        public void TheDownArrow_SendsTheCardToTheBottom()
        {
            var (presenter, view, _) = AtTheBoss(78);
            presenter.PerformAction();
            Card top = view.Kit.Cards[0].Card, second = view.Kit.Cards[1].Card;
            presenter.PressKitArrow(0, false);
            Assert.AreEqual(second, view.Kit.Cards[0].Card);
            Assert.AreNotEqual(top, view.Kit.Cards[1].Card);
            StringAssert.StartsWith(string.Format(UiText.RearrangedFormat, top), view.Message);
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
