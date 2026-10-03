using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Randomness;
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

        private readonly CheatSession _cheats;
        private readonly IRandomSource _cheatRandom;
        private IReadOnlyList<int> _drawnIndices = Array.Empty<int>();

        /// <summary>Under Lucifer's Gaze the House still re-raises a hand it knows will beat it this often (a bluff).</summary>
        public const int GazeBluffPercent = 50;

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
        public bool IsGameOver => Phase == GamePhase.Absolved || Phase == GamePhase.Damned;
        public bool IsRaiseForced => _ledger.Years <= Rules.ForcedRaiseYears;

        /// <summary>Set once the table is full or the player is all in; stays set until the next deal.</summary>
        private bool _sealed;

        public bool IsCommitted => _sealed && Phase != GamePhase.HouseReRaise;

        public int DecisionsSkipped { get; private set; }

        // ------------------------------------------------------------------ the soul

        public int SoulWorth => Rules.SoulWorthYears;
        public bool IsSoulAtStake => _ledger.Years >= Rules.SoulThreshold;
        public bool IsSoulHand { get; private set; }

        public int SoulRemaining => IsSoulAtStake
            ? Math.Max(0, Math.Min(SoulWorth, SoulWorth - (_ledger.Years - Rules.SoulThreshold)))
            : SoulWorth;

        /// <summary>What may still be wagered this hand: the sentence normally, what is left of the soul when it is on the table.</summary>
        private int Purse => IsSoulHand ? _handPurse : _ledger.Years;

        public int WagerLeft => Math.Max(0, Purse - CurrentStake);

        /// <summary>Bets are measured against the soul's worth once it is on the table, against the sentence otherwise.</summary>
        private int StakeBase => IsSoulAtStake ? SoulWorth : _ledger.Years;

        private int AvailableForNextHand => IsSoulAtStake ? SoulRemaining : _ledger.Years;

        public int UpcomingAnte => Math.Min(Rules.Stakes.AnteFor(StakeBase), AvailableForNextHand);

        public int RaiseAmount
        {
            get
            {
                if (!IsDecisionPhase(Phase)) return 0;
                int units = IsAfterDraw ? Rules.RaiseUnitsAfterDraw : Rules.RaiseUnitsBeforeDraw;
                return RoomToRaise(units * Unit);
            }
        }

        public int LeastYearsForgiven => _payouts.GetLeastYearsForgiven(StakeForOutlook, AnteForOutlook, _ledger.Years);
        public int LeastYearsAdded => _payouts.GetLeastYearsAdded(StakeForOutlook, AnteForOutlook, LossSurcharge(CurrentStake > 0 ? IsSoulHand : IsSoulAtStake));

        private int StakeForOutlook => CurrentStake > 0 ? CurrentStake : UpcomingAnte;
        private int AnteForOutlook => CurrentStake > 0 ? Ante : UpcomingAnte;

        private int _handPurse;

        private int LossSurcharge(bool soulHand) => soulHand ? Rules.SoulLossPercent : 100;

        /// <param name="houseBetting">How the house answers raises after the draw; null for a house that never re-raises.</param>
        /// <param name="cheats">The demon's cheating at this table; null for an honest table.</param>
        public HellPokerGame(GameRules rules, IDeck deck, IHandEvaluator evaluator, ICardExchanger exchanger,
            IDrawStrategy houseStrategy, IPayoutTable payouts, IHouseBettingStrategy houseBetting = null, CheatSession cheats = null,
            IRandomSource cheatRandom = null)
        {
            _cheats = cheats ?? new CheatSession(null, 0, null);
            _cheatRandom = cheatRandom;
            if (_cheats.IsActive && cheatRandom == null) throw new ArgumentNullException(nameof(cheatRandom));
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

            // With the soul on the table the bets are measured against the soul's worth and limited to what is left of it.
            IsSoulHand = IsSoulAtStake;
            int stakeBase = StakeBase;
            _handPurse = AvailableForNextHand;
            _deck.Reset();
            Unit = Rules.Stakes.UnitFor(stakeBase);
            Ante = Math.Min(Rules.Stakes.AnteFor(stakeBase), _handPurse);
            TableCap = Math.Max(Ante, Math.Min(Rules.Stakes.CapFor(stakeBase), _handPurse));
            CurrentStake = Ante;
            PlayerHand = _deck.DealHand();
            HouseHand = _deck.DealHand();
            PlayerCardsRevealed = Math.Min(Hand.Size, Rules.OpeningCardsShown + 1);
            HouseCardsRevealed = 0;
            IsAfterDraw = false;
            _sealed = false;
            DecisionsSkipped = 0;
            RoundNumber++;
            Phase = GamePhase.PlayerReveal;
            _cheats.BeginHand(Rules, _ledger.Years);
            Strike(CheatTiming.AfterDeal);
            NoteCommitment();
            SkipEmptyDecisions();
        }

        // ------------------------------------------------------------------ the demon's cheats

        public int Malice => _cheats.Malice;
        public int MaliceMax => _cheats.MaliceMax;
        public ICheat PendingCheat => InPlay ? _cheats.Intent : null;
        public IReadOnlyList<CheatResult> CheatsThisHand => _cheats.Results;
        public bool MajorCheatUsed => _cheats.MajorUsed;
        public int ThornYearsThisHand { get; private set; }
        public int TitheYearsThisHand { get; private set; }

        /// <summary>While the hand is played: true for a card of the player's the player cannot see (veiled, moonless, swapped in).</summary>
        public bool IsPlayerCardHidden(int index) => InPlay && _cheats.Marks.HiddenFromPlayer.Contains(PlayerHand[index]);

        public bool WasPlayerCardHidden(int index) => PlayerHand != null && _cheats.Marks.HiddenFromPlayer.Contains(PlayerHand[index]);

        public bool IsPlayerCardChained(int index) => InPlay && _cheats.Marks.Chained.Contains(PlayerHand[index]);

        public bool IsPlayerCardThorned(int index) => InPlay && _cheats.Marks.Thorned.Contains(PlayerHand[index]);

        /// <summary>True while a House card shows a false face (until the showdown).</summary>
        public bool IsHouseCardFalse(int index) => InPlay && _cheats.Marks.FakeHouseIndex == index;

        /// <summary>The House card as the player sees it: a false face until the showdown turns the truth.</summary>
        public Card HouseCardFace(int index) => IsHouseCardFalse(index) ? _cheats.Marks.FakeHouseFace : HouseHand[index];

        public void RestoreMalice(int malice, bool majorCheatUsed)
        {
            RequirePhase(GamePhase.Betting);
            _cheats.Restore(malice, majorCheatUsed);
        }

        /// <summary>A hand is being played (the marks matter); once it is settled every card shows as it is.</summary>
        private bool InPlay => PlayerHand != null && (IsDecisionPhase(Phase) || Phase == GamePhase.Drawing || Phase == GamePhase.HouseReRaise);

        /// <summary>The moment <paramref name="timing"/> has come: the demon's cheat strikes if it is due, in plain sight.</summary>
        /// <returns>The showdown as it stands afterwards (The Fall may change it).</returns>
        private ShowdownResult Strike(CheatTiming timing, ShowdownResult showdown = null)
        {
            // At the deal the player has seen only the opening cards; the next one turns at the first decision.
            int seen = timing == CheatTiming.AfterDeal ? Math.Min(Rules.OpeningCardsShown, PlayerCardsRevealed) : PlayerCardsRevealed;
            CheatTable after = _cheats.Strike(timing, () => new CheatTable(PlayerHand, HouseHand, _deck, _evaluator, _cheatRandom,
                _cheats.Marks, Unit, HouseCardsRevealed, _drawnIndices, showdown, seen, _cheats.BackfirePercent, _houseStrategy));
            if (after == null) return showdown;
            PlayerHand = after.PlayerHand;
            HouseHand = after.HouseHand;
            return after.Showdown;
        }

        public bool CanBet(BetAction action, out string reason)
        {
            // A house re-raise is a new bet: it is answered even when the player's own betting is sealed.
            if (Phase == GamePhase.HouseReRaise)
            {
                bool answer = action == BetAction.Call || action == BetAction.Fold;
                reason = answer ? null : "The House has raised. Call or fold.";
                return answer;
            }

            if (_sealed && (IsDecisionPhase(Phase) || Phase == GamePhase.Drawing))
            {
                reason = "The pact is sealed: the cards play out on their own.";
                return false;
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
                NoteCommitment();
                Continue(_interruptedPhase);
                return;
            }

            if (action == BetAction.Raise)
            {
                CurrentStake += RaiseAmount;
                if (IsAfterDraw && TryHouseReRaise())
                    return;
                NoteCommitment();
            }

            Continue(Phase);
        }

        public bool CanCheckToDraw(out string reason)
        {
            if (Phase != GamePhase.PlayerReveal)
            {
                reason = "There are no cards left to check through.";
                return false;
            }

            return CanBet(BetAction.Pass, out reason);
        }

        public void CheckToDraw()
        {
            if (!CanCheckToDraw(out string reason))
                throw new InvalidOperationException(reason);

            while (Phase == GamePhase.PlayerReveal)
                Bet(BetAction.Pass);
        }

        public HandInProgress CurrentHand =>
            IsDecisionPhase(Phase) || Phase == GamePhase.Drawing || Phase == GamePhase.HouseReRaise
                ? new HandInProgress(CurrentStake, Ante, IsAfterDraw, IsSoulHand, _sealed, _cheats.Planned?.Id,
                    _cheats.Results.Count > 0 ? _cheats.Results[0].ShownId : _cheats.Intent?.Id, _cheats.IsResolved)
                : null;

        public RoundResult ForfeitHand(HandInProgress hand)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            RequirePhase(GamePhase.Betting);

            int surcharge = LossSurcharge(hand.IsSoulHand);
            int penalty = hand.IsSealed
                ? _payouts.GetLeastYearsAdded(hand.Stake, hand.Ante, surcharge)
                : _payouts.GetFoldPenalty(hand.Stake, hand.IsAfterDraw, surcharge);

            int yearsBefore = _ledger.Years;
            _ledger.Add(penalty);
            Phase = _ledger.Years >= Rules.DamnationYears ? GamePhase.Damned : GamePhase.Betting;
            LastRound = new RoundResult(hand.Stake, true, null, null, null, yearsBefore, _ledger.Years, Phase);
            return LastRound;
        }

        /// <summary>What the face-up cards the player can actually see make (a card hidden by a cheat does not count).</summary>
        public HandCategory? PlayerHandNow =>
            PlayerHand == null
                ? (HandCategory?)null
                : VisibleHandReader.Read(Enumerable.Range(0, PlayerCardsRevealed).Where(i => !IsPlayerCardHidden(i)).Select(i => PlayerHand[i]).ToList(),
                    _evaluator);

        public IReadOnlyCollection<int> SuggestedDiscards()
        {
            if (Phase != GamePhase.Drawing) return Array.Empty<int>();
            // No hint on a hand with hidden cards: it would give away what the player cannot see.
            if (Enumerable.Range(0, Hand.Size).Any(IsPlayerCardHidden)) return Array.Empty<int>();
            IReadOnlyCollection<int> discards = _houseStrategy.ChooseDiscards(PlayerHand).Where(i => !IsPlayerCardChained(i)).ToArray();
            return CanDraw(discards, out _) ? discards : Array.Empty<int>();
        }

        public bool CanDraw(IReadOnlyCollection<int> discardIndices, out string reason)
        {
            if (Phase != GamePhase.Drawing)
            {
                reason = "It is not time to draw.";
                return false;
            }

            if (discardIndices != null && discardIndices.Any(i => i >= 0 && i < Hand.Size && IsPlayerCardChained(i)))
            {
                reason = "That card is chained as collateral: it stays this hand.";
                return false;
            }

            return _exchanger.CanExchange(PlayerHand, discardIndices, _deck, out reason);
        }

        public ExchangeResult Draw(IReadOnlyCollection<int> discardIndices)
        {
            RequirePhase(GamePhase.Drawing);
            if (!CanDraw(discardIndices, out string reason))
                throw new InvalidOperationException(reason);

            // A thorned card thrown back costs a unit, at once.
            int thorns = discardIndices.Count(IsPlayerCardThorned);
            if (thorns > 0)
            {
                ThornYearsThisHand = thorns * Unit;
                _ledger.Add(ThornYearsThisHand);
            }

            _playerExchange = _exchanger.Exchange(PlayerHand, discardIndices, _deck);
            _houseExchange = _exchanger.Exchange(HouseHand, _houseStrategy.ChooseDiscards(HouseHand), _deck);
            PlayerHand = _playerExchange.Hand;
            HouseHand = _houseExchange.Hand;
            _drawnIndices = _playerExchange.ReplacedIndices;
            IsAfterDraw = true;
            Phase = GamePhase.DrawReveal;
            Strike(CheatTiming.AfterDraw);
            SkipEmptyDecisions();

            return _playerExchange;
        }

        public void NextRound()
        {
            RequirePhase(GamePhase.RoundOver);
            ClearHand();
            Phase = GamePhase.Betting;
        }

        public bool CanLeaveTable(out string reason)
        {
            if (Phase != GamePhase.Betting)
            {
                reason = "You can only change tables between hands.";
                return false;
            }

            if (Rules.IsFinalTable)
            {
                reason = "Nobody leaves the Morning Star's table.";
                return false;
            }

            if (IsSoulAtStake)
            {
                reason = "Your soul is on this table. You cannot leave it.";
                return false;
            }

            reason = null;
            return true;
        }

        public void TakeOver(int years, int roundsPlayed)
        {
            RequirePhase(GamePhase.Betting);
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (roundsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed));

            _ledger.Reset(years);
            ClearHand();
            LastRound = null;
            RoundNumber = roundsPlayed;
            Phase = _ledger.IsServed ? GamePhase.Absolved
                : _ledger.Years >= Rules.DamnationYears ? GamePhase.Damned
                : GamePhase.Betting;
        }

        /// <summary>Moves on from an answered decision, past any decision that is no real choice.</summary>
        private void Continue(GamePhase answered)
        {
            Advance(answered);
            SkipEmptyDecisions();
        }

        /// <summary>
        /// A decision where passing is the only thing the player could do is not asked: the game passes for them.
        /// That is every decision once the pact is sealed — nothing left to raise, and no folding.
        /// The draw is never skipped: it is a choice of cards, not a bet.
        /// </summary>
        private void SkipEmptyDecisions()
        {
            while (IsDecisionPhase(Phase) && !HasRealChoice())
            {
                DecisionsSkipped++;
                Advance(Phase);
            }
        }

        private bool HasRealChoice()
        {
            if (_sealed) return false;
            return RaiseAmount > 0 || CanBet(BetAction.Fold, out _);
        }

        /// <summary>The table is full or every year (or the whole soul) is on it: the pact is sealed for the rest of the hand.</summary>
        private void NoteCommitment()
        {
            if (CurrentStake >= TableCap || WagerLeft == 0)
                _sealed = true;
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
                        Strike(CheatTiming.BeforeDraw);
                    }
                    break;

                case GamePhase.DrawReveal when Rules.HouseCardsShown > 0:
                    HouseCardsRevealed = Rules.HouseCardsShown;
                    Phase = GamePhase.HouseReveal;
                    Strike(CheatTiming.HouseReveal);
                    break;

                default:
                    FinishShowdown();
                    break;
            }
        }

        /// <summary>
        /// After a raise past the draw, the house may raise back. Its re-raise may go past the table cap (only the player's own
        /// raises are capped) but never past what the player still has.
        /// </summary>
        private bool TryHouseReRaise()
        {
            int amount = Math.Max(0, Math.Min(Rules.HouseReRaiseUnits * Unit, WagerLeft));
            if (amount == 0 || !HouseWantsToReRaise())
                return false;

            HouseReRaiseAmount = amount;
            _interruptedPhase = Phase;
            Phase = GamePhase.HouseReRaise;
            return true;
        }

        /// <summary>The House's temper — unless Lucifer's Gaze is on the hand: then he knows. A losing hand is always
        /// re-raised; a winning one half the time (<see cref="GazeBluffPercent"/>), so his raise is a threat, never a tell.</summary>
        private bool HouseWantsToReRaise()
        {
            if (_cheats.Marks.Gaze)
            {
                ShowdownOutcome outcome = ShowdownResult.Resolve(_evaluator.Evaluate(PlayerHand), _evaluator.Evaluate(HouseHand)).Outcome;
                if (outcome == ShowdownOutcome.HouseWins) return true;
                if (outcome == ShowdownOutcome.PlayerWins) return _cheatRandom.Next(100) < GazeBluffPercent;
            }
            return _houseBetting != null && _houseBetting.WantsToReRaise(_evaluator.Evaluate(HouseHand));
        }

        private int RoomToRaise(int wanted)
        {
            return Math.Max(0, Math.Min(wanted, Math.Min(TableCap - CurrentStake, WagerLeft)));
        }

        private static bool IsDecisionPhase(GamePhase phase)
        {
            return phase == GamePhase.PlayerReveal || phase == GamePhase.DrawReveal || phase == GamePhase.HouseReveal;
        }

        private void FinishShowdown()
        {
            // The showdown stands as it is judged — the only cheat allowed to change it is Lucifer's Fall, in plain sight.
            ShowdownResult showdown = ShowdownResult.Resolve(_evaluator.Evaluate(PlayerHand), _evaluator.Evaluate(HouseHand));
            Finish(Strike(CheatTiming.BeforeShowdown, showdown));
        }

        /// <summary>Settles the hand. A null showdown means the player folded.</summary>
        private void Finish(ShowdownResult showdown)
        {
            int yearsBefore = _ledger.Years;

            if (showdown == null)
                _ledger.Add(_payouts.GetFoldPenalty(CurrentStake, IsAfterDraw, LossSurcharge(IsSoulHand)));
            else if (showdown.Outcome == ShowdownOutcome.PlayerWins)
            {
                int forgiven = Forgiven(showdown.Player.Category);
                if (_cheats.Marks.Tithe && !_payouts.IsAbsolution(showdown.Player.Category))
                {
                    TitheYearsThisHand = Math.Min(Unit, forgiven);
                    forgiven -= TitheYearsThisHand;
                }
                _ledger.Forgive(forgiven);
                _cheats.PlayerWon(Rules);
            }
            else if (showdown.Outcome == ShowdownOutcome.HouseWins)
                _ledger.Add(_payouts.GetYearsAdded(showdown.House.Category, CurrentStake, Ante, LossSurcharge(IsSoulHand)));

            HouseReRaiseAmount = 0;
            PlayerCardsRevealed = Hand.Size;
            HouseCardsRevealed = Hand.Size;
            Phase = _ledger.IsServed ? GamePhase.Absolved
                : _ledger.Years >= Rules.DamnationYears ? GamePhase.Damned
                : GamePhase.RoundOver;

            LastRound = new RoundResult(CurrentStake, showdown == null, _playerExchange, _houseExchange, showdown,
                yearsBefore, _ledger.Years, Phase);
        }

        /// <summary>
        /// What a win forgives. While Lucifer waits below, an ordinary table never ends the sentence: the last year stays,
        /// and only the absolution hand (which forgives everything) walks out from here.
        /// </summary>
        private int Forgiven(HandCategory playerCategory)
        {
            int forgiven = _payouts.GetYearsForgiven(playerCategory, CurrentStake, Ante, _ledger.Years);
            if (Rules.KeepsTheLastYear && !_payouts.IsAbsolution(playerCategory))
                forgiven = Math.Min(forgiven, _ledger.Years - 1);
            return forgiven;
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
            IsSoulHand = false;
            _sealed = false;
            DecisionsSkipped = 0;
            _handPurse = 0;
            PlayerCardsRevealed = 0;
            HouseCardsRevealed = 0;
            _playerExchange = null;
            _houseExchange = null;
            _drawnIndices = Array.Empty<int>();
            ThornYearsThisHand = 0;
            TitheYearsThisHand = 0;
            _cheats.ClearHand();
        }

        private void RequirePhase(GamePhase expected)
        {
            if (Phase != expected)
                throw new InvalidOperationException($"Expected phase {expected}, but the game is in {Phase}.");
        }
    }
}
