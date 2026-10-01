using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
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
            var payouts = dealer.Payouts;

            var traits = new List<string>
            {
                string.Format(UiText.TraitDrawFormat, dealer.MaxDiscards),
                dealer.HouseRevealDecisions == 1 ? UiText.TraitRevealOne : string.Format(UiText.TraitRevealFormat, dealer.HouseRevealDecisions),
                string.Format(UiText.TraitPayoutFormat, payouts.GetMultiplier(HandCategory.OnePair), payouts.GetMultiplier(HandCategory.Flush),
                    payouts.GetMultiplier(HandCategory.FullHouse)),
                string.Format(UiText.TraitLossFormat, UiText.StakeShare(payouts.LossPercent), UiText.StakeShare(payouts.FoldPercent))
            };

            return new DealerCard(dealer.Id, text.Name, text.Title, text.Description, traits);
        }
    }
}
