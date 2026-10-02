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

        /// <summary>At this sentence the demon takes the player's soul onto the table; see <see cref="GameRules.SoulThreshold"/>.</summary>
        public int SoulThreshold { get; }

        /// <summary>Stakes of the dealer's own, whatever the sentence (Lucifer's fixed 50 / 150); null for the table's scale.</summary>
        public StakeScale Stakes { get; }

        /// <summary>
        /// The final table (Lucifer): nobody sits there by choice — the player is summoned to it at the gate, cannot leave it,
        /// and only here can the sentence end. The final stretch rule does not apply (the table has its own stakes).
        /// </summary>
        public bool IsFinalTable { get; }

        public Dealer(string id, int maxDiscards, int houseCardsShown, PayoutTable payouts, HouseBettingStyle betting = null,
            int soulThreshold = 2000, StakeScale stakes = null, bool isFinalTable = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A dealer needs an id.", nameof(id));
            if (maxDiscards < 0 || maxDiscards > Hand.Size) throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            if (houseCardsShown < 0 || houseCardsShown >= Hand.Size) throw new ArgumentOutOfRangeException(nameof(houseCardsShown));
            if (soulThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(soulThreshold));

            Id = id;
            MaxDiscards = maxDiscards;
            HouseCardsShown = houseCardsShown;
            Payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
            Betting = betting ?? HouseBettingStyle.Silent;
            SoulThreshold = soulThreshold;
            Stakes = stakes;
            IsFinalTable = isFinalTable;
        }

        /// <summary>True when a player with this sentence would have their soul on this demon's table.</summary>
        public bool TakesSoulAt(int years) => years >= SoulThreshold;

        /// <summary>The table's rules with this dealer's house rules laid over them.</summary>
        public GameRules ApplyTo(GameRules table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return table.WithHouseRules(MaxDiscards, HouseCardsShown, SoulThreshold, Stakes, IsFinalTable);
        }
    }
}
