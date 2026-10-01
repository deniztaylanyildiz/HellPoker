using System;
using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// The whole table as the presenter sees it: widgets and raw input, no game rules.
    /// Updates are shown in call order; implementations may animate them, and report <see cref="IsBusy"/> meanwhile.
    /// </summary>
    public interface ITableView
    {
        IHandView House { get; }
        IHandView Player { get; }
        ISentenceView Sentence { get; }
        IStakeSelectorView Stakes { get; }
        IPayoutView Payouts { get; }
        IDealerView Dealer { get; }

        /// <summary>True while animations are still playing; input should wait.</summary>
        bool IsBusy { get; }

        event Action ActionPressed;
        event Action<BetAction> BetPressed;
        event Action MenuPressed;

        /// <summary>Shows or hides the whole table immediately (animations keep running while hidden).</summary>
        void SetVisible(bool visible);

        void SetMessage(string text, Tone tone);

        /// <summary>Shows the main button with this label, or hides it when null.</summary>
        void SetAction(string label);

        void SetBetControls(BetControls controls);

        /// <summary>Shows the total stake on the table; 0 hides it.</summary>
        void SetPot(int years);

        /// <summary>Switches the hellfire look for the end of the sentence on or off.</summary>
        void SetFinalStretch(bool active, string banner);

        /// <summary>Holds the next updates back for a moment, to let a reveal sink in.</summary>
        void Pause(float seconds);
    }
}
