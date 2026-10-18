using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A Phase 2 run as saved, apart from the demo's (its own key, its own version, <see cref="Version"/>): "key=value" lines. The run's
    /// parts (<see cref="ChapterJourney.Capture"/>) and where the player is: the node's state (done or not, a panel waiting), a floor match
    /// (the imp's purse, the hands, its marks, the deck), a demon's table (the bar, the hands, the malice), and a hand left in the middle
    /// (lost on the next launch). A save that cannot be read is thrown away.
    /// </summary>
    public sealed class ChapterSave
    {
        public const int Version = 1;

        public string ClassId;
        public int Seed;

        /// <summary>The circle of Hell (optional key; an older save: 0).</summary>
        public int Circle;
        public int Chapter = 1;

        /// <summary>The chapter map's generator (<see cref="ChapterMap.Version"/>; optional key: 1 for a save from before Demo 2.1).</summary>
        public int MapVersion = ChapterMap.CurrentVersion;
        public bool Lucifer;
        public int LuciferBarStart;
        public int Coins;
        public int Years;
        public int Owed;

        /// <summary>The run's sentence (<see cref="RunSentence"/>; optional key: -1 for a save from before it was kept).</summary>
        public int Sentence = -1;
        public int Charge;
        public bool Ward;
        public int Jokers;

        /// <summary>Optional keys: the Jester's jackpots and the rewards taken for them (Demo 2.1).</summary>
        public int Jackpots;
        public int JackpotRewards;
        public List<string> Relics = new List<string>();
        public string Silenced;
        public string Amplified;
        public int RedrawsLeft = -1;
        public string RedrawTables;
        public List<int> Trail = new List<int>();
        public string Marks;
        public bool Breaker;
        public bool Rested;
        public List<string> EventsSeen = new List<string>();
        public int Matches;
        public string WardenRelic;
        public int BossBarStart;
        public bool BossBeaten;
        public List<string> Loot = new List<string>();
        public bool LootTaken;
        public List<int> BossHands = new List<int>();
        public int FloorHands;

        /// <summary>The class's Phase 2 kit (optional keys: an older save reads as an empty satchel and no taxes): the Peasant's satchel,
        /// the King's taxes, Lucifer's mirror (his one-card satchel, his taxes).</summary>
        public List<Card> Satchel = new List<Card>();
        public int Taxes;
        public Card? LuciferBag;
        public int LuciferTaxes;

        /// <summary>Optional keys of the class upgrades: the ones the run has, a beaten demon's offered one, a soul whetted by the fire.</summary>
        public List<string> Upgrades = new List<string>();
        public string LootUpgrade;
        public bool Whetted;

        /// <summary>Optional keys of what Demo 2.1's offers left: cheaper tables, a richer warden, a loan, the insurance, the confession, the
        /// ferry, a number kept, a swap's offers, the offers seen this run, the unknown nodes revealed ("floor:lane").</summary>
        public int AnteCut;
        public int WardenPurse = 100;
        public int Loan;
        public bool LoanPays;
        public bool Insured;
        public bool BossMalice;
        public bool Skip;
        public int Memo;
        public List<string> Offers = new List<string>();
        public List<string> RunEvents = new List<string>();
        public List<string> Revealed = new List<string>();

        /// <summary>Optional keys of Demo 2.1's bag and camps: the relics in the bag, the forged ones, the camps' sum on the next demon's
        /// bar (null: a save from before it — the old rest / whet flags stand).</summary>
        public List<string> Bag = new List<string>();
        public List<string> Forged = new List<string>();
        public int? CampBar;

        /// <summary>Optional: the purse as the run came to the gate (Mammon's Gold Tooth counts it; -1: none, or a save from before it).</summary>
        public int GateCoins = -1;

        /// <summary>Optional keys of Demo 2.1's markets: the vendors met this chapter, the one of the market stood at (-1: none), the relics
        /// cleansed for good.</summary>
        public List<int> Vendors = new List<int>();
        public int Vendor = -1;
        public List<string> Cleansed = new List<string>();

        /// <summary>Lucifer's Feather has spared its one hand (optional key).</summary>
        public bool FeatherSpent;

        /// <summary>Optional keys (2.1.1): the Devil's Compass's steps taken this chapter, the floors it carried the player across; the
        /// wardens beaten this chapter.</summary>
        public int CompassUsed;
        public List<int> CompassJumps = new List<int>();
        public int WardensHere;

        // ------------------------------------------------------------------ where the player is

        /// <summary>The current node is done (its panel answered, its match over).</summary>
        public bool NodeDone;

        /// <summary>The panel that waits for an answer: "event:&lt;id&gt;", "market", "fire", "gate", "loot", "warden", "chapter"... null: none.</summary>
        public string Pending;

        /// <summary>A match at the table: "imp", "warden", "gamble", "boss", "lucifer"; null: none.</summary>
        public string Match;

        public int MatchHouse = -1;
        public int MatchHands;
        public string MatchMarks;
        public int GambleStake;

        public int BossBar;
        public int Malice;
        public bool MajorUsed;

        /// <summary>A hand left in the middle: its stake (0: none), ante, sealed, after the draw, played with the soul on the table.</summary>
        public int HandStake;
        public int HandAnte;
        public bool HandSealed;
        public bool HandAfterDraw;
        public bool HandSoul;

        public string Encode()
        {
            var lines = new List<string>
            {
                "p2=" + Version,
                "class=" + ClassId,
                "seed=" + Seed.ToString(CultureInfo.InvariantCulture),
                "circle=" + Circle,
                "chapter=" + Chapter,
                "map=" + MapVersion,
                "lucifer=" + B(Lucifer),
                "lucifer.start=" + LuciferBarStart,
                "coins=" + Coins,
                "years=" + Years,
                "owed=" + Owed,
                "sentence=" + Sentence,
                "charge=" + Charge,
                "ward=" + B(Ward),
                "jokers=" + Jokers,
                "jokers.jackpots=" + Jackpots,
                "jokers.rewards=" + JackpotRewards,
                "relics=" + string.Join(",", Relics),
                "silenced=" + (Silenced ?? ""),
                "amplified=" + (Amplified ?? ""),
                "redraws=" + RedrawsLeft,
                "redraws.tables=" + (RedrawTables ?? ""),
                "trail=" + string.Join(",", Trail),
                "marks=" + (Marks ?? ""),
                "breaker=" + B(Breaker),
                "rested=" + B(Rested),
                "events=" + string.Join(",", EventsSeen),
                "matches=" + Matches,
                "warden.relic=" + (WardenRelic ?? ""),
                "boss.start=" + BossBarStart,
                "boss.beaten=" + B(BossBeaten),
                "loot=" + string.Join(",", Loot),
                "loot.taken=" + B(LootTaken),
                "boss.hands=" + string.Join(",", BossHands),
                "floor.hands=" + FloorHands,
                "kit.satchel=" + CardCodes.FormatAll(Satchel),
                "kit.taxes=" + Taxes,
                "kit.lucifer.bag=" + (LuciferBag.HasValue ? CardCodes.Format(LuciferBag.Value) : ""),
                "kit.lucifer.taxes=" + LuciferTaxes,
                "kit.upgrades=" + string.Join(",", Upgrades),
                "loot.upgrade=" + (LootUpgrade ?? ""),
                "whet=" + B(Whetted),
                "ev.antecut=" + AnteCut,
                "ev.warden=" + WardenPurse,
                "ev.loan=" + Loan,
                "ev.loanpays=" + B(LoanPays),
                "ev.insured=" + B(Insured),
                "ev.malice=" + B(BossMalice),
                "ev.skip=" + B(Skip),
                "ev.memo=" + Memo,
                "ev.offers=" + string.Join(",", Offers),
                "events.run=" + string.Join(",", RunEvents),
                "revealed=" + string.Join(",", Revealed),
                "bag=" + string.Join(",", Bag),
                "forged=" + string.Join(",", Forged),
                "camp.bar=" + (CampBar.HasValue ? CampBar.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "gate.coins=" + GateCoins.ToString(CultureInfo.InvariantCulture),
                "vendors=" + string.Join(",", Vendors),
                "vendor=" + Vendor,
                "cleansed=" + string.Join(",", Cleansed),
                "feather.spent=" + B(FeatherSpent),
                "compass.used=" + CompassUsed,
                "compass.jumps=" + string.Join(",", CompassJumps),
                "wardens.here=" + WardensHere,
                "node.done=" + B(NodeDone),
                "pending=" + (Pending ?? ""),
                "match=" + (Match ?? ""),
                "match.house=" + MatchHouse,
                "match.hands=" + MatchHands,
                "match.marks=" + (MatchMarks ?? ""),
                "gamble.stake=" + GambleStake,
                "boss.bar=" + BossBar,
                "malice=" + Malice,
                "major=" + B(MajorUsed),
                "hand.stake=" + HandStake,
                "hand.ante=" + HandAnte,
                "hand.sealed=" + B(HandSealed),
                "hand.drawn=" + B(HandAfterDraw),
                "hand.soul=" + B(HandSoul)
            };
            return string.Join("\n", lines);
        }

        private static string B(bool value) => value ? "1" : "0";

        /// <summary>Reads a save; false for anything garbled or of another version (the caller throws it away).</summary>
        public static bool TryDecode(string text, out ChapterSave save)
        {
            save = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var values = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            if (!values.TryGetValue("p2", out string version) || version != Version.ToString(CultureInfo.InvariantCulture)) return false;
            try
            {
                string S(string key) => values.TryGetValue(key, out string v) && v.Length > 0 ? v : null;
                int I(string key, int fallback = 0) =>
                    values.TryGetValue(key, out string v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : fallback;
                bool Bool(string key) => S(key) == "1";
                List<string> Ids(string key) => (S(key) ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                List<int> Ints(string key) => Ids(key).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();

                save = new ChapterSave
                {
                    ClassId = S("class"),
                    Seed = I("seed"),
                    Circle = Circles.Clamp(I("circle")),
                    Chapter = I("chapter", 1),
                    MapVersion = Math.Max(1, I("map", 1)),
                    Lucifer = Bool("lucifer"),
                    LuciferBarStart = I("lucifer.start"),
                    Coins = I("coins"),
                    Years = I("years"),
                    Owed = I("owed"),
                    Sentence = Math.Max(-1, I("sentence", -1)),
                    Charge = I("charge"),
                    Ward = Bool("ward"),
                    Jokers = I("jokers"),
                    Jackpots = I("jokers.jackpots"),
                    JackpotRewards = I("jokers.rewards"),
                    Relics = Ids("relics"),
                    Silenced = S("silenced"),
                    Amplified = S("amplified"),
                    RedrawsLeft = I("redraws", -1),
                    RedrawTables = S("redraws.tables"),
                    Trail = Ints("trail"),
                    Marks = S("marks"),
                    Breaker = Bool("breaker"),
                    Rested = Bool("rested"),
                    EventsSeen = Ids("events"),
                    // Phase 3: "deck" is no longer written (every hand at a chapter's table is a fresh deck); an old save's is ignored.
                    Matches = I("matches"),
                    WardenRelic = S("warden.relic"),
                    BossBarStart = I("boss.start"),
                    BossBeaten = Bool("boss.beaten"),
                    Loot = Ids("loot"),
                    LootTaken = Bool("loot.taken"),
                    BossHands = Ints("boss.hands"),
                    FloorHands = I("floor.hands"),
                    Satchel = (CardCodes.TryParseDeck(S("kit.satchel") ?? "") ?? Array.Empty<Card>()).ToList(),
                    Taxes = I("kit.taxes"),
                    LuciferBag = CardCodes.TryParse(S("kit.lucifer.bag") ?? "", out Card bag) ? bag : (Card?)null,
                    LuciferTaxes = I("kit.lucifer.taxes"),
                    Upgrades = Ids("kit.upgrades").Where(ClassUpgrades.IsKnown).Distinct().ToList(),
                    LootUpgrade = ClassUpgrades.IsKnown(S("loot.upgrade")) ? S("loot.upgrade") : null,
                    Whetted = Bool("whet"),
                    AnteCut = I("ev.antecut"),
                    WardenPurse = I("ev.warden", 100),
                    Loan = I("ev.loan"),
                    LoanPays = Bool("ev.loanpays"),
                    Insured = Bool("ev.insured"),
                    BossMalice = Bool("ev.malice"),
                    Skip = Bool("ev.skip"),
                    Memo = I("ev.memo"),
                    Offers = Ids("ev.offers"),
                    RunEvents = Ids("events.run"),
                    Revealed = Ids("revealed"),
                    Bag = Ids("bag"),
                    Forged = Ids("forged"),
                    CampBar = S("camp.bar") != null ? I("camp.bar") : (int?)null,
                    GateCoins = I("gate.coins", -1),
                    Vendors = Ints("vendors"),
                    Vendor = I("vendor", -1),
                    Cleansed = Ids("cleansed"),
                    FeatherSpent = Bool("feather.spent"),
                    CompassUsed = Math.Max(0, I("compass.used")),
                    CompassJumps = Ints("compass.jumps"),
                    WardensHere = Math.Max(0, I("wardens.here")),
                    NodeDone = Bool("node.done"),
                    Pending = S("pending"),
                    Match = S("match"),
                    MatchHouse = I("match.house", -1),
                    MatchHands = I("match.hands"),
                    MatchMarks = S("match.marks"),
                    GambleStake = I("gamble.stake"),
                    BossBar = I("boss.bar"),
                    Malice = I("malice"),
                    MajorUsed = Bool("major"),
                    HandStake = I("hand.stake"),
                    HandAnte = I("hand.ante"),
                    HandSealed = Bool("hand.sealed"),
                    HandAfterDraw = Bool("hand.drawn"),
                    HandSoul = Bool("hand.soul")
                };
            }
            catch (FormatException)
            {
                save = null;
                return false;
            }
            if (save.ClassId == null || Sinners.SinnerRoster.Find(save.ClassId) == null || save.Coins < 0 || save.Years < 0)
            {
                save = null;
                return false;
            }
            return true;
        }
    }
}
