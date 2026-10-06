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

        /// <summary>The player held two jokers or more at the showdown: the hand is lost whatever it makes.</summary>
        public bool PlayerBust { get; }

        /// <summary>The House held two jokers or more at the showdown: the player wins whatever the House makes.</summary>
        public bool HouseBust { get; }

        private ShowdownResult(HandEvaluation player, HandEvaluation house, ShowdownOutcome outcome, bool playerBust = false, bool houseBust = false)
        {
            Player = player;
            House = house;
            Outcome = outcome;
            PlayerBust = playerBust;
            HouseBust = houseBust;
        }

        /// <summary>
        /// The showdown under the jokers' rule: a side holding two jokers or more loses, whatever its cards make (both: a push);
        /// otherwise the better hand wins (<see cref="Resolve"/>).
        /// </summary>
        public static ShowdownResult Judge(HandEvaluation player, HandEvaluation house, bool playerBust, bool houseBust)
        {
            if (!playerBust && !houseBust) return Resolve(player, house);
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (house == null) throw new ArgumentNullException(nameof(house));
            ShowdownOutcome outcome = playerBust && houseBust ? ShowdownOutcome.Push
                : playerBust ? ShowdownOutcome.HouseWins
                : ShowdownOutcome.PlayerWins;
            return new ShowdownResult(player, house, outcome, playerBust, houseBust);
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
