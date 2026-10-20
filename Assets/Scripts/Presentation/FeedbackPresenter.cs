using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HellPoker.Presentation
{
    /// <summary>
    /// A bug report or an idea, written for the developer's inbox: the subject "[Hell Poker HATA] ..." / "[Hell Poker ÖNERİ] ...", and a
    /// body with the player's words, the version, where the run stands and its last log lines; as a mailto: link, and as plain text for
    /// the clipboard when no mail app answers.
    /// </summary>
    public static class FeedbackReport
    {
        public const string Address = "caveengine@gmail.com";

        /// <summary>How many of the run log's last lines go into the body.</summary>
        public const int LogLines = 12;

        /// <summary>The subject: the kind's tag (Turkish, as the inbox sorts it) and the text's first words.</summary>
        public static string Subject(bool bug, string text)
        {
            string tag = bug ? "[Hell Poker HATA]" : "[Hell Poker ÖNERİ]";
            string first = OneLine(text);
            if (first.Length > 48) first = first.Substring(0, 48).TrimEnd() + "...";
            return first.Length == 0 ? tag : tag + " " + first;
        }

        public static string Body(bool bug, string text, string version, Abstractions.FeedbackContext context)
        {
            var body = new StringBuilder();
            body.AppendLine((text ?? "").Trim());
            body.AppendLine();
            body.AppendLine("---");
            body.AppendLine("Kind: " + (bug ? "bug" : "idea"));
            body.AppendLine("Version: " + (version ?? ""));
            if (context == null)
            {
                body.AppendLine("Run: none");
                return body.ToString();
            }
            body.AppendLine("Class: " + context.ClassId);
            body.AppendLine("Difficulty: " + context.Difficulty);
            body.AppendLine("Chapter: " + (context.Chapter >= 4 ? "Lucifer" : context.Chapter.ToString()));
            body.AppendLine("Floor: " + (context.Floor > 0 ? context.Floor.ToString() : "-"));
            body.AppendLine("Last log lines:");
            foreach (string line in context.LogLines.Skip(Math.Max(0, context.LogLines.Count - LogLines))) body.AppendLine("  " + line);
            return body.ToString();
        }

        /// <summary>The mailto: link (subject and body escaped).</summary>
        public static string MailTo(string subject, string body) =>
            "mailto:" + Address + "?subject=" + Uri.EscapeDataString(subject ?? "") + "&body=" + Uri.EscapeDataString((body ?? "").Replace("\r\n", "\n"));

        /// <summary>The report as plain text, the address first (the clipboard's fallback).</summary>
        public static string PlainText(string subject, string body) => "To: " + Address + "\nSubject: " + subject + "\n\n" + body;

        private static string OneLine(string text) =>
            string.Join(" ", (text ?? "").Split(new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>The report box: opened from the title menu (the game's pause menu too), sends by mail and always copies to the clipboard.</summary>
    public sealed class FeedbackPresenter : Abstractions.IFeedbackCommands, IDisposable
    {
        /// <summary>A mailto: link stays under this many characters (some mail apps cut longer ones): the log lines go first.</summary>
        public const int MaxLink = 1800;

        private readonly Abstractions.IFeedbackView _view;
        private readonly Abstractions.IExternalLinks _links;
        private readonly Func<Abstractions.FeedbackContext> _context;
        private readonly string _version;

        public FeedbackPresenter(Abstractions.IFeedbackView view, Abstractions.IExternalLinks links, Func<Abstractions.FeedbackContext> context, string version)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _links = links ?? throw new ArgumentNullException(nameof(links));
            _context = context ?? (() => null);
            _version = version ?? "";
            _view.SendPressed += Send;
            _view.BackPressed += CloseFeedback;
        }

        public bool IsFeedbackOpen => _view.IsOpen;

        /// <summary>The last report sent (for tests): its subject, body, and the link handed to the mail app.</summary>
        public string LastSubject { get; private set; }
        public string LastBody { get; private set; }
        public string LastLink { get; private set; }

        public void Open(bool bug = true)
        {
            _view.Show(bug);
            _view.SetStatus(string.Format(Ui.UiText.FeedbackAddressFormat, FeedbackReport.Address));
        }

        public void CloseFeedback() => _view.Hide();

        private void Send(bool bug, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _view.SetStatus(Ui.UiText.FeedbackEmpty);
                return;
            }
            Abstractions.FeedbackContext context = _context();
            LastSubject = FeedbackReport.Subject(bug, text);
            LastBody = FeedbackReport.Body(bug, text, _version, context);
            // The whole report on the clipboard, always; the link may have to leave the log out to stay short.
            _links.Copy(FeedbackReport.PlainText(LastSubject, LastBody));
            LastLink = FeedbackReport.MailTo(LastSubject, LastBody);
            if (LastLink.Length > MaxLink)
                LastLink = FeedbackReport.MailTo(LastSubject, FeedbackReport.Body(bug, text, _version, null) + "\n" + Ui.UiText.FeedbackLogOnClipboard);
            bool opened = _links.Open(LastLink);
            _view.SetStatus(string.Format(opened ? Ui.UiText.FeedbackSentFormat : Ui.UiText.FeedbackCopiedFormat, FeedbackReport.Address));
        }

        public void Dispose()
        {
            _view.SendPressed -= Send;
            _view.BackPressed -= CloseFeedback;
        }
    }
}