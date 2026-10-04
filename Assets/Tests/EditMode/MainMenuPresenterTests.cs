using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class MainMenuPresenterTests
    {
        internal sealed class FakeMenuView : IMainMenuView
        {
            public bool IsVisible { get; private set; }
            public bool ContinueShown { get; private set; }

            public event Action NewGamePressed;
            public event Action ContinuePressed;
            public event Action QuitPressed;
            public event Action ChangeTablePressed;
            public event Action SettingsPressed;
            public event Action RecordsPressed;
            public event Action Confirmed;
            public event Action LanguagePressed;

            public void PressLanguage() => LanguagePressed?.Invoke();

            public void PressRecords() => RecordsPressed?.Invoke();

            public string Taunt { get; private set; }
            public string Warning { get; private set; }
            public bool IsConfirming { get; private set; }
            public bool IsShowingRules => RulesOpen;

            public string ConfirmLabel { get; private set; }

            public void AskToConfirm(string taunt, string warning, string confirmLabel)
            {
                Taunt = taunt;
                Warning = warning;
                ConfirmLabel = confirmLabel;
                IsConfirming = true;
            }

            public void Confirm()
            {
                IsConfirming = false;
                Confirmed?.Invoke();
            }

            /// <summary>The rules panel, open over the menu.</summary>
            public bool RulesOpen { get; set; }

            public void Show(bool canContinue)
            {
                IsVisible = true;
                ContinueShown = canContinue;
            }

            public bool CloseOverlay()
            {
                if (IsConfirming)
                {
                    IsConfirming = false;
                    return true;
                }
                if (!RulesOpen) return false;
                RulesOpen = false;
                return true;
            }

            public void PressSettings() => SettingsPressed?.Invoke();

            public void Hide() => IsVisible = false;

            public void PressNewGame() => NewGamePressed?.Invoke();
            public void PressContinue() => ContinuePressed?.Invoke();
            public void PressQuit() => QuitPressed?.Invoke();
            public void PressChangeTable() => ChangeTablePressed?.Invoke();
        }

        internal sealed class FakeDealerSelectView : IDealerSelectView
        {
            public bool IsVisible { get; private set; }
            public IReadOnlyList<DealerChoice> Shown { get; private set; }
            public string Warning { get; private set; }

            public event Action<int> DealerChosen;
            public event Action BackPressed;
            public event Action SeatConfirmed;
            public event Action SeatCancelled;

            public void Show(IReadOnlyList<DealerChoice> dealers)
            {
                IsVisible = true;
                Shown = dealers;
                Warning = null;
            }

            public void AskToConfirm(string warning) => Warning = warning;
            public bool IsConfirming => Warning != null;
            public void CloseConfirm() => Warning = null;

            public string LockedLine { get; private set; }
            public void ShowLockedLine(int index, string line) => LockedLine = line;

            public void Hide() => IsVisible = false;

            public void Choose(int index) => DealerChosen?.Invoke(index);
            public void PressBack() => BackPressed?.Invoke();
            public void Confirm()
            {
                Warning = null;
                SeatConfirmed?.Invoke();
            }

            public void Cancel()
            {
                Warning = null;
                SeatCancelled?.Invoke();
            }
        }

        internal sealed class FakeSession : IRunSession
        {
            public bool CanContinue { get; set; }
            public int NewRuns { get; private set; }
            public int Switches { get; private set; }
            public Dealer Dealer { get; private set; }
            public string CurrentDealerId => Dealer?.Id;

            /// <summary>The sentence the fake player carries; compared with each dealer's soul line.</summary>
            public int Years { get; set; } = 1000;

            /// <summary>When true, the dealer refuses to let the player leave (soul bound).</summary>
            public bool SoulBound { get; set; }

            public event Action LeaveRequested;
            public event Action<RunSummary> RunEnded;

            public HellPoker.Core.Game.RecordBook Records { get; } = new HellPoker.Core.Game.RecordBook();

            /// <summary>What walking away would cost while a run is on.</summary>
            public AbandonRisk Risk { get; set; } = AbandonRisk.Run;
            public AbandonRisk AbandonRisk => CanContinue ? Risk : AbandonRisk.None;
            public int Abandoned { get; private set; }

            public void AbandonRun()
            {
                Abandoned++;
                CanContinue = false;
            }

            public void EndRun(RunSummary summary)
            {
                CanContinue = false;
                RunEnded?.Invoke(summary);
            }

            public HellPoker.Core.Sinners.SinnerClass Class { get; private set; }

            public void StartNewRun(Dealer dealer, HellPoker.Core.Sinners.SinnerClass sinner = null)
            {
                Class = sinner;
                NewRuns++;
                Dealer = dealer;
                CanContinue = true;
            }

            public void RequestLeave()
            {
                if (!SoulBound) LeaveRequested?.Invoke();
            }

            public bool WouldStakeSoul(Dealer dealer) => dealer.TakesSoulAt(Years);

            public void SwitchTable(Dealer dealer)
            {
                Switches++;
                Dealer = dealer;
            }
        }

        internal sealed class FakeQuitter : IApplicationQuitter
        {
            public bool QuitRequested { get; private set; }

            public void Quit() => QuitRequested = true;
        }

        internal sealed class FakeSettingsView : ISettingsView
        {
            public bool IsVisible { get; private set; }
            public string Speed { get; private set; }
            public bool Fullscreen { get; private set; }
            public bool HandGuide { get; private set; }
            public bool TipsLeft { get; private set; }

            public event Action SpeedPressed;
            public event Action FullscreenPressed;
            public event Action HandGuidePressed;
            public event Action ResetTipsPressed;
            public event Action BackPressed;
            public event Action LanguagePressed;
            public event Action MusicPressed;
            public event Action SfxPressed;
            public void PressMusic() => MusicPressed?.Invoke();
            public void PressSfx() => SfxPressed?.Invoke();
            public string Music { get; private set; }
            public string SfxVolume { get; private set; }
            public string Language { get; private set; }
            public void PressLanguage() => LanguagePressed?.Invoke();

            public void Render(string speed, bool fullscreen, bool handGuide, bool tipsLeft, string language, string music, string sfx)
            {
                Music = music;
                SfxVolume = sfx;
                Language = language;
                Speed = speed;
                Fullscreen = fullscreen;
                HandGuide = handGuide;
                TipsLeft = tipsLeft;
            }

            public void Show() => IsVisible = true;
            public void Hide() => IsVisible = false;

            public void PressSpeed() => SpeedPressed?.Invoke();
            public void PressFullscreen() => FullscreenPressed?.Invoke();
            public void PressHandGuide() => HandGuidePressed?.Invoke();
            public void PressResetTips() => ResetTipsPressed?.Invoke();
            public void PressBack() => BackPressed?.Invoke();
        }

        internal sealed class FakeEndScreen : IEndScreenView
        {
            public bool IsVisible { get; private set; }
            public RunSummary Summary { get; private set; }
            public event Action NewGamePressed;
            public event Action MenuPressed;

            public void Show(RunSummary summary)
            {
                IsVisible = true;
                Summary = summary;
            }

            public void Hide() => IsVisible = false;
            public void PressNewGame() => NewGamePressed?.Invoke();
            public void PressMenu() => MenuPressed?.Invoke();
        }

        internal sealed class FakeRecords : IRecordsView
        {
            public bool IsVisible { get; private set; }
            public int DealersShown { get; private set; }
            public event Action BackPressed;

            public void Show(HellPoker.Core.Game.RecordBook records, IReadOnlyList<DealerCard> dealers)
            {
                IsVisible = true;
                DealersShown = dealers.Count;
            }

            public void Hide() => IsVisible = false;
            public void PressBack() => BackPressed?.Invoke();
        }

        internal sealed class FakeTransition : IScreenTransition
        {
            public int Played { get; private set; }
            public bool IsPlaying { get; set; }
            public void Play() => Played++;
        }

        private FakeMenuView _menu;
        private FakeDealerSelectView _dealerSelect;
        private FakeSettingsView _settings;
        private FakeTransition _transition;
        private FakeEndScreen _end;
        private FakeRecords _records;
        private FakeTableView _table;
        private FakeSession _session;
        private FakeQuitter _quitter;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new FakeMenuView();
            _dealerSelect = new FakeDealerSelectView();
            _table = new FakeTableView();
            _session = new FakeSession();
            _quitter = new FakeQuitter();
            _settings = new FakeSettingsView();
            _transition = new FakeTransition();
            _end = new FakeEndScreen();
            _records = new FakeRecords();
            _presenter = new MainMenuPresenter(_menu, _dealerSelect, _settings, _end, _records, _table, _session, _quitter, _transition,
                DealerRoster.All);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private void StartRunWith(int dealerIndex = 0)
        {
            _menu.PressNewGame();
            _dealerSelect.Choose(dealerIndex);
        }

        /// <summary>The same menu with Lucifer waiting below.</summary>
        private void WithLucifer()
        {
            _presenter.Dispose();
            _presenter = new MainMenuPresenter(_menu, _dealerSelect, _settings, _end, _records, _table, _session, _quitter, _transition,
                DealerRoster.All, DealerRoster.Lucifer);
        }

        [Test]
        public void Lucifer_IsTheFourthCard_Locked()
        {
            WithLucifer();

            _menu.PressNewGame();

            Assert.AreEqual(4, _dealerSelect.Shown.Count);
            DealerChoice lucifer = _dealerSelect.Shown[3];
            Assert.AreEqual("lucifer", lucifer.Card.Id);
            Assert.IsTrue(lucifer.IsLocked);
            Assert.AreEqual("THE MORNING STAR", lucifer.Card.Name);
            Assert.AreEqual("Waits below 250 years", lucifer.Card.Title);
            Assert.IsFalse(_dealerSelect.Shown[0].IsLocked);
        }

        [Test]
        public void ChoosingLucifer_SeatsNobody_HeAnswersFromTheDark()
        {
            WithLucifer();
            _menu.PressNewGame();

            _dealerSelect.Choose(3);

            Assert.AreEqual(0, _session.NewRuns);
            Assert.IsTrue(_dealerSelect.IsVisible, "Still choosing.");
            Assert.AreEqual("Not yet. Come down to me.", _dealerSelect.LockedLine);
        }

        [Test]
        public void ChangingTables_StillShowsHimLocked()
        {
            WithLucifer();
            StartRunWith(0);

            _session.RequestLeave();

            Assert.AreEqual(4, _dealerSelect.Shown.Count);
            Assert.IsTrue(_dealerSelect.Shown[3].IsLocked);
            _dealerSelect.Choose(3);
            Assert.AreEqual(0, _session.Switches);
        }

        [Test]
        public void GameStartsOnTheMenu_WithoutContinue()
        {
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_menu.ContinueShown);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void NewGame_OpensDealerChoice_WithEveryDealer()
        {
            _menu.PressNewGame();

            Assert.IsTrue(_dealerSelect.IsVisible);
            Assert.IsFalse(_menu.IsVisible);
            Assert.IsFalse(_table.Visible);
            Assert.AreEqual(0, _session.NewRuns, "Nothing starts until a dealer is chosen.");
            CollectionAssert.AreEqual(new[] { "MAMMON", "BELIAL", "LILITH" }, new[] { _dealerSelect.Shown[0].Card.Name, _dealerSelect.Shown[1].Card.Name, _dealerSelect.Shown[2].Card.Name });
        }

        [Test]
        public void ChoosingADealer_StartsRunAtTheirTable()
        {
            StartRunWith(2);

            Assert.AreEqual(1, _session.NewRuns);
            Assert.AreEqual(DealerRoster.LilithId, _session.Dealer.Id);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void DealerChoice_Back_ReturnsToMenu()
        {
            _menu.PressNewGame();

            _dealerSelect.PressBack();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.AreEqual(0, _session.NewRuns);
        }

        [Test]
        public void Escape_OnDealerChoice_ReturnsToMenu()
        {
            _menu.PressNewGame();
            Assert.IsTrue(_presenter.IsMenuOpen, "The table does not take input while choosing a dealer.");

            _presenter.GoBack();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_dealerSelect.IsVisible);
        }

        [Test]
        public void MenuButton_OpensMenu_WithContinue_DuringARun()
        {
            StartRunWith();

            _table.PressMenu();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsTrue(_menu.ContinueShown);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void Continue_ReturnsToTable_WithoutRestarting()
        {
            StartRunWith();
            _table.PressMenu();

            _menu.PressContinue();

            Assert.AreEqual(1, _session.NewRuns);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void Escape_TogglesBetweenTableAndMenu()
        {
            StartRunWith();

            _presenter.GoBack();
            Assert.IsTrue(_presenter.IsMenuOpen);

            _presenter.GoBack();
            Assert.IsFalse(_presenter.IsMenuOpen);
        }

        [Test]
        public void Escape_OnFirstMenu_StaysThere()
        {
            _presenter.GoBack();

            Assert.IsTrue(_presenter.IsMenuOpen);
            Assert.IsFalse(_table.Visible);
        }

        // ------------------------------------------------------------------ changing tables

        [Test]
        public void NewRunChoice_FlagsNothing()
        {
            _menu.PressNewGame();

            foreach (DealerChoice choice in _dealerSelect.Shown)
                Assert.IsFalse(choice.IsCurrent || choice.SoulAtStake);
        }

        [Test]
        public void LeaveRequest_OpensTheChoice_WithTheCurrentDealerAndSoulFlags()
        {
            StartRunWith(0);
            _session.Years = 1600;

            _session.RequestLeave();

            Assert.IsTrue(_dealerSelect.IsVisible);
            Assert.IsFalse(_table.Visible);
            Assert.IsTrue(_dealerSelect.Shown[0].IsCurrent);
            Assert.IsFalse(_dealerSelect.Shown[0].SoulAtStake, "Mammon takes the soul at 2000.");
            Assert.IsFalse(_dealerSelect.Shown[1].SoulAtStake, "Belial at 1750.");
            Assert.IsTrue(_dealerSelect.Shown[2].SoulAtStake, "Lilith at 1500.");
        }

        [Test]
        public void ChangingToASafeTable_SwitchesAndKeepsTheRun()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.Choose(1);

            Assert.AreEqual(1, _session.NewRuns, "No new run: the sentence goes along.");
            Assert.AreEqual(1, _session.Switches);
            Assert.AreEqual(DealerRoster.BelialId, _session.CurrentDealerId);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void ChoosingTheCurrentDealer_ReturnsWithoutSwitching()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.Choose(0);

            Assert.AreEqual(0, _session.Switches);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void SittingPastASoulLine_NeedsConfirmation()
        {
            StartRunWith(0);
            _session.Years = 1600;
            _session.RequestLeave();

            _dealerSelect.Choose(2);

            Assert.AreEqual(0, _session.Switches, "Not before the warning is accepted.");
            Assert.IsNotEmpty(_dealerSelect.Warning);
            Assert.IsTrue(_dealerSelect.IsVisible);

            _dealerSelect.Confirm();

            Assert.AreEqual(1, _session.Switches);
            Assert.AreEqual(DealerRoster.LilithId, _session.CurrentDealerId);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void CancellingTheWarning_StaysOnTheChoice()
        {
            StartRunWith(0);
            _session.Years = 1600;
            _session.RequestLeave();
            _dealerSelect.Choose(2);

            _dealerSelect.Cancel();
            _dealerSelect.Confirm();

            Assert.AreEqual(0, _session.Switches, "A confirm after cancelling does nothing.");
            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        [Test]
        public void BackWhileChangingTables_ReturnsToTheTable()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.PressBack();

            Assert.IsTrue(_table.Visible);
            Assert.IsFalse(_menu.IsVisible);
        }

        [Test]
        public void MenuChangeTable_OpensTheChoice()
        {
            StartRunWith(0);
            _table.PressMenu();

            _menu.PressChangeTable();

            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        [Test]
        public void MenuChangeTable_WhenSoulBound_StaysAtTheTable()
        {
            StartRunWith(0);
            _session.SoulBound = true;
            _table.PressMenu();

            _menu.PressChangeTable();

            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsTrue(_table.Visible, "The dealer answers at the table.");
        }

        // ------------------------------------------------------------------ Esc and sub-screens

        [Test]
        public void Settings_OpensFromTheMenu_AndBackReturns()
        {
            _menu.PressSettings();
            Assert.IsTrue(_settings.IsVisible);
            Assert.IsFalse(_menu.IsVisible);
            Assert.IsTrue(_presenter.IsMenuOpen);

            _settings.PressBack();
            Assert.IsFalse(_settings.IsVisible);
            Assert.IsTrue(_menu.IsVisible);
        }

        [Test]
        public void Escape_OnSettings_ReturnsToMenu()
        {
            _menu.PressSettings();

            _presenter.GoBack();

            Assert.IsFalse(_settings.IsVisible);
            Assert.IsTrue(_menu.IsVisible);
        }

        [Test]
        public void Escape_OnTheRules_ClosesThemFirst()
        {
            StartRunWith();
            _table.PressMenu();
            _menu.RulesOpen = true;

            _presenter.GoBack();
            Assert.IsFalse(_menu.RulesOpen);
            Assert.IsTrue(_menu.IsVisible, "Still on the menu, not back at the table yet.");

            _presenter.GoBack();
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void Escape_OnTheWarning_ClosesItAndStaysOnTheChoice()
        {
            StartRunWith(0);
            _session.Years = 1600;
            _session.RequestLeave();
            _dealerSelect.Choose(2);

            _presenter.GoBack();

            Assert.IsFalse(_dealerSelect.IsConfirming);
            Assert.IsTrue(_dealerSelect.IsVisible);
            _dealerSelect.Confirm();
            Assert.AreEqual(0, _session.Switches, "The warning was dismissed: no late seat.");
        }

        [Test]
        public void Escape_WhileChangingTables_ReturnsToTheTable()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _presenter.GoBack();

            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void EveryScreenChange_PlaysTheTransition()
        {
            int before = _transition.Played;
            _menu.PressNewGame();
            _dealerSelect.Choose(0);
            _table.PressMenu();
            _menu.PressSettings();

            Assert.AreEqual(before + 4, _transition.Played);
        }

        // ------------------------------------------------------------------ end of a run, records

        private static RunSummary Summary(bool absolved) =>
            new RunSummary(absolved, 12, 0, 1400, HellPoker.Core.Evaluation.HandCategory.Flush, new[] { "MAMMON" }, false);

        [Test]
        public void EndOfARun_ShowsTheEndScreen()
        {
            StartRunWith();

            _session.EndRun(Summary(true));

            Assert.IsTrue(_end.IsVisible);
            Assert.IsTrue(_end.Summary.Absolved);
            Assert.IsFalse(_table.Visible);
            Assert.IsTrue(_presenter.IsMenuOpen);
        }

        [Test]
        public void EndScreen_NewGame_OpensTheDealerChoice()
        {
            StartRunWith();
            _session.EndRun(Summary(false));

            _end.PressNewGame();

            Assert.IsFalse(_end.IsVisible);
            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        [Test]
        public void EndScreen_Menu_AndEsc_GoToTheMenu_WithoutContinue()
        {
            StartRunWith();
            _session.EndRun(Summary(false));

            _presenter.GoBack();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_menu.ContinueShown, "A finished run cannot be continued.");
            Assert.IsFalse(_end.IsVisible);
        }

        [Test]
        public void Records_OpenFromTheMenu_ForEveryDealer_AndGoBack()
        {
            _menu.PressRecords();
            Assert.IsTrue(_records.IsVisible);
            Assert.AreEqual(3, _records.DealersShown);

            _records.PressBack();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_records.IsVisible);
        }

        // ------------------------------------------------------------------ New Game over a run in progress

        [Test]
        public void NewGame_DuringARun_AsksFirst_WithTheDemonsScorn()
        {
            StartRunWith(0);
            _table.PressMenu();

            _menu.PressNewGame();

            Assert.IsTrue(_menu.IsConfirming);
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_dealerSelect.IsVisible, "Nothing happens before the player answers.");
            Assert.AreEqual(0, _session.Abandoned);
            CollectionAssert.Contains(new[]
            {
                "Leaving with your account open? Afraid of the interest?",
                "Run along. But this is Hell, debtor. Where would you go?",
                "Scared of a few numbers? Close the book, then. I keep a copy."
            }, _menu.Taunt);
            Assert.AreEqual("Abandon this run? Your sentence will be forgotten.", _menu.Warning);
        }

        [Test]
        public void ConfirmingNewGame_AbandonsTheRun_AndOpensTheChoice()
        {
            StartRunWith(1);
            _table.PressMenu();
            _menu.PressNewGame();

            _menu.Confirm();

            Assert.AreEqual(1, _session.Abandoned);
            Assert.IsTrue(_dealerSelect.IsVisible);
            _dealerSelect.PressBack();
            Assert.IsFalse(_menu.ContinueShown, "The abandoned run cannot be continued.");
        }

        [Test]
        public void Escape_OnTheNewGameWarning_ClosesIt_AndKeepsTheRun()
        {
            StartRunWith(0);
            _table.PressMenu();
            _menu.PressNewGame();

            _presenter.GoBack();

            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsTrue(_menu.IsVisible, "Still on the menu.");
            Assert.AreEqual(0, _session.Abandoned);
            _presenter.GoBack();
            Assert.IsTrue(_table.Visible, "The run goes on.");
        }

        [TestCase(AbandonRisk.Hand, "Abandon this run? The hand on the table counts as folded.")]
        [TestCase(AbandonRisk.Soul, "Your soul is on the table. Walking away counts as damnation.")]
        public void TheWarning_SaysWhatWalkingAwayCosts(AbandonRisk risk, string warning)
        {
            StartRunWith(2);
            _session.Risk = risk;
            _table.PressMenu();

            _menu.PressNewGame();

            Assert.AreEqual(warning, _menu.Warning);
        }

        [Test]
        public void NewGame_WithNoRun_GoesStraightToTheChoice()
        {
            _menu.PressNewGame();

            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        // ------------------------------------------------------------------ Quit over a hand in progress

        [TestCase(AbandonRisk.Hand)]
        [TestCase(AbandonRisk.Soul)]
        public void QuitMidHand_AsksFirst_InTheDemonsVoice(AbandonRisk risk)
        {
            StartRunWith(0);
            _session.Risk = risk;
            _table.PressMenu();

            _menu.PressQuit();

            Assert.IsFalse(_quitter.QuitRequested, "Not before the player answers.");
            Assert.IsTrue(_menu.IsConfirming);
            Assert.AreEqual("Leave now and the hand is lost.", _menu.Warning);
            Assert.AreEqual("QUIT", _menu.ConfirmLabel);
            CollectionAssert.Contains(new[]
            {
                "You walked out mid-hand? The ledger noticed. Debited, with interest.",
                "Skipping out on an open account? I charged it as forfeit."
            }, _menu.Taunt);

            _menu.Confirm();

            Assert.IsTrue(_quitter.QuitRequested);
            Assert.AreEqual(0, _session.Abandoned, "Quitting is not a new game: the hand is forfeited on the next launch.");
        }

        [Test]
        public void QuitMidHand_Escape_KeepsPlaying()
        {
            StartRunWith(0);
            _session.Risk = AbandonRisk.Hand;
            _table.PressMenu();
            _menu.PressQuit();

            _presenter.GoBack();

            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsFalse(_quitter.QuitRequested);
        }

        [Test]
        public void QuitBetweenHands_GoesAtOnce()
        {
            StartRunWith(0);
            _session.Risk = AbandonRisk.Run;
            _table.PressMenu();

            _menu.PressQuit();

            Assert.IsTrue(_quitter.QuitRequested);
            Assert.IsFalse(_menu.IsConfirming);
        }

        [Test]
        public void NewGameWarning_StillAbandons_NotQuits()
        {
            StartRunWith(0);
            _table.PressMenu();
            _menu.PressNewGame();
            Assert.AreEqual("ABANDON", _menu.ConfirmLabel);

            _menu.Confirm();

            Assert.AreEqual(1, _session.Abandoned);
            Assert.IsFalse(_quitter.QuitRequested);
        }

        [Test]
        public void Quit_AsksTheApplicationToQuit()
        {
            _menu.PressQuit();

            Assert.IsTrue(_quitter.QuitRequested);
        }
    }
}
