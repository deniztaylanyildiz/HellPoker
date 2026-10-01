using System.Collections.Generic;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// One run of Hell Poker. A hand goes:
    /// Betting → (PlaceBet) → PlayerReveal ×5 (Bet) → Drawing → (Draw) → HouseReveal ×N (Bet) → RoundOver → (NextRound) → Betting ...
    /// A fold at any bet decision ends the hand. The run ends when the sentence is served (Absolved)
    /// or reaches the damnation limit (Damned).
    /// </summary>
    public interface IHellPokerGame
    {
        GameRules Rules { get; }
        GamePhase Phase { get; }
        Hand PlayerHand { get; }
        Hand HouseHand { get; }

        /// <summary>Opening stake of the current hand; also the size of every raise.</summary>
        int Ante { get; }

        /// <summary>Everything at risk in the current hand: ante plus raises. Never more than <see cref="Years"/>.</summary>
        int CurrentStake { get; }

        /// <summary>Sentence minus what is on the table — what the player still holds back.</summary>
        int YearsOffTable { get; }

        /// <summary>What a raise would add now: the ante, or whatever is left when that is less (all in). 0 when all in.</summary>
        int RaiseAmount { get; }

        /// <summary>How many of the player's cards are face up (0-5).</summary>
        int PlayerCardsRevealed { get; }

        /// <summary>How many of the house's cards are face up (0-5).</summary>
        int HouseCardsRevealed { get; }

        int RoundNumber { get; }
        int Years { get; }
        RoundResult LastRound { get; }
        bool IsGameOver { get; }

        /// <summary>True in the final stretch of the sentence, where passing is forbidden.</summary>
        bool IsRaiseForced { get; }

        /// <summary>Within the rules' range and not more than the sentence (the minimum is always allowed and goes all in).</summary>
        bool IsValidStake(int stake);

        /// <summary>Sets the ante, deals both hands face down and turns the player's first card.</summary>
        void PlaceBet(int stake);

        bool CanBet(BetAction action, out string reason);

        /// <summary>Answers the current bet decision; turns the next card or moves to the next phase.</summary>
        void Bet(BetAction action);

        bool CanDraw(IReadOnlyCollection<int> discardIndices, out string reason);

        /// <summary>Exchanges the player's chosen cards, lets the house draw, and turns the house's first card.</summary>
        ExchangeResult Draw(IReadOnlyCollection<int> discardIndices);

        void NextRound();

        void Restart();
    }
}
