using System;
using System.Collections.Generic;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <inheritdoc cref="IHellPokerGame"/>
    /// <remarks>Build with <see cref="HellPokerGameFactory"/> unless you need custom parts (tests, special modes).</remarks>
    public sealed class HellPokerGame : IHellPokerGame
    {
        private readonly IDeck _deck;
        private readonly IHandEvaluator _evaluator;
        private readonly ICardExchanger _exchanger;
        private readonly IDrawStrategy _houseStrategy;
        private readonly IPayoutTable _payouts;
        private readonly PunishmentLedger _ledger;

        private ExchangeResult _playerExchange;
        private ExchangeResult _houseExchange;

        public GameRules Rules { get; }
        public GamePhase Phase { get; private set; }
        public Hand PlayerHand { get; private set; }
        public Hand HouseHand { get; private set; }
        public int Ante { get; private set; }
        public int CurrentStake { get; private set; }
        public int PlayerCardsRevealed { get; private set; }
        public int HouseCardsRevealed { get; private set; }
        public RoundResult LastRound { get; private set; }
        public int RoundNumber { get; private set; }

        public int Years => _ledger.Years;
        public int YearsOffTable => _ledger.Years - CurrentStake;
        public bool IsGameOver => Phase == GamePhase.Absolved || Phase == GamePhase.Damned;
        public bool IsRaiseForced => _ledger.Years <= Rules.ForcedRaiseYears;
        public int RaiseAmount => Math.Max(0, Math.Min(Ante, YearsOffTable));

        public HellPokerGame(GameRules rules, IDeck deck, IHandEvaluator evaluator, ICardExchanger exchanger,
            IDrawStrategy houseStrategy, IPayoutTable payouts)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _exchanger = exchanger ?? throw new ArgumentNullException(nameof(exchanger));
            _houseStrategy = houseStrategy ?? throw new ArgumentNullException(nameof(houseStrategy));
            _payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
            _ledger = new PunishmentLedger(rules.StartingYears);

            Restart();
        }

        public void Restart()
        {
            _ledger.Reset(Rules.StartingYears);
            ClearHand();
            LastRound = null;
            RoundNumber = 0;
            Phase = GamePhase.Betting;
        }

        public bool IsValidStake(int stake)
        {
            if (stake < Rules.MinStake || stake > Rules.MaxStake) return false;

            // You cannot wager years you do not have — except the minimum, which then goes all in.
            return stake <= _ledger.Years || stake == Rules.MinStake;
        }

        public void PlaceBet(int stake)
        {
            RequirePhase(GamePhase.Betting);
            if (!IsValidStake(stake))
                throw new ArgumentOutOfRangeException(nameof(stake), stake,
                    $"Stake must be between {Rules.MinStake} and {Math.Min(Rules.MaxStake, Math.Max(Rules.MinStake, _ledger.Years))}.");

            _deck.Reset();
            Ante = Math.Min(stake, _ledger.Years);
            CurrentStake = Ante;
            PlayerHand = _deck.DealHand();
            HouseHand = _deck.DealHand();
            PlayerCardsRevealed = 1;
            HouseCardsRevealed = 0;
            RoundNumber++;
            Phase = GamePhase.PlayerReveal;
        }

        public bool CanBet(BetAction action, out string reason)
        {
            if (Phase != GamePhase.PlayerReveal && Phase != GamePhase.HouseReveal)
            {
                reason = "There is no bet to answer right now.";
                return false;
            }

            if (action == BetAction.Raise && RaiseAmount == 0)
            {
                reason = "Every year you have is already on the table.";
                return false;
            }

            // Once everything is on the table there is nothing left to raise, so passing is allowed again.
            if (action == BetAction.Pass && IsRaiseForced && RaiseAmount > 0)
            {
                reason = $"With {Rules.ForcedRaiseYears} years or less left, the House demands a raise.";
                return false;
            }

            reason = null;
            return true;
        }

        public void Bet(BetAction action)
        {
            if (!CanBet(action, out string reason))
                throw new InvalidOperationException(reason);

            if (action == BetAction.Fold)
            {
                Finish(showdown: null);
                return;
            }

            if (action == BetAction.Raise)
                CurrentStake += RaiseAmount;

            if (Phase == GamePhase.PlayerReveal)
            {
                if (PlayerCardsRevealed < Hand.Size)
                    PlayerCardsRevealed++;
                else
                    Phase = GamePhase.Drawing;
            }
            else if (HouseCardsRevealed < Rules.HouseRevealDecisions)
            {
                HouseCardsRevealed++;
            }
            else
            {
                FinishShowdown();
            }
        }

        public bool CanDraw(IReadOnlyCollection<int> discardIndices, out string reason)
        {
            if (Phase != GamePhase.Drawing)
            {
                reason = "It is not time to draw.";
                return false;
            }

            return _exchanger.CanExchange(PlayerHand, discardIndices, _deck, out reason);
        }

        public ExchangeResult Draw(IReadOnlyCollection<int> discardIndices)
        {
            RequirePhase(GamePhase.Drawing);

            _playerExchange = _exchanger.Exchange(PlayerHand, discardIndices, _deck);
            _houseExchange = _exchanger.Exchange(HouseHand, _houseStrategy.ChooseDiscards(HouseHand), _deck);
            PlayerHand = _playerExchange.Hand;
            HouseHand = _houseExchange.Hand;

            if (Rules.HouseRevealDecisions == 0)
            {
                FinishShowdown();
            }
            else
            {
                HouseCardsRevealed = 1;
                Phase = GamePhase.HouseReveal;
            }

            return _playerExchange;
        }

        public void NextRound()
        {
            RequirePhase(GamePhase.RoundOver);
            ClearHand();
            Phase = GamePhase.Betting;
        }

        private void FinishShowdown()
        {
            Finish(ShowdownResult.Resolve(_evaluator.Evaluate(PlayerHand), _evaluator.Evaluate(HouseHand)));
        }

        /// <summary>Settles the hand. A null showdown means the player folded.</summary>
        private void Finish(ShowdownResult showdown)
        {
            int yearsBefore = _ledger.Years;

            if (showdown == null)
                _ledger.Add(_payouts.GetFoldPenalty(CurrentStake));
            else if (showdown.Outcome == ShowdownOutcome.PlayerWins)
                _ledger.Forgive(_payouts.GetYearsForgiven(showdown.Player.Category, CurrentStake, _ledger.Years));
            else if (showdown.Outcome == ShowdownOutcome.HouseWins)
                _ledger.Add(_payouts.GetYearsAdded(showdown.House.Category, CurrentStake));

            PlayerCardsRevealed = Hand.Size;
            HouseCardsRevealed = Hand.Size;
            Phase = _ledger.IsServed ? GamePhase.Absolved
                : _ledger.Years >= Rules.DamnationYears ? GamePhase.Damned
                : GamePhase.RoundOver;

            LastRound = new RoundResult(CurrentStake, showdown == null, _playerExchange, _houseExchange, showdown,
                yearsBefore, _ledger.Years, Phase);
        }

        private void ClearHand()
        {
            PlayerHand = null;
            HouseHand = null;
            Ante = 0;
            CurrentStake = 0;
            PlayerCardsRevealed = 0;
            HouseCardsRevealed = 0;
            _playerExchange = null;
            _houseExchange = null;
        }

        private void RequirePhase(GamePhase expected)
        {
            if (Phase != expected)
                throw new InvalidOperationException($"Expected phase {expected}, but the game is in {Phase}.");
        }
    }
}
