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

        /// <summary>Negative when years were forgiven, positive when added.</summary>
        public int YearsChange => YearsAfter - YearsBefore;

        public RoundResult(int stake, bool folded, ExchangeResult playerExchange, ExchangeResult houseExchange, ShowdownResult showdown,
            int yearsBefore, int yearsAfter, GamePhase phaseAfter)
        {
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
