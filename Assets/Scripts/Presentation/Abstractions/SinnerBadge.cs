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

        /// <summary>The power is switched on and waiting for the player's move (the Peasant's free fold, the King's protection).</summary>
        public bool Armed { get; }

        /// <summary>Switched on or a ward up: the badge pulses and says POWER ON.</summary>
        public bool PowerOn => Armed || WardRaised;

        public bool Visible => ClassId != null;

        /// <summary>The Jester's jokers in the deck (shown as a count in place of the pips); -1 for a class without them.</summary>
        public int Jokers { get; }

        public bool ShowsJokers => Jokers >= 0;

        /// <summary>Phase 2's class upgrades the run has: a gold pip each over the badge (their names are in the description).</summary>
        public int Upgrades { get; }

        /// <summary>
        /// Phase 2 (Demo 2.3): the class's powers work on their own — no gauge, no K hint; the pips are the main power's level
        /// (<see cref="Charge"/> of <see cref="Full"/>) and the description says it all.
        /// </summary>
        public bool Passive { get; }

        /// <summary>How many hands a passive has worked in so far: a new one flashes the badge.</summary>
        public int PassiveFlashes { get; }

        /// <summary>The gauge is full and waiting (never for a passive class's level pips).</summary>
        public bool Glows => IsCharged && !Passive;

        public SinnerBadge(string classId, string name, string description, int charge, int full, bool usable = false, bool wardRaised = false,
            bool armed = false, int jokers = -1, int upgrades = 0, bool passive = false, int passiveFlashes = 0)
        {
            Passive = passive;
            PassiveFlashes = passiveFlashes;
            Upgrades = upgrades;
            Jokers = jokers;
            Armed = armed;
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
