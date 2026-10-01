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

        public IHandView House { get; private set; }
        public IHandView Player { get; private set; }
        public ISentenceView Sentence => _sentence;
        public IPayoutView Payouts { get; private set; }
        public IDealerView Dealer => _stage;

        public bool IsBusy => _sequencer.IsBusy;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action MenuPressed;
        public event Action LeavePressed;

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
            // The demon's hall fills the screen behind everything.
            _salon = SalonView.Create(screen, salons);

            Image logo = UiFactory.CreateSprite("Title", screen, UiArt.Title);
            if (logo.sprite != null)
            {
                Vector2 size = logo.sprite.rect.size;
                logo.rectTransform.PlaceTL(Middle + (MiddleWidth - (int)size.x) / 2, 3, (int)size.x, (int)size.y);
            }
            else
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", screen, UiText.Title, 16, Palette.Hell, style: FontStyle.Bold).rectTransform.PlaceTL(Middle, 4, MiddleWidth, 16);
            }

            // Left column: the dealer.
            _dealer = DealerView.Create(screen, 4, 4, _sequencer, dealers);

            Button menu = UiFactory.CreateButton("MenuButton", screen, UiText.Menu, 8, out _, ButtonSkin.Ash);
            ((RectTransform)menu.transform).PlaceTL(4, 248, 56, 18);
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            _leaveButton = UiFactory.CreateButton("LeaveButton", screen, UiText.LeaveTable, 8, out _leaveLabel, ButtonSkin.Ash);
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

            _message = UiFactory.CreateText("Message", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithShadow();
            _message.rectTransform.PlaceTL(Middle, 91, MiddleWidth, 18);
            _pot = UiFactory.CreateText("Pot", screen, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithShadow();
            _pot.rectTransform.PlaceTL(Middle, 111, MiddleWidth, 8);
            _stakeInfo = UiFactory.CreateText("StakeInfo", screen, "", 8, Palette.BoneMid).WithShadow();
            _stakeInfo.rectTransform.PlaceTL(Middle, 123, MiddleWidth, 9);
            _stakeInfo.horizontalOverflow = HorizontalWrapMode.Overflow;

            BuildAnte(screen);

            _actionButton = UiFactory.CreateButton("ActionButton", screen, "", 8, out _actionLabel, ButtonSkin.Ember);
            ((RectTransform)_actionButton.transform).PlaceTL(Middle + MiddleWidth - 96, ControlsY, 96, ButtonHeight);
            _actionButton.onClick.AddListener(() => ActionPressed?.Invoke());

            _raiseButton = CreateBetButton(screen, "RaiseButton", "", BetAction.Raise, Middle, 96, ButtonSkin.Ember, out _raiseLabel);
            _passButton = CreateBetButton(screen, "PassButton", UiText.Pass, BetAction.Pass, Middle + 104, 72, ButtonSkin.Blood, out _);
            _foldButton = CreateBetButton(screen, "FoldButton", UiText.Fold, BetAction.Fold, Middle + 184, 72, ButtonSkin.Ash, out _);
            _callButton = CreateBetButton(screen, "CallButton", "", BetAction.Call, Middle + 24, 112, ButtonSkin.Ember, out _callLabel);
            ApplyBetControls(BetControls.Hidden);

            UiFactory.CreateText("Hint", screen, UiText.Hint, 8, Palette.BoneDark, TextAnchor.MiddleLeft).WithShadow()
                .rectTransform.PlaceTL(68, 253, 408, 9);

            _finalStretch = FinalStretchEffect.Create(screen, _salon, Middle, 230, MiddleWidth);
            _stage = new Stage(this);
        }

        /// <summary>The dealer as the presenter sees it: the demon's portrait and talk, and the hall they sit in.</summary>
        private sealed class Stage : IDealerView
        {
            private readonly TableView _table;

            public Stage(TableView table) => _table = table;

            public void SetDealer(DealerCard dealer)
            {
                _table._sequencer.Do(() => _table._salon.SetSalon(dealer.Id));
                _table._dealer.SetDealer(dealer);
            }

            public void Say(string line, DealerMood mood) => _table._dealer.Say(line, mood);
        }

        /// <summary>"ANTE", a gold coin with the amount, and the years spelled out — next to the deal button.</summary>
        private void BuildAnte(Transform screen)
        {
            RectTransform ante = UiFactory.CreateRect("Ante", screen).PlaceTL(Middle, ControlsY, 152, ButtonHeight);
            _ante = ante.gameObject;

            UiFactory.CreateText("Label", ante, UiText.StakeLabel, 8, Palette.GoldLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 6, 40, 8);

            Image coin = UiFactory.CreateSprite("Chip", ante, UiArt.Coin, Palette.Gold);
            coin.rectTransform.PlaceTL(40, 2, 16, 16);

            _anteAmount = UiFactory.CreateText("Amount", ante, "", 8, Palette.Bone, TextAnchor.MiddleLeft, FontStyle.Bold).WithShadow();
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
            _raiseButton.gameObject.SetActive(decide);
            _passButton.gameObject.SetActive(decide);
            _callButton.gameObject.SetActive(controls.Visible && controls.IsAnswer);
            _foldButton.gameObject.SetActive(controls.Visible);
            _raiseLabel.text = controls.RaiseLabel ?? "";
            _callLabel.text = controls.CallLabel ?? "";
            _raiseButton.interactable = controls.CanRaise;
            _passButton.interactable = controls.CanPass;

            // Against a re-raise the fold button moves next to the call button.
            ((RectTransform)_foldButton.transform).PlaceTL(controls.IsAnswer ? Middle + 144 : Middle + 184, ControlsY, 72, ButtonHeight);
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
                bool locked = state == LeaveState.Locked;
                _leaveLabel.text = locked ? UiText.SoulBound : UiText.LeaveTable;
                _leaveLabel.color = locked ? Palette.Hell : Palette.Bone;
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

        public void Pause(float seconds)
        {
            _sequencer.Wait(seconds);
        }
    }
}
