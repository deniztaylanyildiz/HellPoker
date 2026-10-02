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
        IPayoutView Payouts { get; }
        IDealerView Dealer { get; }

        /// <summary>True while animations are still playing; input should wait (or skip them).</summary>
        bool IsBusy { get; }

        /// <summary>The player is in a hurry: everything still to be shown appears in its final state at once.</summary>
        void SkipAnimations();

        event Action ActionPressed;
        event Action<BetAction> BetPressed;

        /// <summary>The CHECK TO DRAW button.</summary>
        event Action CheckToDrawPressed;

        event Action MenuPressed;
        event Action LeavePressed;
        event Action HandRanksPressed;

        /// <summary>True while the hand ranking panel is open over the table.</summary>
        bool HandRanksOpen { get; }

        /// <summary>Opens the hand ranking panel with what each hand pays at this table.</summary>
        void ShowHandRanks(IPayoutInfo payouts);

        void HideHandRanks();

        /// <summary>Shows or hides the soul bar; while it shows, the year counter is hidden and the hall turns cold.</summary>
        void SetSoul(SoulGauge gauge);

        /// <summary>The LEAVE TABLE button.</summary>
        void SetLeave(LeaveState state);

        /// <summary>Shows or hides the whole table immediately (animations keep running while hidden).</summary>
        void SetVisible(bool visible);

        void SetMessage(string text, Tone tone);

        /// <summary>Shows the main button with this label, or hides it when null.</summary>
        void SetAction(string label);

        void SetBetControls(BetControls controls);

        /// <summary>Shows the total stake on the table; 0 hides it.</summary>
        void SetPot(int years);

        /// <summary>What is at stake, e.g. "Win: at least −100 years · Lose: at least +100 years"; null hides it.</summary>
        void SetStakeInfo(string text);

        /// <summary>The ante of the next hand, shown next to the deal button; 0 hides it.</summary>
        void SetAnte(int years);

        /// <summary>Switches the hellfire look for the end of the sentence on or off.</summary>
        void SetFinalStretch(bool active, string banner);

        /// <summary>Plays a moment after everything queued before it.</summary>
        /// <param name="text">For <see cref="TableMoment.GoodHand"/>: the words that flare up.</param>
        /// <param name="playerCards">For <see cref="TableMoment.DeadMansHand"/>: the player's cards that light up, in order.</param>
        void PlayMoment(TableMoment moment, string text = null, System.Collections.Generic.IReadOnlyList<int> playerCards = null);

        /// <summary>Holds the next updates back for a moment, to let a reveal sink in.</summary>
        void Pause(float seconds);
    }
}
