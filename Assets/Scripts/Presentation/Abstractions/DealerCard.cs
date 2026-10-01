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

        public DealerCard(string id, string name, string title, string description, IReadOnlyList<string> traits)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? "";
            Title = title ?? "";
            Description = description ?? "";
            Traits = traits ?? Array.Empty<string>();
        }
    }
}
