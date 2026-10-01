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
    /// Holds only UI state (selected discards, chosen ante); all rules live in the game.
    /// Each run is a new game built for the chosen dealer. Before the first run there is no game and input is ignored.
    /// Depends on abstractions only, so it can be tested without Unity scenes.
    /// </summary>
    public sealed class TablePresenter : ITableCommands, IRunSession, IDisposable
    {
        private const float ShowdownPause = 0.6f;

        private readonly Func<Dealer, IHellPokerGame> _createGame;
        private readonly ITableView _view;
        private readonly int[] _configuredStakes;
        private readonly HashSet<int> _discards = new HashSet<int>();

        private IHellPokerGame _game;
        private DealerText _dealerText;
        private int[] _stakeOptions;
        private int _stake;
        private bool _finalStretchAnnounced;

        /// <param name="createGame">Builds a fresh game for a run at this dealer's table.</param>
        public TablePresenter(Func<Dealer, IHellPokerGame> createGame, ITableView view, IReadOnlyList<int> stakeOptions)
        {
            _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            if (stakeOptions == null) throw new ArgumentNullException(nameof(stakeOptions));
            _configuredStakes = stakeOptions.Distinct().OrderBy(s => s).ToArray();

            _view.ActionPressed += PerformAction;
            _view.BetPressed += Bet;
            _view.Player.CardClicked += ToggleDiscard;
            _view.Stakes.StakeChosen += ChooseStake;
        }

        public int SelectedStake => _stake;

        public IReadOnlyCollection<int> SelectedDiscards => _discards;

        /// <summary>The game of the current run; null before the first run.</summary>
        public IHellPokerGame Game => _game;

        public bool CanContinue => _game != null && _game.RoundNumber > 0 && !_game.IsGameOver;

        public void StartNewRun(Dealer dealer)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));

            IHellPokerGame game = _createGame(dealer) ?? throw new InvalidOperationException("The game factory returned no game.");
            int[] stakes = _configuredStakes.Where(s => s >= game.Rules.MinStake && s <= game.Rules.MaxStake).ToArray();
            if (stakes.Length == 0)
                throw new InvalidOperationException("None of the stake options are allowed by the game rules.");

            _game = game;
            _stakeOptions = stakes;
            _stake = stakes[stakes.Length / 2];
            _discards.Clear();
            _finalStretchAnnounced = false;
            _dealerText = UiText.Dealer(dealer.Id);

            _view.Dealer.SetDealer(DealerCards.Describe(dealer));
            _view.Payouts.SetTable(dealer.Payouts);
            _view.Sentence.SetDamnationLimit(game.Rules.DamnationYears);
            _view.Sentence.SetYears(game.Years, animate: false);
            _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), Tone.Neutral);
            Refresh();
        }

        public void Dispose()
        {
            _view.ActionPressed -= PerformAction;
            _view.BetPressed -= Bet;
            _view.Player.CardClicked -= ToggleDiscard;
            _view.Stakes.StakeChosen -= ChooseStake;
        }

        public void PerformAction()
        {
            if (_game == null || _view.IsBusy) return;

            switch (_game.Phase)
            {
                case GamePhase.Betting:
                    _game.PlaceBet(_stake);
                    _discards.Clear();
                    break;
                case GamePhase.PlayerReveal:
                case GamePhase.HouseReveal:
                    Bet(BetAction.Pass);
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
                    _view.Sentence.SetYears(_game.Years, animate: true);
                    _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), Tone.Neutral);
                    break;
            }

            Refresh();
        }

        public void Bet(BetAction action)
        {
            if (_game == null || _view.IsBusy) return;

            if (!_game.CanBet(action, out string reason))
            {
                if (reason != null && (_game.Phase == GamePhase.PlayerReveal || _game.Phase == GamePhase.HouseReveal))
                    _view.SetMessage(reason, Tone.Warning);
                return;
            }

            _game.Bet(action);
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

        public void StepStake(int direction)
        {
            if (_game == null) return;

            int[] available = AvailableStakes();
            if (available.Length == 0) return;

            int index = Array.IndexOf(available, _stake) + Math.Sign(direction);
            ChooseStake(available[Math.Max(0, Math.Min(index, available.Length - 1))]);
        }

        private void ChooseStake(int stake)
        {
            if (_game == null || _view.IsBusy || _game.Phase != GamePhase.Betting || !AvailableStakes().Contains(stake)) return;

            _stake = stake;
            Refresh();
        }

        /// <summary>The configured stakes the game currently allows (you cannot wager years you do not have).</summary>
        private int[] AvailableStakes()
        {
            return _stakeOptions.Where(_game.IsValidStake).ToArray();
        }

        private void Refresh()
        {
            switch (_game.Phase)
            {
                case GamePhase.Betting:
                    ShowBetting();
                    break;
                case GamePhase.PlayerReveal:
                    ShowDecision(UiText.PromptPlayerCardFormat, _game.PlayerCardsRevealed);
                    break;
                case GamePhase.Drawing:
                    ShowDrawing();
                    break;
                case GamePhase.HouseReveal:
                    ShowDecision(UiText.PromptHouseCardFormat, _game.HouseCardsRevealed);
                    break;
                default:
                    ShowResult(_game.LastRound);
                    break;
            }

            _view.SetFinalStretch(_game.IsRaiseForced, string.Format(UiText.FinalStretchBannerFormat, _game.Rules.ForcedRaiseYears));

            // The dealer remarks once when the player first reaches the gates (unless the run just ended).
            if (_game.IsRaiseForced && !_finalStretchAnnounced && !_game.IsGameOver)
            {
                _finalStretchAnnounced = true;
                _view.Dealer.Say(UiText.Pick(_dealerText.FinalStretch, _game.RoundNumber), Tone.Warning);
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
            _view.Sentence.SetYears(_game.Years, animate: true);

            int[] available = AvailableStakes();
            if (!available.Contains(_stake) && available.Length > 0)
                _stake = available.Last();
            _view.Stakes.SetAvailable(available);
            _view.Stakes.SetSelected(_stake);
            _view.Stakes.SetVisible(true);
            _view.SetMessage(UiText.PromptBet, Tone.Neutral);
            _view.SetAction(UiText.Deal);
        }

        /// <summary>A card was just turned: animate it, then offer Raise / Pass / Fold.</summary>
        private void ShowDecision(string promptFormat, int cardNumber)
        {
            _view.Stakes.SetVisible(false);
            _view.SetAction(null);
            _view.SetBetControls(BetControls.Hidden);
            _view.Player.SetInteractable(false);
            _view.Player.SetSelection(null);

            // House cards first, so on the deal the table fills up before the player's first card turns.
            _view.House.Show(Slots(_game.HouseHand, _game.HouseCardsRevealed));
            _view.Player.Show(Slots(_game.PlayerHand, _game.PlayerCardsRevealed));
            ShowStakeOnTable();

            bool mustRaise = !_game.CanBet(BetAction.Pass, out _);
            string prompt = string.Format(promptFormat, cardNumber, Hand.Size);
            _view.SetMessage(prompt + (mustRaise ? UiText.PromptForcedChoice : UiText.PromptChoice), mustRaise ? Tone.Warning : Tone.Neutral);
            _view.SetBetControls(new BetControls(true, RaiseLabel(), _game.CanBet(BetAction.Raise, out _), !mustRaise));
        }

        private string RaiseLabel()
        {
            int amount = _game.RaiseAmount;
            if (amount == 0) return UiText.AllInDone;
            return amount < _game.Ante ? string.Format(UiText.AllInFormat, amount) : string.Format(UiText.RaiseFormat, amount);
        }

        /// <summary>Years put on the table come straight off the sentence counter, like chips pushed forward.</summary>
        private void ShowStakeOnTable()
        {
            _view.SetPot(_game.CurrentStake);
            _view.Sentence.SetYears(_game.YearsOffTable, animate: true);
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
            _view.SetBetControls(BetControls.Hidden);
            _view.SetAction(null);
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
            _view.Sentence.SetYears(_game.Years, animate: true);

            switch (_game.Phase)
            {
                case GamePhase.Absolved:
                    bool deadMansHand = showdown != null && showdown.Player.Category == HandCategory.DeadMansHand;
                    _view.SetMessage(deadMansHand ? UiText.AbsolvedMessage : UiText.ServedMessage, Tone.Triumph);
                    _view.Dealer.Say(_dealerText.Absolved, Tone.Bad);
                    _view.SetAction(UiText.Again);
                    break;
                case GamePhase.Damned:
                    _view.SetMessage(string.Format(UiText.DamnedFormat, _game.Years), Tone.Doom);
                    _view.Dealer.Say(_dealerText.Damned, Tone.Doom);
                    _view.SetAction(UiText.Again);
                    break;
                default:
                    _view.SetMessage(ResultMessage(round), playerWon ? Tone.Good : houseWon || round.Folded ? Tone.Bad : Tone.Neutral);
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
                _view.Dealer.Say(UiText.Pick(_dealerText.PlayerFolds, counter), Tone.Bad);
            else if (round.Showdown.Outcome == ShowdownOutcome.PlayerWins)
                _view.Dealer.Say(UiText.Pick(_dealerText.PlayerWins, counter), Tone.Good);
            else if (round.Showdown.Outcome == ShowdownOutcome.HouseWins)
                _view.Dealer.Say(UiText.Pick(_dealerText.HouseWins, counter), Tone.Bad);
            else
                _view.Dealer.Say(UiText.Pick(_dealerText.Push, counter), Tone.Neutral);
        }

        private static string ResultMessage(RoundResult round)
        {
            if (round.Folded)
                return string.Format(UiText.FoldFormat, round.YearsChange);

            string player = UiText.CategoryName(round.Showdown.Player.Category);
            string house = UiText.CategoryName(round.Showdown.House.Category);
            switch (round.Showdown.Outcome)
            {
                case ShowdownOutcome.PlayerWins: return string.Format(UiText.WinFormat, player, house, -round.YearsChange);
                case ShowdownOutcome.HouseWins: return string.Format(UiText.LossFormat, house, player, round.YearsChange);
                default: return string.Format(UiText.PushFormat, player, house);
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
