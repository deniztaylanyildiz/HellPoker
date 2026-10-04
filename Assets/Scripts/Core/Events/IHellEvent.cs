using System.Collections.Generic;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Events
{
    /// <summary>The table between hands, as an event sees it and may change it. Implemented by the game.</summary>
    public interface IEventTable
    {
        GameRules Rules { get; }
        int Years { get; }

        /// <summary>Hands dealt so far this run.</summary>
        int DealtHands { get; }

        bool IsSoulAtStake { get; }

        /// <summary>What is left of the soul, in years of its worth (sold parts and burns gone).</summary>
        int SoulRemaining { get; }

        /// <summary>A whole soul, in years.</summary>
        int SoulWorth { get; }

        int Malice { get; }
        int MaliceMax { get; }

        /// <summary>The run's marks: the next hand's modifier, deferred years, the sold soul.</summary>
        RunEffects Effects { get; }

        /// <summary>Forgives years now (never the last one: an event does not end a sentence).</summary>
        void ForgiveYears(int years);

        /// <summary>Adds years now (it may damn).</summary>
        void AddYears(int years);

        /// <summary>Empties the demon's malice gauge.</summary>
        void EmptyMalice();
    }

    /// <summary>The choices every event offers: take it or let it pass.</summary>
    public static class EventOptions
    {
        public const string Accept = "accept";
        public const string Pass = "pass";

        public static readonly IReadOnlyList<string> AcceptOrPass = new[] { Accept, Pass };
    }

    /// <summary>
    /// Something that happens between hands, now and then (never at Lucifer's table): a stranger at the table, a demon's offer.
    /// It always asks: at least two options, one of them always to let it pass (<see cref="EventOptions.Pass"/>).
    /// A new event is a new class and one line in <see cref="EventDeck.Standard"/> (plus its words and its owner's portrait).
    /// </summary>
    public interface IHellEvent
    {
        /// <summary>Stable id: the save ("event.seen") and the presentation's words.</summary>
        string Id { get; }

        /// <summary>Whose offer it is: a demon's id for a demon's own, else the stranger's id (portrait and voice).</summary>
        string OwnerId(string dealerId);

        bool CanAppear(IEventTable table, string dealerId);

        IReadOnlyList<string> Options { get; }

        /// <summary>The player chose <paramref name="option"/>. Passing never changes anything.</summary>
        void Apply(string option, IEventTable table, string dealerId, IRandomSource random);

        /// <summary>A rough estimate of the years taking it is worth (positive: good for the player). For the balance
        /// simulation's player; never shown.</summary>
        int ExpectedYears(IEventTable table, string dealerId);
    }
}
