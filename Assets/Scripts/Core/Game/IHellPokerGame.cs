using System.Collections.Generic;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// One run of Hell Poker. A hand goes:
    /// Betting → (PlaceBet) → PlayerReveal: first cards together, then a decision on cards 3, 4 and 5 (Bet)
    /// → Drawing → (Draw) → DrawReveal: one decision → HouseReveal: some house cards turn, one decision → showdown
    /// → RoundOver → (NextRound) → Betting ...
    /// After the draw, a raise may be answered by a house re-raise (HouseReRaise: Call or Fold).
    /// A fold at any decision ends the hand. The run ends when the sentence is served (Absolved), or when the soul —
    /// on the table once the sentence reaches the dealer's soul line — has burned away completely (Damned).
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

        /// <summary>The most the player's own raises may bring the table to this hand (a share of the sentence at its start).
        /// A called house re-raise may go past it.</summary>
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

        // ------------------------------------------------------------------ the soul

        /// <summary>True once the sentence has reached the dealer's soul line: the player's soul is on the table.</summary>
        bool IsSoulAtStake { get; }

        /// <summary>True for a hand dealt with the soul on the table (bets measured against the soul, losses burn faster).</summary>
        bool IsSoulHand { get; }

        /// <summary>What a whole soul is worth, in years (never shown to the player).</summary>
        int SoulWorth { get; }

        /// <summary>What is left of the soul (its whole worth while it is not on the table). At 0 the player is damned.</summary>
        int SoulRemaining { get; }

        /// <summary>What may still be put on the table this hand: the rest of the sentence, or the rest of the soul.</summary>
        int WagerLeft { get; }

        // ------------------------------------------------------------------ changing tables

        /// <summary>Only between hands, and never while the soul is on this table.</summary>
        bool CanLeaveTable(out string reason);

        /// <summary>Seats the player here with the sentence they carry from another table (between hands only).</summary>
        void TakeOver(int years, int roundsPlayed);

        /// <summary>Puts down the ante, deals both hands face down and turns the player's opening cards.</summary>
        void PlaceBet();

        bool CanBet(BetAction action, out string reason);

        /// <summary>Answers the current bet decision; turns the next card or moves to the next phase.</summary>
        void Bet(BetAction action);

        // ------------------------------------------------------------------ guidance for the player

        /// <summary>What the player's face-up cards make right now; null before any card shows.</summary>
        HandCategory? PlayerHandNow { get; }

        /// <summary>While drawing: the cards the House's own logic would throw back from the player's hand (a hint only).</summary>
        IReadOnlyCollection<int> SuggestedDiscards();

        bool CanDraw(IReadOnlyCollection<int> discardIndices, out string reason);

        /// <summary>Exchanges the player's chosen cards and lets the house draw; then one decision on the new hand.</summary>
        ExchangeResult Draw(IReadOnlyCollection<int> discardIndices);

        void NextRound();

        void Restart();
    }
}
