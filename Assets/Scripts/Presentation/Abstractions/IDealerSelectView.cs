using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The screen where the player picks which demon to play against.</summary>
    public interface IDealerSelectView
    {
        /// <summary>Index into the list given to <see cref="Show"/>.</summary>
        event Action<int> DealerChosen;
        event Action BackPressed;

        bool IsVisible { get; }

        void Show(IReadOnlyList<DealerCard> dealers);

        void Hide();
    }
}
