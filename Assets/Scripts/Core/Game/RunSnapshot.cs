using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A run as saved: the demon, the sentence, the hands played and the run's stats — and, while a hand is being played,
    /// that hand (stake, draw, soul, seal), so closing the game mid-hand cannot undo it. The soul needs no field of its own —
    /// it follows from the sentence and the demon's soul line. The deck and the cards are not saved: a resumed run gets a
    /// fresh shuffle, and a hand left behind is forfeited, never played on.
    /// Text format, one "key=value" per line, starting with "v=4". v=4 adds the sinner's class ("class", "class.charges";
    /// later also the events and the relics, each with optional keys); a v=1..3 save reads as a Peasant's run. The hand lines ("hand.*") are optional, so saves made
    /// between hands read as before. v=2 adds Lucifer ("lucifer" at his table, "origin", "attempts"); a v=1 save still reads,
    /// as a run that never met him. v=3 adds the demon's cheats: "malice" (the gauge), "cheat.major" (the big cheat spent at
    /// this table), "grudge" (optional: hands of faster malice after walking out on a cheat) and, in a hand, "hand.cheat" /
    /// "hand.cheat.done" (whether the player walked out on it); a v=2 (or v=1) save reads with an empty gauge. Keys no longer
    /// written ("hand.shown", "backfires") are ignored. Anything unreadable — a garbled file, another version, impossible
    /// numbers — decodes to nothing, so a bad save is simply ignored.
    /// </summary>
    public sealed class RunSnapshot
    {
        public const int Version = 4;

        /// <summary>The sinner class of the run (v=4; an older save is a Peasant's).</summary>
        public string ClassId { get; }

        /// <summary>What was left of the class's ability (v=4); null: full.</summary>
        public int? ClassCharges { get; }

        /// <summary>The run's events (v=4, optional): seen events, the cooldown, the next hand's modifier, deferred years, the
        /// sold soul. An empty state for a run without them.</summary>
        public RunEventState Events { get; }

        /// <summary>The demon's malice gauge at the table.</summary>
        public int Malice { get; }

        /// <summary>True once the demon's big cheat was spent at this table (Lucifer's Fall, once per attempt).</summary>
        public bool MajorCheatUsed { get; }

        /// <summary>Hands of faster malice left: the player walked out on a cheat.</summary>
        public int Grudge { get; }

        /// <summary>The oldest version still read.</summary>
        public const int OldestVersion = 1;

        /// <summary>True when the run sits at Lucifer's table (<see cref="DealerId"/> is his).</summary>
        public bool AtLucifer { get; }

        /// <summary>The demon the player was last summoned from; null if never summoned.</summary>
        public string OriginDealerId { get; }

        /// <summary>How many times the player has been summoned to Lucifer's table.</summary>
        public int LuciferAttempts { get; }

        public string DealerId { get; }

        /// <summary>The sentence; with a hand in progress, the sentence it was dealt at (nothing settled yet).</summary>
        public int Years { get; }

        /// <summary>Hands dealt so far, the one in progress included.</summary>
        public int RoundsPlayed { get; }

        public RunStats Stats { get; }

        /// <summary>The hand that was being played when the save was made; null between hands.</summary>
        public HandInProgress Hand { get; }

        public RunSnapshot(string dealerId, int years, int roundsPlayed, RunStats stats, HandInProgress hand = null,
            bool atLucifer = false, string originDealerId = null, int luciferAttempts = 0, int malice = 0, bool majorCheatUsed = false, int grudge = 0,
            string classId = null, int? classCharges = null, RunEventState events = null)
        {
            Events = events ?? RunEventState.Empty;
            if (classCharges.HasValue && classCharges.Value < 0) throw new ArgumentOutOfRangeException(nameof(classCharges));
            ClassId = string.IsNullOrEmpty(classId) ? Sinners.Peasant.ClassId : classId;
            ClassCharges = classCharges;
            if (grudge < 0) throw new ArgumentOutOfRangeException(nameof(grudge));
            Grudge = grudge;
            if (malice < 0) throw new ArgumentOutOfRangeException(nameof(malice));
            Malice = malice;
            MajorCheatUsed = majorCheatUsed;
            if (string.IsNullOrEmpty(dealerId)) throw new ArgumentException("A dealer id is needed.", nameof(dealerId));
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (roundsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed));
            if (hand != null && roundsPlayed == 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed), "A hand in progress was dealt.");
            if (luciferAttempts < 0) throw new ArgumentOutOfRangeException(nameof(luciferAttempts));
            if (atLucifer && (luciferAttempts == 0 || string.IsNullOrEmpty(originDealerId)))
                throw new ArgumentException("A player at Lucifer's table was summoned from somewhere.", nameof(originDealerId));
            DealerId = dealerId;
            Years = years;
            RoundsPlayed = roundsPlayed;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Hand = hand;
            AtLucifer = atLucifer;
            OriginDealerId = string.IsNullOrEmpty(originDealerId) ? null : originDealerId;
            LuciferAttempts = luciferAttempts;
        }

        public string Encode()
        {
            var lines = new List<string>
            {
                "v=" + Version,
                "dealer=" + DealerId,
                "years=" + Years.ToString(CultureInfo.InvariantCulture),
                "rounds=" + RoundsPlayed.ToString(CultureInfo.InvariantCulture),
                "hands=" + Stats.HandsPlayed.ToString(CultureInfo.InvariantCulture),
                "lowest=" + Stats.LowestYears.ToString(CultureInfo.InvariantCulture),
                "highest=" + Stats.HighestYears.ToString(CultureInfo.InvariantCulture),
                "best=" + (Stats.BestHand.HasValue ? ((int)Stats.BestHand.Value).ToString(CultureInfo.InvariantCulture) : ""),
                "dealers=" + string.Join(",", Stats.Dealers),
                "soul=" + (Stats.SoulStaked ? "1" : "0"),
                "lucifer=" + Flag(AtLucifer),
                "origin=" + (OriginDealerId ?? ""),
                "attempts=" + LuciferAttempts.ToString(CultureInfo.InvariantCulture),
                "malice=" + Malice.ToString(CultureInfo.InvariantCulture),
                "cheat.major=" + Flag(MajorCheatUsed),
                "grudge=" + Grudge.ToString(CultureInfo.InvariantCulture),
                "class=" + ClassId
            };
            if (ClassCharges.HasValue)
                lines.Add("class.charges=" + ClassCharges.Value.ToString(CultureInfo.InvariantCulture));
            Events.Encode(lines);
            if (Hand != null)
            {
                lines.Add("hand.stake=" + Hand.Stake.ToString(CultureInfo.InvariantCulture));
                lines.Add("hand.ante=" + Hand.Ante.ToString(CultureInfo.InvariantCulture));
                lines.Add("hand.drawn=" + Flag(Hand.IsAfterDraw));
                lines.Add("hand.soul=" + Flag(Hand.IsSoulHand));
                lines.Add("hand.sealed=" + Flag(Hand.IsSealed));
                if (Hand.CheatId != null)
                {
                    lines.Add("hand.cheat=" + Hand.CheatId);
                    lines.Add("hand.cheat.done=" + Flag(Hand.CheatResolved));
                }
            }
            return string.Join("\n", lines);
        }

        private static string Flag(bool value) => value ? "1" : "0";

        /// <returns>Null when the save has no hand lines; throws <see cref="FormatException"/> when they are broken.</returns>
        private static HandInProgress DecodeHand(Dictionary<string, string> values)
        {
            if (!values.ContainsKey("hand.stake")) return null;
            bool cheat = values.TryGetValue("hand.cheat", out string cheatId) && cheatId.Length > 0;
            return new HandInProgress(KeyValues.Int(values, "hand.stake"), KeyValues.Int(values, "hand.ante"),
                KeyValues.Flag(values, "hand.drawn"), KeyValues.Flag(values, "hand.soul"), KeyValues.Flag(values, "hand.sealed"),
                cheat ? cheatId : null, cheat && KeyValues.Flag(values, "hand.cheat.done"));
        }

        /// <returns>False (and null) for anything that is not a readable save of this version.</returns>
        public static bool TryDecode(string text, out RunSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                Dictionary<string, string> values = KeyValues.Parse(text);
                if (!values.TryGetValue("v", out string versionText)
                    || !int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out int version)
                    || version < OldestVersion || version > Version)
                    return false;

                string dealer = values.TryGetValue("dealer", out string id) ? id : null;
                if (string.IsNullOrWhiteSpace(dealer)) return false;

                HandCategory? best = null;
                string bestText = values.TryGetValue("best", out string b) ? b : "";
                if (bestText.Length > 0)
                {
                    int bestValue = KeyValues.Int(values, "best");
                    if (!Enum.IsDefined(typeof(HandCategory), bestValue)) return false;
                    best = (HandCategory)bestValue;
                }

                string[] dealers = (values.TryGetValue("dealers", out string list) ? list : "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var stats = new RunStats(KeyValues.Int(values, "hands"), KeyValues.Int(values, "lowest"), KeyValues.Int(values, "highest"),
                    best, dealers.Length > 0 ? dealers : new[] { dealer }, values.TryGetValue("soul", out string soul) && soul == "1");

                // v=1 knew nothing of Lucifer: such a run never met him.
                bool atLucifer = version >= 2 && KeyValues.Flag(values, "lucifer");
                string origin = version >= 2 && values.TryGetValue("origin", out string o) ? o : null;
                int attempts = version >= 2 ? KeyValues.Int(values, "attempts") : 0;

                // v=1 and v=2 knew nothing of the cheats: the gauge starts empty.
                int malice = version >= 3 ? KeyValues.Int(values, "malice") : 0;
                bool majorUsed = version >= 3 && KeyValues.Flag(values, "cheat.major");
                // "grudge" came later within v=3: a save without it holds none.
                int grudge = values.ContainsKey("grudge") ? KeyValues.Int(values, "grudge") : 0;

                // v=4 knows the sinner's class; an older run was a Peasant's, with the ability untouched.
                string classId = version >= 4 && values.TryGetValue("class", out string c) && c.Length > 0 ? c : null;
                int? charges = version >= 4 && values.ContainsKey("class.charges") ? KeyValues.Int(values, "class.charges") : (int?)null;

                snapshot = new RunSnapshot(dealer, KeyValues.Int(values, "years"), KeyValues.Int(values, "rounds"), stats, DecodeHand(values),
                    atLucifer, origin, attempts, malice, majorUsed, grudge, classId, charges, RunEventState.Decode(values));
                return true;
            }
            catch (Exception exception) when (exception is FormatException || exception is ArgumentException || exception is KeyNotFoundException
                                               || exception is OverflowException)
            {
                snapshot = null;
                return false;
            }
        }
    }

    /// <summary>The "key=value" lines saves and records are written in.</summary>
    internal static class KeyValues
    {
        public static Dictionary<string, string> Parse(string text)
        {
            var values = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                int equals = line.IndexOf('=');
                if (equals <= 0) throw new FormatException("Not a key=value line: " + line);
                values[line.Substring(0, equals)] = line.Substring(equals + 1);
            }
            return values;
        }

        public static int Int(Dictionary<string, string> values, string key)
        {
            return int.Parse(values[key], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        /// <summary>"1" or "0"; anything else is a broken save.</summary>
        public static bool Flag(Dictionary<string, string> values, string key)
        {
            switch (values[key])
            {
                case "1": return true;
                case "0": return false;
                default: throw new FormatException($"{key} is not 0 or 1.");
            }
        }
    }
}

