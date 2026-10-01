using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>Tunable numbers for a Hell Poker run. The dealer's house rules are laid over these (see Dealers.Dealer).</summary>
    public sealed class GameRules
    {
        public int StartingYears { get; }

        /// <summary>Reaching this sentence means eternal damnation (game over).</summary>
        public int DamnationYears { get; }

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

        public GameRules(int startingYears = 1000, int damnationYears = 2000, int maxDiscards = MaxDiscardPolicy.ClassicLimit,
            int forcedRaiseYears = 250, int houseCardsShown = 2, StakeScale stakes = null, int openingCardsShown = 2,
            int raiseUnitsBeforeDraw = 1, int raiseUnitsAfterDraw = 2, int houseReRaiseUnits = 1)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (damnationYears <= startingYears) throw new ArgumentOutOfRangeException(nameof(damnationYears), "Must be above the starting sentence.");
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
            DamnationYears = damnationYears;
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

        /// <summary>The same rules with a dealer's house rules (discard limit, house cards shown) swapped in.</summary>
        public GameRules WithHouseRules(int maxDiscards, int houseCardsShown)
        {
            return new GameRules(StartingYears, DamnationYears, maxDiscards, ForcedRaiseYears, houseCardsShown, Stakes, OpeningCardsShown,
                RaiseUnitsBeforeDraw, RaiseUnitsAfterDraw, HouseReRaiseUnits);
        }
    }
}
