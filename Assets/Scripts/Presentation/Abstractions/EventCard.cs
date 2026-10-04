using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>An event between hands, in words, for the panel in the middle of the table.</summary>
    public sealed class EventCard
    {
        public string EventId { get; }

        /// <summary>Whose portrait shows: a demon's id, or a stranger's (the event's own art).</summary>
        public string OwnerId { get; }

        public string OwnerName { get; }
        public string Title { get; }
        public string Text { get; }

        /// <summary>The buttons, in order; the last one always lets it pass (Esc).</summary>
        public IReadOnlyList<string> Options { get; }

        public EventCard(string eventId, string ownerId, string ownerName, string title, string text, IReadOnlyList<string> options)
        {
            EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
            OwnerId = ownerId ?? "";
            OwnerName = ownerName ?? "";
            Title = title ?? "";
            Text = text ?? "";
            Options = options ?? Array.Empty<string>();
        }
    }
}