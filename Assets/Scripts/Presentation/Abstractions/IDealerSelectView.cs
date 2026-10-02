using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A demon on the choice screen, with what sitting at their table would mean right now.</summary>
    public sealed class DealerChoice
    {
        public DealerCard Card { get; }

        /// <summary>True when the player's current sentence is past this demon's soul line.</summary>
        public bool SoulAtStake { get; }

        /// <summary>True for the demon the player already sits with.</summary>
        public bool IsCurrent { get; }

        /// <summary>True for a demon nobody may choose (Lucifer): the card shows, darkened, but cannot be sat at.</summary>
        public bool IsLocked { get; }

        public DealerChoice(DealerCard card, bool soulAtStake, bool isCurrent, bool isLocked = false)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            SoulAtStake = soulAtStake;
            IsCurrent = isCurrent;
            IsLocked = isLocked;
        }
    }

    /// <summary>The screen where the player picks which demon to play against (to start a run, or to change tables).</summary>
    public interface IDealerSelectView
    {
        /// <summary>Index into the list given to <see cref="Show"/>.</summary>
        event Action<int> DealerChosen;
        event Action BackPressed;

        /// <summary>The player accepted the warning shown by <see cref="AskToConfirm"/>.</summary>
        event Action SeatConfirmed;

        /// <summary>The player backed out of the warning.</summary>
        event Action SeatCancelled;

        bool IsVisible { get; }

        /// <summary>True while the warning of <see cref="AskToConfirm"/> is open.</summary>
        bool IsConfirming { get; }

        void Show(IReadOnlyList<DealerChoice> dealers);

        /// <summary>Shows a warning with "sit anyway" and "back".</summary>
        void AskToConfirm(string warning);

        /// <summary>Closes the warning without an answer (Esc).</summary>
        void CloseConfirm();

        /// <summary>A locked demon answers from the dark (the line shows in the details panel).</summary>
        void ShowLockedLine(int index, string line);

        void Hide();
    }
}
