using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// Per-table charges kept per demon, so changing seats cannot refill them: what was spent at a demon's table is still spent
    /// when the player comes back to it, a table never sat at starts full. Lucifer's table is fresh at every summons (a new
    /// attempt); a fall goes back to the demon the player came from, with whatever was left there.
    /// Shared by the sinner's per-table ability (the Warlock's ward, the King's protection) and the relics' redraw (the Bone Die).
    /// </summary>
    public sealed class TableCharges
    {
        private readonly Dictionary<string, int> _left = new Dictionary<string, int>();
        private readonly Func<int> _full;
        private string _current;
        private int? _pending;

        /// <param name="full">What a table starts with (read each time: a relic won mid-run changes it).</param>
        public TableCharges(Func<int> full)
        {
            _full = full ?? throw new ArgumentNullException(nameof(full));
        }

        public int Full => Math.Max(0, _full());

        /// <summary>The demon whose table the player sits at; null before the first seat.</summary>
        public string Current => _current;

        /// <summary>What is left at the current table.</summary>
        public int Left => LeftAt(_current);

        public int LeftAt(string dealerId)
        {
            if (dealerId == null) return Math.Min(Full, _pending ?? Full);
            return _left.TryGetValue(dealerId, out int left) ? Math.Max(0, Math.Min(Full, left)) : Full;
        }

        /// <summary>
        /// The player sits at <paramref name="dealerId"/>'s table: what was left there (full if never sat at). <paramref name="fresh"/>
        /// (Lucifer's summons): full again whatever happened before. A count restored from an older save without the per-demon
        /// list goes to the first table sat at.
        /// </summary>
        public void SitAt(string dealerId, bool fresh = false)
        {
            if (string.IsNullOrEmpty(dealerId)) throw new ArgumentException("A table needs a demon.", nameof(dealerId));
            if (fresh)
                _left.Remove(dealerId);
            else if (_pending.HasValue && !_left.ContainsKey(dealerId))
                _left[dealerId] = _pending.Value;
            _pending = null;
            _current = dealerId;
        }

        /// <summary>Spends one charge at the current table; false when none is left.</summary>
        public bool TrySpend()
        {
            int left = Left;
            if (left <= 0) return false;
            if (_current == null) _pending = left - 1;
            else _left[_current] = left - 1;
            return true;
        }

        /// <summary>Every demon's table full again (a new relic that redraws comes fresh everywhere).</summary>
        public void Refill()
        {
            _left.Clear();
            _pending = null;
        }

        /// <summary>The per-demon counts for the save: "mammon:0,belial:1" (only tables where something was spent).</summary>
        public string Encode()
        {
            int full = Full;
            return string.Join(",", _left.Where(p => p.Value < full).OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Key + ":" + Math.Max(0, p.Value).ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// A saved run comes back: <paramref name="tables"/> as <see cref="Encode"/> wrote it (null: an older save without it),
        /// and <paramref name="current"/>, the count at the table the run was saved at (an older save's only number; -1: none).
        /// Broken entries are skipped.
        /// </summary>
        public void Restore(string tables, int current = -1)
        {
            _left.Clear();
            _current = null;
            _pending = current >= 0 ? current : (int?)null;
            if (string.IsNullOrEmpty(tables)) return;
            foreach (string entry in tables.Split(','))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && parts[0].Length > 0 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int left) && left >= 0)
                    _left[parts[0]] = left;
            }
            _pending = null;   // the list knows every table, the current one included
        }
    }
}
