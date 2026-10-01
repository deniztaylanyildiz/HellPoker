namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// The soul bar, in shares of a whole soul — never in years (the player must not see what a soul is worth).
    /// </summary>
    public readonly struct SoulGauge
    {
        public bool Visible { get; }

        /// <summary>What is left of the soul, 0..1.</summary>
        public float Remaining { get; }

        /// <summary>The part of what is left that is on the table right now, 0..1 of a whole soul (blinks on the bar).</summary>
        public float AtRisk { get; }

        public SoulGauge(bool visible, float remaining, float atRisk)
        {
            Visible = visible;
            Remaining = Clamp(remaining);
            AtRisk = Clamp(atRisk);
        }

        public static SoulGauge Hidden => new SoulGauge(false, 1f, 0f);

        private static float Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    /// <summary>The LEAVE TABLE button: not offered, offered, or bound by the soul on the table.</summary>
    public enum LeaveState
    {
        Hidden,
        Open,
        Locked
    }
}
