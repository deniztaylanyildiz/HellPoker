using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Where the player is, for a bug report: the run's class, circle, chapter (4: Lucifer's table), floor and its last log lines.</summary>
    public sealed class FeedbackContext
    {
        public string ClassId { get; }
        public int Circle { get; }
        public int Chapter { get; }

        /// <summary>The floor (1-based) the player stands on; 0 at none (the chapter's opening, Lucifer).</summary>
        public int Floor { get; }

        public IReadOnlyList<string> LogLines { get; }

        public FeedbackContext(string classId, int circle, int chapter, int floor, IReadOnlyList<string> logLines)
        {
            ClassId = classId;
            Circle = circle;
            Chapter = chapter;
            Floor = floor;
            LogLines = logLines ?? Array.Empty<string>();
        }
    }

    /// <summary>The bug report / idea box over the menu: the kind, a short text, SEND and BACK.</summary>
    public interface IFeedbackView
    {
        /// <summary>SEND: a bug (true) or an idea (false), and the player's words.</summary>
        event Action<bool, string> SendPressed;
        event Action BackPressed;

        bool IsOpen { get; }

        void Show(bool bug);
        void Hide();

        /// <summary>The line under the box: what happened to the report (the mail app opened, the clipboard, the address).</summary>
        void SetStatus(string status);
    }

    /// <summary>The world outside the game: a link opened (the mail app), the clipboard.</summary>
    public interface IExternalLinks
    {
        /// <summary>False when the link could not be handed over.</summary>
        bool Open(string url);

        void Copy(string text);
    }

    /// <summary>What the keyboard asks of the report box.</summary>
    public interface IFeedbackCommands
    {
        bool IsFeedbackOpen { get; }
        void CloseFeedback();
    }
}