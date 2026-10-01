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
        void SetInteractable(bool interactable);
    }
}
