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

        /// <summary>
        /// Between hands, a sentence at or below this summons the player to Lucifer's table — the only place a sentence can
        /// end (bar the Dead Man's Hand). 0 means there is no Lucifer: every table can set the player free.
        /// </summary>
        public int LuciferGateYears { get; }

        /// <summary>Climbing back above the gate at Lucifer's table casts the player down: the sentence becomes at least this.</summary>
        public int LuciferCastDownYears { get; }

        /// <summary>True for the rules of Lucifer's own table.</summary>
        public bool IsFinalTable { get; }

        /// <summary>
        /// True at an ordinary table while Lucifer waits below: a win there never ends the sentence (it keeps at least one
        /// year) unless it is the absolution hand. The end belongs to Lucifer's table.
        /// </summary>
        public bool KeepsTheLastYear => LuciferGateYears > 0 && !IsFinalTable;

        // ------------------------------------------------------------------ the demons' malice (cheats)

        /// <summary>Malice gained at the start of every hand.</summary>
        public int MalicePerHand { get; }

        /// <summary>Malice gained when the player wins a hand.</summary>
        public int MalicePerWin { get; }

        /// <summary>At or below this sentence every hand gains <see cref="MaliceLowSentenceBonus"/> more (not at the final table).</summary>
        public int MaliceLowSentenceYears { get; }

        public int MaliceLowSentenceBonus { get; }

        /// <summary>At or below this sentence a demon's cheat is a major one <see cref="MajorCheatPercent"/>% of the time.</summary>
        public int MajorCheatYears { get; }

        public int MajorCheatPercent { get; }

        /// <summary>
        /// A player who walks out on a hand the demon meant to cheat (closing the game mid-hand) is not forgiven: the gauge
        /// fills at once, and for this many hands after it grows <see cref="GrudgeMalicePerHand"/> faster.
        /// </summary>
        public int GrudgeHands { get; }

        public int GrudgeMalicePerHand { get; }

        /// <summary>Between hands, an event happens this often (percent), at least <see cref="EventCooldownHands"/> hands apart.</summary>
        public int EventChancePercent { get; }

        public int EventCooldownHands { get; }

        // ------------------------------------------------------------------ the deck

        /// <summary>
        /// True: the deck goes on from hand to hand (dealt, thrown and drawn cards do not come back) until it is too thin for another
        /// hand, when the demon shuffles all 52 again — the cards can be counted. False: a fresh shuffle every hand (as it was).
        /// </summary>
        public bool ContinuousDeck { get; }

        /// <summary>SHUFFLE between hands (the player's own): the deck back to 52, for this many years on the sentence.</summary>
        public int ShuffleYears { get; }

        /// <summary>Below this sentence the player may not shuffle (it would be a cheap way to count).</summary>
        public int ShuffleMinYears { get; }

        public GameRules(int startingYears = 1000, int soulThreshold = 2000, int maxDiscards = MaxDiscardPolicy.ClassicLimit,
            int forcedRaiseYears = 250, int houseCardsShown = 2, StakeScale stakes = null, int openingCardsShown = 2,
            int raiseUnitsBeforeDraw = 1, int raiseUnitsAfterDraw = 2, int houseReRaiseUnits = 1,
            int soulWorthYears = 1000, int soulLossPercent = 150, int luciferGateYears = 250, int luciferCastDownYears = 500,
            bool isFinalTable = false, int malicePerHand = 1, int malicePerWin = 1, int maliceLowSentenceYears = 500,
            int maliceLowSentenceBonus = 1, int majorCheatYears = 400, int majorCheatPercent = 50, int grudgeHands = 3,
            int grudgeMalicePerHand = 1, int eventChancePercent = 12, int eventCooldownHands = 4, bool continuousDeck = true,
            int shuffleYears = 10, int shuffleMinYears = 300)
        {
            if (shuffleYears < 0) throw new ArgumentOutOfRangeException(nameof(shuffleYears));
            if (shuffleMinYears < 0) throw new ArgumentOutOfRangeException(nameof(shuffleMinYears));
            ContinuousDeck = continuousDeck;
            ShuffleYears = shuffleYears;
            ShuffleMinYears = shuffleMinYears;
            if (malicePerHand < 0) throw new ArgumentOutOfRangeException(nameof(malicePerHand));
            if (malicePerWin < 0) throw new ArgumentOutOfRangeException(nameof(malicePerWin));
            if (maliceLowSentenceBonus < 0) throw new ArgumentOutOfRangeException(nameof(maliceLowSentenceBonus));
            if (majorCheatPercent < 0 || majorCheatPercent > 100) throw new ArgumentOutOfRangeException(nameof(majorCheatPercent));
            MalicePerHand = malicePerHand;
            MalicePerWin = malicePerWin;
            MaliceLowSentenceYears = maliceLowSentenceYears;
            MaliceLowSentenceBonus = maliceLowSentenceBonus;
            MajorCheatYears = majorCheatYears;
            MajorCheatPercent = majorCheatPercent;
            if (grudgeHands < 0) throw new ArgumentOutOfRangeException(nameof(grudgeHands));
            if (grudgeMalicePerHand < 0) throw new ArgumentOutOfRangeException(nameof(grudgeMalicePerHand));
            GrudgeHands = grudgeHands;
            GrudgeMalicePerHand = grudgeMalicePerHand;
            if (eventChancePercent < 0 || eventChancePercent > 100) throw new ArgumentOutOfRangeException(nameof(eventChancePercent));
            if (eventCooldownHands < 0) throw new ArgumentOutOfRangeException(nameof(eventCooldownHands));
            EventChancePercent = eventChancePercent;
            EventCooldownHands = eventCooldownHands;
            if (luciferGateYears < 0) throw new ArgumentOutOfRangeException(nameof(luciferGateYears));
            if (luciferGateYears > 0 && luciferCastDownYears <= luciferGateYears)
                throw new ArgumentOutOfRangeException(nameof(luciferCastDownYears), "Being cast down must put the player above the gate.");
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
            LuciferGateYears = luciferGateYears;
            LuciferCastDownYears = luciferCastDownYears;
            IsFinalTable = isFinalTable;
        }

        public static GameRules Default => new GameRules();

        /// <summary>
        /// The same rules with a dealer's house rules (discard limit, house cards shown, soul line) swapped in. A dealer with
        /// stakes of their own brings them along; the final table also drops the final stretch (it has its own scale).
        /// </summary>
        public GameRules WithHouseRules(int maxDiscards, int houseCardsShown, int soulThreshold, StakeScale stakes = null,
            bool finalTable = false)
        {
            return new GameRules(StartingYears, soulThreshold, maxDiscards, finalTable ? 0 : ForcedRaiseYears, houseCardsShown,
                stakes ?? Stakes, OpeningCardsShown, RaiseUnitsBeforeDraw, RaiseUnitsAfterDraw, HouseReRaiseUnits, SoulWorthYears,
                SoulLossPercent, LuciferGateYears, LuciferCastDownYears, finalTable, MalicePerHand, MalicePerWin, MaliceLowSentenceYears,
                MaliceLowSentenceBonus, MajorCheatYears, MajorCheatPercent, GrudgeHands, GrudgeMalicePerHand,
                EventChancePercent, EventCooldownHands, ContinuousDeck, ShuffleYears, ShuffleMinYears);
        }
    }
}
