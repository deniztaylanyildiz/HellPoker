using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Phase 2: a whole run (<see cref="ChapterJourney"/>) — three chapters, each from its first floor to its demon's table, then
    /// Lucifer's. The map (<see cref="IChapterMapView"/>) shows the floors and where the player may go; a panel over it
    /// (<see cref="IChapterPanelView"/>) tells each node — a chapter's opening, the treasure, the black market, a stranger's offer, the
    /// purgatory fire, the gate's tribute, a match's end, a demon's spoils, the run's end — and takes the answer. Floor matches, the
    /// demons' tables and Lucifer's are played at a table of their own (a <see cref="TablePresenter"/> on its own view), which says when
    /// it is done. The run is saved apart from the demo's (<see cref="ChapterArchive"/>) at every node, every answer and every step of a
    /// hand; a hand left in the middle is lost on the next launch. Knows the chapters' rules only through Core; the demo's run is never
    /// touched.
    /// </summary>
    public sealed class ChapterPresenter : IChapterCommands, IChapterSession, IDisposable
    {
        private readonly IChapterMapView _map;
        private readonly IChapterPanelView _panel;

        /// <summary>Builds the chapter's own table (its view and its presenter) the first time a chapter starts.</summary>
        private readonly Func<(TablePresenter presenter, ITableView view)> _buildTable;
        private TablePresenter _table;
        private ITableView _tableView;
        private readonly IScreenTransition _transition;
        private readonly IAudio _audio;
        private readonly GameRules _bossTable;
        private readonly Func<int> _newSeed;
        private readonly ChapterArchive _archive;
        private readonly IRunLogSink _logSink;
        private ChapterRecords _records;

        private ChapterJourney _journey;
        private MapNode _selected;
        private bool _visible;
        private bool _atTable;
        private bool _ended;
        private RunLog _log;

        /// <summary>The current node is done (its match over, its panel answered).</summary>
        private bool _nodeDone = true;

        /// <summary>The panel that waits for its answer (for the save): see <see cref="ChapterSave.Pending"/>.</summary>
        private string _pending;

        /// <summary>The floor match at the table (null at a demon's table and on the map).</summary>
        private FloorTable _match;

        /// <summary>The demon's game (or Lucifer's), once a gate is passed.</summary>
        private HellPokerGame _bossGame;

        private PanelCard _card;
        private readonly List<Action> _answers = new List<Action>();

        public event Action MenuRequested;
        public event Action NewRunRequested;

        /// <param name="bossTable">The numbers of the demons' tables (the soul's worth, the cheats' pace, the deck).</param>
        /// <param name="newSeed">A master seed for each new run.</param>
        /// <param name="buildTable">The chapter's own table (a view and a presenter apart from the demo's), built when first needed.</param>
        /// <param name="archive">Where the run and the records are kept; null: nothing is kept.</param>
        /// <param name="logSink">Where the run's diary is written; null: none.</param>
        public ChapterPresenter(IChapterMapView map, IChapterPanelView panel, Func<(TablePresenter presenter, ITableView view)> buildTable,
            IScreenTransition transition, IAudio audio, GameRules bossTable, Func<int> newSeed, ChapterArchive archive = null,
            IRunLogSink logSink = null, IRelicBagView bag = null)
        {
            _bag = bag;
            if (_bag != null)
            {
                _bag.PlacePressed += BagPlacePressed;
                _bag.DropPressed += BagDropPressed;
                _bag.DonePressed += BagDonePressed;
            }
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _buildTable = buildTable ?? throw new ArgumentNullException(nameof(buildTable));
            _transition = transition;
            _audio = audio ?? NullAudio.Instance;
            _bossTable = bossTable ?? GameRules.Default;
            _newSeed = newSeed ?? throw new ArgumentNullException(nameof(newSeed));
            _archive = archive;
            _logSink = logSink;
            _records = archive?.LoadRecords() ?? new ChapterRecords();

            _map.NodePressed += PressNode;
            _map.MenuPressed += RequestMenu;
            _map.RelicsPressed += ShowRelicsFromMap;
            _map.CompassPressed += PressCompass;
            _panel.OptionPressed += Answer;
            Lang.Changed += OnLanguageChanged;
        }

        private void EnsureTable()
        {
            if (_table != null) return;
            (_table, _tableView) = _buildTable();
            _table.ChapterTableFinished += TableFinished;
            _table.ChapterTableChanged += Save;
            _tableView.MenuPressed += RequestMenu;
        }

        public void Dispose()
        {
            CloseLog();
            _map.NodePressed -= PressNode;
            _map.MenuPressed -= RequestMenu;
            _map.RelicsPressed -= ShowRelicsFromMap;
            _map.CompassPressed -= PressCompass;
            _panel.OptionPressed -= Answer;
            if (_bag != null)
            {
                _bag.PlacePressed -= BagPlacePressed;
                _bag.DropPressed -= BagDropPressed;
                _bag.DonePressed -= BagDonePressed;
            }
            Lang.Changed -= OnLanguageChanged;
            if (_table == null) return;
            _table.ChapterTableFinished -= TableFinished;
            _table.ChapterTableChanged -= Save;
            _tableView.MenuPressed -= RequestMenu;
            _table.Dispose();
        }

        /// <summary>The chapter's table (the keys go to it while it is on screen); null before the first chapter.</summary>
        public ITableCommands Table => _table;

        /// <summary>The whole run (for tests); null before the first.</summary>
        public ChapterJourney Journey => _journey;

        /// <summary>The chapter being played (for tests); null before the first.</summary>
        public ChapterRun Run => _journey?.Run;

        /// <summary>Phase 2's records (for tests and the records screen).</summary>
        public ChapterRecords Records => _records;

        /// <summary>The panel on show (for tests); null when none.</summary>
        public PanelCard Panel => _panel.IsOpen ? _card : null;

        private ChapterRun _run => _journey?.Run;

        public bool HasRun => _journey != null && !_ended;

        /// <summary>A run waits: in memory, or saved from an earlier session.</summary>
        public bool CanContinue => HasRun || (_archive?.HasRun ?? false);

        public bool IsVisible => _visible;

        /// <summary>Where the run stands, for a bug report (null with no run going on): the class, circle, chapter, floor, the log's lines.</summary>
        public FeedbackContext Feedback => _journey == null ? null
            : new FeedbackContext(_journey.Sinner.Id, _journey.Circle, _journey.Stage == JourneyStage.Lucifer ? 4 : _journey.Chapter,
                _run.Current == null ? 0 : _run.Current.Floor + 1, _log?.Lines.ToArray());
        public bool IsMapOpen => _visible && !_atTable;
        public bool IsAtTable => _visible && _atTable;

        /// <summary>The music of the screen: the chapter's demon (Lucifer's at the end).</summary>
        /// <summary>
        /// The music of the chapter's screen: the map's (each chapter's own variation), a floor's table (the demon's music, plainer), the
        /// demon's table, Lucifer's call before his table, his table, and the two endings.
        /// </summary>
        public string MusicId
        {
            get
            {
                if (_endTrack != null) return _endTrack;
                string boss = (_run?.Rules ?? ChapterRules.For(1)).BossId;
                if (_journey?.Stage == JourneyStage.Lucifer) return _atTable ? DealerRoster.LuciferId : SfxIds.LuciferCallMusic;
                if (_pending == "lucifer") return SfxIds.LuciferCallMusic;
                if (_atTable) return _bossGame != null ? boss : SfxIds.FloorMusic(boss);
                return SfxIds.MapMusic(boss);
            }
        }

        /// <summary>The ending's music once a run is over (freedom, or a doom); null while it goes on.</summary>
        private string _endTrack;

        // ------------------------------------------------------------------ the run

        public void Start(SinnerClass sinnerClass) => Start(sinnerClass, 0);

        /// <summary>A new chapter run as this class in <paramref name="circle"/> (the deepest the class has open at most).</summary>
        public void Start(SinnerClass sinnerClass, int circle)
        {
            if (sinnerClass == null) throw new ArgumentNullException(nameof(sinnerClass));
            if (HasRun) Abandon();
            EnsureTable();
            circle = Math.Max(0, Math.Min(circle, _records.CirclesOpen(sinnerClass.Id)));
            _journey = ChapterJourney.Begin(sinnerClass, _newSeed(), circle);
            _journey.RelicAvailable = id => ChapterUnlocks.IsRelicOpen(id, _records);
            _match = null;
            _bossGame = null;
            _atTable = false;
            _ended = false;
            _endTrack = null;
            _nodeDone = true;
            _selected = PickDefault(_run.Choices);
            _visible = true;
            _records.RunStarted();
            _archive?.SaveRecords(_records);
            _log = new RunLog(_logSink?.Version, Lang.Current.ToString(), "phase2", sinnerClass.Id, _journey.Sentence.Years, DateTime.Now);
            Note($"phase 2 run: {sinnerClass.Id}, circle {circle}, {_run.Purse.Coins} coins, sentence {_journey.Sentence.Years} years");
            ShowMap();
            ShowChapterOpening();
        }

        /// <summary>The run that waits: the one in memory, or the saved one (a hand left in the middle is lost now).</summary>
        public void Continue()
        {
            if (HasRun)
            {
                Show();
                return;
            }
            ChapterSave save = _archive?.LoadRun();
            if (save == null) return;
            EnsureTable();
            Resume(save);
        }

        /// <summary>The run in progress is given up: it counts as damned (a new run over it).</summary>
        public void Abandon()
        {
            if (_journey == null || _ended)
            {
                if (_journey == null && _archive?.LoadRun() is ChapterSave saved)
                {
                    _records.RunEnded(JourneyEnd.Abandoned, saved.Lucifer ? 4 : saved.Chapter, saved.ClassId, 0);
                    _archive.SaveRecords(_records);
                    _archive.ClearRun();
                }
                return;
            }
            Note("abandoned for a new run");
            FinishRun(JourneyEnd.Abandoned, show: false);
        }

        public void Show()
        {
            if (_journey == null) return;
            _visible = true;
            if (_atTable && _tableView != null)
            {
                _tableView.SetVisible(true);
                _map.Hide();
            }
            else
            {
                ShowMap();
                if (_card != null) _panel.Show(_card);
            }
        }

        public void Hide()
        {
            _visible = false;
            _map.Hide();
            _panel.Hide();
            _tableView?.SetVisible(false);
        }

        private void RequestMenu()
        {
            if (!_visible) return;
            MenuRequested?.Invoke();
        }

        // ------------------------------------------------------------------ the map

        private void ShowMap()
        {
            if (_journey == null) return;
            _tableView?.SetVisible(false);
            _map.Show(MapState());
        }

        private ChapterMapState MapState()
        {
            ChapterRules rules = _run.Rules;
            var lines = new List<string>
            {
                string.Format(UiText.MapSentenceFormat, _journey.Sentence.Years),
                string.Format(UiText.MapTributeFormat, _run.Tribute)
            };
            if (_run.YearsOwed > 0) lines.Add(UiText.MapOwed);
            lines.Add("");
            lines.Add(UiText.MapRelicsLabel + " " + string.Format(UiText.MapRelicsCountFormat, _run.Effects.CarriedOffered, _run.Effects.MaxCarried,
                _run.Effects.Bag.Count, _run.Effects.MaxBag));
            if (_run.Effects.Relics.Count == 0) lines.Add("  " + UiText.MapNoRelics);
            foreach (string id in _run.Effects.Relics)
            {
                string mark = id == _run.Effects.SilencedCurse ? " (" + UiText.MapSilenced + ")" : id == _run.Effects.AmplifiedRelic ? " (" + UiText.MapDesired + ")" : "";
                lines.Add("  " + UiText.RelicName(id) + mark);
            }
            if (_run.Kit.Upgrades.Count > 0)
            {
                lines.Add(UiText.MapUpgradesLabel);
                foreach (string id in _run.Kit.Upgrades) lines.Add("  " + UiText.UpgradeName(id));
            }
            if (_run.ImpsEyeNext) lines.Add(UiText.MapEyeReady);
            if (_run.NextTableMarks.HidesAll) lines.Add(UiText.MapSpectacleReady);
            if (_run.NextTableMarks.FreeHands > 0) lines.Add(UiText.MapInsomniaReady);

            string title = string.Format(UiText.ChapterTitleFormat, rules.Number, UiText.ChapterName(rules.Number));
            string prompt = _compassPicking ? UiText.CompassPrompt : _mapNotice ?? (_run.Current == null ? UiText.MapPromptStart : UiText.MapPrompt);
            bool calm = !_atTable && !_panel.IsOpen && _nodeDone;
            MapNode[] choices = !calm || _compassPicking ? Array.Empty<MapNode>() : _run.Choices.ToArray();
            MapNode[] targets = calm && _compassPicking ? _run.CompassTargets.ToArray() : Array.Empty<MapNode>();
            bool compass = _run.Effects.CombinedRelics.Extra(RelicExtras.CompassSteps) > 0;
            return new ChapterMapState(_run.Map, _run.Current, choices, _compassPicking ? _compassSelected : _selected, _run.Trail.ToArray(), title, prompt,
                _run.Purse.Coins, lines, Describe, UiText.GateTitle(rules.Number), rules.Number,
                BarAt(_run.Years + _run.YearsOwed, BossShares.For(_journey.Sinner.Id, rules.BossId)), BarLabel(), _run.ShownKind,
                compass ? string.Format(UiText.CompassButtonFormat, _run.CompassStepsLeft) : null, targets);
        }

        // ------------------------------------------------------------------ the Devil's Compass (2.1.1)

        /// <summary>The compass is out: the next node clicked (of the floor the player stands on) is where it steps to.</summary>
        private bool _compassPicking;
        private MapNode _compassSelected;

        /// <summary>The map's COMPASS: out (the floor's other nodes glow) or put away; said why when it cannot be used.</summary>
        private void PressCompass()
        {
            if (!IsMapOpen || IsBagOpen || _journey == null || _ended) return;
            if (_compassPicking)
            {
                PutCompassAway();
                return;
            }
            // Never over a panel or at a node not done yet (a table, an offer waiting).
            if (_panel.IsOpen || !_nodeDone) return;
            string why = _run.CompassStepsLeft <= 0 ? UiText.CompassNone : _run.Current == null ? UiText.CompassNotYet
                : !_run.CompassTargets.Any() ? UiText.CompassNowhere : null;
            if (why != null)
            {
                _mapNotice = why;
                _map.Show(MapState());
                return;
            }
            _compassPicking = true;
            _compassSelected = _run.CompassTargets.OrderBy(n => n.Lane).First();
            _map.Show(MapState());
        }

        private void PutCompassAway()
        {
            _compassPicking = false;
            _compassSelected = null;
            _map.Show(MapState());
        }

        /// <summary>The compass steps to <paramref name="node"/>: the curse's years on the sentence, a flicker across the floor, the way on.</summary>
        private void StepWithCompass(MapNode node)
        {
            MapNode from = _run.Current;
            int years = _run.CompassYears;
            if (!_run.StepWithCompass(node)) return;
            _compassPicking = false;
            _compassSelected = null;
            Note($"chapter {_journey.Chapter} floor {node.Floor + 1}: the compass, lane {from.Lane} -> {node.Lane} (+{years} years, sentence {_journey.Sentence.Years}, " +
                 $"{_run.CompassStepsLeft} steps left)");
            _audio.PlaySfx(SfxIds.Transition);
            _map.PlayStep(from, node);
            _selected = PickDefault(_run.Choices);
            _mapNotice = string.Format(UiText.CompassDoneFormat, years);
            _map.Show(MapState());
            Save();
        }

        /// <summary>The chapter demon's bar, small and without a number (the map's, the gate's): <paramref name="value"/> toward the soul
        /// line of a table that would begin at <paramref name="tableStart"/>; the tick where the run's share began.</summary>
        private BossBarDisplay BarAt(int value, int tableStart)
        {
            ChapterRules rules = _run.Rules;
            int share = BossShares.For(_journey.Sinner.Id, rules.BossId);
            int limit = Math.Max(value + 1, Math.Max(tableStart, share) * rules.SoulLinePercent / 100);
            return new BossBarDisplay(value, share, limit, BarLine.Soul, null);
        }

        private string BarLabel() => UiText.Upper(string.Format(UiText.MapBarLabelFormat, UiText.GenitiveOf(UiText.Dealer(_run.Rules.BossId))));

        /// <summary>A node in words: its name and what waits there.</summary>
        private string Describe(MapNode node)
        {
            ChapterRules rules = _run.Rules;
            string what;
            NodeKind shown = _run.ShownKind(node);
            if (shown == NodeKind.Unknown) return UiText.NodeName(NodeKind.Unknown) + "\n" + UiText.NodeUnknown;
            switch (shown)
            {
                case NodeKind.Table: what = string.Format(UiText.NodeTableFormat, _run.ImpCoinsAt(node.Floor), rules.Ante, rules.AnteStepHands); break;
                case NodeKind.Warden:
                    string warden = UiText.NameInSentence(UiText.Dealer(ChapterCast.WardenOf(rules)));
                    what = string.Format(UiText.NodeWardenFormat, rules.WardenCoins, rules.Ante, rules.AnteStepHands, rules.WardenTollPercent,
                        rules.WardenTollMax, warden) + "\n" + UiText.WardenTrick(rules.BossId);
                    break;
                case NodeKind.Event: what = UiText.NodeEvent; break;
                case NodeKind.BlackMarket: what = UiText.NodeMarket; break;
                case NodeKind.Treasure: what = string.Format(UiText.NodeTreasureFormat, _run.TreasureCoins); break;
                default: what = node.Floor == _run.Map.FloorCount - 1 ? UiText.NodeFire : UiText.NodeCamp; break;
            }
            return UiText.NodeName(shown) + (node.Kind == NodeKind.Unknown ? " (?)" : "") + "\n" + what;
        }

        private static MapNode PickDefault(IEnumerable<MapNode> choices)
        {
            var list = choices.OrderBy(n => n.Lane).ToList();
            return list.Count == 0 ? null : list[(list.Count - 1) / 2];
        }

        private void PressNode(int floor, int lane)
        {
            if (!IsMapOpen || _panel.IsOpen || _journey == null || _ended || !_nodeDone) return;
            if (_compassPicking)
            {
                MapNode target = _run.CompassTargets.FirstOrDefault(n => n.Floor == floor && n.Lane == lane);
                if (target != null) StepWithCompass(target);
                return;
            }
            MapNode node = _run.Choices.FirstOrDefault(n => n.Floor == floor && n.Lane == lane);
            if (node == null) return;
            Go(node);
        }

        private void Go(MapNode node)
        {
            _mapNotice = null;
            _audio.PlaySfx(SfxIds.NodeSelect);
            _run.MoveTo(node);
            _nodeDone = false;
            _selected = PickDefault(_run.Choices);
            if (node.Kind == NodeKind.Unknown) Note($"chapter {_journey.Chapter} floor {node.Floor + 1}: unknown, it was {node.Hidden}");
            switch (ChapterRun.KindOf(node))
            {
                case NodeKind.Table:
                case NodeKind.Warden:
                    StartMatch(_run.OpenTable());
                    return;
                case NodeKind.Treasure:
                    _run.TakeTreasure();
                    Note($"chapter {_journey.Chapter} floor {node.Floor + 1}: treasure +{_run.Rules.TreasureCoins} (purse {_run.Purse.Coins})");
                    ShowTreasure();
                    break;
                case NodeKind.BlackMarket:
                    ShowMarket(_run.OpenMarket(), null);
                    break;
                case NodeKind.Event:
                    IFloorEvent offer = _run.DrawEvent();
                    if (offer == null) Onward();
                    else ShowOffer(offer);
                    break;
                default:
                    ShowFire();
                    break;
            }
            ShowMap();
            Save();
        }

        /// <summary>Back to the map after a node — or, past the last floor, on to the gate.</summary>
        private void Onward()
        {
            ClosePanel();
            _nodeDone = true;
            // The Jester's twenty jokers after the first: a reward to choose before the way goes on.
            if (_run.Sinner.JackpotRewardsWaiting > 0)
            {
                ShowJackpotReward();
                return;
            }
            if (_run.AtGate)
            {
                GateArrival arrival = _run.ArriveAtGate();
                if (arrival.LoanRepaid >= 0 || arrival.InsurerGone)
                    Note($"gate: loan {(arrival.LoanRepaid > 0 ? "+" + arrival.LoanRepaid : arrival.LoanRepaid == 0 ? "fled" : "-")}, insurer {(arrival.InsurerGone ? "gone" : "-")}");
                ShowGate();
                Save();
                return;
            }
            ShowMap();
            Save();
        }

        /// <summary>The Jester's twenty jokers (after the Rattle's): a common relic, or the coins.</summary>
        private void ShowJackpotReward()
        {
            ShowPanel(new PanelCard(null, UiText.SinnerName(_run.Sinner.Id), UiText.JackpotRewardTitle, UiText.JackpotRewardText,
                    new[] { new PanelOption(UiText.JackpotRewardRelic), new PanelOption(string.Format(UiText.WardenCoinsFormat, ChapterRun.JackpotCoins)) }),
                new Action[]
                {
                    () =>
                    {
                        string relic = _run.TakeJackpotRelic();
                        Note($"twenty jokers: {(relic ?? "the coins")}");
                        if (_run.WardenRelicWaiting != null) ShowWardenRelic();
                        else Onward();
                    },
                    () =>
                    {
                        _run.TakeJackpotCoins();
                        Note($"twenty jokers: +{ChapterRun.JackpotCoins} coins");
                        Onward();
                    }
                }, "jackpot");
        }

        // ------------------------------------------------------------------ the panels

        private void ShowPanel(PanelCard card, IReadOnlyList<Action> answers, string pending = null)
        {
            _compassPicking = false;
            _answers.Clear();
            _answers.AddRange(answers);
            _card = card;
            _pending = pending;
            _panel.Show(card);
            if (pending == "lucifer") _audio.PlayMusic(MusicId);   // the call: the music holds its breath
            if (!_atTable) _map.Show(MapState());   // the choices go dark under a panel
        }

        private void ClosePanel()
        {
            _card = null;
            _pending = null;
            _answers.Clear();
            _panel.Hide();
        }

        private void Answer(int index)
        {
            if (_card == null || index < 0 || index >= _answers.Count) return;
            _answers[index]?.Invoke();
            Save();
        }

        /// <summary>A chapter opens: its name, its demon, what waits below.</summary>
        private void ShowChapterOpening()
        {
            _audio.PlaySfx(SfxIds.ChapterTitle);
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            string text = string.Format(UiText.IntroFormat, rules.Floors, boss, _run.Tribute, rules.YearsPerMissingCoin, _run.Purse.Coins);
            // A later chapter's purse was topped up to the class's start: said under the opening words.
            if (_journey.Chapter > 1 && _journey.PurseToppedUp > 0) text += "\n" + string.Format(UiText.PurseToppedUpFormat, _run.Purse.Coins);
            if (_journey.Chapter > 1 && _journey.Sown.HasValue) text += "\n" + string.Format(UiText.SownFormat, _journey.Sown.Value);
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, string.Format(UiText.ChapterTitleFormat, rules.Number,
                    UiText.ChapterName(rules.Number)), text, new[] { new PanelOption(UiText.PanelDescend) }),
                new Action[] { () => { ClosePanel(); ShowMap(); } }, "chapter");
            Save();
        }

        private void ShowTreasure()
        {
            ShowPanel(new PanelCard("Events/treasure", null, UiText.TreasureTitle, string.Format(UiText.TreasureTextFormat, _run.TreasureCoins),
                new[] { new PanelOption(UiText.TreasureTake) }), new Action[] { Onward }, "treasure");
        }

        // ------------------------------------------------------------------ the black market

        private void ShowMarket(BlackMarket market, string bought)
        {
            if (bought != null) _audio.PlaySfx(SfxIds.MarketBuy);
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            MarketVendor vendor = market.Vendor;
            bool full = market.SellsNoMore;
            void Add(PanelOption option, Action answer)
            {
                options.Add(option);
                answers.Add(answer);
            }

            // The relics on the stall (the bone merchant's cost years too).
            foreach (RelicOffer offer in market.Relics)
            {
                string id = offer.RelicId;
                bool can = market.CanBuyRelic(id);
                string detail = UiText.RelicPanelDetail(id) + (vendor == MarketVendor.BoneMerchant
                    ? "\n" + string.Format(UiText.MarketBoneDetailFormat, _run.Scaled(BlackMarket.BoneYears)) : "") +
                    (can ? "" : "\n" + (!full ? UiText.MarketNoCoins : UiText.MarketFull));
                Add(new PanelOption(string.Format(UiText.MarketRelicFormat, UiText.RelicName(id), offer.Price), detail, can), () =>
                {
                    if (!market.BuyRelic(id)) return;
                    Note($"black market ({vendor}): bought {id} ({ChapterRelics.RarityOf(id)}) for {offer.Price} (purse {_run.Purse.Coins})");
                    _records.RelicBought();
                    _archive?.SaveRecords(_records);
                    ShowMarket(market, UiText.RelicName(id));
                });
            }
            var held = _run.Effects.AllHeld.Where(r => !RelicRoster.IsReward(r)).ToArray();
            switch (vendor)
            {
                case MarketVendor.Smuggler:
                    foreach (string id in held)
                    {
                        string relic = id;
                        bool can = market.CanDropRelic(relic);
                        Add(new PanelOption(string.Format(UiText.MarketDropFormat, UiText.RelicName(relic), market.DropPrice),
                            UiText.MarketDropDetail + (can ? "" : "\n" + UiText.MarketNoCoins), can), () =>
                        {
                            if (!market.DropRelic(relic)) return;
                            Note($"relics: {relic} left behind (the smuggler's, {market.DropPrice} coins; purse {_run.Purse.Coins})");
                            ShowMarket(market, null, string.Format(UiText.RelicLeftFormat, UiText.RelicName(relic)));
                        });
                    }
                    break;
                case MarketVendor.Pawnshop:
                    foreach (string id in held)
                    {
                        string relic = id;
                        Add(new PanelOption(string.Format(UiText.MarketSellFormat, UiText.RelicName(relic), market.SellPrice(relic)), UiText.MarketSellDetail), () =>
                        {
                            int price = market.SellPrice(relic);
                            if (!market.SellRelic(relic)) return;
                            Note($"pawnshop: sold {relic} for {price} (purse {_run.Purse.Coins})");
                            ShowMarket(market, null, string.Format(UiText.RelicSoldFormat, UiText.RelicName(relic), price));
                        });
                    }
                    if (market.CanPawn)
                        Add(new PanelOption(string.Format(UiText.MarketPawnFormat, market.PawnCoinsNow), string.Format(UiText.MarketPawnDetailFormat, market.PawnYearsNow)), () =>
                        {
                            if (!market.Pawn()) return;
                            Note($"pawnshop: the soul pawned, +{market.PawnCoinsNow} coins, +{market.PawnYearsNow} years (sentence {_journey.Sentence.Years})");
                            ShowMarket(market, null);
                        });
                    break;
                case MarketVendor.Alchemist:
                    foreach (RelicRarity rarity in market.Transmutable)
                    {
                        RelicRarity r = rarity;
                        Add(new PanelOption(string.Format(UiText.MarketTransmuteFormat, UiText.Upper(UiText.RarityName(r)),
                            UiText.Upper(UiText.RarityName(r == RelicRarity.Common ? RelicRarity.Rare : RelicRarity.Legendary))), UiText.MarketTransmuteDetail),
                            () => ShowTransmute(market, r, null));
                    }
                    bool cleanse = held.Any(market.CanCleanse);
                    Add(new PanelOption(string.Format(UiText.MarketCleanseFormat, market.CleanseCost), UiText.MarketCleanseDetail +
                        (cleanse ? "" : "\n" + UiText.MarketNoCoins), cleanse), () =>
                    {
                        if (cleanse) ShowCleanse(market);
                    });
                    break;
                case MarketVendor.GamblersStall:
                    if (market.Box == null)
                    {
                        bool box = market.CanBuyBox;
                        Add(new PanelOption(string.Format(UiText.MarketBoxFormat, market.BoxCost), UiText.MarketBoxDetail +
                            (box ? "" : "\n" + (full ? UiText.MarketFull : UiText.MarketNoCoins)), box), () =>
                        {
                            if (!market.BuyBox()) return;
                            Note($"gambler's stall: a box ({market.Box})");
                            ShowMarket(market, null);
                        });
                    }
                    else
                    {
                        Add(new PanelOption(UiText.MarketBoxOpen, UiText.MarketBoxDetail), () =>
                        {
                            string relic = market.OpenBox();
                            Note($"gambler's stall: the box held {relic ?? "nothing"}");
                            if (_run.WardenRelicWaiting != null) { ShowWardenRelic(); return; }
                            ShowMarket(market, relic != null ? UiText.RelicName(relic) : null);
                        });
                        Add(new PanelOption(UiText.MarketBoxDouble, UiText.MarketBoxDoubleDetail), () =>
                        {
                            IReadOnlyList<string> won = market.DoubleOrNothing();
                            Note($"gambler's stall: double or nothing -> {(won.Count == 0 ? "lost" : string.Join(", ", won))}");
                            if (_run.WardenRelicWaiting != null) { ShowWardenRelic(); return; }
                            ShowMarket(market, won.Count == 0 ? null : string.Join(", ", won.Select(UiText.RelicName)), won.Count == 0 ? UiText.MarketBoxLost : null);
                        });
                    }
                    break;
                case MarketVendor.Tailor:
                    foreach (string upgrade in market.Upgrades)
                    {
                        string u = upgrade;
                        bool can = market.CanBuyUpgrade(u);
                        Add(new PanelOption(string.Format(UiText.MarketUpgradeFormat, UiText.UpgradeName(u), market.UpgradeCost),
                            UiText.UpgradeText(u) + (can ? "" : "\n" + UiText.MarketNoCoins), can), () =>
                        {
                            if (!market.BuyUpgrade(u)) return;
                            Note($"tailor: upgrade {u} (purse {_run.Purse.Coins})");
                            ShowMarket(market, UiText.UpgradeName(u));
                        });
                    }
                    break;
            }
            if (market.OffersEye)
            {
                bool eye = market.CanBuyImpsEye;
                Add(new PanelOption(string.Format(UiText.MarketEyeFormat, market.EyePrice),
                    UiText.MarketEyeDetail + (eye ? "" : "\n" + (_run.ImpsEyeNext ? UiText.MarketEyeWaits : UiText.MarketNoCoins)), eye), () =>
                {
                    if (market.BuyImpsEye()) ShowMarket(market, UiText.MarketEyeName);
                });
            }
            if (market.OffersJokers)
            {
                string jokers = string.Format(UiText.MarketJokerDetailFormat, _run.Sinner.Jokers);
                Add(new PanelOption(string.Format(UiText.MarketJokerInFormat, market.JokerServicePrice), jokers, market.CanChangeJokers(1)), () =>
                {
                    if (market.ChangeJokers(1)) ShowMarket(market, null);
                });
                Add(new PanelOption(string.Format(UiText.MarketJokerOutFormat, market.JokerServicePrice), jokers, market.CanChangeJokers(-1)), () =>
                {
                    if (market.ChangeJokers(-1)) ShowMarket(market, null);
                });
            }
            // Every market: the haggle, once; the stall turned over, once.
            if (!market.Haggled)
                Add(new PanelOption(UiText.MarketHaggle, UiText.MarketHaggleDetail), () =>
                {
                    bool won = market.Haggle();
                    Note($"black market ({vendor}): haggled, prices {market.PricePercent}%");
                    ShowMarket(market, null, won ? UiText.VendorHaggleWon(vendor) : UiText.VendorHaggleLost(vendor));
                });
            if (!market.Rerolled && vendor != MarketVendor.GamblersStall)
            {
                bool can = market.CanReroll;
                Add(new PanelOption(string.Format(UiText.MarketRerollFormat, market.RerollCost), UiText.MarketRerollDetail + (can ? "" : "\n" + UiText.MarketNoCoins), can), () =>
                {
                    if (market.Reroll()) ShowMarket(market, null, UiText.MarketRerolled);
                });
            }
            Add(new PanelOption(UiText.PanelLeave, UiText.MarketLeaveDetail, isLeave: true), Onward);

            string text = string.Format(UiText.VendorGreetingFormat(vendor), _run.Purse.Coins);
            if (vendor == MarketVendor.Smuggler) text += "\n" + string.Format(UiText.MarketWardenTipFormat, market.NextWardenPurse);
            if (vendor == MarketVendor.GamblersStall && market.Box != null)
                text += "\n" + string.Format(UiText.MarketBoxBoughtFormat, UiText.RarityName(market.Box.Value));
            if (_marketLine != null) text += "\n" + _marketLine;
            if (bought != null) text += "\n" + string.Format(UiText.MarketBoughtFormat, bought);
            string title = UiText.VendorTitle(vendor);
            int selected = _card != null && _card.Title == title ? Math.Min(_card.Selected, options.Count - 1) : options.Count - 1;
            ShowPanel(new PanelCard("Events/market_" + vendor.ToString().ToLowerInvariant(), UiText.VendorOwner(vendor), title, text, options, selected),
                answers, "market");
            _marketLine = null;
            if (!_atTable) _map.Show(MapState());   // the purse and the relics on the side change with every purchase
        }

        /// <summary>A line the market says once (the haggle's answer, a box lost).</summary>
        private string _marketLine;

        private void ShowMarket(BlackMarket market, string bought, string line)
        {
            _marketLine = line;
            ShowMarket(market, bought);
        }

        /// <summary>The alchemist's crucible: two relics of <paramref name="rarity"/>, picked one after the other.</summary>
        private void ShowTransmute(BlackMarket market, RelicRarity rarity, string first)
        {
            var candidates = market.HeldOf(rarity).Where(id => id != first).ToArray();
            var options = candidates.Select(id => new PanelOption(UiText.RelicName(id), UiText.RelicPanelDetail(id))).ToList();
            var answers = candidates.Select(id => (Action)(() =>
            {
                if (first == null)
                {
                    ShowTransmute(market, rarity, id);
                    return;
                }
                string made = market.Transmute(first, id);
                Note($"alchemist: {first} + {id} -> {made}");
                ShowMarket(market, null, made != null ? string.Format(UiText.MarketTransmutedFormat, UiText.RelicName(made)) : null);
            })).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(() => ShowMarket(market, null));
            ShowPanel(new PanelCard("Events/market_alchemist", UiText.VendorOwner(MarketVendor.Alchemist), UiText.VendorTitle(MarketVendor.Alchemist),
                first == null ? UiText.MarketTransmuteWhich : string.Format(UiText.MarketTransmuteSecondFormat, UiText.RelicName(first)), options), answers, "market");
        }

        private void ShowCleanse(BlackMarket market)
        {
            var candidates = _run.Effects.AllHeld.Where(market.CanCleanse).ToArray();
            var options = candidates.Select(id => new PanelOption(UiText.RelicName(id), "− " + UiText.RelicCurse(id))).ToList();
            var answers = candidates.Select(id => (Action)(() =>
            {
                if (!market.Cleanse(id)) return;
                Note($"alchemist: {id}'s curse burned away");
                ShowMarket(market, null, string.Format(UiText.MarketCleansedFormat, UiText.RelicName(id)));
            })).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(() => ShowMarket(market, null));
            ShowPanel(new PanelCard("Events/market_alchemist", UiText.VendorOwner(MarketVendor.Alchemist), UiText.VendorTitle(MarketVendor.Alchemist),
                UiText.MarketCleanseWhich, options), answers, "market");
        }

        // ------------------------------------------------------------------ an offer

        private string OfferText(IFloorEvent offer)
        {
            string boss = UiText.NameInSentence(UiText.Dealer(_run.Rules.BossId));
            string format = UiText.FloorEventTextFormat(offer.Id);
            switch (offer.Id)
            {
                case FloorEventIds.Usurer: return string.Format(format, PurgatoryUsurer.Coins, PurgatoryUsurer.Years, boss);
                case FloorEventIds.MammonsLedger: return string.Format(format, MammonsLedger.YearsNow, MammonsLedger.YearsBack);
                case FloorEventIds.GamblerGhost: return string.Format(format, _run.Purse.Coins / 2, _run.Rules.MultiplierCap);
                case FloorEventIds.LyingWitness: return string.Format(format, LyingWitness.Price, LyingWitness.LiePercent);
                case FloorEventIds.Spectacle: return string.Format(format, Spectacle.WinPercent / 100.0);
                case FloorEventIds.FalseCoin: return string.Format(format, FalseCoin.Coins, FalseCoin.Years);
                case FloorEventIds.NightBargain: return string.Format(format, NightBargain.Coins, NightBargain.Years);
                case FloorEventIds.Insomnia: return string.Format(format, Insomnia.Hands);
                default: return format;
            }
        }

        private void ShowOffer(IFloorEvent offer)
        {
            _audio.PlaySfx(SfxIds.EventOpen);
            if (offer is IChoiceEvent choice)
            {
                ShowChoiceOffer(choice, eyeOpen: false);
                return;
            }
            string portrait = offer.Id == FloorEventIds.MammonsLedger ? _run.Rules.BossId : "Events/" + offer.Id;
            ShowPanel(new PanelCard(portrait, UiText.FloorEventOwner(offer.Id), UiText.FloorEventTitle(offer.Id), OfferText(offer),
                    new[] { new PanelOption(UiText.EventAccept), new PanelOption(UiText.EventPass, isLeave: true) }),
                new Action[]
                {
                    () =>
                    {
                        if (offer is Desire desire && _run.Effects.Relics.Count > 1)
                        {
                            ShowDesireChoice(desire);
                            return;
                        }
                        TakeOffer(offer);
                    },
                    () =>
                    {
                        Note($"offer {offer.Id}: passed");
                        Onward();
                    }
                }, "event:" + offer.Id);
        }

        // ------------------------------------------------------------------ Demo 2.1's offers: choices, dice, a result

        /// <summary>
        /// A stranger of Demo 2.1: the offer (a swindler's small hint always; the trap plainly for the Warlock's prophecy and the Liar's
        /// Tongue, or once the Summoner's imp has looked for coins), its choices, and the result on a panel of its own.
        /// </summary>
        private void ShowChoiceOffer(IChoiceEvent offer, bool eyeOpen)
        {
            string text = string.Format(UiText.OfferText(offer.Id), UiText.OfferArgs(offer, _run));
            if (_offerLine != null)
            {
                text += "\n" + _offerLine;
                _offerLine = null;
            }
            if (offer.Family == EventFamily.Scam)
            {
                string hint = UiText.OfferHint(offer.Id), trap = UiText.OfferTrap(offer.Id);
                if (hint != null) text += "\n" + hint;
                if (trap != null && ChoiceEventDeck.SeesTraps(_run)) text += "\n" + string.Format(UiText.ProphecyTrapFormat, trap);
                else if (trap != null && eyeOpen) text += "\n" + string.Format(UiText.EyeTrapFormat, trap);
            }
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            foreach (string choice in offer.Choices(_run))
            {
                string c = choice;
                bool relic = c.StartsWith(EventChoices.GivePrefix, StringComparison.Ordinal) || c.StartsWith(EventChoices.TakePrefix, StringComparison.Ordinal);
                string id = relic ? c.Substring(c.IndexOf(':') + 1) : null;
                string label = relic ? UiText.RelicName(id) : UiText.OfferChoice(offer.Id, c, offer.Cost(_run, c));
                string detail = relic ? UiText.RelicPanelDetail(id) : UiText.OfferChoiceDetail(offer.Id, c);
                bool can = offer.CanChoose(_run, c);
                if (!can && offer.Cost(_run, c) > 0) detail = (detail == null ? "" : detail + "\n") + UiText.MarketNoCoins;
                options.Add(new PanelOption(label, detail, can, isLeave: c == EventChoices.Pass));
                answers.Add(() =>
                {
                    if (offer.CanChoose(_run, c)) ChooseOffer(offer, c);
                });
            }
            // The Summoner's imp looks a swindler over, once, for coins.
            if (offer.Family == EventFamily.Scam && !eyeOpen && _run.Sinner.Id == Summoner.ClassId && UiText.OfferTrap(offer.Id) != null &&
                !ChoiceEventDeck.SeesTraps(_run))
            {
                int price = SummonerEyePrice;
                bool can = _run.Purse.Coins > price;
                options.Insert(Math.Max(0, options.Count - 1), new PanelOption(string.Format(UiText.OfferEyeFormat, price),
                    UiText.OfferEyeDetail + (can ? "" : "\n" + UiText.MarketNoCoins), can));
                answers.Insert(Math.Max(0, answers.Count - 1), () =>
                {
                    if (!_run.Purse.TrySpend(price)) return;
                    Note($"offer {offer.Id}: the imp's eye ({price} coins)");
                    ShowChoiceOffer(offer, eyeOpen: true);
                });
            }
            ShowPanel(new PanelCard("Events/" + offer.Id, UiText.OfferOwner(offer.Id), UiText.OfferTitle(offer.Id), text, options), answers,
                "event:" + offer.Id);
        }

        /// <summary>The Summoner's eye on a swindler: these coins.</summary>
        public const int SummonerEyePrice = 5;

        /// <summary>A line the next offer panel says once (the relic given in a sin swap).</summary>
        private string _offerLine;

        /// <summary>A line the map says once, in place of its prompt (a relic left behind for a warden's or a demon's); null: none.</summary>
        private string _mapNotice;

        /// <summary>A relic left the run by the player's choice: the log and the map say so.</summary>
        private void RelicLeft(string id, string why)
        {
            if (id == null) return;
            Note($"relics: {id} left behind ({why})");
            _mapNotice = string.Format(UiText.RelicLeftFormat, UiText.RelicName(id));
        }

        private void ChooseOffer(IChoiceEvent offer, string choice)
        {
            EventOutcome outcome = offer.Choose(_run, choice);
            Note($"offer {offer.Id}: {choice} -> {outcome.Result}" + (outcome.Coins != 0 ? $", {Signed(outcome.Coins)} coins" : "") +
                 (outcome.Years != 0 ? $", {Signed(outcome.Years)} years" : "") + (outcome.Relic != null ? $", relic {outcome.Relic}" : "") +
                 (outcome.Upgrade != null ? $", upgrade {outcome.Upgrade}" : "") + $" (purse {_run.Purse.Coins}, sentence {_journey.Sentence.Years})");
            if (outcome.Waits)
            {
                // The swap's two offers — the relic given is gone, and the panel says so.
                if (outcome.Result == "give" && outcome.Relic != null)
                {
                    Note($"relics: {outcome.Relic} left behind (given to the sin trader)");
                    _offerLine = string.Format(UiText.RelicLeftFormat, UiText.RelicName(outcome.Relic));
                }
                ShowChoiceOffer(offer, eyeOpen: false);
                return;
            }
            if (outcome.OpensTable && _run.PendingChase != null)
            {
                StartMatch(_run.PendingChase);
                return;
            }
            if (choice == EventChoices.Pass)
            {
                if (outcome.Result == "sealed") _mapNotice = string.Format(UiText.ProphecySealFormat, outcome.Coins);
                Onward();
                return;
            }
            ShowOfferResult(offer, outcome);
        }

        private void ShowOfferResult(IChoiceEvent offer, EventOutcome o)
        {
            var lines = new List<string> { string.Format(UiText.OfferResult(offer.Id, o.Result), o.Value) };
            if (offer.Id == ChoiceEventIds.BlindSeer)
            {
                string rule = UiText.SeerRule(_run.Rules.BossId);
                if (rule != null) lines.Add(string.Format(UiText.SeerRuleFormat, rule));
            }
            if (o.Coins != 0) lines.Add(string.Format(UiText.OfferCoinsFormat, Signed(o.Coins)));
            if (o.Years != 0) lines.Add(string.Format(UiText.OfferYearsFormat, Signed(o.Years)));
            if (o.Relic != null && o.Result != "give" && o.Result != "none")
                lines.Add(string.Format(UiText.OfferRelicFormat, UiText.RelicName(o.Relic), UiText.RelicGift(o.Relic), UiText.RelicCurse(o.Relic)));
            if (o.Upgrade != null) lines.Add(string.Format(UiText.OfferUpgradeFormat, UiText.UpgradeName(o.Upgrade), UiText.UpgradeText(o.Upgrade)));
            ShowPanel(new PanelCard("Events/" + offer.Id, UiText.OfferOwner(offer.Id), UiText.OfferTitle(offer.Id), string.Join("\n", lines),
                    new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { () => { if (_run.WardenRelicWaiting != null) ShowWardenRelic(); else Onward(); } }, "event.done");
            if (!_atTable) _map.Show(MapState());
        }

        private void TakeOffer(IFloorEvent offer)
        {
            offer.Accept(_run);
            Note($"offer {offer.Id}: taken (purse {_run.Purse.Coins}, owed {_run.YearsOwed})");
            ClosePanel();
            if (_run.PendingGamble != null) StartMatch(_run.PendingGamble);
            else Onward();
        }

        private void ShowDesireChoice(Desire desire)
        {
            var relics = _run.Effects.Relics.ToArray();
            var options = relics.Select(id => new PanelOption(UiText.RelicName(id), string.Format(UiText.MarketRelicDetailFormat, UiText.RelicGift(id),
                UiText.RelicCurse(id)))).ToList();
            var answers = relics.Select(id => (Action)(() =>
            {
                desire.RelicId = id;
                TakeOffer(desire);
            })).ToList();
            ShowPanel(new PanelCard("Events/" + desire.Id, UiText.FloorEventOwner(desire.Id), UiText.DesireWhich, UiText.DesireWhichText, options),
                answers, "event:" + desire.Id);
        }

        // ------------------------------------------------------------------ the purgatory fire

        private void ShowFire()
        {
            _audio.PlaySfx(SfxIds.Fire);
            ShowCamp(done: null);
        }

        /// <summary>
        /// The Purgatory Camp: one action (rest, whet the soul, a rite, the forge) — from the fourth circle on, two of them are offered —
        /// and, before or after it, the relics arranged as often as the player likes (not an action). <paramref name="done"/>: the words
        /// of the action taken; null while none is.
        /// </summary>
        private void ShowCamp(string done)
        {
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            string genitive = UiText.GenitiveInSentence(UiText.Dealer(rules.BossId));
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            if (done == null)
            {
                foreach (CampAction action in _run.CampOffers)
                {
                    switch (action)
                    {
                        case CampAction.Rest:
                            options.Add(new PanelOption(UiText.CampRest, string.Format(UiText.CampRestDetailFormat, _run.RestYears, genitive, _run.RestBar)));
                            answers.Add(() => Tend(FireChoice.RestByTheFire, null, string.Format(UiText.CampRestedFormat, _run.RestYears)));
                            break;
                        case CampAction.Whet:
                            bool whet = _run.CanTend(FireChoice.WhetSoul);
                            options.Add(new PanelOption(UiText.CampWhet, string.Format(UiText.CampWhetDetailFormat, genitive, ChapterRun.WhetBarPercent) +
                                (whet ? "" : "\n" + UiText.FireWhetNone), whet));
                            answers.Add(() =>
                            {
                                if (whet) Tend(FireChoice.WhetSoul, null, null);
                            });
                            break;
                        case CampAction.Rite:
                            options.Add(new PanelOption(UiText.CampRite, UiText.CampRiteDetail));
                            answers.Add(ShowRite);
                            break;
                        default:
                            bool forge = _run.CanTend(FireChoice.Forge);
                            options.Add(new PanelOption(UiText.CampForge, UiText.CampForgeDetail + (forge ? "" : "\n" + UiText.CampForgeNone), forge));
                            answers.Add(() =>
                            {
                                if (forge) ShowForge();
                            });
                            break;
                    }
                }
            }
            if (_bag != null && _run.Effects.AllHeld.Any(r => !RelicRoster.IsReward(r)))
            {
                options.Add(new PanelOption(UiText.CampArrange, UiText.CampArrangeDetail));
                answers.Add(() => OpenBag(editable: true, () => ShowCamp(done)));
            }
            if (done != null)
            {
                options.Add(new PanelOption(UiText.PanelOn));
                answers.Add(Onward);
            }
            string text = done ?? string.Format(UiText.CampTextFormat, boss) + (_run.CampOffers.Count < 4 ? "\n" + UiText.CampTwoNote : "");
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.CampTitle, text, options), answers, done == null ? "fire" : "fire.done");
        }

        private void ShowRite()
        {
            string boss = UiText.NameInSentence(UiText.Dealer(_run.Rules.BossId));
            bool silence = _run.CanTend(FireChoice.SilenceCurse);
            var options = new List<PanelOption>
            {
                new PanelOption(UiText.FireBreak, string.Format(UiText.FireBreakDetailFormat, boss)),
                new PanelOption(UiText.FireSilence, UiText.FireSilenceDetail + (silence ? "" : "\n" + UiText.FireSilenceNone), silence),
                new PanelOption(UiText.Back, null, isLeave: true)
            };
            var answers = new List<Action>
            {
                () => Tend(FireChoice.BreakFirstCheat, null, UiText.FireDoneBreak),
                () =>
                {
                    if (!silence) return;
                    var relics = _run.Effects.Relics.ToArray();
                    if (relics.Length == 1) Tend(FireChoice.SilenceCurse, relics[0], string.Format(UiText.FireDoneSilenceFormat, UiText.RelicName(relics[0])));
                    else ShowSilenceChoice(relics);
                },
                () => ShowCamp(null)
            };
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.CampRiteWhich, UiText.CampRiteText, options), answers, "fire");
        }

        private void ShowForge()
        {
            var relics = _run.Forgeable.ToArray();
            var options = relics.Select(id => new PanelOption(UiText.RelicName(id), "+ " + UiText.RelicGift(id))).ToList();
            var answers = relics.Select(id => (Action)(() => Tend(FireChoice.Forge, id, string.Format(UiText.CampForgedFormat, UiText.RelicName(id))))).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(() => ShowCamp(null));
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.CampForgeWhich, UiText.CampForgeText, options), answers, "fire");
        }

        private void ShowSilenceChoice(IReadOnlyList<string> relics)
        {
            var options = relics.Select(id => new PanelOption(UiText.RelicName(id), "− " + UiText.RelicCurse(id))).ToList();
            var answers = relics.Select(id => (Action)(() => Tend(FireChoice.SilenceCurse, id,
                string.Format(UiText.FireDoneSilenceFormat, UiText.RelicName(id))))).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(ShowRite);
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireSilenceWhich, UiText.FireSilenceWhichText, options), answers, "fire");
        }

        private void Tend(FireChoice choice, string relicId, string done)
        {
            if (!_run.TendFire(choice, relicId)) return;
            if (choice == FireChoice.WhetSoul && _run.FireUpgrade != null)
                done = string.Format(UiText.FireDoneWhetFormat, UiText.UpgradeName(_run.FireUpgrade), UiText.UpgradeText(_run.FireUpgrade));
            Note($"camp (floor {(_run.Current?.Floor ?? 0) + 1}): {choice}{(relicId != null ? " " + relicId : "")}{(choice == FireChoice.WhetSoul ? " " + _run.FireUpgrade : "")}" +
                 $" (sentence {_journey.Sentence.Years}, next bar {(_run.CampBarPercent >= 0 ? "+" : "")}{_run.CampBarPercent}%)");
            ShowCamp(done);
        }

        // ------------------------------------------------------------------ the relics: worn and in the bag (Demo 2.1)

        private readonly IRelicBagView _bag;
        private bool _bagEditable;
        private int _bagPicked = -1, _bagCursor;
        private bool _bagConfirm;
        private string _bagMessage;
        private Action _bagClosed;

        /// <summary>The relics' view is open (the keys go to it).</summary>
        public bool IsBagOpen => _bag != null && _bag.IsOpen;

        /// <summary>The worn relics (an earned one apart) and the bag, as the ten places.</summary>
        private string[] BagPlaces()
        {
            var places = new string[RelicBagState.Slots];
            var worn = _run.Effects.Relics.Where(r => !RelicRoster.IsReward(r)).ToList();
            for (int i = 0; i < RelicBagState.Worn && i < worn.Count; i++) places[i] = worn[i];
            for (int i = 0; i < RelicBagState.Bagged && i < _run.Effects.Bag.Count; i++) places[RelicBagState.Worn + i] = _run.Effects.Bag[i];
            return places;
        }

        private void OpenBag(bool editable, Action closed)
        {
            if (_bag == null) return;
            _bagEditable = editable;
            _bagPicked = -1;
            _bagCursor = 0;
            _bagConfirm = false;
            _bagMessage = editable ? UiText.BagHint : UiText.BagLookOnly;
            _bagClosed = closed;
            ShowBag();
        }

        private void ShowBag()
        {
            string[] places = BagPlaces();
            var badges = places.Select(id => id == null ? null : new RelicBadge(id, UiText.RelicName(id),
                UiText.RarityName(ChapterRelics.RarityOf(id)) + "\n" + string.Format(UiText.RelicDescriptionFormat, UiText.RelicGift(id), UiText.RelicCurse(id)),
                rarity: ChapterRelics.RarityOf(id), forged: _run.Effects.Forged.Contains(id))).ToList();
            _bag.Show(new RelicBagState(badges, _bagPicked, _bagCursor, _bagEditable, UiText.RelicTotals(_run.Effects.CombinedRelics), _bagMessage, _bagConfirm));
        }

        private void BagPlacePressed(int place)
        {
            if (!IsBagOpen || place < 0 || place >= RelicBagState.Slots) return;
            _bagCursor = place;
            _bagConfirm = false;
            string[] places = BagPlaces();
            if (!_bagEditable)
            {
                ShowBag();
                return;
            }
            if (_bagPicked < 0)
            {
                if (places[place] != null) _bagPicked = place;
                ShowBag();
                return;
            }
            if (_bagPicked == place)
            {
                _bagPicked = -1;
                ShowBag();
                return;
            }
            string from = places[_bagPicked], to = places[place];
            bool fromWorn = _bagPicked < RelicBagState.Worn, toWorn = place < RelicBagState.Worn;
            if (fromWorn != toWorn)
            {
                string worn = fromWorn ? from : to, bagged = fromWorn ? to : from;
                bool moved = worn != null && bagged != null ? _run.Effects.Swap(worn, bagged)
                    : worn != null ? _run.Effects.Unequip(worn) : _run.Effects.Equip(bagged);
                if (moved)
                {
                    Note($"relics: {(worn != null ? worn + " to the bag" : "")}{(worn != null && bagged != null ? ", " : "")}{(bagged != null ? bagged + " worn" : "")}");
                    _bagMessage = bagged != null ? string.Format(UiText.BagMovedFormat, UiText.RelicName(bagged)) : UiText.BagHint;
                }
            }
            _bagPicked = -1;
            ShowBag();
            if (!_atTable) _map.Show(MapState());
        }

        private void BagDropPressed()
        {
            if (!IsBagOpen || !_bagEditable) return;
            string[] places = BagPlaces();
            if (_bagPicked < 0 || places[_bagPicked] == null)
            {
                _bagMessage = UiText.BagPickFirst;
                ShowBag();
                return;
            }
            if (!_bagConfirm)
            {
                _bagConfirm = true;
                ShowBag();
                return;
            }
            string id = places[_bagPicked];
            if (_run.Effects.Drop(id))
            {
                Note($"relics: {id} left behind (the bag)");
                _bagMessage = string.Format(UiText.BagDroppedFormat, UiText.RelicName(id));
            }
            _bagPicked = -1;
            _bagConfirm = false;
            _bagCursor = Math.Min(_bagCursor, RelicBagState.Slots - 1);
            ShowBag();
            if (!_atTable) _map.Show(MapState());
        }

        private void BagDonePressed()
        {
            if (!IsBagOpen) return;
            _bag.Hide();
            Action closed = _bagClosed;
            _bagClosed = null;
            closed?.Invoke();
            Save();
        }

        /// <summary>The map's RELICS: a look at what is worn and what is in the bag.</summary>
        private void ShowRelicsFromMap()
        {
            if (!IsMapOpen || _panel.IsOpen || IsBagOpen || _journey == null || _ended) return;
            OpenBag(editable: false, null);
        }

        // ------------------------------------------------------------------ the floors' matches

        private void StartMatch(FloorTable table)
        {
            ClosePanel();
            _match = table;
            _bossGame = null;
            _atTable = true;
            _map.Hide();
            _table.ChapterLog = _log;
            _table.SitAtFloor(table, _run);
            if (_visible) _tableView.SetVisible(true);
            Curtain();
            Save();
        }

        /// <summary>A chapter's table said it is done: a floor match goes back to the map, a demon's table ends the chapter (or the run).</summary>
        private void TableFinished()
        {
            if (_journey == null || _ended) return;
            if (_journey.Stage == JourneyStage.Lucifer)
            {
                ResolveLucifer();
                return;
            }
            if (_bossGame != null)
            {
                ResolveBoss();
                return;
            }
            if (_match == null) return;
            FinishMatch();
        }

        private void FinishMatch()
        {
            FloorTable match = _match;
            bool gamble = match == _run.PendingGamble;
            bool chase = match == _run.PendingChase;
            int stolen = _run.EventMemo;
            int relicsBefore = _run.Effects.Relics.Count;
            _run.FinishTable(match);
            _journey.FloorHands += match.HandsPlayed;
            if (match.IsWarden && match.MatchWon && !gamble)
            {
                _records.WardenBeaten(_run.WardensBeatenHere);
                _archive?.SaveRecords(_records);
            }
            string given = _run.Effects.Relics.Count > relicsBefore ? _run.Effects.Relics.Last() : null;
            string kind = gamble ? "gambler's hand" : chase ? $"pickpocket's chase ({(_run.ChaseWon == true ? "caught" : "got away")})" : match.IsWarden ? "warden" : "imp's table";
            Note($"chapter {_journey.Chapter} floor {(_run.Current?.Floor ?? 0) + 1}: {kind}, {match.HandsPlayed} hands, {Signed(match.Coins)} coins" +
                 (match.TollTaken > 0 ? $", toll {match.TollTaken}" : "") + (match.CoinsToVault > 0 ? $", {match.CoinsToVault} to the vault" : "") +
                 $" (purse {_run.Purse.Coins})" + (given != null ? $", relic {given}" : ""));
            _match = null;
            _atTable = false;
            _tableView.SetVisible(false);
            ShowMap();
            Curtain();
            if (_run.PurseEmptied)
            {
                FinishRun(JourneyEnd.PurseEmptied, house: ChapterCast.HouseOf(match, _run.Rules), hands: match.HandsPlayed);
                return;
            }
            if (chase)
            {
                ShowChaseEnd(match, stolen);
                Save();
                return;
            }
            ShowMatchEnd(match, gamble, given);
            Save();
        }

        /// <summary>The pickpocket's chase is over: caught (the coins back, and more) or gone.</summary>
        private void ShowChaseEnd(FloorTable match, int stolen)
        {
            var lines = new List<string> { string.Format(UiText.MatchTextFormat, match.HandsPlayed, match.Wins, Signed(match.Coins)) };
            lines.Add(_run.ChaseWon == true ? string.Format(UiText.ChaseWonFormat, stolen + _run.Scaled(ChapterRun.ChaseBonus)) : UiText.ChaseLost);
            lines.Add(string.Format(UiText.MatchPurseFormat, _run.Purse.Coins));
            ShowPanel(new PanelCard("Events/" + ChoiceEventIds.Pickpocket, UiText.OfferOwner(ChoiceEventIds.Pickpocket), UiText.ChaseTitle,
                string.Join("\n", lines), new[] { new PanelOption(UiText.PanelOn) }), new Action[] { Onward }, "matchend");
        }

        private void ShowMatchEnd(FloorTable match, bool gamble, string relicGiven)
        {
            string house = ChapterCast.HouseOf(match, _run.Rules);
            string title = gamble ? UiText.GambleOverTitle : UiText.MatchWonTitle;
            var lines = new List<string>
            {
                gamble ? string.Format(UiText.GambleTextFormat, Signed(match.Coins))
                    : string.Format(UiText.MatchTextFormat, match.HandsPlayed, match.Wins, Signed(match.Coins))
            };
            if (match.TollTaken > 0) lines.Add(string.Format(UiText.MatchTollFormat, match.TollTaken));
            if (relicGiven != null)
                lines.Add(UiText.RarityName(ChapterRelics.RarityOf(relicGiven)) + " · " +
                    string.Format(UiText.MatchRelicFormat, UiText.RelicName(relicGiven), UiText.RelicGift(relicGiven), UiText.RelicCurse(relicGiven)));
            lines.Add(string.Format(UiText.MatchPurseFormat, _run.Purse.Coins));
            ShowPanel(new PanelCard(house, UiText.Dealer(house).Name, title, string.Join("\n", lines), new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { () => { if (_run.WardenRelicWaiting != null) ShowWardenRelic(); else Onward(); } }, "matchend");
        }

        private static string Signed(int coins) => coins > 0 ? "+" + coins : coins < 0 ? "−" + (-coins) : "0";

        private void ShowWardenRelic()
        {
            string id = _run.WardenRelicWaiting;
            string warden = ChapterCast.WardenOf(_run.Rules);
            // Every slot worn and the bag full: one held relic is left behind for it — or the warden's coins (an offer's: it is left).
            bool fromWarden = _run.CurrentKind == NodeKind.Warden;
            var options = new List<PanelOption>
            {
                fromWarden ? new PanelOption(string.Format(UiText.WardenCoinsFormat, _run.Rules.WardenRelicCoins))
                    : new PanelOption(UiText.LeaveNewRelic, null, isLeave: true)
            };
            var answers = new List<Action> { () => { if (fromWarden) _run.TakeWardenCoins(); else _run.LeaveWaitingRelic(); Onward(); } };
            foreach (string carried in _run.Effects.AllHeld.Where(r => !RelicRoster.IsReward(r)).ToArray())
            {
                string swap = carried;
                options.Add(new PanelOption(string.Format(UiText.WardenSwapFormat, UiText.RelicName(swap)),
                    string.Format(UiText.WardenSwapDetailFormat, UiText.RelicName(swap))));
                answers.Add(() =>
                {
                    if (_run.SwapForWardenRelic(swap)) RelicLeft(swap, "for " + id);
                    Onward();
                });
            }
            ShowPanel(new PanelCard(warden, UiText.Dealer(warden).Name, UiText.WardenRelicTitle,
                UiText.RarityName(ChapterRelics.RarityOf(id)) + " · " +
                string.Format(UiText.WardenRelicTextFormat, UiText.RelicName(id), UiText.RelicGift(id), UiText.RelicCurse(id), _run.Rules.WardenRelicCoins),
                options), answers, "warden");
        }

        // ------------------------------------------------------------------ the gate and the demon

        private void ShowGate()
        {
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            int coins = _run.Purse.Coins;
            int tribute = _run.Tribute;
            int missing = tribute - _run.TributePaid(coins);
            int after = _run.BarAfterGate(coins);
            var lines = new List<string>();
            // What waited at the gate besides the tribute: the debtor imp (or his absence), the insurer's ash.
            GateArrival arrival = _run.Arrival;
            if (arrival != null && arrival.LoanRepaid > 0) lines.Add(string.Format(UiText.GateLoanRepaidFormat, arrival.LoanRepaid));
            else if (arrival != null && arrival.LoanRepaid == 0) lines.Add(UiText.GateLoanFled);
            if (arrival != null && arrival.InsurerGone) lines.Add(UiText.GateInsurerGone);
            lines.Add(string.Format(UiText.GateTextFormat, boss, tribute, coins));
            lines.Add(missing > 0 ? string.Format(UiText.GateShortFormat, missing)
                : string.Format(UiText.GatePaidFormat, coins - _run.TributePaid(coins)));
            if (_run.YearsOwed > 0) lines.Add(UiText.GateOwed);
            lines.Add(string.Format(UiText.GateSentenceFormat, _journey.Sentence.Years + missing * rules.YearsPerMissingCoin));
            lines.Add(UiText.GateAfter);
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, UiText.GateTitle(rules.Number), string.Join("\n", lines),
                new[] { new PanelOption(UiText.GatePay) }, bar: BarAt(after, after), barLabel: BarLabel()), new Action[] { SitWithTheDemon }, "gate");
        }

        private void SitWithTheDemon()
        {
            ClosePanel();
            GateToll toll = _run.PayTribute();
            if (toll.PaidInFull)
            {
                _records.TributePaidInFull();
                _archive?.SaveRecords(_records);
            }
            Note($"chapter {_journey.Chapter} gate: tribute {_run.Tribute}, had {toll.CoinsBefore}, missing {toll.Missing} (+{toll.YearsForMissing} years)" +
                 (toll.YearsOwed > 0 ? $", owed +{toll.YearsOwed}" : "") + $", purse left {toll.CoinsLeft}, sentence {_journey.Sentence.Years}");
            _bossGame = _run.OpenBossTable(_bossTable);
            Note($"{_run.Rules.BossId}'s table: bar {_run.BossBarStart} years, soul line {_bossGame.Rules.SoulThreshold}");
            SitAtBoss();
        }

        private void SitAtBoss()
        {
            _match = null;
            _atTable = true;
            _map.Hide();
            _table.ChapterLog = _log;
            _table.SitAtBoss(_bossGame, _run, _run.BossBarStart);
            if (_visible) _tableView.SetVisible(true);
            Curtain();
            Save();
        }

        private void ResolveBoss()
        {
            HellPokerGame game = _bossGame;
            ChapterRules rules = _run.Rules;
            _journey.BossHands[_journey.BossIndex] = game.RoundNumber;
            _bossGame = null;
            _atTable = false;
            _tableView.SetVisible(false);
            if (game.Phase == GamePhase.Damned)
            {
                Note($"{rules.BossId}: the soul burned after {game.RoundNumber} hands (sentence {_journey.Sentence.Years})");
                ShowMap();
                Curtain();
                FinishRun(JourneyEnd.Damned, house: rules.BossId);
                return;
            }
            _run.LeaveBossTable(0);
            _records.BossBeaten(rules.BossId);
            _archive?.SaveRecords(_records);
            Note($"{rules.BossId}: beaten in {game.RoundNumber} hands (sentence {_journey.Sentence.Years})");
            if (_journey.Chapter >= ChapterRules.Chapters)
            {
                Curtain();
                ShowLuciferCalls();
                return;
            }
            ShowMap();
            Curtain();
            ShowLoot();
        }

        // ------------------------------------------------------------------ the spoils of a beaten demon

        private void ShowLoot()
        {
            _audio.PlaySfx(SfxIds.Loot);
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            bool full = _run.RelicsFull;
            foreach (string id in _run.LootOffers)
            {
                string relic = id;
                options.Add(new PanelOption(UiText.RelicName(relic), UiText.RelicPanelDetail(relic) + (full ? "\n" + UiText.LootSwapNote : "")));
                answers.Add(() =>
                {
                    if (full) ShowLootSwap(relic);
                    else if (_run.TakeLoot(relic)) LootTaken(relic);
                });
            }
            if (_run.LootUpgrade != null)
            {
                string upgrade = _run.LootUpgrade;
                options.Add(new PanelOption(string.Format(UiText.LootUpgradeFormat, UiText.UpgradeName(upgrade)), UiText.UpgradeText(upgrade)));
                answers.Add(() =>
                {
                    if (_run.TakeLootUpgrade()) LootTaken(null, upgrade);
                });
            }
            options.Add(new PanelOption(string.Format(UiText.WardenCoinsFormat, ChapterRun.LootCoins)));
            answers.Add(() =>
            {
                if (_run.TakeLootCoins()) LootTaken(null);
            });
            string text = string.Format(UiText.LootTextFormat, boss, _journey.BossHands[_journey.BossIndex], ChapterRun.LootCoins);
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, UiText.LootTitle, text, options), answers, "loot");
        }

        private void ShowLootSwap(string relic)
        {
            var carried = _run.Effects.AllHeld.Where(r => !RelicRoster.IsReward(r)).ToArray();
            var options = carried.Select(id => new PanelOption(string.Format(UiText.WardenSwapFormat, UiText.RelicName(id)),
                string.Format(UiText.WardenSwapDetailFormat, UiText.RelicName(id)))).ToList();
            var answers = carried.Select(id => (Action)(() =>
            {
                if (!_run.TakeLoot(relic, id)) return;
                RelicLeft(id, "for " + relic);
                LootTaken(relic);
            })).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(ShowLoot);
            ShowPanel(new PanelCard(_run.Rules.BossId, UiText.Dealer(_run.Rules.BossId).Name, UiText.LootSwapTitle,
                string.Format(UiText.LootSwapTextFormat, UiText.RelicName(relic)), options), answers, "loot");
        }

        private void LootTaken(string relic, string upgrade = null)
        {
            Note(relic != null ? $"spoils: {relic} ({ChapterRelics.RarityOf(relic)})" : upgrade != null ? $"spoils: upgrade {upgrade}"
                : $"spoils: +{ChapterRun.LootCoins} coins (purse {_run.Purse.Coins})");
            ClosePanel();
            _journey.NextChapter();
            Note($"chapter {_journey.Chapter}: {_run.Rules.BossId}, purse {_run.Purse.Coins}, bar {_run.Years}" +
                 (_journey.Sown.HasValue ? $", the seed sows {_journey.Sown.Value}" : ""));
            _nodeDone = true;
            _selected = PickDefault(_run.Choices);
            ShowMap();
            Curtain();
            ShowChapterOpening();
        }

        // ------------------------------------------------------------------ Lucifer

        private void ShowLuciferCalls()
        {
            int bar = BossShares.For(_journey.Sinner.Id, DealerRoster.LuciferId);
            _map.Hide();
            ShowPanel(new PanelCard(DealerRoster.LuciferId, UiText.Dealer(DealerRoster.LuciferId).Name, UiText.LuciferCallsTitle,
                    string.Format(UiText.LuciferCallsTextFormat, ChapterRules.LuciferCastDownPercent - 100),
                    new[] { new PanelOption(UiText.LuciferSit) }),
                new Action[] { SitWithLucifer }, "lucifer");
            Save();
        }

        private void SitWithLucifer()
        {
            ClosePanel();
            _bossGame = _journey.OpenLuciferTable(_bossTable);
            Note($"lucifer's table: bar {_journey.LuciferBarStart}, cast down above {BossTable.LuciferGate(_journey.LuciferBarStart)}");
            SitAtLucifer();
        }

        private void SitAtLucifer()
        {
            _match = null;
            _atTable = true;
            _map.Hide();
            _table.ChapterLog = _log;
            _table.SitAtLucifer(_bossGame, _run, _journey.LuciferBarStart, BossTable.LuciferGate(_journey.LuciferBarStart), _journey.IsCastDown);
            if (_visible) _tableView.SetVisible(true);
            Curtain();
            Save();
        }

        private void ResolveLucifer()
        {
            HellPokerGame game = _bossGame;
            _journey.BossHands[3] = game.RoundNumber;
            _bossGame = null;
            _atTable = false;
            _tableView.SetVisible(false);
            JourneyEnd end = game.Phase == GamePhase.Absolved ? JourneyEnd.Freed
                : game.Phase == GamePhase.Damned ? JourneyEnd.Damned : JourneyEnd.CastDown;
            Note($"lucifer: {end} after {game.RoundNumber} hands (bar {game.Years}, sentence {_journey.Sentence.Years})");
            Curtain();
            FinishRun(end, house: DealerRoster.LuciferId);
        }

        // ------------------------------------------------------------------ the end of the run

        private void FinishRun(JourneyEnd end, bool show = true, string house = null, int hands = 0)
        {
            ChapterJourney journey = _journey;
            int deepest = journey.Stage == JourneyStage.Lucifer ? 4 : journey.Chapter;
            journey.Finish(end);
            ChapterRecords before = ChapterRecords.Decode(_records.Encode());
            _records.RunEnded(end, deepest, journey.Sinner.Id, journey.TotalHands, journey.Circle, journey.Sentence.Years);
            IReadOnlyList<string> opened = ChapterUnlocks.Opened(before, _records);
            bool circleOpened = end == JourneyEnd.Freed && _records.CirclesOpen(journey.Sinner.Id) > before.CirclesOpen(journey.Sinner.Id);
            if (opened.Count > 0) Note("unlocked: " + string.Join(", ", opened));
            _archive?.SaveRecords(_records);
            _archive?.ClearRun();
            _log?.End(end.ToString().ToUpperInvariant(), journey.Sentence.Years, journey.TotalHands);
            CloseLog();
            _log = null;
            _ended = true;
            _atTable = false;
            _match = null;
            _bossGame = null;
            _endTrack = end == JourneyEnd.Freed ? SfxIds.FreedMusic : SfxIds.DoomMusic;
            if (end == JourneyEnd.PurseEmptied) _audio.PlaySfx(SfxIds.PurseEmpty);
            _audio.PlayMusic(_endTrack);
            if (!show) return;

            string portrait = house ?? _run.Rules.BossId;
            string title, text;
            switch (end)
            {
                case JourneyEnd.PurseEmptied:
                    title = UiText.PurseEmptyTitle;
                    text = string.Format(UiText.PurseEmptyTextFormat, UiText.NameInSentence(UiText.Dealer(portrait)), hands);
                    break;
                case JourneyEnd.CastDown:
                    title = UiText.FallTitle;
                    text = UiText.FallText;
                    break;
                case JourneyEnd.Freed:
                    title = UiText.FreedTitle;
                    // What is left of the sentence is the run's score: none at all is a clean walk.
                    text = UiText.FreedText + "\n" + (journey.Sentence.Years == 0 ? UiText.FreedClean
                        : string.Format(UiText.FreedSentenceFormat, journey.Sentence.Years));
                    break;
                default:
                    title = UiText.ChapterDamnedTitle;
                    text = string.Format(UiText.ChapterDamnedTextFormat, UiText.NameInSentence(UiText.Dealer(portrait)));
                    break;
            }
            text += "\n\n" + Summary(journey);
            foreach (string id in opened)
                text += "\n" + string.Format(UiText.UnlockedFormat, id == Core.Sinners.Summoner.ClassId ? UiText.SinnerName(id) : UiText.RelicName(id));
            if (circleOpened)
                text += "\n" + string.Format(UiText.CircleOpenedFormat, UiText.SinnerName(journey.Sinner.Id), _records.CirclesOpen(journey.Sinner.Id));
            ShowMap();
            ShowPanel(new PanelCard(portrait, UiText.Dealer(portrait).Name, title, text,
                    new[] { new PanelOption(UiText.ChapterNewRun), new PanelOption(UiText.Menu, isLeave: true) }),
                new Action[]
                {
                    () => { ClosePanel(); NewRunRequested?.Invoke(); },
                    () => { ClosePanel(); _journey = null; MenuRequested?.Invoke(); }
                });
        }

        /// <summary>The run in a few lines: the class, the hands, the coins left, every demon's hands.</summary>
        private string Summary(ChapterJourney journey)
        {
            string Hands(int boss) => journey.BossHands[boss] > 0 ? journey.BossHands[boss].ToString() : "—";
            return string.Format(UiText.RunSummaryFormat, UiText.SinnerName(journey.Sinner.Id), journey.TotalHands, journey.Run.Purse.Coins,
                Hands(0), Hands(1), Hands(2), Hands(3));
        }

        // ------------------------------------------------------------------ the save

        private void Save()
        {
            if (_archive == null || _journey == null || _ended || _journey.IsOver) return;
            ChapterSave save = _journey.Capture();
            save.NodeDone = _nodeDone;
            save.Pending = _pending;
            if (_match != null)
            {
                save.Match = _match == _run.PendingGamble ? "gamble" : _match == _run.PendingChase ? "chase" : _match.IsWarden ? "warden" : "imp";
                save.MatchHouse = _match.HousePurse?.Coins ?? -1;
                save.MatchHands = _match.HandsPlayed;
                save.MatchMarks = _match.Marks.Encode();
                save.GambleStake = _match.HousePurse == null ? _match.AnteNow : 0;
                save.Deck = _match.DeckCards.ToList();
                CaptureHand(save, _match.Game, _match.HandInPlay);
            }
            else if (_bossGame != null)
            {
                save.Match = _journey.Stage == JourneyStage.Lucifer ? "lucifer" : "boss";
                save.BossBar = _bossGame.Years;
                save.MatchHands = _bossGame.RoundNumber;
                save.Malice = _bossGame.Malice;
                save.MajorUsed = _bossGame.MajorCheatUsed;
                save.Deck = _bossGame.DeckCards.ToList();
                CaptureHand(save, _bossGame, _bossGame.CurrentHand != null);
            }
            _archive.SaveRun(save);
        }

        private static void CaptureHand(ChapterSave save, HellPokerGame game, bool inHand)
        {
            HandInProgress hand = inHand ? game.CurrentHand : null;
            if (hand == null) return;
            save.HandStake = hand.Stake;
            save.HandAnte = hand.Ante;
            save.HandSealed = hand.IsSealed;
            save.HandAfterDraw = hand.IsAfterDraw;
            save.HandSoul = hand.IsSoulHand;
        }

        /// <summary>A saved run comes back where it was left; a hand left in the middle is lost now (no way out of a bad hand).</summary>
        private void Resume(ChapterSave save)
        {
            ChapterJourney journey;
            try { journey = ChapterJourney.Restore(save); }
            catch (ArgumentException)
            {
                _archive?.ClearRun();
                return;
            }
            _journey = journey;
            _journey.RelicAvailable = id => ChapterUnlocks.IsRelicOpen(id, _records);
            _ended = false;
            _endTrack = null;
            _visible = true;
            _atTable = false;
            _match = null;
            _bossGame = null;
            _nodeDone = save.NodeDone;
            _selected = PickDefault(_run.Choices);
            _log = new RunLog(_logSink?.Version, Lang.Current.ToString(), "phase2", journey.Sinner.Id, journey.Sentence.Years, DateTime.Now,
                Math.Max(1, journey.TotalHands));
            Note($"resumed: chapter {journey.Chapter}{(journey.Stage == JourneyStage.Lucifer ? " (lucifer)" : "")}, purse {_run.Purse.Coins}, sentence {journey.Sentence.Years}");
            bool handLost = save.HandStake > 0;

            switch (save.Match)
            {
                case "imp":
                case "warden":
                {
                    FloorTable table = _run.ReopenTable(TableMarks.Decode(save.MatchMarks), save.MatchHouse);
                    table.Resume(handLost ? Math.Max(0, save.MatchHands - 1) : save.MatchHands);
                    _match = table;
                    if (handLost)
                    {
                        int lost = table.ForfeitHand(save.HandStake);
                        Note($"a hand left in the middle is lost: {lost} coins");
                        if (table.IsOver)
                        {
                            _atTable = true;
                            ShowMap();
                            FinishMatch();
                            return;
                        }
                        ShowLostHand(string.Format(UiText.LostHandCoinsFormat, -lost), () => StartMatch(table));
                        return;
                    }
                    StartMatch(table);
                    return;
                }
                case "chase":
                {
                    FloorTable chase = _run.OpenChase(save.Memo, save.MatchHouse, handLost ? Math.Max(0, save.MatchHands - 1) : save.MatchHands);
                    _match = chase;
                    if (handLost)
                    {
                        int lost = chase.ForfeitHand(save.HandStake);
                        Note($"a hand of the chase left in the middle is lost: {lost} coins");
                        if (chase.IsOver)
                        {
                            _atTable = true;
                            ShowMap();
                            FinishMatch();
                            return;
                        }
                        ShowLostHand(string.Format(UiText.LostHandCoinsFormat, -lost), () => StartMatch(chase));
                        return;
                    }
                    StartMatch(chase);
                    return;
                }
                case "gamble":
                    if (handLost)
                    {
                        int lost = -_run.Purse.Add(-Math.Min(save.HandStake, Math.Max(0, _run.Purse.Coins - 1)));   // half the purse, never all
                        Note($"the gambler's hand left in the middle is lost: {lost} coins");
                        _nodeDone = false;
                        ShowLostHand(string.Format(UiText.LostHandCoinsFormat, lost), Onward);
                        return;
                    }
                    StartMatch(_run.OpenGamble(Math.Max(1, save.GambleStake)));
                    return;
                case "boss":
                case "lucifer":
                {
                    bool lucifer = save.Match == "lucifer";
                    HellPokerGame game = lucifer
                        ? journey.OpenLuciferTable(_bossTable, save.BossBar, save.MatchHands, save.LuciferBarStart)
                        : _run.ReopenBossTable(_bossTable, save.BossBarStart, save.BossBar, save.MatchHands);
                    game.RestoreMalice(save.Malice, save.MajorUsed);
                    _bossGame = game;
                    if (handLost)
                    {
                        game.ForfeitHand(new HandInProgress(save.HandStake, Math.Min(save.HandAnte, save.HandStake), save.HandAfterDraw, save.HandSoul,
                            save.HandSealed));
                        Note($"a hand left in the middle is lost: the bar is {game.Years}");
                        bool over = game.IsGameOver || (lucifer && journey.IsCastDown(game.Years));
                        Action sit = lucifer ? (Action)SitAtLucifer : SitAtBoss;
                        if (over)
                        {
                            _atTable = true;
                            ShowLostHand(UiText.LostHandBar, () => { if (lucifer) ResolveLucifer(); else ResolveBoss(); });
                            return;
                        }
                        ShowLostHand(UiText.LostHandBar, sit);
                        return;
                    }
                    if (lucifer) SitAtLucifer();
                    else SitAtBoss();
                    return;
                }
            }

            ShowMap();
            Curtain();
            ShowPending(save.Pending);
        }

        private void ShowLostHand(string text, Action then)
        {
            ShowMap();
            Curtain();
            ShowPanel(new PanelCard(null, null, UiText.LostHandTitle, text, new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { () => { ClosePanel(); then(); } });
        }

        /// <summary>The panel a saved run was waiting at.</summary>
        private void ShowPending(string pending)
        {
            if (string.IsNullOrEmpty(pending))
            {
                if (_run.WardenRelicWaiting != null) ShowWardenRelic();
                else if (!_nodeDone && _run.Current != null) Onward();
                return;
            }
            if (pending.StartsWith("event:", StringComparison.Ordinal))
            {
                IFloorEvent offer = FloorEventDeck.Find(pending.Substring("event:".Length));
                if (offer is IChoiceEvent choice) ShowChoiceOffer(choice, eyeOpen: false);   // its blow already struck (an encounter's)
                else if (offer != null && offer.CanAppear(_run)) ShowOffer(offer);
                else Onward();
                return;
            }
            switch (pending)
            {
                case "chapter": ShowChapterOpening(); break;
                case "treasure": ShowTreasure(); break;
                case "market": ShowMarket(_run.OpenMarket(), null); break;
                case "fire": ShowFire(); break;
                case "gate": ShowGate(); break;
                case "warden": ShowWardenRelic(); break;
                case "loot": ShowLoot(); break;
                case "lucifer": ShowLuciferCalls(); break;
                case "jackpot": ShowJackpotReward(); break;
                default: Onward(); break;   // a match's end, the fire's answer: already done
            }
        }

        /// <summary>The smoke tour's starting point (<see cref="FpsTour"/>): a run saved at Mammon's spoils, ready to be continued.</summary>
        internal void PrepareTour(SinnerClass sinnerClass) => PrepareSave(sinnerClass, 1, "loot");

        /// <summary>A save placed for the tour and the scene tests: Mammon's spoils ("loot"), a chapter's opening words ("chapter",
        /// with <paramref name="chapter"/>) or Lucifer's call after Lilith ("lucifer").</summary>
        internal void PrepareSave(SinnerClass sinnerClass, int chapter, string pending) =>
            _archive?.SaveRun(PlacedSave(sinnerClass, _newSeed(), chapter, pending));

        internal static ChapterSave PlacedSave(SinnerClass sinnerClass, int seed, int chapter, string pending)
        {
            ChapterJourney journey = ChapterJourney.Begin(sinnerClass, seed);
            if (pending != "gate") journey.Run.PayTribute();
            if (pending == "loot" || pending == "lucifer") journey.Run.LeaveBossTable(0);   // the demon beaten
            ChapterSave save = journey.Capture();
            save.Chapter = chapter;
            save.NodeDone = true;
            save.Pending = pending;
            return save;
        }

        // ------------------------------------------------------------------ the log

        private void Note(string text) => _log?.Note(text);

        /// <summary>The run's diary is written as it stands (the game closing; a run that ended).</summary>
        public void CloseLog()
        {
            if (_log == null || _logSink == null) return;
            try { _logSink.Write(_log); }
            catch (Exception) { /* a log never stops the game */ }
        }

        private void Curtain()
        {
            if (!_visible) return;
            _transition?.Play();
            _audio.PlaySfx(SfxIds.Transition);
            _audio.PlayMusic(MusicId);
        }

        // ------------------------------------------------------------------ the keys

        public void Step(int dx, int dy)
        {
            if (!IsMapOpen) return;
            if (IsBagOpen)
            {
                // The places: five worn over five in the bag; left / right along a row, up / down between the two.
                int row = _bagCursor / RelicBagState.Worn, col = _bagCursor % RelicBagState.Worn;
                if (dy != 0) row = Math.Max(0, Math.Min(1, row + dy));
                if (dx != 0) col = (col + dx + RelicBagState.Worn) % RelicBagState.Worn;
                _bagCursor = row * RelicBagState.Worn + col;
                ShowBag();
                return;
            }
            if (_panel.IsOpen && _card != null)
            {
                int count = _card.Options.Count;
                if (count == 0) return;
                int step = dy != 0 ? dy : dx;
                int selected = (_card.Selected + step + count) % count;
                _card = new PanelCard(_card.PortraitId, _card.Owner, _card.Title, _card.Text, _card.Options, selected);
                _panel.Show(_card);
                return;
            }
            if (_compassPicking)
            {
                var targets = _run.CompassTargets.OrderBy(n => n.Lane).ToList();
                if (targets.Count == 0) return;
                int t = _compassSelected == null ? -1 : targets.IndexOf(_compassSelected);
                _compassSelected = targets[t < 0 ? 0 : Math.Max(0, Math.Min(targets.Count - 1, t + (dx != 0 ? dx : dy)))];
                _map.Show(MapState());
                return;
            }
            var choices = _run.Choices.OrderBy(n => n.Lane).ToList();
            if (choices.Count == 0) return;
            int at = _selected == null ? -1 : choices.IndexOf(_selected);
            int next = at < 0 ? 0 : Math.Max(0, Math.Min(choices.Count - 1, at + (dx != 0 ? dx : dy)));
            _selected = choices[next];
            _map.Show(MapState());
        }

        public void Confirm()
        {
            if (!IsMapOpen) return;
            if (IsBagOpen)
            {
                BagPlacePressed(_bagCursor);
                return;
            }
            if (_panel.IsOpen && _card != null)
            {
                Answer(_card.Selected);
                return;
            }
            if (_compassPicking)
            {
                if (_compassSelected != null) StepWithCompass(_compassSelected);
                return;
            }
            if (_selected != null && _nodeDone && !_ended && _run.Choices.Contains(_selected)) Go(_selected);
        }

        public bool Back()
        {
            if (!IsMapOpen) return false;
            if (IsBagOpen)
            {
                BagDonePressed();
                return true;
            }
            // A panel waits for its answer; Esc takes its way out when it has one (the market, an offer: leave / pass).
            if (_panel.IsOpen && _card != null)
            {
                for (int i = _card.Options.Count - 1; i >= 0; i--)
                {
                    if (!_card.Options[i].IsLeave) continue;
                    Answer(i);
                    return true;
                }
                return false;   // nothing to leave by: Esc goes to the menu, the panel waits for the way back
            }
            if (_compassPicking)
            {
                PutCompassAway();   // Esc: the compass goes back in the pocket
                return true;
            }
            return false;
        }

        /// <summary>The compass is out, picking where it steps (for tests).</summary>
        public bool IsCompassOut => _compassPicking;

        /// <summary>The map's COMPASS (for tests and the keys).</summary>
        public void UseCompass() => PressCompass();

        // ------------------------------------------------------------------ language

        private void OnLanguageChanged()
        {
            if (_journey == null || !_visible || _atTable) return;
            _map.Show(MapState());
        }
    }
}
