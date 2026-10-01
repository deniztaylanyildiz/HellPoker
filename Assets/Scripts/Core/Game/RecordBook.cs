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

        public int AbsolutionsAt(string dealerId) => _absolutionsByDealer.TryGetValue(dealerId ?? "", out int count) ? count : 0;

        public void RunStarted() => RunsStarted++;

        /// <param name="dealerId">The demon at whose table the run ended.</param>
        public void RunEnded(bool absolved, string dealerId, int handsPlayed)
        {
            if (!absolved)
            {
                Damnations++;
                return;
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
                "fastest=" + (FastestAbsolution.HasValue ? FastestAbsolution.Value.ToString(CultureInfo.InvariantCulture) : "")
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

        private static int NonNegative(int value)
        {
            if (value < 0) throw new ArgumentException("A count cannot be negative.");
            return value;
        }
    }
}
