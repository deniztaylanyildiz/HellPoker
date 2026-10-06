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

        /// <summary>An effect, in its place in the table's queue: it sounds when the animation before it has played.</summary>
        void PlaySfx(string sfxId);

        /// <summary>A button of the event panel was pressed (its index in <see cref="EventCard.Options"/>).</summary>
        event Action<int> EventOptionPressed;

        /// <summary>Opens the event panel in the middle of the table (it opens like a curtain).</summary>
        void ShowEvent(EventCard card);

        void HideEvent();

        /// <summary>A relic beside the portrait was clicked (its id): the Bone Die's redraw.</summary>
        event Action<string> RelicPressed;

        /// <summary>The relics the run carries, beside the portrait; empty for none.</summary>
        void SetRelics(System.Collections.Generic.IReadOnlyList<RelicBadge> relics);

        /// <summary>The class badge under the portrait was clicked (the King's protection).</summary>
        event Action SinnerPressed;

        /// <summary>The class badge: the class and what is left of its ability; <see cref="SinnerBadge.Hidden"/> for none.</summary>
        void SetSinner(SinnerBadge badge);

        /// <summary>A power switched on and waiting: the line that stays under the message, the Warlock's shield (<see cref="PowerDisplay.None"/>: off).</summary>
        void SetPower(PowerDisplay power);

        /// <summary>The cards left in the deck, over the House's row (hover: how it works); -1 hides it (the Jester's deck).</summary>
        void SetDeckCount(int cards);

        /// <summary>The SHUFFLE button under the deal button: its label, or null to hide it; locked buttons stay clickable.</summary>
        void SetShuffle(string label, bool locked);

        /// <summary>SHUFFLE was pressed.</summary>
        event Action ShufflePressed;

        /// <summary>The joker picker at the showdown, on this card; null closes it.</summary>
        void ShowJokerPicker(JokerPick pick);

        /// <summary>The picker's arrows: (rank step, suit step), each -1, 0 or +1.</summary>
        event Action<int, int> JokerStepPressed;

        /// <summary>The picker's NAME IT.</summary>
        event Action JokerConfirmPressed;

        /// <summary>True while the hand ranking panel is open over the table.</summary>
        bool HandRanksOpen { get; }

        /// <summary>Opens the hand ranking panel with what each hand pays at this table.</summary>
        /// <param name="footnote">A line under the ranks (the demon's announced cheat and what it does); null for none.</param>
        void ShowHandRanks(IPayoutInfo payouts, string footnote = null);

        // ------------------------------------------------------------------ the demon's cheats

        /// <summary>The malice gauge under the demon's portrait.</summary>
        void SetMalice(MaliceGauge gauge);

        /// <summary>The cheat the demon announces above the portrait; null takes it down.</summary>
        void SetIntent(CheatCard intent);

        /// <summary>The announced intent was a lie: the sign shatters and shows the truth.</summary>
        void RevealLie(CheatCard truth);

        /// <summary>A cheat strikes on the cards (after everything queued before it).</summary>
        void PlayCheat(CheatImpact impact);

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
