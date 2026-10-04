namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The class badge under the dealer's portrait: who the player is, and what is left of their ability.</summary>
    public sealed class SinnerBadge
    {
        public static readonly SinnerBadge Hidden = new SinnerBadge(null, "", "", 0, 0);

        public string ClassId { get; }
        public string Name { get; }

        /// <summary>What the ability does, for the hover box.</summary>
        public string Description { get; }

        public int Charges { get; }
        public int MaxCharges { get; }

        public bool Visible => ClassId != null;

        public SinnerBadge(string classId, string name, string description, int charges, int maxCharges)
        {
            ClassId = classId;
            Name = name ?? "";
            Description = description ?? "";
            Charges = charges;
            MaxCharges = maxCharges;
        }
    }
}