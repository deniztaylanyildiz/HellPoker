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
        public int Chapter = 1;
        public bool Lucifer;
        public int LuciferBarStart;
        public int Coins;
        public int Years;
        public int Owed;
        public int Charge;
        public bool Ward;
        public int Jokers;
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
        public List<Card> Deck = new List<Card>();
        public int Matches;
        public string WardenRelic;
        public int BossBarStart;
        public bool BossBeaten;
        public List<string> Loot = new List<string>();
        public bool LootTaken;
        public List<int> BossHands = new List<int>();
        public int FloorHands;

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
                "chapter=" + Chapter,
                "lucifer=" + B(Lucifer),
                "lucifer.start=" + LuciferBarStart,
                "coins=" + Coins,
                "years=" + Years,
                "owed=" + Owed,
                "charge=" + Charge,
                "ward=" + B(Ward),
                "jokers=" + Jokers,
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
                "deck=" + CardCodes.FormatAll(Deck),
                "matches=" + Matches,
                "warden.relic=" + (WardenRelic ?? ""),
                "boss.start=" + BossBarStart,
                "boss.beaten=" + B(BossBeaten),
                "loot=" + string.Join(",", Loot),
                "loot.taken=" + B(LootTaken),
                "boss.hands=" + string.Join(",", BossHands),
                "floor.hands=" + FloorHands,
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
                    Chapter = I("chapter", 1),
                    Lucifer = Bool("lucifer"),
                    LuciferBarStart = I("lucifer.start"),
                    Coins = I("coins"),
                    Years = I("years"),
                    Owed = I("owed"),
                    Charge = I("charge"),
                    Ward = Bool("ward"),
                    Jokers = I("jokers"),
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
                    Deck = (CardCodes.TryParseDeck(S("deck") ?? "") ?? Array.Empty<Card>()).ToList(),
                    Matches = I("matches"),
                    WardenRelic = S("warden.relic"),
                    BossBarStart = I("boss.start"),
                    BossBeaten = Bool("boss.beaten"),
                    Loot = Ids("loot"),
                    LootTaken = Bool("loot.taken"),
                    BossHands = Ints("boss.hands"),
                    FloorHands = I("floor.hands"),
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
