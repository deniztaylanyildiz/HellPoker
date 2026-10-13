namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A cursed relic beside the dealer's portrait: its icon, name, gift and curse, and uses left this hand (the Bone Die).</summary>
    public sealed class RelicBadge
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>The gift and the curse, for the hover box.</summary>
        public string Description { get; }

        /// <summary>Uses left this hand; -1 for a relic that is not used, only carried.</summary>
        public int Uses { get; }

        /// <summary>The relic is being used: the player is picking its card (the Bone Die) — its tile pulses.</summary>
        public bool Selecting { get; }

        /// <summary>An earned relic (the Jester's Rattle): it sits apart from the offered ones, in a gold frame.</summary>
        public bool IsReward { get; }

        /// <summary>A Phase 2 relic's rarity: its frame's colour (the demo's relics keep the plain frame: <see cref="RelicRarity.Common"/>).</summary>
        public HellPoker.Core.Relics.RelicRarity Rarity { get; }

        /// <summary>Forged at a camp (Demo 2.1): an ember mark on its icon.</summary>
        public bool Forged { get; }

        public RelicBadge(string id, string name, string description, int uses = -1, bool selecting = false, bool isReward = false,
            HellPoker.Core.Relics.RelicRarity rarity = HellPoker.Core.Relics.RelicRarity.Common, bool forged = false)
        {
            Forged = forged;
            Rarity = rarity;
            IsReward = isReward;
            Selecting = selecting;
            Id = id;
            Name = name ?? "";
            Description = description ?? "";
            Uses = uses;
        }
    }
}