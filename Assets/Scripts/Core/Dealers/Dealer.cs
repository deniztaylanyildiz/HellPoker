using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Game;

namespace HellPoker.Core.Dealers
{
    /// <summary>
    /// A demon who runs the table. Each one brings their own house rules: how many cards may be exchanged,
    /// how much of their hand they show before the showdown, and their own payout ledger.
    /// The table's own numbers (starting sentence, damnation, stakes) stay the same for every demon.
    /// Names, portraits and lines live in the presentation layer, keyed by <see cref="Id"/>.
    /// </summary>
    /// <remarks>Future demon tricks (cheats, events) belong here as further parts of the bundle.</remarks>
    public sealed class Dealer
    {
        public string Id { get; }

        /// <summary>How many cards both the player and the house may exchange.</summary>
        public int MaxDiscards { get; }

        /// <summary>How many house cards come with a bet decision; see <see cref="GameRules.HouseRevealDecisions"/>.</summary>
        public int HouseRevealDecisions { get; }

        public PayoutTable Payouts { get; }

        public Dealer(string id, int maxDiscards, int houseRevealDecisions, PayoutTable payouts)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A dealer needs an id.", nameof(id));
            if (maxDiscards < 0 || maxDiscards > Hand.Size) throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            if (houseRevealDecisions < 0 || houseRevealDecisions >= Hand.Size) throw new ArgumentOutOfRangeException(nameof(houseRevealDecisions));

            Id = id;
            MaxDiscards = maxDiscards;
            HouseRevealDecisions = houseRevealDecisions;
            Payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
        }

        /// <summary>The table's rules with this dealer's house rules laid over them.</summary>
        public GameRules ApplyTo(GameRules table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            return new GameRules(table.StartingYears, table.DamnationYears, table.MinStake, table.MaxStake,
                MaxDiscards, table.ForcedRaiseYears, HouseRevealDecisions);
        }
    }
}
