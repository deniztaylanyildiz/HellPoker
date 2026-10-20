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

        private readonly Dictionary<string, int> _beatenByBoss = new Dictionary<string, int>();

        /// <summary>Demo 3.4: how often each class walked free at each difficulty ("&lt;class&gt;.&lt;difficulty&gt;").</summary>
        private readonly Dictionary<string, int> _freedAt = new Dictionary<string, int>();

        /// <summary>Demo 3.4: the hardest level any class walked free at; null for never (Hard opens with a Medium freedom, Ironman with a
        /// Hard one: <see cref="IsDifficultyOpen"/>).</summary>
        public Difficulty? HardestFreed { get; private set; }

        /// <summary>How often each demon was beaten (the milestones' count).</summary>
        public IReadOnlyDictionary<string, int> BeatenByBoss => _beatenByBoss;

        private readonly Dictionary<string, int> _lowestSentence = new Dictionary<string, int>();

        /// <summary>The fewest years left when a run of <paramref name="classId"/> walked free at <paramref name="difficulty"/> (its score);
        /// null for never.</summary>
        public int? LowestSentence(string classId, Difficulty difficulty) =>
            classId != null && _lowestSentence.TryGetValue(Key(classId, difficulty), out int years) ? years : (int?)null;

        /// <summary>How often <paramref name="classId"/> walked free at <paramref name="difficulty"/>.</summary>
        public int FreedAt(string classId, Difficulty difficulty) => classId != null && _freedAt.TryGetValue(Key(classId, difficulty), out int n) ? n : 0;

        private static string Key(string classId, Difficulty difficulty) => classId + "." + DifficultyRules.IdOf(difficulty);

        /// <summary>A level may be chosen: Easy and Medium always; Hard once any class walked free at Medium (or harder); Ironman once at Hard.
        /// <see cref="ChapterUnlocks.AllClassesOpen"/> opens them all.</summary>
        public bool IsDifficultyOpen(Difficulty difficulty) =>
            ChapterUnlocks.AllClassesOpen || difficulty <= Difficulty.Medium || (HardestFreed.HasValue && HardestFreed.Value >= difficulty - 1);

        // ------------------------------------------------------------------ Ironman: no load cheats

        private readonly List<string> _deadRuns = new List<string>();
        private readonly Dictionary<string, int> _savesByRun = new Dictionary<string, int>();

        /// <summary>The most dead Ironman runs remembered.</summary>
        public const int DeadRunsKept = 64;

        /// <summary>An Ironman run died (its save is gone): its id is never loaded again.</summary>
        public void RunDied(string runId)
        {
            if (string.IsNullOrEmpty(runId) || _deadRuns.Contains(runId)) return;
            _deadRuns.Add(runId);
            if (_deadRuns.Count > DeadRunsKept) _deadRuns.RemoveAt(0);
            _savesByRun.Remove(runId);
        }

        public bool IsDead(string runId) => !string.IsNullOrEmpty(runId) && _deadRuns.Contains(runId);

        /// <summary>An Ironman run was saved for the <paramref name="count"/>th time.</summary>
        public void RunSaved(string runId, int count)
        {
            if (string.IsNullOrEmpty(runId)) return;
            _savesByRun[runId] = Math.Max(count, _savesByRun.TryGetValue(runId, out int n) ? n : 0);
        }

        /// <summary>An Ironman save may be loaded: not a dead run's, not older than the last one written for its run (a copy put back).</summary>
        public bool MayLoad(string runId, int saveCount) =>
            !IsDead(runId) && !(runId != null && _savesByRun.TryGetValue(runId, out int last) && saveCount < last);

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

        public int Damned => PurseEmptied + SoulsLost + CastDown + Abandoned;

        public void RunStarted() => Runs++;

        /// <summary>A run ended: how, how far it came (chapter 1-3; 4 for Lucifer's table), its class and its hands.</summary>
        /// <param name="difficulty">The level the run was played at: a freedom there may open the next.</param>
        /// <param name="sentence">The years left of the run's sentence (a freedom's score); -1 for unknown.</param>
        public void RunEnded(JourneyEnd end, int deepest, string classId, int hands, Difficulty difficulty = Difficulty.Easy, int sentence = -1)
        {
            DeepestChapter = Math.Max(DeepestChapter, deepest);
            if (deepest >= 4) LuciferReached++;
            switch (end)
            {
                case JourneyEnd.Freed:
                    Freed++;
                    if (!string.IsNullOrEmpty(classId)) _freedByClass[classId] = (_freedByClass.TryGetValue(classId, out int n) ? n : 0) + 1;
                    if (!FastestFreedom.HasValue || hands < FastestFreedom.Value) FastestFreedom = hands;
                    if (!HardestFreed.HasValue || difficulty > HardestFreed.Value) HardestFreed = difficulty;
                    if (!string.IsNullOrEmpty(classId)) _freedAt[Key(classId, difficulty)] = FreedAt(classId, difficulty) + 1;
                    if (!string.IsNullOrEmpty(classId) && sentence >= 0)
                    {
                        int? lowest = LowestSentence(classId, difficulty);
                        if (!lowest.HasValue || sentence < lowest.Value) _lowestSentence[Key(classId, difficulty)] = sentence;
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
            lines.AddRange(_freedAt.OrderBy(p => p.Key).Select(p => "free." + p.Key + "=" + I(p.Value)));
            if (HardestFreed.HasValue) lines.Add("hardest=" + DifficultyRules.IdOf(HardestFreed.Value));
            lines.Add("ironman.dead=" + string.Join(",", _deadRuns));
            lines.AddRange(_savesByRun.OrderBy(p => p.Key).Select(p => "ironman.saves." + p.Key + "=" + I(p.Value)));
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
            foreach (var pair in values.Where(p => p.Key.StartsWith("free.", StringComparison.Ordinal)))
                book._freedAt[pair.Key.Substring("free.".Length)] = Count(pair.Key);
            if (values.TryGetValue("hardest", out string hardest) && hardest.Length > 0) book.HardestFreed = DifficultyRules.Parse(hardest);
            book._deadRuns.AddRange((values.TryGetValue("ironman.dead", out string dead) ? dead : "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            foreach (var pair in values.Where(p => p.Key.StartsWith("ironman.saves.", StringComparison.Ordinal)))
                book._savesByRun[pair.Key.Substring("ironman.saves.".Length)] = Count(pair.Key);
            foreach (var pair in values.Where(p => p.Key.StartsWith("beaten.", StringComparison.Ordinal)))
                book._beatenByBoss[pair.Key.Substring("beaten.".Length)] = Count(pair.Key);
            foreach (var pair in values.Where(p => p.Key.StartsWith("sentence.", StringComparison.Ordinal)))
            {
                string key = pair.Key.Substring("sentence.".Length);
                int dot = key.LastIndexOf('.');
                if (dot <= 0) continue;
                string level = key.Substring(dot + 1);
                if (level.Length > 0 && char.IsDigit(level[0]))
                {
                    // Demo 3.4: an old book's circle records (sentence.<class>.<circle>) become the class's Easy record — the best of them.
                    string easy = Key(key.Substring(0, dot), Difficulty.Easy);
                    int years = Count(pair.Key);
                    if (!book._lowestSentence.TryGetValue(easy, out int kept) || years < kept) book._lowestSentence[easy] = years;
                }
                else book._lowestSentence[key] = Count(pair.Key);
            }
            // An old book's freedoms (before the levels) count as Easy ones.
            if (book._freedAt.Count == 0)
                foreach (var pair in book._freedByClass)
                    if (pair.Value > 0) book._freedAt[Key(pair.Key, Difficulty.Easy)] = pair.Value;
            if (!book.HardestFreed.HasValue && book._freedAt.Count > 0) book.HardestFreed = Difficulty.Easy;
            book.WardensBeaten = Count("wardens");
            book.MostWardensInAChapter = Count("wardens.chapter");
            book.RelicsBought = Count("bought");
            book.TributesPaid = Count("tributes");
            return book;
        }
    }
}
