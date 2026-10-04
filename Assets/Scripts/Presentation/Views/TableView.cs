using System;
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
        // Middle column.
        private const int Middle = 112;
        private const int MiddleWidth = 256;
        private const int CardSpacing = 8;
        private const int ControlsY = 204;
        private const int ButtonHeight = 20;

        private AnimationSequencer _sequencer;
        private Canvas _canvas;
        private Text _message;
        private Text _pot;
        private Text _stakeInfo;
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

        private RelicBarView _relics;

        public void SetRelics(System.Collections.Generic.IReadOnlyList<RelicBadge> relics) => _relics.Set(relics);

        private EventPanelView _event;

        /// <summary>The event panel (for tests and screenshots).</summary>
        public EventPanelView EventPanel => _event;

        public void ShowEvent(EventCard card) => _event.Show(card);

        /// <summary>Where the table's effects sound (silent until the bootstrap gives it the game's audio).</summary>
        public IAudio Audio { get; set; } = NullAudio.Instance;

        public void PlaySfx(string sfxId) => _sequencer.Do(() => Audio.PlaySfx(sfxId));

        public void HideEvent() => _event.Hide();

        private SinnerBadgeView _sinner;

        public void SetSinner(SinnerBadge badge) => _sinner.Set(badge);

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
                logo.rectTransform.PlaceTL(Middle + (MiddleWidth - (int)size.x) / 2, 3, (int)size.x, (int)size.y);
            }
            else
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", screen, "", 16, Palette.Hell, style: FontStyle.Bold).WithOutline().Localized(() => UiText.Title)
                    .rectTransform.PlaceTL(Middle, 4, MiddleWidth, 16);
            }

            // Left column: the dealer.
            _dealer = DealerView.Create(screen, 4, 4, _sequencer, dealers);

            Button menu = UiFactory.CreateButton("MenuButton", screen, "", 8, out Text menuLabel, ButtonSkin.Ash);
            menuLabel.Localized(() => UiText.Menu);
            ((RectTransform)menu.transform).PlaceTL(4, 248, 56, 18);
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            Button hands = UiFactory.CreateButton("HandsButton", screen, "", 8, out Text handsLabel, ButtonSkin.Ash);
            handsLabel.Localized(() => UiText.HandsButton);
            ((RectTransform)hands.transform).PlaceTL(64, 248, 56, 18);
            hands.onClick.AddListener(() => HandRanksPressed?.Invoke());

            _leaveButton = UiFactory.CreateButton("LeaveButton", screen, "", 8, out _leaveLabel, ButtonSkin.Ash);
            ((RectTransform)_leaveButton.transform).PlaceTL(4, 210, 104, 18);
            _leaveButton.onClick.AddListener(() => LeavePressed?.Invoke());
            _leaveButton.gameObject.SetActive(false);

            // Right column: the sentence (or the soul) and the dealer's payouts.
            _sentence = SentenceView.Create(screen, 372, 4, _sequencer);
            _soul = SoulView.Create(screen, 372, 4, _sequencer);
            Payouts = PayoutTableView.Create(screen, 372, 62, 178, _sequencer);

            // Middle column: house cards, the talk of the table, player cards, the bet controls.
            int handWidth = CardView.Size.x * 5 + CardSpacing * 4;
            int handX = Middle + (MiddleWidth - handWidth) / 2;
            House = HandView.Create(screen, "HouseHand", handX, 38, CardSpacing, 28, _sequencer);
            Player = HandView.Create(screen, "PlayerHand", handX, 140, CardSpacing, 192, _sequencer);

            _message = UiFactory.CreateText("Message", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline();
            _message.rectTransform.PlaceTL(Middle, 91, MiddleWidth, 18);
            _pot = UiFactory.CreateText("Pot", screen, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _pot.rectTransform.PlaceTL(Middle, 111, MiddleWidth, 8);
            _stakeInfo = UiFactory.CreateText("StakeInfo", screen, "", 8, Palette.BoneMid).WithOutline();
            _stakeInfo.rectTransform.PlaceTL(Middle, 123, MiddleWidth, 9);
            _stakeInfo.horizontalOverflow = HorizontalWrapMode.Overflow;

            BuildAnte(screen);

            _actionButton = UiFactory.CreateButton("ActionButton", screen, "", 8, out _actionLabel, ButtonSkin.Ember);
            ((RectTransform)_actionButton.transform).PlaceTL(Middle + MiddleWidth - 96, ControlsY, 96, ButtonHeight);
            _actionButton.onClick.AddListener(() => ActionPressed?.Invoke());

            _raiseButton = CreateBetButton(screen, "RaiseButton", "", BetAction.Raise, Middle, 96, ButtonSkin.Ember, out _raiseLabel);
            _passButton = CreateBetButton(screen, "PassButton", "", BetAction.Pass, Middle + 104, 72, ButtonSkin.Blood, out Text passLabel);
            passLabel.Localized(() => UiText.Pass);
            _foldButton = CreateBetButton(screen, "FoldButton", "", BetAction.Fold, Middle + 184, 72, ButtonSkin.Ash, out Text foldLabel);
            foldLabel.Localized(() => UiText.Fold);
            _callButton = CreateBetButton(screen, "CallButton", "", BetAction.Call, Middle + 24, 112, ButtonSkin.Ember, out _callLabel);
            // Two short lines, so four buttons fit the row in the 8 px title font.
            _checkToDrawButton = UiFactory.CreateButton("CheckToDrawButton", screen, "", 8, out Text checkLabel, ButtonSkin.Blood);
            checkLabel.Localized(() => UiText.CheckToDrawButton);
            checkLabel.verticalOverflow = VerticalWrapMode.Overflow;
            checkLabel.lineSpacing = 1f;
            _checkToDrawButton.onClick.AddListener(() => CheckToDrawPressed?.Invoke());
            ApplyBetControls(BetControls.Hidden);

            UiFactory.CreateText("Hint", screen, "", 8, Palette.BoneDark, TextAnchor.MiddleLeft).WithOutline().Localized(() => UiText.Hint)
                .rectTransform.PlaceTL(126, 253, 350, 9);

            _finalStretch = FinalStretchEffect.Create(screen, _salon, Middle, 230, MiddleWidth);
            _moments = TableMoments.Create(screen, _sequencer, (HandView)Player, _sentence);
            // The malice gauge and the announced cheat ride on the portrait box (4, 4, 104 × 104).
            _malice = MaliceView.Create(screen, 4, 4, DealerView.PortraitSize + 8, _sequencer);
            // The class badge in the portrait box's lower right corner (the malice pips run along the lower left).
            _sinner = SinnerBadgeView.Create(screen, 4 + DealerView.PortraitSize + 8 - 34, 4 + DealerView.PortraitSize + 8 - 22, _sequencer,
                () => SinnerPressed?.Invoke());
            _cheatEffects = CheatEffects.Create(screen, _sequencer, (HandView)Player, (HandView)House, new Vector2Int(56, 56));
            _scenes = TableScenes.Create(screen, _sequencer, _dealer);
            // The relics down the portrait box's right edge, under the intent sign.
            _relics = RelicBarView.Create(screen, 4 + DealerView.PortraitSize + 8 - 22, 24, _sequencer, id => RelicPressed?.Invoke(id));
            _event = EventPanelView.Create(screen, Middle, 60, _sequencer, dealers, index => EventOptionPressed?.Invoke(index));
            _handRanks = HandRanksPanel.Create(screen, (PixelScreen.Width - HandRanksPanel.Width) / 2, 40, () => UiText.HandRanksTableFooter);
            _stage = new Stage(this);
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

        public void SetMessage(string text, Tone tone)
        {
            _sequencer.Do(() =>
            {
                _message.text = text;
                _message.color = Palette.For(tone);
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
            // Locked buttons stay clickable: the presenter answers with the reason.
            _raiseButton.GetComponent<ButtonFeel>().Locked = !controls.CanRaise;
            _passButton.GetComponent<ButtonFeel>().Locked = !controls.CanPass;
            _checkToDrawButton.GetComponent<ButtonFeel>().Locked = !controls.CanCheckToDraw;

            if (checkToDraw)
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

        public void SetPot(int years)
        {
            _sequencer.Do(() => _pot.text = years > 0 ? string.Format(UiText.PotFormat, years) : "");
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
                _anteAmount.text = string.Format(UiText.AnteFormat, years);
            });
        }

        public void ShowHandRanks(Core.Game.IPayoutInfo payouts, string footnote = null) => _handRanks.Show(payouts, footnote);

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
        }

        public void PlayMoment(TableMoment moment, string text = null, System.Collections.Generic.IReadOnlyList<int> playerCards = null)
        {
            // A backfire (and the Warlock's ward) belongs to the cheat's effects: it lands on the player's cards.
            if (moment == TableMoment.Backfire || moment == TableMoment.Ward)
                _cheatEffects.PlayBackfire(text, playerCards);
            else
                _moments.Play(moment, text, playerCards);
        }

        public void Pause(float seconds)
        {
            _sequencer.Wait(seconds);
        }
    }
}
