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
    /// Text format, one "key=value" per line, starting with "v=1". The hand lines ("hand.*") are optional, so saves made
    /// between hands read as before. Anything unreadable — a garbled file, another version, impossible numbers — decodes
    /// to nothing, so a bad save is simply ignored.
    /// </summary>
    public sealed class RunSnapshot
    {
        public const int Version = 1;

        public string DealerId { get; }

        /// <summary>The sentence; with a hand in progress, the sentence it was dealt at (nothing settled yet).</summary>
        public int Years { get; }

        /// <summary>Hands dealt so far, the one in progress included.</summary>
        public int RoundsPlayed { get; }

        public RunStats Stats { get; }

        /// <summary>The hand that was being played when the save was made; null between hands.</summary>
        public HandInProgress Hand { get; }

        public RunSnapshot(string dealerId, int years, int roundsPlayed, RunStats stats, HandInProgress hand = null)
        {
            if (string.IsNullOrEmpty(dealerId)) throw new ArgumentException("A dealer id is needed.", nameof(dealerId));
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (roundsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed));
            if (hand != null && roundsPlayed == 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed), "A hand in progress was dealt.");
            DealerId = dealerId;
            Years = years;
            RoundsPlayed = roundsPlayed;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Hand = hand;
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
                "soul=" + (Stats.SoulStaked ? "1" : "0")
            };
            if (Hand != null)
            {
                lines.Add("hand.stake=" + Hand.Stake.ToString(CultureInfo.InvariantCulture));
                lines.Add("hand.ante=" + Hand.Ante.ToString(CultureInfo.InvariantCulture));
                lines.Add("hand.drawn=" + Flag(Hand.IsAfterDraw));
                lines.Add("hand.soul=" + Flag(Hand.IsSoulHand));
                lines.Add("hand.sealed=" + Flag(Hand.IsSealed));
            }
            return string.Join("\n", lines);
        }

        private static string Flag(bool value) => value ? "1" : "0";

        /// <returns>Null when the save has no hand lines; throws <see cref="FormatException"/> when they are broken.</returns>
        private static HandInProgress DecodeHand(Dictionary<string, string> values)
        {
            if (!values.ContainsKey("hand.stake")) return null;
            return new HandInProgress(KeyValues.Int(values, "hand.stake"), KeyValues.Int(values, "hand.ante"),
                KeyValues.Flag(values, "hand.drawn"), KeyValues.Flag(values, "hand.soul"), KeyValues.Flag(values, "hand.sealed"));
        }

        /// <returns>False (and null) for anything that is not a readable save of this version.</returns>
        public static bool TryDecode(string text, out RunSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                Dictionary<string, string> values = KeyValues.Parse(text);
                if (!values.TryGetValue("v", out string version) || version != Version.ToString(CultureInfo.InvariantCulture))
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

                snapshot = new RunSnapshot(dealer, KeyValues.Int(values, "years"), KeyValues.Int(values, "rounds"), stats, DecodeHand(values));
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
