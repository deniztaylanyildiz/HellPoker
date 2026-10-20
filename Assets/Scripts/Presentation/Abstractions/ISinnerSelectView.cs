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

        /// <summary>Demo 2.3.1: the whole text, in a box while the card is hovered (the card itself keeps its short words); null: none.</summary>
        public string Hover { get; }

        public SinnerCard(string id, string name, string title, string ability, string detail, string start, bool locked = false,
            string lockNote = null, string hover = null)
        {
            Hover = hover;
            Locked = locked;
            LockNote = lockNote ?? "";
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? "";
            Title = title ?? "";
            Ability = ability ?? "";
            Detail = detail ?? "";
            Start = start ?? "";
        }
    }

    /// <summary>Demo 3.4: the difficulty row under the class cards (Phase 2): the level chosen, its name, whether it is open, its hover.</summary>
    public sealed class DifficultyChoice
    {
        public int Level { get; }
        public string Name { get; }
        public bool Open { get; }

        /// <summary>What the level adds (an open one), or what opens it (a locked one).</summary>
        public string Hover { get; }
        public bool CanStepDown { get; }
        public bool CanStepUp { get; }

        public DifficultyChoice(int level, string name, bool open, string hover, bool canStepDown, bool canStepUp)
        {
            Level = level;
            Name = name ?? "";
            Open = open;
            Hover = hover ?? "";
            CanStepDown = canStepDown;
            CanStepUp = canStepUp;
        }
    }

    /// <summary>The screen after the demon is chosen for a new run: who was the player, up there? (Peasant, Warlock, King.)</summary>
    public interface ISinnerSelectView
    {
        event Action<int> SinnerChosen;
        event Action BackPressed;

        /// <summary>Demo 3.4: the difficulty row's arrows: the step (-1 / +1).</summary>
        event Action<int> DifficultyStepped;

        bool IsVisible { get; }

        /// <param name="dealerId">The demon the run will be played against (their hall shows behind).</param>
        /// <param name="note">The line under the title (the level's rules in Phase 2); null: the usual question.</param>
        /// <param name="difficulty">Phase 2's difficulty row under the cards; null: none (the Boss Simulator).</param>
        void Show(IReadOnlyList<SinnerCard> sinners, string dealerId, string note = null, DifficultyChoice difficulty = null);

        void Hide();
    }
}