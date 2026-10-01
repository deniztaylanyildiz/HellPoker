using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Everything the player is told about a demon dealer: who they are and what their house rules mean.</summary>
    public sealed class DealerCard
    {
        /// <summary>Also the portrait key.</summary>
        public string Id { get; }
        public string Name { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<string> Traits { get; }

        /// <summary>The sentence at which this demon takes the player's soul onto the table.</summary>
        public int SoulThreshold { get; }

        public DealerCard(string id, string name, string title, string description, IReadOnlyList<string> traits, int soulThreshold = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? "";
            Title = title ?? "";
            Description = description ?? "";
            Traits = traits ?? Array.Empty<string>();
            SoulThreshold = soulThreshold;
        }
    }
}
