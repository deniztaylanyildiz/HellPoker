using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
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
    internal sealed class FakeChapterMapView : IChapterMapView
    {
        public event Action<int, int> NodePressed;
        public event Action MenuPressed;
        public bool IsVisible { get; private set; }
        public ChapterMapState State { get; private set; }
        public int Shows { get; private set; }

        public void Show(ChapterMapState state)
        {
            State = state;
            IsVisible = true;
            Shows++;
        }

        public void Hide() => IsVisible = false;
        public void Press(MapNode node) => NodePressed?.Invoke(node.Floor, node.Lane);
        public void PressMenu() => MenuPressed?.Invoke();
    }

    internal sealed class FakeChapterPanelView : IChapterPanelView
    {
        public event Action<int> OptionPressed;
        public bool IsOpen => Card != null;
        public PanelCard Card { get; private set; }
        public List<string> Titles { get; } = new List<string>();

        public void Show(PanelCard card)
        {
            Card = card;
            Titles.Add(card.Title);
        }

        public void Hide() => Card = null;
        public void Press(int index) => OptionPressed?.Invoke(index);
    }

    /// <summary>Phase 2's chapter presenter, with fake screens: the map, the panels, the floors' tables and the demon's.</summary>
    public class ChapterPresenterTests
    {
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;
        private AudioTests.FakeAudio _audio;

        [SetUp]
        public void SetUp()
        {
            _audio = new AudioTests.FakeAudio();
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242);
        }

        [TearDown]
        public void TearDown()
        {
            _chapters.Dispose();   // the chapter's table goes with it
        }

        /// <summary>One action at a chapter's table; a floor's imp deals every hand with a single coin (a player who only passes
        /// and never draws would otherwise feed its purse for a long while).</summary>
        private void Act()
        {
            FloorTable floor = _table.Floor;
            if (floor?.HousePurse != null && _table.Game.Phase == GamePhase.Betting && floor.HousePurse.Coins > 1)
                floor.HousePurse.Add(1 - floor.HousePurse.Coins);
            _table.PerformAction();
        }

        private void Begin(SinnerClass sinner = null)
        {
            _chapters.Start(sinner ?? SinnerRoster.Peasant);
            Assert.IsTrue(_panel.IsOpen, "the chapter opens with its words");
            _panel.Press(0);   // DESCEND
        }

        [Test]
        public void AChapterStartsOnTheMapWithItsFirstFloorLit()
        {
            _chapters.Start(SinnerRoster.Peasant);
            Assert.IsTrue(_map.IsVisible);
            Assert.AreEqual(UiText.PanelDescend, _panel.Card.Options[0].Label);
            Assert.IsEmpty(_map.State.Choices, "nothing is picked under a panel");
            _panel.Press(0);
            Assert.IsFalse(_panel.IsOpen);
            Assert.AreEqual(6, _map.State.Choices.Count);
            Assert.AreEqual(120, _map.State.Coins, "the Peasant's purse");
            Assert.IsTrue(_chapters.HasRun);
            Assert.IsTrue(_chapters.IsMapOpen);
        }

        [Test]
        public void AFirstFloorTableIsPlayedForCoinsAtItsOwnTable()
        {
            Begin();
            _map.Press(_map.State.Choices.First());
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.IsFalse(_map.IsVisible);
            Assert.IsTrue(_table.AtChapterTable);
            Assert.AreEqual(Currency.Coins, _tableView.Currency);
            Assert.AreEqual(ChapterCast.ImpId, _tableView.DealerView.Dealer.Id);
            Assert.AreEqual(UiText.CoinsLabel, _tableView.SentenceView.Label);
            Assert.AreEqual(120, _tableView.SentenceView.Years, "the counter is the purse");
            Assert.AreEqual(LeaveState.Hidden, _tableView.Leave, "no leaving a chapter's table");
            ChapterRules rules = _chapters.Run.Rules;
            Assert.AreEqual(15, rules.ImpCoinsAt(0), "the first floor's imp: half a purse, a warm-up");
            Assert.AreEqual(string.Format(UiText.FloorSeatTitleFormat, 15, rules.Ante, rules.AnteStepHands),
                _tableView.DealerView.Dealer.Title, "the imp's purse and the ante under its name");
        }

        [Test]
        public void AFloorMatchEndsWhenTheImpsPurseIsEmptyAndGoesBackToTheMap()
        {
            Begin();
            _chapters.Run.Purse.Add(100000);   // a purse no imp can empty: the match ends with the imp's
            _map.Press(_map.State.Choices.First());
            int guard = 0;
            while (_chapters.IsAtTable && guard++ < 2000)
            {
                Act();
                if (_table.Floor != null && _table.Game.Phase == GamePhase.RoundOver)
                    Assert.AreEqual(_chapters.Run.Purse.Coins, _tableView.SentenceView.Years, "the counter shows the purse after every hand");
            }
            Assert.IsFalse(_chapters.IsAtTable, "the imp's purse ran dry, then the map");
            Assert.IsTrue(_map.IsVisible);
            Assert.IsTrue(_panel.IsOpen);
            Assert.AreEqual(UiText.MatchWonTitle, _panel.Card.Title);
            StringAssert.Contains(string.Format(UiText.MatchPurseFormat, _chapters.Run.Purse.Coins), _panel.Card.Text);
        }

        [Test]
        public void AnEmptyPurseEndsTheRunAtTheTable()
        {
            Begin();
            int guard = 0;
            while (guard++ < 5000 && !(_panel.IsOpen && _panel.Card.Title == UiText.PurseEmptyTitle))
            {
                if (_panel.IsOpen)
                {
                    Assert.AreNotEqual(UiText.GateTitle(1), _panel.Card.Title, "a purse of one coin never reaches the gate");
                    _panel.Press(_panel.Card.Options.Count - 1);
                }
                else if (_chapters.IsAtTable) _table.PerformAction();
                else
                {
                    _chapters.Run.Purse.Add(1 - _chapters.Run.Purse.Coins);   // one coin to the next table: all in, every hand
                    _map.Press(_map.State.Choices.First());
                }
            }
            Assert.AreEqual(UiText.PurseEmptyTitle, _panel.Card.Title);
            Assert.AreEqual(0, _chapters.Run.Purse.Coins);
            Assert.IsTrue(_chapters.Run.PurseEmptied);
            Assert.IsFalse(_chapters.HasRun, "the run is over");
            CollectionAssert.AreEqual(new[] { UiText.ChapterNewRun, UiText.Menu }, _panel.Card.Options.Select(o => o.Label));
        }

        [Test]
        public void TheWholeChapterPlaysThroughTheGateToTheDemonsTable()
        {
            Begin();
            _chapters.Run.Purse.Add(100000);   // the floors cannot end this run
            int guard = 0;
            bool sawBoss = false, sawGate = false;
            while (guard++ < 5000)
            {
                if (_panel.IsOpen)
                {
                    PanelCard card = _panel.Card;
                    if (card.Title == UiText.LootTitle || card.Title == UiText.ChapterDamnedTitle) break;
                    if (card.Title == UiText.GateTitle(1)) sawGate = true;
                    _panel.Press(card.Options.Count - 1);   // the way out: leave, pass, on (the fire's rest)
                }
                else if (_chapters.IsAtTable)
                {
                    sawBoss |= _table.AtChapterTable && _table.Floor == null;
                    Act();
                }
                else
                {
                    _map.Press(_map.State.Choices.First());
                }
            }
            Assert.IsTrue(sawGate, "the last floor leads to the gate");
            Assert.IsTrue(sawBoss, "the gate leads to Mammon's table");
            Assert.That(new[] { UiText.LootTitle, UiText.ChapterDamnedTitle }, Does.Contain(_panel.Card.Title));
            if (_panel.Card.Title == UiText.ChapterDamnedTitle) Assert.IsFalse(_chapters.HasRun, "a burned soul ends the run");
            else Assert.IsTrue(_chapters.Run.BossBeaten, "the spoils of a beaten demon");
        }

        [Test]
        public void TheGateWritesTheMissingCoinsOnTheSentence()
        {
            Begin();
            ChapterRun run = _chapters.Run;
            run.Purse.Add(5000);
            int guard = 0;
            while (!(_panel.IsOpen && _panel.Card.Title == UiText.GateTitle(1)) && guard++ < 3000)
            {
                if (_panel.IsOpen) _panel.Press(_panel.Card.Options.Count - 1);
                else if (_chapters.IsAtTable) Act();
                else _map.Press(_map.State.Choices.First());
            }
            run.Purse.Add(40 - run.Purse.Coins);   // short of the Peasant's 180 (120 + 60)
            int coins = run.Purse.Coins, years = run.Years, owed = run.YearsOwed;
            Assert.AreEqual(705, run.TributeYears(coins), "40 coins pay 39 (the last coin stays): 141 missing");
            _panel.Press(0);   // PAY AND SIT
            int bar = years + 705 + owed;
            Assert.AreEqual(bar + bar / 10, run.Years, "the tribute's years on the bar — and the fire's whetted soul (the last answer) a tenth on");
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.AreEqual(Currency.Bar, _tableView.Currency, "the demon's bar: no number on the counter");
            Assert.AreEqual("MAMMON'S BAR", _tableView.SentenceView.Label);
            Assert.AreEqual(run.BossBarStart * 2, _table.Game.Rules.SoulThreshold, "Mammon's soul line: twice his bar");
            Assert.AreEqual(0, _table.Game.Rules.ForcedRaiseYears, "no final stretch on a bar");
            Assert.AreEqual(DealerRoster.MammonId, _tableView.DealerView.Dealer.Id);
            Assert.AreEqual(run.Years, _table.Game.Years);

            // The demon's table never tells years: the bet, the stake line and the prompt are shares of the bar.
            StringAssert.Contains("%", _tableView.StakeInfo);
            StringAssert.DoesNotContain("years", _tableView.StakeInfo);
            StringAssert.Contains("% of the bar", _tableView.Message);
            Assert.Less(_tableView.Ante, 100, "the ante as a percent of the bar, not its years");
        }

        [Test]
        public void ARunIsSavedAndContinued_AHandLeftInTheMiddleIsLost()
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            Begin();
            _map.Press(_map.State.Choices.First());
            _table.PerformAction();   // the deal: a hand in the middle
            Assert.IsTrue(archive.HasRun, "saved at every step");
            ChapterSave saved = archive.LoadRun();
            Assert.Greater(saved.HandStake, 0, "the hand in the middle is in the save");
            Assert.AreEqual("imp", saved.Match);
            Assert.AreEqual(_chapters.Run.Purse.Coins, saved.Coins, "the purse is settled only at the hand's end");

            // The game closes and opens again: a fresh presenter on the same save.
            _chapters.Dispose();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            Assert.IsTrue(_chapters.CanContinue);
            Assert.IsFalse(_chapters.HasRun, "nothing in memory yet");
            _chapters.Continue();
            Assert.AreEqual(UiText.LostHandTitle, _panel.Card.Title, "the hand left behind is lost");
            Assert.AreEqual(saved.Coins - saved.HandStake, _chapters.Run.Purse.Coins, "the stake is lost");
            Assert.AreEqual(1, _chapters.Run.Trail.Count, "back where it was");
            _panel.Press(0);
            Assert.IsTrue(_chapters.IsAtTable, "the match goes on");
            Assert.AreEqual(1, _table.Floor.HandsPlayed, "the lost hand counts");
        }

        /// <summary>A presenter sitting at Lucifer's table (a run saved there), its bar set to <paramref name="bar"/>, the next deal stacked.</summary>
        private ChapterArchive SitAtLucifer(int bar, string deal)
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            archive.SaveRun(new ChapterSave
            {
                ClassId = Core.Sinners.Peasant.ClassId, Seed = 7, Chapter = 3, Lucifer = true, LuciferBarStart = 1050, Coins = 40, Years = 1,
                Match = "lucifer", BossBar = 1050, NodeDone = true, BossBeaten = true, LootTaken = true
            });
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.IsTrue(_chapters.IsAtTable, "straight back to his table");
            Assert.AreEqual(JourneyStage.Lucifer, _chapters.Journey.Stage);
            var game = (HellPokerGame)_table.Game;
            game.TakeOver(bar, 0);
            var head = TestCards.Cards(deal).ToList();
            game.RestoreDeck(head.Concat(Core.Cards.Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            return archive;
        }

        private void PlayUntilAPanel()
        {
            int guard = 0;
            while (!_panel.IsOpen && guard++ < 200) _table.PerformAction();
        }

        [Test]
        public void LucifersBarEmpty_IsSalvation_AndTheRunsSummary()
        {
            ChapterArchive archive = SitAtLucifer(50, "AS AC 8S 8C 2D 9H 9D 4C 5C 6H");   // the Dead Man's Hand: nothing breaks it
            PlayUntilAPanel();
            Assert.AreEqual(UiText.FreedTitle, _panel.Card.Title);
            StringAssert.Contains(UiText.SinnerName(Core.Sinners.Peasant.ClassId), _panel.Card.Text, "the run's summary");
            Assert.AreEqual(JourneyEnd.Freed, _chapters.Journey.End);
            Assert.AreEqual(1, archive.LoadRecords().Freed);
            Assert.IsFalse(archive.HasRun, "a finished run leaves no save");
        }

        [Test]
        public void LucifersBarPastAQuarterMore_CastsTheRunDown()
        {
            ChapterArchive archive = SitAtLucifer(1310, "3D 5C 7H JC KD 9C 9D 9H 9S 2C");   // nothing against four nines, 2 below his gate
            PlayUntilAPanel();
            Assert.AreEqual(UiText.FallTitle, _panel.Card.Title);
            Assert.AreEqual(JourneyEnd.CastDown, _chapters.Journey.End);
            Assert.AreEqual(1, archive.LoadRecords().CastDown);
        }

        [Test]
        public void AbandoningARun_CountsItDamned_AndClearsTheSave()
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            Begin();
            Assert.IsTrue(archive.HasRun);
            _chapters.Abandon();
            Assert.IsFalse(archive.HasRun);
            Assert.AreEqual(1, archive.LoadRecords().Abandoned);
            Assert.AreEqual(1, archive.LoadRecords().Runs);
            Assert.IsFalse(_chapters.CanContinue);
        }

        [Test]
        public void MenuOnTheMapAsksForTheTitleMenu()
        {
            Begin();
            int asked = 0;
            _chapters.MenuRequested += () => asked++;
            _map.PressMenu();
            Assert.AreEqual(1, asked);
            Assert.IsFalse(_chapters.Back(), "with no panel open, Esc is the menu's");
        }

        [Test]
        public void TheArrowsPickANodeAndEnterGoesThere()
        {
            Begin();
            _chapters.Step(1, 0);
            MapNode picked = _map.State.Selected;
            Assert.IsNotNull(picked);
            _chapters.Confirm();
            Assert.AreEqual(picked, _chapters.Run.Current);
        }

        [Test]
        public void EscOnAPanelTakesItsWayOut()
        {
            _chapters.Start(SinnerRoster.Peasant);
            Assert.IsFalse(_chapters.Back(), "the opening words have no way out but on: Esc is the menu's");
            Assert.IsTrue(_panel.IsOpen);
        }

        // ------------------------------------------------------------------ Phase 2's class mechanics and the demons' rules

        [Test]
        public void TheClassPanel_ThePeasantKeepsAThrownCard()
        {
            Begin();
            _map.Press(_map.State.Choices.First());
            Assert.AreEqual(string.Format(UiText.KitSatchelTitleFormat, 0, ClassKit.SatchelSize), _tableView.Kit.Title);
            Act();   // the deal
            for (int guard = 0; _table.Game.Phase != GamePhase.Drawing && guard < 10; guard++) Act();
            Assert.AreEqual(GamePhase.Drawing, _table.Game.Phase);
            _table.ToggleDiscard(0);
            _table.ToggleDiscard(1);
            Act();   // DRAW
            Assert.AreEqual(UiText.KitKeepTitle, _tableView.Kit.Title, "the thrown cards, to keep one");
            Assert.AreEqual(2, _tableView.Kit.Cards.Count);
            _tableView.PressKitCard(0);
            Assert.AreEqual(1, _chapters.Run.Kit.Satchel.Count);
            Assert.AreEqual(string.Format(UiText.KitSatchelTitleFormat, 1, ClassKit.SatchelSize), _tableView.Kit.Title);
        }

        [Test]
        public void TheKingsPanel_SpendsATaxToTurnACard()
        {
            Begin(SinnerRoster.King);
            _map.Press(_map.State.Choices.First());
            Assert.AreEqual(3, _tableView.Kit.Buttons.Count);
            Assert.IsTrue(_tableView.Kit.Buttons.All(b => b.Locked), "no taxes, no hand");
            _chapters.Run.Kit.AddTax(5);
            Act();   // the deal
            Assert.IsFalse(_tableView.Kit.Buttons[0].Locked, "a card may turn now");
            _tableView.PressKitButton(0);
            Assert.AreEqual(1, _table.Game.HouseCardsRevealed);
            Assert.AreEqual(4, _chapters.Run.Kit.Taxes);
            Assert.AreEqual(UiText.RevealedMessage, _tableView.Message);
        }

        [Test]
        public void ADemonsTable_OpensWithItsRule_AndTheWarlockSeesTheDeck()
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            archive.SaveRun(new ChapterSave
            {
                ClassId = Core.Sinners.Warlock.ClassId, Seed = 7, Chapter = 2, Coins = 40, Years = 1950, Match = "boss", BossBarStart = 1950,
                BossBar = 1950, NodeDone = false
            });
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.IsInstanceOf<BelialRoleSwap>(_table.Kit.Rule);
            Assert.AreEqual(UiText.DemonRuleLine(_table.Kit.Rule), _tableView.DealerView.LastLine, "the rule, said as the table opens");
            Assert.AreEqual(UiText.KitProphecyTitle, _tableView.Kit.Title);
            Assert.AreEqual(2, _tableView.Kit.Cards.Count, "two cards of the top after Mammon");
        }

        [Test]
        public void LucifersTable_OpensWithTheMirrorOfTheClass()
        {
            SitAtLucifer(1050, "AH");
            Assert.IsInstanceOf<LuciferMirror>(_table.Kit.Rule);
            Assert.AreEqual(KitKind.Satchel, ((LuciferMirror)_table.Kit.Rule).Mirrors);
            Assert.AreEqual(UiText.DemonRuleLine(_table.Kit.Rule), _tableView.DealerView.LastLine);
        }

        // ------------------------------------------------------------------ the demon's health bar

        private void SitAtMammon()
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            archive.SaveRun(new ChapterSave
            {
                ClassId = Core.Sinners.Peasant.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1300, Match = "boss", BossBarStart = 1300,
                BossBar = 1300, NodeDone = false
            });
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.IsTrue(_chapters.IsAtTable);
        }

        [Test]
        public void ADemonsTable_ShowsHisBar_UnderThePortrait_NotTheOldCounter()
        {
            SitAtMammon();
            BossBarDisplay bar = _tableView.BossBar;
            Assert.IsFalse(bar.IsHidden);
            Assert.AreEqual(1300, bar.Value);
            Assert.AreEqual(1300, bar.Start);
            Assert.AreEqual(_table.Game.Rules.SoulThreshold, bar.Limit, "it ends at the soul line");
            Assert.AreEqual(BarLine.Soul, bar.Line);
            Assert.AreEqual(Currency.Bar, _tableView.Currency, "the corner counter goes (the view hides it)");
            Assert.AreEqual("", _tableView.DealerView.Dealer.Title, "the bar sits where the line under the name was");
        }

        [Test]
        public void MammonsInterest_IsDrawnAsItsOwnStep_AfterTheHand()
        {
            SitAtMammon();
            _table.PerformAction();   // the deal
            _tableView.BossBars.Clear();
            _table.Bet(BetAction.Fold);
            int interest = (1300 * MammonInterest.InterestPercent + 99) / 100;
            int after = _table.Game.Years;
            Assert.Greater(after, 1300 + interest, "the fold and the interest");
            int i = _tableView.BossBars.FindIndex(b => b.Value == after - interest);
            Assert.GreaterOrEqual(i, 0, "the fold first");
            Assert.AreEqual(after, _tableView.BossBars.Last().Value, "then the interest");
            Assert.Greater(_tableView.BossBars.FindLastIndex(b => b.Value == after), i);
        }

        [Test]
        public void LucifersBar_EndsAtTheFallLine()
        {
            SitAtLucifer(1050, "AH");
            BossBarDisplay bar = _tableView.BossBar;
            Assert.AreEqual(BarLine.Fall, bar.Line);
            Assert.AreEqual(BossTable.LuciferGate(1050), bar.Limit);
            Assert.AreEqual(1050, bar.Start);
        }

        [Test]
        public void TheMapAndTheGate_ShowTheBarSmall_NoYears()
        {
            Begin();
            Assert.IsFalse(_map.State.Bar.IsHidden);
            Assert.AreEqual(_chapters.Run.Years, _map.State.Bar.Value);
            StringAssert.Contains(UiText.Upper(UiText.GenitiveOf(UiText.Dealer(DealerRoster.MammonId))), _map.State.BarLabel);
            Assert.IsFalse(_map.State.Lines.Any(l => l.Contains(_chapters.Run.Years.ToString())), "no years on the map");
        }

        // ------------------------------------------------------------------ the chapter's sounds

        [Test]
        public void TheMapPlaysItsChaptersMusic_AFloorItsOwn_AndPickingANodeClicks()
        {
            _chapters.Start(SinnerRoster.Peasant);
            Assert.Contains(SfxIds.ChapterTitle, _audio.Sfx, "the chapter's title rings");
            _panel.Press(0);
            Assert.AreEqual(SfxIds.MapMusic(DealerRoster.MammonId), _chapters.MusicId, "the menu plays it as the map shows");
            _map.Press(_map.State.Choices.First());
            Assert.Contains(SfxIds.NodeSelect, _audio.Sfx);
            Assert.AreEqual(SfxIds.FloorMusic(DealerRoster.MammonId), _audio.Music.Last());
        }

        [Test]
        public void AFloorHand_RingsItsCoins_WonOrLost()
        {
            Begin();
            _map.Press(_map.State.Choices.First());
            _table.PerformAction();   // the deal
            _tableView.Sfx.Clear();
            _table.Bet(BetAction.Fold);
            Assert.AreEqual(GamePhase.RoundOver, _table.Game.Phase);
            Assert.IsTrue(_tableView.Sfx.Contains(SfxIds.CoinLoss) || _tableView.Sfx.Contains(SfxIds.CoinLossBig), "a fold loses coins");
            Assert.IsFalse(_tableView.Sfx.Contains(SfxIds.Loss), "not the demo's years");
        }

        [Test]
        public void AnEmptyPurse_SoundsItAndTheDoomsMusic()
        {
            Begin();
            int guard = 0;
            while (guard++ < 5000 && !(_panel.IsOpen && _panel.Card.Title == UiText.PurseEmptyTitle))
            {
                if (_panel.IsOpen) _panel.Press(_panel.Card.Options.Count - 1);
                else if (_chapters.IsAtTable) _table.PerformAction();
                else
                {
                    _chapters.Run.Purse.Add(1 - _chapters.Run.Purse.Coins);
                    _map.Press(_map.State.Choices.First());
                }
            }
            Assert.Contains(SfxIds.PurseEmpty, _audio.Sfx);
            Assert.AreEqual(SfxIds.DoomMusic, _audio.Music.Last());
        }

        [Test]
        public void Salvation_PlaysTheFreedMusic()
        {
            SitAtLucifer(50, "AS AC 8S 8C 2D 9H 9D 4C 5C 6H");
            Assert.AreEqual(DealerRoster.LuciferId, _audio.Music.Last(), "at his table, his music");
            PlayUntilAPanel();
            Assert.AreEqual(SfxIds.FreedMusic, _audio.Music.Last());
        }

        [Test]
        public void ADemonsTable_PlaysHisMusic_TheBarsHit_AndMammonsInterest()
        {
            SitAtMammon();
            Assert.AreEqual(DealerRoster.MammonId, _audio.Music.Last());
            _table.PerformAction();   // the deal
            _tableView.Sfx.Clear();
            _table.Bet(BetAction.Fold);
            Assert.Contains(SfxIds.BarGrow, _tableView.Sfx, "a fold grows the bar");
            Assert.Contains(SfxIds.RuleInterest, _tableView.Sfx, "and Mammon's interest on it");
            Assert.IsFalse(_tableView.Sfx.Contains(SfxIds.BarHit));
        }

        [Test]
        public void TheBarNearItsLine_SoundsTheWarningOnce()
        {
            SitAtMammon();
            var game = (HellPokerGame)_table.Game;
            game.TakeOver(game.Rules.SoulThreshold * 9 / 10, 0);
            _tableView.Sfx.Clear();
            _table.PerformAction();   // the deal: the bar is drawn again
            Assert.AreEqual(1, _tableView.Sfx.Count(s => s == SfxIds.SoulWarning));
            _table.ToggleDiscard(0);
            Assert.AreEqual(1, _tableView.Sfx.Count(s => s == SfxIds.SoulWarning), "once, not every refresh");
        }

        [Test]
        public void ThePeasantKeepingACard_SoundsTheSatchel()
        {
            Begin();
            _map.Press(_map.State.Choices.First());
            Act();
            for (int guard = 0; _table.Game.Phase != GamePhase.Drawing && guard < 10; guard++) Act();
            _table.ToggleDiscard(0);
            Act();   // DRAW
            _tableView.Sfx.Clear();
            _tableView.PressKitCard(0);
            Assert.Contains(SfxIds.SatchelPut, _tableView.Sfx);
        }

        [Test]
        public void TheKingSpendingATax_SoundsTheTreasury()
        {
            Begin(SinnerRoster.King);
            _map.Press(_map.State.Choices.First());
            _chapters.Run.Kit.AddTax(5);
            Act();
            _tableView.Sfx.Clear();
            _tableView.PressKitButton(0);
            Assert.Contains(SfxIds.TaxSpend, _tableView.Sfx);
        }

        [Test]
        public void TheWarlockSeesTheDeck_AtTheDeal()
        {
            Begin(SinnerRoster.Warlock);
            _map.Press(_map.State.Choices.First());
            _tableView.Sfx.Clear();
            Act();   // the deal
            Assert.Contains(SfxIds.ProphecySee, _tableView.Sfx);
        }

        // ------------------------------------------------------------------ the spoils and the fire (Phase 2's build variety)

        [Test]
        public void TheSpoils_TwoOfTheDemonsRelics_AnUpgrade_OrTheCoins()
        {
            _chapters.Dispose();
            var archive = new ChapterArchive(new MemoryStore());
            archive.SaveRun(ChapterPresenter.PlacedSave(SinnerRoster.Peasant, 21, 1, "loot"));
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.AreEqual(UiText.LootTitle, _panel.Card.Title);
            var labels = _panel.Card.Options.Select(o => o.Label).ToList();
            Assert.AreEqual(4, labels.Count, "two relics, an upgrade, the coins");
            Assert.AreEqual(UiText.RelicName(ChapterRelicIds.MammonsSeal), labels[0]);
            Assert.AreEqual(UiText.RelicName(ChapterRelicIds.MammonsScales), labels[1]);
            string upgrade = _chapters.Run.LootUpgrade;
            Assert.AreEqual(string.Format(UiText.LootUpgradeFormat, UiText.UpgradeName(upgrade)), labels[2]);
            StringAssert.StartsWith(UiText.RarityName(RelicRarity.Boss), _panel.Card.Options[0].Detail, "the rarity on the relic's line");
            _panel.Press(2);
            CollectionAssert.AreEqual(new[] { upgrade }, _chapters.Journey.Kit.Upgrades, "the upgrade joins the run");
            Assert.AreEqual(2, _chapters.Journey.Chapter, "on to Belial");
        }

        [Test]
        public void TheFire_OffersAFourthBoon_WhetTheSoul()
        {
            Begin();
            ChapterRun run = _chapters.Run;
            run.Purse.Add(5000);
            int guard = 0;
            while (!(_panel.IsOpen && _panel.Card.Title == UiText.FireTitle) && guard++ < 3000)
            {
                if (_panel.IsOpen) _panel.Press(_panel.Card.Options.Count - 1);
                else if (_chapters.IsAtTable) Act();
                else
                {
                    MapNode fire = run.Choices.FirstOrDefault(n => n.Kind == NodeKind.PurgatoryFire);
                    _map.Press(fire ?? run.Choices.First());
                }
            }
            Assert.AreEqual(UiText.FireTitle, _panel.Card.Title);
            Assert.AreEqual(4, _panel.Card.Options.Count);
            Assert.AreEqual(UiText.FireWhet, _panel.Card.Options[3].Label);
            _panel.Press(3);
            Assert.AreEqual(1, run.Kit.Upgrades.Count);
            StringAssert.Contains(UiText.UpgradeName(run.Kit.Upgrades[0]), _panel.Card.Text, "the fire says what it gave");
        }

        [Test]
        public void ARunsCircle_IsNeverDeeperThanTheClassHasOpen_AndAFreedomTellsWhatOpened()
        {
            _chapters.Start(SinnerRoster.Peasant, 3);
            Assert.AreEqual(0, _chapters.Journey.Circle, "no circle open yet");
            _chapters.Records.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 90);
            _chapters.Start(SinnerRoster.Peasant, 3);
            Assert.AreEqual(1, _chapters.Journey.Circle, "one past the deepest freed");
            Assert.IsTrue(ChapterUnlocks.IsRelicOpen(ChapterRelicIds.AbyssCoin, _chapters.Records));
            Assert.IsNotNull(_chapters.Journey.RelicAvailable, "the run draws only open relics");
        }
    }
}