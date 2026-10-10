using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Phase 2: one chapter, from its first floor to the demon's table. The map (<see cref="IChapterMapView"/>) shows the floors and
    /// where the player may go; a panel over it (<see cref="IChapterPanelView"/>) tells each node — the treasure, the black market,
    /// a stranger's offer, the purgatory fire, the gate's tribute, a match's end — and takes the answer. Table and warden matches,
    /// and the demon's own table, are played at a table of their own (a <see cref="TablePresenter"/> on its own view), which says
    /// when it is done. Knows the chapter's rules only through Core (<see cref="ChapterRun"/>); the demo's run is never touched.
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
        private readonly int _chapter;

        private ChapterRun _run;
        private readonly List<MapNode> _trail = new List<MapNode>();
        private MapNode _selected;
        private bool _visible;
        private bool _atTable;
        private bool _ended;

        /// <summary>The floor match at the table (null at the demon's table and on the map).</summary>
        private FloorTable _match;

        /// <summary>The demon's game, once the gate is passed.</summary>
        private IHellPokerGame _bossGame;

        private PanelCard _card;
        private readonly List<Action> _answers = new List<Action>();

        public event Action MenuRequested;
        public event Action NewRunRequested;

        /// <param name="bossTable">The numbers of the demon's table (the sentence's stakes, the soul line is the demon's own).</param>
        /// <param name="newSeed">A master seed for each new chapter run.</param>
        /// <param name="buildTable">The chapter's own table (a view and a presenter apart from the demo's), built when first needed.</param>
        public ChapterPresenter(IChapterMapView map, IChapterPanelView panel, Func<(TablePresenter presenter, ITableView view)> buildTable,
            IScreenTransition transition, IAudio audio, GameRules bossTable, Func<int> newSeed, int chapter = 1)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _buildTable = buildTable ?? throw new ArgumentNullException(nameof(buildTable));
            _transition = transition;
            _audio = audio ?? NullAudio.Instance;
            _bossTable = bossTable ?? GameRules.Default;
            _newSeed = newSeed ?? throw new ArgumentNullException(nameof(newSeed));
            _chapter = chapter;

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
            _tableView.MenuPressed += RequestMenu;
        }

        public void Dispose()
        {
            _map.NodePressed -= PressNode;
            _map.MenuPressed -= RequestMenu;
            _panel.OptionPressed -= Answer;
            Lang.Changed -= OnLanguageChanged;
            if (_table == null) return;
            _table.ChapterTableFinished -= TableFinished;
            _tableView.MenuPressed -= RequestMenu;
            _table.Dispose();
        }

        /// <summary>The chapter's table (the keys go to it while it is on screen); null before the first chapter.</summary>
        public ITableCommands Table => _table;

        /// <summary>The chapter run (for tests); null before the first.</summary>
        public ChapterRun Run => _run;

        /// <summary>The panel on show (for tests); null when none.</summary>
        public PanelCard Panel => _panel.IsOpen ? _card : null;

        public bool HasRun => _run != null && !_ended;
        public bool IsVisible => _visible;
        public bool IsMapOpen => _visible && !_atTable;
        public bool IsAtTable => _visible && _atTable;

        /// <summary>The chapter's demon's music on the map and at every table of the chapter.</summary>
        public string MusicId => (_run?.Rules ?? ChapterRules.For(_chapter)).BossId;

        // ------------------------------------------------------------------ the run

        public void Start(SinnerClass sinnerClass)
        {
            if (sinnerClass == null) throw new ArgumentNullException(nameof(sinnerClass));
            EnsureTable();
            ChapterRules rules = ChapterRules.For(_chapter);
            _run = new ChapterRun(rules, new Sinner(sinnerClass), new RunEffects(), sinnerClass.StartingYears, 0, _newSeed());
            _trail.Clear();
            _match = null;
            _bossGame = null;
            _atTable = false;
            _ended = false;
            _selected = PickDefault(_run.Choices);
            _visible = true;
            ShowMap();
            ShowIntro();
        }

        public void Show()
        {
            if (_run == null) return;
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
            if (_run == null) return;
            _tableView?.SetVisible(false);
            _map.Show(MapState());
        }

        private ChapterMapState MapState()
        {
            ChapterRules rules = _run.Rules;
            var lines = new List<string>
            {
                string.Format(UiText.MapTributeFormat, rules.Tribute),
                string.Format(UiText.MapYearsFormat, _run.Years)
            };
            if (_run.YearsOwed > 0) lines.Add(string.Format(UiText.MapOwedFormat, _run.YearsOwed));
            if (_run.Purse.InDebt) lines.Add(string.Format(UiText.MapDebtNoteFormat, rules.YearsPerMissingCoin));
            lines.Add("");
            lines.Add(UiText.MapRelicsLabel);
            if (_run.Effects.Relics.Count == 0) lines.Add("  " + UiText.MapNoRelics);
            foreach (string id in _run.Effects.Relics)
                lines.Add("  " + UiText.RelicName(id) + (id == _run.Effects.SilencedCurse ? " (" + UiText.MapSilenced + ")" : ""));
            if (_run.ImpsEyeNext) lines.Add(UiText.MapEyeReady);

            string title = string.Format(UiText.ChapterTitleFormat, rules.Number, UiText.ChapterName(rules.Number));
            string prompt = _run.Current == null ? UiText.MapPromptStart : UiText.MapPrompt;
            return new ChapterMapState(_run.Map, _run.Current, _atTable || _panel.IsOpen ? Array.Empty<MapNode>() : _run.Choices.ToArray(), _selected,
                _trail.ToArray(), title, prompt, _run.Purse.Coins, lines, Describe);
        }

        /// <summary>A node in words: its name and what waits there.</summary>
        private string Describe(MapNode node)
        {
            ChapterRules rules = _run.Rules;
            string what;
            switch (node.Kind)
            {
                case NodeKind.Table: what = string.Format(UiText.NodeTableFormat, rules.TableHands, rules.TableWinsNeeded, rules.TableBonus); break;
                case NodeKind.Warden:
                    what = string.Format(UiText.NodeWardenFormat, rules.WardenHands, rules.WardenWinsNeeded, rules.WardenBonus, rules.WardenTollPercent,
                        rules.WardenTollMax);
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
            if (!IsMapOpen || _panel.IsOpen || _run == null) return;
            MapNode node = _run.Choices.FirstOrDefault(n => n.Floor == floor && n.Lane == lane);
            if (node == null) return;
            Go(node);
        }

        private void Go(MapNode node)
        {
            _run.MoveTo(node);
            _trail.Add(node);
            _selected = PickDefault(_run.Choices);
            switch (node.Kind)
            {
                case NodeKind.Table:
                case NodeKind.Warden:
                    StartMatch(_run.OpenTable());
                    return;
                case NodeKind.Treasure:
                    _run.TakeTreasure();
                    ShowPanel(new PanelCard("Events/treasure", null, UiText.TreasureTitle, string.Format(UiText.TreasureTextFormat, _run.Rules.TreasureCoins),
                        new[] { new PanelOption(UiText.TreasureTake) }), new Action[] { Onward });
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
        }

        /// <summary>Back to the map after a node — or, past the last floor, on to the gate.</summary>
        private void Onward()
        {
            ClosePanel();
            if (_run.AtGate)
            {
                ShowGate();
                return;
            }
            ShowMap();
        }

        // ------------------------------------------------------------------ the panels

        private void ShowPanel(PanelCard card, IReadOnlyList<Action> answers)
        {
            _answers.Clear();
            _answers.AddRange(answers);
            _card = card;
            _panel.Show(card);
            if (!_atTable) _map.Show(MapState());   // the choices go dark under a panel
        }

        private void ClosePanel()
        {
            _card = null;
            _answers.Clear();
            _panel.Hide();
        }

        private void Answer(int index)
        {
            if (_card == null || index < 0 || index >= _answers.Count) return;
            _answers[index]?.Invoke();
        }

        private void ShowIntro()
        {
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            string text = string.Format(UiText.IntroFormat, rules.Floors, boss, rules.Tribute, rules.YearsPerMissingCoin, rules.BossHands, _run.Purse.Coins);
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, string.Format(UiText.ChapterTitleFormat, rules.Number, UiText.ChapterName(rules.Number)),
                text, new[] { new PanelOption(UiText.PanelDescend) }), new Action[] { () => { ClosePanel(); ShowMap(); } });
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
                    if (market.BuyRelic(id)) ShowMarket(market, UiText.RelicName(id));
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
            ShowPanel(new PanelCard("Events/black_market", UiText.MarketOwner, UiText.MarketTitle, text, options, selected), answers);
            if (!_atTable) _map.Show(MapState());   // the purse and the relics on the side change with every purchase
        }

        // ------------------------------------------------------------------ an offer

        private void ShowOffer(IFloorEvent offer)
        {
            string text;
            switch (offer.Id)
            {
                case FloorEventIds.Usurer:
                    text = string.Format(UiText.FloorEventTextFormat(offer.Id), PurgatoryUsurer.Coins, PurgatoryUsurer.Years);
                    break;
                case FloorEventIds.MammonsLedger:
                    text = string.Format(UiText.FloorEventTextFormat(offer.Id), MammonsLedger.YearsNow, MammonsLedger.YearsBack);
                    break;
                default:
                    text = string.Format(UiText.FloorEventTextFormat(offer.Id), _run.Purse.Coins / 2, _run.Rules.MultiplierCap);
                    break;
            }
            string portrait = offer.Id == FloorEventIds.MammonsLedger ? _run.Rules.BossId : "Events/" + offer.Id;
            ShowPanel(new PanelCard(portrait, UiText.FloorEventOwner(offer.Id), UiText.FloorEventTitle(offer.Id), text,
                    new[] { new PanelOption(UiText.EventAccept), new PanelOption(UiText.EventPass, isLeave: true) }),
                new Action[]
                {
                    () =>
                    {
                        offer.Accept(_run);
                        ClosePanel();
                        if (_run.PendingGamble != null) StartMatch(_run.PendingGamble);
                        else Onward();
                    },
                    Onward
                });
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
                        new PanelOption(UiText.FireShuffle, string.Format(UiText.FireShuffleDetailFormat, ChapterRun.FreeAntePercent))
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
                    () => Tend(FireChoice.ShuffleAndFreeAnte, null, UiText.FireDoneShuffle)
                });
        }

        private void ShowSilenceChoice(IReadOnlyList<string> relics)
        {
            var options = relics.Select(id => new PanelOption(UiText.RelicName(id), "− " + UiText.RelicCurse(id))).ToList();
            var answers = relics.Select(id => (Action)(() => Tend(FireChoice.SilenceCurse, id,
                string.Format(UiText.FireDoneSilenceFormat, UiText.RelicName(id))))).ToList();
            options.Add(new PanelOption(UiText.Back, null, isLeave: true));
            answers.Add(ShowFire);
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireSilenceWhich, UiText.FireSilenceWhichText, options), answers);
        }

        private void Tend(FireChoice choice, string relicId, string done)
        {
            _run.TendFire(choice, relicId);
            ShowPanel(new PanelCard("Events/purgatory_fire", null, UiText.FireTitle, done, new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { Onward });
        }

        // ------------------------------------------------------------------ the floors' matches

        private void StartMatch(FloorTable table)
        {
            ClosePanel();
            _match = table;
            _atTable = true;
            _map.Hide();
            _table.SitAtFloor(table, _run);
            if (_visible) _tableView.SetVisible(true);
            Curtain();
        }

        /// <summary>A chapter's table said it is done: a floor match goes back to the map, the demon's table ends the chapter.</summary>
        private void TableFinished()
        {
            if (_bossGame != null)
            {
                EndChapter();
                return;
            }
            if (_match == null) return;

            FloorTable match = _match;
            bool gamble = match == _run.PendingGamble;
            int relicsBefore = _run.Effects.Relics.Count;
            int coinsBefore = _run.Purse.Coins;
            _run.FinishTable(match);
            int bonus = _run.Purse.Coins - coinsBefore;
            string given = _run.Effects.Relics.Count > relicsBefore ? _run.Effects.Relics.Last() : null;
            _match = null;
            _atTable = false;
            _tableView.SetVisible(false);
            ShowMap();
            Curtain();
            ShowMatchEnd(match, gamble, bonus, given);
        }

        private void ShowMatchEnd(FloorTable match, bool gamble, int bonus, string relicGiven)
        {
            string house = ChapterCast.HouseOf(match, _run.Rules);
            string title = match.MatchWon ? UiText.MatchWonTitle : UiText.MatchLostTitle;
            var lines = new List<string>
            {
                string.Format(UiText.MatchTextFormat, match.Wins, match.HandsPlayed, Signed(match.Coins), match.WinsNeeded)
            };
            if (match.TollTaken > 0) lines.Add(string.Format(UiText.MatchTollFormat, match.TollTaken));
            if (!gamble && bonus > 0) lines.Add(string.Format(UiText.MatchBonusFormat, bonus));
            if (relicGiven != null)
                lines.Add(string.Format(UiText.MatchRelicFormat, UiText.RelicName(relicGiven), UiText.RelicGift(relicGiven), UiText.RelicCurse(relicGiven)));
            lines.Add(string.Format(UiText.MatchPurseFormat, _run.Purse.Coins));
            ShowPanel(new PanelCard(house, UiText.Dealer(house).Name, title, string.Join("\n", lines), new[] { new PanelOption(UiText.PanelOn) }),
                new Action[] { () => { if (_run.WardenRelicWaiting != null) ShowWardenRelic(); else Onward(); } });
        }

        private static string Signed(int coins) => coins > 0 ? "+" + coins : coins < 0 ? "−" + (-coins) : "0";

        private void ShowWardenRelic()
        {
            string id = _run.WardenRelicWaiting;
            string collector = ChapterCast.WardenOf(_run.Rules);
            var options = new List<PanelOption> { new PanelOption(string.Format(UiText.WardenCoinsFormat, _run.Rules.WardenRelicCoins)) };
            var answers = new List<Action> { () => { _run.TakeWardenCoins(); Onward(); } };
            foreach (string carried in _run.Effects.Relics.Where(r => !RelicRoster.IsReward(r)).ToArray())
            {
                string swap = carried;
                options.Add(new PanelOption(string.Format(UiText.WardenSwapFormat, UiText.RelicName(swap)),
                    string.Format(UiText.WardenSwapDetailFormat, UiText.RelicName(swap))));
                answers.Add(() => { _run.SwapForWardenRelic(swap); Onward(); });
            }
            ShowPanel(new PanelCard(collector, UiText.Dealer(collector).Name, UiText.WardenRelicTitle,
                string.Format(UiText.WardenRelicTextFormat, UiText.RelicName(id), UiText.RelicGift(id), UiText.RelicCurse(id), _run.Rules.WardenRelicCoins),
                options), answers);
        }

        // ------------------------------------------------------------------ the gate and the demon

        private void ShowGate()
        {
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            int coins = _run.Purse.Coins;
            int missing = Math.Max(0, rules.Tribute - coins);
            int after = _run.Years + rules.TributeYears(coins) + _run.YearsOwed;
            var lines = new List<string> { string.Format(UiText.GateTextFormat, boss, rules.Tribute, coins) };
            lines.Add(missing > 0 ? string.Format(UiText.GateShortFormat, missing, rules.TributeYears(coins))
                : string.Format(UiText.GatePaidFormat, coins - rules.Tribute));
            if (_run.YearsOwed > 0) lines.Add(string.Format(UiText.GateOwedFormat, _run.YearsOwed));
            lines.Add(string.Format(UiText.GateAfterFormat, after, rules.BossHands));
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, UiText.GateTitle, string.Join("\n", lines),
                new[] { new PanelOption(UiText.GatePay) }), new Action[] { SitWithTheDemon });
        }

        private void SitWithTheDemon()
        {
            ClosePanel();
            _run.PayTribute();
            _bossGame = _run.OpenBossTable(_bossTable);
            _atTable = true;
            _map.Hide();
            _table.SitAtBoss(_bossGame, _run);
            if (_visible) _tableView.SetVisible(true);
            Curtain();
        }

        private void EndChapter()
        {
            IHellPokerGame game = _bossGame;
            ChapterRules rules = _run.Rules;
            string boss = UiText.NameInSentence(UiText.Dealer(rules.BossId));
            string title, text;
            _run.LeaveBossTable(game.Years);   // the sentence as the demon's table left it (the side panel shows it)
            if (game.Phase == GamePhase.Damned)
            {
                title = UiText.ChapterDamnedTitle;
                text = string.Format(UiText.ChapterDamnedTextFormat, boss);
            }
            else if (game.Phase == GamePhase.Absolved)
            {
                title = UiText.ChapterFreeTitle;
                text = string.Format(UiText.ChapterFreeTextFormat, boss);
            }
            else
            {
                title = UiText.ChapterDoneTitle;
                text = string.Format(UiText.ChapterDoneTextFormat, boss, game.RoundNumber, _run.Years, _run.Purse.Coins);
            }
            _run.EndChapter();
            _ended = true;
            _atTable = false;
            _tableView.SetVisible(false);
            ShowMap();
            Curtain();
            ShowPanel(new PanelCard(rules.BossId, UiText.Dealer(rules.BossId).Name, title, text,
                    new[] { new PanelOption(UiText.ChapterNewRun), new PanelOption(UiText.Menu, isLeave: true) }),
                new Action[]
                {
                    () => { ClosePanel(); NewRunRequested?.Invoke(); },
                    () => { ClosePanel(); _run = null; MenuRequested?.Invoke(); }
                });
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
            if (_selected != null && _run.Choices.Contains(_selected)) Go(_selected);
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
            if (_run == null || !_visible || _atTable) return;
            _map.Show(MapState());
        }
    }
}
