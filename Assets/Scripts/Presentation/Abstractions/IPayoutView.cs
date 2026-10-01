using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Abstractions
{
    public interface IPayoutView
    {
        /// <summary>Highlights the row of the winning category, or clears it with null.</summary>
        void Highlight(HandCategory? category);
    }
}
