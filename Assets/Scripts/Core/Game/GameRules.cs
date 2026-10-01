using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>Tunable numbers for a Hell Poker run. The dealer's house rules are laid over these (see Dealers.Dealer).</summary>
    public sealed class GameRules
    {
        public int StartingYears { get; }

        /// <summary>
        /// At this sentence the player's soul goes on the table (each dealer has their own line). Past it, every year added
        /// burns the soul; when nothing is left of it, the player is damned.
        /// </summary>
        public int SoulThreshold { get; }

        /// <summary>What a soul is worth, in years. The player never sees this number — only a bar.</summary>
        public int SoulWorthYears { get; }

        /// <summary>Losses on a hand played with the soul on the table cost this much more (percent, on top of the dealer's).</summary>
        public int SoulLossPercent { get; }

        /// <summary>Reaching this sentence means eternal damnation (game over): the soul line plus the whole soul.</summary>
        public int DamnationYears => SoulThreshold + SoulWorthYears;

        /// <summary>How many cards a player may exchange in the draw.</summary>
        public int MaxDiscards { get; }

        /// <summary>
        /// At or below this many years left, passing is forbidden: every bet decision must raise (or fold) —
        /// until the table cap or all in is reached, when there is nothing left to raise.
        /// </summary>
        public int ForcedRaiseYears { get; }

        /// <summary>How many of the house's cards are turned before the last bet decision (0-4); the rest go straight to the showdown.</summary>
        public int HouseCardsShown { get; }

        /// <summary>How many of the player's cards turn together at the deal, before the first decision (0-4).</summary>
        public int OpeningCardsShown { get; }

        /// <summary>A raise before the draw adds this many betting units.</summary>
        public int RaiseUnitsBeforeDraw { get; }

        /// <summary>A raise after the draw adds this many betting units.</summary>
        public int RaiseUnitsAfterDraw { get; }

        /// <summary>When the house re-raises, it adds this many betting units.</summary>
        public int HouseReRaiseUnits { get; }

        /// <summary>Betting unit, ante and table cap.</summary>
        public StakeScale Stakes { get; }

        public GameRules(int startingYears = 1000, int soulThreshold = 2000, int maxDiscards = MaxDiscardPolicy.ClassicLimit,
            int forcedRaiseYears = 250, int houseCardsShown = 2, StakeScale stakes = null, int openingCardsShown = 2,
            int raiseUnitsBeforeDraw = 1, int raiseUnitsAfterDraw = 2, int houseReRaiseUnits = 1,
            int soulWorthYears = 1000, int soulLossPercent = 150)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (soulThreshold <= startingYears) throw new ArgumentOutOfRangeException(nameof(soulThreshold), "Must be above the starting sentence.");
            if (soulWorthYears <= 0) throw new ArgumentOutOfRangeException(nameof(soulWorthYears));
            if (soulLossPercent < 0) throw new ArgumentOutOfRangeException(nameof(soulLossPercent));
            if (maxDiscards < 0 || maxDiscards > Hand.Size) throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            if (forcedRaiseYears < 0) throw new ArgumentOutOfRangeException(nameof(forcedRaiseYears));
            if (houseCardsShown < 0 || houseCardsShown >= Hand.Size)
                throw new ArgumentOutOfRangeException(nameof(houseCardsShown), $"Must be between 0 and {Hand.Size - 1}.");
            if (openingCardsShown < 0 || openingCardsShown >= Hand.Size)
                throw new ArgumentOutOfRangeException(nameof(openingCardsShown), $"Must be between 0 and {Hand.Size - 1}.");
            if (raiseUnitsBeforeDraw <= 0) throw new ArgumentOutOfRangeException(nameof(raiseUnitsBeforeDraw));
            if (raiseUnitsAfterDraw <= 0) throw new ArgumentOutOfRangeException(nameof(raiseUnitsAfterDraw));
            if (houseReRaiseUnits <= 0) throw new ArgumentOutOfRangeException(nameof(houseReRaiseUnits));

            StartingYears = startingYears;
            SoulThreshold = soulThreshold;
            SoulWorthYears = soulWorthYears;
            SoulLossPercent = soulLossPercent;
            MaxDiscards = maxDiscards;
            ForcedRaiseYears = forcedRaiseYears;
            HouseCardsShown = houseCardsShown;
            Stakes = stakes ?? StakeScale.Default;
            OpeningCardsShown = openingCardsShown;
            RaiseUnitsBeforeDraw = raiseUnitsBeforeDraw;
            RaiseUnitsAfterDraw = raiseUnitsAfterDraw;
            HouseReRaiseUnits = houseReRaiseUnits;
        }

        public static GameRules Default => new GameRules();

        /// <summary>The same rules with a dealer's house rules (discard limit, house cards shown, soul line) swapped in.</summary>
        public GameRules WithHouseRules(int maxDiscards, int houseCardsShown, int soulThreshold)
        {
            return new GameRules(StartingYears, soulThreshold, maxDiscards, ForcedRaiseYears, houseCardsShown, Stakes, OpeningCardsShown,
                RaiseUnitsBeforeDraw, RaiseUnitsAfterDraw, HouseReRaiseUnits, SoulWorthYears, SoulLossPercent);
        }
    }
}
