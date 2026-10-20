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

        /// <summary>The chapter demon's bar as it would stand at the gate (small, no number), and its name.</summary>
        public BossBarDisplay Bar { get; }
        public string BarLabel { get; }

        /// <summary>The icon a node shows: its kind — an unknown node's "?" until it is walked or revealed.</summary>
        public Func<MapNode, NodeKind> Shown { get; }

        /// <summary>The Devil's Compass's button ("COMPASS 2"); null: no compass worn (no button).</summary>
        public string CompassLabel { get; }

        /// <summary>While the compass picks: the nodes of the floor it may step to (they glow apart from the ways); empty otherwise.</summary>
        public IReadOnlyCollection<MapNode> CompassTargets { get; }

        public ChapterMapState(ChapterMap map, MapNode current, IReadOnlyCollection<MapNode> choices, MapNode selected, IReadOnlyList<MapNode> trail,
            string title, string prompt, int coins, IReadOnlyList<string> lines, Func<MapNode, string> describe, string gateLabel = null, int chapter = 1,
            BossBarDisplay bar = null, string barLabel = null, Func<MapNode, NodeKind> shown = null, string compassLabel = null,
            IReadOnlyCollection<MapNode> compassTargets = null)
        {
            CompassLabel = compassLabel;
            CompassTargets = compassTargets ?? Array.Empty<MapNode>();
            Shown = shown ?? (n => n.Kind);
            Bar = bar ?? BossBarDisplay.Hidden;
            BarLabel = barLabel ?? "";
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

        /// <summary>The map's RELICS button (the worn relics and the bag, to look at).</summary>
        event Action RelicsPressed;

        /// <summary>The Devil's Compass's button (shown only while one is worn).</summary>
        event Action CompassPressed;

        bool IsVisible { get; }

        void Show(ChapterMapState state);

        /// <summary>The compass carried the player across the floor: a short flicker from one node to the other.</summary>
        void PlayStep(MapNode from, MapNode to);

        void Hide();
    }

    /// <summary>
    /// The relics of a Phase 2 run (Demo 2.1): five worn (only they act at a table) and five in the bag. At a camp they change places
    /// freely — pick one, pick where it goes — and one may be left behind (asked twice); elsewhere they are only looked at.
    /// </summary>
    public sealed class RelicBagState
    {
        public const int Worn = 5, Bagged = 5, Slots = Worn + Bagged;

        /// <summary>The ten places: 0-4 worn, 5-9 the bag; null for an empty one.</summary>
        public IReadOnlyList<RelicBadge> Places { get; }

        /// <summary>The place picked to move (−1: none) and the keyboard's place.</summary>
        public int Picked { get; }
        public int Cursor { get; }

        /// <summary>A camp: places may change and a relic may be left behind; false: a look only.</summary>
        public bool Editable { get; }

        /// <summary>The worn relics' sum in words ("Win +12%, ante −10%, malice +1 a hand").</summary>
        public string Totals { get; }

        /// <summary>A line of what just happened or what to do.</summary>
        public string Message { get; }

        /// <summary>LEAVE IT BEHIND was pressed once: the next press leaves the picked relic for good.</summary>
        public bool ConfirmingDrop { get; }

        public RelicBagState(IReadOnlyList<RelicBadge> places, int picked, int cursor, bool editable, string totals, string message, bool confirmingDrop)
        {
            Places = places ?? throw new ArgumentNullException(nameof(places));
            if (places.Count != Slots) throw new ArgumentException("Ten places.", nameof(places));
            Picked = picked;
            Cursor = cursor;
            Editable = editable;
            Totals = totals ?? "";
            Message = message ?? "";
            ConfirmingDrop = confirmingDrop;
        }
    }

    public interface IRelicBagView
    {
        /// <summary>A place clicked (0-4 worn, 5-9 the bag).</summary>
        event Action<int> PlacePressed;

        /// <summary>DONE (or Esc), and LEAVE IT BEHIND.</summary>
        event Action DonePressed;
        event Action DropPressed;

        bool IsOpen { get; }

        void Show(RelicBagState state);

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

        /// <summary>Demo 3.5: the option drawn as a relic card (a big one in a row, or a small one); null: a button.</summary>
        public RelicCardData Card { get; }

        public PanelOption(string label, string detail = null, bool enabled = true, bool isLeave = false, RelicCardData card = null)
        {
            Card = card;
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

        /// <summary>A demon's bar under the words (the gate's), with its name; hidden for none.</summary>
        public BossBarDisplay Bar { get; }
        public string BarLabel { get; }

        /// <summary>Demo 3.5: cards only to look at (a treasure's coins, a relic won, the warden's relic waiting), in a row over the answers.</summary>
        public IReadOnlyList<RelicCardData> Showcase { get; }

        /// <summary>Demo 3.5.1: the bar before something shrank it (Lilith's gate: the Last Bribe) — the panel opens on it, coins fly to it and
        /// it shrinks to <see cref="Bar"/>; null: none.</summary>
        public BossBarDisplay BarBefore { get; }

        public PanelCard(string portraitId, string owner, string title, string text, IReadOnlyList<PanelOption> options, int selected = 0,
            BossBarDisplay bar = null, string barLabel = null, IReadOnlyList<RelicCardData> showcase = null, BossBarDisplay barBefore = null)
        {
            BarBefore = barBefore;
            Showcase = showcase ?? Array.Empty<RelicCardData>();
            Bar = bar ?? BossBarDisplay.Hidden;
            BarLabel = barLabel ?? "";
            PortraitId = portraitId;
            Owner = owner ?? "";
            Title = title ?? "";
            Text = text ?? "";
            Options = options ?? Array.Empty<PanelOption>();
            Selected = selected;
        }

        /// <summary>The same panel, the keys pointing at another answer (its cards, bar and words kept).</summary>
        public PanelCard WithSelected(int selected) =>
            new PanelCard(PortraitId, Owner, Title, Text, Options, selected, Bar, BarLabel, Showcase, BarBefore);
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

        /// <summary>Demo 3.4: an Ironman run with progress since its last camp (quitting loses it).</summary>
        bool IronmanUnsaved { get; }

        /// <summary>Demo 3.3: the run that waits, in short (the title menu's card); null with none.</summary>
        ContinueCard ContinueCard { get; }

        /// <summary>Phase 2's own records (for the records screen).</summary>
        Core.Chapters.ChapterRecords Records { get; }

        bool IsVisible { get; }

        /// <summary>Where the run stands, for a bug report; null with no run.</summary>
        FeedbackContext Feedback { get; }

        /// <summary>The music the chapter's screen plays now.</summary>
        string MusicId { get; }

        /// <summary>A new chapter run as this class (a run in progress is abandoned: it counts as damned).</summary>
        void Start(Core.Sinners.SinnerClass sinnerClass);

        /// <summary>A new chapter run as this class at a difficulty (Demo 3.4).</summary>
        void Start(Core.Sinners.SinnerClass sinnerClass, Core.Chapters.Difficulty difficulty);

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
