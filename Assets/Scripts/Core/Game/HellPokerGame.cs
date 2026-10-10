using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Randomness;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Game
{
    /// <inheritdoc cref="IHellPokerGame"/>
    /// <remarks>Build with <see cref="HellPokerGameFactory"/> unless you need custom parts (tests, special modes).</remarks>
    public sealed class HellPokerGame : IHellPokerGame, IEventTable
    {
        private readonly IDeck _deck;
        private readonly IHandEvaluator _evaluator;
        private readonly ICardExchanger _exchanger;
        private readonly IDrawStrategy _houseStrategy;
        private readonly IPayoutTable _payouts;
        private readonly IHouseBettingStrategy _houseBetting;
        private readonly IHouseFoldStrategy _houseFolding;
        private readonly PunishmentLedger _ledger;

        private ExchangeResult _playerExchange;
        private ExchangeResult _houseExchange;

        private readonly CheatSession _cheats;

        /// <summary>The run's sinner class and what is left of its ability; null for a classless game (tests, old runs).</summary>
        public Sinner Sinner { get; }

        /// <summary>How many House cards turn before the last decision: the table's rule, or more for a class that sees through
        /// a demon's concealment (the Warlock at Belial's table).</summary>
        public int HouseCardsShown => ThisHand.HouseCardsShown >= 0 ? ThisHand.HouseCardsShown
            : Math.Max(0, (Sinner?.Class.HouseCardsShownAt(Rules) ?? Rules.HouseCardsShown) + Relic.HouseCardsDelta);

        /// <summary>The relics' combined effects on this hand (fixed at the deal).</summary>
        public RelicEffects Relic { get; private set; } = RelicEffects.None;

        /// <summary>Cards that may still be redrawn at this table (the Bone Die; the run's, full again at a new table).</summary>
        public int RedrawsLeft => Effects.RedrawsLeft;

        public bool CanRedraw(int index)
        {
            if (RedrawsLeft <= 0 || PlayerHand == null || index < 0 || index >= Hand.Size || _deck.Count == 0) return false;
            bool beforeDraw = Phase == GamePhase.Drawing || (Phase == GamePhase.PlayerReveal && !IsAfterDraw);
            if (!beforeDraw || index >= PlayerCardsRevealed || IsPlayerCardHidden(index)) return false;
            // A chained card must stay; a thorned one is not shaken off this way.
            return !IsPlayerCardChained(index) && !IsPlayerCardThorned(index);
        }

        /// <summary>The Bone Die: the card goes back, the next card of the deck takes its place. Returns the new card, or null.</summary>
        public Card? Redraw(int index)
        {
            if (!CanRedraw(index) || !Effects.SpendRedraw()) return null;
            Card card = _deck.Draw();
            PlayerHand = PlayerHand.With(index, card);
            return card;
        }

        /// <summary>What a won hand forgives more (the King's crown): a share of the ante.</summary>
        private int CrownBonus(int ante) => AnteShare(ante, Sinner?.Class.WinAntePercent ?? 0);

        /// <summary>A share of the ante, in percent, rounded up (the crown, the Rattle).</summary>
        private static int AnteShare(int ante, int percent) => (ante * percent + 99) / 100;
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

        /// <summary>House cards that play face up from the deal (a floor's imp's eye, bought at the black market); 0 at every table.</summary>
        public int HouseCardsOpenAtDeal { get; set; }
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
            ? Math.Max(0, Math.Min(SoulWorth - Effects.SoulSold, SoulWorth - Effects.SoulSold - (_ledger.Years - Rules.SoulThreshold)))
            : SoulWorth;

        /// <summary>What may still be wagered this hand: the sentence normally, what is left of the soul when it is on the table.</summary>
        private int Purse => IsSoulHand ? _handPurse : _ledger.Years;

        public int WagerLeft => Math.Max(0, Purse - CurrentStake);

        /// <summary>Bets are measured against the soul's worth once it is on the table, against the sentence otherwise.</summary>
        private int StakeBase => IsSoulAtStake ? SoulWorth : _ledger.Years;

        private int AvailableForNextHand => IsSoulAtStake ? SoulRemaining : _ledger.Years;

        /// <summary>The next hand's ante, with the marks it will be dealt under (an event's, the relics').</summary>
        public int UpcomingAnte => Math.Min(AnteUnder(StakeBase, Effects.NextHand ?? HandModifier.None, Effects.CombinedRelics),
            AvailableForNextHand);

        /// <summary>The ante for a sentence (or soul) of <paramref name="stakeBase"/>: units an event sets, or the rule's ante
        /// scaled by the event's and the relics' percents (rounded up, at least 1).</summary>
        private int AnteUnder(int stakeBase, HandModifier hand, RelicEffects relic) =>
            hand.AnteUnits > 0 ? hand.AnteUnits * Rules.Stakes.UnitFor(stakeBase)
                : Math.Max(1, (Rules.Stakes.AnteFor(stakeBase) * hand.AntePercent * relic.AntePercent / 100 + 99) / 100);

        public int RaiseAmount
        {
            get
            {
                if (!IsDecisionPhase(Phase)) return 0;
                int units = IsAfterDraw ? Rules.RaiseUnitsAfterDraw : Rules.RaiseUnitsBeforeDraw;
                return RoomToRaise(units * Unit);
            }
        }

        public int LeastYearsForgiven
        {
            get
            {
                // In a hand: its own marks; between hands: the ones the next hand will be dealt under.
                bool inHand = CurrentStake > 0;
                int eventPercent = inHand ? ThisHand.WinPercent : (Effects.NextHand ?? HandModifier.None).WinPercent;
                int relicPercent = (inHand ? Relic : Effects.CombinedRelics).WinPercent;
                RelicEffects relic = inHand ? Relic : Effects.CombinedRelics;
                return Math.Min(_ledger.Years, Scaled(_payouts.GetLeastYearsForgiven(StakeForOutlook, AnteForOutlook, int.MaxValue) + CrownBonus(AnteForOutlook)
                    + AnteShare(AnteForOutlook, relic.WinAntePercent), eventPercent, relicPercent));
            }
        }

        private static int Scaled(int years, int eventPercent, int relicPercent) => (int)((long)years * eventPercent / 100 * relicPercent / 100);
        public int LeastYearsAdded => _payouts.GetLeastYearsAdded(StakeForOutlook, AnteForOutlook, LossSurcharge(CurrentStake > 0 ? IsSoulHand : IsSoulAtStake))
            + AnteShare(AnteForOutlook, (CurrentStake > 0 ? Relic : Effects.CombinedRelics).LossAntePercent);

        private int StakeForOutlook => CurrentStake > 0 ? CurrentStake : UpcomingAnte;
        private int AnteForOutlook => CurrentStake > 0 ? Ante : UpcomingAnte;

        private int _handPurse;

        private int LossSurcharge(bool soulHand) => !soulHand ? 100 : Relic.SoulLossPercent >= 0 ? Relic.SoulLossPercent : Rules.SoulLossPercent;

        /// <param name="houseBetting">How the house answers raises after the draw; null for a house that never re-raises.</param>
        /// <param name="cheats">The demon's cheating at this table; null for an honest table.</param>
        /// <param name="houseFolding">Whether the House gives up facing a raise after the draw (a floor's imp); null: it never does.</param>
        public HellPokerGame(GameRules rules, IDeck deck, IHandEvaluator evaluator, ICardExchanger exchanger,
            IDrawStrategy houseStrategy, IPayoutTable payouts, IHouseBettingStrategy houseBetting = null, CheatSession cheats = null,
            IRandomSource cheatRandom = null, Sinner sinner = null, IHouseFoldStrategy houseFolding = null)
        {
            _houseFolding = houseFolding;
            Sinner = sinner;
            _cheats = cheats ?? new CheatSession(null, 0, null);
            _cheatRandom = cheatRandom;
            if (_cheats.IsActive && cheatRandom == null) throw new ArgumentNullException(nameof(cheatRandom));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            // Jokers are judged as the best card they can be wherever a hand is judged (without jokers: the plain evaluator).
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            _evaluator = evaluator is WildJokerEvaluator ? evaluator : new WildJokerEvaluator(evaluator);
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
            _deck.Reset();   // a new run: a fresh deck
            ClearHand();
            LastRound = null;
            RoundNumber = 0;
            Phase = GamePhase.Betting;
        }

        public void PlaceBet()
        {
            RequirePhase(GamePhase.Betting);
            DeferredPaid = 0;

            // With the soul on the table the bets are measured against the soul's worth and limited to what is left of it.
            IsSoulHand = IsSoulAtStake;
            int stakeBase = StakeBase;
            _handPurse = AvailableForNextHand;
            Unit = Rules.Stakes.UnitFor(stakeBase);
            // An event's mark on this hand (Charon's half ante, the burning bridge's three units and no cap...).
            ThisHand = Effects.NextHand ?? HandModifier.None;
            Effects.NextHand = HandModifier.None;
            // The relics the run carries: their gifts and curses on every hand.
            Relic = Effects.CombinedRelics;
            Ante = Math.Min(AnteUnder(stakeBase, ThisHand, Relic), _handPurse);
            TableCap = ThisHand.NoCap ? _handPurse : Math.Max(Ante, Math.Min(Rules.Stakes.CapFor(stakeBase), _handPurse));
            CurrentStake = Ante;
            PrepareDeck();
            PlayerHand = _deck.DealHand();
            HouseHand = _deck.DealHand();
            if (ThisHand.GhostSeed.HasValue)
                PlayerHand = GhostHand(ThisHand.GhostSeed.Value) ?? PlayerHand;   // the lost soul plays it
            PlayerCardsRevealed = Math.Min(Hand.Size, Rules.OpeningCardsShown + 1);
            HouseCardsRevealed = Math.Max(0, Math.Min(Hand.Size - 1, HouseCardsOpenAtDeal));
            IsAfterDraw = false;
            _sealed = false;
            DecisionsSkipped = 0;
            RoundNumber++;
            Phase = GamePhase.PlayerReveal;
            _cheats.BeginHand(Rules, _ledger.Years, Relic.MaliceExtraPerHand);
            Strike(CheatTiming.AfterDeal);
            NoteCommitment();
            SkipEmptyDecisions();
        }

        // ------------------------------------------------------------------ the deck (it goes on from hand to hand)

        public int DeckCount => _deck.Count;

        /// <summary>A whole deck (the 52; the Jester's jokers are never counted).</summary>
        public const int FullDeck = 52;

        /// <summary>The cards left, the next one to be drawn first (for the save and the next table); empty for a deck not counted.</summary>
        public IReadOnlyList<Card> DeckCards => !Rules.ContinuousDeck || JokerDeck ? Array.Empty<Card>() : _deck.Remaining.Reverse().ToArray();

        /// <summary>
        /// The run's deck goes on here (another table, or a saved run): the cards left, in order. Ignored for a deck that is not
        /// counted (the Jester's, or without the rule) and for anything that is not a part of one deck (a card twice, a joker).
        /// </summary>
        public bool RestoreDeck(IReadOnlyList<Card> cardsInDrawOrder)
        {
            if (cardsInDrawOrder == null || !Rules.ContinuousDeck || JokerDeck) return false;
            if (cardsInDrawOrder.Any(c => c.IsJoker) || cardsInDrawOrder.Distinct().Count() != cardsInDrawOrder.Count) return false;
            _deck.Restore(cardsInDrawOrder);
            return true;
        }

        /// <summary>True when this hand began with the demon shuffling a deck run too thin.</summary>
        public bool DeckShuffledThisHand { get; private set; }

        /// <summary>The player shuffled (SHUFFLE) since the last deal.</summary>
        public bool ShuffledThisHand { get; private set; }

        /// <summary>How many times the demon shuffled a thin deck, and the player paid to shuffle, at this table.</summary>
        public int AutoShuffles { get; private set; }
        public int PlayerShuffles { get; private set; }

        /// <summary>The Jester's deck (jokers in it): a fresh shuffle every hand, nothing to count.</summary>
        private bool JokerDeck => Sinner != null && Sinner.Class.StartingJokers > 0;

        /// <summary>
        /// The most cards one hand can take from the deck: both hands, both draws at their fullest, the relics' redraws, the most a
        /// cheat deals (The Fall: one card for each hand), and a lost soul's ghost hand.
        /// </summary>
        public int CardsForAHand
        {
            get
            {
                HandModifier hand = CurrentStake > 0 ? ThisHand : Effects.NextHand ?? HandModifier.None;
                RelicEffects relic = CurrentStake > 0 ? Relic : Effects.CombinedRelics;
                return 2 * Hand.Size + 2 * Rules.MaxDiscards + relic.RedrawsPerTable + CheatTable.MostCardsACheatDeals
                       + (hand.GhostSeed.HasValue ? Hand.Size : 0);
            }
        }

        /// <summary>
        /// Before a deal: the deck goes on as it is — unless it could run out within this hand, when the demon shuffles all 52
        /// again. The Jester's deck (and a table without the rule) is shuffled fresh every hand.
        /// </summary>
        private void PrepareDeck()
        {
            DeckShuffledThisHand = false;
            ShuffledThisHand = false;
            // The Jester's deck: as many jokers as the run has earned (none for any other class).
            if (_deck is IJokerDeck jokerDeck) jokerDeck.SetJokers(Sinner?.Jokers ?? 0);
            if (!Rules.ContinuousDeck || JokerDeck)
            {
                _deck.Reset();
                return;
            }
            if (_deck.Count >= CardsForAHand) return;
            _deck.Reset();
            DeckShuffledThisHand = true;
            AutoShuffles++;
        }

        public ShuffleRefusal WhyNoShuffle()
        {
            if (!Rules.ContinuousDeck || JokerDeck) return ShuffleRefusal.NoDeckToCount;
            if (Phase != GamePhase.Betting) return ShuffleRefusal.NotBetweenHands;
            if (IsSoulAtStake) return ShuffleRefusal.SoulOnTable;
            if (ShuffledThisHand) return ShuffleRefusal.AlreadyShuffled;
            if (_deck.Count >= FullDeck) return ShuffleRefusal.DeckFull;
            if (_deck.Count < CardsForAHand) return ShuffleRefusal.ShuffleComing;   // the demon shuffles it anyway, for nothing
            if (_ledger.Years < Rules.ShuffleMinYears) return ShuffleRefusal.TooFewYears;
            if (_ledger.Years + Rules.ShuffleYears >= Rules.SoulThreshold) return ShuffleRefusal.WouldStakeSoul;
            return ShuffleRefusal.None;
        }

        public bool Shuffle()
        {
            if (WhyNoShuffle() != ShuffleRefusal.None) return false;
            _ledger.Add(Rules.ShuffleYears);
            _deck.Reset();
            ShuffledThisHand = true;
            PlayerShuffles++;
            return true;
        }

        // ------------------------------------------------------------------ the demon's cheats

        public int Malice => _cheats.Malice;
        public int MaliceMax => _cheats.MaliceMax;
        public ICheat PendingCheat => InPlay ? _cheats.Intent : null;
        public IReadOnlyList<CheatResult> CheatsThisHand => _cheats.Results;
        public bool MajorCheatUsed => _cheats.MajorUsed;
        public int Grudge => _cheats.Grudge;
        public int ThornYearsThisHand { get; private set; }
        public int TitheYearsThisHand { get; private set; }

        /// <summary>While the hand is played: true for a card of the player's the player cannot see (veiled, moonless, swapped in).</summary>
        public bool IsPlayerCardHidden(int index) => InPlay && _cheats.Marks.HiddenFromPlayer.Contains(PlayerHand[index]);

        public bool WasPlayerCardHidden(int index) => PlayerHand != null && _cheats.Marks.HiddenFromPlayer.Contains(PlayerHand[index]);

        public bool IsPlayerCardChained(int index) => InPlay && _cheats.Marks.Chained.Contains(PlayerHand[index]);

        public bool IsPlayerCardThorned(int index) => InPlay && _cheats.Marks.Thorned.Contains(PlayerHand[index]);

        /// <summary>The King's crown guards the whole hand this hand (every card in it, the new ones too).</summary>
        public bool IsPlayerCardProtected(int index) => InPlay && Sinner != null && Sinner.HandProtected;

        /// <summary>The cheat really planned for this hand while it is still to come (a Warlock sees through the lie).</summary>
        public ICheat PendingCheatTruth => InPlay && _cheats.Intent != null ? _cheats.Planned : null;

        // ------------------------------------------------------------------ the sinner's power (a full charge gauge)

        /// <summary>Why the class's power cannot be used right now; <see cref="PowerRefusal.None"/> when it can.</summary>
        public PowerRefusal WhyNoPower()
        {
            if (Sinner == null || Sinner.Ability == SinnerAbility.None) return PowerRefusal.NoPower;
            if (Sinner.HandProtected) return PowerRefusal.HandAlreadyProtected;   // said before the empty gauge: it is the news
            if (!Sinner.IsCharged) return PowerRefusal.NotCharged;
            // The Peasant's power is switched on at any time and waits for his next fold (this hand or a later one).
            if (Sinner.Ability == SinnerAbility.FreeFold) return PowerRefusal.None;
            if (!InPlay) return PowerRefusal.NoHand;
            switch (Sinner.Ability)
            {
                case SinnerAbility.Ward:
                    if (Sinner.WardRaised) return PowerRefusal.WardAlreadyRaised;
                    ICheat coming = PendingCheatTruth;
                    if (coming == null) return PowerRefusal.NoCheatAnnounced;
                    return coming.Tier == CheatTier.Minor ? PowerRefusal.None : PowerRefusal.MajorCheat;
                case SinnerAbility.Protect:
                    // The crown guards the hand against a cheat announced and still to come (the King cannot read a lie: any
                    // announcement will do).
                    if (Sinner.HandProtected) return PowerRefusal.HandAlreadyProtected;
                    return PendingCheatTruth == null ? PowerRefusal.NoCheatAnnounced : PowerRefusal.None;
                default:
                    return PowerRefusal.NoPower;
            }
        }

        /// <summary>
        /// Uses — or, for the Peasant, switches on — the power: the Peasant's free fold waits, switched on, for his fold (the gauge
        /// is spent only then); the Warlock's ward and the King's crown go up at once (they wait for the cheat themselves). False
        /// when the power cannot be used now (<see cref="WhyNoPower"/>).
        /// </summary>
        public bool ArmPower()
        {
            if (WhyNoPower() != PowerRefusal.None) return false;
            return Sinner.Ability == SinnerAbility.FreeFold ? Sinner.Arm() : Sinner.TryUse(Sinner.Ability);
        }

        /// <summary>Switches a waiting power off again (the player changed their mind); the gauge stays full.</summary>
        public void DisarmPower() => Sinner?.Disarm();

        /// <summary>The power is switched on and waiting for the player's move (the Peasant's free fold).</summary>
        public bool PowerArmed => Sinner != null && Sinner.PowerArmed;

        /// <summary>
        /// Uses the power in one go: the Peasant walks away from this hand for nothing (switched on, then folded), the Warlock raises
        /// a ward, the King's crown guards the hand. False when it cannot be used now.
        /// </summary>
        public bool UsePower()
        {
            if (WhyNoPower() != PowerRefusal.None) return false;
            switch (Sinner.Ability)
            {
                case SinnerAbility.FreeFold:
                    if (!CanBet(BetAction.Fold, out _)) return false;
                    Sinner.Arm();
                    Bet(BetAction.Fold);
                    return true;
                case SinnerAbility.Ward:
                case SinnerAbility.Protect:
                    return Sinner.TryUse(Sinner.Ability);
                default:
                    return false;
            }
        }
        /// <summary>True while a House card shows a false face (until the showdown).</summary>
        public bool IsHouseCardFalse(int index) => InPlay && _cheats.Marks.FakeHouseIndex == index;

        /// <summary>The House card as the player sees it: a false face until the showdown turns the truth.</summary>
        public Card HouseCardFace(int index) => IsHouseCardFalse(index) ? _cheats.Marks.FakeHouseFace : HouseHand[index];

        public void RestoreMalice(int malice, bool majorCheatUsed, int grudge = 0)
        {
            RequirePhase(GamePhase.Betting);
            _cheats.Restore(malice, majorCheatUsed, grudge);
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
                // A floor's imp may give up its hand rather than face a raise past the draw: the stake on the table is the player's.
                if (IsAfterDraw && HouseFolds())
                {
                    Finish(showdown: null, houseFolded: true);
                    return;
                }
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
            IsDecisionPhase(Phase) || Phase == GamePhase.Drawing || Phase == GamePhase.HouseReRaise || Phase == GamePhase.NamingJoker
                ? new HandInProgress(CurrentStake, Ante, IsAfterDraw, IsSoulHand, _sealed, _cheats.Planned?.Id, _cheats.IsResolved)
                : null;

        public RoundResult ForfeitHand()
        {
            HandInProgress hand = CurrentHand ?? throw new InvalidOperationException("No hand is being played.");
            ClearHand();
            Phase = GamePhase.Betting;
            return ForfeitHand(hand);
        }

        public RoundResult ForfeitHand(HandInProgress hand)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            RequirePhase(GamePhase.Betting);

            int surcharge = LossSurcharge(hand.IsSoulHand);
            int penalty = hand.IsSealed
                ? _payouts.GetLeastYearsAdded(hand.Stake, hand.Ante, surcharge)
                : _payouts.GetFoldPenalty(hand.Stake, hand.IsAfterDraw, surcharge);

            // Walking out on a hand the demon meant to cheat is not forgotten: the cheat comes next hand, and more follow.
            if (hand.FledACheat)
                _cheats.PlayerFled(Rules);

            int yearsBefore = _ledger.Years;
            _ledger.Add(penalty);
            Phase = _ledger.Years >= DamnationYears ? GamePhase.Damned : GamePhase.Betting;
            LastRound = new RoundResult(hand.Stake, true, null, null, null, yearsBefore, _ledger.Years, Phase);
            // A hand left behind counts as the fold it was — or, sealed, as the loss.
            Sinner?.HandSettled(!hand.IsSealed, hand.IsSealed ? ShowdownOutcome.HouseWins : (ShowdownOutcome?)null);
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
            int yearsBefore = _ledger.Years;
            int thorns = ThornsIn(discardIndices);
            if (thorns > 0)
            {
                ThornYearsThisHand = thorns * Unit;
                _ledger.Add(ThornYearsThisHand);
            }

            _playerExchange = _exchanger.Exchange(PlayerHand, discardIndices, _deck);
            // The House draws what the deck still holds (never more: an empty deck — which a counted deck keeps clear of — is no error).
            int[] houseDiscards = _houseStrategy.ChooseDiscards(HouseHand).Take(_deck.Count).ToArray();
            _houseExchange = _exchanger.Exchange(HouseHand, houseDiscards, _deck);
            PlayerHand = _playerExchange.Hand;
            HouseHand = _houseExchange.Hand;
            _drawnIndices = _playerExchange.ReplacedIndices;
            IsAfterDraw = true;

            // The thorn took the last of the soul: there is no hand left to play.
            if (_ledger.Years >= DamnationYears)
            {
                HouseReRaiseAmount = 0;
                PlayerCardsRevealed = Hand.Size;
                HouseCardsRevealed = Hand.Size;
                Phase = GamePhase.Damned;
                LastRound = new RoundResult(CurrentStake, true, _playerExchange, _houseExchange, null, yearsBefore, _ledger.Years, Phase,
                    thornDamned: true);
                return _playerExchange;
            }

            // The cards change no more: if the jokers already decide the hand, it ends here — no betting round after the draw.
            if (SettleIfJokersDecide()) return _playerExchange;

            Phase = GamePhase.DrawReveal;
            Strike(CheatTiming.AfterDraw);
            SkipEmptyDecisions();

            return _playerExchange;
        }

        public int ThornCost(IReadOnlyCollection<int> discardIndices) => ThornsIn(discardIndices) * Unit;

        private int ThornsIn(IReadOnlyCollection<int> discardIndices) =>
            discardIndices == null ? 0 : discardIndices.Distinct().Count(i => i >= 0 && i < Hand.Size && IsPlayerCardThorned(i));

        public void NextRound()
        {
            RequirePhase(GamePhase.RoundOver);
            ClearHand();
            Phase = GamePhase.Betting;

            // A sentence deferred to later (Mammon's ledger) comes due between hands.
            DeferredPaid = Effects.HandSettled();
            if (DeferredPaid > 0)
            {
                _ledger.Add(DeferredPaid);
                if (_ledger.Years >= DamnationYears) Phase = GamePhase.Damned;
            }
        }

        // ------------------------------------------------------------------ the run's events

        /// <summary>The run's events and their marks (shared by every table's game; see <see cref="UseEffects"/>).</summary>
        public RunEffects Effects { get; private set; } = new RunEffects();

        /// <summary>How this hand differs, by an event; <see cref="HandModifier.None"/> between hands and for an ordinary hand.</summary>
        public HandModifier ThisHand { get; private set; } = HandModifier.None;

        /// <summary>Years that came due between the last hand and this one (Mammon's ledger); 0 for none.</summary>
        public int DeferredPaid { get; private set; }

        /// <summary>Damnation comes this early: the soul line plus what is left of the soul's worth (some may have been sold).</summary>
        public int DamnationYears => Rules.DamnationYears - Effects.SoulSold;

        public void UseEffects(RunEffects effects)
        {
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        int IEventTable.Years => _ledger.Years;
        int IEventTable.DealtHands => RoundNumber;

        public void ForgiveYears(int years)
        {
            RequirePhase(GamePhase.Betting);
            // An event never ends a sentence: the last year stays (only a hand — at the right table — can end it).
            _ledger.Forgive(Math.Max(0, Math.Min(years, _ledger.Years - 1)));
        }

        public void AddYears(int years)
        {
            RequirePhase(GamePhase.Betting);
            _ledger.Add(Math.Max(0, years));
            if (_ledger.Years >= DamnationYears) Phase = GamePhase.Damned;
        }

        public void EmptyMalice() => _cheats.Restore(0, _cheats.MajorUsed, _cheats.Grudge);

        /// <summary>
        /// The lost soul's hand: two pair or three of a kind, made of cards still in the deck (the dealt ones leave play).
        /// Null when the deck cannot make one.
        /// </summary>
        private Hand GhostHand(int seed)
        {
            var random = new Randomness.SystemRandomSource(seed);
            var byRank = _deck.Remaining.GroupBy(c => c.Rank).ToDictionary(g => g.Key, g => g.ToList());
            bool trips = random.Next(2) == 0;
            var ranks = byRank.Keys.OrderBy(r => r).ToList();
            var made = new List<Card>();
            if (trips)
            {
                var three = ranks.Where(r => byRank[r].Count >= 3).ToList();
                if (three.Count == 0) return null;
                Rank r3 = three[random.Next(three.Count)];
                made.AddRange(byRank[r3].Take(3));
            }
            else
            {
                var two = ranks.Where(r => byRank[r].Count >= 2).ToList();
                if (two.Count < 2) return null;
                Rank a = two[random.Next(two.Count)];
                two.Remove(a);
                Rank b = two[random.Next(two.Count)];
                made.AddRange(byRank[a].Take(2));
                made.AddRange(byRank[b].Take(2));
            }
            var kickers = ranks.Where(r => made.All(c => c.Rank != r)).ToList();
            while (made.Count < Hand.Size && kickers.Count > 0)
            {
                Rank k = kickers[random.Next(kickers.Count)];
                kickers.Remove(k);
                made.Add(byRank[k][0]);
            }
            if (made.Count < Hand.Size) return null;
            foreach (Card card in made) _deck.Take(card);
            return new Hand(made);
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
                : _ledger.Years >= DamnationYears ? GamePhase.Damned
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
            if (SettleIfJokersDecide()) return;
            while (IsDecisionPhase(Phase) && !HasRealChoice())
            {
                DecisionsSkipped++;
                Advance(Phase);
                if (SettleIfJokersDecide()) return;
            }
        }

        // ------------------------------------------------------------------ a hand the jokers already decide

        /// <summary>
        /// The showdown the jokers have already decided, whatever is still to come; null while it is open (the cards may still decide it).
        /// After the draw the counts are final (the cards change no more: the Bone Die works before the draw, no cheat touches a joker).
        /// Before it, every count the player could still reach (keeping what cannot be thrown back, throwing up to the draw's limit, drawing
        /// the deck's jokers) against every count the House could (it keeps one joker at most, then may draw more) must give one outcome.
        /// A side with two jokers or more loses; both: the fewer wins, as many each is a push; neither: the cards decide — never settled.
        /// </summary>
        public ShowdownOutcome? JokerOutcomeIsSettled()
        {
            if (PlayerHand == null || HouseHand == null) return null;
            int p = JokerResolver.CountJokers(PlayerHand), h = JokerResolver.CountJokers(HouseHand);
            int inDeck = JokerResolver.CountJokers(_deck.Remaining);
            if (p + h + inDeck == 0) return null;   // a deck without jokers: the cards always decide

            int pMin = p, pMax = p, hMin = h, hMax = h;
            if (!IsAfterDraw)
            {
                int locked = Enumerable.Range(0, Hand.Size).Count(i => PlayerHand[i].IsJoker && IsPlayerCardChained(i));
                pMin = Math.Max(locked, p - Rules.MaxDiscards);
                pMax = Math.Min(Hand.Size, p + Math.Min(inDeck, Rules.MaxDiscards));
                hMin = Math.Max(Math.Min(h, 1), h - Rules.MaxDiscards);
                hMax = Math.Min(Hand.Size, hMin + Math.Min(inDeck, Rules.MaxDiscards));
            }

            ShowdownOutcome? settled = null;
            for (int pj = pMin; pj <= pMax; pj++)
            for (int hj = hMin; hj <= hMax; hj++)
            {
                ShowdownOutcome? outcome = JokerVerdict(pj, hj);
                if (outcome == null) return null;   // the cards could decide
                if (settled == null) settled = outcome;
                else if (settled != outcome) return null;
            }
            return settled;
        }

        /// <summary>The jokers' own verdict: null when neither side holds two (the cards decide).</summary>
        private static ShowdownOutcome? JokerVerdict(int playerJokers, int houseJokers)
        {
            bool playerBust = playerJokers >= ShowdownResult.BustJokers, houseBust = houseJokers >= ShowdownResult.BustJokers;
            if (!playerBust && !houseBust) return null;
            if (playerBust && houseBust)
                return playerJokers < houseJokers ? ShowdownOutcome.PlayerWins : playerJokers > houseJokers ? ShowdownOutcome.HouseWins : ShowdownOutcome.Push;
            return playerBust ? ShowdownOutcome.HouseWins : ShowdownOutcome.PlayerWins;
        }

        /// <summary>A hand the jokers have decided ends at once, as a showdown at the stake on the table (no more betting).</summary>
        private bool SettleIfJokersDecide()
        {
            bool open = IsDecisionPhase(Phase) || Phase == GamePhase.Drawing || (IsAfterDraw && Phase != GamePhase.RoundOver && !IsGameOver);
            if (!open || Phase == GamePhase.HouseReRaise || JokerOutcomeIsSettled() == null) return false;
            HouseReRaiseAmount = 0;
            _settledByJokers = true;
            Finish(JudgeShowdown(null));
            return true;
        }

        private bool _settledByJokers;

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

                case GamePhase.DrawReveal when HouseCardsShown > 0:
                    HouseCardsRevealed = Math.Max(HouseCardsRevealed, HouseCardsShown);
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
            int amount = Math.Max(0, Math.Min((Rules.HouseReRaiseUnits + Relic.ReRaiseExtraUnits) * Unit, WagerLeft));
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
                ShowdownOutcome outcome = JudgeShowdown(null).Outcome;
                if (outcome == ShowdownOutcome.HouseWins) return true;
                if (outcome == ShowdownOutcome.PlayerWins) return _cheatRandom.Next(100) < GazeBluffPercent;
            }
            // Two jokers lose at the showdown: the House does not raise on them.
            if (JokerResolver.CountJokers(HouseHand) >= 2) return false;
            return _houseBetting != null && _houseBetting.WantsToReRaise(_evaluator.Evaluate(HouseHand));
        }

        /// <summary>Two jokers lose anyway: an imp holding them has nothing to give up (it plays on, as a demon never raises them).</summary>
        private bool HouseFolds() =>
            _houseFolding != null && JokerResolver.CountJokers(HouseHand) < ShowdownResult.BustJokers && _houseFolding.WantsToFold(_evaluator.Evaluate(HouseHand));

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
            ShowdownResult showdown = JudgeShowdown(null);
            ShowdownResult after = Strike(CheatTiming.BeforeShowdown, showdown);
            if (JokerResolver.CountJokers(PlayerHand) + JokerResolver.CountJokers(HouseHand) == 0)
            {
                Finish(after);
                return;
            }

            // The Jester's deck: every card turns; a single joker of the player's waits for its name, the House names its own.
            PlayerCardsRevealed = Hand.Size;
            HouseCardsRevealed = Hand.Size;
            // The picker opens only when the card the joker becomes can change the outcome; otherwise it becomes its best card at once.
            if (JokerResolver.CountJokers(PlayerHand) == 1 && JokerCanChangeTheOutcome())
            {
                Phase = GamePhase.NamingJoker;
                return;
            }
            Finish(JudgeShowdown(null));
        }

        // ------------------------------------------------------------------ the jokers (the Jester's deck)

        /// <summary>
        /// The showdown as the jokers make it: the House's single joker as its best card, the player's as <paramref name="named"/>
        /// (or its best card when null); a side with two jokers or more loses.
        /// </summary>
        private ShowdownResult JudgeShowdown(Card? named)
        {
            Hand player = named.HasValue ? JokerResolver.Name(PlayerHand, named.Value) ?? PlayerHand : PlayerHand;
            return ShowdownResult.Judge(_evaluator.Evaluate(player), _evaluator.Evaluate(HouseHand),
                JokerResolver.CountJokers(PlayerHand), JokerResolver.CountJokers(HouseHand));
        }

        /// <summary>True when naming the player's joker one card or another could turn the showdown (win, loss, push).</summary>
        private bool JokerCanChangeTheOutcome()
        {
            ShowdownOutcome? first = null;
            foreach (Card card in JokerResolver.Choices(PlayerHand))
            {
                ShowdownOutcome outcome = JudgeShowdown(card).Outcome;
                if (first == null) first = outcome;
                else if (outcome != first) return true;
            }
            return false;
        }

        public IReadOnlyList<Card> JokerChoices => Phase == GamePhase.NamingJoker ? JokerResolver.Choices(PlayerHand) : Array.Empty<Card>();

        public Card BestJokerCard
        {
            get
            {
                RequirePhase(GamePhase.NamingJoker);
                Hand best = JokerResolver.Resolve(PlayerHand);
                int joker = Enumerable.Range(0, Hand.Size).First(i => PlayerHand[i].IsJoker);
                return best[joker];
            }
        }

        public HandEvaluation EvaluateJokerAs(Card card)
        {
            RequirePhase(GamePhase.NamingJoker);
            Hand named = JokerResolver.Name(PlayerHand, card) ?? throw new ArgumentException($"The joker cannot become {card}.", nameof(card));
            return _evaluator.Evaluate(named);
        }

        public bool CanNameJoker(Card card) => Phase == GamePhase.NamingJoker && JokerResolver.Name(PlayerHand, card) != null;

        public void NameJoker(Card card)
        {
            RequirePhase(GamePhase.NamingJoker);
            if (!CanNameJoker(card)) throw new ArgumentException($"The joker cannot become {card}.", nameof(card));
            Finish(JudgeShowdown(card));
        }

        /// <summary>Settles the hand. A null showdown means the player folded — or, <paramref name="houseFolded"/>, the House did.</summary>
        private void Finish(ShowdownResult showdown, bool houseFolded = false)
        {
            int yearsBefore = _ledger.Years;

            bool freeFold = false;
            if (houseFolded)
            {
                // The stake on the table, one to one (as the weakest winning hand), with the crown's and the relics' shares.
                _ledger.Forgive(Forgiven(HandCategory.HighCard));
                _cheats.PlayerWon(Rules);
            }
            else if (showdown == null)
            {
                // The Peasant's honest heart (his power, switched on): the fold costs nothing, and the gauge is spent now.
                freeFold = Sinner != null && Sinner.PowerArmed && Sinner.Ability == SinnerAbility.FreeFold && Sinner.TryUse(SinnerAbility.FreeFold);
                if (!freeFold)
                    _ledger.Add(_payouts.GetFoldPenalty(CurrentStake, IsAfterDraw, LossSurcharge(IsSoulHand)));
            }
            else if (showdown.Outcome == ShowdownOutcome.PlayerWins)
            {
                // A hand broken by jokers that still won (fewer jokers than the demon's) pays as the weakest hand: its own make means nothing.
                int forgiven = Forgiven(showdown.PlayerBust ? HandCategory.HighCard : showdown.Player.Category);
                if (_cheats.Marks.Tithe && !_payouts.IsAbsolution(showdown.Player.Category))
                {
                    TitheYearsThisHand = Math.Min(Unit, forgiven);
                    forgiven -= TitheYearsThisHand;
                }
                _ledger.Forgive(forgiven);
                _cheats.PlayerWon(Rules);
            }
            else if (showdown.Outcome == ShowdownOutcome.HouseWins)
                _ledger.Add((int)Math.Ceiling(_payouts.GetYearsAdded(showdown.HouseBust ? HandCategory.HighCard : showdown.House.Category, CurrentStake, Ante,
                                                  LossSurcharge(IsSoulHand)) *
                                              (ThisHand.LossPercent / 100.0))   // the lost soul doubles a loss
                            + AnteShare(Ante, Relic.LossAntePercent));        // the Rattle: half an ante more

            HouseReRaiseAmount = 0;
            PlayerCardsRevealed = Hand.Size;
            HouseCardsRevealed = Hand.Size;
            Phase = _ledger.IsServed ? GamePhase.Absolved
                : _ledger.Years >= DamnationYears ? GamePhase.Damned
                : GamePhase.RoundOver;

            // The power charges with every settled hand — but not the one the Peasant walked away from with it (his gauge is empty).
            bool playerFolded = showdown == null && !houseFolded;
            if (!freeFold) Sinner?.HandSettled(playerFolded, houseFolded ? ShowdownOutcome.PlayerWins : showdown?.Outcome);
            Sinner?.EndHand();   // the crown's guard lasts the hand
            // The Jester's twenty jokers: the deck is cleared of them, and the first time the Rattle joins the run (beyond the relic limit).
            bool jackpot = !freeFold && Sinner != null && Sinner.HitJokerJackpot;
            bool rattle = jackpot && Effects.AddRelic(RelicIds.JestersRattle);
            LastRound = new RoundResult(CurrentStake, playerFolded, _playerExchange, _houseExchange, showdown,
                yearsBefore, _ledger.Years, Phase, freeFold: freeFold, jokerJackpot: jackpot, rattleGiven: rattle, settledByJokers: _settledByJokers,
                houseFolded: houseFolded);
            _settledByJokers = false;
        }

        /// <summary>
        /// What a win forgives. While Lucifer waits below, an ordinary table never ends the sentence: the last year stays,
        /// and only the absolution hand (which forgives everything) walks out from here.
        /// </summary>
        private int Forgiven(HandCategory playerCategory)
        {
            int forgiven = _payouts.GetYearsForgiven(playerCategory, CurrentStake, Ante, _ledger.Years);
            if (!_payouts.IsAbsolution(playerCategory))
            {
                // The whole win first (with the King's crown), then an event's and the relics' percents (Charon halves it, Belial's
                // show doubles it, the Rosary takes a tenth), and only then the sentence's cap: a percent of a capped win would
                // never bring the last years down to zero.
                forgiven = Math.Min(_ledger.Years, Scaled(_payouts.GetYearsForgiven(playerCategory, CurrentStake, Ante, int.MaxValue) + CrownBonus(Ante)
                    + AnteShare(Ante, Relic.WinAntePercent), ThisHand.WinPercent, Relic.WinPercent));
                // The burning bridge brings the sentence down to its line.
                if (ThisHand.WinSetsYears >= 0 && _ledger.Years > ThisHand.WinSetsYears)
                    forgiven = Math.Max(forgiven, _ledger.Years - ThisHand.WinSetsYears);
            }
            if (Rules.KeepsTheLastYear && !_payouts.IsAbsolution(playerCategory))
                forgiven = Math.Min(forgiven, _ledger.Years - 1);
            return forgiven;
        }

        private void ClearHand()
        {
            ThisHand = HandModifier.None;
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
            Sinner?.EndHand();
            _cheats.ClearHand();
        }

        private void RequirePhase(GamePhase expected)
        {
            if (Phase != expected)
                throw new InvalidOperationException($"Expected phase {expected}, but the game is in {Phase}.");
        }
    }
}
