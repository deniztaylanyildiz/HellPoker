using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// Records across all runs: how many were started, how many ended free or damned, absolutions per demon and the
    /// fastest absolution (fewest hands). Saved as "key=value" lines starting with "v=1"; an unreadable book starts empty.
    /// </summary>
    public sealed class RecordBook
    {
        public const int Version = 1;

        private readonly Dictionary<string, int> _absolutionsByDealer = new Dictionary<string, int>();

        public int RunsStarted { get; private set; }
        public int Absolutions { get; private set; }
        public int Damnations { get; private set; }

        /// <summary>Fewest hands to walk free; null until the first absolution.</summary>
        public int? FastestAbsolution { get; private set; }

        public IReadOnlyDictionary<string, int> AbsolutionsByDealer => _absolutionsByDealer;

        /// <summary>Runs that were summoned to Lucifer at least once.</summary>
        public int LuciferReached { get; private set; }

        /// <summary>Runs that ended free at Lucifer's table.</summary>
        public int LuciferDefeated { get; private set; }

        /// <summary>Fewest summons it took to beat Lucifer; null until he is first beaten.</summary>
        public int? FewestLuciferAttempts { get; private set; }

        /// <summary>Runs set free by the Dead Man's Hand without ever meeting Lucifer.</summary>
        public int WildBillEscapes { get; private set; }

        /// <summary>Demons' cheats that turned on them and helped the player, over all runs.</summary>
        public int BackfiresSeen { get; private set; }

        public void NoteBackfires(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            BackfiresSeen += count;
        }

        public int AbsolutionsAt(string dealerId) => _absolutionsByDealer.TryGetValue(dealerId ?? "", out int count) ? count : 0;

        public void RunStarted() => RunsStarted++;

        /// <param name="dealerId">The demon the run is credited to: where it ended, or — at Lucifer's table — where the player came from.</param>
        /// <param name="luciferAttempts">How many times the run was summoned to Lucifer.</param>
        /// <param name="beatLucifer">True when the run ended free at Lucifer's table.</param>
        /// <param name="wildBill">True when the Dead Man's Hand set the run free without Lucifer.</param>
        public void RunEnded(bool absolved, string dealerId, int handsPlayed, int luciferAttempts = 0, bool beatLucifer = false,
            bool wildBill = false)
        {
            if (luciferAttempts > 0) LuciferReached++;

            if (!absolved)
            {
                Damnations++;
                return;
            }

            if (beatLucifer)
            {
                LuciferDefeated++;
                if (!FewestLuciferAttempts.HasValue || luciferAttempts < FewestLuciferAttempts.Value)
                    FewestLuciferAttempts = luciferAttempts;
            }
            else if (wildBill)
            {
                WildBillEscapes++;
            }

            Absolutions++;
            if (!string.IsNullOrEmpty(dealerId))
                _absolutionsByDealer[dealerId] = AbsolutionsAt(dealerId) + 1;
            if (!FastestAbsolution.HasValue || handsPlayed < FastestAbsolution.Value)
                FastestAbsolution = handsPlayed;
        }

        public string Encode()
        {
            var lines = new List<string>
            {
                "v=" + Version,
                "runs=" + RunsStarted.ToString(CultureInfo.InvariantCulture),
                "absolved=" + Absolutions.ToString(CultureInfo.InvariantCulture),
                "damned=" + Damnations.ToString(CultureInfo.InvariantCulture),
                "fastest=" + (FastestAbsolution.HasValue ? FastestAbsolution.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "lucifer.reached=" + LuciferReached.ToString(CultureInfo.InvariantCulture),
                "lucifer.defeated=" + LuciferDefeated.ToString(CultureInfo.InvariantCulture),
                "lucifer.fewest=" + (FewestLuciferAttempts.HasValue ? FewestLuciferAttempts.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "wildbill=" + WildBillEscapes.ToString(CultureInfo.InvariantCulture),
                "backfires=" + BackfiresSeen.ToString(CultureInfo.InvariantCulture)
            };
            lines.AddRange(_absolutionsByDealer.OrderBy(pair => pair.Key)
                .Select(pair => "free." + pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            return string.Join("\n", lines);
        }

        /// <summary>The saved book, or an empty one when there is none or it cannot be read.</summary>
        public static RecordBook Decode(string text)
        {
            var book = new RecordBook();
            if (string.IsNullOrWhiteSpace(text)) return book;

            try
            {
                Dictionary<string, string> values = KeyValues.Parse(text);
                if (!values.TryGetValue("v", out string version) || version != Version.ToString(CultureInfo.InvariantCulture))
                    return new RecordBook();

                book.RunsStarted = NonNegative(KeyValues.Int(values, "runs"));
                book.Absolutions = NonNegative(KeyValues.Int(values, "absolved"));
                book.Damnations = NonNegative(KeyValues.Int(values, "damned"));
                string fastest = values.TryGetValue("fastest", out string f) ? f : "";
                book.FastestAbsolution = fastest.Length > 0 ? NonNegative(KeyValues.Int(values, "fastest")) : (int?)null;
                // Lucifer's records came later: a book without them simply has none yet.
                book.LuciferReached = OptionalCount(values, "lucifer.reached");
                book.LuciferDefeated = OptionalCount(values, "lucifer.defeated");
                book.WildBillEscapes = OptionalCount(values, "wildbill");
                book.BackfiresSeen = OptionalCount(values, "backfires");
                string fewest = values.TryGetValue("lucifer.fewest", out string l) ? l : "";
                book.FewestLuciferAttempts = fewest.Length > 0 ? NonNegative(KeyValues.Int(values, "lucifer.fewest")) : (int?)null;
                foreach (var pair in values.Where(pair => pair.Key.StartsWith("free.", StringComparison.Ordinal)))
                    book._absolutionsByDealer[pair.Key.Substring("free.".Length)] = NonNegative(KeyValues.Int(values, pair.Key));
                return book;
            }
            catch (Exception exception) when (exception is FormatException || exception is KeyNotFoundException || exception is OverflowException
                                               || exception is ArgumentException)
            {
                return new RecordBook();
            }
        }

        private static int OptionalCount(Dictionary<string, string> values, string key)
        {
            return values.ContainsKey(key) ? NonNegative(KeyValues.Int(values, key)) : 0;
        }

        private static int NonNegative(int value)
        {
            if (value < 0) throw new ArgumentException("A count cannot be negative.");
            return value;
        }
    }
}
