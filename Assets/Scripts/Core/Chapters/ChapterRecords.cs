using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Phase 2's records, apart from the demo's book: runs, how they ended (free; damned by an empty purse, a burned soul, Lucifer's
    /// fall, or abandoned), how far they came (Lucifer reached), the deepest chapter, freedom per class and the fewest hands to it.
    /// "key=value" lines starting with "p2r=1"; an unreadable book starts empty.
    /// </summary>
    public sealed class ChapterRecords
    {
        public const int Version = 1;

        private readonly Dictionary<string, int> _freedByClass = new Dictionary<string, int>();

        public int Runs { get; private set; }
        public int Freed { get; private set; }
        public int PurseEmptied { get; private set; }
        public int SoulsLost { get; private set; }
        public int CastDown { get; private set; }
        public int Abandoned { get; private set; }
        public int LuciferReached { get; private set; }

        /// <summary>The deepest chapter any run reached (4: Lucifer's table); 0 for none.</summary>
        public int DeepestChapter { get; private set; }

        /// <summary>Fewest hands to walk free; null until the first freedom.</summary>
        public int? FastestFreedom { get; private set; }

        public IReadOnlyDictionary<string, int> FreedByClass => _freedByClass;

        public int Damned => PurseEmptied + SoulsLost + CastDown + Abandoned;

        public void RunStarted() => Runs++;

        /// <summary>A run ended: how, how far it came (chapter 1-3; 4 for Lucifer's table), its class and its hands.</summary>
        public void RunEnded(JourneyEnd end, int deepest, string classId, int hands)
        {
            DeepestChapter = Math.Max(DeepestChapter, deepest);
            if (deepest >= 4) LuciferReached++;
            switch (end)
            {
                case JourneyEnd.Freed:
                    Freed++;
                    if (!string.IsNullOrEmpty(classId)) _freedByClass[classId] = (_freedByClass.TryGetValue(classId, out int n) ? n : 0) + 1;
                    if (!FastestFreedom.HasValue || hands < FastestFreedom.Value) FastestFreedom = hands;
                    break;
                case JourneyEnd.PurseEmptied: PurseEmptied++; break;
                case JourneyEnd.Damned: SoulsLost++; break;
                case JourneyEnd.CastDown: CastDown++; break;
                case JourneyEnd.Abandoned: Abandoned++; break;
            }
        }

        public string Encode()
        {
            string I(int n) => n.ToString(CultureInfo.InvariantCulture);
            var lines = new List<string>
            {
                "p2r=" + Version, "runs=" + I(Runs), "freed=" + I(Freed), "purse=" + I(PurseEmptied), "soul=" + I(SoulsLost),
                "fell=" + I(CastDown), "abandoned=" + I(Abandoned), "lucifer=" + I(LuciferReached), "deepest=" + I(DeepestChapter),
                "fastest=" + (FastestFreedom.HasValue ? I(FastestFreedom.Value) : "")
            };
            lines.AddRange(_freedByClass.OrderBy(p => p.Key).Select(p => "freed." + p.Key + "=" + I(p.Value)));
            return string.Join("\n", lines);
        }

        public static ChapterRecords Decode(string text)
        {
            var book = new ChapterRecords();
            if (string.IsNullOrWhiteSpace(text)) return book;
            var values = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            if (!values.TryGetValue("p2r", out string version) || version != Version.ToString(CultureInfo.InvariantCulture)) return book;
            int Count(string key) =>
                values.TryGetValue(key, out string v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0 ? n : 0;
            book.Runs = Count("runs");
            book.Freed = Count("freed");
            book.PurseEmptied = Count("purse");
            book.SoulsLost = Count("soul");
            book.CastDown = Count("fell");
            book.Abandoned = Count("abandoned");
            book.LuciferReached = Count("lucifer");
            book.DeepestChapter = Math.Min(4, Count("deepest"));
            book.FastestFreedom = values.TryGetValue("fastest", out string f) && f.Length > 0 ? Count("fastest") : (int?)null;
            foreach (var pair in values.Where(p => p.Key.StartsWith("freed.", StringComparison.Ordinal)))
                book._freedByClass[pair.Key.Substring("freed.".Length)] = Count(pair.Key);
            return book;
        }
    }
}
