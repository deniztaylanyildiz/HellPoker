using System;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    public enum ShowdownOutcome
    {
        PlayerWins,
        HouseWins,
        Push
    }

    public sealed class ShowdownResult
    {
        public HandEvaluation Player { get; }
        public HandEvaluation House { get; }
        public ShowdownOutcome Outcome { get; }

        private ShowdownResult(HandEvaluation player, HandEvaluation house, ShowdownOutcome outcome)
        {
            Player = player;
            House = house;
            Outcome = outcome;
        }

        public static ShowdownResult Resolve(HandEvaluation player, HandEvaluation house)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (house == null) throw new ArgumentNullException(nameof(house));

            int comparison = player.CompareTo(house);
            ShowdownOutcome outcome = comparison > 0 ? ShowdownOutcome.PlayerWins
                : comparison < 0 ? ShowdownOutcome.HouseWins
                : ShowdownOutcome.Push;

            return new ShowdownResult(player, house, outcome);
        }
    }
}
