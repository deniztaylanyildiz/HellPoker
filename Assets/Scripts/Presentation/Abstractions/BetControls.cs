namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// State of the bet buttons. A normal decision shows Raise / Pass / Fold; an answer to a house re-raise
    /// (<see cref="IsAnswer"/>) shows Call / Fold instead.
    /// </summary>
    public readonly struct BetControls
    {
        public bool Visible { get; }
        public string RaiseLabel { get; }
        public bool CanRaise { get; }
        public bool CanPass { get; }

        /// <summary>Label of the Call button when answering a re-raise; null in a normal decision.</summary>
        public string CallLabel { get; }

        public bool IsAnswer => CallLabel != null;

        public BetControls(bool visible, string raiseLabel, bool canRaise, bool canPass, string callLabel = null)
        {
            Visible = visible;
            RaiseLabel = raiseLabel;
            CanRaise = canRaise;
            CanPass = canPass;
            CallLabel = callLabel;
        }

        public static BetControls Hidden => new BetControls(false, null, false, false);

        public static BetControls Answer(string callLabel) => new BetControls(true, null, false, false, callLabel);
    }
}
