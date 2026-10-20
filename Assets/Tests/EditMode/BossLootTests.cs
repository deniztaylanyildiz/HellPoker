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
    internal sealed class FakeBossLootView : IBossLootView
    {
        public event Action<int> CardPressed;
        public event Action RerollPressed;
        public event Action BackPressed;
        public bool IsOpen { get; private set; }
        public bool IsAnimating { get; set; }
        public BossLootState State { get; private set; }
        public bool Dealt { get; private set; }
        public List<int> Changed { get; } = new List<int>();
        public (int index, LootFlight flight)? Chosen { get; private set; }
        public int Skips { get; private set; }

        public void Show(BossLootState state, bool deal)
        {
            IsOpen = true;
            State = state;
            Dealt = deal;
        }

        public void Select(BossLootState state) => State = state;

        public void Reroll(BossLootState state, IReadOnlyCollection<int> changed)
        {
            State = state;
            Changed.Clear();
            Changed.AddRange(changed);
        }

        public void PlayChosen(int index, LootFlight flight, Action done)
        {
            Chosen = (index, flight);
            done?.Invoke();
        }

        public void Skip()
        {
            Skips++;
            IsAnimating = false;
        }

        public void Hide() => IsOpen = false;

        public void Press(int index) => CardPressed?.Invoke(index);
        public void PressReroll() => RerollPressed?.Invoke();
        public void PressBack() => BackPressed?.Invoke();
    }

    /// <summary>Demo 3.5: the spoils as ARAM's choice — the same loot on cards, the same takes, REROLL once, the swap step, the save.</summary>
    public class BossLootTests
    {
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeBossLootView _loot;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;
        private MemoryStore _store;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _loot = new FakeBossLootView();
            _tableView = new FakeTableView();
            _store = new MemoryStore();
            Build();
        }

        private void Build()
        {
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242,
                new ChapterArchive(_store), loot: _loot);
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        private void AtTheSpoils(ChapterSave save = null)
        {
            new ChapterArchive(_store).SaveRun(save ?? ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "loot"));
            _chapters.Continue();
        }

        [Test]
        public void TheSpoils_AreAScene_OneCardForEachOfTheLoot_DealtOneByOne()
        {
            AtTheSpoils();
            Assert.IsTrue(_loot.IsOpen, "a scene, not the panel");
            Assert.IsFalse(_panel.IsOpen);
            Assert.IsTrue(_loot.Dealt, "the cards come in");
            ChapterRun run = _chapters.Run;
            int expected = run.LootOffers.Count + (run.LootUpgrade != null ? 1 : 0) + 1;
            Assert.AreEqual(expected, _loot.State.Cards.Count, "two relics, the upgrade, the coins");
            for (int i = 0; i < run.LootOffers.Count; i++) Assert.AreEqual(run.LootOffers[i], _loot.State.Cards[i].RelicId);
            Assert.AreEqual(RelicCardKind.Upgrade, _loot.State.Cards[run.LootOffers.Count].Kind);
            Assert.AreEqual(RelicCardKind.Coins, _loot.State.Cards.Last().Kind);
            Assert.AreEqual(UiText.LootSceneTitle, _loot.State.Title);
            StringAssert.Contains("Mammon", _loot.State.Subtitle);
            Assert.IsTrue(_loot.State.CanReroll);
            Assert.AreEqual(UiText.LootReroll, _loot.State.RerollLabel);
        }

        [Test]
        public void ARelicCard_TakesTheRelic_AndTheNextChapterOpens()
        {
            AtTheSpoils();
            string relic = _chapters.Run.LootOffers[0];
            _loot.Press(0);
            Assert.AreEqual((0, LootFlight.Relic), _loot.Chosen, "the chosen card plays out, the relic flies");
            Assert.IsFalse(_loot.IsOpen);
            Assert.AreEqual(2, _chapters.Journey.Chapter, "on to Belial");
            CollectionAssert.Contains(_chapters.Journey.Effects.AllHeld, relic);
        }

        [Test]
        public void TheUpgradeCard_TakesTheUpgrade_TheCoinsCard_TheCoins()
        {
            AtTheSpoils();
            string upgrade = _chapters.Run.LootUpgrade;
            _loot.Press(_chapters.Run.LootOffers.Count);
            Assert.AreEqual(LootFlight.Upgrade, _loot.Chosen?.flight);
            CollectionAssert.Contains(_chapters.Journey.Kit.Upgrades, upgrade);

            _chapters.Dispose();
            _store = new MemoryStore();
            _loot = new FakeBossLootView();
            Build();
            AtTheSpoils();
            int purse = _chapters.Run.Purse.Coins;
            _loot.Press(_loot.State.Cards.Count - 1);
            Assert.AreEqual(LootFlight.Coins, _loot.Chosen?.flight);
            Assert.AreEqual(2, _chapters.Journey.Chapter);
            Assert.GreaterOrEqual(_chapters.Run.Purse.Coins, purse + ChapterRun.LootCoins, "the coins (the next chapter tops the purse up too)");
        }

        [Test]
        public void Reroll_Once_TheRelicCardsTurnOver_TheUpgradeAndCoinsStay_AndTheSaveKeepsIt()
        {
            AtTheSpoils();
            var before = _chapters.Run.LootOffers.ToList();
            string upgrade = _loot.State.Cards[before.Count].UpgradeId;
            _loot.PressReroll();
            var after = _chapters.Run.LootOffers.ToList();
            CollectionAssert.AreNotEqual(before, after);
            CollectionAssert.IsSubsetOf(_loot.Changed, Enumerable.Range(0, before.Count), "only relic cards turn over");
            Assert.Greater(_loot.Changed.Count, 0);
            Assert.AreEqual(upgrade, _loot.State.Cards[before.Count].UpgradeId, "the upgrade stays");
            Assert.AreEqual(RelicCardKind.Coins, _loot.State.Cards.Last().Kind);
            Assert.IsFalse(_loot.State.CanReroll);
            Assert.AreEqual(UiText.LootRerolled, _loot.State.RerollLabel, "REROLLED, dim");
            foreach (string id in after) CollectionAssert.DoesNotContain(_chapters.Run.Effects.AllHeld, id, "no held relic comes");
            _loot.PressReroll();
            CollectionAssert.AreEqual(after, _chapters.Run.LootOffers, "once");

            // The game closes on the spoils: the same cards, the reroll spent.
            _chapters.Dispose();
            _loot = new FakeBossLootView();
            Build();
            _chapters.Continue();
            Assert.IsTrue(_loot.IsOpen, "back on the spoils");
            CollectionAssert.AreEqual(after, _loot.State.Cards.Take(after.Count).Select(c => c.RelicId), "the same cards");
            Assert.IsFalse(_loot.State.CanReroll, "the reroll stays spent");
        }

        [Test]
        public void TheKeys_WalkTheCardsAndReroll_EnterTakes_APressDuringTheDealHurriesIt()
        {
            AtTheSpoils();
            int count = _loot.State.Cards.Count;
            _loot.IsAnimating = true;
            _chapters.Confirm();
            Assert.AreEqual(1, _loot.Skips, "a press while the cards come in hurries them");
            Assert.IsNull(_loot.Chosen, "and takes nothing");
            for (int i = 0; i < count; i++) _chapters.Step(1, 0);
            Assert.AreEqual(count, _loot.State.Selected, "past the cards: REROLL");
            _chapters.Confirm();
            Assert.IsTrue(_chapters.Run.LootRerolled, "Enter on REROLL rerolls");
            _chapters.Step(1, 0);
            Assert.AreEqual(0, _loot.State.Selected, "REROLL spent: round to the first card");
            _chapters.Step(-1, 0);
            Assert.AreEqual(count - 1, _loot.State.Selected);
            _chapters.Confirm();
            Assert.AreEqual(LootFlight.Coins, _loot.Chosen?.flight, "Enter takes the pointed card");
        }

        [Test]
        public void WithTheBagFull_ARelicAsksWhatItReplaces_SmallCards_BackToTheCards_ThenTheSwap()
        {
            string[] held = ChapterRelics.All.Where(id => !ChapterRelics.IsClassRelic(id) && !RelicRoster.IsReward(id)
                                                         && ChapterRelics.RarityOf(id) != RelicRarity.Boss).Take(10).ToArray();
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "loot");
            save.Relics = held.Take(5).ToList();
            save.Bag = held.Skip(5).ToList();
            AtTheSpoils(save);
            Assert.AreEqual(UiText.CardBagFull, _loot.State.Cards[0].Footer, "the bag is full: the cards say so");
            string relic = _chapters.Run.LootOffers[0];
            _loot.Press(0);
            Assert.IsNull(_loot.Chosen, "not yet");
            Assert.IsNotNull(_loot.State.Swap, "which one stays behind?");
            Assert.AreEqual(10, _loot.State.Swap.Count);
            Assert.IsTrue(_loot.State.Swap.All(c => c.Small));
            Assert.AreEqual(UiText.SwapTitle, _loot.State.SwapTitle);

            _loot.PressBack();
            Assert.IsNull(_loot.State.Swap, "BACK: the cards again");
            Assert.AreEqual(0, _loot.State.Selected, "on the relic chosen");

            _loot.Press(0);
            _loot.Press(2);   // the third held relic stays behind
            Assert.AreEqual((0, LootFlight.Relic), _loot.Chosen);
            CollectionAssert.Contains(_chapters.Journey.Effects.AllHeld, relic);
            CollectionAssert.DoesNotContain(_chapters.Journey.Effects.AllHeld, held[2]);
        }

        [Test]
        public void EscOnTheSpoils_GoesToTheMenu_TheSceneComesBackWithContinue()
        {
            AtTheSpoils();
            Assert.IsFalse(_chapters.Back(), "nothing to close: the menu takes Esc");
            _chapters.Hide();
            Assert.IsFalse(_loot.IsOpen);
            _chapters.Continue();
            Assert.IsTrue(_loot.IsOpen, "the spoils wait");
            Assert.IsFalse(_loot.Dealt, "shown at once, not dealt again");
        }
    }
}
