using System.Collections.Generic;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Phase 3: a chapter's table has no payouts panel; the hands' panel (H) shows the multipliers of the table's own payout table and,
    /// under the ranks, these notes — how a win and a loss are paid, and what this table adds (a demon's bar scale, a floor's bonus, Mammon's interest).
    /// </summary>
    public sealed partial class TablePresenter
    {
        /// <summary>The notes under the hands' panel at a chapter's table, a line each; null at a demo table (its payouts panel says it).</summary>
        internal string PayoutNotes()
        {
            if (_seat == null) return null;
            IPayoutInfo payouts = ShownPayouts;
            var lines = new List<string> { UiText.PayoutNoteAnte };
            string fold = payouts.FoldPercentBeforeDraw == payouts.FoldPercentAfterDraw
                ? UiText.ShareShort(payouts.FoldPercentAfterDraw)
                : UiText.ShareShort(payouts.FoldPercentBeforeDraw) + " / " + UiText.ShareShort(payouts.FoldPercentAfterDraw);
            lines.Add(string.Format(UiText.PayoutNoteLossFormat, UiText.LossSurcharge(payouts.LossPercent), fold));
            // The game's own table: a demon's is scaled to the bar, a floor's may pay more (the panel's multipliers are the same).
            switch ((_game as HellPokerGame)?.Payouts ?? (object)payouts)
            {
                case ScaledPayoutTable scaled:
                    lines.Add(string.Format(UiText.PayoutNoteScaleFormat, scaled.WinScalePercent, scaled.LossScalePercent));
                    break;
                case FloorPayoutTable floor when floor.WinPercent != 100:
                    lines.Add(string.Format(UiText.PayoutNoteFloorWinFormat, floor.WinPercent));
                    break;
            }
            if (_kit?.Rule is MammonInterest)
                lines.Add(string.Format(UiText.PayoutNoteInterestFormat, MammonInterest.InterestPercent));
            return string.Join("\n", lines);
        }
    }
}
