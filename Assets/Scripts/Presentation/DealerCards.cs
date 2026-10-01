using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>Turns a dealer's house rules into what the player reads about them. Traits are derived from the rules, so they never lie.</summary>
    public static class DealerCards
    {
        public static DealerCard Describe(Dealer dealer)
        {
            DealerText text = UiText.Dealer(dealer.Id);
            PayoutTable payouts = dealer.Payouts;

            var traits = new List<string>
            {
                string.Format(UiText.TraitDrawFormat, dealer.MaxDiscards),
                dealer.HouseCardsShown == 1 ? UiText.TraitRevealOne : string.Format(UiText.TraitRevealFormat, dealer.HouseCardsShown),
                string.Format(UiText.TraitPayoutFormat, payouts.GetMultiplier(HandCategory.OnePair), payouts.GetMultiplier(HandCategory.Flush),
                    payouts.GetMultiplier(HandCategory.FullHouse), payouts.GetMultiplier(HandCategory.RoyalFlush)),
                string.Format(UiText.TraitLossFormat, UiText.LossSurcharge(payouts.LossPercent)),
                payouts.FoldPercentBeforeDraw == payouts.FoldPercentAfterDraw
                    ? string.Format(UiText.TraitFoldSameFormat, UiText.ShareShort(payouts.FoldPercentAfterDraw))
                    : string.Format(UiText.TraitFoldFormat, UiText.ShareShort(payouts.FoldPercentBeforeDraw), UiText.ShareShort(payouts.FoldPercentAfterDraw)),
                string.Format(UiText.TraitTemperFormat, UiText.CategoryName(dealer.Betting.StrongFrom), dealer.Betting.StrongPercent,
                    dealer.Betting.BluffPercent)
            };

            return new DealerCard(dealer.Id, text.Name, text.Title, text.Description, traits);
        }
    }
}
