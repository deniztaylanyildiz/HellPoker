namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The class badge under the dealer's portrait: who the player is, and their power's charge gauge.</summary>
    public sealed class SinnerBadge
    {
        public static readonly SinnerBadge Hidden = new SinnerBadge(null, "", "", 0, 0);

        public string ClassId { get; }
        public string Name { get; }

        /// <summary>What the class does, for the hover box.</summary>
        public string Description { get; }

        /// <summary>The gauge (pips lit) and how many pips it has.</summary>
        public int Charge { get; }
        public int Full { get; }

        /// <summary>The gauge is full: it glows.</summary>
        public bool IsCharged => Full > 0 && Charge >= Full;

        /// <summary>The power can be used right now: a short hint shows (K / the badge).</summary>
        public bool Usable { get; }

        /// <summary>The Warlock's ward is up and waiting for the cheat.</summary>
        public bool WardRaised { get; }

        public bool Visible => ClassId != null;

        public SinnerBadge(string classId, string name, string description, int charge, int full, bool usable = false, bool wardRaised = false)
        {
            ClassId = classId;
            Name = name ?? "";
            Description = description ?? "";
            Charge = charge;
            Full = full;
            Usable = usable;
            WardRaised = wardRaised;
        }
    }
}
