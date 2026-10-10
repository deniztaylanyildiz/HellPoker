using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    public sealed class RoundResult
    {
        /// <summary>Total stake at the end of the hand (ante plus raises).</summary>
        public int Stake { get; }

        public bool Folded { get; }

        /// <summary>Null if the player folded before the draw.</summary>
        public ExchangeResult PlayerExchange { get; }

        /// <summary>Null if the player folded before the draw.</summary>
        public ExchangeResult HouseExchange { get; }

        /// <summary>Null if the player folded.</summary>
        public ShowdownResult Showdown { get; }

        public int YearsBefore { get; }
        public int YearsAfter { get; }
        public GamePhase PhaseAfter { get; }

        /// <summary>True when a thorn thrown back at the draw took the last of the soul: the hand ended there, unplayed
        /// (<see cref="Folded"/> is set too, as there is no showdown).</summary>
        public bool ThornDamned { get; }

        /// <summary>True when the fold cost nothing (the Peasant's honest heart, once per run).</summary>
        public bool FreeFold { get; }

        /// <summary>The Jester's deck reached twenty jokers with this hand: they went, the count is two again.</summary>
        public bool JokerJackpot { get; }

        /// <summary>... and the Jester's Rattle joined the run (the first time only).</summary>
        public bool RattleGiven { get; }

        /// <summary>The jokers decided the hand before its betting was done: it ended at once, at the stake on the table.</summary>
        public bool SettledByJokers { get; }

        /// <summary>The House gave up its hand facing the player's raise (a floor's imp): the player won the stake, no showdown.</summary>
        public bool HouseFolded { get; }

        /// <summary>Negative when years were forgiven, positive when added.</summary>
        public int YearsChange => YearsAfter - YearsBefore;

        public RoundResult(int stake, bool folded, ExchangeResult playerExchange, ExchangeResult houseExchange, ShowdownResult showdown,
            int yearsBefore, int yearsAfter, GamePhase phaseAfter, bool thornDamned = false, bool freeFold = false, bool jokerJackpot = false,
            bool rattleGiven = false, bool settledByJokers = false, bool houseFolded = false)
        {
            HouseFolded = houseFolded;
            SettledByJokers = settledByJokers;
            JokerJackpot = jokerJackpot;
            RattleGiven = rattleGiven;
            FreeFold = freeFold;
            ThornDamned = thornDamned;
            Stake = stake;
            Folded = folded;
            PlayerExchange = playerExchange;
            HouseExchange = houseExchange;
            Showdown = showdown;
            YearsBefore = yearsBefore;
            YearsAfter = yearsAfter;
            PhaseAfter = phaseAfter;
        }
    }
}
