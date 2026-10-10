using System;
using System.Collections.Generic;
using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The chapter's map as the presenter describes it: the floors, where the player stands, where they may go.</summary>
    public sealed class ChapterMapState
    {
        public ChapterMap Map { get; }

        /// <summary>Where the player stands; null before the first floor.</summary>
        public MapNode Current { get; }

        /// <summary>The nodes the player may step to (lit).</summary>
        public IReadOnlyCollection<MapNode> Choices { get; }

        /// <summary>The choice the keyboard points at (framed); null for none.</summary>
        public MapNode Selected { get; }

        /// <summary>The nodes walked so far, in order (the trail).</summary>
        public IReadOnlyList<MapNode> Trail { get; }

        public string Title { get; }
        public string Prompt { get; }

        /// <summary>The purse (never below zero).</summary>
        public int Coins { get; }

        /// <summary>The side panel's lines under the purse (tribute, sentence, what is owed, the relics...).</summary>
        public IReadOnlyList<string> Lines { get; }

        /// <summary>What a node is, in words (its hover box).</summary>
        public Func<MapNode, string> Describe { get; }

        /// <summary>The words by the gate under the last floor (the chapter's own gate).</summary>
        public string GateLabel { get; }

        /// <summary>The chapter (1-3): the map's backdrop.</summary>
        public int Chapter { get; }

        public ChapterMapState(ChapterMap map, MapNode current, IReadOnlyCollection<MapNode> choices, MapNode selected, IReadOnlyList<MapNode> trail,
            string title, string prompt, int coins, IReadOnlyList<string> lines, Func<MapNode, string> describe, string gateLabel = null, int chapter = 1)
        {
            GateLabel = gateLabel ?? "";
            Chapter = chapter;
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Current = current;
            Choices = choices ?? Array.Empty<MapNode>();
            Selected = selected;
            Trail = trail ?? Array.Empty<MapNode>();
            Title = title ?? "";
            Prompt = prompt ?? "";
            Coins = coins;
            Lines = lines ?? Array.Empty<string>();
            Describe = describe ?? (_ => "");
        }
    }

    /// <summary>The chapter's map screen.</summary>
    public interface IChapterMapView
    {
        /// <summary>A node was clicked: (floor, lane).</summary>
        event Action<int, int> NodePressed;

        /// <summary>The map's MENU button.</summary>
        event Action MenuPressed;

        bool IsVisible { get; }

        void Show(ChapterMapState state);

        void Hide();
    }

    /// <summary>One answer on a chapter panel: its button, what it means (shown while it is pointed at), and whether it can be taken.</summary>
    public sealed class PanelOption
    {
        public string Label { get; }
        public string Detail { get; }

        /// <summary>A locked option stays clickable: the presenter answers with why (its detail says it).</summary>
        public bool Enabled { get; }

        /// <summary>The option that leaves (drawn plainer).</summary>
        public bool IsLeave { get; }

        public PanelOption(string label, string detail = null, bool enabled = true, bool isLeave = false)
        {
            Label = label ?? "";
            Detail = detail ?? "";
            Enabled = enabled;
            IsLeave = isLeave;
        }
    }

    /// <summary>A panel over the map: who speaks (a portrait), a title, words, and the answers.</summary>
    public sealed class PanelCard
    {
        /// <summary>The portrait: a stranger's own art ("Events/&lt;id&gt;"), or a demon's / a floor face's id; null for none.</summary>
        public string PortraitId { get; }

        public string Owner { get; }
        public string Title { get; }
        public string Text { get; }
        public IReadOnlyList<PanelOption> Options { get; }

        /// <summary>The answer the keyboard points at.</summary>
        public int Selected { get; }

        public PanelCard(string portraitId, string owner, string title, string text, IReadOnlyList<PanelOption> options, int selected = 0)
        {
            PortraitId = portraitId;
            Owner = owner ?? "";
            Title = title ?? "";
            Text = text ?? "";
            Options = options ?? Array.Empty<PanelOption>();
            Selected = selected;
        }
    }

    /// <summary>The panel over the chapter's map (the treasure, the market, an offer, the fire, the gate, a match's end...).</summary>
    public interface IChapterPanelView
    {
        /// <summary>An answer was clicked (its index).</summary>
        event Action<int> OptionPressed;

        bool IsOpen { get; }

        void Show(PanelCard card);

        void Hide();
    }

    /// <summary>The keys on the chapter's map and its panels.</summary>
    public interface IChapterCommands
    {
        /// <summary>True while the map (or a panel over it) takes the keys; false at a chapter's table.</summary>
        bool IsMapOpen { get; }

        /// <summary>True while one of the chapter's tables is on screen.</summary>
        bool IsAtTable { get; }

        /// <summary>The chapter's table: the table keys go to it while it is on screen (null before the first chapter).</summary>
        ITableCommands Table { get; }

        /// <summary>The arrows: another choice on the map, another answer on a panel.</summary>
        void Step(int dx, int dy);

        /// <summary>Enter / Space: go to the pointed node, or take the pointed answer.</summary>
        void Confirm();

        /// <summary>Esc: closes what can be closed; false when nothing could (the menu takes it).</summary>
        bool Back();
    }

    /// <summary>What the title menu needs of Phase 2's chapters.</summary>
    public interface IChapterSession
    {
        /// <summary>A chapter run is going on in this session.</summary>
        bool HasRun { get; }

        /// <summary>A run waits to be gone back to: in this session, or saved from an earlier one.</summary>
        bool CanContinue { get; }

        /// <summary>Phase 2's own records (for the records screen).</summary>
        Core.Chapters.ChapterRecords Records { get; }

        bool IsVisible { get; }

        /// <summary>The music the chapter's screen plays now.</summary>
        string MusicId { get; }

        /// <summary>A new chapter run as this class (a run in progress is abandoned: it counts as damned).</summary>
        void Start(Core.Sinners.SinnerClass sinnerClass);

        /// <summary>Back to the run that waits (the saved one is loaded; a hand left in the middle is lost).</summary>
        void Continue();

        /// <summary>The run that waits is given up (it counts as damned).</summary>
        void Abandon();

        void Show();

        void Hide();

        /// <summary>MENU (or Esc) on the map, or the chapter's end: back to the title menu.</summary>
        event Action MenuRequested;

        /// <summary>NEW RUN at the chapter's end: the class choice again.</summary>
        event Action NewRunRequested;
    }
}
