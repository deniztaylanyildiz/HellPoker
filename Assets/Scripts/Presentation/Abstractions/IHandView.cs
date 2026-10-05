using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    public interface IHandView
    {
        /// <summary>Raised with the index of the clicked card.</summary>
        event Action<int> CardClicked;

        void SetCaption(string text, Tone tone);

        /// <summary>Shows the given slots; implementations animate only the slots that changed.</summary>
        void Show(IReadOnlyList<CardSlot> slots);

        void SetSelection(ICollection<int> selectedIndices);

        /// <summary>Softly marks the cards worth keeping (a suggestion only); null or empty clears the marks.</summary>
        void SetHints(ICollection<int> keepIndices);
        void SetInteractable(bool interactable);

        /// <summary>
        /// A power is picking a card (the King's protection, the Bone Die): the cards it may take blink in a gold frame, the others
        /// dim, and clicks go through. Null (or empty): no picking.
        /// </summary>
        void SetPicking(ICollection<int> pickable);
    }
}
