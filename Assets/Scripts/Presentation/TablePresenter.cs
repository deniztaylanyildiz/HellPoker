using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Translates player intent into game commands and game state into view updates.
    /// Holds only UI state (selected discards); all rules — including the size of every bet — live in the game.
    /// Each table is a game built for its dealer; changing tables carries the sentence over. Before the first run there is
    /// no game and input is ignored. While the soul is on the table no number of years ever reaches the view: the soul
    /// bar shows shares of a soul instead. Depends on abstractions only, so it can be tested without Unity scenes.
    /// </summary>
    public sealed class TablePresenter : ITableCommands, IRunSession, IDisposable
    {
        private const float ShowdownPause = 0.6f;

        /// <summary>A loss of this many betting units or more is felt (<see cref="TableMoment.BigLoss"/>).</summary>
        public const int BigLossUnits = 4;

        private readonly Func<Dealer, IHellPokerGame> _createGame;
        private readonly ITableView _view;
        private readonly IGuideSettings _guide;
        private readonly RunArchive _archive;
        private readonly HashSet<int> _discards = new HashSet<int>();
        private readonly RecordBook _records;

        private IHellPokerGame _game;
        private Dealer _dealer;
        private DealerText _dealerText;
        private bool _finalStretchAnnounced;
        private bool _soulShown;
        private RunStats _stats;
        private int _settledRound = -1;

        public event Action LeaveRequested;
        public event Action<RunSummary> RunEnded;

        /// <param name="createGame">Builds a fresh game for a run at this dealer's table.</param>
        /// <param name="guide">The player's guide settings; without them the hand guide is on and no first-game tips are told.</param>
        /// <param name="archive">Where the run is saved after every hand and the records are kept; without it nothing is saved.</param>
        public TablePresenter(Func<Dealer, IHellPokerGame> createGame, ITableView view, IGuideSettings guide = null, RunArchive archive = null)
        {
            _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _guide = guide;
            _archive = archive;
            _records = archive?.LoadRecords() ?? new RecordBook();

            _view.ActionPressed += PerformAction;
            _view.BetPressed += Bet;
            _view.LeavePressed += RequestLeave;
            _view.HandRanksPressed += ToggleHandRanks;
            _view.Player.CardClicked += ToggleDiscard;
        }

        public IReadOnlyCollection<int> SelectedDiscards => _discards;

        /// <summary>The game of the current table; null before the first run.</summary>
        public IHellPokerGame Game => _game;

        public bool CanContinue => _game != null && _game.RoundNumber > 0 && !_game.IsGameOver;

        public string CurrentDealerId => _dealer?.Id;

        public RecordBook Records => _records;

        /// <summary>This run's story so far; null before the first run.</summary>
        public RunStats Stats => _stats;

        public void StartNewRun(Dealer dealer)
        {
            SeatAt(dealer, carriedYears: null, roundsPlayed: 0);
            BeginRun();
            _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            Refresh();
        }

        /// <summary>Picks up a saved run between hands: the same demon, sentence, hands and story.</summary>
        public void Resume(Dealer dealer, RunSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SeatAt(dealer, snapshot.Years, snapshot.RoundsPlayed);
            _stats = snapshot.Stats;
            _stats.SatWith(dealer.Id);
            _settledRound = _game.RoundNumber;
            if (_game.IsGameOver)
            {
                _archive?.ClearRun();   // a finished run is not continued
            }
            else if (!_game.IsSoulAtStake)
            {
                _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            }
            Refresh();
        }

        private void BeginRun()
        {
            _stats = new RunStats(_game.Years, _dealer.Id);
            _settledRound = _game.RoundNumber;
            _records.RunStarted();
            _archive?.SaveRecords(_records);
            SaveRun();
        }

        private void SaveRun()
        {
            if (_archive == null || _stats == null || _game.IsGameOver || _game.Phase != GamePhase.Betting) return;
            _archive.SaveRun(new RunSnapshot(_dealer.Id, _game.Years, _game.RoundNumber, _stats));
        }

        /// <summary>Once per finished hand: the story grows, and the run is saved — or, when it is over, the records are.</summary>
        private void SettleHand()
        {
            if (_stats == null || _game.RoundNumber == _settledRound) return;
            bool settled = _game.Phase == GamePhase.RoundOver || _game.IsGameOver;
            if (!settled || _game.LastRound == null) return;

            _settledRound = _game.RoundNumber;
            RoundResult round = _game.LastRound;
            _stats.RecordHand(_game.Years, round.Folded ? (HandCategory?)null : round.Showdown.Player.Category,
                _game.IsSoulAtStake || _game.IsSoulHand);

            if (_game.IsGameOver)
            {
                _records.RunEnded(_game.Phase == GamePhase.Absolved, _dealer.Id, _stats.HandsPlayed);
                _archive?.SaveRecords(_records);
                _archive?.ClearRun();
            }
            else if (_archive != null)
            {
                // Saved as it will be at the next deal, so a quit between hands loses nothing and gains nothing.
                _archive.SaveRun(new RunSnapshot(_dealer.Id, _game.Years, _game.RoundNumber, _stats));
            }
        }

        private RunSummary Summary()
        {
            return new RunSummary(_game.Phase == GamePhase.Absolved, _stats.HandsPlayed, _stats.LowestYears, _stats.HighestYears,
                _stats.BestHand, _stats.Dealers.Select(id => UiText.Dealer(id).Name).ToArray(), _stats.SoulStaked);
        }

        public bool WouldStakeSoul(Dealer dealer)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));
            return _game != null && dealer.TakesSoulAt(_game.Years);
        }

        public void SwitchTable(Dealer dealer)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));
            if (_game == null)
            {
                StartNewRun(dealer);
                return;
            }

            SeatAt(dealer, _game.Years, _game.RoundNumber);
            _settledRound = _game.RoundNumber;
            if (_stats == null)
                BeginRun();
            _stats.SatWith(dealer.Id);
            _stats.Note(_game.Years, _game.IsSoulAtStake);
            SaveRun();
            if (!_game.IsSoulAtStake)
                _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            Refresh();
        }

        public void RequestLeave()
        {
            if (_game == null || Hurry() || _game.IsGameOver) return;

            // A finished hand counts as "between hands": move on to the next one first.
            if (_game.Phase == GamePhase.RoundOver)
            {
                _game.NextRound();
                Refresh();
            }

            if (_game.CanLeaveTable(out string reason))
                LeaveRequested?.Invoke();
            else if (_game.IsSoulAtStake && _game.Phase == GamePhase.Betting)
                _view.Dealer.Say(UiText.Pick(_dealerText.SoulLocked, _game.RoundNumber), DealerMood.Menacing);
            else
                _view.SetMessage(reason, Tone.Warning);
        }

        /// <summary>Builds the dealer's game, carries the sentence over if moving from another table, and dresses the table.</summary>
        private void SeatAt(Dealer dealer, int? carriedYears, int roundsPlayed)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));

            IHellPokerGame game = _createGame(dealer) ?? throw new InvalidOperationException("The game factory returned no game.");
            if (carriedYears.HasValue)
                game.TakeOver(carriedYears.Value, roundsPlayed);

            _game = game;
            _dealer = dealer;
            _discards.Clear();
            _finalStretchAnnounced = false;
            _soulShown = false;
            _dealerText = UiText.Dealer(dealer.Id);

            _view.Dealer.SetDealer(DealerCards.Describe(dealer));
            _view.Payouts.SetTable(dealer.Payouts);
            _view.SetSoul(SoulGauge.Hidden);
            if (!_game.IsSoulAtStake)
            {
                _view.Sentence.SetSoulLine(_game.Rules.SoulThreshold);
                _view.Sentence.SetYears(_game.Years, animate: false);
            }
        }

        public void Dispose()
        {
            _view.ActionPressed -= PerformAction;
            _view.BetPressed -= Bet;
            _view.LeavePressed -= RequestLeave;
            _view.HandRanksPressed -= ToggleHandRanks;
            _view.Player.CardClicked -= ToggleDiscard;
        }

        public void PerformAction()
        {
            if (_game == null || Hurry()) return;

            switch (_game.Phase)
            {
                case GamePhase.Betting:
                    _game.PlaceBet();
                    _discards.Clear();
                    break;
                case GamePhase.PlayerReveal:
                case GamePhase.DrawReveal:
                case GamePhase.HouseReveal:
                    Bet(BetAction.Pass);
                    return;
                case GamePhase.HouseReRaise:
                    Bet(BetAction.Call);
                    return;
                case GamePhase.Drawing:
                    _game.Draw(_discards.ToArray());
                    _discards.Clear();
                    break;
                case GamePhase.RoundOver:
                    _game.NextRound();
                    break;
                default:
                    // The run is over: the end screen takes it from here (or, with nobody listening, a fresh run).
                    if (RunEnded != null)
                    {
                        RunEnded(Summary());
                        return;
                    }

                    _game.Restart();
                    _finalStretchAnnounced = false;
                    _soulShown = false;
                    _view.SetSoul(SoulGauge.Hidden);
                    _view.Sentence.SetSoulLine(_game.Rules.SoulThreshold);
                    BeginRun();
                    _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
                    break;
            }

            Refresh();
        }

        public void Bet(BetAction action)
        {
            if (_game == null || Hurry()) return;

            if (!_game.CanBet(action, out _))
            {
                string why = LockedReason(action);
                if (why != null)
                    _view.SetMessage(why, Tone.Warning);
                return;
            }

            _game.Bet(action);
            if (_game.Phase == GamePhase.HouseReRaise && !Tip(UiText.TipFirstReRaise))
                _view.Dealer.Say(UiText.Pick(_dealerText.ReRaise, _game.RoundNumber), DealerMood.Scheming);
            Refresh();
        }

        public void ToggleDiscard(int index)
        {
            if (_game == null || Hurry() || _game.Phase != GamePhase.Drawing) return;

            if (!_discards.Remove(index))
            {
                _discards.Add(index);
                if (!_game.CanDraw(_discards, out string reason))
                {
                    _discards.Remove(index);
                    _view.SetMessage(reason, Tone.Warning);
                    return;
                }
            }

            Refresh();
        }

        private static bool IsBetPhase(GamePhase phase)
        {
            return phase == GamePhase.PlayerReveal || phase == GamePhase.DrawReveal || phase == GamePhase.HouseReveal ||
                   phase == GamePhase.HouseReRaise;
        }

        /// <summary>
        /// A press while the table is still animating means "hurry up": the animations jump to their end and the press is
        /// spent on that, so one press never acts on a table the player has not seen yet. True when the press was spent.
        /// </summary>
        private bool Hurry()
        {
            if (!_view.IsBusy) return false;
            _view.SkipAnimations();
            return true;
        }

        public void ToggleHandRanks()
        {
            if (_game == null) return;
            if (_view.HandRanksOpen)
                _view.HideHandRanks();
            else
                _view.ShowHandRanks(_dealer.Payouts);
        }

        public bool CloseOverlay()
        {
            if (!_view.HandRanksOpen) return false;
            _view.HideHandRanks();
            return true;
        }

        private bool GuideOn => _guide?.HandGuide ?? true;

        /// <summary>The first time a moment comes up, the dealer explains it in one line (instead of their usual remark).</summary>
        /// <returns>True when a tip was told.</returns>
        private bool Tip(string tip)
        {
            if (_guide == null || _guide.HasSeenTip(tip)) return false;
            string text = UiText.TipText(tip, _game.Rules.ForcedRaiseYears);
            if (text == null) return false;
            _guide.MarkTipSeen(tip);
            _view.Dealer.Say(text, DealerMood.Neutral);
            return true;
        }

        /// <summary>The player's caption: what the face-up cards make right now (hand guide), or just "YOUR HAND".</summary>
        private void ShowPlayerCaption()
        {
            HandCategory? now = _game.PlayerHandNow;
            if (GuideOn && now.HasValue)
                _view.Player.SetCaption(string.Format(UiText.HandNowFormat, UiText.CategoryName(now.Value).ToUpperInvariant()), Tone.Neutral);
            else
                _view.Player.SetCaption(UiText.PlayerCaption, Tone.Muted);
        }

        /// <summary>Why a bet button is locked, in the table's own words; null when there is nothing to say.</summary>
        private string LockedReason(BetAction action)
        {
            if (!IsBetPhase(_game.Phase)) return null;

            if (_game.Phase == GamePhase.HouseReRaise)
                return SoulMode ? UiText.LockedAnswerSoul : UiText.LockedAnswer;

            switch (action)
            {
                case BetAction.Raise:
                    if (_game.WagerLeft == 0) return SoulMode ? UiText.LockedAllOfIt : UiText.LockedAllIn;
                    return UiText.LockedTableFull;
                case BetAction.Pass:
                    return string.Format(UiText.LockedPassFormat, _game.Rules.ForcedRaiseYears);
                case BetAction.Call:
                    return UiText.LockedNothingToCall;
                default:
                    return null;
            }
        }

        /// <summary>True while numbers must stay hidden: the soul is on the table, or this hand was dealt with it there.</summary>
        private bool SoulMode => _game.IsSoulAtStake || _game.IsSoulHand;

        private void Refresh()
        {
            switch (_game.Phase)
            {
                case GamePhase.Betting:
                    ShowBetting();
                    break;
                case GamePhase.PlayerReveal:
                    ShowDecision(string.Format(UiText.PromptPlayerCardFormat, _game.PlayerCardsRevealed, Hand.Size));
                    break;
                case GamePhase.Drawing:
                    ShowDrawing();
                    break;
                case GamePhase.DrawReveal:
                    ShowDecision(UiText.PromptAfterDraw);
                    break;
                case GamePhase.HouseReveal:
                    ShowDecision(string.Format(UiText.PromptHouseShowsFormat, _game.HouseCardsRevealed, Hand.Size));
                    break;
                case GamePhase.HouseReRaise:
                    ShowReRaise();
                    break;
                default:
                    ShowResult(_game.LastRound);
                    break;
            }

            SettleHand();
            _view.SetLeave(_game.Phase != GamePhase.Betting ? LeaveState.Hidden
                : _game.IsSoulAtStake ? LeaveState.Locked : LeaveState.Open);
            _view.SetFinalStretch(_game.IsRaiseForced, string.Format(UiText.FinalStretchBannerFormat, _game.Rules.ForcedRaiseYears));
            AnnounceSoul();

            // The dealer remarks once when the player first reaches the gates (unless the run just ended).
            if (_game.IsRaiseForced && !_finalStretchAnnounced && !_game.IsGameOver)
            {
                _finalStretchAnnounced = true;
                if (!Tip(UiText.TipFinalStretch))
                    _view.Dealer.Say(UiText.Pick(_dealerText.FinalStretch, _game.RoundNumber), DealerMood.Menacing);
            }
        }

        /// <summary>The moment the soul goes on the table — or comes back — gets its own line.</summary>
        private void AnnounceSoul()
        {
            if (_game.IsGameOver) return;

            if (_game.IsSoulAtStake && !_soulShown)
            {
                _soulShown = true;
                if (!Tip(UiText.TipSoul))
                    _view.Dealer.Say(UiText.Pick(_dealerText.SoulTaken, _game.RoundNumber), DealerMood.Gloating);
            }
            else if (!_game.IsSoulAtStake && _soulShown)
            {
                _soulShown = false;
                _view.SetSoul(SoulGauge.Hidden);
                _view.Sentence.SetSoulLine(_game.Rules.SoulThreshold);
                _view.Sentence.SetYears(_game.Years, animate: true);
                _view.Dealer.Say(UiText.Pick(_dealerText.SoulReleased, _game.RoundNumber), DealerMood.Annoyed);
            }
        }

        private void ShowBetting()
        {
            _view.SetBetControls(BetControls.Hidden);
            _view.Player.SetInteractable(false);
            _view.Player.SetSelection(null);
            _view.Player.SetHints(null);
            _view.Payouts.Highlight(null);
            _view.House.Show(Slots(null, 0));
            _view.Player.Show(Slots(null, 0));
            _view.House.SetCaption(UiText.HouseCaption, Tone.Muted);
            _view.Player.SetCaption(UiText.PlayerCaption, Tone.Muted);
            _view.SetPot(0);

            if (SoulMode)
            {
                ShowSoul(_game.UpcomingAnte);
                _view.SetStakeInfo(UiText.SoulStakeInfo);
                _view.SetAnte(0);
                _view.SetMessage(UiText.SoulPromptBet, Tone.Warning);
            }
            else
            {
                _view.Sentence.SetYears(_game.Years, animate: true);
                ShowStakeInfo();
                _view.SetAnte(_game.UpcomingAnte);
                _view.SetMessage(string.Format(UiText.PromptBetFormat, _game.UpcomingAnte), Tone.Neutral);
            }
            _view.SetAction(UiText.Deal);
        }

        /// <summary>Cards were just turned: animate them, then offer Raise / Pass / Fold.</summary>
        private void ShowDecision(string prompt)
        {
            ShowHandInPlay();
            _view.SetAction(null);

            bool mustRaise = !_game.CanBet(BetAction.Pass, out _);
            _view.SetMessage(prompt + (mustRaise ? UiText.PromptForcedChoice : UiText.PromptChoice), mustRaise ? Tone.Warning : Tone.Neutral);
            _view.SetBetControls(new BetControls(true, RaiseLabel(), _game.CanBet(BetAction.Raise, out _), !mustRaise));
            if (_game.Phase == GamePhase.PlayerReveal)
                Tip(UiText.TipFirstDecision);
        }

        /// <summary>The house raised back: Call or Fold.</summary>
        private void ShowReRaise()
        {
            ShowHandInPlay();
            _view.SetAction(null);
            if (SoulMode)
            {
                ShowSoul(_game.CurrentStake + _game.HouseReRaiseAmount);
                _view.SetMessage(UiText.SoulPromptReRaise, Tone.Warning);
                _view.SetBetControls(BetControls.Answer(UiText.MatchIt));
            }
            else
            {
                _view.SetMessage(string.Format(UiText.PromptReRaiseFormat, _game.HouseReRaiseAmount), Tone.Warning);
                _view.SetBetControls(BetControls.Answer(string.Format(UiText.CallFormat, _game.HouseReRaiseAmount)));
            }
        }

        /// <summary>Common to every decision: cards as the game shows them, the stake on the table, no ante or discards.</summary>
        private void ShowHandInPlay()
        {
            _view.SetAnte(0);
            _view.SetBetControls(BetControls.Hidden);
            _view.Player.SetInteractable(false);
            _view.Player.SetSelection(null);

            _view.Player.SetHints(null);

            // House cards first, so on the deal the table fills up before the player's cards turn.
            _view.House.Show(Slots(_game.HouseHand, _game.HouseCardsRevealed));
            _view.Player.Show(Slots(_game.PlayerHand, _game.PlayerCardsRevealed));
            ShowPlayerCaption();
            ShowStakeOnTable();
        }

        private string RaiseLabel()
        {
            int amount = _game.RaiseAmount;
            if (SoulMode)
                return amount > 0 ? UiText.WagerMore : _game.WagerLeft == 0 ? UiText.WagerAll : UiText.TableFull;

            if (amount == 0) return _game.WagerLeft == 0 ? UiText.AllInDone : UiText.TableFull;
            return amount == _game.WagerLeft ? string.Format(UiText.AllInFormat, amount) : string.Format(UiText.RaiseFormat, amount);
        }

        /// <summary>Years put on the table come straight off the sentence counter — or, with the soul at stake, blink on the soul bar.</summary>
        private void ShowStakeOnTable()
        {
            if (SoulMode)
            {
                _view.SetPot(0);
                ShowSoul(_game.CurrentStake);
                _view.SetStakeInfo(UiText.SoulStakeInfo);
                return;
            }

            _view.SetPot(_game.CurrentStake);
            _view.Sentence.SetYears(_game.YearsOffTable, animate: true);
            ShowStakeInfo();
        }

        private void ShowStakeInfo()
        {
            _view.SetStakeInfo(string.Format(UiText.StakeInfoFormat, _game.LeastYearsForgiven, _game.LeastYearsAdded));
        }

        /// <summary>The soul bar: what is left, and how much of it is on the table — in shares of a soul, never in years.</summary>
        private void ShowSoul(int atStake)
        {
            float worth = _game.SoulWorth;
            _view.SetSoul(new SoulGauge(true, _game.SoulRemaining / worth, atStake / worth));
        }

        private void ShowDrawing()
        {
            _view.SetBetControls(BetControls.Hidden);
            _view.Player.Show(Slots(_game.PlayerHand, Hand.Size));
            _view.Player.SetInteractable(true);
            _view.Player.SetSelection(_discards);
            ShowPlayerCaption();
            if (GuideOn)
            {
                IReadOnlyCollection<int> toss = _game.SuggestedDiscards();
                _view.Player.SetHints(Enumerable.Range(0, Hand.Size).Where(i => !toss.Contains(i)).ToArray());
            }
            else
            {
                _view.Player.SetHints(null);
            }
            ShowStakeOnTable();
            _view.SetMessage(string.Format(UiText.PromptDrawFormat, _game.Rules.MaxDiscards), Tone.Neutral);
            _view.SetAction(_discards.Count == 0 ? UiText.Stand : string.Format(UiText.DrawFormat, _discards.Count));
            Tip(UiText.TipFirstDraw);
        }

        private void ShowResult(RoundResult round)
        {
            bool soulHand = _game.IsSoulHand || _soulShown || _game.IsSoulAtStake;
            _view.SetBetControls(BetControls.Hidden);
            _view.SetAction(null);
            _view.SetAnte(0);
            _view.SetStakeInfo(null);
            _view.Player.SetInteractable(false);
            _view.Player.SetSelection(null);
            _view.Player.SetHints(null);
            _view.Player.Show(Slots(_game.PlayerHand, Hand.Size));
            _view.House.Show(Slots(_game.HouseHand, Hand.Size));
            _view.Pause(ShowdownPause);

            // A run can be over with no hand behind it here (a finished save, a sentence carried in): nothing to name.
            ShowdownResult showdown = round?.Showdown;
            bool playerWon = showdown?.Outcome == ShowdownOutcome.PlayerWins;
            bool houseWon = showdown?.Outcome == ShowdownOutcome.HouseWins;

            if (round == null)
            {
                _view.House.SetCaption(UiText.HouseCaption, Tone.Muted);
                _view.Player.SetCaption(UiText.PlayerCaption, Tone.Muted);
            }
            else if (round.Folded)
            {
                _view.House.SetCaption(UiText.HouseCaption, Tone.Muted);
                _view.Player.SetCaption(UiText.FoldedCaption, Tone.Bad);
            }
            else
            {
                // Both hands by name; the winner's is lit and marked.
                string house = string.Format(UiText.HouseDrewFormat, UiText.CategoryName(showdown.House.Category), round.HouseExchange.Drawn.Count);
                string player = UiText.CategoryName(showdown.Player.Category);
                _view.House.SetCaption(houseWon ? string.Format(UiText.WinnerFormat, house) : house, houseWon ? Tone.Bad : Tone.Muted);
                _view.Player.SetCaption(playerWon ? string.Format(UiText.WinnerFormat, player) : player, playerWon ? Tone.Triumph : Tone.Muted);
            }

            _view.Payouts.Highlight(playerWon ? showdown.Player.Category : (HandCategory?)null);
            if (round != null)
                PlayMoments(round, playerWon);
            if (soulHand)
            {
                _view.SetPot(0);
                if (_game.IsSoulAtStake || _game.Phase == GamePhase.Damned)
                    ShowSoul(0);   // the bar fills or burns to what is left
            }
            else
            {
                _view.Sentence.SetYears(_game.Years, animate: true);
            }

            switch (_game.Phase)
            {
                case GamePhase.Absolved:
                    bool deadMansHand = showdown != null && showdown.Player.Category == HandCategory.DeadMansHand;
                    _view.SetMessage(deadMansHand ? UiText.AbsolvedMessage : UiText.ServedMessage, Tone.Triumph);
                    _view.Dealer.Say(_dealerText.Absolved, DealerMood.Annoyed);
                    _view.SetAction(UiText.TheEnd);
                    break;
                case GamePhase.Damned:
                    _view.SetMessage(UiText.DamnedMessage, Tone.Doom);
                    _view.Dealer.Say(_dealerText.Damned, DealerMood.Gloating);
                    _view.SetAction(UiText.TheEnd);
                    break;
                default:
                    if (round == null) break;   // only a finished run can come here without a hand
                    _view.SetMessage(ResultMessage(round, soulHand), playerWon ? Tone.Good : houseWon || round.Folded ? Tone.Bad : Tone.Neutral);
                    SayRoundLine(round);
                    _view.SetAction(UiText.Next);
                    break;
            }
        }

        /// <summary>The hand's big moments: a heavy loss shakes the table, a good win flares up, the Dead Man's Hand gets its scene.</summary>
        private void PlayMoments(RoundResult round, bool playerWon)
        {
            if (round.YearsChange > 0 && _game.Unit > 0 && round.YearsChange >= BigLossUnits * _game.Unit)
                _view.PlayMoment(TableMoment.BigLoss);

            if (!playerWon) return;
            HandCategory category = round.Showdown.Player.Category;
            if (category == HandCategory.DeadMansHand)
                _view.PlayMoment(TableMoment.DeadMansHand, playerCards: DeadMansCards(_game.PlayerHand));
            else if (category >= HandCategory.TwoPair)
                _view.PlayMoment(TableMoment.GoodHand, string.Format(UiText.GoodHandFormat, UiText.CategoryName(category).ToUpperInvariant()));
        }

        /// <summary>Where A♠ A♣ 8♠ 8♣ sit in the hand, in that order.</summary>
        private static int[] DeadMansCards(Hand hand)
        {
            var wanted = new[] { (Rank.Ace, Suit.Spades), (Rank.Ace, Suit.Clubs), (Rank.Eight, Suit.Spades), (Rank.Eight, Suit.Clubs) };
            var found = new List<int>();
            foreach (var (rank, suit) in wanted)
            {
                for (int i = 0; i < Hand.Size; i++)
                {
                    if (hand[i].Rank != rank || hand[i].Suit != suit) continue;
                    found.Add(i);
                    break;
                }
            }
            return found.ToArray();
        }

        /// <summary>The dealer reacts to how the hand ended — annoyed by your wins, gloating over your losses.</summary>
        private void SayRoundLine(RoundResult round)
        {
            int counter = _game.RoundNumber;
            if (round.Folded)
                _view.Dealer.Say(UiText.Pick(_dealerText.PlayerFolds, counter), DealerMood.Gloating);
            else if (round.Showdown.Outcome == ShowdownOutcome.PlayerWins)
                _view.Dealer.Say(UiText.Pick(_dealerText.PlayerWins, counter), DealerMood.Annoyed);
            else if (round.Showdown.Outcome == ShowdownOutcome.HouseWins)
                _view.Dealer.Say(UiText.Pick(_dealerText.HouseWins, counter), DealerMood.Gloating);
            else
                _view.Dealer.Say(UiText.Pick(_dealerText.Push, counter), DealerMood.Neutral);
        }

        private static string ResultMessage(RoundResult round, bool soulHand)
        {
            if (round.Folded)
                return soulHand ? UiText.SoulFold : string.Format(UiText.FoldFormat, round.YearsChange);

            string player = UiText.CategoryName(round.Showdown.Player.Category);
            string house = UiText.CategoryName(round.Showdown.House.Category);
            switch (round.Showdown.Outcome)
            {
                case ShowdownOutcome.PlayerWins:
                    return soulHand ? string.Format(UiText.SoulWinFormat, player, house) : string.Format(UiText.WinFormat, player, house, -round.YearsChange);
                case ShowdownOutcome.HouseWins:
                    return soulHand ? string.Format(UiText.SoulLossFormat, house, player) : string.Format(UiText.LossFormat, house, player, round.YearsChange);
                default:
                    return string.Format(UiText.PushFormat, player, house);
            }
        }

        /// <summary>Face up for the first <paramref name="faceUp"/> cards, face down for the rest; empty with no hand.</summary>
        private static CardSlot[] Slots(Hand hand, int faceUp)
        {
            var slots = new CardSlot[Hand.Size];
            for (int i = 0; i < Hand.Size; i++)
                slots[i] = hand == null ? CardSlot.Empty : i < faceUp ? CardSlot.Face(hand[i]) : CardSlot.Back;
            return slots;
        }
    }
}
