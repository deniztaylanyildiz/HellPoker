using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The demon's malice gauge as the table shows it: pips in the demon's own style (coins, scales, thorns, an ember).</summary>
    public readonly struct MaliceGauge
    {
        /// <summary>Whose pips to draw.</summary>
        public string DealerId { get; }
        public int Value { get; }
        public int Max { get; }
        public bool Visible => Max > 0;

        public MaliceGauge(string dealerId, int value, int max)
        {
            DealerId = dealerId;
            Max = Math.Max(0, max);
            Value = Math.Max(0, Math.Min(value, Max));
        }

        public static MaliceGauge Hidden => new MaliceGauge(null, 0, 0);
    }

    /// <summary>What the player is told about a cheat: its icon key, its name and what it does, in one sentence.</summary>
    public sealed class CheatCard
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public CheatCard(string id, string name, string description)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? "";
            Description = description ?? "";
        }
    }

    /// <summary>A cheat striking at the table, in meaning: which cheat, which cards. The view picks the effect.</summary>
    public sealed class CheatImpact
    {
        public string CheatId { get; }

        /// <summary>Positions in the player's hand it hits.</summary>
        public IReadOnlyList<int> PlayerCards { get; }

        /// <summary>Positions in the House's hand it hits.</summary>
        public IReadOnlyList<int> HouseCards { get; }

        public CheatImpact(string cheatId, IReadOnlyList<int> playerCards = null, IReadOnlyList<int> houseCards = null)
        {
            CheatId = cheatId ?? throw new ArgumentNullException(nameof(cheatId));
            PlayerCards = playerCards ?? Array.Empty<int>();
            HouseCards = houseCards ?? Array.Empty<int>();
        }
    }

    /// <summary>What a cheat left on a card, shown on it for as long as the hand lasts.</summary>
    public enum CardMark
    {
        None,

        /// <summary>Collateral: chained, cannot be thrown back.</summary>
        Chained,

        /// <summary>A thorn: throwing it back costs.</summary>
        Thorned,

        /// <summary>The player's own card, hidden from them (veil, moonless night, the serpent's gift).</summary>
        Veiled,

        /// <summary>A House card showing a false face (the faintest silver sheen).</summary>
        FalseFace,

        /// <summary>The King's protection: a small crown, no cheat may touch the card this hand.</summary>
        Protected,

        /// <summary>A joker turned into this card at the showdown: a tiny fool's cap in the corner, so the player knows which.</summary>
        Joker
    }
}
