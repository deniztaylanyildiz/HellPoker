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
            IRunLogSink logSink = null)
        {
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
            _panel.OptionPressed -= Answer;
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
        public bool IsMapOpen => _visible && !_atTable;
        public bool IsAtTable => _visible && _atTable;

        /// <summary>The music of the screen: the chapter's demon (Lucifer's at the end).</summary>
        public string MusicId => _journey?.Stage == JourneyStage.Lucifer ? DealerRoster.LuciferId : (_run?.Rules ?? ChapterRules.For(1)).BossId;

        // ------------------------------------------------------------------ the run

        public void Start(SinnerClass sinnerClass)
        {
            if (sinnerClass == null) throw new ArgumentNullException(nameof(sinnerClass));
            if (HasRun) Abandon();
            EnsureTable();
            _journey = ChapterJourney.Begin(sinnerClass, _newSeed());
            _match = null;
            _bossGame = null;
            _atTable = false;
            _ended = false;
            _nodeDone = true;
            _selected = PickDefault(_run.Choices);
            _visible = true;
            _records.RunStarted();
            _archive?.SaveRecords(_records);
            _log = new RunLog(_logSink?.Version, Lang.Current.ToString(), "phase2", sinnerClass.Id, BossShares.Total(sinnerClass.Id), DateTime.Now);
            Note($"phase 2 run: {sinnerClass.Id}, {_run.Purse.Coins} coins, sentence {BossShares.Total(sinnerClass.Id)} years");
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
                string.Format(UiText.MapTributeFormat, _run.Tribute),
                string.Format(UiText.MapYearsFormat, _run.Years, UiText.GenitiveInSentence(UiText.Dealer(rules.BossId)))
            };
            if (_run.YearsOwed > 0) lines.Add(string.Format(UiText.MapOwedFormat, _run.YearsOwed));
            lines.Add("");
            lines.Add(UiText.MapRelicsLabel);
            if (_run.Effects.Relics.Count == 0) lines.Add("  " + UiText.MapNoRelics);
            foreach (string id in _run.Effects.Relics)
            {
                string mark = id == _run.Effects.SilencedCurse ? " (" + UiText.MapSilenced + ")" : id == _run.Effects.AmplifiedRelic ? " (" + UiText.MapDesired + ")" : "";
                lines.Add("  " + UiText.RelicName(id) + mark);
            }
            if (_run.ImpsEyeNext) lines.Add(UiText.MapEyeReady);
            if (_run.NextTableMarks.HidesAll) lines.Add(UiText.MapSpectacleReady);
            if (_run.NextTableMarks.FreeHands > 0) lines.Add(UiText.MapInsomniaReady);

            string title = string.Format(UiText.ChapterTitleFormat, rules.Number, UiText.ChapterName(rules.Number));
            string prompt = _run.Current == null ? UiText.MapPromptStart : UiText.MapPrompt;
            MapNode[] choices = _atTable || _panel.IsOpen || !_nodeDone ? Array.Empty<MapNode>() : _run.Choices.ToArray();
            return new ChapterMapState(_run.Map, _run.Current, choices, _selected, _run.Trail.ToArray(), title, prompt, _run.Purse.Coins, lines, Describe,
                UiText.GateTitle(rules.Number), rules.Number);
        }

        /// <summary>A node in words: its name and what waits there.</summary>
        private string Describe(MapNode node)
        {
            ChapterRules rules = _run.Rules;
            string what;
            switch (node.Kind)
            {
                case NodeKind.Table: what = string.Format(UiText.NodeTableFormat, rules.ImpCoinsAt(node.Floor), rules.Ante, rules.AnteStepHands); break;
                case NodeKind.Warden:
                    string warden = UiText.NameInSentence(UiText.Dealer(ChapterCast.WardenOf(rules)));
                    what = string.Format(UiText.NodeWardenFormat, rules.WardenCoins, rules.Ante, rules.AnteStepHands, rules.WardenTollPercent,
                        rules.WardenTollMax, warden) + "\n" + UiText.WardenTrick(rules.BossId);
                    break;
                case NodeKind.Event: what = UiText.NodeEvent; break;
                case NodeKind.BlackMarket: what = UiText.NodeMarket; break;
                case NodeKind.Treasure: what = string.Format(UiText.NodeTreasureFormat, rules.TreasureCoins); break;
                default: what = UiText.NodeFire; break;
            }
            return UiText.NodeName(node.Kind) + "\n" + what;
        }

        private static MapNode PickDefault(IEnumerable<MapNode> choices)
        {
            var list = choices.OrderBy(n => n.Lane).ToList();
            return list.Count == 0 ? null : list[(list.Count - 1) / 2];
        }

        private void PressNode(int floor, int lane)
        {
            if (!IsMapOpen || _panel.IsOpen || _journey == null || _ended || !_nodeDone) return;
            MapNode node = _run.Choices.FirstOrDefault(n => n.Floor == floor && n.Lane == lane);
            if (node == null) return;
            Go(node);
        }

        private void Go(MapNode node)
        {
            _run.MoveTo(node);
            _nodeDone = false;
            _selected = PickDefault(_run.Choices);
            switch (node.Kind)
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
            if (_run.AtGate)
            {
                ShowGate();
                Save();
                return;
            }
            ShowMap();
            Save();
        }

        // ------------------------------------------------------------------ the panels

        private void ShowPanel(PanelCard card, IReadOnlyList<Action> answers, string pending = null)
        {
            _answers.Clear();
            _answers.AddRange(answers);
            _card = card;
            _pending = pending;
            _panel.Show(card);
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
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            string text = string.Format(UiText.IntroFormat, rules.Floors, boss, _run.Tribute, rules.YearsPerMissingCoin, _run.Purse.Coins);
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, string.Format(UiText.ChapterTitleFormat, rules.Number,
                    UiText.ChapterName(rules.Number)), text, new[] { new PanelOption(UiText.PanelDescend) }),
                new Action[] { () => { ClosePanel(); ShowMap(); } }, "chapter");
            Save();
        }

        private void ShowTreasure()
        {
            ShowPanel(new PanelCard("Events/treasure", null, UiText.TreasureTitle, string.Format(UiText.TreasureTextFormat, _run.Rules.TreasureCoins),
                new[] { new PanelOption(UiText.TreasureTake) }), new Action[] { Onward }, "treasure");
        }

        // ------------------------------------------------------------------ the black market

        private void ShowMarket(BlackMarket market, string bought)
        {
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            bool full = _run.Effects.CarriedOffered >= RelicRoster.MaxCarried;
            foreach (RelicOffer offer in market.Relics)
            {
                string id = offer.RelicId;
                bool can = market.CanBuyRelic(id);
                string why = can ? "" : "\n" + (full ? UiText.MarketFull : UiText.MarketNoCoins);
                options.Add(new PanelOption(string.Format(UiText.MarketRelicFormat, UiText.RelicName(id), offer.Price),
                    string.Format(UiText.MarketRelicDetailFormat, UiText.RelicGift(id), UiText.RelicCurse(id)) + why, can));
                answers.Add(() =>
                {
                    if (market.BuyRelic(id))
                    {
                        Note($"black market: bought {id} for {offer.Price} (purse {_run.Purse.Coins})");
                        ShowMarket(market, UiText.RelicName(id));
                    }
                });
            }
            foreach (string id in _run.Effects.Relics.Where(r => !RelicRoster.IsReward(r)).ToArray())
            {
                string relic = id;
                bool can = market.CanDropRelic(relic);
                options.Add(new PanelOption(string.Format(UiText.MarketDropFormat, UiText.RelicName(relic), market.DropPrice),
                    UiText.MarketDropDetail + (can ? "" : "\n" + UiText.MarketNoCoins), can));
                answers.Add(() =>
                {
                    if (market.DropRelic(relic)) ShowMarket(market, null);
                });
            }
            bool eye = market.CanBuyImpsEye;
            options.Add(new PanelOption(string.Format(UiText.MarketEyeFormat, market.EyePrice),
                UiText.MarketEyeDetail + (eye ? "" : "\n" + (_run.ImpsEyeNext ? UiText.MarketEyeWaits : UiText.MarketNoCoins)), eye));
            answers.Add(() =>
            {
                if (market.BuyImpsEye()) ShowMarket(market, UiText.MarketEyeName);
            });
            if (_run.Sinner.Class.StartingJokers > 0)
            {
                string jokers = string.Format(UiText.MarketJokerDetailFormat, _run.Sinner.Jokers);
                options.Add(new PanelOption(string.Format(UiText.MarketJokerInFormat, market.JokerServicePrice), jokers, market.CanChangeJokers(1)));
                answers.Add(() =>
                {
                    if (market.ChangeJokers(1)) ShowMarket(market, null);
                });
                options.Add(new PanelOption(string.Format(UiText.MarketJokerOutFormat, market.JokerServicePrice), jokers, market.CanChangeJokers(-1)));
                answers.Add(() =>
                {
                    if (market.ChangeJokers(-1)) ShowMarket(market, null);
                });
            }
            options.Add(new PanelOption(UiText.PanelLeave, UiText.MarketLeaveDetail, isLeave: true));
            answers.Add(Onward);

            string text = string.Format(UiText.MarketTextFormat, _run.Purse.Coins);
            if (bought != null) text += "\n" + string.Format(UiText.MarketBoughtFormat, bought);
            int selected = _card != null && _card.Title == UiText.MarketTitle ? Math.Min(_card.Selected, options.Count - 1) : options.Count - 1;
            ShowPanel(new PanelCard("Events/black_market", UiText.MarketOwner, UiText.MarketTitle, text, options, selected), answers, "market");
            if (!_atTable) _map.Show(MapState());   // the purse and the relics on the side change with every purchase
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
            string boss = UiText.NameInSentence(UiText.Dealer(_run.Rules.BossId));
            bool silence = _run.CanTend(FireChoice.SilenceCurse);
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireTitle, string.Format(UiText.FireTextFormat, boss),
                    new[]
                    {
                        new PanelOption(UiText.FireBreak, string.Format(UiText.FireBreakDetailFormat, boss)),
                        new PanelOption(UiText.FireSilence, UiText.FireSilenceDetail + (silence ? "" : "\n" + UiText.FireSilenceNone), silence),
                        new PanelOption(UiText.FireRest, string.Format(UiText.FireRestDetailFormat, UiText.GenitiveInSentence(UiText.Dealer(_run.Rules.BossId)),
                            ChapterRun.RestBarPercent))
                    }),
                new Action[]
                {
                    () => Tend(FireChoice.BreakFirstCheat, null, UiText.FireDoneBreak),
                    () =>
                    {
                        if (!silence) return;
                        var relics = _run.Effects.Relics.ToArray();
                        if (relics.Length == 1) Tend(FireChoice.SilenceCurse, relics[0], string.Format(UiText.FireDoneSilenceFormat, UiText.RelicName(relics[0])));
                        else ShowSilenceChoice(relics);
                    },
                    () => Tend(FireChoice.RestByTheFire, null, UiText.FireDoneRest)
                }, "fire");
        }

        private void ShowSilenceChoice(IReadOnlyList<string> relics)
        {
            var options = relics.Select(id => new PanelOption(UiText.RelicName(id), "− " + UiText.RelicCurse(id))).ToList();
            var answers = relics.Select(id => (Action)(() => Tend(FireChoice.SilenceCurse, id,
                string.Format(UiText.FireDoneSilenceFormat, UiText.RelicName(id))))).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(ShowFire);
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireSilenceWhich, UiText.FireSilenceWhichText, options), answers, "fire");
        }

        private void Tend(FireChoice choice, string relicId, string done)
        {
            _run.TendFire(choice, relicId);
            Note($"purgatory fire: {choice}{(relicId != null ? " " + relicId : "")}");
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireTitle, done, new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { Onward }, "fire.done");
        }

        // ------------------------------------------------------------------ the floors' matches

        private void StartMatch(FloorTable table)
        {
            ClosePanel();
            _match = table;
            _bossGame = null;
            _atTable = true;
            _map.Hide();
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
            int relicsBefore = _run.Effects.Relics.Count;
            _run.FinishTable(match);
            _journey.FloorHands += match.HandsPlayed;
            string given = _run.Effects.Relics.Count > relicsBefore ? _run.Effects.Relics.Last() : null;
            string kind = gamble ? "gambler's hand" : match.IsWarden ? "warden" : "imp's table";
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
            ShowMatchEnd(match, gamble, given);
            Save();
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
                lines.Add(string.Format(UiText.MatchRelicFormat, UiText.RelicName(relicGiven), UiText.RelicGift(relicGiven), UiText.RelicCurse(relicGiven)));
            lines.Add(string.Format(UiText.MatchPurseFormat, _run.Purse.Coins));
            ShowPanel(new PanelCard(house, UiText.Dealer(house).Name, title, string.Join("\n", lines), new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { () => { if (_run.WardenRelicWaiting != null) ShowWardenRelic(); else Onward(); } }, "matchend");
        }

        private static string Signed(int coins) => coins > 0 ? "+" + coins : coins < 0 ? "−" + (-coins) : "0";

        private void ShowWardenRelic()
        {
            string id = _run.WardenRelicWaiting;
            string warden = ChapterCast.WardenOf(_run.Rules);
            var options = new List<PanelOption> { new PanelOption(string.Format(UiText.WardenCoinsFormat, _run.Rules.WardenRelicCoins)) };
            var answers = new List<Action> { () => { _run.TakeWardenCoins(); Onward(); } };
            foreach (string carried in _run.Effects.Relics.Where(r => !RelicRoster.IsReward(r)).ToArray())
            {
                string swap = carried;
                options.Add(new PanelOption(string.Format(UiText.WardenSwapFormat, UiText.RelicName(swap)),
                    string.Format(UiText.WardenSwapDetailFormat, UiText.RelicName(swap))));
                answers.Add(() => { _run.SwapForWardenRelic(swap); Onward(); });
            }
            ShowPanel(new PanelCard(warden, UiText.Dealer(warden).Name, UiText.WardenRelicTitle,
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
            int after = _run.Years + _run.TributeYears(coins) + _run.YearsOwed;
            if (_run.RestedByTheFire) after -= after * ChapterRun.RestBarPercent / 100;
            var lines = new List<string> { string.Format(UiText.GateTextFormat, boss, tribute, coins) };
            lines.Add(missing > 0 ? string.Format(UiText.GateShortFormat, missing, _run.TributeYears(coins))
                : string.Format(UiText.GatePaidFormat, coins - _run.TributePaid(coins)));
            if (_run.YearsOwed > 0) lines.Add(string.Format(UiText.GateOwedFormat, _run.YearsOwed));
            lines.Add(string.Format(UiText.GateAfterFormat, after));
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, UiText.GateTitle(rules.Number), string.Join("\n", lines),
                new[] { new PanelOption(UiText.GatePay) }), new Action[] { SitWithTheDemon }, "gate");
        }

        private void SitWithTheDemon()
        {
            ClosePanel();
            GateToll toll = _run.PayTribute();
            Note($"chapter {_journey.Chapter} gate: tribute {_run.Tribute}, had {toll.CoinsBefore}, missing {toll.Missing} (+{toll.YearsForMissing} years)" +
                 (toll.YearsOwed > 0 ? $", owed +{toll.YearsOwed}" : "") + $", purse left {toll.CoinsLeft}");
            _bossGame = _run.OpenBossTable(_bossTable);
            Note($"{_run.Rules.BossId}'s table: bar {_run.BossBarStart} years, soul line {_bossGame.Rules.SoulThreshold}");
            SitAtBoss();
        }

        private void SitAtBoss()
        {
            _match = null;
            _atTable = true;
            _map.Hide();
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
                Note($"{rules.BossId}: the soul burned after {game.RoundNumber} hands");
                ShowMap();
                Curtain();
                FinishRun(JourneyEnd.Damned, house: rules.BossId);
                return;
            }
            _run.LeaveBossTable(0);
            Note($"{rules.BossId}: beaten in {game.RoundNumber} hands");
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
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            var options = new List<PanelOption>();
            var answers = new List<Action>();
            bool full = _run.Effects.CarriedOffered >= RelicRoster.MaxCarried;
            foreach (string id in _run.LootOffers)
            {
                string relic = id;
                options.Add(new PanelOption(UiText.RelicName(relic), string.Format(UiText.MarketRelicDetailFormat, UiText.RelicGift(relic),
                    UiText.RelicCurse(relic)) + (full ? "\n" + UiText.LootSwapNote : "")));
                answers.Add(() =>
                {
                    if (full) ShowLootSwap(relic);
                    else if (_run.TakeLoot(relic)) LootTaken(relic);
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
            var carried = _run.Effects.Relics.Where(r => !RelicRoster.IsReward(r)).ToArray();
            var options = carried.Select(id => new PanelOption(string.Format(UiText.WardenSwapFormat, UiText.RelicName(id)),
                string.Format(UiText.WardenSwapDetailFormat, UiText.RelicName(id)))).ToList();
            var answers = carried.Select(id => (Action)(() =>
            {
                if (_run.TakeLoot(relic, id)) LootTaken(relic);
            })).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(ShowLoot);
            ShowPanel(new PanelCard(_run.Rules.BossId, UiText.Dealer(_run.Rules.BossId).Name, UiText.LootSwapTitle,
                string.Format(UiText.LootSwapTextFormat, UiText.RelicName(relic)), options), answers, "loot");
        }

        private void LootTaken(string relic)
        {
            Note(relic != null ? $"spoils: {relic}" : $"spoils: +{ChapterRun.LootCoins} coins (purse {_run.Purse.Coins})");
            ClosePanel();
            _journey.NextChapter();
            Note($"chapter {_journey.Chapter}: {_run.Rules.BossId}, purse {_run.Purse.Coins}, bar {_run.Years}");
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
            Note($"lucifer: {end} after {game.RoundNumber} hands (bar {game.Years})");
            Curtain();
            FinishRun(end, house: DealerRoster.LuciferId);
        }

        // ------------------------------------------------------------------ the end of the run

        private void FinishRun(JourneyEnd end, bool show = true, string house = null, int hands = 0)
        {
            ChapterJourney journey = _journey;
            int deepest = journey.Stage == JourneyStage.Lucifer ? 4 : journey.Chapter;
            journey.Finish(end);
            _records.RunEnded(end, deepest, journey.Sinner.Id, journey.TotalHands);
            _archive?.SaveRecords(_records);
            _archive?.ClearRun();
            _log?.End(end.ToString().ToUpperInvariant(), _run.Years, journey.TotalHands);
            CloseLog();
            _log = null;
            _ended = true;
            _atTable = false;
            _match = null;
            _bossGame = null;
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
                    text = UiText.FreedText;
                    break;
                default:
                    title = UiText.ChapterDamnedTitle;
                    text = string.Format(UiText.ChapterDamnedTextFormat, UiText.NameInSentence(UiText.Dealer(portrait)));
                    break;
            }
            text += "\n\n" + Summary(journey);
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
                save.Match = _match == _run.PendingGamble ? "gamble" : _match.IsWarden ? "warden" : "imp";
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
            _ended = false;
            _visible = true;
            _atTable = false;
            _match = null;
            _bossGame = null;
            _nodeDone = save.NodeDone;
            _selected = PickDefault(_run.Choices);
            _log = new RunLog(_logSink?.Version, Lang.Current.ToString(), "phase2", journey.Sinner.Id, _run.Years, DateTime.Now,
                Math.Max(1, journey.TotalHands));
            Note($"resumed: chapter {journey.Chapter}{(journey.Stage == JourneyStage.Lucifer ? " (lucifer)" : "")}, purse {_run.Purse.Coins}");
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
                if (offer != null && offer.CanAppear(_run)) ShowOffer(offer);
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
            journey.Run.PayTribute();
            if (pending != "chapter") journey.Run.LeaveBossTable(0);
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
            if (_panel.IsOpen && _card != null)
            {
                Answer(_card.Selected);
                return;
            }
            if (_selected != null && _nodeDone && !_ended && _run.Choices.Contains(_selected)) Go(_selected);
        }

        public bool Back()
        {
            if (!IsMapOpen) return false;
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
            return false;
        }

        // ------------------------------------------------------------------ language

        private void OnLanguageChanged()
        {
            if (_journey == null || !_visible || _atTable) return;
            _map.Show(MapState());
        }
    }
}
