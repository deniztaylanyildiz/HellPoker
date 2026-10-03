using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
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

        /// <summary>Once the pact is sealed the remaining cards turn on their own, one by one, this far apart.</summary>
        public const float SealedRevealPause = 0.6f;

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

        /// <summary>Lucifer's table, where the player is summoned below the gate; null for a run without him.</summary>
        private readonly Dealer _finalDealer;

        /// <summary>The run's dealings with Lucifer (summons, attempts, where the player came from).</summary>
        private LuciferGate _gate = new LuciferGate(0, 0);

        /// <summary>The demon the player was summoned from — and is cast down to.</summary>
        private Dealer _origin;

        public event Action LeaveRequested;
        public event Action<RunSummary> RunEnded;

        /// <param name="createGame">Builds a fresh game for a run at this dealer's table.</param>
        /// <param name="guide">The player's guide settings; without them the hand guide is on and no first-game tips are told.</param>
        /// <param name="archive">Where the run is saved after every hand and the records are kept; without it nothing is saved.</param>
        /// <param name="finalDealer">Lucifer: below the gate the player is summoned to his table. Without him every table can
        /// set the player free.</param>
        public TablePresenter(Func<Dealer, IHellPokerGame> createGame, ITableView view, IGuideSettings guide = null, RunArchive archive = null,
            Dealer finalDealer = null)
        {
            _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _guide = guide;
            _archive = archive;
            _finalDealer = finalDealer;
            _records = archive?.LoadRecords() ?? new RecordBook();

            _view.ActionPressed += PerformAction;
            _view.BetPressed += Bet;
            _view.CheckToDrawPressed += CheckToDraw;
            _view.LeavePressed += RequestLeave;
            _view.HandRanksPressed += ToggleHandRanks;
            _view.Player.CardClicked += ToggleDiscard;
        }

        public IReadOnlyCollection<int> SelectedDiscards => _discards;

        /// <summary>The game of the current table; null before the first run.</summary>
        public IHellPokerGame Game => _game;

        public bool CanContinue => _game != null && !_abandoned && _game.RoundNumber > 0 && !_game.IsGameOver;

        /// <summary>True once the player walked away from this run (a new game over it): nothing more is played or saved.</summary>
        private bool _abandoned;

        /// <summary>There is a table to play at: a run has started and has not been walked away from.</summary>
        private bool Playing => _game != null && !_abandoned;

        public AbandonRisk AbandonRisk
        {
            get
            {
                if (!CanContinue) return AbandonRisk.None;
                HandInProgress hand = _game.CurrentHand;
                if (_game.IsSoulAtStake || (hand != null && hand.IsSoulHand)) return AbandonRisk.Soul;
                return hand != null ? AbandonRisk.Hand : AbandonRisk.Run;
            }
        }

        /// <summary>
        /// The player walks away from the run (a new game started over it). A hand on the table is forfeited — a fold at the
        /// state it is in, as if the game had been closed now — and with the soul on the table the run counts as damned.
        /// Otherwise the run is simply forgotten.
        /// </summary>
        public void AbandonRun()
        {
            if (!CanContinue) return;
            _view.SkipAnimations();

            HandInProgress hand = _game.CurrentHand;
            bool soul = _game.IsSoulAtStake || (hand != null && hand.IsSoulHand);
            if (hand != null)
            {
                _game.ForfeitHand();
                _stats.RecordHand(_game.Years, null, soul || _game.IsSoulAtStake);
            }

            if (soul || _game.IsSoulAtStake || _game.Phase == GamePhase.Damned)
                _records.RunEnded(false, _dealer.IsFinalTable && _origin != null ? _origin.Id : _dealer.Id, _stats.HandsPlayed,
                    _gate.Attempts);
            _archive?.SaveRecords(_records);
            _archive?.ClearRun();
            _abandoned = true;
        }

        public string CurrentDealerId => _dealer?.Id;

        public RecordBook Records => _records;

        /// <summary>This run's story so far; null before the first run.</summary>
        public RunStats Stats => _stats;

        /// <summary>The run's dealings with Lucifer.</summary>
        public LuciferGate Gate => _gate;

        public bool IsAtFinalTable => _dealer != null && _dealer.IsFinalTable;

        public void StartNewRun(Dealer dealer)
        {
            SeatAt(dealer, carriedYears: null, roundsPlayed: 0);
            _gate = NewGate();
            _origin = null;
            BeginRun();
            _view.Dealer.Say(UiText.Pick(_dealerText.Greeting, 0), DealerMood.Neutral);
            Refresh();
        }

        /// <summary>
        /// Picks up a saved run: the same demon, sentence, hands and story. A hand that was still being played when the game
        /// closed is not played on — it is forfeited (a fold at the state it was left in; a sealed hand is lost whole).
        /// </summary>
        /// <param name="origin">At Lucifer's table: the demon the player was summoned from (where a fall lands).</param>
        public void Resume(Dealer dealer, RunSnapshot snapshot, Dealer origin = null)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SeatAt(dealer, snapshot.Years, snapshot.RoundsPlayed);
            if (_game.Phase == GamePhase.Betting)
                _game.RestoreMalice(snapshot.Malice, snapshot.MajorCheatUsed, snapshot.Grudge);   // the demon has not forgotten
            GameRules rules = _game.Rules;
            _gate = _finalDealer == null
                ? new LuciferGate(0, 0)
                : new LuciferGate(rules.LuciferGateYears, rules.LuciferCastDownYears, snapshot.AtLucifer, snapshot.OriginDealerId,
                    snapshot.LuciferAttempts);
            _origin = origin;
            if (!_game.IsSoulAtStake)
                ShowSentenceLine();   // the attempt is only known now
            _stats = snapshot.Stats;
            _stats.SatWith(dealer.Id);
            _settledRound = _game.RoundNumber;

            if (snapshot.Hand != null && !_game.IsGameOver)
            {
                ForfeitLeftHand(snapshot.Hand);
                return;
            }

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

        /// <summary>The game was closed mid-hand: the hand is settled as forfeit, and the dealer has a word about it.</summary>
        private void ForfeitLeftHand(HandInProgress hand)
        {
            RoundResult round = _game.ForfeitHand(hand);
            bool soul = hand.IsSoulHand || _game.IsSoulAtStake || _game.Phase == GamePhase.Damned;
            _stats.RecordHand(_game.Years, null, soul);
            if (_game.IsGameOver)
            {
                EndRunRecords();
                Refresh();   // the end of the run speaks for itself
                return;
            }

            // The demon whose hand was left speaks first — the forfeit may still move the player (Lucifer's gate).
            // Walking out on a cheat earns scorn, and a grudge: the demon's cheats come sooner for a while.
            string[] lines = hand.FledACheat ? (_dealerText.Hunted ?? _dealerText.Fled) : _dealerText.Fled;
            _view.Dealer.Say(UiText.Pick(lines, _game.RoundNumber), DealerMood.Gloating);
            Refresh();

            string message = soul
                ? hand.IsSealed ? UiText.FledSealedSoul : UiText.FledSoul
                : string.Format(hand.IsSealed ? UiText.FledSealedFormat : UiText.FledFormat, round.YearsChange);
            if (hand.FledACheat)
                message += "\n" + UiText.GrudgeMessage;
            _view.SetMessage(message, Tone.Bad);
        }

        /// <summary>Freed at Lucifer's table.</summary>
        private bool BeatLucifer => _game.Phase == GamePhase.Absolved && _dealer.IsFinalTable;

        /// <summary>Freed by Wild Bill's hand at an ordinary table, never having been summoned. (A player who has met Lucifer
        /// and fallen, freed the same way, gets the plain absolution — he will remember it.)</summary>
        private bool WildBill => _game.Phase == GamePhase.Absolved && !_dealer.IsFinalTable && _finalDealer != null && _gate.Attempts == 0;

        /// <summary>
        /// An ordinary table has brought the sentence down to its last year, which it may not take: that year is Lucifer's,
        /// and the next hand summons the player.
        /// </summary>
        private bool IsHoldingTheLastYear => _finalDealer != null && _game.Rules.KeepsTheLastYear && _game.Years == 1 &&
                                            _game.Phase == GamePhase.RoundOver;

        private void EndRunRecords()
        {
            // A run that ends at Lucifer's table is credited to the demon it came from.
            string credited = _dealer.IsFinalTable && _origin != null ? _origin.Id : _dealer.Id;
            _records.RunEnded(_game.Phase == GamePhase.Absolved, credited, _stats.HandsPlayed, _gate.Attempts, BeatLucifer, WildBill);
            _archive?.SaveRecords(_records);
            _archive?.ClearRun();
        }

        private void BeginRun()
        {
            _stats = new RunStats(_game.Years, _dealer.Id);
            _settledRound = _game.RoundNumber;
            _records.RunStarted();
            _archive?.SaveRecords(_records);
            SaveRun();
        }

        /// <summary>
        /// Saves the run between hands — and while a hand is played, with that hand (stake, draw, soul, seal), from the deal
        /// on, so closing the game mid-hand cannot undo it. A settled hand is saved by <see cref="SettleHand"/>.
        /// </summary>
        private void SaveRun()
        {
            if (_archive == null || _stats == null || _game.IsGameOver) return;
            HandInProgress hand = _game.CurrentHand;
            if (_game.Phase != GamePhase.Betting && hand == null) return;
            _archive.SaveRun(Snapshot(hand));
        }

        private RunSnapshot Snapshot(HandInProgress hand = null)
        {
            return new RunSnapshot(_dealer.Id, _game.Years, _game.RoundNumber, _stats, hand,
                _gate.IsAtLucifer, _gate.OriginDealerId, _gate.Attempts, _game.Malice, _game.MajorCheatUsed, _game.Grudge);
        }

        private LuciferGate NewGate() => _finalDealer == null ? new LuciferGate(0, 0) : new LuciferGate(_game.Rules);

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

            // A demon's cheat that helped the player goes into the run's story and the records.
            int backfires = _game.CheatsThisHand.Count(r => r.Backfired);
            if (backfires > 0)
            {
                _records.NoteBackfires(backfires);
                _archive?.SaveRecords(_records);
            }

            if (_game.IsGameOver)
            {
                EndRunRecords();
            }
            else if (_archive != null)
            {
                // Saved as it will be at the next deal, so a quit between hands loses nothing and gains nothing.
                _archive.SaveRun(Snapshot());
            }
        }

        private RunSummary Summary()
        {
            return new RunSummary(_game.Phase == GamePhase.Absolved, _stats.HandsPlayed, _stats.LowestYears, _stats.HighestYears,
                _stats.BestHand, _stats.Dealers.Select(id => UiText.Dealer(id).Name).ToArray(), _stats.SoulStaked,
                BeatLucifer, WildBill, _gate.Attempts);
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
            if (!Playing || Hurry() || _game.IsGameOver) return;

            // A finished hand counts as "between hands": move on to the next one first.
            if (_game.Phase == GamePhase.RoundOver)
            {
                _game.NextRound();
                Refresh();
            }

            if (_game.CanLeaveTable(out string reason))
                LeaveRequested?.Invoke();
            else if ((_game.IsSoulAtStake || _game.Rules.IsFinalTable) && _game.Phase == GamePhase.Betting)
                _view.Dealer.Say(UiText.Pick(_dealerText.SoulLocked, _game.RoundNumber), DealerMood.Menacing);
            else
                _view.SetMessage(reason, Tone.Warning);
        }

        // ------------------------------------------------------------------ Lucifer's gate

        /// <summary>
        /// Between hands the gate may move the player: down to the gate, Lucifer summons them (wherever they sit); back above
        /// it at his table, he casts them down to the demon they came from. True when the table changed.
        /// </summary>
        private bool PassThroughGate()
        {
            if (_finalDealer == null || _game.IsGameOver || _stats == null) return false;

            switch (_gate.Check(_game.Years, _game.Phase))
            {
                case GateCall.Summoned:
                    BeSummoned();
                    return true;
                case GateCall.CastDown:
                    BeCastDown();
                    return true;
                default:
                    return false;
            }
        }

        private void BeSummoned()
        {
            // The old demon's last word, then the hall goes dark and the eyes open.
            _view.Dealer.Say(UiText.Pick(_dealerText.Farewell, _gate.Attempts), DealerMood.Menacing);
            _origin = _dealer;
            _gate.Summon(_dealer.Id);
            SeatAt(_finalDealer, _game.Years, _game.RoundNumber, SeatChange.Summoned);
            _settledRound = _game.RoundNumber;
            _stats.SatWith(_finalDealer.Id);

            if (_gate.Attempts > 1 || !Tip(UiText.TipLucifer))
                _view.Dealer.Say(LuciferGreeting(), DealerMood.Menacing);
        }

        /// <summary>He remembers: the first summons is a welcome, the later ones are not.</summary>
        private string LuciferGreeting()
        {
            if (_gate.Attempts <= 1) return UiText.Pick(_dealerText.Greeting, 0);
            string[] lines = _dealerText.Remembers;
            return lines == null || lines.Length == 0 ? UiText.Pick(_dealerText.Greeting, 0) : lines[Math.Min(_gate.Attempts - 2, lines.Length - 1)];
        }

        private void BeCastDown()
        {
            if (_origin == null) throw new InvalidOperationException("Cast down — but nobody knows where the player came from.");

            _view.Dealer.Say(UiText.Pick(_dealerText.CastDown, _gate.Attempts), DealerMood.Gloating);
            int years = _gate.CastDown(_game.Years);
            Dealer origin = _origin;
            SeatAt(origin, years, _game.RoundNumber, SeatChange.CastDown);
            _settledRound = _game.RoundNumber;
            _stats.SatWith(origin.Id);
            _stats.Note(_game.Years, _game.IsSoulAtStake);
            _view.Dealer.Say(UiText.Pick(_dealerText.Returned, _gate.Attempts), DealerMood.Gloating);
        }

        /// <summary>Builds the dealer's game, carries the sentence over if moving from another table, and dresses the table.</summary>
        private void SeatAt(Dealer dealer, int? carriedYears, int roundsPlayed, SeatChange change = SeatChange.Instant)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));

            IHellPokerGame game = _createGame(dealer) ?? throw new InvalidOperationException("The game factory returned no game.");
            if (carriedYears.HasValue)
            {
                game.TakeOver(carriedYears.Value, roundsPlayed);
                // The demons' malice belongs to the run, not to one table: moving on never empties the gauge (or the grudge).
                if (_game != null && !_abandoned && game.Phase == GamePhase.Betting)
                    game.RestoreMalice(_game.Malice, majorCheatUsed: false, _game.Grudge);
            }

            _game = game;
            _abandoned = false;
            _dealer = dealer;
            _discards.Clear();
            _finalStretchAnnounced = false;
            _soulShown = false;
            _dealerText = UiText.Dealer(dealer.Id);

            _view.Dealer.SetDealer(DealerCards.Describe(dealer), change);
            _view.Payouts.SetTable(dealer.Payouts);
            _view.SetSoul(SoulGauge.Hidden);
            if (!_game.IsSoulAtStake)
            {
                ShowSentenceLine();
                _view.Sentence.SetYears(_game.Years, animate: false);
            }
        }

        /// <summary>
        /// What the counter measures against: the demon's soul line — or at Lucifer's table the gate (climb above it and you
        /// are cast down), with the attempt in place of "YEARS LEFT IN HELL".
        /// </summary>
        private void ShowSentenceLine()
        {
            if (_game.Rules.IsFinalTable)
            {
                int gate = _game.Rules.LuciferGateYears;
                _view.Sentence.SetLimit(gate, string.Format(UiText.CastDownLineFormat, gate));
                _view.Sentence.SetLabel(string.Format(UiText.AttemptLabelFormat, Math.Max(1, _gate.Attempts)));
                return;
            }

            _view.Sentence.SetSoulLine(_game.Rules.SoulThreshold);
            _view.Sentence.SetLabel(UiText.YearsLabel);
        }

        public void Dispose()
        {
            _view.ActionPressed -= PerformAction;
            _view.BetPressed -= Bet;
            _view.CheckToDrawPressed -= CheckToDraw;
            _view.LeavePressed -= RequestLeave;
            _view.HandRanksPressed -= ToggleHandRanks;
            _view.Player.CardClicked -= ToggleDiscard;
        }

        public void PerformAction()
        {
            if (!Playing || Hurry()) return;

            TableState before = CaptureState();
            switch (_game.Phase)
            {
                case GamePhase.Betting:
                    _game.PlaceBet();
                    _discards.Clear();
                    PlayOutSealedHand(before);
                    // A cheat at the deal lands once the cards are on the table.
                    Refresh();
                    PlayCheatStrikes(before);
                    TipFirstCheat();
                    ShowMalice();
                    return;
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
                    PlayOutSealedHand(before);
                    PlayCheatStrikes(before);
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

                    // A fresh run starts where the player last chose to sit, never at Lucifer's table.
                    if (_dealer.IsFinalTable && _origin != null)
                        SeatAt(_origin, carriedYears: null, roundsPlayed: 0);
                    else
                        _game.Restart();
                    _gate = NewGate();
                    _origin = null;
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
            if (!Playing || Hurry()) return;

            if (!_game.CanBet(action, out _))
            {
                string why = LockedReason(action);
                if (why != null)
                    _view.SetMessage(why, Tone.Warning);
                return;
            }

            TableState before = CaptureState();
            _game.Bet(action);
            if (_game.Phase == GamePhase.HouseReRaise && !Tip(UiText.TipFirstReRaise))
                _view.Dealer.Say(UiText.Pick(_dealerText.ReRaise, _game.RoundNumber), DealerMood.Scheming);
            PlayOutSealedHand(before);
            PlayCheatStrikes(before);
            Refresh();
        }

        public void CheckToDraw()
        {
            if (!Playing || Hurry()) return;

            if (!_game.CanCheckToDraw(out _))
            {
                // Only worth explaining where the button shows: before the draw, under the final stretch.
                if (_game.Phase == GamePhase.PlayerReveal && _game.IsRaiseForced)
                    _view.SetMessage(string.Format(UiText.LockedCheckToDrawFormat, _game.Rules.ForcedRaiseYears), Tone.Warning);
                return;
            }

            TableState before = CaptureState();
            _game.CheckToDraw();
            PlayCheatStrikes(before);
            Refresh();
        }

        /// <summary>What the table showed before a command, to tell what the game did on its own after it.</summary>
        private readonly struct TableState
        {
            public readonly int PlayerCards;
            public readonly int HouseCards;
            public readonly bool Sealed;
            public readonly int Skipped;

            /// <summary>The demon's cheat results already shown this hand.</summary>
            public readonly int Cheats;

            public TableState(int playerCards, int houseCards, bool @sealed, int skipped, int cheats)
            {
                PlayerCards = playerCards;
                HouseCards = houseCards;
                Sealed = @sealed;
                Skipped = skipped;
                Cheats = cheats;
            }
        }

        private TableState CaptureState()
        {
            bool inHand = _game.Phase != GamePhase.Betting;
            return new TableState(inHand ? _game.PlayerCardsRevealed : 0, inHand ? _game.HouseCardsRevealed : 0,
                inHand && _game.IsCommitted, inHand ? _game.DecisionsSkipped : 0, inHand ? _game.CheatsThisHand.Count : 0);
        }

        // ------------------------------------------------------------------ the demon's cheats

        private static CheatCard CardOf(string cheatId) =>
            new CheatCard(cheatId, UiText.CheatName(cheatId), UiText.CheatDescription(cheatId));

        /// <summary>
        /// Every cheat that struck during the last command, in plain sight: the sign goes up (a cheat at the deal strikes
        /// before it could be seen otherwise), a lie comes out ("LIAR"), the card as it was shows for a moment, the blow lands
        /// on the cards, and the demon says their line (with the sly re-raise look). A cheat that helped the player turns
        /// the new card at once, "BACKFIRE" blinks over it and the demon is angry. Otherwise what changed shows with the next
        /// table update (a card turning, a mark appearing). A blocked or fizzled cheat just takes its sign down.
        /// </summary>
        private void PlayCheatStrikes(TableState before)
        {
            IReadOnlyList<CheatResult> results = _game.CheatsThisHand;
            for (int k = before.Cheats; k < results.Count; k++)
            {
                CheatResult result = results[k];
                if (result.Outcome != CheatOutcome.Played) continue;

                _view.SetIntent(CardOf(result.ShownId));
                if (result.WasLie)
                {
                    _view.RevealLie(CardOf(result.CheatId));
                    _view.Dealer.Say(UiText.LiarLine, DealerMood.Gloating);
                }

                int faceUp = _game.Phase == GamePhase.RoundOver || _game.IsGameOver ? Hand.Size : _game.PlayerCardsRevealed;
                int at = result.PlayerCards.Count > 0 ? result.PlayerCards[0] : -1;
                if (result.Lost.HasValue && at >= 0 && _game.PlayerHand != null)
                    _view.Player.Show(PlayerSlots(faceUp, at, result.Lost.Value, keepHidden: true));

                _view.PlayCheat(new CheatImpact(result.CheatId, result.PlayerCards, result.HouseCards));
                _view.Dealer.Say(UiText.CheatLine(result.CheatId), DealerMood.Scheming);

                if (result.Backfired && _game.PlayerHand != null)
                {
                    _view.Player.Show(PlayerSlots(faceUp, keepHidden: true));
                    _view.PlayMoment(TableMoment.Backfire, UiText.BackfireFlash, result.PlayerCards);
                    _view.Dealer.Say(UiText.Pick(_dealerText.Backfire, _game.RoundNumber), DealerMood.Annoyed);
                }
            }
        }

        /// <summary>The gauge under the portrait and the announced cheat over it, as they stand.</summary>
        private void ShowMalice()
        {
            _view.SetMalice(new MaliceGauge(_dealer.Id, _game.Malice, _game.MaliceMax));
            _view.SetIntent(_game.PendingCheat == null ? null : CardOf(_game.PendingCheat.Id));
        }

        /// <summary>The first time a demon shows a cheat, they explain the gauge and the sign, once.</summary>
        private void TipFirstCheat()
        {
            if (_game.PendingCheat != null || _game.CheatsThisHand.Count > 0)
                Tip(UiText.TipCheatFor(_dealer.Id));
        }

        /// <summary>The result line, with the hand's cheat told on a second line ("Mammon took your K♠ for a 3♦.").</summary>
        private string WithCheatLog(string message, bool soul)
        {
            CheatResult played = _game.CheatsThisHand.FirstOrDefault(r => r.Outcome == CheatOutcome.Played);
            string log = UiText.CheatLog(_dealerText.Name, played, soul, _game.ThornYearsThisHand, _game.TitheYearsThisHand);
            return log == null ? message : message + "\n" + log;
        }

        /// <summary>The H panel's line about the announced cheat; null when nothing is coming.</summary>
        private string IntentFootnote()
        {
            if (_game?.PendingCheat == null) return null;
            string id = _game.PendingCheat.Id;
            return UiText.CheatName(id) + ": " + UiText.CheatDescription(id);
        }

        /// <summary>
        /// The pact was sealed (the table is full): the moment is marked once, and the decisions the game passed by itself
        /// are played out slowly — the bet buttons gone, each remaining card turning on its own with a beat in between.
        /// Any key or click hurries it along (the queue is skipped like any other animation).
        /// </summary>
        private void PlayOutSealedHand(TableState before)
        {
            if (!_game.IsCommitted) return;

            bool skipped = _game.DecisionsSkipped > before.Skipped;
            bool justSealed = !before.Sealed;
            bool handGoesOn = _game.Phase == GamePhase.Drawing;
            if (!skipped)
            {
                // Sealed by the last raise before the draw: nothing to play out yet, but the moment is marked.
                if (justSealed && handGoesOn)
                    AnnounceSeal();
                return;
            }

            _view.SetBetControls(BetControls.Hidden);
            _view.SetAction(null);
            _view.SetAnte(0);

            if (!_game.IsAfterDraw)
            {
                // Before the draw: the player's remaining cards, one at a time (a sealed deal starts from the opening cards).
                int from = Math.Max(before.PlayerCards, Math.Min(Hand.Size, _game.Rules.OpeningCardsShown));
                _view.House.Show(HouseSlots(0));
                _view.Player.Show(PlayerSlots(from));
                ShowStakeOnTable();
                if (justSealed)
                    AnnounceSeal();
                for (int shown = from + 1; shown <= _game.PlayerCardsRevealed; shown++)
                {
                    _view.Pause(SealedRevealPause);
                    _view.Player.Show(PlayerSlots(shown));
                }
                return;
            }

            // After the draw: the player's new cards land, then the House's cards turn one by one into the showdown.
            if (justSealed)
                AnnounceSeal();
            if (!SoulMode && _game.LastRound != null)
            {
                // A called re-raise is on the table now. The hand is already settled in the game, so the counter shows
                // the sentence as it stood before it, minus the table, until the result comes in.
                _view.SetPot(_game.LastRound.Stake);
                _view.Sentence.SetYears(_game.LastRound.YearsBefore - _game.LastRound.Stake, animate: true);
                _view.SetStakeInfo(null);
            }
            // Cards a cheat hid stay dark while the House turns its cards: only the result shows them.
            _view.Player.Show(PlayerSlots(Hand.Size, keepHidden: true));
            _view.House.Show(HouseSlots(before.HouseCards));
            ShowPlayerCaption();
            for (int shown = before.HouseCards + 1; shown <= Hand.Size; shown++)
            {
                _view.Pause(SealedRevealPause);
                _view.House.Show(HouseSlots(shown));
            }
        }

        private void AnnounceSeal()
        {
            _view.SetBetControls(BetControls.Hidden);
            _view.PlayMoment(TableMoment.PactSealed, UiText.PactSealed);
            _view.SetMessage(UiText.SealedMessage, Tone.Warning);
            _view.Dealer.Say(UiText.Pick(_dealerText.Sealed, _game.RoundNumber), DealerMood.Gloating);
        }

        public void ToggleDiscard(int index)
        {
            if (!Playing || Hurry() || _game.Phase != GamePhase.Drawing) return;

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
                _view.ShowHandRanks(_dealer.Payouts, IntentFootnote());
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
            string text = UiText.TipText(tip, _game.Rules.ForcedRaiseYears, _game.Rules.LuciferGateYears);
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
            PassThroughGate();

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
            SaveRun();
            ShowMalice();
            _view.SetLeave(_game.Phase != GamePhase.Betting ? LeaveState.Hidden
                : _game.Rules.IsFinalTable ? LeaveState.Summoned
                : _game.IsSoulAtStake ? LeaveState.Locked : LeaveState.Open);
            bool finalTable = _game.Rules.IsFinalTable;
            // While Lucifer waits below, an ordinary table never plays its final stretch: at the gate the next hand is his.
            bool luciferWaits = _finalDealer != null && _game.Rules.LuciferGateYears > 0;
            bool finalStretch = finalTable ? IsLastMoments : _game.IsRaiseForced && !luciferWaits;
            _view.SetFinalStretch(finalStretch, finalTable
                ? UiText.LastMomentsBanner
                : string.Format(UiText.FinalStretchBannerFormat, _game.Rules.ForcedRaiseYears));
            AnnounceSoul();

            // The dealer remarks once when the player first reaches the gates (unless the run just ended).
            if (finalStretch && !_finalStretchAnnounced && !_game.IsGameOver)
            {
                _finalStretchAnnounced = true;
                if (finalTable || !Tip(UiText.TipFinalStretch))
                    _view.Dealer.Say(UiText.Pick(_dealerText.FinalStretch, _game.RoundNumber), DealerMood.Menacing);
            }
        }

        /// <summary>
        /// The last moments at Lucifer's table: the whole sentence would fit on his table, so one hand could end it.
        /// His hall runs hotter and fire drips from his eyes.
        /// </summary>
        private bool IsLastMoments => !_game.IsGameOver && _game.Years > 0 && _game.Rules.Stakes.CapFor(_game.Years) >= _game.Years;

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
            bool beforeDraw = _game.Phase == GamePhase.PlayerReveal;
            _view.SetMessage(prompt + (mustRaise ? UiText.PromptForcedChoice : UiText.PromptChoice), mustRaise ? Tone.Warning : Tone.Neutral);
            _view.SetBetControls(new BetControls(true, RaiseLabel(), _game.CanBet(BetAction.Raise, out _), !mustRaise,
                showCheckToDraw: beforeDraw, canCheckToDraw: beforeDraw && _game.CanCheckToDraw(out _)));
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
            _view.House.Show(HouseSlots(_game.HouseCardsRevealed));
            _view.Player.Show(PlayerSlots(_game.PlayerCardsRevealed));
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
            _view.Player.Show(PlayerSlots(Hand.Size));
            _view.Player.SetInteractable(true);
            _view.Player.SetSelection(_discards);
            ShowPlayerCaption();
            // No keep frames on a hand with a card in the dark: they could only be guesses — or give it away.
            bool anyHidden = Enumerable.Range(0, Hand.Size).Any(_game.IsPlayerCardHidden);
            if (GuideOn && !anyHidden)
            {
                IReadOnlyCollection<int> toss = _game.SuggestedDiscards();
                _view.Player.SetHints(Enumerable.Range(0, Hand.Size).Where(i => !toss.Contains(i)).ToArray());
            }
            else
            {
                _view.Player.SetHints(null);
            }
            ShowStakeOnTable();
            if (_game.IsCommitted)
                _view.SetMessage(string.Format(UiText.SealedDrawPrompt, _game.Rules.MaxDiscards), Tone.Warning);
            else
                _view.SetMessage(string.Format(UiText.PromptDrawFormat, _game.Rules.MaxDiscards), Tone.Neutral);
            // A thorned card picked to go: the price is said before the draw, on the button too.
            int thorn = _game.ThornCost(_discards);
            if (thorn > 0)
                _view.SetMessage(SoulMode ? UiText.ThornWarningSoul : string.Format(UiText.ThornWarningFormat, thorn), Tone.Warning);
            string draw = _discards.Count == 0 ? UiText.Stand : string.Format(UiText.DrawFormat, _discards.Count);
            if (thorn > 0)
                draw = SoulMode ? string.Format(UiText.DrawThornSoulFormat, _discards.Count) : string.Format(UiText.DrawThornFormat, _discards.Count, thorn);
            _view.SetAction(draw);
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
            // The House turns first: a card a cheat kept from the player turns only once both hands are on the table.
            _view.House.Show(HouseSlots(Hand.Size));
            _view.Player.Show(PlayerSlots(Hand.Size));
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
                // Damned at the draw: the thorn took the last of the soul, nobody folded.
                _view.Player.SetCaption(round.ThornDamned ? UiText.ThornedCaption : UiText.FoldedCaption, Tone.Bad);
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
                    _view.SetMessage(deadMansHand ? UiText.AbsolvedMessage : BeatLucifer ? UiText.MorningStarFallsMessage : UiText.ServedMessage,
                        Tone.Triumph);
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
                    _view.SetMessage(WithCheatLog(ResultMessage(round, soulHand), soulHand),
                        playerWon ? Tone.Good : houseWon || round.Folded ? Tone.Bad : Tone.Neutral);
                    if (IsHoldingTheLastYear)
                    {
                        // Not a bug: an ordinary table keeps the last year for Lucifer — and says so.
                        _view.Dealer.Say(UiText.LastYearLine, DealerMood.Menacing);
                        _view.Sentence.SetLimit(_game.Rules.SoulThreshold, UiText.LastYearNote);
                    }
                    else
                    {
                        SayRoundLine(round);
                    }
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

        /// <summary>
        /// The player's cards as the player may see them: a card a cheat hid stays face down — under its veil once its turn
        /// has come — a chained or thorned card carries its mark. Hidden cards never show their face before the showdown.
        /// </summary>
        /// <param name="lostAt">Show <paramref name="lost"/> at this position instead (the moment before a cheat changes it).</param>
        /// <param name="keepHidden">The hand is settled but the showdown is not shown yet (a sealed hand playing out, a cheat at
        /// the showdown): a hidden card stays face down until the result shows both hands.</param>
        private CardSlot[] PlayerSlots(int faceUp, int lostAt = -1, Core.Cards.Card lost = default, bool keepHidden = false)
        {
            CardSlot[] slots = Slots(_game.PlayerHand, faceUp);
            if (_game.PlayerHand == null) return slots;
            for (int i = 0; i < Hand.Size; i++)
            {
                if (i == lostAt && i < faceUp)
                {
                    slots[i] = CardSlot.Face(lost);
                    continue;
                }
                if (_game.IsPlayerCardHidden(i) || (keepHidden && _game.WasPlayerCardHidden(i)))
                    slots[i] = i < faceUp ? CardSlot.Back.WithMark(CardMark.Veiled) : CardSlot.Back;
                else if (_game.IsPlayerCardChained(i) && i < faceUp)
                    slots[i] = slots[i].WithMark(CardMark.Chained);
                else if (_game.IsPlayerCardThorned(i) && i < faceUp)
                    slots[i] = slots[i].WithMark(CardMark.Thorned);
            }
            return slots;
        }

        /// <summary>The House's cards as the player sees them: a false face (with its faint sheen) until the showdown.</summary>
        private CardSlot[] HouseSlots(int faceUp)
        {
            CardSlot[] slots = Slots(_game.HouseHand, faceUp);
            if (_game.HouseHand == null) return slots;
            for (int i = 0; i < faceUp && i < Hand.Size; i++)
            {
                if (_game.IsHouseCardFalse(i))
                    slots[i] = CardSlot.Face(_game.HouseCardFace(i)).WithMark(CardMark.FalseFace);
            }
            return slots;
        }
    }
}
