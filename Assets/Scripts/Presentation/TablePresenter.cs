using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
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

        private readonly Func<Dealer, IHellPokerGame> _createGame;
        private readonly ITableView _view;
        private readonly HashSet<int> _discards = new HashSet<int>();

        private IHellPokerGame _game;
        private Dealer _dealer;
        private DealerText _dealerText;
        private bool _finalStretchAnnounced;
        private bool _soulShown;

        public event Action LeaveRequested;

        /// <param name="createGame">Builds a fresh game for a run at this dealer's table.</param>
        public TablePresenter(Func<Dealer, IHellPokerGame> createGame, ITableView view)
        {
            _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _view.ActionPressed += PerformAction;
            _view.BetPressed += Bet;
            _view.LeavePressed += RequestLeave;
            _view.Player.CardClicked += ToggleDiscard;
        }

        public IReadOnlyCollection<int> SelectedDiscards => _discards;

        /// <summary>The game of the current table; null before the first run.</summary>
        public IHellPokerGame Game => _game;

        public bool CanContinue => _game != null && _game.RoundNumber > 0 && !_game.IsGameOver;

        public string CurrentDealerId => _dealer?.Id;

        public void StartNewRun(Dealer dealer)
        {
            SeatAt(dealer, carriedYears: null, roundsPlayed: 0);
            _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            Refresh();
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
            if (!_game.IsSoulAtStake)
                _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            Refresh();
        }

        public void RequestLeave()
        {
            if (_game == null || _view.IsBusy || _game.IsGameOver) return;

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
            _view.Player.CardClicked -= ToggleDiscard;
        }

        public void PerformAction()
        {
            if (_game == null || _view.IsBusy) return;

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
                    _game.Restart();
                    _finalStretchAnnounced = false;
                    _soulShown = false;
                    _view.SetSoul(SoulGauge.Hidden);
                    _view.Sentence.SetSoulLine(_game.Rules.SoulThreshold);
                    _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
                    break;
            }

            Refresh();
        }

        public void Bet(BetAction action)
        {
            if (_game == null || _view.IsBusy) return;

            if (!_game.CanBet(action, out string reason))
            {
                if (reason != null && IsBetPhase(_game.Phase))
                    _view.SetMessage(reason, Tone.Warning);
                return;
            }

            _game.Bet(action);
            if (_game.Phase == GamePhase.HouseReRaise)
                _view.Dealer.Say(UiText.Pick(_dealerText.ReRaise, _game.RoundNumber), DealerMood.Scheming);
            Refresh();
        }

        public void ToggleDiscard(int index)
        {
            if (_game == null || _view.IsBusy || _game.Phase != GamePhase.Drawing) return;

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

            _view.SetLeave(_game.Phase != GamePhase.Betting ? LeaveState.Hidden
                : _game.IsSoulAtStake ? LeaveState.Locked : LeaveState.Open);
            _view.SetFinalStretch(_game.IsRaiseForced, string.Format(UiText.FinalStretchBannerFormat, _game.Rules.ForcedRaiseYears));
            AnnounceSoul();

            // The dealer remarks once when the player first reaches the gates (unless the run just ended).
            if (_game.IsRaiseForced && !_finalStretchAnnounced && !_game.IsGameOver)
            {
                _finalStretchAnnounced = true;
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

            // House cards first, so on the deal the table fills up before the player's cards turn.
            _view.House.Show(Slots(_game.HouseHand, _game.HouseCardsRevealed));
            _view.Player.Show(Slots(_game.PlayerHand, _game.PlayerCardsRevealed));
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
            ShowStakeOnTable();
            _view.SetMessage(string.Format(UiText.PromptDrawFormat, _game.Rules.MaxDiscards), Tone.Neutral);
            _view.SetAction(_discards.Count == 0 ? UiText.Stand : string.Format(UiText.DrawFormat, _discards.Count));
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
            _view.Player.Show(Slots(_game.PlayerHand, Hand.Size));
            _view.House.Show(Slots(_game.HouseHand, Hand.Size));
            _view.Pause(ShowdownPause);

            ShowdownResult showdown = round.Showdown;
            bool playerWon = showdown?.Outcome == ShowdownOutcome.PlayerWins;
            bool houseWon = showdown?.Outcome == ShowdownOutcome.HouseWins;

            if (round.Folded)
            {
                _view.House.SetCaption(UiText.HouseCaption, Tone.Muted);
                _view.Player.SetCaption(UiText.FoldedCaption, Tone.Bad);
            }
            else
            {
                string house = UiText.CategoryName(showdown.House.Category);
                _view.House.SetCaption(string.Format(UiText.HouseDrewFormat, house, round.HouseExchange.Drawn.Count), houseWon ? Tone.Bad : Tone.Muted);
                _view.Player.SetCaption(UiText.CategoryName(showdown.Player.Category), playerWon ? Tone.Triumph : Tone.Muted);
            }

            _view.Payouts.Highlight(playerWon ? showdown.Player.Category : (HandCategory?)null);
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
                    _view.SetAction(UiText.Again);
                    break;
                case GamePhase.Damned:
                    _view.SetMessage(UiText.DamnedMessage, Tone.Doom);
                    _view.Dealer.Say(_dealerText.Damned, DealerMood.Gloating);
                    _view.SetAction(UiText.Again);
                    break;
                default:
                    _view.SetMessage(ResultMessage(round, soulHand), playerWon ? Tone.Good : houseWon || round.Folded ? Tone.Bad : Tone.Neutral);
                    SayRoundLine(round);
                    _view.SetAction(UiText.Next);
                    break;
            }
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
