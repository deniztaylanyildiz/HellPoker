using System;
using System.Linq;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Builds and owns the whole table screen. Knows nothing about game rules; it only exposes widgets and input events.
    /// Every update goes through one <see cref="AnimationSequencer"/>, so the table shows things in the order they happen.
    /// Layout, on the 480×270 pixel screen: the demon top left, the House's cards above and the player's below in the
    /// middle column, the sentence counter and payouts on the right. No table — just dark stone.
    /// </summary>
    public sealed class TableView : MonoBehaviour, ITableView
    {
        // Phase 3: the table on the 640×360 screen. The demon's column keeps to the left edge, the counter's to the right, the bottom row
        // to the bottom; the middle column is centred and wider, and its lines have room between them.
        private const int W = PixelScreen.Width, H = PixelScreen.Height;
        private const int MiddleWidth = 320;
        private const int Middle = (W - MiddleWidth) / 2;
        private const int RightX = W - 108;                  // the counter and the payouts
        private const int BottomY = H - 22;                  // MENU, HANDS, the shortcut line
        private const int CardSpacing = 8;
        private const int ControlsY = 250;
        private const int ButtonHeight = 20;
        private const int TitleY = 8;
        private const int DealerBottom = 4 + DealerView.Height;   // the portrait, the name and the dialog box
        private const int HouseY = 52, HouseCaptionY = 38;

        // The middle column's lines (2.1.1: nothing in it overlaps — the layout test walks every text and button).
        private const int LineHeight = 9;
        private const int MessageY = 112, MessageLines = 3;
        private const int PotY = 142, StakeY = 152;
        private const int PlayerY = 172;

        /// <summary>The result band across the middle (a chapter's table): the big word and the amount; the message keeps to two lines under it.</summary>
        private const int BandY = 110, BandHeight = 27, BandMessageY = 140, BandMessageLines = 2;   // the dotted İ of the title clears the House's cards

        private AnimationSequencer _sequencer;
        private Canvas _canvas;
        private Text _message;
        private Text _pot;
        private Text _stakeInfo;
        private Text _powerHint;
        private Text _foldLabel;
        private Text _actionLabel;
        private Button _actionButton;
        private Button _raiseButton;
        private Text _raiseLabel;
        private Button _passButton;
        private Button _checkToDrawButton;
        private Button _foldButton;
        private Button _callButton;
        private Text _callLabel;
        private GameObject _ante;
        private Text _anteAmount;
        private FinalStretchEffect _finalStretch;
        private SentenceView _sentence;
        private DealerView _dealer;
        private SalonView _salon;
        private Stage _stage;
        private SoulView _soul;
        private Button _leaveButton;
        private Text _leaveLabel;
        private HandRanksPanel _handRanks;
        private TableMoments _moments;
        private TableScenes _scenes;
        private MaliceView _malice;
        private CheatEffects _cheatEffects;

        public IHandView House { get; private set; }
        public IHandView Player { get; private set; }
        public ISentenceView Sentence => _sentence;
        public IPayoutView Payouts { get; private set; }
        public IDealerView Dealer => _stage;

        public bool IsBusy => _sequencer.IsBusy;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action CheckToDrawPressed;
        public event Action MenuPressed;
        public event Action LeavePressed;
        public event Action HandRanksPressed;
        public event Action SinnerPressed;
        public event Action<int> EventOptionPressed;
        public event Action<string> RelicPressed;
        public event Action<int> KitCardPressed;
        public event Action<int> KitButtonPressed;
        public event Action ShufflePressed;
        public event Action<int, int> JokerStepPressed;
        public event Action JokerConfirmPressed;

        private JokerPickerView _jokerPicker;
        private Text _deckCount;
        private GameObject _deckTip;
        private Text _deckTipText;
        private Button _shuffleButton;
        private Text _shuffleLabel;

        /// <summary>The deck counter's text (for tests and screenshots); empty while hidden.</summary>
        public string DeckCountText => _deckCount.gameObject.activeSelf ? _deckCount.text : "";

        public void SetDeck(string label, string hint)
        {
            _sequencer.Do(() =>
            {
                _deckCount.gameObject.SetActive(label != null);
                if (label == null)
                {
                    _deckTip.SetActive(false);
                    return;
                }
                _deckCount.text = label;
                _deckTipText.text = hint ?? "";
            });
        }

        public void SetShuffle(string label, bool locked)
        {
            _sequencer.Do(() =>
            {
                _shuffleButton.gameObject.SetActive(label != null);
                _shuffleLabel.text = label ?? "";
                _shuffleButton.GetComponent<ButtonFeel>().Locked = locked;
            });
        }

        /// <summary>The deck counter's own box: left of the title, on a line of its own (2.1.1: it ran into the House's caption).</summary>
        public const int DeckX = Middle, DeckY = TitleY + 4, DeckWidth = 46;

        /// <summary>The deck's count left of the title (the House's caption has its row to itself); hovering it tells how the deck works.</summary>
        private void BuildDeckCounter(Transform screen)
        {
            _deckCount = UiFactory.CreateText("DeckCount", screen, "", 8, Palette.GoldLight, TextAnchor.MiddleLeft).WithOutline();
            _deckCount.rectTransform.PlaceTL(DeckX, DeckY, DeckWidth, 9);
            _deckCount.horizontalOverflow = HorizontalWrapMode.Overflow;
            _deckCount.raycastTarget = true;

            Image tip = UiFactory.CreateImage("DeckTip", screen, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            tip.rectTransform.PlaceTL(DeckX, DeckY + 11, 200, 30);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _deckTipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _deckTipText.rectTransform.PlaceTL(4, 3, 192, 26);
            _deckTip = tip.gameObject;
            _deckTip.SetActive(false);

            var hover = _deckCount.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => _deckTip.SetActive(true));
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _deckTip.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
            _deckCount.gameObject.SetActive(false);
        }

        /// <summary>The joker picker (for tests and screenshots).</summary>
        public JokerPickerView JokerPicker => _jokerPicker;

        public void ShowJokerPicker(JokerPick pick) => _jokerPicker.Show(pick);

        private RelicBarView _relics;
        private KitView _kit;
        private BossBarWidget _bossBar;

        /// <summary>The demon's health bar under the portrait (for tests and screenshots).</summary>
        public BossBarWidget BossBar => _bossBar;

        public void SetBossBar(BossBarDisplay bar) => _sequencer.Do(() => _bossBar.Apply(bar, animate: true));

        /// <summary>Phase 2's class panel (for tests and screenshots).</summary>
        public KitView Kit => _kit;

        public void SetKit(KitDisplay kit) => _kit.Set(kit);

        public void SetRelics(System.Collections.Generic.IReadOnlyList<RelicBadge> relics)
        {
            _relics.Set(relics);
            _panel.SetRelics(relics);
        }

        // ------------------------------------------------------------------ the player's panel (Demo 3.1)

        /// <summary>Under the counter in the right column, where the payouts stood (a chapter's table hides them).</summary>
        public const int PanelY = 62;

        private PlayerPanelView _panel;

        /// <summary>The player's panel (for tests and screenshots): shown at a chapter's table, in place of the payouts.</summary>
        public PlayerPanelView PlayerPanel => _panel;

        /// <summary>The class badge and the relics on the demon's portrait (the Boss Simulator's table; hidden at a chapter's).</summary>
        public SinnerBadgeView PortraitBadge => _sinner;

        public RelicBarView PortraitRelics => _relics;

        public void SetSinnerMood(SinnerMood mood) => _panel.SetMood(mood);

        /// <summary>Where the table's blocks stand (masa birliği: one layout for every table, the Boss Simulator's too).</summary>
        public TableLayout Layout { get; } = TableLayout.Table;

        /// <summary>
        /// Lays the table out: the demon top right, the counter top left, the player's panel bottom left with the kit (or LEAVE TABLE) over
        /// it. Every block's place comes from <see cref="TableLayout"/>; nothing else moves them.
        /// </summary>
        private void ApplyLayout()
        {
            Move(_dealer.transform, Layout.Dealer);
            Move(_malice.transform, Layout.Dealer);
            _malice.SetTipsLeft(Layout.DealerTipsLeft);
            Move(_bossBar.transform, Layout.Bar);
            Move(_sentence.transform, Layout.Counter);
            Move(_soul.transform, Layout.Counter);
            Move(_panel.transform, Layout.Panel);
            _panel.SetTipsRight(Layout.PanelTipsRight);
            _kit.MoveTo(Layout.KitX, Layout.KitY, Layout.KitFromBottom);
            ((RectTransform)_leaveButton.transform).PlaceTL(Layout.Leave.x, Layout.Leave.y, TableLayout.LeaveWidth, TableLayout.LeaveHeight);
            _cheatEffects.DealerPoint = Layout.DealerPoint;
            _fanfare.SetTargets(new Vector2Int(Layout.Counter.x + SentenceView.Width / 2, Layout.Counter.y + 10),
                new Vector2Int(Layout.Bar.x + TableLayout.BarWidth / 2, Layout.Bar.y + 3));
            // The player's own panel on every table; the old badge and relics on the demon's portrait stay hidden.
            _panel.gameObject.SetActive(true);
            _sinner.gameObject.SetActive(false);
            _relics.gameObject.SetActive(false);
            SetPayoutTableShown(false);
        }

        private static void Move(Transform widget, Vector2Int topLeft) => ((RectTransform)widget).anchoredPosition = new Vector2(topLeft.x, -topLeft.y);

        private HandFanfareView _fanfare;

        /// <summary>The hand endings' animations (for tests and screenshots).</summary>
        public HandFanfareView Fanfare => _fanfare;

        public void PlayHand(HandFanfare fanfare) => _fanfare.Play(fanfare);

        private EventPanelView _event;

        /// <summary>The event panel (for tests and screenshots).</summary>
        public EventPanelView EventPanel => _event;

        public void ShowEvent(EventCard card) => _event.Show(card);

        /// <summary>Where the table's effects sound (silent until the bootstrap gives it the game's audio).</summary>
        public IAudio Audio { get; set; } = NullAudio.Instance;

        public void PlaySfx(string sfxId) => _sequencer.Do(() => Audio.PlaySfx(sfxId));

        public void HideEvent() => _event.Hide();

        private SinnerBadgeView _sinner;

        public void SetSinner(SinnerBadge badge)
        {
            _sinner.Set(badge);
            _panel.Set(badge);
        }

        public bool HandRanksOpen => _handRanks.IsOpen;

        public static TableView Create(Transform parent, DealerAnimationLibrary dealers, SalonLibrary salons)
        {
            Canvas canvas = UiFactory.CreateScreen("TableCanvas", parent, 0, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<TableView>();
            view._canvas = canvas;
            view._sequencer = canvas.gameObject.AddComponent<AnimationSequencer>();
            view.Build(screen, dealers, salons);
            return view;
        }

        private void Build(RectTransform screen, DealerAnimationLibrary dealers, SalonLibrary salons)
        {
            // The demon's hall fills the screen behind everything; clicking it hurries any animation along.
            _salon = SalonView.Create(screen, salons);
            ClickCatcher.Attach(_salon.gameObject, () =>
            {
                if (IsBusy) SkipAnimations();
            });

            Image logo = UiFactory.CreateSprite("Title", screen, UiArt.Title);
            if (logo.sprite != null)
            {
                Vector2 size = logo.sprite.rect.size;
                logo.rectTransform.PlaceTL(Middle + (MiddleWidth - (int)size.x) / 2, TitleY, (int)size.x, (int)size.y);
            }
            else
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", screen, "", 16, Palette.Hell, style: FontStyle.Bold).WithOutline().Localized(() => UiText.Title)
                    .rectTransform.PlaceTL(Middle, TitleY, MiddleWidth, 16);
            }

            // Left column: the dealer.
            _dealer = DealerView.Create(screen, 4, 4, _sequencer, dealers);

            Button menu = UiFactory.CreateButton("MenuButton", screen, "", 8, out Text menuLabel, ButtonSkin.Ash);
            menuLabel.Localized(() => UiText.Menu);
            ((RectTransform)menu.transform).PlaceTL(4, BottomY, 56, 18);
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            _hands = UiFactory.CreateButton("HandsButton", screen, "", 8, out _handsLabel, ButtonSkin.Ash);
            _handsLabel.Localized(() => PayoutTableShown ? UiText.HandsButton : UiText.HandsPayoutsButton);
            _handsLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _handsLabel.lineSpacing = 1f;
            ((RectTransform)_hands.transform).PlaceTL(64, BottomY, 56, 18);
            _hands.onClick.AddListener(() => HandRanksPressed?.Invoke());
            BuildHandsTip(screen);

            _leaveButton = UiFactory.CreateButton("LeaveButton", screen, "", 8, out _leaveLabel, ButtonSkin.Ash);
            ((RectTransform)_leaveButton.transform).PlaceTL(4, DealerBottom + 4, 104, 18);
            _leaveButton.onClick.AddListener(() => LeavePressed?.Invoke());
            _leaveButton.gameObject.SetActive(false);

            // Right column: the sentence (or the soul) and the dealer's payouts.
            _sentence = SentenceView.Create(screen, RightX, 4, _sequencer);
            _soul = SoulView.Create(screen, RightX, 4, _sequencer);
            Payouts = PayoutTableView.Create(screen, RightX, 62, 178, _sequencer);

            // Middle column: house cards, the talk of the table, player cards, the bet controls.
            int handWidth = CardView.Size.x * 5 + CardSpacing * 4;
            int handX = Middle + (MiddleWidth - handWidth) / 2;
            House = HandView.Create(screen, "HouseHand", handX, HouseY, CardSpacing, HouseCaptionY, _sequencer, Middle, MiddleWidth);
            // 2.1.1: the player's row a pixel higher and its caption a pixel lower — the caption no longer hugs the cards' edge.
            Player = HandView.Create(screen, "PlayerHand", handX, PlayerY, CardSpacing, PlayerY + CardView.Size.y + 6, _sequencer, Middle, MiddleWidth);

            // The message: three lines at most (91-118), the pot under it (119), the outlook or the power's line under that (128). With the
            // result band up (a chapter's table) the message moves under the band and keeps to two lines.
            _message = UiFactory.CreateText("Message", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline();
            _message.rectTransform.PlaceTL(Middle, MessageY, MiddleWidth, MessageLines * LineHeight);
            BuildMessageTip(screen);
            _pot = UiFactory.CreateText("Pot", screen, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _pot.rectTransform.PlaceTL(Middle, PotY, MiddleWidth, 8);
            _stakeInfo = UiFactory.CreateText("StakeInfo", screen, "", 8, Palette.BoneMid).WithOutline();
            _stakeInfo.rectTransform.PlaceTL(Middle, StakeY, MiddleWidth, 9);
            _stakeInfo.horizontalOverflow = HorizontalWrapMode.Overflow;
            // A power switched on: what to do stays on this line (in place of the win / lose outlook) until it is used or off.
            _powerHint = UiFactory.CreateText("PowerHint", screen, "", 8, Palette.GoldLight).WithOutline();   // the text font: the title font overflows the column
            _powerHint.rectTransform.PlaceTL(Middle, StakeY, MiddleWidth, 9);
            _powerHint.horizontalOverflow = HorizontalWrapMode.Overflow;
            _powerHint.gameObject.SetActive(false);

            BuildAnte(screen);

            _actionButton = UiFactory.CreateButton("ActionButton", screen, "", 8, out _actionLabel, ButtonSkin.Ember);
            ((RectTransform)_actionButton.transform).PlaceTL(Middle + MiddleWidth - 96, ControlsY, 96, ButtonHeight);
            _actionButton.onClick.AddListener(() => ActionPressed?.Invoke());

            // SHUFFLE, under the deal button between hands.
            _shuffleButton = UiFactory.CreateButton("ShuffleButton", screen, "", 8, out _shuffleLabel, ButtonSkin.Ash);
            ((RectTransform)_shuffleButton.transform).PlaceTL(Middle + MiddleWidth - 96, ControlsY + ButtonHeight + 4, 96, 18);
            _shuffleButton.onClick.AddListener(() => ShufflePressed?.Invoke());
            _shuffleButton.gameObject.SetActive(false);
            BuildDeckCounter(screen);

            _raiseButton = CreateBetButton(screen, "RaiseButton", "", BetAction.Raise, Middle, 96, ButtonSkin.Ember, out _raiseLabel);
            _passButton = CreateBetButton(screen, "PassButton", "", BetAction.Pass, Middle + 104, 72, ButtonSkin.Blood, out Text passLabel);
            passLabel.Localized(() => UiText.Pass);
            // FOLD, or FREE / FOLD on two short lines while the Peasant's power is on (the label comes with the bet controls).
            _foldButton = CreateBetButton(screen, "FoldButton", "", BetAction.Fold, Middle + 184, 72, ButtonSkin.Ash, out _foldLabel);
            _foldLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _foldLabel.lineSpacing = 1f;
            _callButton = CreateBetButton(screen, "CallButton", "", BetAction.Call, Middle + 24, 112, ButtonSkin.Ember, out _callLabel);
            // Two short lines, so four buttons fit the row in the 8 px title font.
            _checkToDrawButton = UiFactory.CreateButton("CheckToDrawButton", screen, "", 8, out Text checkLabel, ButtonSkin.Blood);
            checkLabel.Localized(() => UiText.CheckToDrawButton);
            checkLabel.verticalOverflow = VerticalWrapMode.Overflow;
            checkLabel.lineSpacing = 1f;
            _checkToDrawButton.onClick.AddListener(() => CheckToDrawPressed?.Invoke());
            ApplyBetControls(BetControls.Hidden);

            _hint = UiFactory.CreateText("Hint", screen, "", 8, Palette.BoneDark, TextAnchor.MiddleLeft).WithOutline().Localized(() => UiText.Hint);
            _hint.rectTransform.PlaceTL(HintX, BottomY + 5, W - 4 - HintX, 9);

            _finalStretch = FinalStretchEffect.Create(screen, _salon, Middle, ControlsY + 26, MiddleWidth);
            _moments = TableMoments.Create(screen, _sequencer, (HandView)Player, _sentence, Middle, BandY - 2, MiddleWidth);
            // Demo 3.1: a chapter table's hand ending by its strength (the coins fly to the counter on a floor, to the demon's bar at his table).
            _fanfare = HandFanfareView.Create(screen, _sequencer, (HandView)Player, (HandView)House, c => Glow(c),
                new Vector2Int(W / 2, PlayerY + CardView.Size.y / 2), new Vector2Int(W / 2, HouseY + CardView.Size.y / 2),
                new Vector2Int(RightX + SentenceView.Width / 2, 14), new Vector2Int(8 + 46, 125), Middle, BandY - 2, MiddleWidth);
            // The malice gauge and the announced cheat ride on the portrait box (4, 4, 104 × 104).
            _malice = MaliceView.Create(screen, 4, 4, DealerView.PortraitSize + 8, _sequencer);
            // The class badge in the portrait box's lower right corner (the malice pips run along the lower left).
            _sinner = SinnerBadgeView.Create(screen, 4 + DealerView.PortraitSize + 8 - SinnerBadgeView.Width - 4, 4 + DealerView.PortraitSize + 8 - 22, _sequencer,
                () => SinnerPressed?.Invoke());
            _cheatEffects = CheatEffects.Create(screen, _sequencer, (HandView)Player, (HandView)House, new Vector2Int(56, 56), new Vector2(W / 2, PlayerY + 24));
            _scenes = TableScenes.Create(screen, _sequencer, _dealer);
            // The relics down the portrait box's right edge, under the intent sign.
            _relics = RelicBarView.Create(screen, 4 + DealerView.PortraitSize + 8 - 22, 24, _sequencer, id => RelicPressed?.Invoke(id));
            // Demo 3.1: a chapter's table shows the class and the relics in the player's own panel instead (the portrait stays clean).
            _panel = PlayerPanelView.Create(screen, RightX, PanelY, _sequencer, UiArt.Sinners, () => SinnerPressed?.Invoke(), id => RelicPressed?.Invoke(id));
            _panel.gameObject.SetActive(false);
            // Phase 2's class panel: under the dialog box, where a chapter's table has no LEAVE TABLE.
            _kit = KitView.Create(screen, 4, DealerBottom + 3, _sequencer, i => KitCardPressed?.Invoke(i), i => KitButtonPressed?.Invoke(i));
            // Phase 2: the demon's health bar, under the name (in place of the line under it).
            _bossBar = BossBarWidget.Create(screen, "BossBar", 8, 122, 92, 7, hover: true);
            // The joker picker sits where the bet controls do (they are hidden while it is open).
            _jokerPicker = JokerPickerView.Create(screen, Middle, ControlsY - 12, _sequencer, (rank, suit) => JokerStepPressed?.Invoke(rank, suit),
                () => JokerConfirmPressed?.Invoke());
            _event = EventPanelView.Create(screen, (W - EventPanelView.Width) / 2, 80, _sequencer, dealers, index => EventOptionPressed?.Invoke(index));
            _handRanks = HandRanksPanel.Create(screen, (W - HandRanksPanel.Width) / 2, 50, () => UiText.HandRanksTableFooter);
            BuildResults(screen);
            _stage = new Stage(this);
            ApplyLayout();
            BuildCarriedRule(screen);
        }

        // ------------------------------------------------------------------ a hand's result, plainly (2.1.1)

        private Image _band;
        private Text _bandTitle;
        private Text _bandAmount;
        private readonly Image[] _edges = new Image[4];
        private float _glowLeft;
        private Color _glowColor;
        private const float GlowBlink = 0.08f, GlowSeconds = GlowBlink * 4;
        private Text _float;
        private float _floatLeft;
        private int _floatX, _floatY;
        private const float FloatSeconds = 0.9f;
        private const int FloatRise = 14;
        private const int BarFloatX = 4 + DealerView.PortraitSize + 8 + 4;

        /// <summary>The band on show (for tests and screenshots); null when down.</summary>
        public ResultBand Band { get; private set; }

        /// <summary>The last amount floated (for tests and screenshots).</summary>
        public string LastFloat { get; private set; }

        private void BuildResults(Transform screen)
        {
            _band = UiFactory.CreateImage("ResultBand", screen, Palette.Black);
            _band.raycastTarget = false;
            _band.rectTransform.PlaceTL(Middle, BandY, MiddleWidth, BandHeight);
            UiFactory.AddBorder(_band.gameObject, Palette.Gold, 1f);
            _bandTitle = UiFactory.CreateText("Title", _band.transform, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _bandTitle.rectTransform.PlaceTL(0, 2, MiddleWidth, 16);
            _bandTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bandAmount = UiFactory.CreateText("Amount", _band.transform, "", 8, Palette.Bone).WithOutline();
            _bandAmount.rectTransform.PlaceTL(0, 18, MiddleWidth, 8);
            _bandAmount.horizontalOverflow = HorizontalWrapMode.Overflow;
            _band.gameObject.SetActive(false);

            // The screen's edge: two pixels each side, blinking a moment in the result's colour.
            (int x, int y, int w, int h)[] edges =
            {
                (0, 0, PixelScreen.Width, 2), (0, PixelScreen.Height - 2, PixelScreen.Width, 2), (0, 0, 2, PixelScreen.Height),
                (PixelScreen.Width - 2, 0, 2, PixelScreen.Height)
            };
            for (int i = 0; i < edges.Length; i++)
            {
                _edges[i] = UiFactory.CreateImage("ResultEdge" + i, screen, Palette.Gold);
                _edges[i].raycastTarget = false;
                _edges[i].rectTransform.PlaceTL(edges[i].x, edges[i].y, edges[i].w, edges[i].h);
                _edges[i].enabled = false;
            }

            _float = UiFactory.CreateText("ResultFloat", screen, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _float.horizontalOverflow = HorizontalWrapMode.Overflow;
            _float.enabled = false;
        }

        public void SetResultBand(ResultBand band)
        {
            _sequencer.Do(() =>
            {
                Band = band;
                _band.gameObject.SetActive(band != null);
                if (band != null)
                {
                    Color title = band.Outcome == HandOutcome.Win ? Palette.GoldLight : band.Outcome == HandOutcome.Loss ? Palette.Hell : Palette.BoneMid;
                    Color border = band.Outcome == HandOutcome.Win ? Palette.Gold : band.Outcome == HandOutcome.Loss ? Palette.Crimson : Palette.BoneDark;
                    _bandTitle.text = band.Title;
                    _bandTitle.color = title;
                    _bandAmount.text = band.Amount;
                    _band.GetComponent<Outline>().effectColor = border;
                    _band.transform.SetAsLastSibling();
                }
                LayOutMessage();
            });
        }

        public void FloatAmount(string text, Tone tone, bool overBar)
        {
            _sequencer.Do(() =>
            {
                LastFloat = text;
                _float.text = text ?? "";
                _float.color = Palette.For(tone);
                // Over the counter's number (top right), or beside the demon's bar (Demo 3.1: right of it, in the gap before the middle
                // column — over the bar it rose through his name and the malice pips).
                // Demo 3.2: wherever the layout put the counter and the bar.
                RectInt bar = Layout.BarFloat;
                _floatX = overBar ? bar.x : Layout.Counter.x;
                _floatY = overBar ? bar.y : Layout.Counter.y + 18;
                _float.rectTransform.PlaceTL(_floatX, _floatY, overBar ? bar.width : SentenceView.Width, 8);
                _float.transform.SetAsLastSibling();
                _float.enabled = true;
                _floatLeft = FloatSeconds;
            });
        }

        public void SetRecentHands(System.Collections.Generic.IReadOnlyList<HandPip> hands) => _sentence.SetRecent(hands);

        private void Glow(Color color)
        {
            _glowColor = color;
            _glowLeft = GlowSeconds;
            foreach (Image edge in _edges) edge.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (_glowLeft > 0f)
            {
                _glowLeft -= Time.unscaledDeltaTime * AnimationClock.Speed;
                // Two hard blinks (on, off, on, off): whole colours, no blending.
                bool on = _glowLeft > 0f && (int)((GlowSeconds - _glowLeft) / GlowBlink) % 2 == 0;
                foreach (Image edge in _edges)
                {
                    edge.enabled = on;
                    edge.color = _glowColor;
                }
            }
            if (_floatLeft > 0f)
            {
                _floatLeft -= Time.unscaledDeltaTime * AnimationClock.Speed;
                int rise = Mathf.RoundToInt(FloatRise * (1f - Mathf.Clamp01(_floatLeft / FloatSeconds)));
                _float.rectTransform.anchoredPosition = new Vector2(_floatX, -(_floatY - rise));
                if (_floatLeft <= 0f) _float.enabled = false;
            }
        }

        private void FinishResults()
        {
            _glowLeft = 0f;
            foreach (Image edge in _edges) edge.enabled = false;
            _floatLeft = 0f;
            _float.enabled = false;
        }

        // ------------------------------------------------------------------ the message: three lines at most, the rest on its hover

        private string _messageFull = "";
        private GameObject _messageTip;
        private Text _messageTipText;

        /// <summary>The message as shown (for tests: the lines it was folded into).</summary>
        public string MessageShown => _message.text;

        private void BuildMessageTip(Transform screen)
        {
            Image tip = UiFactory.CreateImage("MessageTip", screen, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _messageTipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _messageTip = tip.gameObject;
            _messageTip.SetActive(false);
            var hover = _message.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowMessageTip());
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _messageTip.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
        }

        private void ShowMessageTip()
        {
            if (!_message.raycastTarget) return;
            const int width = 256;
            _messageTipText.text = _messageFull;
            _messageTipText.rectTransform.PlaceTL(4, 3, width - 8, 200);
            int height = Mathf.CeilToInt(_messageTipText.preferredHeight) + 6;
            _messageTipText.rectTransform.PlaceTL(4, 3, width - 8, height - 6);
            ((RectTransform)_messageTip.transform).PlaceTL(Middle, Band != null ? BandY - height - 2 : MessageY + MessageLines * LineHeight + 2, width, height);
            _messageTip.transform.SetAsLastSibling();
            _messageTip.SetActive(true);
        }

        /// <summary>
        /// The message in its place: three lines over the pot (two under the result band). Lines past the last are folded into it with " · ";
        /// if that still runs over, the last line is cut short ("…") and the whole message is on the hover.
        /// </summary>
        private void LayOutMessage()
        {
            int lines = Band != null ? BandMessageLines : MessageLines;
            _message.rectTransform.PlaceTL(Middle, Band != null ? BandMessageY : MessageY, MiddleWidth, lines * LineHeight);
            _stakeInfo.gameObject.SetActive(Band == null);
            _powerHint.transform.localScale = Band == null ? Vector3.one : Vector3.zero;   // under the band the message has the column

            string[] parts = (_messageFull ?? "").Split('\n');
            if (parts.Length > lines)
                parts = parts.Take(lines - 1).Concat(new[] { string.Join(" · ", parts.Skip(lines - 1).Where(p => p.Trim().Length > 0)) }).ToArray();
            _message.text = string.Join("\n", parts);
            bool cut = false;
            while (_message.preferredHeight > lines * LineHeight + 1 && _message.text.Length > 1)
            {
                // Too long for its lines: cut the last line back a word at a time.
                string text = _message.text.TrimEnd('…', ' ');
                int space = text.LastIndexOf(' ');
                if (space <= 0) break;
                _message.text = text.Substring(0, space) + " …";
                cut = true;
            }
            _message.raycastTarget = cut;
            if (!cut) _messageTip.SetActive(false);
        }

        /// <summary>The dealer as the presenter sees it: the demon's portrait and talk, and the hall they sit in.</summary>
        private sealed class Stage : IDealerView
        {
            private readonly TableView _table;

            public Stage(TableView table) => _table = table;

            /// <summary>
            /// A new table: whatever was still animating from the last one finishes at once, then the hall (in its normal
            /// mood — the presenter re-applies the final stretch or the soul) and the portrait change together.
            /// </summary>
            public void SetDealer(DealerCard dealer, SeatChange change = SeatChange.Instant)
            {
                _finalTable = dealer.IsFinalTable;
                if (change == SeatChange.Instant)
                {
                    _table._sequencer.Complete();
                    _table._scenes.Abort();
                    _table._salon.SetSalon(dealer.Id, SalonMode.Normal);
                    _table._dealer.SetDealer(dealer);
                    return;
                }

                // Summoned or cast down: the change happens inside its scene, after the last word of the old demon.
                _table._scenes.Begin(change, () =>
                {
                    // Nothing of the old table's mood comes along (its last moments, its way out); the presenter re-applies
                    // whatever holds at the new one.
                    _table._finalStretch.SetActive(false, "");
                    _table._sentence.SetPulsing(false);
                    _table._dealer.SetFinalStretch(false);
                    _table._leaveButton.gameObject.SetActive(false);
                    _table._salon.SetSalon(dealer.Id, SalonMode.Normal);
                    _table._dealer.ShowDealer(dealer);
                });
            }

            /// <summary>True for the Morning Star: every line he speaks makes the screen shudder.</summary>
            private bool _finalTable;

            public void Relabel(DealerCard dealer) => _table._dealer.Relabel(dealer);

            public void SetLine(string line) => _table._dealer.SetLine(line);

            public void Say(string line, DealerMood mood)
            {
                _table._scenes.End();
                _table._dealer.Say(line, mood);
                if (_finalTable && !string.IsNullOrEmpty(line))
                    _table._scenes.Tremor();
            }
        }

        /// <summary>"ANTE", a gold coin with the amount, and the years spelled out — next to the deal button.</summary>
        private void BuildAnte(Transform screen)
        {
            RectTransform ante = UiFactory.CreateRect("Ante", screen).PlaceTL(Middle, ControlsY, 152, ButtonHeight);
            _ante = ante.gameObject;

            UiFactory.CreateText("Label", ante, "", 8, Palette.GoldLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline().Localized(() => UiText.StakeLabel)
                .rectTransform.PlaceTL(0, 6, 40, 8);

            Image coin = UiFactory.CreateSprite("Chip", ante, UiArt.Coin, Palette.Gold);
            coin.rectTransform.PlaceTL(40, 2, 16, 16);

            _anteAmount = UiFactory.CreateText("Amount", ante, "", 8, Palette.Bone, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline();
            _anteAmount.rectTransform.PlaceTL(60, 6, 92, 8);
            _anteAmount.horizontalOverflow = HorizontalWrapMode.Overflow;
            _ante.SetActive(false);
        }

        private Button CreateBetButton(Transform screen, string name, string label, BetAction action, int x, int width, ButtonSkin skin,
            out Text labelText)
        {
            Button button = UiFactory.CreateButton(name, screen, label, 8, out labelText, skin);
            ((RectTransform)button.transform).PlaceTL(x, ControlsY, width, ButtonHeight);
            button.onClick.AddListener(() => BetPressed?.Invoke(action));
            return button;
        }

        public void SetVisible(bool visible)
        {
            // Disable rendering and clicks but keep the GameObject alive, so queued animations still finish.
            if (!visible) _handRanks.Hide();
            _canvas.enabled = visible;
            GetComponent<GraphicRaycaster>().enabled = visible;
        }

        /// <summary>The power line (it stands where the win / lose outlook is, which comes back when it is off) and the ward's shield.</summary>
        public void SetPower(PowerDisplay power)
        {
            power = power ?? PowerDisplay.None;
            _sequencer.Do(() =>
            {
                _powerHint.text = power.Hint ?? "";
                _powerHint.color = power.Warning ? Palette.Hell : Palette.GoldLight;
                _powerHint.gameObject.SetActive(power.Hint != null);
                _stakeInfo.enabled = power.Hint == null;
                _malice.SetWard(power.WardUp);
            });
        }

        public void SetMessage(string text, Tone tone)
        {
            _sequencer.Do(() =>
            {
                _messageFull = text ?? "";
                _message.color = Palette.For(tone);
                LayOutMessage();
            });
        }

        public void SetAction(string label)
        {
            _scenes.End();   // a scene in progress lifts before the table is ready to play
            _sequencer.Do(() =>
            {
                _actionButton.gameObject.SetActive(label != null);
                _actionLabel.text = label ?? "";
            });
        }

        public void SetBetControls(BetControls controls)
        {
            _sequencer.Do(() => ApplyBetControls(controls));
        }

        private void ApplyBetControls(BetControls controls)
        {
            bool decide = controls.Visible && !controls.IsAnswer;
            bool checkToDraw = decide && controls.ShowCheckToDraw;
            _raiseButton.gameObject.SetActive(decide);
            _passButton.gameObject.SetActive(decide);
            _checkToDrawButton.gameObject.SetActive(checkToDraw);
            _callButton.gameObject.SetActive(controls.Visible && controls.IsAnswer);
            _foldButton.gameObject.SetActive(controls.Visible);
            _raiseLabel.text = controls.RaiseLabel ?? "";
            _callLabel.text = controls.CallLabel ?? "";
            _foldLabel.text = controls.FoldLabel ?? UiText.Fold;
            // Locked buttons stay clickable: the presenter answers with the reason.
            _raiseButton.GetComponent<ButtonFeel>().Locked = !controls.CanRaise;
            _passButton.GetComponent<ButtonFeel>().Locked = !controls.CanPass;
            _checkToDrawButton.GetComponent<ButtonFeel>().Locked = !controls.CanCheckToDraw;

            if (!PayoutTableShown)
            {
                // Phase 3: a chapter's table has no payouts panel, the row runs on into the right column and every word has room.
                if (checkToDraw)
                {
                    PlaceControl(_raiseButton, 0, 104);
                    PlaceControl(_passButton, 112, 64);
                    PlaceControl(_checkToDrawButton, 184, 80);
                    PlaceControl(_foldButton, 272, 72);
                }
                else
                {
                    PlaceControl(_raiseButton, 0, 104);
                    PlaceControl(_passButton, 112, 80);
                    PlaceControl(_foldButton, controls.IsAnswer ? 144 : 200, 80);
                }
            }
            else if (checkToDraw)
            {
                // Four buttons share the row: RAISE · PASS · CHECK TO DRAW · FOLD.
                PlaceControl(_raiseButton, 0, 88);
                PlaceControl(_passButton, 96, 40);
                PlaceControl(_checkToDrawButton, 144, 64);
                PlaceControl(_foldButton, 216, 40);
            }
            else
            {
                PlaceControl(_raiseButton, 0, 96);
                PlaceControl(_passButton, 104, 72);
                // Against a re-raise the fold button moves next to the call button.
                PlaceControl(_foldButton, controls.IsAnswer ? 144 : 184, 72);
            }
        }

        private static void PlaceControl(Button button, int x, int width)
        {
            ((RectTransform)button.transform).PlaceTL(Middle + x, ControlsY, width, ButtonHeight);
        }

        // ------------------------------------------------------------------ the payouts: a panel, or in the hands' panel (Phase 3)

        private Button _hands;
        private Text _handsLabel;
        private GameObject _handsTip;
        private Text _hint;
        private const int HintX = 126, WideHintX = 206, HandsWidth = 56, WideHandsWidth = 76;   // Demo 3.4: SAVE (144-200) before the hint

        /// <summary>Masa birliği: always false — no table has a payouts panel; they are in the hands' panel (H).</summary>
        public bool PayoutTableShown { get; private set; } = true;

        private void SetPayoutTableShown(bool shown)
        {
            PayoutTableShown = shown;
            ((PayoutTableView)Payouts).gameObject.SetActive(shown);
            _handsLabel.text = shown ? UiText.HandsButton : UiText.HandsPayoutsButton;
            ((RectTransform)_hands.transform).PlaceTL(64, BottomY, shown ? HandsWidth : WideHandsWidth, 18);
            int hintX = shown ? HintX : WideHintX;
            _hint.rectTransform.PlaceTL(hintX, BottomY + 5, W - 4 - hintX, 9);
            _handsTip.SetActive(false);
        }

        /// <summary>The hands button's hover (a chapter's table): what the panel holds and its key.</summary>
        private void BuildHandsTip(Transform screen)
        {
            Image tip = UiFactory.CreateImage("HandsTip", screen, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            Text text = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(() => UiText.HandsPayoutsHint);
            text.rectTransform.PlaceTL(4, 3, 152, 9);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            tip.rectTransform.PlaceTL(64, BottomY - 16, 160, 14);
            _handsTip = tip.gameObject;
            _handsTip.SetActive(false);

            var hover = _hands.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => _handsTip.SetActive(!PayoutTableShown));
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _handsTip.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
        }

        public void SetPot(int years)
        {
            _sequencer.Do(() => _pot.text = years > 0 ? string.Format(_currency == Currency.Bar ? UiText.BarPotFormat : UiText.PotFormat, years) : "");
        }

        public void SetStakeInfo(string text)
        {
            _sequencer.Do(() => _stakeInfo.text = text ?? "");
        }

        public void SetAnte(int years)
        {
            _sequencer.Do(() =>
            {
                _ante.SetActive(years > 0);
                _anteAmount.text = string.Format(_currency == Currency.Coins ? UiText.CoinAnteFormat
                    : _currency == Currency.Bar ? UiText.BarAnteFormat : UiText.AnteFormat, years);
            });
        }

        public void ShowHandRanks(Core.Game.IPayoutInfo payouts, string footnote = null, string notes = null)
        {
            _handsTip.SetActive(false);
            _handRanks.Show(payouts, footnote, notes);
        }

        /// <summary>The hands' panel (for tests and screenshots).</summary>
        public HandRanksPanel HandRanks => _handRanks;

        public void SetMalice(MaliceGauge gauge) => _malice.SetGauge(gauge);

        public void SetIntent(CheatCard intent) => _malice.SetIntent(intent);

        public void RevealLie(CheatCard truth) => _malice.RevealLie(truth);

        public void PlayCheat(CheatImpact impact) => _cheatEffects.Play(impact);

        public void HideHandRanks() => _handRanks.Hide();

        public void SetSoul(SoulGauge gauge)
        {
            _soul.SetGauge(gauge);
            _sequencer.Do(() =>
            {
                _soulVisible = gauge.Visible;
                _sentence.SetVisible(!gauge.Visible);
                _dealer.SetSoul(gauge.Visible);
                if (gauge.Visible)
                    _salon.SetMode(SalonMode.Soul);
                else if (_salon.Mode == SalonMode.Soul)
                    _salon.SetMode(_finalStretch.IsActive ? SalonMode.Hell : SalonMode.Normal);
            });
        }

        public void SetLeave(LeaveState state)
        {
            _sequencer.Do(() =>
            {
                _leaveButton.gameObject.SetActive(state != LeaveState.Hidden);
                bool locked = state == LeaveState.Locked || state == LeaveState.Summoned;
                _leaveLabel.text = state == LeaveState.Summoned ? UiText.NoEscape : locked ? UiText.SoulBound : UiText.LeaveTable;
                _leaveButton.GetComponent<ButtonFeel>().LabelColor = locked ? Palette.Hell : Palette.Bone;
            });
        }

        public void SetEnraged(bool enraged) => _sequencer.Do(() => _dealer.SetEnraged(enraged));

        private Image _carried;
        private GameObject _carriedTip;
        private Text _carriedTipText;

        /// <summary>The carried rule's icon as shown (for tests): its rule id, null for none.</summary>
        public string CarriedRuleShown { get; private set; }

        public void SetCarriedRule(string ruleId, string hover)
        {
            CarriedRuleShown = ruleId;
            _carried.gameObject.SetActive(ruleId != null);
            if (ruleId == null) return;
            Sprite icon = UiArt.CheatIcon(ruleId == "interest" ? "tithe" : "serpent_swap");
            Image picture = _carried.transform.Find("Icon").GetComponent<Image>();
            picture.sprite = icon;
            picture.color = icon != null ? Color.white : Palette.Ember;
            _carriedTipText.text = hover ?? "";
        }

        /// <summary>The carried rule's icon left of the demon's portrait (in the gap before the middle column), and its hover.</summary>
        private void BuildCarriedRule(Transform screen)
        {
            _carried = UiFactory.CreateImage("CarriedRule", screen, Palette.Black);
            _carried.rectTransform.PlaceTL(Layout.Dealer.x - 22, Layout.Dealer.y + 4, 20, 20);
            UiFactory.AddBorder(_carried.gameObject, Palette.Ember, 1f);
            _carried.raycastTarget = true;
            Image icon = UiFactory.CreateImage("Icon", _carried.transform, Color.white);
            icon.raycastTarget = false;
            icon.rectTransform.PlaceTL(2, 2, 16, 16);
            Image tip = UiFactory.CreateImage("CarriedTip", _carried.transform, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Ember, 1f);
            _carriedTipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _carriedTipText.rectTransform.PlaceTL(4, 3, 172, 40);
            tip.rectTransform.PlaceTL(-160, 22, 180, 46);
            _carriedTip = tip.gameObject;
            _carriedTip.SetActive(false);
            var hover = _carried.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => _carriedTip.SetActive(true));
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _carriedTip.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
            _carried.gameObject.SetActive(false);
        }

        public void SetFinalStretch(bool active, string banner)
        {
            _sequencer.Do(() =>
            {
                _finalStretch.SetActive(active, banner);
                _sentence.SetPulsing(active);
                _dealer.SetFinalStretch(active);
            });
        }

        /// <summary>Everything queued jumps to its end: cards land, counters arrive, the dealer finishes the sentence.</summary>
        public void SkipAnimations()
        {
            Audio.CutLong();   // long effects stop with the animations they belong to
            _scenes.End();
            _sequencer.Complete();
            _dealer.FinishLine();
            _sentence.Snap();
            _soul.Snap();
            _moments.Finish();
            _cheatEffects.Finish();
            _malice.Finish();
            _scenes.Finish();
            _panel.Finish();
            _fanfare.Finish();
            FinishResults();
        }

        public void PlayMoment(TableMoment moment, string text = null, System.Collections.Generic.IReadOnlyList<int> playerCards = null)
        {
            if (moment == TableMoment.WinGlow || moment == TableMoment.LossGlow)
            {
                Color color = moment == TableMoment.WinGlow ? Palette.Gold : Palette.Hell;
                _sequencer.Do(() => Glow(color));
                return;
            }
            // A backfire (and the Warlock's ward) belongs to the cheat's effects: it lands on the player's cards.
            if (moment == TableMoment.Backfire || moment == TableMoment.Ward)
                _cheatEffects.PlayBackfire(text, playerCards);
            else if (moment == TableMoment.JokerLaugh)
                _moments.Play(TableMoment.GoodHand, text, playerCards);   // the fool's laugh flares up as a good hand's name does
            else
                _moments.Play(moment, text, playerCards);
        }

        public void Pause(float seconds)
        {
            _sequencer.Wait(seconds);
        }

        private Currency _currency = Currency.Years;
        private bool _soulVisible;

        public void SetCurrency(Currency currency)
        {
            _sequencer.Do(() =>
            {
                _currency = currency;
                // A demon's table shows his bar under the portrait (no number) and the run's sentence in the corner (the soul's lamp in
                // its place while the soul is on the table).
                _sentence.SetNumberHidden(false);
                _sentence.SetVisible(!_soulVisible);
            });
        }
    }
}
