namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// State of the bet buttons. A normal decision shows Raise / Pass / Fold — before the draw also CHECK TO DRAW
    /// (<see cref="ShowCheckToDraw"/>); an answer to a house re-raise (<see cref="IsAnswer"/>) shows Call / Fold instead.
    /// </summary>
    public readonly struct BetControls
    {
        public bool Visible { get; }
        public string RaiseLabel { get; }
        public bool CanRaise { get; }
        public bool CanPass { get; }

        /// <summary>Label of the Call button when answering a re-raise; null in a normal decision.</summary>
        public string CallLabel { get; }

        /// <summary>The CHECK TO DRAW button is shown (decisions before the draw).</summary>
        public bool ShowCheckToDraw { get; }

        /// <summary>CHECK TO DRAW may be pressed; when shown but not allowed it looks locked.</summary>
        public bool CanCheckToDraw { get; }

        /// <summary>The Fold button's label when it is not the usual one (the Peasant's power on: FREE FOLD); null: FOLD.</summary>
        public string FoldLabel { get; }

        public bool IsAnswer => CallLabel != null;

        public BetControls(bool visible, string raiseLabel, bool canRaise, bool canPass, string callLabel = null,
            bool showCheckToDraw = false, bool canCheckToDraw = false, string foldLabel = null)
        {
            FoldLabel = foldLabel;
            Visible = visible;
            RaiseLabel = raiseLabel;
            CanRaise = canRaise;
            CanPass = canPass;
            CallLabel = callLabel;
            ShowCheckToDraw = showCheckToDraw;
            CanCheckToDraw = canCheckToDraw;
        }

        public static BetControls Hidden => new BetControls(false, null, false, false);

        public static BetControls Answer(string callLabel, string foldLabel = null) => new BetControls(true, null, false, false, callLabel, foldLabel: foldLabel);
    }
}
