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

        /// <summary>Negative when years were forgiven, positive when added.</summary>
        public int YearsChange => YearsAfter - YearsBefore;

        public RoundResult(int stake, bool folded, ExchangeResult playerExchange, ExchangeResult houseExchange, ShowdownResult showdown,
            int yearsBefore, int yearsAfter, GamePhase phaseAfter, bool thornDamned = false, bool freeFold = false, bool jokerJackpot = false,
            bool rattleGiven = false)
        {
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
