using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A run between hands, as saved: the demon, the sentence, the hands played and the run's stats. The soul needs no
    /// field of its own — it follows from the sentence and the demon's soul line. The deck is not saved: a resumed run
    /// gets a fresh shuffle.
    /// Text format, one "key=value" per line, starting with "v=1". Anything unreadable — a garbled file, another version,
    /// impossible numbers — decodes to nothing, so a bad save is simply ignored.
    /// </summary>
    public sealed class RunSnapshot
    {
        public const int Version = 1;

        public string DealerId { get; }
        public int Years { get; }
        public int RoundsPlayed { get; }
        public RunStats Stats { get; }

        public RunSnapshot(string dealerId, int years, int roundsPlayed, RunStats stats)
        {
            if (string.IsNullOrEmpty(dealerId)) throw new ArgumentException("A dealer id is needed.", nameof(dealerId));
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (roundsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(roundsPlayed));
            DealerId = dealerId;
            Years = years;
            RoundsPlayed = roundsPlayed;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
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
            return string.Join("\n", lines);
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

                snapshot = new RunSnapshot(dealer, KeyValues.Int(values, "years"), KeyValues.Int(values, "rounds"), stats);
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
    }
}
