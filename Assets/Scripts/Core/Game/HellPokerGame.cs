using System;
using System.Collections.Generic;
using HellPoker.Core.Betting;
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
        private readonly IHouseBettingStrategy _houseBetting;
        private readonly PunishmentLedger _ledger;

        private ExchangeResult _playerExchange;
        private ExchangeResult _houseExchange;

        /// <summary>The decision the house's re-raise interrupted; play resumes from it after a call.</summary>
        private GamePhase _interruptedPhase;

        public GameRules Rules { get; }
        public GamePhase Phase { get; private set; }
        public Hand PlayerHand { get; private set; }
        public Hand HouseHand { get; private set; }
        public int Unit { get; private set; }
        public int Ante { get; private set; }
        public int TableCap { get; private set; }
        public int CurrentStake { get; private set; }
        public int HouseReRaiseAmount { get; private set; }
        public bool IsAfterDraw { get; private set; }
        public int PlayerCardsRevealed { get; private set; }
        public int HouseCardsRevealed { get; private set; }
        public RoundResult LastRound { get; private set; }
        public int RoundNumber { get; private set; }

        public int Years => _ledger.Years;
        public int YearsOffTable => _ledger.Years - CurrentStake;
        public int UpcomingAnte => Rules.Stakes.AnteFor(_ledger.Years);
        public bool IsGameOver => Phase == GamePhase.Absolved || Phase == GamePhase.Damned;
        public bool IsRaiseForced => _ledger.Years <= Rules.ForcedRaiseYears;

        public int RaiseAmount
        {
            get
            {
                if (!IsDecisionPhase(Phase)) return 0;
                int units = IsAfterDraw ? Rules.RaiseUnitsAfterDraw : Rules.RaiseUnitsBeforeDraw;
                return RoomToRaise(units * Unit);
            }
        }

        public int LeastYearsForgiven => _payouts.GetLeastYearsForgiven(StakeForOutlook, _ledger.Years);
        public int LeastYearsAdded => _payouts.GetLeastYearsAdded(StakeForOutlook);

        private int StakeForOutlook => CurrentStake > 0 ? CurrentStake : UpcomingAnte;

        /// <param name="houseBetting">How the house answers raises after the draw; null for a house that never re-raises.</param>
        public HellPokerGame(GameRules rules, IDeck deck, IHandEvaluator evaluator, ICardExchanger exchanger,
            IDrawStrategy houseStrategy, IPayoutTable payouts, IHouseBettingStrategy houseBetting = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _exchanger = exchanger ?? throw new ArgumentNullException(nameof(exchanger));
            _houseStrategy = houseStrategy ?? throw new ArgumentNullException(nameof(houseStrategy));
            _payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
            _houseBetting = houseBetting;
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

        public void PlaceBet()
        {
            RequirePhase(GamePhase.Betting);

            int years = _ledger.Years;
            _deck.Reset();
            Unit = Rules.Stakes.UnitFor(years);
            Ante = Rules.Stakes.AnteFor(years);
            TableCap = Rules.Stakes.CapFor(years);
            CurrentStake = Ante;
            PlayerHand = _deck.DealHand();
            HouseHand = _deck.DealHand();
            PlayerCardsRevealed = Math.Min(Hand.Size, Rules.OpeningCardsShown + 1);
            HouseCardsRevealed = 0;
            IsAfterDraw = false;
            RoundNumber++;
            Phase = GamePhase.PlayerReveal;
        }

        public bool CanBet(BetAction action, out string reason)
        {
            if (Phase == GamePhase.HouseReRaise)
            {
                bool answer = action == BetAction.Call || action == BetAction.Fold;
                reason = answer ? null : "The House has raised. Call or fold.";
                return answer;
            }

            if (!IsDecisionPhase(Phase))
            {
                reason = "There is no bet to answer right now.";
                return false;
            }

            if (action == BetAction.Call)
            {
                reason = "There is nothing to call.";
                return false;
            }

            if (action == BetAction.Raise && RaiseAmount == 0)
            {
                reason = CurrentStake >= TableCap ? "The table is at its limit." : "Every year you have is already on the table.";
                return false;
            }

            // Once nothing more can be raised (cap or all in), passing is allowed again.
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

            if (action == BetAction.Call)
            {
                CurrentStake += HouseReRaiseAmount;
                HouseReRaiseAmount = 0;
                Advance(_interruptedPhase);
                return;
            }

            if (action == BetAction.Raise)
            {
                CurrentStake += RaiseAmount;
                if (IsAfterDraw && TryHouseReRaise())
                    return;
            }

            Advance(Phase);
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
            IsAfterDraw = true;
            Phase = GamePhase.DrawReveal;

            return _playerExchange;
        }

        public void NextRound()
        {
            RequirePhase(GamePhase.RoundOver);
            ClearHand();
            Phase = GamePhase.Betting;
        }

        /// <summary>Moves on from a decision that has been answered.</summary>
        private void Advance(GamePhase answered)
        {
            switch (answered)
            {
                case GamePhase.PlayerReveal:
                    if (PlayerCardsRevealed < Hand.Size)
                    {
                        PlayerCardsRevealed++;
                        Phase = GamePhase.PlayerReveal;
                    }
                    else
                    {
                        Phase = GamePhase.Drawing;
                    }
                    break;

                case GamePhase.DrawReveal when Rules.HouseCardsShown > 0:
                    HouseCardsRevealed = Rules.HouseCardsShown;
                    Phase = GamePhase.HouseReveal;
                    break;

                default:
                    FinishShowdown();
                    break;
            }
        }

        /// <summary>After a raise past the draw, the house may raise back (if the cap leaves room).</summary>
        private bool TryHouseReRaise()
        {
            int amount = RoomToRaise(Rules.HouseReRaiseUnits * Unit);
            if (amount == 0 || _houseBetting == null || !_houseBetting.WantsToReRaise(_evaluator.Evaluate(HouseHand)))
                return false;

            HouseReRaiseAmount = amount;
            _interruptedPhase = Phase;
            Phase = GamePhase.HouseReRaise;
            return true;
        }

        private int RoomToRaise(int wanted)
        {
            return Math.Max(0, Math.Min(wanted, Math.Min(TableCap - CurrentStake, YearsOffTable)));
        }

        private static bool IsDecisionPhase(GamePhase phase)
        {
            return phase == GamePhase.PlayerReveal || phase == GamePhase.DrawReveal || phase == GamePhase.HouseReveal;
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
                _ledger.Add(_payouts.GetFoldPenalty(CurrentStake, IsAfterDraw));
            else if (showdown.Outcome == ShowdownOutcome.PlayerWins)
                _ledger.Forgive(_payouts.GetYearsForgiven(showdown.Player.Category, CurrentStake, _ledger.Years));
            else if (showdown.Outcome == ShowdownOutcome.HouseWins)
                _ledger.Add(_payouts.GetYearsAdded(showdown.House.Category, CurrentStake));

            HouseReRaiseAmount = 0;
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
            Unit = 0;
            Ante = 0;
            TableCap = 0;
            CurrentStake = 0;
            HouseReRaiseAmount = 0;
            IsAfterDraw = false;
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
