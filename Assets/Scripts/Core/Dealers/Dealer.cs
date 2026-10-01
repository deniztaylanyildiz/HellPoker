using System;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Game;

namespace HellPoker.Core.Dealers
{
    /// <summary>
    /// A demon who runs the table. Each one brings their own house rules: how many cards may be exchanged,
    /// how much of their hand they show before the last bet, their payout ledger and their temper when you raise.
    /// The table's own numbers (starting sentence, damnation, betting scale) stay the same for every demon.
    /// Names, portraits and lines live in the presentation layer, keyed by <see cref="Id"/>.
    /// </summary>
    /// <remarks>Future demon tricks (cheats, events) belong here as further parts of the bundle.</remarks>
    public sealed class Dealer
    {
        public string Id { get; }

        /// <summary>How many cards both the player and the house may exchange.</summary>
        public int MaxDiscards { get; }

        /// <summary>How many house cards turn before the last bet decision; see <see cref="GameRules.HouseCardsShown"/>.</summary>
        public int HouseCardsShown { get; }

        public PayoutTable Payouts { get; }

        /// <summary>When and how often this demon re-raises (or bluffs) after your raise past the draw.</summary>
        public HouseBettingStyle Betting { get; }

        public Dealer(string id, int maxDiscards, int houseCardsShown, PayoutTable payouts, HouseBettingStyle betting = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A dealer needs an id.", nameof(id));
            if (maxDiscards < 0 || maxDiscards > Hand.Size) throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            if (houseCardsShown < 0 || houseCardsShown >= Hand.Size) throw new ArgumentOutOfRangeException(nameof(houseCardsShown));

            Id = id;
            MaxDiscards = maxDiscards;
            HouseCardsShown = houseCardsShown;
            Payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
            Betting = betting ?? HouseBettingStyle.Silent;
        }

        /// <summary>The table's rules with this dealer's house rules laid over them.</summary>
        public GameRules ApplyTo(GameRules table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return table.WithHouseRules(MaxDiscards, HouseCardsShown);
        }
    }
}
