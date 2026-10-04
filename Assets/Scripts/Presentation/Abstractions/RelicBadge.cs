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

        public RelicBadge(string id, string name, string description, int uses = -1)
        {
            Id = id;
            Name = name ?? "";
            Description = description ?? "";
            Uses = uses;
        }
    }
}