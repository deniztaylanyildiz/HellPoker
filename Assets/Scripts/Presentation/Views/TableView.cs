using System;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Builds and owns the whole table layout. Knows nothing about game rules; it only exposes widgets and input events.
    /// Every update goes through one <see cref="AnimationSequencer"/>, so the table shows things in the order they happen.
    /// Layout (1920×1080 reference): the dealer on the left, the card table in the middle, sentence and payouts on the right.
    /// </summary>
    public sealed class TableView : MonoBehaviour, ITableView
    {
        private static readonly Vector2 CardSize = new Vector2(160f, 224f);
        private const float CenterX = -40f;
        private const float TableY = 30f;
        private const float ControlsY = -462f;

        private AnimationSequencer _sequencer;
        private Text _message;
        private Text _pot;
        private Text _actionLabel;
        private Button _actionButton;
        private Button _raiseButton;
        private Text _raiseLabel;
        private Button _passButton;
        private Button _foldButton;
        private Button _callButton;
        private Text _callLabel;
        private Text _stakeInfo;
        private GameObject _ante;
        private Text _anteAmount;
        private Text _anteChip;
        private FinalStretchEffect _finalStretch;
        private SentenceView _sentence;
        private DealerView _dealer;

        public IHandView House { get; private set; }
        public IHandView Player { get; private set; }
        public ISentenceView Sentence => _sentence;
        public IPayoutView Payouts { get; private set; }
        public IDealerView Dealer => _dealer;

        public bool IsBusy => _sequencer.IsBusy;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action MenuPressed;

        public static TableView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateCanvas("TableCanvas", parent, 0);
            var view = canvas.gameObject.AddComponent<TableView>();
            view._sequencer = canvas.gameObject.AddComponent<AnimationSequencer>();
            view.Build(canvas.transform);
            return view;
        }

        private void Build(Transform root)
        {
            Image background = UiFactory.CreateSprite("Background", root, UiArt.Background, Palette.Background);
            background.rectTransform.Stretch();
            background.preserveAspect = false;
            background.raycastTarget = true;

            Image table = UiFactory.CreateSprite("Table", root, UiArt.Table, Palette.Felt);
            table.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, TableY), new Vector2(1070f, 800f));
            if (table.sprite == null)
                UiFactory.AddBorder(table.gameObject, Palette.CardBack, 3f);

            Image logo = UiFactory.CreateSprite("Title", root, UiArt.Title);
            logo.preserveAspect = true;
            logo.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(CenterX, 10f), new Vector2(600f, 152f), new Vector2(0.5f, 1f));
            if (logo.sprite == null)
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", logo.transform, UiText.Title, 52, Palette.Ember, style: FontStyle.Bold).rectTransform.Stretch();
            }

            _dealer = DealerView.Create(root, new Vector2(0f, 1f), new Vector2(40f, -36f), _sequencer);

            Button menu = UiFactory.CreateButton("MenuButton", root, UiText.Menu, 22, out _, ButtonSkin.Ash);
            ((RectTransform)menu.transform).Place(new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(170f, 56f), new Vector2(0f, 0f));
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            _sentence = SentenceView.Create(root, new Vector2(1f, 1f), new Vector2(-50f, -40f), new Vector2(1f, 1f), _sequencer);
            Payouts = PayoutTableView.Create(root, new Vector2(1f, 1f), new Vector2(-50f, -262f), new Vector2(1f, 1f), _sequencer);

            House = HandView.Create(root, "HouseHand", new Vector2(CenterX, TableY + 182f), CardSize, 22f, captionAbove: true, _sequencer);
            Player = HandView.Create(root, "PlayerHand", new Vector2(CenterX, TableY - 182f), CardSize, 22f, captionAbove: false, _sequencer);

            _message = UiFactory.CreateText("Message", root, "", 29, Palette.Bone).WithShadow(2f);
            _message.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, TableY + 50f), new Vector2(980f, 40f));
            _pot = UiFactory.CreateText("Pot", root, "", 24, Palette.Gold, style: FontStyle.Bold).WithShadow(2f);
            _pot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, TableY + 12f), new Vector2(800f, 32f));
            _stakeInfo = UiFactory.CreateText("StakeInfo", root, "", 20, Palette.PaleGold, style: FontStyle.Italic).WithShadow();
            _stakeInfo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, TableY - 18f), new Vector2(900f, 26f));

            BuildAnte(root);

            _actionButton = UiFactory.CreateButton("ActionButton", root, "", 32, out _actionLabel, ButtonSkin.Ember);
            ((RectTransform)_actionButton.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX + 400f, ControlsY), new Vector2(300f, 84f));
            _actionButton.onClick.AddListener(() => ActionPressed?.Invoke());

            _raiseButton = CreateBetButton(root, "RaiseButton", "", BetAction.Raise, CenterX - 300f, ButtonSkin.Ember, out _raiseLabel);
            _passButton = CreateBetButton(root, "PassButton", UiText.Pass, BetAction.Pass, CenterX, ButtonSkin.Blood, out _);
            _foldButton = CreateBetButton(root, "FoldButton", UiText.Fold, BetAction.Fold, CenterX + 300f, ButtonSkin.Ash, out _);
            _callButton = CreateBetButton(root, "CallButton", "", BetAction.Call, CenterX - 150f, ButtonSkin.Ember, out _callLabel);
            ApplyBetControls(BetControls.Hidden);

            UiFactory.CreateText("Hint", root, UiText.Hint, 18, Palette.MutedText, style: FontStyle.Italic).WithShadow()
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(CenterX + 60f, 14f), new Vector2(1200f, 28f));

            // Last, so the hellfire glow draws over the table without blocking clicks.
            _finalStretch = FinalStretchEffect.Create(root, background, table, new Vector2(CenterX, -132f));
        }

        /// <summary>"ANTE" and a gold chip with the amount, where the deal happens.</summary>
        private void BuildAnte(Transform root)
        {
            RectTransform ante = UiFactory.CreateRect("Ante", root).Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX - 120f, ControlsY), new Vector2(440f, 96f));
            _ante = ante.gameObject;

            UiFactory.CreateText("Label", ante, UiText.StakeLabel, 30, Palette.Gold, TextAnchor.MiddleRight, FontStyle.Bold).WithShadow()
                .rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(120f, 96f), new Vector2(0f, 0.5f));

            Image chip = UiFactory.CreateSprite("Chip", ante, UiArt.ChipSelected, Palette.Ember);
            chip.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(92f, 92f), new Vector2(0f, 0.5f));
            _anteChip = UiFactory.CreateText("Unit", chip.transform, "", 26, Palette.Ink, style: FontStyle.Bold);
            _anteChip.rectTransform.Stretch();

            _anteAmount = UiFactory.CreateText("Amount", ante, "", 30, Palette.Bone, TextAnchor.MiddleLeft, FontStyle.Bold).WithShadow();
            _anteAmount.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(250f, 0f), new Vector2(200f, 96f), new Vector2(0f, 0.5f));
            _ante.SetActive(false);
        }

        private Button CreateBetButton(Transform root, string name, string label, BetAction action, float x, ButtonSkin skin, out Text labelText)
        {
            Button button = UiFactory.CreateButton(name, root, label, 30, out labelText, skin);
            ((RectTransform)button.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(x, ControlsY), new Vector2(270f, 84f));
            button.onClick.AddListener(() => BetPressed?.Invoke(action));
            return button;
        }

        public void SetVisible(bool visible)
        {
            // Disable rendering and clicks but keep the GameObject alive, so queued animations still finish.
            GetComponent<Canvas>().enabled = visible;
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
            var fold = (RectTransform)_foldButton.transform;
            fold.anchoredPosition = new Vector2(controls.IsAnswer ? CenterX + 150f : CenterX + 300f, ControlsY);
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
                _anteChip.text = years.ToString();
            });
        }

        public void SetFinalStretch(bool active, string banner)
        {
            _sequencer.Do(() =>
            {
                _finalStretch.SetActive(active, banner);
                _sentence.SetPulsing(active);
            });
        }

        public void Pause(float seconds)
        {
            _sequencer.Wait(seconds);
        }
    }
}
