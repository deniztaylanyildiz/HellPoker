using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>The purgatory fire's boons (one of three). The fire is the last floor: every boon is for the demon's table.</summary>
    public enum FireChoice
    {
        /// <summary>The first minor cheat the demon would really strike at their table is broken (<see cref="FirstCheatBreaker"/>).</summary>
        BreakFirstCheat,

        /// <summary>A carried relic's curse silenced until the chapter ends, the demon's table included (its gift stays).</summary>
        SilenceCurse,

        /// <summary>Rest by the fire: the demon's bar starts shorter by <see cref="ChapterRun.RestBarPercent"/> of itself.</summary>
        RestByTheFire
    }

    /// <summary>What the gate took: the tribute paid, the coins missing and the years they cost (with what the floors' offers left owing).</summary>
    public sealed class GateToll
    {
        public int CoinsBefore { get; }
        public int Paid { get; }
        public int Missing { get; }
        public int YearsForMissing { get; }
        public int YearsOwed { get; }
        public int YearsAfter { get; }
        public int CoinsLeft { get; }

        public bool PaidInFull => Missing == 0;

        public GateToll(int coinsBefore, int paid, int missing, int yearsForMissing, int yearsOwed, int yearsAfter, int coinsLeft)
        {
            CoinsBefore = coinsBefore;
            Paid = paid;
            Missing = missing;
            YearsForMissing = yearsForMissing;
            YearsOwed = yearsOwed;
            YearsAfter = yearsAfter;
            CoinsLeft = coinsLeft;
        }
    }

    /// <summary>
    /// One chapter's floors, from the first table to the gate and the demon's table: the map, the purse, where the player stands, and
    /// the run's parts that go along (the sinner — charge, jokers —, the relics, the deck, the share of the sentence at this demon). The
    /// floors are played for coins and never touch the sentence; at the gate the tribute is paid and every coin missing is years on the
    /// demon's bar (<see cref="PayTribute"/>). A driver (the presentation, the simulation) chooses the path and plays each node:
    /// <see cref="OpenTable"/> for a table or a warden, <see cref="TakeTreasure"/>, <see cref="OpenMarket"/>, <see cref="DrawEvent"/>,
    /// <see cref="TendFire"/>; then <see cref="OpenBossTable"/>, and the loot of a beaten demon (<see cref="LootOffers"/>).
    /// </summary>
    public sealed class ChapterRun
    {
        /// <summary>The dice streams of a chapter under its seed: the map, the nodes' own dice (market, events, relics), the matches.</summary>
        public const int MapStream = 10, NodeStream = 11, MatchStream = 12;

        /// <summary>Resting by the fire: the demon's bar starts this much (percent of itself) shorter.</summary>
        public const int RestBarPercent = 10;

        /// <summary>A beaten demon's loot: this many relics to choose from, or these coins.</summary>
        public const int LootRelics = 3, LootCoins = 30;

        private readonly int _seed;
        private readonly IRandomSource _nodeRandom;
        private int _matches;
        private readonly List<MapNode> _trail = new List<MapNode>();
        private readonly HashSet<string> _eventsSeen = new HashSet<string>();

        public ChapterRules Rules { get; }
        public ChapterMap Map { get; }
        public Sinner Sinner { get; }
        public RunEffects Effects { get; }
        public CoinPurse Purse { get; }

        /// <summary>The run's share of the sentence at this chapter's demon (<see cref="BossShares"/>): carried through the floors untouched
        /// (but by an offer), the tribute's years put on it at the gate; at the demon's table it is the demon's bar.</summary>
        public int Years { get; private set; }

        /// <summary>Years the floors' offers left owing: written on the demon's bar at the gate.</summary>
        public int YearsOwed { get; private set; }

        /// <summary>Where the player stands; null before the first floor.</summary>
        public MapNode Current { get; private set; }

        /// <summary>The nodes walked so far, in order.</summary>
        public IReadOnlyList<MapNode> Trail => _trail;

        /// <summary>The run's deck as it goes on (empty: a fresh one — every chapter starts with one).</summary>
        public IReadOnlyList<Card> DeckCards { get; private set; } = Array.Empty<Card>();

        /// <summary>The demon's first minor cheat will be broken (the purgatory fire's); see <see cref="BossGuard"/>.</summary>
        public bool BreaksFirstCheat { get; private set; }

        /// <summary>The player rested by the fire: the demon's bar starts shorter (<see cref="RestBarPercent"/>).</summary>
        public bool RestedByTheFire { get; private set; }

        /// <summary>What the next match is played under (the black market's eye, a stranger's offer).</summary>
        public TableMarks NextTableMarks { get; private set; } = TableMarks.None;

        /// <summary>The next match plays one of the house's cards face up (bought at the black market).</summary>
        public bool ImpsEyeNext => NextTableMarks.OpenCard;

        /// <summary>The imp's eye is bought for the next match; false when one already waits.</summary>
        public bool BuyImpsEye()
        {
            if (ImpsEyeNext) return false;
            AddTableMarks(new TableMarks(openCard: true));
            return true;
        }

        /// <summary>A mark for the next match (an offer's), on top of what waits already.</summary>
        public void AddTableMarks(TableMarks marks) => NextTableMarks = NextTableMarks.With(marks);

        /// <summary>The guard of the demon's table: the sinner, behind the fire's breaker when it was chosen.</summary>
        public ICheatGuard BossGuard() => BreaksFirstCheat ? new FirstCheatBreaker(Sinner) : (ICheatGuard)Sinner;

        /// <summary>A warden's relic waiting for a choice: the run carries all it may (swap one, or take the coins).</summary>
        public string WardenRelicWaiting { get; private set; }

        /// <summary>The gambler's single hand, opened by his offer; null when none is waiting.</summary>
        public FloorTable PendingGamble { get; private set; }

        public bool AtGate => Current != null && Current.Floor == Map.FloorCount - 1;

        /// <summary>The coins the gate takes from this sinner: their class's starting purse and the chapter's
        /// <see cref="ChapterRules.TributeOverStart"/>.</summary>
        public int Tribute => Rules.TributeFor(Sinner.Id);

        /// <summary>What the gate takes from a purse of <paramref name="coins"/>: the tribute — but never the last coin (the next chapter's
        /// first table needs one).</summary>
        public int TributePaid(int coins) => Math.Min(Tribute, Math.Max(0, coins - 1));

        /// <summary>The years a purse of <paramref name="coins"/> owes at the gate: every coin of the tribute it cannot pay.</summary>
        public int TributeYears(int coins) => (Tribute - TributePaid(coins)) * Rules.YearsPerMissingCoin;

        /// <summary>The player's purse went empty at a floor's table: the run is over (it counts as damnation).</summary>
        public bool PurseEmptied { get; private set; }

        /// <summary>The offers the player has seen this chapter (an offer comes once while others are left).</summary>
        public IReadOnlyCollection<string> EventsSeen => _eventsSeen;

        /// <summary>The matches opened so far (each its own dice).</summary>
        public int MatchesOpened => _matches;

        /// <summary>A new run's first chapter: the purse the sinner's class starts with (<see cref="ChapterRules.StartingCoinsOf"/>) and
        /// the class's share of the sentence at the chapter's demon (<see cref="BossShares"/>).</summary>
        public static ChapterRun Begin(ChapterRules rules, Sinner sinner, RunEffects effects, int seed)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return new ChapterRun(rules, sinner, effects, BossShares.For(sinner?.Id, rules.BossId), rules.StartingCoinsOf(sinner?.Id), seed);
        }

        /// <param name="years">The run's share of the sentence at the chapter's demon.</param>
        /// <param name="coins">The purse the chapter starts with: a new run's class purse, or what is left from the last chapter.</param>
        public ChapterRun(ChapterRules rules, Sinner sinner, RunEffects effects, int years, int coins, int seed, IReadOnlyList<Card> deck = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Sinner = sinner ?? throw new ArgumentNullException(nameof(sinner));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            if (years <= 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            Years = years;
            Purse = new CoinPurse(coins);
            _seed = seed;
            _nodeRandom = new SystemRandomSource(RandomSeeds.Derive(seed, NodeStream));
            Map = ChapterMap.Generate(rules, new SystemRandomSource(RandomSeeds.Derive(seed, MapStream)));
            DeckCards = deck ?? Array.Empty<Card>();
        }

        /// <summary>The nodes the player may step to: the first floor's at the start, then the ones the current node leads to.</summary>
        public IEnumerable<MapNode> Choices => Current == null ? Map.Floors[0] : Map.NextFrom(Current);

        public void MoveTo(MapNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (PurseEmptied) throw new InvalidOperationException("The run is over: the purse is empty.");
            if (!Choices.Contains(node)) throw new InvalidOperationException($"{node} cannot be reached from here.");
            if (WardenRelicWaiting != null) throw new InvalidOperationException("The warden's relic waits for a choice.");
            Current = node;
            _trail.Add(node);
        }

        private int NextMatchSeed() => RandomSeeds.Derive(RandomSeeds.Derive(_seed, MatchStream), ++_matches);

        /// <summary>The match at the current node (a table's imp or a warden), under the marks that waited for it.</summary>
        /// <param name="houseCoins">A saved match's imp purse (-1: a fresh match).</param>
        public FloorTable OpenTable(int houseCoins = -1)
        {
            if (Current == null || (Current.Kind != NodeKind.Table && Current.Kind != NodeKind.Warden))
                throw new InvalidOperationException("There is no table here.");
            TableMarks marks = NextTableMarks;
            NextTableMarks = TableMarks.None;
            return Current.Kind == NodeKind.Warden
                ? FloorTable.Warden(Rules, Sinner, Effects, Purse, NextMatchSeed(), DeckCards, marks, houseCoins)
                : FloorTable.Imp(Rules, Sinner, Effects, Purse, NextMatchSeed(), DeckCards, marks, Current.Floor, houseCoins);
        }

        /// <summary>The marks of a saved match come back (they were taken by <see cref="OpenTable"/> before it was saved).</summary>
        public FloorTable ReopenTable(TableMarks marks, int houseCoins)
        {
            NextTableMarks = marks ?? TableMarks.None;
            _matches = Math.Max(0, _matches - 1);
            return OpenTable(houseCoins);
        }

        /// <summary>
        /// The match is over: the deck goes on. The player's purse empty: the run is over (<see cref="PurseEmptied"/>). A warden beaten
        /// gives a relic — or, when the run carries all it may, the relic waits (<see cref="WardenRelicWaiting"/>) for a swap or the coins.
        /// </summary>
        public void FinishTable(FloorTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (!table.IsOver) throw new InvalidOperationException("The match is not over.");
            DeckCards = table.DeckCards;
            if (Purse.IsEmpty) PurseEmptied = true;
            if (table == PendingGamble)
            {
                PendingGamble = null;
                return;
            }
            if (!table.MatchWon || !table.IsWarden) return;

            string relic = RandomRelicsNotCarried(1).FirstOrDefault();
            if (relic == null) Purse.Add(Rules.WardenRelicCoins);
            else if (!Effects.AddRelic(relic)) WardenRelicWaiting = relic;
        }

        /// <summary>The warden's relic in place of <paramref name="swapOut"/> (no coins then).</summary>
        public bool SwapForWardenRelic(string swapOut)
        {
            if (WardenRelicWaiting == null || RelicRoster.IsReward(swapOut) || !Effects.RemoveRelic(swapOut)) return false;
            Effects.AddRelic(WardenRelicWaiting);
            WardenRelicWaiting = null;
            return true;
        }

        /// <summary>The coins instead of the warden's relic.</summary>
        public void TakeWardenCoins()
        {
            if (WardenRelicWaiting == null) return;
            WardenRelicWaiting = null;
            Purse.Add(Rules.WardenRelicCoins);
        }

        private IReadOnlyList<string> RandomRelicsNotCarried(int count)
        {
            var pool = RelicRoster.Offered.Select(r => r.Id).Where(id => !Effects.Relics.Contains(id)).ToList();
            var picked = new List<string>();
            while (picked.Count < count && pool.Count > 0)
            {
                int i = _nodeRandom.Next(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }

        public void TakeTreasure()
        {
            if (Current?.Kind != NodeKind.Treasure) throw new InvalidOperationException("There is no treasure here.");
            Purse.Add(Rules.TreasureCoins);
        }

        public BlackMarket OpenMarket()
        {
            if (Current?.Kind != NodeKind.BlackMarket) throw new InvalidOperationException("There is no market here.");
            return new BlackMarket(this, _nodeRandom);
        }

        /// <summary>The offer at the current event node: one not seen this chapter if any can appear; null when none can.</summary>
        public IFloorEvent DrawEvent(IReadOnlyList<IFloorEvent> deck = null)
        {
            if (Current?.Kind != NodeKind.Event) throw new InvalidOperationException("There is no event here.");
            var can = (deck ?? FloorEventDeck.For(Rules)).Where(e => e.CanAppear(this)).ToList();
            var fresh = can.Where(e => !_eventsSeen.Contains(e.Id)).ToList();
            var pool = fresh.Count > 0 ? fresh : can;
            if (pool.Count == 0) return null;
            IFloorEvent offer = pool[_nodeRandom.Next(pool.Count)];
            _eventsSeen.Add(offer.Id);
            return offer;
        }

        /// <summary>Years written on the demon's bar at the gate (an offer's price).</summary>
        public void OweAtGate(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            YearsOwed += years;
        }

        /// <summary>Years struck off the share now (never the last one: only a hand ends a sentence).</summary>
        public void StrikeYears(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years -= Math.Min(years, Years - 1);
        }

        /// <summary>The gambler's single hand for <paramref name="stake"/> coins: play <see cref="PendingGamble"/>, then <see cref="FinishTable"/>.</summary>
        public FloorTable OpenGamble(int stake)
        {
            PendingGamble = FloorTable.Gamble(Rules, stake, Sinner, Effects, Purse, NextMatchSeed(), DeckCards);
            return PendingGamble;
        }

        public bool CanTend(FireChoice choice) => choice != FireChoice.SilenceCurse || Effects.Relics.Count > 0;

        /// <summary>The purgatory fire's boon. <paramref name="relicId"/>: the relic whose curse is silenced.</summary>
        public bool TendFire(FireChoice choice, string relicId = null)
        {
            if (Current?.Kind != NodeKind.PurgatoryFire) throw new InvalidOperationException("There is no fire here.");
            switch (choice)
            {
                case FireChoice.BreakFirstCheat:
                    BreaksFirstCheat = true;
                    return true;
                case FireChoice.SilenceCurse:
                    return relicId != null && Effects.SilenceCurse(relicId);
                default:
                    RestedByTheFire = true;
                    return true;
            }
        }

        /// <summary>
        /// The gate: the tribute (<see cref="Tribute"/>) is paid from the purse; every coin missing is <see cref="ChapterRules.YearsPerMissingCoin"/>
        /// years on the demon's bar, and what the floors' offers left owing is written on too. What is left of the purse goes on.
        /// </summary>
        public GateToll PayTribute()
        {
            int before = Purse.Coins;
            int tribute = Tribute;
            int paid = TributePaid(before);
            int missing = tribute - paid;
            int yearsForMissing = missing * Rules.YearsPerMissingCoin;
            Years += yearsForMissing + YearsOwed;
            int owed = YearsOwed;
            YearsOwed = 0;
            Purse.Add(-paid);
            return new GateToll(before, paid, missing, yearsForMissing, owed, Years, Purse.Coins);
        }

        /// <summary>The chapter is over (after its demon): a silenced curse speaks again, a desired relic is itself again.</summary>
        public void EndChapter()
        {
            Effects.LiftSilence();
            Effects.LiftAmplify();
        }

        // ------------------------------------------------------------------ the demon's table, after the gate

        /// <summary>The dice of the demon's table under the chapter's seed.</summary>
        public const int BossStream = 13;

        /// <summary>The demon's bar as the table began (the soul line is measured from it); 0 before the table.</summary>
        public int BossBarStart { get; private set; }

        /// <summary>
        /// The chapter's demon after the tribute: the run's share of the sentence at this demon (<see cref="Years"/>, the tribute's years
        /// on it, a rest by the fire off it) is the demon's bar (<see cref="BossTable"/>). The run's sinner, relics and a fresh deck, the
        /// demon's own temper and cheats behind the fire's breaker (when chosen). Played until the bar is empty or the soul burns.
        /// </summary>
        /// <param name="table">The template of the table's numbers (the soul's worth, the cheats' pace, the deck).</param>
        public HellPokerGame OpenBossTable(GameRules table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (RestedByTheFire)
            {
                Years = Math.Max(1, Years - Years * RestBarPercent / 100);
                RestedByTheFire = false;
            }
            Dealers.Dealer boss = Rules.Boss;
            Effects.SitAt(boss.Id, fresh: true);
            BossBarStart = Years;
            HellPokerGame game = BossTable.Create(table, boss, Years, Rules.SoulLinePercent, Rules.BossWinPercent, Rules.BossLossPercent,
                RandomSeeds.Derive(_seed, BossStream), BossGuard(), Sinner);
            game.UseEffects(Effects);
            return game;
        }

        /// <summary>A saved demon's table comes back: the bar it began with and the bar it stands at.</summary>
        public HellPokerGame ReopenBossTable(GameRules table, int barStart, int bar, int handsPlayed)
        {
            if (barStart <= 0 || bar < 0) throw new ArgumentOutOfRangeException(nameof(barStart));
            Years = barStart;
            RestedByTheFire = false;
            HellPokerGame game = OpenBossTable(table);
            if (bar != barStart || handsPlayed > 0) game.TakeOver(Math.Max(1, bar), handsPlayed);
            return game;
        }

        /// <summary>The demon's table is over: the bar is empty (the demon beaten) or the soul burned.</summary>
        public static bool BossTableOver(GamePhase phase) => BossTable.IsOver(phase);

        /// <summary>The demon is beaten: the bar is empty.</summary>
        public bool BossBeaten { get; private set; }

        /// <summary>The bar the player leaves the demon's table with: 0 when the demon is beaten.</summary>
        public void LeaveBossTable(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = years;
            BossBeaten = years == 0;
            if (BossBeaten && _loot == null) _loot = RandomRelicsNotCarried(LootRelics).ToList();
        }

        // ------------------------------------------------------------------ the loot of a beaten demon

        private List<string> _loot;

        /// <summary>The relics a beaten demon leaves (choose one, or <see cref="LootCoins"/>); empty before he is beaten.</summary>
        public IReadOnlyList<string> LootOffers => (IReadOnlyList<string>)_loot ?? Array.Empty<string>();

        /// <summary>The loot was taken (a relic or the coins).</summary>
        public bool LootTaken { get; private set; }

        /// <summary>A relic of the loot; when the run carries all it may, <paramref name="swapOut"/> goes in its place.</summary>
        public bool TakeLoot(string relicId, string swapOut = null)
        {
            if (LootTaken || !BossBeaten || relicId == null || !LootOffers.Contains(relicId)) return false;
            if (Effects.CarriedOffered >= RelicRoster.MaxCarried)
            {
                if (swapOut == null || RelicRoster.IsReward(swapOut) || !Effects.RemoveRelic(swapOut)) return false;
            }
            if (!Effects.AddRelic(relicId)) return false;
            LootTaken = true;
            return true;
        }

        /// <summary>The coins instead of a relic.</summary>
        public bool TakeLootCoins()
        {
            if (LootTaken || !BossBeaten) return false;
            Purse.Add(LootCoins);
            LootTaken = true;
            return true;
        }

        // ------------------------------------------------------------------ a saved chapter

        /// <summary>
        /// A saved chapter comes back onto a fresh one (same seed: the same map): where the player stood and walked, the share and
        /// what is owed, the marks, the fire's boons, the offers seen, the deck, the loot.
        /// </summary>
        public void Restore(IEnumerable<int> trailLanes, int years, int owed, TableMarks marks, bool breaksFirstCheat, bool rested,
            IEnumerable<string> eventsSeen, IReadOnlyList<Card> deck, int matches, string wardenRelicWaiting, int bossBarStart,
            bool bossBeaten, IEnumerable<string> loot, bool lootTaken)
        {
            _trail.Clear();
            Current = null;
            int floor = 0;
            foreach (int lane in trailLanes ?? Enumerable.Empty<int>())
            {
                if (floor >= Map.FloorCount || lane < 0 || lane >= Rules.Lanes) break;
                MapNode node = Map[floor, lane];
                if (Current != null && !Map.NextFrom(Current).Contains(node)) break;
                Current = node;
                _trail.Add(node);
                floor++;
            }
            if (years > 0) Years = years;
            YearsOwed = Math.Max(0, owed);
            NextTableMarks = marks ?? TableMarks.None;
            BreaksFirstCheat = breaksFirstCheat;
            RestedByTheFire = rested;
            _eventsSeen.Clear();
            foreach (string id in eventsSeen ?? Enumerable.Empty<string>()) _eventsSeen.Add(id);
            DeckCards = deck ?? Array.Empty<Card>();
            _matches = Math.Max(0, matches);
            WardenRelicWaiting = RelicRoster.Find(wardenRelicWaiting ?? "") != null ? wardenRelicWaiting : null;
            BossBarStart = Math.Max(0, bossBarStart);
            BossBeaten = bossBeaten;
            var offers = (loot ?? Enumerable.Empty<string>()).Where(id => RelicRoster.Find(id) != null).ToList();
            _loot = bossBeaten ? offers : null;
            LootTaken = lootTaken;
        }
    }
}
