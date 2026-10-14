namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What a demon's health bar ends at: the soul line (Mammon, Belial, Lilith) or Lucifer's fall line.</summary>
    public enum BarLine
    {
        Soul,
        Fall
    }

    /// <summary>
    /// A demon's health bar as the screens show it — never a number. <see cref="Value"/> is what is left of the bar (the player's share
    /// of the sentence at the demon), <see cref="Limit"/> the line at its right end (the soul line, or Lucifer's fall line): the bar may
    /// grow past its <see cref="Start"/> toward it. A change between two displays is drawn by the view (a win flashes out, a loss fills
    /// in dark red).
    /// </summary>
    public sealed class BossBarDisplay
    {
        public static readonly BossBarDisplay Hidden = new BossBarDisplay(0, 0, 0, BarLine.Soul, null);

        public int Value { get; }
        public int Start { get; }
        public int Limit { get; }
        public BarLine Line { get; }

        /// <summary>The hover's words (the demon's bar, the hand); null for none.</summary>
        public string Hint { get; }

        public bool IsHidden => Limit <= 0;

        /// <summary>The next win ends the demon (2.1.1, "THE LAST BLOW"): the bar's fill glows.</summary>
        public bool LastBlow { get; }

        public BossBarDisplay(int value, int start, int limit, BarLine line, string hint, bool lastBlow) : this(value, start, limit, line, hint)
        {
            LastBlow = lastBlow;
        }

        /// <summary>The share of the whole bar (to its line) the value fills, 0 to 1.</summary>
        public float Fill => Limit <= 0 ? 0f : Clamp01(Value / (float)Limit);

        /// <summary>Where the start sits on the bar, 0 to 1.</summary>
        public float StartMark => Limit <= 0 ? 0f : Clamp01(Start / (float)Limit);

        public BossBarDisplay(int value, int start, int limit, BarLine line, string hint)
        {
            Value = value < 0 ? 0 : value;
            Start = start;
            Limit = limit;
            Line = line;
            Hint = hint;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
