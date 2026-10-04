using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A sinner class on the choice screen, in words: who they were and what they can do.</summary>
    public sealed class SinnerCard
    {
        public string Id { get; }
        public string Name { get; }
        public string Title { get; }
        public string Ability { get; }
        public string Detail { get; }

        /// <summary>"STARTS AT 1250 YEARS".</summary>
        public string Start { get; }

        public SinnerCard(string id, string name, string title, string ability, string detail, string start)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? "";
            Title = title ?? "";
            Ability = ability ?? "";
            Detail = detail ?? "";
            Start = start ?? "";
        }
    }

    /// <summary>The screen after the demon is chosen for a new run: who was the player, up there? (Peasant, Warlock, King.)</summary>
    public interface ISinnerSelectView
    {
        event Action<int> SinnerChosen;
        event Action BackPressed;

        bool IsVisible { get; }

        /// <param name="dealerId">The demon the run will be played against (their hall shows behind).</param>
        void Show(IReadOnlyList<SinnerCard> sinners, string dealerId);

        void Hide();
    }
}