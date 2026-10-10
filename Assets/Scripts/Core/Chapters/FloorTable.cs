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

    /// <summary>
    /// A match on a floor, for coins: a table's imp or a warden, each with a purse of its own (<see cref="ChapterRules.ImpCoins"/>,
    /// <see cref="ChapterRules.WardenCoins"/>). The match goes on until one purse is empty: the imp's — the match is won — or the
    /// player's — the run is over. No leaving, no hand limit; the ante grows every few hands (<see cref="ChapterRules.AnteAt"/>) so a
    /// match does not drag. The hands are the game's own (<see cref="HellPokerGame"/>, played as at any table: the run's sinner, charge,
    /// jokers and relics included) under the chapter demon's table rules (discards, cards shown), with the hand's ante, at most
    /// <see cref="ChapterRules.CapAntes"/> antes, never more than either purse holds (less is all in), and the floor's payouts
    /// (<see cref="FloorPayoutTable"/>). A hand never takes more than the losing purse holds.
    /// The game's "sentence" is a purse far above every line (<see cref="Purse"/>): the coins are what comes off it or goes on it,
    /// so the soul, the gate and the final stretch never come near. An imp re-raises seldom, never bluffs and may give up a weak hand
    /// facing a raise after the draw; it never cheats. A warden plays with the demon's temper and the demon's minor cheats.
    /// The sentence itself is never touched here.
    /// </summary>
    public sealed class FloorTable
    {
        /// <summary>The floor game's purse: the coins are counted off it.</summary>
        public const int Purse = 1000000;

        /// <summary>The dice stream of the imp's folding (the deck, the House's temper and the cheats keep theirs).</summary>
        public const int FoldStream = 4;

        /// <summary>The run's per-table charges (the Bone Die) come full at every floor match.</summary>
        public const string SeatId = "floor";

        private readonly CoinPurse _purse;
        private readonly Func<int, int> _anteAt;
        private readonly int _capAntes;
        private readonly int? _handsLimit;
        private readonly int? _lossCap;
        private readonly int _tollPercent;
        private readonly int _tollMax;
        private bool _freeAnte;
        private bool _settled = true;

        public HellPokerGame Game { get; }
        public bool IsWarden { get; }

        /// <summary>The imp's (or the warden's) coins; null for the gambler's ghost, who pays what he must.</summary>
        public CoinPurse HousePurse { get; }

        public int HandsPlayed { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Pushes { get; private set; }
        public int Folds { get; private set; }
        public int HouseFolds { get; private set; }
        public int Coins { get; private set; }
        public int TollTaken { get; private set; }

        /// <summary>The player's purse is empty: the run is over.</summary>
        public bool PlayerBroke => _settled && _purse.IsEmpty;

        /// <summary>The imp's purse is empty: the match is won.</summary>
        public bool HouseBroke => _settled && HousePurse != null && HousePurse.IsEmpty;

        /// <summary>The last hand settled and a purse empty (the gambler's single hand: once it is settled).</summary>
        public bool IsOver => _settled && (PlayerBroke || HouseBroke || (_handsLimit.HasValue && HandsPlayed >= _handsLimit.Value));

        public bool MatchWon => IsOver && HouseBroke && !PlayerBroke;

        /// <summary>The ante of the hand being played, or of the next one between hands (before what either purse holds).</summary>
        public int AnteNow => _anteAt(_settled ? HandsPlayed + 1 : HandsPlayed);

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
            IRandomSource houseRandom, IRandomSource deckRandom, IRandomSource cheatRandom, Sinner sinner, RunEffects effects, CoinPurse purse,
            CoinPurse housePurse, Func<int, int> anteAt, int capAntes, int? handsLimit, int? lossCap, bool warden, int tollPercent, int tollMax,
            IReadOnlyList<Card> deck, bool freeFirstAnte, int openHouseCards)
        {
            _purse = purse ?? throw new ArgumentNullException(nameof(purse));
            _anteAt = anteAt ?? throw new ArgumentNullException(nameof(anteAt));
            if (purse.IsEmpty) throw new InvalidOperationException("An empty purse does not sit down.");
            HousePurse = housePurse;
            _capAntes = capAntes;
            _handsLimit = handsLimit;
            _lossCap = lossCap;
            IsWarden = warden;
            _tollPercent = tollPercent;
            _tollMax = tollMax;
            _freeAnte = freeFirstAnte;

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
            Game.HouseCardsOpenAtDeal = openHouseCards;
            SetStakes();
        }

        /// <summary>The coming hand's stakes: its ante and cap, and never more on the table than either purse holds.</summary>
        private void SetStakes()
        {
            int ante = AnteNow;
            Game.StakesOverride = StakeScale.Fixed(ante, ante * _capAntes);
            Game.StakeLimit = Math.Max(1, Math.Min(_purse.Coins, HousePurse?.Coins ?? int.MaxValue));
        }

        /// <summary>The floor's rules: the chapter demon's discards and open cards, fixed stakes, no soul, no gate, no final stretch.</summary>
        private static GameRules FloorRules(Dealer boss, int ante, int cap) =>
            new GameRules(startingYears: Purse, soulThreshold: 2 * Purse, maxDiscards: boss.MaxDiscards, forcedRaiseYears: 0,
                houseCardsShown: boss.HouseCardsShown, stakes: StakeScale.Fixed(ante, cap), luciferGateYears: 0, maliceLowSentenceBonus: 0,
                majorCheatYears: 0, eventChancePercent: 0);

        /// <summary>A table's imp with <see cref="ChapterRules.ImpCoins"/>: the chapter demon's table rules, a weak temper, no cheats.
        /// <paramref name="impsEye"/>: one of the imp's cards plays face up from the deal (bought at the black market).</summary>
        public static FloorTable Imp(ChapterRules rules, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null, bool freeFirstAnte = false, bool impsEye = false)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return Match(rules, rules.AnteAt, rules.CapAntes, new CoinPurse(rules.ImpCoins), null, null, warden: false, sinner, effects,
                purse, seed, deck, freeFirstAnte, impsEye ? 1 : 0);
        }

        /// <summary>A warden with <see cref="ChapterRules.WardenCoins"/>: the chapter demon's temper and minor cheats (a full gauge), and
        /// a toll on every hand the player wins.</summary>
        public static FloorTable Warden(ChapterRules rules, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null, bool freeFirstAnte = false)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return Match(rules, rules.AnteAt, rules.CapAntes, new CoinPurse(rules.WardenCoins), null, null, warden: true, sinner, effects,
                purse, seed, deck, freeFirstAnte, 0);
        }

        /// <summary>One hand against the gambler's ghost for <paramref name="stake"/> coins, nothing to raise. A loss costs the stake and no
        /// more (the ghost's word: half the purse, never all of it); a win pays up to the floor's multiplier.</summary>
        public static FloorTable Gamble(ChapterRules rules, int stake, Sinner sinner, RunEffects effects, CoinPurse purse, int seed,
            IReadOnlyList<Card> deck = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            return Match(rules, _ => stake, 1, null, 1, stake, warden: false, sinner, effects, purse, seed, deck, false, 0);
        }

        private static FloorTable Match(ChapterRules rules, Func<int, int> anteAt, int capAntes, CoinPurse housePurse, int? handsLimit, int? lossCap,
            bool warden, Sinner sinner, RunEffects effects, CoinPurse purse, int seed, IReadOnlyList<Card> deck, bool freeFirstAnte, int openHouseCards)
        {
            int ante = anteAt(1), cap = ante * capAntes;
            Dealer boss = rules.Boss;
            HouseBettingStyle temper = warden ? boss.Betting : new HouseBettingStyle(rules.ImpStrongFrom, rules.ImpReRaisePercent, 0);
            Dealer house = new Dealer(boss.Id, boss.MaxDiscards, boss.HouseCardsShown, boss.Payouts, temper, boss.SoulThreshold,
                backfirePercent: warden ? boss.BackfirePercent : 0);
            ICheatPolicy cheats = warden ? MinorCheatsOf(boss) : null;
            IRandomSource Stream(int stream) => new SystemRandomSource(RandomSeeds.Derive(seed, stream));
            IHouseFoldStrategy folding = warden ? null : new WeakHandFoldStrategy(HandCategory.OnePair, rules.ImpFoldPercent, Stream(FoldStream));
            return new FloorTable(FloorRules(boss, ante, cap), house, new FloorPayoutTable(boss.Payouts, rules.MultiplierCap), cheats,
                cheats == null ? 0 : boss.MaliceMax, folding, Stream(HellPokerGameFactory.HouseStream), Stream(HellPokerGameFactory.DeckStream),
                Stream(HellPokerGameFactory.CheatStream), sinner, effects, purse, housePurse, anteAt, capAntes, handsLimit, lossCap, warden,
                warden ? rules.WardenTollPercent : 0, rules.WardenTollMax, deck, freeFirstAnte, openHouseCards);
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
        }

        /// <summary>
        /// The hand is over: its coins go to the purse (the first hand of a free ante does not lose its ante) — never more than the losing
        /// purse holds —, the warden takes his toll from a won hand, and the match counts it.
        /// </summary>
        public FloorHand Settle()
        {
            if (_settled) throw new InvalidOperationException("No hand to settle.");
            if (Game.Phase != GamePhase.RoundOver) throw new InvalidOperationException("The hand is still being played.");
            RoundResult round = Game.LastRound;
            int coins = -round.YearsChange;
            if (_freeAnte)
            {
                if (coins < 0) coins += Math.Min(Game.Ante, -coins);   // the ante was never the player's
                _freeAnte = false;
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
            HousePurse?.Add(-coins);
            Coins += coins;
            int toll = won && _tollPercent > 0 && _purse.Coins > 0 ? Math.Min(_tollMax, _purse.Coins * _tollPercent / 100) : 0;
            _purse.Add(-toll);
            TollTaken += toll;
            _settled = true;
            if (!IsOver) SetStakes();
            return new FloorHand(coins, toll, won, lost, round.Folded, round.HouseFolded);
        }
    }
}
