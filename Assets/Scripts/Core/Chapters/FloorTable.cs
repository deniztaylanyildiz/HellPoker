using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>What one floor hand did to the purse.</summary>
    public sealed class FloorHand
    {
        /// <summary>The coins the hand won (positive) or cost (negative), the warden's toll not included.</summary>
        public int Coins { get; }

        /// <summary>The warden's share of the purse taken after a won hand (0 for none).</summary>
        public int Toll { get; }

        public bool Won { get; }
        public bool Lost { get; }
        public bool Folded { get; }
        public bool HouseFolded { get; }

        public FloorHand(int coins, int toll, bool won, bool lost, bool folded, bool houseFolded)
        {
            Coins = coins;
            Toll = toll;
            Won = won;
            Lost = lost;
            Folded = folded;
            HouseFolded = houseFolded;
        }
    }

    /// <summary>What the next match is played under (the black market's eye, a stranger's offer). <see cref="None"/>: nothing.</summary>
    public sealed class TableMarks
    {
        /// <summary>One of the house's cards plays face up from the deal (the imp's eye, the lying witness).</summary>
        public bool OpenCard { get; }

        /// <summary>The open card lies this often (percent; the lying witness's).</summary>
        public int OpenCardLiePercent { get; }

        /// <summary>The house shows no card before the showdown, and a win pays this percent of itself (Belial's Show; 100: none).</summary>
        public bool HidesAll { get; }

        public int WinPercent { get; }

        /// <summary>The first hands without an ante — and without a raise (Lilith's Insomnia).</summary>
        public int FreeHands { get; }

        public TableMarks(bool openCard = false, int openCardLiePercent = 0, bool hidesAll = false, int winPercent = 100, int freeHands = 0)
        {
            if (openCardLiePercent < 0 || openCardLiePercent > 100) throw new ArgumentOutOfRangeException(nameof(openCardLiePercent));
            if (winPercent <= 0 || freeHands < 0) throw new ArgumentOutOfRangeException(nameof(winPercent));
            OpenCard = openCard;
            OpenCardLiePercent = openCardLiePercent;
            HidesAll = hidesAll;
            WinPercent = winPercent;
            FreeHands = freeHands;
        }

        public static TableMarks None { get; } = new TableMarks();

        public bool IsNone => !OpenCard && !HidesAll && FreeHands == 0 && WinPercent == 100;

        /// <summary>Two marks on the same match (an offer on top of a bought eye).</summary>
        public TableMarks With(TableMarks other) =>
            other == null || other.IsNone ? this
            : new TableMarks(OpenCard || other.OpenCard, Math.Max(OpenCardLiePercent, other.OpenCardLiePercent), HidesAll || other.HidesAll,
                WinPercent * other.WinPercent / 100, Math.Max(FreeHands, other.FreeHands));

        /// <summary>"eye,lie,hide,win,free" for the save.</summary>
        public string Encode() => $"{(OpenCard ? 1 : 0)},{OpenCardLiePercent},{(HidesAll ? 1 : 0)},{WinPercent},{FreeHands}";

        public static TableMarks Decode(string text)
        {
            if (string.IsNullOrEmpty(text)) return None;
            string[] parts = text.Split(',');
            if (parts.Length != 5 || !parts.All(p => int.TryParse(p, out _))) return None;
            int[] n = parts.Select(int.Parse).ToArray();
            try { return new TableMarks(n[0] == 1, n[1], n[2] == 1, n[3], n[4]); }
            catch (ArgumentException) { return None; }
        }
    }

    /// <summary>
    /// A match on a floor, for coins: a table's imp or a warden, each with a purse of its own (<see cref="ChapterRules.ImpCoinsAt"/>,
    /// <see cref="ChapterRules.WardenCoins"/>). The match goes on until one purse is empty: the imp's — the match is won — or the
    /// player's — the run is over. No leaving, no hand limit; the ante grows every few hands (<see cref="ChapterRules.AnteAt"/>) so a
    /// match does not drag. An imp's purse never grows past <see cref="ChapterRules.HousePurseCapPercent"/> of its start: the rest goes
    /// to the vault. The hands are the game's own (<see cref="HellPokerGame"/>, played as at any table: the run's sinner, charge,
    /// jokers and relics included) under the chapter demon's table rules (discards, cards shown), with the hand's ante, at most
    /// <see cref="ChapterRules.CapAntes"/> antes, never more than either purse holds (less is all in), and the floor's payouts
    /// (<see cref="FloorPayoutTable"/>). A hand never takes more than the losing purse holds.
    /// The game's "sentence" is a purse far above every line (<see cref="Purse"/>): the coins are what comes off it or goes on it,
    /// so the soul, the gate and the final stretch never come near. An imp re-raises seldom, never bluffs and may give up a weak hand
    /// facing a raise after the draw; it never cheats. A warden plays with the demon's temper and a trick of the chapter's
    /// (<see cref="WardenCheatsOf"/>). The sentence itself is never touched here.
    /// </summary>
    public sealed class FloorTable
    {
        /// <summary>The floor game's purse: the coins are counted off it.</summary>
        public const int Purse = 1000000;

        /// <summary>The dice stream of the imp's folding (the deck, the House's temper and the cheats keep theirs).</summary>
        public const int FoldStream = 4;

        /// <summary>The dice of the table's marks (the lying witness).</summary>
        public const int MarkStream = 5;

        /// <summary>The run's per-table charges (the Bone Die) come full at every floor match.</summary>
        public const string SeatId = "floor";

        private readonly CoinPurse _purse;
        private readonly Func<int, int> _anteAt;
        private readonly int _capAntes;
        private readonly int? _handsLimit;
        private readonly int? _lossCap;
        private readonly int _tollPercent;
        private readonly int _tollMax;
        private readonly int _purseCap;
        private readonly IRandomSource _markRandom;
        private int _freeHands;
        private bool _settled = true;

        public HellPokerGame Game { get; }
        public bool IsWarden { get; }

        /// <summary>The marks this match is played under.</summary>
        public TableMarks Marks { get; }

        /// <summary>The imp's (or the warden's) coins; null for the gambler's ghost, who pays what he must.</summary>
        public CoinPurse HousePurse { get; }

        /// <summary>The coins the imp sat down with.</summary>
        public int HouseStart { get; }

        public int HandsPlayed { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Pushes { get; private set; }
        public int Folds { get; private set; }
        public int HouseFolds { get; private set; }
        public int Coins { get; private set; }
        public int TollTaken { get; private set; }

        /// <summary>Coins over the imp's cap sent to the vault.</summary>
        public int CoinsToVault { get; private set; }

        /// <summary>The open card of the hand being played lies (the lying witness's word); false otherwise.</summary>
        public bool OpenCardLies { get; private set; }

        /// <summary>The player's purse is empty: the run is over.</summary>
        public bool PlayerBroke => _settled && _purse.IsEmpty;

        /// <summary>The imp's purse is empty: the match is won.</summary>
        public bool HouseBroke => _settled && HousePurse != null && HousePurse.IsEmpty;

        /// <summary>The last hand settled and a purse empty (the gambler's single hand: once it is settled).</summary>
        public bool IsOver => _settled && (PlayerBroke || HouseBroke || (_handsLimit.HasValue && HandsPlayed >= _handsLimit.Value));

        public bool MatchWon => IsOver && HouseBroke && !PlayerBroke;

        /// <summary>A hand has been dealt and not settled yet.</summary>
        public bool HandInPlay => !_settled;

        /// <summary>The ante of the hand being played, or of the next one between hands (before what either purse holds).</summary>
        public int AnteNow => _anteAt(_settled ? HandsPlayed + 1 : HandsPlayed);

        /// <summary>Free hands left (no ante, no raise), the one being played counted.</summary>
        public int FreeHandsLeft => _freeHands;

        /// <summary>Hands until the ante grows (counting the one being played, or the next one).</summary>
        public int HandsToNextAnte
        {
            get
            {
                int hand = _settled ? HandsPlayed + 1 : HandsPlayed;
                int next = hand + 1;
                while (_anteAt(next) == _anteAt(hand) && next - hand < 1000) next++;
                return next - hand;
            }
        }

        private FloorTable(GameRules rules, Dealer house, IPayoutTable payouts, ICheatPolicy cheats, int maliceMax, IHouseFoldStrategy folding,
            IRandomSource houseRandom, IRandomSource deckRandom, IRandomSource cheatRandom, IRandomSource markRandom, Sinner sinner,
            RunEffects effects, CoinPurse purse, CoinPurse housePurse, int purseCap, Func<int, int> anteAt, int capAntes, int? handsLimit,
            int? lossCap, bool warden, int tollPercent, int tollMax, IReadOnlyList<Card> deck, TableMarks marks)
        {
            _purse = purse ?? throw new ArgumentNullException(nameof(purse));
            _anteAt = anteAt ?? throw new ArgumentNullException(nameof(anteAt));
            if (purse.IsEmpty) throw new InvalidOperationException("An empty purse does not sit down.");
            HousePurse = housePurse;
            HouseStart = housePurse?.Coins ?? 0;
            _purseCap = purseCap;
            _capAntes = capAntes;
            _handsLimit = handsLimit;
            _lossCap = lossCap;
            IsWarden = warden;
            _tollPercent = tollPercent;
            _tollMax = tollMax;
            _markRandom = markRandom;
            Marks = marks ?? TableMarks.None;
            _freeHands = Marks.FreeHands;

            Game = new HellPokerGame(rules, new Deck(new FisherYatesShuffler(deckRandom)), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), payouts,
                new HandStrengthBettingStrategy(house.Betting, houseRandom),
                new CheatSession(cheats, maliceMax, cheatRandom, sinner, house.BackfirePercent), cheatRandom, sinner, folding);
            if (effects != null)
            {
                effects.SitAt(SeatId, fresh: true);
                Game.UseEffects(effects);
            }
            if (deck != null && deck.Count > 0) Game.RestoreDeck(deck);
            Game.HouseCardsOpenAtDeal = Marks.OpenCard && !Marks.HidesAll ? 1 : 0;
            SetStakes();
        }

        /// <summary>The coming hand's stakes: its ante and cap, and never more on the table than either purse holds.</summary>
        private void SetStakes()
        {
            int ante = AnteNow;
            Game.StakesOverride = StakeScale.Fixed(ante, ante * _capAntes);
            Game.StakeLimit = Math.Max(1, Math.Min(_purse.Coins, HousePurse?.Coins ?? int.MaxValue));
            Game.RaiseForbidden = _freeHands > 0;
        }

        /// <summary>The floor's rules: the chapter demon's discards and open cards (none under Belial's Show), fixed stakes, no soul, no
        /// gate, no final stretch.</summary>
        private static GameRules FloorRules(Dealer boss, int ante, int cap, bool hidesAll) =>
            new GameRules(startingYears: Purse, soulThreshold: 2 * Purse, maxDiscards: boss.MaxDiscards, forcedRaiseYears: 0,
                houseCardsShown: hidesAll ? 0 : boss.HouseCardsShown, stakes: StakeScale.Fixed(ante, cap), luciferGateYears: 0,
                maliceLowSentenceBonus: 0, majorCheatYears: 0, eventChancePercent: 0);

        /// <summary>A table's imp on <paramref name="floor"/> (<see cref="ChapterRules.ImpCoinsAt"/>): the chapter demon's table rules,
        /// a weak temper, no cheats.</summary>
        /// <param name="houseCoins">The imp's purse as a saved match left it (-1: a fresh one).</param>
        public static FloorTable Imp(ChapterRules rules, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null, TableMarks marks = null, int floor = 1, int houseCoins = -1)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            int start = rules.ImpCoinsAt(floor);
            return Match(rules, rules.AnteAt, rules.CapAntes, start, houseCoins, null, null, warden: false, sinner, effects, purse, seed, deck, marks);
        }

        /// <summary>A warden with <see cref="ChapterRules.WardenCoins"/>: the chapter demon's temper and the chapter's trick
        /// (<see cref="WardenCheatsOf"/>), and a toll on every hand the player wins.</summary>
        public static FloorTable Warden(ChapterRules rules, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null, TableMarks marks = null, int houseCoins = -1)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return Match(rules, rules.AnteAt, rules.CapAntes, rules.WardenCoins, houseCoins, null, null, warden: true, sinner, effects, purse,
                seed, deck, marks);
        }

        /// <summary>One hand against the gambler's ghost for <paramref name="stake"/> coins, nothing to raise. A loss costs the stake and no
        /// more (the ghost's word: half the purse, never all of it); a win pays up to the floor's multiplier.</summary>
        public static FloorTable Gamble(ChapterRules rules, int stake, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            return Match(rules, _ => stake, 1, 0, -1, 1, stake, warden: false, sinner, effects, purse, seed, deck, null);
        }

        private static FloorTable Match(ChapterRules rules, Func<int, int> anteAt, int capAntes, int houseStart, int houseCoins, int? handsLimit,
            int? lossCap, bool warden, Sinner sinner, RunEffects effects, CoinPurse purse, int seed, IReadOnlyList<Card> deck, TableMarks marks)
        {
            marks = marks ?? TableMarks.None;
            int ante = anteAt(1), cap = ante * capAntes;
            Dealer boss = rules.Boss;
            HouseBettingStyle temper = warden ? boss.Betting : new HouseBettingStyle(rules.ImpStrongFrom, rules.ImpReRaisePercent, 0);
            Dealer house = new Dealer(boss.Id, boss.MaxDiscards, boss.HouseCardsShown, boss.Payouts, temper, boss.SoulThreshold,
                backfirePercent: warden ? boss.BackfirePercent : 0);
            ICheatPolicy cheats = warden ? WardenCheatsOf(rules) : null;
            IRandomSource Stream(int stream) => new SystemRandomSource(RandomSeeds.Derive(seed, stream));
            IHouseFoldStrategy folding = warden ? null : new WeakHandFoldStrategy(HandCategory.OnePair, rules.ImpFoldPercent, Stream(FoldStream));
            CoinPurse housePurse = houseStart <= 0 ? null : new CoinPurse(houseCoins >= 0 ? houseCoins : houseStart);
            int purseCap = Math.Max(houseStart, houseStart * rules.HousePurseCapPercent / 100);
            return new FloorTable(FloorRules(boss, ante, cap, marks.HidesAll), house,
                new FloorPayoutTable(boss.Payouts, rules.MultiplierCap, marks.WinPercent), cheats, cheats == null ? 0 : WardenMaliceOf(rules),
                folding, Stream(HellPokerGameFactory.HouseStream), Stream(HellPokerGameFactory.DeckStream), Stream(HellPokerGameFactory.CheatStream),
                Stream(MarkStream), sinner, effects, purse, housePurse, purseCap, anteAt, capAntes, handsLimit, lossCap, warden,
                warden ? rules.WardenTollPercent : 0, rules.WardenTollMax, deck, marks);
        }

        /// <summary>The gauge of Belial's and Lilith's wardens: full every hand (their trick is every hand's).</summary>
        public const int WardenMalice = 1;

        /// <summary>A warden's gauge: Mammon's Collector cheats as Mammon's gauge fills; the later wardens play their trick every hand.</summary>
        public static int WardenMaliceOf(ChapterRules rules) => rules.BossId == DealerRoster.MammonId ? rules.Boss.MaliceMax : WardenMalice;

        /// <summary>
        /// The chapter's warden's trick: Mammon's Collector plays the demon's minor cheats; Belial's False Prophet lies with one of his
        /// open cards every hand (False Face); Lilith's Night Nurse hands one of the drawn cards over in the dark (the Night Veil, at the draw).
        /// </summary>
        public static ICheatPolicy WardenCheatsOf(ChapterRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            switch (rules.BossId)
            {
                case DealerRoster.BelialId: return new DemonCheatPolicy(new ICheat[] { new FalseFaceCheat() }, null);
                case DealerRoster.LilithId: return new DemonCheatPolicy(new ICheat[] { new NightNurseCheat() }, null);
                default: return MinorCheatsOf(rules.Boss);
            }
        }

        /// <summary>The demon's minor cheats only (a liar still lies among them).</summary>
        private static ICheatPolicy MinorCheatsOf(Dealer boss)
        {
            if (boss.Cheats == null) return null;
            ICheat[] minor = boss.Cheats.Cheats.Where(c => c.Tier == CheatTier.Minor).ToArray();
            if (minor.Length == 0) return null;
            int lies = boss.Cheats is DemonCheatPolicy policy ? policy.LiePercent : 0;
            return new DemonCheatPolicy(minor, null, lies);
        }

        /// <summary>The cards left in the run's deck (for the next match); empty for a deck not counted.</summary>
        public IReadOnlyList<Card> DeckCards => Game.DeckCards;

        /// <summary>A saved match comes back: the hands it had played (the ante follows them).</summary>
        public void Resume(int handsPlayed)
        {
            if (handsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(handsPlayed));
            if (HandsPlayed > 0 || !_settled) throw new InvalidOperationException("Only a match not yet begun resumes.");
            HandsPlayed = handsPlayed;
            _freeHands = Math.Max(0, Marks.FreeHands - handsPlayed);
            SetStakes();
        }

        /// <summary>Deals the next hand (the last one must be settled first).</summary>
        public void Deal()
        {
            if (!_settled) throw new InvalidOperationException("Settle the last hand first.");
            if (IsOver) throw new InvalidOperationException("The match is over.");
            if (Game.Phase == GamePhase.RoundOver) Game.NextRound();
            SetStakes();
            Game.PlaceBet();
            HandsPlayed++;
            _settled = false;
            // The lying witness: the open card may be a lie (a face out of the deck in its place).
            OpenCardLies = false;
            if (Marks.OpenCard && Marks.OpenCardLiePercent > 0 && Game.HouseCardsRevealed > 0 && _markRandom.Next(100) < Marks.OpenCardLiePercent)
                OpenCardLies = Game.ShowFalseFace(0, _markRandom);
        }

        /// <summary>
        /// A hand left behind when the game closed: it is lost as at any table (a fold, or — sealed — the weakest loss): the player pays
        /// the stake, never more than the purse holds; it goes to the imp's purse.
        /// </summary>
        public int ForfeitHand(int stake)
        {
            if (stake <= 0) return 0;
            int coins = -Math.Min(stake, _purse.Coins);
            if (_lossCap.HasValue) coins = Math.Max(coins, -_lossCap.Value);
            coins = _purse.Add(coins);
            AddToHouse(-coins);
            Coins += coins;
            HandsPlayed++;
            Losses++;
            if (_freeHands > 0) _freeHands--;
            if (!IsOver) SetStakes();
            return coins;
        }

        private void AddToHouse(int coins)
        {
            if (HousePurse == null) return;
            HousePurse.Add(coins);
            int over = HousePurse.Coins - _purseCap;
            if (over <= 0) return;
            HousePurse.Add(-over);   // the imp's cap: the rest goes to the vault
            CoinsToVault += over;
        }

        /// <summary>
        /// The hand is over: its coins go to the purse (a free hand does not lose its ante) — never more than the losing purse holds —,
        /// the warden takes his toll from a won hand, and the match counts it.
        /// </summary>
        public FloorHand Settle()
        {
            if (_settled) throw new InvalidOperationException("No hand to settle.");
            if (Game.Phase != GamePhase.RoundOver) throw new InvalidOperationException("The hand is still being played.");
            RoundResult round = Game.LastRound;
            int coins = -round.YearsChange;
            if (_freeHands > 0)
            {
                if (coins < 0) coins += Math.Min(Game.Ante, -coins);   // the ante was never the player's
                _freeHands--;
            }

            bool won = round.HouseFolded || round.Showdown?.Outcome == ShowdownOutcome.PlayerWins;
            bool lost = !round.Folded && round.Showdown?.Outcome == ShowdownOutcome.HouseWins;
            if (won) Wins++;
            else if (lost) Losses++;
            else if (round.Folded) Folds++;
            else Pushes++;
            if (round.HouseFolded) HouseFolds++;

            if (coins < 0 && _lossCap.HasValue) coins = Math.Max(coins, -_lossCap.Value);
            if (coins > 0 && HousePurse != null) coins = Math.Min(coins, HousePurse.Coins);
            coins = _purse.Add(coins);
            AddToHouse(-coins);
            Coins += coins;
            int toll = won && _tollPercent > 0 && _purse.Coins > 0 ? Math.Min(_tollMax, _purse.Coins * _tollPercent / 100) : 0;
            _purse.Add(-toll);
            TollTaken += toll;
            _settled = true;
            OpenCardLies = false;
            if (!IsOver) SetStakes();
            return new FloorHand(coins, toll, won, lost, round.Folded, round.HouseFolded);
        }
    }
}
