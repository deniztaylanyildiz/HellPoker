using System;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Demo 3.4: where SAVE stands: nowhere, on the map (beside its MENU, over its panels), or at a table (beside HANDS, dim).</summary>
    public enum SaveButtonPlace
    {
        Hidden,
        Map,
        Table
    }

    /// <summary>Demo 3.4: the SAVE button of a chapter's screens.</summary>
    public interface ISaveButtonView
    {
        event Action Pressed;

        /// <param name="enabled">False: dim, and its hover says why (at a table).</param>
        void Show(SaveButtonPlace place, bool enabled);

        /// <summary>The run was saved: "SAVED" flashes over the button.</summary>
        void Saved();
    }
}
