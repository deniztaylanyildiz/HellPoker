namespace HellPoker.Presentation.Abstractions
{
    /// <summary>State of the Raise / Pass / Fold buttons.</summary>
    public readonly struct BetControls
    {
        public bool Visible { get; }
        public string RaiseLabel { get; }
        public bool CanRaise { get; }
        public bool CanPass { get; }

        public BetControls(bool visible, string raiseLabel, bool canRaise, bool canPass)
        {
            Visible = visible;
            RaiseLabel = raiseLabel;
            CanRaise = canRaise;
            CanPass = canPass;
        }

        public static BetControls Hidden => new BetControls(false, null, false, false);
    }
}
