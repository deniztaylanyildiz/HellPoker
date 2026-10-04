using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Events
{
    /// <summary>
    /// When the run's events happen: rolled once between every two hands (never at Lucifer's table), with
    /// <see cref="ChancePercent"/>% chance, at least <see cref="CooldownHands"/> hands after the last one, each event at most once
    /// a run, among those that can appear now. Its own dice: the cards, the House and the cheats roll as before.
    /// </summary>
    public sealed class EventSession
    {
        private readonly IReadOnlyList<IHellEvent> _deck;
        private readonly HashSet<string> _seen = new HashSet<string>();

        public IRandomSource Random { get; }
        public int ChancePercent { get; }
        public int CooldownHands { get; }

        /// <summary>Events already offered this run (taken or not).</summary>
        public IReadOnlyCollection<string> Seen => _seen;

        /// <summary>Hands played since the last event (or since the run began).</summary>
        public int HandsSinceLast { get; private set; }

        public EventSession(IReadOnlyList<IHellEvent> deck, IRandomSource random, int chancePercent = 12, int cooldownHands = 4)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            if (chancePercent < 0 || chancePercent > 100) throw new ArgumentOutOfRangeException(nameof(chancePercent));
            if (cooldownHands < 0) throw new ArgumentOutOfRangeException(nameof(cooldownHands));
            ChancePercent = chancePercent;
            CooldownHands = cooldownHands;
        }

        /// <summary>A new run: nothing seen, the cooldown from the start.</summary>
        public void Reset()
        {
            _seen.Clear();
            HandsSinceLast = 0;
        }

        /// <summary>A saved run comes back.</summary>
        public void Restore(IEnumerable<string> seen, int handsSinceLast)
        {
            _seen.Clear();
            foreach (string id in seen ?? Enumerable.Empty<string>())
                if (!string.IsNullOrEmpty(id)) _seen.Add(id);
            HandsSinceLast = Math.Max(0, handsSinceLast);
        }

        /// <summary>
        /// Between two hands (call once per hand): the next event to offer, or null. An event offered is seen at once — leaving
        /// it unanswered (closing the game) counts as letting it pass.
        /// </summary>
        public IHellEvent Roll(IEventTable table, string dealerId, bool finalTable)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            HandsSinceLast++;
            if (finalTable || HandsSinceLast < CooldownHands) return null;
            if (Random.Next(100) >= ChancePercent) return null;

            IHellEvent[] open = _deck.Where(e => !_seen.Contains(e.Id) && e.CanAppear(table, dealerId)).ToArray();
            if (open.Length == 0) return null;
            IHellEvent chosen = open[Random.Next(open.Length)];
            _seen.Add(chosen.Id);
            HandsSinceLast = 0;
            return chosen;
        }
    }
}
