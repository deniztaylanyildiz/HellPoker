namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// Demo 3.1: the SCREEN SHAKE setting as the views read it (like <see cref="AnimationClock"/>'s speed). Off: no shake and no white flash
    /// anywhere (a heavy loss, a hand's moment, the Morning Star's tremor); every other animation still plays.
    /// </summary>
    public static class ScreenShake
    {
        public static bool Enabled { get; private set; } = true;

        public static void Set(bool enabled) => Enabled = enabled;
    }
}
