using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.5: the panels that show relic cards — a treasure's coins, the relic a match won, the warden's relic with every place full.</summary>
    public class RelicCardFlowTests
    {
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;
        private MemoryStore _store;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _store = new MemoryStore();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242,
                new ChapterArchive(_store));
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        private void Continue(ChapterSave save)
        {
            new ChapterArchive(_store).SaveRun(save);
            _chapters.Continue();
        }

        /// <summary>Plain relics (no class's, no reward), in a fixed order.</summary>
        private static string[] Plain(int count) =>
            ChapterRelics.All.Where(id => !ChapterRelics.IsClassRelic(id) && !RelicRoster.IsReward(id)).Take(count).ToArray();

        [Test]
        public void ATreasure_ShowsItsCoins_OnACard()
        {
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "treasure");
            Continue(save);
            Assert.AreEqual(UiText.TreasureTitle, _panel.Card.Title);
            Assert.AreEqual(1, _panel.Card.Showcase.Count);
            RelicCardData coins = _panel.Card.Showcase[0];
            Assert.AreEqual(RelicCardKind.Coins, coins.Kind);
            Assert.AreEqual(string.Format(UiText.CoinsCardFormat, _chapters.Run.TreasureCoins), coins.Name);
            Assert.IsNull(_panel.Card.Options[0].Card, "TAKE stays a button");
        }

        [Test]
        public void TheWardensRelic_WithEveryPlaceFull_WaitsOnItsCard_TakeIt_AsksWhatItReplaces_WithSmallCards()
        {
            string[] relics = Plain(11);
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "warden");
            save.Relics = relics.Take(5).ToList();
            save.Bag = relics.Skip(5).Take(5).ToList();
            save.WardenRelic = relics[10];
            Continue(save);

            Assert.AreEqual(UiText.WardenRelicTitle, _panel.Card.Title);
            RelicCardData waiting = _panel.Card.Showcase.Single();
            Assert.AreEqual(relics[10], waiting.RelicId, "the warden's relic on its card");
            Assert.AreEqual(UiText.CardBagFull, waiting.Footer);
            Assert.AreEqual(2, _panel.Card.Options.Count, "the coins (or leave it), TAKE IT");
            Assert.AreEqual(UiText.WardenTake, _panel.Card.Options[1].Label);

            _panel.Press(1);   // TAKE IT
            Assert.AreEqual(UiText.SwapTitle, _panel.Card.Title);
            Assert.AreEqual(11, _panel.Card.Options.Count, "the ten held as small cards, and BACK");
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(_panel.Card.Options[i].Card.Small, "a small card: icon and name");
                Assert.AreEqual(relics[i], _panel.Card.Options[i].Card.RelicId);
                StringAssert.Contains(UiText.RelicGift(relics[i]), _panel.Card.Options[i].Card.Hover, "its words in the hover box");
            }
            Assert.IsTrue(_panel.Card.Options[10].IsLeave);

            _panel.Press(10);   // BACK
            Assert.AreEqual(UiText.WardenRelicTitle, _panel.Card.Title, "back to the card");
            Assert.AreEqual(relics[10], _chapters.Run.WardenRelicWaiting, "nothing taken yet");

            _panel.Press(1);
            _panel.Press(3);   // leave the fourth worn relic for it
            Assert.IsNull(_chapters.Run.WardenRelicWaiting);
            CollectionAssert.Contains(_chapters.Run.Effects.AllHeld, relics[10], "the warden's relic is held");
            CollectionAssert.DoesNotContain(_chapters.Run.Effects.AllHeld, relics[3], "the one left behind is gone");
        }

        [Test]
        public void TheWardensRelic_OrHisCoins_AsBefore()
        {
            string[] relics = Plain(11);
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "warden");
            save.Relics = relics.Take(5).ToList();
            save.Bag = relics.Skip(5).Take(5).ToList();
            save.WardenRelic = relics[10];
            Continue(save);
            _panel.Press(0);   // the coins, or (an offer's relic) leave it
            Assert.IsNull(_chapters.Run.WardenRelicWaiting, "the relic is let go");
            Assert.AreEqual(10, _chapters.Run.Effects.AllHeld.Count(id => !RelicRoster.IsReward(id)), "nothing held changes");
        }
    }
}