namespace HellPoker.Core.Game
{
    /// <summary>
    /// The run's events as saved (v=4, every key optional): "event.seen" (ids), "event.since" (hands since the last one),
    /// "effect.next" (the next hand's modifier), "effect.deferred" / "effect.deferred.hands" (Mammon's ledger), "soul.sold",
    /// "relics" (the cursed relics carried).
    /// An event left on screen when the game closed is already seen: it counts as passed.
    /// </summary>
    public sealed class RunEventState
    {
        public static readonly RunEventState Empty = new RunEventState(null, 0, null, 0, 0, 0);

        /// <summary>The cursed relics the run carries ("relics", optional).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Relics { get; }

        public System.Collections.Generic.IReadOnlyList<string> Seen { get; }
        public int HandsSince { get; }
        public Events.HandModifier Next { get; }
        public int DeferredYears { get; }
        public int DeferredHands { get; }
        public int SoulSold { get; }

        public RunEventState(System.Collections.Generic.IEnumerable<string> seen, int handsSince, Events.HandModifier next, int deferredYears,
            int deferredHands, int soulSold, System.Collections.Generic.IEnumerable<string> relics = null)
        {
            Relics = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(relics ?? new string[0], s => !string.IsNullOrEmpty(s)));
            if (handsSince < 0 || deferredYears < 0 || deferredHands < 0 || soulSold < 0)
                throw new System.ArgumentOutOfRangeException(nameof(handsSince));
            Seen = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(seen ?? new string[0], s => !string.IsNullOrEmpty(s)));
            HandsSince = handsSince;
            Next = next ?? Events.HandModifier.None;
            DeferredYears = deferredYears;
            DeferredHands = deferredHands;
            SoulSold = soulSold;
        }

        internal void Encode(System.Collections.Generic.List<string> lines)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            if (Seen.Count > 0) lines.Add("event.seen=" + string.Join(",", Seen));
            if (HandsSince > 0) lines.Add("event.since=" + HandsSince.ToString(c));
            if (!Next.IsNone) lines.Add("effect.next=" + Next.Encode());
            if (DeferredYears > 0)
            {
                lines.Add("effect.deferred=" + DeferredYears.ToString(c));
                lines.Add("effect.deferred.hands=" + DeferredHands.ToString(c));
            }
            if (SoulSold > 0) lines.Add("soul.sold=" + SoulSold.ToString(c));
            if (Relics.Count > 0) lines.Add("relics=" + string.Join(",", Relics));
        }

        internal static RunEventState Decode(System.Collections.Generic.Dictionary<string, string> values)
        {
            int Optional(string key) => values.ContainsKey(key) ? KeyValues.Int(values, key) : 0;
            string seen = values.TryGetValue("event.seen", out string s) ? s : "";
            string next = values.TryGetValue("effect.next", out string n) ? n : "";
            return new RunEventState(seen.Split(','), Optional("event.since"), Events.HandModifier.Decode(next), Optional("effect.deferred"),
                Optional("effect.deferred.hands"), Optional("soul.sold"), (values.TryGetValue("relics", out string r) ? r : "").Split(','));
        }
    }
}
