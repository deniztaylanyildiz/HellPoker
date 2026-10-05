namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// A power switched on at the table, as the screen must show it until it is used or switched off: a line that stays under the
    /// message (what to do: "Pick a card to protect (K: cancel)"), and the Warlock's shield by the intent sign while his ward waits.
    /// (The badge's own glow and "POWER ON" come with <see cref="SinnerBadge"/>, the cards' frames with IHandView.SetPicking.)
    /// </summary>
    public sealed class PowerDisplay
    {
        public static readonly PowerDisplay None = new PowerDisplay(null, false);

        /// <summary>The line that stays on screen while the power waits; null: none.</summary>
        public string Hint { get; }

        /// <summary>The Warlock's ward is up: a small shield by the intent sign.</summary>
        public bool WardUp { get; }

        public PowerDisplay(string hint, bool wardUp)
        {
            Hint = string.IsNullOrEmpty(hint) ? null : hint;
            WardUp = wardUp;
        }

        public bool IsOn => Hint != null || WardUp;
    }
}
