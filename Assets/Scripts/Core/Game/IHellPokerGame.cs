using System.Collections.Generic;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// One run of Hell Poker. A hand goes:
    /// Betting → (PlaceBet) → PlayerReveal: first cards together, then a decision on cards 3, 4 and 5 (Bet)
    /// → Drawing → (Draw) → DrawReveal: one decision → HouseReveal: some house cards turn, one decision → showdown
    /// → RoundOver → (NextRound) → Betting ...
    /// After the draw, a raise may be answered by a house re-raise (HouseReRaise: Call or Fold).
    /// A fold at any decision ends the hand. The run ends when the sentence is served (Absolved)
    /// or reaches the damnation limit (Damned).
    /// </summary>
    public interface IHellPokerGame
    {
        GameRules Rules { get; }
        GamePhase Phase { get; }
        Hand PlayerHand { get; }
        Hand HouseHand { get; }

        /// <summary>The betting unit of the current hand (a tenth of the sentence at its start, rounded to a readable step).</summary>
        int Unit { get; }

        /// <summary>Opening stake of the current hand: one unit (all in when less is left).</summary>
        int Ante { get; }

        /// <summary>The ante the next hand will start with, at the current sentence.</summary>
        int UpcomingAnte { get; }

        /// <summary>The most that may be on the table this hand (a share of the sentence at its start).</summary>
        int TableCap { get; }

        /// <summary>Everything at risk in the current hand: ante plus raises plus called re-raises.</summary>
        int CurrentStake { get; }

        /// <summary>Sentence minus what is on the table — what the player still holds back.</summary>
        int YearsOffTable { get; }

        /// <summary>What a raise would add now (1 unit before the draw, 2 after), cut down by the table cap. 0 when nothing can be added.</summary>
        int RaiseAmount { get; }

        /// <summary>In <see cref="GamePhase.HouseReRaise"/>: what the house raised, and what calling costs.</summary>
        int HouseReRaiseAmount { get; }

        /// <summary>True once the cards have been exchanged this hand.</summary>
        bool IsAfterDraw { get; }

        /// <summary>The least a win would forgive with what is on the table (the upcoming ante between hands).</summary>
        int LeastYearsForgiven { get; }

        /// <summary>The least a loss would add with what is on the table (the upcoming ante between hands).</summary>
        int LeastYearsAdded { get; }

        /// <summary>How many of the player's cards are face up (0-5).</summary>
        int PlayerCardsRevealed { get; }

        /// <summary>How many of the house's cards are face up (0-5).</summary>
        int HouseCardsRevealed { get; }

        int RoundNumber { get; }
        int Years { get; }
        RoundResult LastRound { get; }
        bool IsGameOver { get; }

        /// <summary>True in the final stretch of the sentence, where passing is forbidden while a raise is possible.</summary>
        bool IsRaiseForced { get; }

        /// <summary>Puts down the ante, deals both hands face down and turns the player's opening cards.</summary>
        void PlaceBet();

        bool CanBet(BetAction action, out string reason);

        /// <summary>Answers the current bet decision; turns the next card or moves to the next phase.</summary>
        void Bet(BetAction action);

        bool CanDraw(IReadOnlyCollection<int> discardIndices, out string reason);

        /// <summary>Exchanges the player's chosen cards and lets the house draw; then one decision on the new hand.</summary>
        ExchangeResult Draw(IReadOnlyCollection<int> discardIndices);

        void NextRound();

        void Restart();
    }
}
