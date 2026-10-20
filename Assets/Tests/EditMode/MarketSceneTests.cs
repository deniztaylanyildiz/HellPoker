using System;
using System.Collections.Generic;
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
    internal sealed class FakeMarketView : IMarketView
    {
        public event Action<int> SlotPressed;
        public bool IsOpen { get; private set; }
        public bool IsAnimating { get; set; }
        public MarketScene Scene { get; private set; }
        public MarketFeel Feel { get; private set; }
        public int Shows { get; private set; }

        public void Show(MarketScene scene, MarketFeel feel)
        {
            IsOpen = true;
            Scene = scene;
            Feel = feel;
            Shows++;
        }

        public void Select(MarketScene scene) => Scene = scene;
        public void Skip() => IsAnimating = false;
        public void Hide() => IsOpen = false;
        public void Press(int index) => SlotPressed?.Invoke(index);

        public int IndexOf(Func<MarketSlot, bool> which)
        {
            for (int i = 0; i < Scene.Slots.Count; i++) if (which(Scene.Slots[i])) return i;
            return -1;
        }
    }

    /// <summary>Demo 3.5: the black market as a scene — the same wares, prices and services as the panel, laid out as cards and tiles.</summary>
    public class MarketSceneTests
    {
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeMarketView _market;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;
        private MemoryStore _store;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _market = new FakeMarketView();
            _tableView = new FakeTableView();
            _store = new MemoryStore();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242,
                new ChapterArchive(_store), market: _market);
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        /// <summary>A save standing on the first chapter's nearest market, kept by <paramref name="vendor"/>.</summary>
        internal static ChapterSave AtMarket(SinnerClass cls, MarketVendor vendor, int coins = 300, int seed = 11)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, seed);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == NodeKind.BlackMarket);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(cls, seed, 1, "market");
            save.Trail = found.Select(n => n.Lane).ToList();
            save.NodeDone = false;
            save.Coins = coins;
            save.Vendor = (int)vendor;
            return save;
        }

        private void Open(MarketVendor vendor, SinnerClass cls = null, int coins = 300, Action<ChapterSave> tweak = null)
        {
            ChapterSave save = AtMarket(cls ?? SinnerRoster.Find(Jester.ClassId), vendor, coins);
            tweak?.Invoke(save);
            new ChapterArchive(_store).SaveRun(save);
            _chapters.Continue();
        }

        [Test]
        public void TheMarket_IsAScene_ItsStallCardsTilesAndLeave_AreThePanelsAnswers()
        {
            Open(MarketVendor.Smuggler);
            Assert.IsTrue(_market.IsOpen, "a scene, not the panel");
            Assert.IsFalse(_panel.IsOpen);
            MarketScene scene = _market.Scene;
            Assert.AreEqual("smuggler", scene.VendorId);
            Assert.AreEqual(_chapters.Run.Purse.Coins, scene.Coins);
            StringAssert.Contains("/", scene.RelicsLabel);
            // (OpenMarket would roll a new market: the scene's own words are read instead.)
            var stall = scene.Slots.Where(s => s.Kind == MarketSlotKind.Stall).ToList();
            Assert.AreEqual(BlackMarket.RelicsOnTable, stall.Count, "the smuggler's three relics");
            foreach (MarketSlot slot in stall)
            {
                string price = slot.Label.Substring(slot.Label.LastIndexOf(' ') + 1);
                Assert.AreEqual(string.Format(UiText.MarketRelicFormat, UiText.RelicName(slot.Card.RelicId), price), slot.Label);
                Assert.AreEqual(string.Format(UiText.CardPriceFormat, price), slot.Card.Footer, "the price at the card's foot");
            }
            Assert.AreEqual(MarketSlotKind.Leave, scene.Slots.Last().Kind, "LEAVE last");
            Assert.That(scene.Slots.Any(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Haggle));
            Assert.That(scene.Slots.Any(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Eye));
            // The tests read the scene through the panel's card: one answer for every place.
            Assert.IsNotNull(_chapters.Panel);
            Assert.AreEqual(scene.Slots.Count, _chapters.Panel.Options.Count);
            Assert.IsTrue(_chapters.IsMarketOpen);
        }

        [Test]
        public void Buying_ARelic_StampsItsCardSold_AndPlaysTheBuyingMoment()
        {
            Open(MarketVendor.Smuggler);
            int index = _market.IndexOf(s => s.Kind == MarketSlotKind.Stall && s.Enabled);
            string relic = _market.Scene.Slots[index].Card.RelicId;
            int coins = _chapters.Run.Purse.Coins;
            _market.Press(index);
            Assert.IsTrue(_chapters.Run.Effects.Holds(relic), "bought");
            Assert.Less(_chapters.Run.Purse.Coins, coins);
            MarketSlot sold = _market.Scene.Slots[index];
            Assert.AreEqual(relic, sold.Card.RelicId, "the card keeps its place");
            Assert.AreEqual(RelicCardState.Taken, sold.Card.State, "SOLD");
            Assert.IsFalse(sold.Enabled);
            Assert.AreEqual(MarketMoment.Bought, _market.Feel.Moment);
            Assert.AreEqual(index, _market.Feel.Slot);
            Assert.AreEqual(_chapters.Run.Purse.Coins, _market.Scene.Coins);
            StringAssert.Contains(UiText.VendorThanks(MarketVendor.Smuggler), _market.Scene.Line, "a word of thanks");
        }

        [Test]
        public void APlaceNotToBeHad_Shakes_AndTheVendorSaysWhy()
        {
            Open(MarketVendor.Smuggler, coins: 5);
            int index = _market.IndexOf(s => s.Kind == MarketSlotKind.Stall);
            Assert.IsFalse(_market.Scene.Slots[index].Enabled, "five coins buy nothing");
            int coins = _chapters.Run.Purse.Coins;
            _market.Press(index);
            Assert.AreEqual(coins, _chapters.Run.Purse.Coins);
            Assert.AreEqual(MarketMoment.Refused, _market.Feel.Moment);
            Assert.AreEqual(index, _market.Feel.Slot);
            StringAssert.Contains(UiText.MarketNoCoins, _market.Scene.Line);
        }

        [Test]
        public void TheBoneMerchant_SaysHisYearsUnderEveryCard()
        {
            Open(MarketVendor.BoneMerchant);
            string years = string.Format(UiText.CardYearsFormat, _chapters.Run.Scaled(BlackMarket.BoneYears));
            foreach (MarketSlot slot in _market.Scene.Slots.Where(s => s.Kind == MarketSlotKind.Stall))
                Assert.AreEqual(years, slot.Under);
        }

        [Test]
        public void ThePawnshop_SellsACarriedRelic_ThroughAPick()
        {
            Open(MarketVendor.Pawnshop, tweak: s => s.Relics = new List<string> { RelicIds.RustyCrown, ChapterRelicIds.AshPurse });
            int sell = _market.IndexOf(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Sell);
            Assert.GreaterOrEqual(sell, 0, "one tile for the selling");
            Assert.IsFalse(_market.Scene.Slots.Any(s => s.Kind == MarketSlotKind.Pick), "the carried relics wait behind it");
            _market.Press(sell);
            Assert.IsTrue(_market.Scene.Picking);
            var picks = _market.Scene.Slots.Where(s => s.Kind == MarketSlotKind.Pick).ToList();
            CollectionAssert.AreEquivalent(new[] { RelicIds.RustyCrown, ChapterRelicIds.AshPurse }, picks.Select(p => p.Card.RelicId));
            Assert.AreEqual(UiText.Back, _market.Scene.Slots.Last().Label);
            int coins = _chapters.Run.Purse.Coins;
            int at = _market.IndexOf(s => s.Kind == MarketSlotKind.Pick && s.Card.RelicId == RelicIds.RustyCrown);
            _market.Press(at);
            Assert.IsFalse(_chapters.Run.Effects.Holds(RelicIds.RustyCrown), "sold");
            Assert.Greater(_chapters.Run.Purse.Coins, coins);
            Assert.IsFalse(_market.Scene.Picking, "back at the stall");
            Assert.AreEqual(MarketMoment.Paid, _market.Feel.Moment);
        }

        [Test]
        public void APick_GoesBackToTheStall_WithBackOrEsc()
        {
            Open(MarketVendor.Smuggler, tweak: s => s.Relics = new List<string> { RelicIds.RustyCrown });
            _market.Press(_market.IndexOf(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Drop));
            Assert.IsTrue(_market.Scene.Picking);
            Assert.IsTrue(_chapters.Back(), "Esc takes BACK");
            Assert.IsFalse(_market.Scene.Picking);
            Assert.IsTrue(_market.IsOpen, "still at the market");
            Assert.IsTrue(_chapters.Run.Effects.Holds(RelicIds.RustyCrown), "nothing thrown away");
            // Esc at the stall leaves the market.
            Assert.IsTrue(_chapters.Back());
            Assert.IsFalse(_market.IsOpen);
            Assert.IsNull(_chapters.Panel);
        }

        [Test]
        public void TheAlchemistsCrucible_AndTheCleansing_AreScenesPicksToo()
        {
            Open(MarketVendor.Alchemist, tweak: s => s.Relics = new List<string> { RelicIds.RustyCrown, RelicIds.FerrymansCoin });
            int cleanse = _market.IndexOf(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Cleanse);
            Assert.GreaterOrEqual(cleanse, 0);
            _market.Press(cleanse);
            Assert.IsTrue(_market.Scene.Picking);
            Assert.That(_market.Scene.Slots.Count(s => s.Kind == MarketSlotKind.Pick), Is.GreaterThan(0));
            _market.Press(_market.Scene.Slots.Count - 1);   // BACK
            Assert.IsFalse(_market.Scene.Picking);
        }

        [Test]
        public void TheGamblersBox_LiesFaceDownOnTheStall_ThenOpens()
        {
            Open(MarketVendor.GamblersStall);
            int box = _market.IndexOf(s => s.Kind == MarketSlotKind.Stall);
            Assert.IsTrue(_market.Scene.Slots[box].FaceDown, "the box face down");
            string cost = _market.Scene.Slots[box].Label.Substring(_market.Scene.Slots[box].Label.LastIndexOf(' ') + 1);
            Assert.AreEqual(string.Format(UiText.CardPriceFormat, cost), _market.Scene.Slots[box].Under, "its price under it");
            _market.Press(box);
            Assert.That(_market.Scene.Slots.Any(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.BoxOpen));
            Assert.That(_market.Scene.Slots.Any(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.BoxDouble));
            int held = _chapters.Run.Effects.AllHeld.Count();
            _market.Press(_market.IndexOf(s => s.Kind == MarketSlotKind.Stall && s.FaceDown));   // the box pressed: opened
            Assert.AreEqual(held + 1, _chapters.Run.Effects.AllHeld.Count(), "a relic out of the box");
            Assert.That(_market.Scene.Slots.Any(s => s.Kind == MarketSlotKind.Stall && s.Card.Footer == UiText.CardYours), "shown on the stall as yours");
        }

        [Test]
        public void TheTailorsUpgrades_AreCards_TheOneBoughtStaysSold()
        {
            Open(MarketVendor.Tailor, SinnerRoster.King);
            var upgrades = _market.Scene.Slots.Where(s => s.Kind == MarketSlotKind.Stall).ToList();
            Assert.That(upgrades.Count, Is.GreaterThan(0));
            Assert.That(upgrades.All(s => s.Card.Kind == RelicCardKind.Upgrade));
            int index = _market.IndexOf(s => s.Kind == MarketSlotKind.Stall && s.Enabled);
            string upgrade = _market.Scene.Slots[index].Card.UpgradeId;
            _market.Press(index);
            Assert.IsTrue(_chapters.Run.Kit.Has(upgrade));
            var stall = _market.Scene.Slots.Where(s => s.Kind == MarketSlotKind.Stall).ToList();
            Assert.AreEqual(1, stall.Count, "one a visit: the bought one stays, sold");
            Assert.AreEqual(RelicCardState.Taken, stall[0].Card.State);
            Assert.AreEqual(LootFlight.Upgrade, _market.Feel.Flight);
        }

        [Test]
        public void TheKeys_MoveOverTheGrid_AndBuyWithEnter()
        {
            Open(MarketVendor.Smuggler);
            MarketScene scene = _market.Scene;
            Assert.AreEqual(scene.Slots.Count - 1, scene.Selected, "the keys start at LEAVE");
            _chapters.Step(0, -1);
            Assert.AreEqual(MarketSlotKind.Service, _market.Scene.Slots[_market.Scene.Selected].Kind, "up: the tiles");
            for (int i = 0; i < 4; i++) _chapters.Step(0, -1);
            Assert.AreEqual(MarketSlotKind.Stall, _market.Scene.Slots[_market.Scene.Selected].Kind, "up again: the cards");
            _chapters.Step(-1, 0);
            _chapters.Step(-1, 0);
            _chapters.Step(-1, 0);
            Assert.AreEqual(0, _market.Scene.Selected, "left: the first card");
            _chapters.Step(1, 0);
            Assert.AreEqual(1, _market.Scene.Selected);
            Assert.AreEqual(1, _chapters.Panel.Selected, "the panel's card follows");
            string relic = _market.Scene.Slots[1].Card.RelicId;
            _chapters.Confirm();
            Assert.IsTrue(_chapters.Run.Effects.Holds(relic), "Enter buys");
            Assert.AreEqual(1, _market.Scene.Selected, "the keys stay where they were");
        }

        [Test]
        public void EveryPlace_CanBeReachedWithTheKeys()
        {
            foreach (MarketVendor vendor in Enum.GetValues(typeof(MarketVendor)))
            {
                TearDown();
                SetUp();
                Open(vendor, tweak: s => s.Relics = new List<string> { RelicIds.RustyCrown, RelicIds.FerrymansCoin });
                var reached = new HashSet<int> { _market.Scene.Selected };
                var frontier = new Queue<int>(reached);
                while (frontier.Count > 0)
                {
                    int from = frontier.Dequeue();
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int to = _market.Scene.Move(from, dx, dy);
                        if (reached.Add(to)) frontier.Enqueue(to);
                    }
                }
                Assert.AreEqual(_market.Scene.Slots.Count, reached.Count, $"{vendor}: every place reached");
            }
        }

        [Test]
        public void Rerolling_TurnsTheStallOver()
        {
            Open(MarketVendor.Smuggler);
            int reroll = _market.IndexOf(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Reroll);
            _market.Press(reroll);
            Assert.AreEqual(MarketMoment.Rerolled, _market.Feel.Moment);
            Assert.IsFalse(_market.Scene.Slots.Any(s => s.Kind == MarketSlotKind.Service && s.Service == MarketService.Reroll), "once");
            Assert.AreEqual(BlackMarket.RelicsOnTable, _market.Scene.Slots.Count(s => s.Kind == MarketSlotKind.Stall), "a new stall, none sold");
        }

        [Test]
        public void TheMenu_HidesTheScene_AndContinueBringsItBack()
        {
            Open(MarketVendor.Pawnshop);
            _chapters.Hide();
            Assert.IsFalse(_market.IsOpen);
            Assert.IsNull(_chapters.Panel);
            _chapters.Show();
            Assert.IsTrue(_market.IsOpen);
            Assert.IsNotNull(_chapters.Panel);
        }

        [Test]
        public void TheGrid_TilesThreeARow_PicksTwo_LeaveAlone()
        {
            var slots = new List<MarketSlot>
            {
                MarketSlot.Stall(RelicCards.Relic(RelicIds.RustyCrown, "1"), "a", "", true),
                MarketSlot.Stall(RelicCards.Relic(RelicIds.BoneDie, "1"), "b", "", true),
                MarketSlot.Tile(MarketService.Haggle, "h", "", ""),
                MarketSlot.Tile(MarketService.Reroll, "r", "", ""),
                MarketSlot.Tile(MarketService.Eye, "e", "", ""),
                MarketSlot.Tile(MarketService.JokerIn, "j", "", ""),
                MarketSlot.Leave("x", "")
            };
            var scene = new MarketScene("smuggler", "", "", "", 0, "", slots, 0);
            CollectionAssert.AreEqual(new[] { (0, 0), (0, 1), (1, 0), (1, 1), (1, 2), (2, 0), (3, 0) }, scene.Grid());
            Assert.AreEqual(2, scene.Move(0, 0, 1), "down from the first card: the first tile");
            Assert.AreEqual(5, scene.Move(4, 0, 1), "down from the third tile: the only one below");
            Assert.AreEqual(6, scene.Move(5, 0, 1));
            Assert.AreEqual(4, scene.Move(4, 1, 0), "the row's end holds");
            Assert.AreEqual(1, scene.Move(3, 0, -1), "up from the second tile: the second card");
        }

        [Test]
        public void WithoutAScene_TheMarketIsThePanel_AsBefore()
        {
            _chapters.Dispose();
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242,
                new ChapterArchive(_store));
            Open(MarketVendor.Smuggler, tweak: s => s.Relics = new List<string> { RelicIds.RustyCrown });
            Assert.IsTrue(_panel.IsOpen);
            Assert.IsFalse(_market.IsOpen);
            PanelCard card = _chapters.Panel;
            string drop = string.Format(UiText.MarketDropFormat, UiText.RelicName(RelicIds.RustyCrown), "").TrimEnd();
            Assert.That(card.Options.Any(o => o.Label.StartsWith(drop)), "a carried relic's throwing away, one answer each");
            Assert.AreEqual(card.Options.Count - 1, card.Selected);
        }
    }
}
