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

        private readonly Dictionary<string, int> _circleByClass = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _beatenByBoss = new Dictionary<string, int>();

        /// <summary>The deepest circle each class walked free in (absent: never free).</summary>
        public IReadOnlyDictionary<string, int> CircleByClass => _circleByClass;

        /// <summary>How often each demon was beaten (the milestones' count).</summary>
        public IReadOnlyDictionary<string, int> BeatenByBoss => _beatenByBoss;

        private readonly Dictionary<string, int> _lowestSentence = new Dictionary<string, int>();

        /// <summary>The fewest years left when a run of <paramref name="classId"/> walked free in <paramref name="circle"/> (its score); null
        /// for never.</summary>
        public int? LowestSentence(string classId, int circle) =>
            classId != null && _lowestSentence.TryGetValue(SentenceKey(classId, circle), out int years) ? years : (int?)null;

        private static string SentenceKey(string classId, int circle) => classId + "." + Circles.Clamp(circle).ToString(CultureInfo.InvariantCulture);

        /// <summary>The milestones' counts: wardens beaten, relics bought at the black market, tributes paid in full.</summary>
        public int WardensBeaten { get; private set; }
        public int RelicsBought { get; private set; }
        public int TributesPaid { get; private set; }

        /// <summary>The most wardens one run beat in one chapter (the Devil's Compass opens at three).</summary>
        public int MostWardensInAChapter { get; private set; }

        /// <summary>A warden beaten; <paramref name="inThisChapter"/>: how many this run has beaten in the chapter now (0: not counted).</summary>
        public void WardenBeaten(int inThisChapter = 0)
        {
            WardensBeaten++;
            MostWardensInAChapter = Math.Max(MostWardensInAChapter, inThisChapter);
        }
        public void RelicBought() => RelicsBought++;
        public void TributePaidInFull() => TributesPaid++;

        public void BossBeaten(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return;
            _beatenByBoss[bossId] = (_beatenByBoss.TryGetValue(bossId, out int n) ? n : 0) + 1;
        }

        public int Beaten(string bossId) => bossId != null && _beatenByBoss.TryGetValue(bossId, out int n) ? n : 0;

        /// <summary>The deepest circle the class walked free in; -1 for never.</summary>
        public int BestCircle(string classId) =>
            classId == null ? -1 : _circleByClass.TryGetValue(classId, out int c) ? c : _freedByClass.ContainsKey(classId) ? 0 : -1;   // an older book: free in the first

        /// <summary>The deepest circle the class may play: one past the deepest it walked free in (0 until its first freedom).</summary>
        public int CirclesOpen(string classId) => Math.Min(Circles.Deepest, BestCircle(classId) + 1);

        public int Damned => PurseEmptied + SoulsLost + CastDown + Abandoned;

        public void RunStarted() => Runs++;

        /// <summary>A run ended: how, how far it came (chapter 1-3; 4 for Lucifer's table), its class and its hands.</summary>
        /// <param name="circle">The circle the run was played in: a freedom there opens the next for its class.</param>
        /// <param name="sentence">The years left of the run's sentence (a freedom's score); -1 for unknown.</param>
        public void RunEnded(JourneyEnd end, int deepest, string classId, int hands, int circle = 0, int sentence = -1)
        {
            DeepestChapter = Math.Max(DeepestChapter, deepest);
            if (deepest >= 4) LuciferReached++;
            switch (end)
            {
                case JourneyEnd.Freed:
                    Freed++;
                    if (!string.IsNullOrEmpty(classId)) _freedByClass[classId] = (_freedByClass.TryGetValue(classId, out int n) ? n : 0) + 1;
                    if (!FastestFreedom.HasValue || hands < FastestFreedom.Value) FastestFreedom = hands;
                    if (!string.IsNullOrEmpty(classId)) _circleByClass[classId] = Math.Max(BestCircle(classId), Circles.Clamp(circle));
                    if (!string.IsNullOrEmpty(classId) && sentence >= 0)
                    {
                        int? lowest = LowestSentence(classId, circle);
                        if (!lowest.HasValue || sentence < lowest.Value) _lowestSentence[SentenceKey(classId, circle)] = sentence;
                    }
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
            lines.AddRange(_circleByClass.OrderBy(p => p.Key).Select(p => "circle." + p.Key + "=" + I(p.Value)));
            lines.AddRange(_beatenByBoss.OrderBy(p => p.Key).Select(p => "beaten." + p.Key + "=" + I(p.Value)));
            lines.AddRange(_lowestSentence.OrderBy(p => p.Key).Select(p => "sentence." + p.Key + "=" + I(p.Value)));
            lines.Add("wardens=" + I(WardensBeaten));
            lines.Add("wardens.chapter=" + I(MostWardensInAChapter));
            lines.Add("bought=" + I(RelicsBought));
            lines.Add("tributes=" + I(TributesPaid));
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
            foreach (var pair in values.Where(p => p.Key.StartsWith("circle.", StringComparison.Ordinal)))
                book._circleByClass[pair.Key.Substring("circle.".Length)] = Circles.Clamp(Count(pair.Key));
            foreach (var pair in values.Where(p => p.Key.StartsWith("beaten.", StringComparison.Ordinal)))
                book._beatenByBoss[pair.Key.Substring("beaten.".Length)] = Count(pair.Key);
            foreach (var pair in values.Where(p => p.Key.StartsWith("sentence.", StringComparison.Ordinal)))
                book._lowestSentence[pair.Key.Substring("sentence.".Length)] = Count(pair.Key);
            book.WardensBeaten = Count("wardens");
            book.MostWardensInAChapter = Count("wardens.chapter");
            book.RelicsBought = Count("bought");
            book.TributesPaid = Count("tributes");
            return book;
        }
    }
}
