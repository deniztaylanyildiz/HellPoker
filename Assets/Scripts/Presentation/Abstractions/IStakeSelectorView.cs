using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    public interface IStakeSelectorView
    {
        event Action<int> StakeChosen;

        void SetSelected(int stake);

        /// <summary>Enables only the given stakes; the others are shown greyed out.</summary>
        void SetAvailable(ICollection<int> stakes);

        void SetVisible(bool visible);
    }
}
