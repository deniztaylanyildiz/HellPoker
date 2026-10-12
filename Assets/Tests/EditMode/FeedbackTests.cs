using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.0's bug report / idea box: the mail's subject and body, the link, the clipboard.</summary>
    public class FeedbackTests
    {
        private sealed class FakeFeedbackView : IFeedbackView
        {
            public event Action<bool, string> SendPressed;
            public event Action BackPressed;
            public bool IsOpen { get; private set; }
            public bool Bug { get; private set; }
            public string Status { get; private set; }

            public void Show(bool bug)
            {
                IsOpen = true;
                Bug = bug;
            }

            public void Hide() => IsOpen = false;
            public void SetStatus(string status) => Status = status;
            public void Send(bool bug, string text) => SendPressed?.Invoke(bug, text);
            public void Back() => BackPressed?.Invoke();
        }

        private sealed class FakeLinks : IExternalLinks
        {
            public bool Answers = true;
            public readonly List<string> Opened = new List<string>();
            public string Copied;

            public bool Open(string url)
            {
                Opened.Add(url);
                return Answers;
            }

            public void Copy(string text) => Copied = text;
        }

        private static FeedbackContext Context(int lines = 3) =>
            new FeedbackContext("king", 2, 2, 5, Enumerable.Range(1, lines).Select(i => "line " + i).ToArray());

        [Test]
        public void TheSubject_TagsTheKind_InTheInboxsWords()
        {
            Assert.AreEqual("[Hell Poker HATA] The map froze", FeedbackReport.Subject(true, "The map\nfroze"));
            Assert.AreEqual("[Hell Poker ÖNERİ] More demons", FeedbackReport.Subject(false, "  More demons "));
            StringAssert.EndsWith("...", FeedbackReport.Subject(true, new string('a', 200)));
        }

        [Test]
        public void TheBody_TellsTheVersion_WhereTheRunStands_AndItsLastLogLines()
        {
            string body = FeedbackReport.Body(true, "It broke.", "2.0.0-demo", Context(20));
            StringAssert.StartsWith("It broke.", body);
            StringAssert.Contains("Version: 2.0.0-demo", body);
            StringAssert.Contains("Class: king", body);
            StringAssert.Contains("Circle: 2", body);
            StringAssert.Contains("Chapter: 2", body);
            StringAssert.Contains("Floor: 5", body);
            StringAssert.Contains("line 20", body);
            StringAssert.DoesNotContain("line 8\n", body.Replace("\r\n", "\n"), "only the last twelve");
            StringAssert.Contains("Run: none", FeedbackReport.Body(false, "x", "v", null));
            StringAssert.Contains("Chapter: Lucifer", FeedbackReport.Body(false, "x", "v", new FeedbackContext("peasant", 0, 4, 0, null)));
        }

        [Test]
        public void Send_OpensTheMailApp_AndAlwaysCopiesTheReport()
        {
            var view = new FakeFeedbackView();
            var links = new FakeLinks();
            var presenter = new FeedbackPresenter(view, links, () => Context(), "2.0.0-demo");
            presenter.Open();
            Assert.IsTrue(presenter.IsFeedbackOpen);
            Assert.IsTrue(view.Bug, "a bug by default");

            view.Send(true, "   ");
            Assert.AreEqual(UiText.FeedbackEmpty, view.Status);
            Assert.IsEmpty(links.Opened);

            view.Send(true, "The gate took my last coin.");
            Assert.AreEqual(1, links.Opened.Count);
            StringAssert.StartsWith("mailto:caveengine@gmail.com?subject=", links.Opened[0]);
            StringAssert.Contains(Uri.EscapeDataString("[Hell Poker HATA]"), links.Opened[0]);
            StringAssert.StartsWith("To: caveengine@gmail.com", links.Copied);
            StringAssert.Contains("Class: king", links.Copied);
            Assert.AreEqual(string.Format(UiText.FeedbackSentFormat, FeedbackReport.Address), view.Status);

            links.Answers = false;
            view.Send(false, "More relics, please.");
            Assert.AreEqual(string.Format(UiText.FeedbackCopiedFormat, FeedbackReport.Address), view.Status, "no mail app: the clipboard and the address");

            view.Back();
            Assert.IsFalse(presenter.IsFeedbackOpen);
            presenter.Dispose();
        }

        [Test]
        public void ALongLog_StaysOnTheClipboard_TheLinkStaysShort()
        {
            var view = new FakeFeedbackView();
            var links = new FakeLinks();
            var long_ = new FeedbackContext("jester", 0, 3, 7, Enumerable.Range(1, 12).Select(i => new string('x', 200) + i).ToArray());
            var presenter = new FeedbackPresenter(view, links, () => long_, "v");
            view.Send(true, "Too long.");
            Assert.LessOrEqual(links.Opened[0].Length, FeedbackPresenter.MaxLink + 400);
            StringAssert.Contains(new string('x', 200) + "12", links.Copied, "the whole log on the clipboard");
            presenter.Dispose();
        }
    }
}
