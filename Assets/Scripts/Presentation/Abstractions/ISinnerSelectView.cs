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

        /// <summary>Phase 2: the class is locked (<see cref="LockNote"/> says what opens it); CHOOSE does nothing.</summary>
        public bool Locked { get; }
        public string LockNote { get; }

        /// <summary>Phase 2's circle of Hell chosen for the class, and the deepest it has open; -1: no circle selector (the demo).</summary>
        public int Circle { get; }
        public int CirclesOpen { get; }

        public bool ShowsCircles => CirclesOpen >= 0;

        public SinnerCard(string id, string name, string title, string ability, string detail, string start, bool locked = false,
            string lockNote = null, int circle = 0, int circlesOpen = -1)
        {
            Locked = locked;
            LockNote = lockNote ?? "";
            Circle = circle;
            CirclesOpen = circlesOpen;
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

        /// <summary>A card's circle arrows: its index and the step (-1 / +1).</summary>
        event Action<int, int> CircleStepped;

        bool IsVisible { get; }

        /// <param name="dealerId">The demon the run will be played against (their hall shows behind).</param>
        /// <param name="note">The line under the title (the circle's rules in Phase 2); null: the usual question.</param>
        void Show(IReadOnlyList<SinnerCard> sinners, string dealerId, string note = null);

        void Hide();
    }
}