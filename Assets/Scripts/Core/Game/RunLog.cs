using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A readable diary of one run for the playtest: what was played, hand by hand, and everything around it — tables, Lucifer,
    /// the demons' cheats, events, relics, the class power — and how it ended. No personal data: the game's own facts only.
    /// Pure text: the presentation feeds it and decides where it is written (and that writing never stops the game).
    /// </summary>
    public sealed class RunLog
    {
        private readonly List<string> _lines = new List<string>();
        private readonly string _header;

        public DateTime Started { get; }

        /// <summary>The file it is written to: run-yyyyMMdd-HHmmss.txt (sorts by time).</summary>
        public string FileName => "run-" + Started.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt";

        /// <summary>True once the run's end was told (absolved, damned, abandoned, or the game closed mid-run).</summary>
        public bool IsEnded { get; private set; }

        public string Result { get; private set; }

        /// <summary>The diary's lines so far (for tests).</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <param name="resumedAtHand">A run picked up from a save: the hand it continues from (0: a new run).</param>
        public RunLog(string version, string language, string dealerId, string classId, int startingYears, DateTime started, int resumedAtHand = 0)
        {
            Started = started;
            var header = new StringBuilder();
            header.AppendLine("HELL POKER run log");
            header.AppendLine("version:  " + (version ?? "?"));
            header.AppendLine("language: " + (language ?? "?"));
            header.AppendLine("started:  " + started.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            header.AppendLine("demon:    " + (dealerId ?? "?"));
            header.AppendLine("class:    " + (classId ?? "?"));
            header.AppendLine((resumedAtHand > 0 ? "resumed:  from a save after hand " + resumedAtHand + ", at " : "start:    ") +
                              startingYears.ToString(CultureInfo.InvariantCulture) + " years");
            _header = header.ToString();
        }

        /// <summary>One settled hand: "#12 belial 340 -> 290  ONE PAIR vs HIGH CARD  stake 75  sealed soul".</summary>
        /// <param name="player">The player's hand name, or null for a fold.</param>
        public void Hand(int number, string dealerId, int yearsBefore, int yearsAfter, string player, string house, int stake,
            bool sealedHand, bool soul, bool freeFold = false)
        {
            var line = new StringBuilder();
            line.Append('#').Append(number.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(dealerId ?? "?").Append(' ')
                .Append(yearsBefore.ToString(CultureInfo.InvariantCulture)).Append(" -> ").Append(yearsAfter.ToString(CultureInfo.InvariantCulture))
                .Append("  ");
            line.Append(player == null ? (freeFold ? "free fold" : "fold") : player + " vs " + (house ?? "?"));
            line.Append("  stake ").Append(stake.ToString(CultureInfo.InvariantCulture));
            if (sealedHand) line.Append("  sealed");
            if (soul) line.Append("  soul");
            _lines.Add(line.ToString());
        }

        /// <summary>A demon's cheat: announced (as what), struck, blocked, fizzled, backfired.</summary>
        public void Cheat(int hand, string what, string cheatId, string announcedAs = null)
        {
            string shown = announcedAs != null && announcedAs != cheatId ? " (announced as " + announcedAs + ")" : "";
            _lines.Add("   cheat " + what + ": " + (cheatId ?? "?") + shown + "  [hand " + hand.ToString(CultureInfo.InvariantCulture) + "]");
        }

        /// <summary>Anything else that happened: a new table, Lucifer, an event, a relic, the class power.</summary>
        public void Note(string text)
        {
            if (!string.IsNullOrEmpty(text)) _lines.Add("-- " + text);
        }

        /// <summary>How the run ended (or that the game was closed with it still going).</summary>
        public void End(string result, int years, int hands)
        {
            if (IsEnded) return;
            IsEnded = true;
            Result = result;
            _lines.Add("== " + result + " at " + years.ToString(CultureInfo.InvariantCulture) + " years after " +
                       hands.ToString(CultureInfo.InvariantCulture) + " hands");
        }

        public string ToText()
        {
            var text = new StringBuilder(_header);
            text.AppendLine();
            foreach (string line in _lines)
                text.AppendLine(line);
            if (!IsEnded) text.AppendLine("== (still being played)");
            return text.ToString();
        }
    }
}
