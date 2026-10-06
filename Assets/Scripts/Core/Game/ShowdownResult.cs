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

        /// <summary>The jokers each side held at the showdown.</summary>
        public int PlayerJokers { get; }
        public int HouseJokers { get; }

        /// <summary>Both sides held two jokers or more: the one with fewer won (as many each: a push).</summary>
        public bool BothBust => PlayerBust && HouseBust;

        /// <summary>The winner's own hand was broken by jokers too (a joker duel): it pays as the weakest hand would.</summary>
        public bool WinnerBust => Outcome == ShowdownOutcome.PlayerWins ? PlayerBust : Outcome == ShowdownOutcome.HouseWins && HouseBust;

        /// <summary>Two jokers or more break a hand.</summary>
        public const int BustJokers = 2;

        private ShowdownResult(HandEvaluation player, HandEvaluation house, ShowdownOutcome outcome, int playerJokers = 0, int houseJokers = 0)
        {
            Player = player;
            House = house;
            Outcome = outcome;
            PlayerJokers = playerJokers;
            HouseJokers = houseJokers;
            PlayerBust = playerJokers >= BustJokers;
            HouseBust = houseJokers >= BustJokers;
        }

        /// <summary>
        /// The showdown under the jokers' rule: a side holding two jokers or more loses, whatever its cards make; when both do, the
        /// one with fewer jokers wins (as many each: a push); otherwise the better hand wins (<see cref="Resolve"/>).
        /// </summary>
        public static ShowdownResult Judge(HandEvaluation player, HandEvaluation house, int playerJokers, int houseJokers)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (house == null) throw new ArgumentNullException(nameof(house));
            bool playerBust = playerJokers >= BustJokers, houseBust = houseJokers >= BustJokers;
            if (!playerBust && !houseBust)
                return new ShowdownResult(player, house, Resolve(player, house).Outcome, playerJokers, houseJokers);
            ShowdownOutcome outcome = playerBust && houseBust
                ? playerJokers < houseJokers ? ShowdownOutcome.PlayerWins : playerJokers > houseJokers ? ShowdownOutcome.HouseWins : ShowdownOutcome.Push
                : playerBust ? ShowdownOutcome.HouseWins
                : ShowdownOutcome.PlayerWins;
            return new ShowdownResult(player, house, outcome, playerJokers, houseJokers);
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
