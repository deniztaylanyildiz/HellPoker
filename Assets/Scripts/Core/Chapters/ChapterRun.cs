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
        RestByTheFire,

        /// <summary>Whet the soul: a class upgrade at once — but the demon's bar starts longer by <see cref="ChapterRun.WhetBarPercent"/>.</summary>
        WhetSoul
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

    /// <summary>What waited at the gate besides the tribute: a debtor imp's coins (−1: no loan; 0: he never came), the insurer gone.</summary>
    public sealed class GateArrival
    {
        public int LoanRepaid { get; }
        public bool InsurerGone { get; }

        public GateArrival(int loanRepaid, bool insurerGone)
        {
            LoanRepaid = loanRepaid;
            InsurerGone = insurerGone;
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

        /// <summary>Whetting the soul by the fire: the demon's bar starts this much (percent of itself) longer.</summary>
        public const int WhetBarPercent = 10;

        /// <summary>A beaten demon's loot: this many relics to choose from (his own two), a class upgrade, or these coins.</summary>
        public const int LootRelics = 2, LootCoins = 30;

        private readonly int _seed;
        private readonly IRandomSource _nodeRandom;
        private int _matches;
        private readonly List<MapNode> _trail = new List<MapNode>();
        private readonly HashSet<string> _eventsSeen = new HashSet<string>();

        public ChapterRules Rules { get; }

        /// <summary>The class's Phase 2 mechanics (the satchel, the prophecy, the treasury): the run's, from chapter to chapter.</summary>
        public ClassKit Kit { get; }

        /// <summary>The hooks of the table opened last (its class acts, its demon's rule); null before one.</summary>
        public ChapterHooks Hooks { get; private set; }

        private FloorTable Hooked(FloorTable table)
        {
            Hooks = ChapterHooks.Attach(table.Game, Kit, null, 0, RandomSeeds.Derive(_seed, MatchStream + 100 + _matches));
            if (Hooks.Summon != null) Hooks.Summon.Purse = Purse;   // the Summoner's imps cost coins on the floors
            return table;
        }

        public ChapterMap Map { get; }
        public Sinner Sinner { get; }
        public RunEffects Effects { get; }
        public CoinPurse Purse { get; }

        /// <summary>The run's share of the sentence at this chapter's demon (<see cref="BossShares"/>): carried through the floors untouched
        /// (but by an offer), the tribute's years put on it at the gate; at the demon's table it is the demon's bar.</summary>
        public int Years { get; private set; }

        /// <summary>Years the floors' offers left owing: written on the demon's bar at the gate.</summary>
        public int YearsOwed { get; private set; }

        /// <summary>The run's sentence (<see cref="RunSentence"/>): years owed or struck go on or off it at once, the tribute's at the gate,
        /// and it follows the demon's table. Null for a chapter played alone (no run around it).</summary>
        public RunSentence Sentence { get; set; }

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

        /// <summary>The pickpocket's chase, opened by the choice to run after him; null when none is waiting.</summary>
        public FloorTable PendingChase { get; private set; }

        /// <summary>A side table (the gambler's hand, the pickpocket's chase) waiting to be played; null for none.</summary>
        public FloorTable PendingSideTable => PendingGamble ?? PendingChase;

        public bool AtGate => Current != null && Current.Floor == Map.FloorCount - 1;

        /// <summary>The coins the gate takes from this sinner: their class's starting purse and the chapter's
        /// <see cref="ChapterRules.TributeOverStart"/>.</summary>
        public int Tribute => Math.Max(1, Rules.TributeFor(Sinner.Id) * Effects.CombinedRelics.TributePercent / 100);

        /// <summary>A price of the black market's this chapter: its first-chapter price grown with the chapter, the relics' percent on it.</summary>
        public int PriceOf(int firstChapterPrice) => Math.Max(1, Rules.Price(firstChapterPrice) * Effects.CombinedRelics.MarketPercent / 100);

        /// <summary>Which relics may be drawn (a locked one may not); null: all.</summary>
        public Func<string, bool> RelicAvailable { get; set; }

        /// <summary>What the gate takes from a purse of <paramref name="coins"/>: the tribute — but never the last coin (the next chapter's
        /// first table needs one).</summary>
        public int TributePaid(int coins) => Math.Min(Tribute, Math.Max(0, coins - 1));

        /// <summary>The years a purse of <paramref name="coins"/> owes at the gate: every coin of the tribute it cannot pay.</summary>
        public int TributeYears(int coins) => (Tribute - TributePaid(coins)) * Rules.YearsPerMissingCoin;

        /// <summary>The demon's bar as his table will begin with <paramref name="coins"/> paid at the gate: the share, the tribute's missing
        /// coins, what is owed; then the fire (a rest shorter, a whetted soul longer) and the relics' percent.</summary>
        public int BarAfterGate(int coins)
        {
            int bar = Years + TributeYears(coins) + YearsOwed;
            if (RestedByTheFire) bar = Math.Max(1, bar - bar * RestBarPercent / 100);
            if (WhettedSoul) bar += bar * WhetBarPercent / 100;
            int percent = Effects.CombinedRelics.BossBarPercent * Circles.BarAt(Rules.Circle) / 100;
            return percent == 100 ? bar : Math.Max(1, bar * percent / 100);
        }

        /// <summary>The player's purse went empty at a floor's table: the run is over (it counts as damnation).</summary>
        public bool PurseEmptied { get; private set; }

        /// <summary>The offers the player has seen this chapter (an offer comes once while others are left).</summary>
        public IReadOnlyCollection<string> EventsSeen => _eventsSeen;

        /// <summary>The matches opened so far (each its own dice).</summary>
        public int MatchesOpened => _matches;

        /// <summary>A new run's first chapter: the purse the sinner's class starts with (<see cref="ChapterRules.StartingCoinsOf"/>) and
        /// the class's share of the sentence at the chapter's demon (<see cref="BossShares"/>).</summary>
        public static ChapterRun Begin(ChapterRules rules, Sinner sinner, RunEffects effects, int seed, int mapVersion = ChapterMap.CurrentVersion)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return new ChapterRun(rules, sinner, effects, BossShares.For(sinner?.Id, rules.BossId), rules.StartingCoinsOf(sinner?.Id), seed,
                mapVersion: mapVersion);
        }

        /// <param name="years">The run's share of the sentence at the chapter's demon.</param>
        /// <param name="coins">The purse the chapter starts with: a new run's class purse, or what is left from the last chapter.</param>
        /// <param name="kit">The class's Phase 2 mechanics carried from the last chapter (null: a new one).</param>
        /// <param name="mapVersion">The map's generator (<see cref="ChapterMap.CurrentVersion"/>; an older save's own).</param>
        public ChapterRun(ChapterRules rules, Sinner sinner, RunEffects effects, int years, int coins, int seed, IReadOnlyList<Card> deck = null,
            ClassKit kit = null, int mapVersion = ChapterMap.CurrentVersion)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Sinner = sinner ?? throw new ArgumentNullException(nameof(sinner));
            Kit = kit ?? new ClassKit(sinner);
            if (kit == null) Kit.EnterChapter(rules.Number);
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            if (years <= 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            Years = years;
            Purse = new CoinPurse(coins);
            _seed = seed;
            _nodeRandom = new SystemRandomSource(RandomSeeds.Derive(seed, NodeStream));
            Map = ChapterMap.Generate(rules, new SystemRandomSource(RandomSeeds.Derive(seed, MapStream)), mapVersion);
            DeckCards = deck ?? Array.Empty<Card>();
        }

        /// <summary>What waits at <paramref name="node"/>: an unknown node's content, any other node's own kind.</summary>
        public static NodeKind KindOf(MapNode node) => node == null ? NodeKind.Table : node.Kind == NodeKind.Unknown ? node.Hidden : node.Kind;

        /// <summary>What waits where the player stands (an unknown node: what it turned out to be); null before the first floor.</summary>
        public NodeKind? CurrentKind => Current == null ? (NodeKind?)null : KindOf(Current);

        private readonly HashSet<MapNode> _revealed = new HashSet<MapNode>();

        /// <summary>An unknown node shows what it holds from now on (a lost soul's way, a lantern's light).</summary>
        public void Reveal(MapNode node)
        {
            if (node != null && node.Kind == NodeKind.Unknown) _revealed.Add(node);
        }

        /// <summary>The unknown nodes revealed ahead of time (for the save).</summary>
        public IReadOnlyCollection<MapNode> Revealed => _revealed;

        /// <summary>What the map shows at <paramref name="node"/>: its kind, or an unknown's content once it is walked or revealed.</summary>
        public NodeKind ShownKind(MapNode node) =>
            node.Kind != NodeKind.Unknown || _revealed.Contains(node) || _trail.Contains(node) || Effects.CombinedRelics.RevealsUnknown ? KindOf(node) : NodeKind.Unknown;

        /// <summary>The nodes the player may step to: the first floor's at the start, then the ones the current node leads to.</summary>
        public IEnumerable<MapNode> Choices =>
            Current == null ? Map.Floors[0]
            : SkipNext && Current.Floor + 2 < Map.FloorCount ? Map.NextFrom(Current).SelectMany(Map.NextFrom).Distinct()
            : Map.NextFrom(Current);

        public void MoveTo(MapNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (PurseEmptied) throw new InvalidOperationException("The run is over: the purse is empty.");
            if (!Choices.Contains(node)) throw new InvalidOperationException($"{node} cannot be reached from here.");
            if (WardenRelicWaiting != null) throw new InvalidOperationException("The warden's relic waits for a choice.");
            if (Current != null && node.Floor == Current.Floor + 2)
            {
                // The lava ferry: carried over a floor — through one of its nodes, nothing done there.
                MapNode over = Map.NextFrom(Current).First(n => Map.NextFrom(n).Contains(node));
                _trail.Add(over);
                _skipped.Add(over);
                SkipNext = false;
            }
            Current = node;
            _trail.Add(node);
            Arrival = null;
        }

        private readonly HashSet<MapNode> _skipped = new HashSet<MapNode>();

        /// <summary>A node of the trail the ferry carried the player over (nothing was done there).</summary>
        public bool WasSkipped(MapNode node) => _skipped.Contains(node);

        private int NextMatchSeed() => RandomSeeds.Derive(RandomSeeds.Derive(_seed, MatchStream), ++_matches);

        /// <summary>The match at the current node (a table's imp or a warden), under the marks that waited for it.</summary>
        /// <param name="houseCoins">A saved match's imp purse (-1: a fresh match).</param>
        public FloorTable OpenTable(int houseCoins = -1)
        {
            if (Current == null || (CurrentKind != NodeKind.Table && CurrentKind != NodeKind.Warden))
                throw new InvalidOperationException("There is no table here.");
            TableMarks marks = NextTableMarks;
            NextTableMarks = TableMarks.None;
            bool warden = CurrentKind == NodeKind.Warden;
            if (houseCoins < 0)
            {
                // A fresh match takes what the offers left for it: the accountant's cheaper antes, a reported ledger's richer warden.
                if (AnteCutTables > 0)
                {
                    marks = marks.With(new TableMarks(anteDelta: -1));
                    AnteCutTables--;
                }
                if (warden && NextWardenPursePercent != 100)
                {
                    houseCoins = Math.Max(1, Rules.WardenCoins * NextWardenPursePercent / 100);
                    NextWardenPursePercent = 100;
                }
            }
            return Hooked(warden
                ? FloorTable.Warden(Rules, Sinner, Effects, Purse, NextMatchSeed(), DeckCards, marks, houseCoins)
                : FloorTable.Imp(Rules, Sinner, Effects, Purse, NextMatchSeed(), DeckCards, marks, Current.Floor, houseCoins));
        }

        // ------------------------------------------------------------------ what Demo 2.1's offers leave behind

        /// <summary>The next tables (imps' or wardens') whose ante is one less (Mammon's accountant, his ledger put right).</summary>
        public int AnteCutTables { get; set; }

        /// <summary>The next warden's purse, in percent of his own (a reported ledger: 150).</summary>
        public int NextWardenPursePercent { get; set; } = 100;

        /// <summary>Coins a debtor imp owes back at the gate (0: none) — and whether he will be there to pay (rolled as the loan is made).</summary>
        public int LoanDue { get; set; }
        public bool LoanPays { get; set; }

        /// <summary>The soul insurance was bought (the insurer is gone by the gate).</summary>
        public bool Insured { get; set; }

        /// <summary>The confession: this chapter's demon sits down with his malice full.</summary>
        public bool BossMaliceFull { get; set; }

        /// <summary>The lava ferry: the next step skips a floor (to a node two floors down, through one of the next floor's).</summary>
        public bool SkipNext { get; set; }

        /// <summary>A number an offer keeps while its panel waits (the coins a pickpocket took).</summary>
        public int EventMemo { get; set; }

        /// <summary>The relics a sin swap offers in place of the one given (empty when none waits).</summary>
        public List<string> EventOffers { get; } = new List<string>();

        /// <summary>The offers of Demo 2.1 seen anywhere in the run (each comes once a run); shared by the run's chapters.</summary>
        public ISet<string> RunEventsSeen { get; set; } = new HashSet<string>();

        /// <summary>The dice of the nodes (the market, the offers, the relics), for an offer's outcome.</summary>
        public IRandomSource NodeDice => _nodeRandom;

        /// <summary>An amount of an offer grown with the chapter: ×1, ×1.25, ×1.5.</summary>
        public int Scaled(int amount) => amount * (100 + 25 * Math.Max(0, Rules.Number - 1)) / 100;

        /// <summary>A relic the run is given (an offer's, a lottery's): carried when a slot is free, else it waits for a choice (as a
        /// warden's does). False when none could be drawn.</summary>
        public bool GiveRelic(string id)
        {
            if (id == null) return false;
            if (!Effects.AddRelic(id)) WardenRelicWaiting = id;
            return true;
        }

        /// <summary>A relic of <paramref name="rarity"/> not carried (a locked one never); null when none is left.</summary>
        public string DrawRelic(RelicRarity rarity) =>
            ChapterRelics.Draw(_nodeRandom, rarity == RelicRarity.Common ? new RelicWeights(1, 0, 0) : rarity == RelicRarity.Rare ? new RelicWeights(0, 1, 0)
                : new RelicWeights(0, 0, 1), 1, Effects.Relics.ToList(), RelicAvailable).FirstOrDefault();

        /// <summary>Whether <paramref name="count"/> relics of <paramref name="rarity"/> (null: any) could still be drawn.</summary>
        public bool CanDrawRelic(RelicRarity? rarity, int count = 1) =>
            ChapterRelics.All.Count(id => !Effects.Relics.Contains(id) && (RelicAvailable == null || RelicAvailable(id)) &&
                                          (rarity == null || ChapterRelics.RarityOf(id) == rarity.Value)) >= count;

        /// <summary>A helper's good turn, in percent, as the relics bend it (the Lost Soul's Letter more, the Liar's Tongue less).</summary>
        public int HelpPercent(int percent) =>
            Math.Max(0, percent * (100 + Effects.CombinedRelics.Extra(RelicExtras.HelpGoodPercent)) / 100);

        /// <summary>A class upgrade the run does not have yet, given at once; null when every one is had.</summary>
        public string GiveUpgrade()
        {
            IReadOnlyList<string> upgrades = UpgradesOffered();
            if (upgrades.Count == 0) return null;
            string upgrade = upgrades[_nodeRandom.Next(upgrades.Count)];
            return Kit.AddUpgrade(upgrade) ? upgrade : null;
        }

        /// <summary>The pickpocket's chase for <paramref name="stolen"/> coins: play <see cref="PendingChase"/>, then <see cref="FinishTable"/>.</summary>
        public FloorTable OpenChase(int stolen, int houseCoins = -1, int handsPlayed = 0)
        {
            EventMemo = Math.Max(1, stolen);
            PendingChase = Hooked(FloorTable.Chase(Rules, ChaseHands, EventMemo + Scaled(ChaseBonus), Sinner, Effects, Purse, NextMatchSeed(), DeckCards, houseCoins));
            if (handsPlayed > 0) PendingChase.Resume(handsPlayed);
            return PendingChase;
        }

        /// <summary>The chase: this many hands; caught, the thief gives back what he took and this much more.</summary>
        public const int ChaseHands = 3, ChaseBonus = 10;

        /// <summary>The last chase was won (the stolen coins came back, and more); null before one.</summary>
        public bool? ChaseWon { get; private set; }

        /// <summary>What the gate found (the debtor imp, the insurer); set as the player arrives, once.</summary>
        public GateArrival Arrival { get; private set; }

        /// <summary>The player stands at the gate: a debtor imp pays back (or is gone), the insurer is gone. Once.</summary>
        public GateArrival ArriveAtGate()
        {
            if (Arrival != null) return Arrival;
            int repaid = LoanDue <= 0 ? -1 : LoanPays ? LoanDue : 0;
            if (repaid > 0) Purse.Add(repaid);
            Arrival = new GateArrival(repaid, Insured);
            LoanDue = 0;
            Insured = false;
            return Arrival;
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
            if (table == PendingChase)
            {
                PendingChase = null;
                ChaseWon = !PurseEmptied && table.Coins > 0;
                if (ChaseWon == true) Purse.Add(EventMemo + Scaled(ChaseBonus));
                EventMemo = 0;
                return;
            }
            if (!table.MatchWon || !table.IsWarden) return;

            string relic = ChapterRelics.Draw(_nodeRandom, RelicWeights.Warden, 1, Effects.Relics.ToList(), RelicAvailable).FirstOrDefault();
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

        /// <summary>A beaten demon's loot: his own two relics (one carried already: a rare in its place), and a class upgrade not yet had.</summary>
        private void DrawLoot()
        {
            var loot = ChapterRelics.BossLoot(Rules.BossId).Where(id => !Effects.Relics.Contains(id)).ToList();
            if (loot.Count < LootRelics)
            {
                var exclude = Effects.Relics.Concat(loot).ToList();
                loot.AddRange(ChapterRelics.Draw(_nodeRandom, new RelicWeights(0, 1, 0), LootRelics - loot.Count, exclude, RelicAvailable));
            }
            _loot = loot;
            IReadOnlyList<string> upgrades = UpgradesOffered();
            LootUpgrade = upgrades.Count > 0 ? upgrades[_nodeRandom.Next(upgrades.Count)] : null;
        }

        /// <summary>Which upgrades may be given (a locked one may not); null: all.</summary>
        public Func<string, bool> UpgradeAvailable { get; set; }

        private IReadOnlyList<string> UpgradesOffered() => Kit.UpgradesLeft.Where(u => UpgradeAvailable == null || UpgradeAvailable(u)).ToArray();

        public void TakeTreasure()
        {
            if (CurrentKind != NodeKind.Treasure) throw new InvalidOperationException("There is no treasure here.");
            Purse.Add(TreasureCoins);
        }

        /// <summary>A table imp's purse on <paramref name="floor"/> (the relics' percent on the chapter's).</summary>
        public int ImpCoinsAt(int floor) => Math.Max(1, Rules.ImpCoinsAt(floor) * Effects.CombinedRelics.ImpPursePercent / 100);

        /// <summary>A treasure's coins (the relics' percent on the chapter's).</summary>
        public int TreasureCoins => Rules.TreasureCoins * Effects.CombinedRelics.TreasurePercent / 100;

        public BlackMarket OpenMarket()
        {
            if (CurrentKind != NodeKind.BlackMarket) throw new InvalidOperationException("There is no market here.");
            return new BlackMarket(this, _nodeRandom);
        }

        /// <summary>The offer at the current event node: one not seen this chapter if any can appear; null when none can.</summary>
        public IFloorEvent DrawEvent(IReadOnlyList<IFloorEvent> deck = null)
        {
            if (CurrentKind != NodeKind.Event) throw new InvalidOperationException("There is no event here.");
            // Demo 2.1's offers come once a run; the chapters' own once a chapter while others are left.
            var can = (deck ?? FloorEventDeck.For(Rules)).Where(e => e.CanAppear(this) && !(e is IChoiceEvent && RunEventsSeen.Contains(e.Id))).ToList();
            var fresh = can.Where(e => !_eventsSeen.Contains(e.Id)).ToList();
            var pool = fresh.Count > 0 ? fresh : can;
            if (pool.Count == 0) return null;
            IFloorEvent offer = pool[_nodeRandom.Next(pool.Count)];
            _eventsSeen.Add(offer.Id);
            if (offer is IChoiceEvent choice)
            {
                RunEventsSeen.Add(offer.Id);
                EventOffers.Clear();
                if (choice.IsEncounter) choice.Appear(this);   // it happens as it appears (the pickpocket's hand)
            }
            return offer;
        }

        /// <summary>Years written on the demon's bar at the gate (an offer's price).</summary>
        public void OweAtGate(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            YearsOwed += years;
            Sentence?.Add(years);
        }

        /// <summary>Years struck off the share now (never the last one: only a hand ends a sentence).</summary>
        public void StrikeYears(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            int struck = Math.Min(years, Years - 1);
            Years -= struck;
            Sentence?.Strike(struck);
        }

        /// <summary>The gambler's single hand for <paramref name="stake"/> coins: play <see cref="PendingGamble"/>, then <see cref="FinishTable"/>.</summary>
        public FloorTable OpenGamble(int stake)
        {
            PendingGamble = Hooked(FloorTable.Gamble(Rules, stake, Sinner, Effects, Purse, NextMatchSeed(), DeckCards));
            return PendingGamble;
        }

        public bool CanTend(FireChoice choice) => FireOffers.Contains(choice) &&
            (choice == FireChoice.SilenceCurse ? Effects.Relics.Count > 0 : choice != FireChoice.WhetSoul || UpgradesOffered().Count > 0);

        /// <summary>The dice of the fire's offer (the fourth circle: two boons of four, the same every time the fire is shown).</summary>
        public const int FireStream = 14;

        /// <summary>The boons this chapter's fire offers: all four — or, from the fourth circle on, two of them.</summary>
        public IReadOnlyList<FireChoice> FireOffers
        {
            get
            {
                var all = ((FireChoice[])Enum.GetValues(typeof(FireChoice))).ToList();
                int count = Circles.FireOffersAt(Rules.Circle);
                if (count >= all.Count) return all;
                var dice = new SystemRandomSource(RandomSeeds.Derive(_seed, FireStream));
                var offered = new List<FireChoice>();
                while (offered.Count < count)
                {
                    int i = dice.Next(all.Count);
                    offered.Add(all[i]);
                    all.RemoveAt(i);
                }
                return offered.OrderBy(c => c).ToList();
            }
        }

        /// <summary>The soul was whetted by the fire: the demon's bar starts longer (<see cref="WhetBarPercent"/>).</summary>
        public bool WhettedSoul { get; private set; }

        /// <summary>The upgrade the fire gave (when the soul was whetted); null otherwise.</summary>
        public string FireUpgrade { get; private set; }

        /// <summary>The purgatory fire's boon. <paramref name="relicId"/>: the relic whose curse is silenced.</summary>
        public bool TendFire(FireChoice choice, string relicId = null)
        {
            if (CurrentKind != NodeKind.PurgatoryFire) throw new InvalidOperationException("There is no fire here.");
            if (!FireOffers.Contains(choice)) return false;
            switch (choice)
            {
                case FireChoice.BreakFirstCheat:
                    BreaksFirstCheat = true;
                    return true;
                case FireChoice.SilenceCurse:
                    return relicId != null && Effects.SilenceCurse(relicId);
                case FireChoice.WhetSoul:
                {
                    IReadOnlyList<string> upgrades = UpgradesOffered();
                    if (upgrades.Count == 0) return false;
                    FireUpgrade = upgrades[_nodeRandom.Next(upgrades.Count)];
                    Kit.AddUpgrade(FireUpgrade);
                    WhettedSoul = true;
                    return true;
                }
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
            ArriveAtGate();
            int before = Purse.Coins;
            int tribute = Tribute;
            int paid = TributePaid(before);
            int missing = tribute - paid;
            int yearsForMissing = missing * Rules.YearsPerMissingCoin;
            Years += yearsForMissing + YearsOwed;
            Sentence?.Add(yearsForMissing);   // what was owed went on the sentence as it was owed
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
        public HellPokerGame OpenBossTable(GameRules table) => OpenBossTable(table, startBar: true);

        /// <param name="startBar">The bar is set as the table begins (the fire, the relics); false for a saved table's bar.</param>
        private HellPokerGame OpenBossTable(GameRules table, bool startBar)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (!startBar)
            {
                RestedByTheFire = false;
                WhettedSoul = false;
            }
            if (RestedByTheFire)
            {
                Years = Math.Max(1, Years - Years * RestBarPercent / 100);
                RestedByTheFire = false;
            }
            if (WhettedSoul)
            {
                Years += Years * WhetBarPercent / 100;
                WhettedSoul = false;
            }
            int barPercent = Effects.CombinedRelics.BossBarPercent * Circles.BarAt(Rules.Circle) / 100;
            if (startBar && barPercent != 100) Years = Math.Max(1, Years * barPercent / 100);
            Dealers.Dealer boss = Rules.Boss;
            Effects.SitAt(boss.Id, fresh: true);
            BossBarStart = Years;
            HellPokerGame game = BossTable.Create(table, boss, Years, Rules.SoulLinePercent, Circles.WinPercentAt(Rules.Circle, Rules.BossWinPercent), Rules.BossLossPercent,
                RandomSeeds.Derive(_seed, BossStream), BossGuard(), Sinner, Circles.MaliceShortAt(Rules.Circle));
            game.UseEffects(Effects);
            if (startBar && BossMaliceFull) game.RestoreMalice(game.MaliceMax, false);   // the confession: he knows
            BossMaliceFull = false;
            Sentence?.Follow(game);
            Hooks = ChapterHooks.Attach(game, Kit, boss.Id, BossBarStart, RandomSeeds.Derive(_seed, BossStream));
            return game;
        }

        /// <summary>A saved demon's table comes back: the bar it began with and the bar it stands at.</summary>
        public HellPokerGame ReopenBossTable(GameRules table, int barStart, int bar, int handsPlayed)
        {
            if (barStart <= 0 || bar < 0) throw new ArgumentOutOfRangeException(nameof(barStart));
            Years = barStart;
            HellPokerGame game = OpenBossTable(table, startBar: false);
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
            Sentence?.Unfollow();
            Years = years;
            BossBeaten = years == 0;
            if (BossBeaten && _loot == null) DrawLoot();
        }

        // ------------------------------------------------------------------ the loot of a beaten demon

        private List<string> _loot;

        /// <summary>The class upgrade a beaten demon leaves (or a relic, or the coins); null when the class has every upgrade.</summary>
        public string LootUpgrade { get; private set; }

        /// <summary>The relics a beaten demon leaves (choose one, or <see cref="LootCoins"/>); empty before he is beaten.</summary>
        public IReadOnlyList<string> LootOffers => (IReadOnlyList<string>)_loot ?? Array.Empty<string>();

        /// <summary>The loot was taken (a relic or the coins).</summary>
        public bool LootTaken { get; private set; }

        /// <summary>A relic of the loot; when the run carries all it may, <paramref name="swapOut"/> goes in its place.</summary>
        public bool TakeLoot(string relicId, string swapOut = null)
        {
            if (LootTaken || !BossBeaten || relicId == null || !LootOffers.Contains(relicId)) return false;
            if (Effects.RelicSlotsFull)
            {
                if (swapOut == null || RelicRoster.IsReward(swapOut) || !Effects.RemoveRelic(swapOut)) return false;
            }
            if (!Effects.AddRelic(relicId)) return false;
            LootTaken = true;
            return true;
        }

        /// <summary>The loot's class upgrade instead of a relic.</summary>
        public bool TakeLootUpgrade()
        {
            if (LootTaken || !BossBeaten || LootUpgrade == null || !Kit.AddUpgrade(LootUpgrade)) return false;
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
            bool bossBeaten, IEnumerable<string> loot, bool lootTaken, string lootUpgrade = null, bool whetted = false)
        {
            _trail.Clear();
            Current = null;
            int floor = 0;
            foreach (int lane in trailLanes ?? Enumerable.Empty<int>())
            {
                MapNode node = Map.Find(floor, lane);
                if (node == null) break;
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
            LootUpgrade = bossBeaten && ClassUpgrades.For(Sinner.Id).Contains(lootUpgrade) ? lootUpgrade : null;
            LootTaken = lootTaken;
            WhettedSoul = whetted;
        }

        /// <summary>What Demo 2.1's offers left behind, as a save kept it (the unknown nodes revealed as "floor:lane").</summary>
        public void RestoreOffers(int anteCut, int wardenPurse, int loan, bool loanPays, bool insured, bool bossMalice, bool skip, int memo,
            IEnumerable<string> offers, ISet<string> runEvents, IEnumerable<string> revealed)
        {
            AnteCutTables = Math.Max(0, anteCut);
            NextWardenPursePercent = wardenPurse > 0 ? wardenPurse : 100;
            LoanDue = Math.Max(0, loan);
            LoanPays = loanPays;
            Insured = insured;
            BossMaliceFull = bossMalice;
            SkipNext = skip;
            EventMemo = Math.Max(0, memo);
            EventOffers.Clear();
            EventOffers.AddRange((offers ?? Enumerable.Empty<string>()).Where(id => RelicRoster.Find(id) != null));
            if (runEvents != null) RunEventsSeen = runEvents;
            foreach (string code in revealed ?? Enumerable.Empty<string>())
            {
                string[] parts = code.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int f) && int.TryParse(parts[1], out int l)) Reveal(Map.Find(f, l));
            }
        }
    }
}
