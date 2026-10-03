using System;
using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Cheats
{
    public enum CheatOutcome
    {
        /// <summary>The cheat struck.</summary>
        Played,

        /// <summary>A guard refused it before it struck (a player's ability, later).</summary>
        Blocked,

        /// <summary>It had nothing to work on (e.g. only Dead Man's Hand cards to aim at) and came to nothing.</summary>
        Fizzled
    }

    /// <summary>
    /// What a cheat did, in plain meaning — which cards it touched, what was lost and gained, how many years moved. The
    /// presenter turns it into words; the view picks the animation.
    /// </summary>
    public sealed class CheatResult
    {
        public string CheatId { get; }

        /// <summary>The cheat that was announced for the hand; differs from <see cref="CheatId"/> when Belial lied.</summary>
        public string ShownId { get; }

        public CheatOutcome Outcome { get; }

        /// <summary>Positions in the player's hand the cheat touched.</summary>
        public IReadOnlyList<int> PlayerCards { get; }

        /// <summary>Positions in the House's hand the cheat touched.</summary>
        public IReadOnlyList<int> HouseCards { get; }

        /// <summary>The player's card that was taken, changed or hidden away, if any.</summary>
        public Card? Lost { get; }

        /// <summary>What came in its place, if any.</summary>
        public Card? Gained { get; }

        /// <summary>Years the cheat moves (the tithe on a win), if any; never shown while the soul is on the table.</summary>
        public int Years { get; }

        public bool WasLie => ShownId != null && ShownId != CheatId;

        /// <summary>
        /// The cheat turned on its demon: the player's hand came out of it stronger (a higher category, or the same one
        /// stronger). Only cheats that leave something to chance can do it (Belial's slipping tongue, Lucifer's burning card
        /// and The Fall); every other cheat never helps the player.
        /// </summary>
        public bool Backfired { get; }

        public CheatResult(string cheatId, CheatOutcome outcome, IReadOnlyList<int> playerCards = null, IReadOnlyList<int> houseCards = null,
            Card? lost = null, Card? gained = null, int years = 0, string shownId = null, bool backfired = false)
        {
            Backfired = backfired;
            if (string.IsNullOrEmpty(cheatId)) throw new ArgumentException("A cheat id is needed.", nameof(cheatId));
            CheatId = cheatId;
            ShownId = shownId ?? cheatId;
            Outcome = outcome;
            PlayerCards = playerCards ?? Array.Empty<int>();
            HouseCards = houseCards ?? Array.Empty<int>();
            Lost = lost;
            Gained = gained;
            Years = years;
        }

        public static CheatResult Fizzled(string cheatId) => new CheatResult(cheatId, CheatOutcome.Fizzled);

        /// <summary>The same result, told as announced under <paramref name="shownId"/>.</summary>
        public CheatResult AnnouncedAs(string shownId) =>
            new CheatResult(CheatId, Outcome, PlayerCards, HouseCards, Lost, Gained, Years, shownId, Backfired);

        /// <summary>The same result, marked as having helped the player.</summary>
        public CheatResult AsBackfire() =>
            new CheatResult(CheatId, Outcome, PlayerCards, HouseCards, Lost, Gained, Years, ShownId, backfired: true);
    }
}
