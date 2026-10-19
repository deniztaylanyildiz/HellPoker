using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Phase 3 at a chapter's table: every hand is dealt from a fresh, shuffled deck (no deck goes from hand to hand, table to table,
    /// chapter to chapter or into the save; the satchel's cards stay out of it), and the payouts are in the hands' panel (H), not in a panel.
    /// </summary>
    public class PhaseThreeTableTests
    {
        private const int Dealt = 2 * Hand.Size;

        private static Sinner NewPeasant() => new Sinner(SinnerRoster.Find(Sinners.Peasant.ClassId));

        private static void PlayOut(HellPokerGame game)
        {
            if (game.Phase == GamePhase.PlayerReveal && game.CanCheckToDraw(out _)) game.CheckToDraw();
            int guard = 0;
            while (game.Phase != GamePhase.RoundOver && !game.IsGameOver && guard++ < 50)
            {
                if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
            }
        }

        [Test]
        public void AFloorsTable_DealsEveryHandFromAWholeShuffledDeck()
        {
            FloorTable table = FloorTable.Imp(ChapterRules.For(1), NewPeasant(), new RunEffects(), new CoinPurse(500), 3);
            var firstCards = new List<Card[]>();
            for (int hand = 0; hand < 4; hand++)
            {
                table.Deal();
                Assert.AreEqual(HellPokerGame.FullDeck - Dealt, table.Game.DeckCount, $"hand {hand + 1}: 52 less the two hands");
                Assert.AreEqual(ShuffleRefusal.NoDeckToCount, table.Game.WhyNoShuffle(), "nothing to count, nothing to shuffle");
                firstCards.Add(table.Game.PlayerHand.ToArray());
                PlayOut(table.Game);
                table.Settle();
                Assert.IsEmpty(table.DeckCards, "no deck goes on to the next table");
                if (table.IsOver) break;
            }
            Assert.Greater(firstCards.Select(c => string.Join(",", c)).Distinct().Count(), 1, "each a new shuffle");
        }

        [Test]
        public void TheSatchelsCards_AreOutOfEveryFreshDeck()
        {
            var kit = new ClassKit(NewPeasant());
            Card[] kept = { TestCards.Card("AS"), TestCards.Card("KS") };
            kit.Relics = () => Relics.RelicRoster.Combined(new[] { Relics.ChapterRelicIds.RopeBelt });
            kit.Restore(kept, 0, null, 0, new[] { UpgradeIds.WideSatchel });
            FloorTable table = FloorTable.Imp(ChapterRules.For(1), kit.Sinner, new RunEffects(), new CoinPurse(500), 5);
            ChapterHooks.Attach(table.Game, kit, null, 0, 1);
            for (int hand = 0; hand < 6 && !table.IsOver; hand++)
            {
                table.Deal();
                Assert.AreEqual(HellPokerGame.FullDeck - Dealt - kept.Length, table.Game.DeckCount, "the satchel's two are out of the deck");
                Assert.IsFalse(table.Game.Deck.Remaining.Intersect(kept).Any());
                Assert.IsFalse(table.Game.PlayerHand.Concat(table.Game.HouseHand).Intersect(kept).Any(), "and never dealt");
                PlayOut(table.Game);
                table.Settle();
            }
        }

        [Test]
        public void ADemonsTable_DealsEveryHandFromAWholeDeck()
        {
            HellPokerGame game = BossTable.Create(GameRules.Default, DealerRoster.Mammon, 1300, 200, 175, 250, 11, null, NewPeasant());
            for (int hand = 0; hand < 3 && !game.IsGameOver; hand++)
            {
                game.PlaceBet();
                Assert.AreEqual(HellPokerGame.FullDeck - Dealt, game.DeckCount, $"hand {hand + 1}");
                Assert.IsEmpty(game.DeckCards);
                PlayOut(game);
                if (!game.IsGameOver) game.NextRound();
            }
        }

        [Test]
        public void TheOldCountedDeck_StaysAKnob()
        {
            try
            {
                ChapterTuning.FreshDeckEveryHand = false;
                FloorTable table = FloorTable.Imp(ChapterRules.For(1), NewPeasant(), new RunEffects(), new CoinPurse(500), 3);
                table.Deal();
                PlayOut(table.Game);
                table.Settle();
                Assert.IsNotEmpty(table.DeckCards, "HELLPOKER_CHAPTER_DECK=0: the counted deck of Phase 2");
            }
            finally
            {
                ChapterTuning.Reset();
            }
        }

        [Test]
        public void TheSave_NoLongerHoldsADeck_AndAnOldOnesIsIgnored()
        {
            var save = new ChapterSave { ClassId = Sinners.Peasant.ClassId, Seed = 7, Chapter = 2, Coins = 40, Years = 1300 };
            string encoded = save.Encode();
            StringAssert.DoesNotContain("deck=", encoded);
            Assert.IsTrue(ChapterSave.TryDecode(encoded + "\ndeck=AS,KS,QS", out ChapterSave old), "an old save with a deck still loads");
            Assert.AreEqual(2, old.Chapter);
        }

        // ------------------------------------------------------------------ at the presenter

        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242);
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        /// <summary>A run saved at a demon's table of <paramref name="chapter"/> (an old save's deck in it), continued.</summary>
        private void ContinueAtTheDemon(int chapter)
        {
            _chapters.Dispose();
            var store = new MemoryStore();
            var archive = new ChapterArchive(store);
            archive.SaveRun(new ChapterSave
            {
                ClassId = Sinners.Peasant.ClassId, Seed = 7, Chapter = chapter, Coins = 40, Years = 1300, Match = "boss", BossBarStart = 1300,
                BossBar = 1300, NodeDone = false
            });
            // An old save (2.3.1) carried the run's deck: ten cards left. It is ignored now.
            string key = ChapterArchive.RunKey;
            store.SetString(key, store.GetString(key, "") + "\ndeck=2C,3C,4C,5C,6C,7C,8C,9C,10C,JC");
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.IsTrue(_chapters.IsAtTable);
        }

        [Test]
        public void AfterAChapterAndAfterContinue_EveryHandHasAWholeDeck_AndNoCounterOrShuffle()
        {
            foreach (int chapter in new[] { 1, 2 })
            {
                ContinueAtTheDemon(chapter);
                for (int hand = 0; hand < 2; hand++)
                {
                    _table.PerformAction();   // the deal
                    Assert.AreEqual(HellPokerGame.FullDeck - Dealt, _table.Game.DeckCount, $"chapter {chapter}, hand {hand + 1}: a whole deck");
                    Assert.IsNull(_tableView.DeckLabel, "no deck counter at a chapter's table");
                    Assert.IsNull(_tableView.ShuffleLabel, "no SHUFFLE");
                    int guard = 0;
                    while (_table.Game.Phase != GamePhase.RoundOver && !_table.Game.IsGameOver && guard++ < 30) _table.PerformAction();
                    if (_table.Game.IsGameOver) break;
                    _table.PerformAction();   // NEXT HAND
                }
            }
        }

        [Test]
        public void S_DoesNothingAtAChaptersTable()
        {
            ContinueAtTheDemon(1);
            string before = _tableView.Message;
            _table.ShuffleDeck();
            Assert.AreEqual(before, _tableView.Message, "no refusal said: there is simply nothing to shuffle");
        }

        [Test]
        public void TheHandsPanel_HoldsTheTablesOwnPayouts_AndHowItPays()
        {
            ContinueAtTheDemon(1);
            _table.ToggleHandRanks();
            Assert.IsTrue(_tableView.HandRanksOpen);
            Assert.AreEqual(DealerRoster.Mammon.Payouts.GetMultiplier(HandCategory.RoyalFlush), _tableView.HandRanksPayouts.GetMultiplier(HandCategory.RoyalFlush),
                "the demon's own multipliers");
            StringAssert.Contains(UiText.PayoutNoteAnte, _tableView.HandRanksNotes);
            var scaled = (ScaledPayoutTable)((HellPokerGame)_table.Game).Payouts;
            StringAssert.Contains(string.Format(UiText.PayoutNoteScaleFormat, scaled.WinScalePercent, scaled.LossScalePercent), _tableView.HandRanksNotes,
                "the bar's scale, from the game's own table");
            StringAssert.Contains(string.Format(UiText.PayoutNoteInterestFormat, MammonInterest.InterestPercent), _tableView.HandRanksNotes,
                "Mammon's interest");
            _table.ToggleHandRanks();
            Assert.IsFalse(_tableView.HandRanksOpen, "H closes it");
        }

        [Test]
        public void AFloorsHandsPanel_ShowsTheCappedMultipliers()
        {
            _chapters.Start(SinnerRoster.Peasant);
            _panel.Press(0);
            _map.Press(_map.State.Choices.First(n => n.Kind == NodeKind.Table));
            Assert.IsTrue(_chapters.IsAtTable);
            _table.ToggleHandRanks();
            var payouts = _tableView.HandRanksPayouts;
            Assert.IsInstanceOf<FloorPayoutTable>(payouts);
            Assert.AreEqual(((FloorPayoutTable)payouts).Cap, payouts.GetMultiplier(HandCategory.RoyalFlush), "×3 at most on a floor");
            Assert.IsNotNull(_tableView.HandRanksNotes);
        }

        [Test]
        public void TheSimulatorsHandsPanel_HoldsTheDemonsOwnPayouts()
        {
            // Masa birliği: the Boss Simulator's tables have no payouts panel either; Belial's high multipliers and Lilith's heavier loss
            // are in the hands' panel.
            _table.StartNewRun(DealerRoster.Belial, SinnerRoster.Peasant);
            _table.ToggleHandRanks();
            Assert.AreEqual(DealerRoster.Belial.Payouts.GetMultiplier(HandCategory.RoyalFlush), _tableView.HandRanksPayouts.GetMultiplier(HandCategory.RoyalFlush));
            StringAssert.Contains(UiText.PayoutNoteAnte, _tableView.HandRanksNotes);
            _table.ToggleHandRanks();
            _table.StartNewRun(DealerRoster.Lilith, SinnerRoster.Peasant);
            _table.ToggleHandRanks();
            StringAssert.Contains(UiText.LossSurcharge(DealerRoster.Lilith.Payouts.LossPercent), _tableView.HandRanksNotes, "her loss ×1.25");
        }
    }
}
