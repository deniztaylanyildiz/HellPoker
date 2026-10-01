using System;
using System.Collections.Generic;
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
    /// Layout is authored for a 1920×1080 reference resolution.
    /// </summary>
    public sealed class TableView : MonoBehaviour, ITableView
    {
        private static readonly Vector2 CardSize = new Vector2(160f, 224f);
        private const float CenterX = -160f;
        private const float ControlsY = -455f;

        private AnimationSequencer _sequencer;
        private Text _message;
        private Text _pot;
        private Text _actionLabel;
        private Button _actionButton;
        private Button _raiseButton;
        private Text _raiseLabel;
        private Button _passButton;
        private Button _foldButton;
        private FinalStretchEffect _finalStretch;
        private SentenceView _sentence;

        public IHandView House { get; private set; }
        public IHandView Player { get; private set; }
        public ISentenceView Sentence => _sentence;
        public IStakeSelectorView Stakes { get; private set; }
        public IPayoutView Payouts { get; private set; }

        public bool IsBusy => _sequencer.IsBusy;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action MenuPressed;

        public static TableView Create(Transform parent, IPayoutInfo payouts, IReadOnlyList<int> stakeOptions)
        {
            Canvas canvas = CreateCanvas(parent);
            var view = canvas.gameObject.AddComponent<TableView>();
            view._sequencer = canvas.gameObject.AddComponent<AnimationSequencer>();
            view.Build(canvas.transform, payouts, stakeOptions);
            return view;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("TableCanvas", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void Build(Transform root, IPayoutInfo payouts, IReadOnlyList<int> stakeOptions)
        {
            Image background = UiFactory.CreateImage("Background", root, Palette.Background);
            background.rectTransform.Stretch();

            Image felt = UiFactory.CreateImage("Felt", root, Palette.Felt);
            felt.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, -5f), new Vector2(1180f, 780f));
            UiFactory.AddBorder(felt.gameObject, Palette.CardBack, 3f);

            UiFactory.CreateText("Title", root, UiText.Title, 60, Palette.Ember, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(600f, 80f), new Vector2(0f, 1f));
            UiFactory.CreateText("Subtitle", root, UiText.Subtitle, 22, Palette.MutedText, TextAnchor.UpperLeft)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(44f, -96f), new Vector2(600f, 30f), new Vector2(0f, 1f));

            Button menu = UiFactory.CreateButton("MenuButton", root, UiText.Menu, 22, out _);
            ((RectTransform)menu.transform).Place(new Vector2(0f, 1f), new Vector2(44f, -142f), new Vector2(150f, 46f), new Vector2(0f, 1f));
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            _sentence = SentenceView.Create(root, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(1f, 1f), _sequencer);
            Payouts = PayoutTableView.Create(root, new Vector2(1f, 0.5f), new Vector2(-30f, -60f), new Vector2(1f, 0.5f), payouts, _sequencer);

            House = HandView.Create(root, "HouseHand", new Vector2(CenterX, 190f), CardSize, 24f, captionAbove: true, _sequencer);
            Player = HandView.Create(root, "PlayerHand", new Vector2(CenterX, -190f), CardSize, 24f, captionAbove: false, _sequencer);

            _message = UiFactory.CreateText("Message", root, "", 30, Palette.Bone, style: FontStyle.Bold);
            _message.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, 22f), new Vector2(1100f, 80f));
            _pot = UiFactory.CreateText("Pot", root, "", 24, Palette.Gold, style: FontStyle.Bold);
            _pot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX, -36f), new Vector2(800f, 34f));

            Stakes = StakeSelectorView.Create(root, new Vector2(CenterX - 200f, ControlsY), stakeOptions, _sequencer);

            _actionButton = UiFactory.CreateButton("ActionButton", root, "", 34, out _actionLabel);
            ((RectTransform)_actionButton.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(CenterX + 400f, ControlsY), new Vector2(300f, 84f));
            _actionButton.onClick.AddListener(() => ActionPressed?.Invoke());

            _raiseButton = CreateBetButton(root, "RaiseButton", "", BetAction.Raise, CenterX - 310f, Palette.Ember, out _raiseLabel);
            _passButton = CreateBetButton(root, "PassButton", UiText.Pass, BetAction.Pass, CenterX, Palette.Button, out _);
            _foldButton = CreateBetButton(root, "FoldButton", UiText.Fold, BetAction.Fold, CenterX + 310f, Palette.Fold, out _);
            ApplyBetControls(BetControls.Hidden);

            UiFactory.CreateText("Hint", root, UiText.Hint, 18, Palette.MutedText)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(CenterX - 150f, 20f), new Vector2(1000f, 30f));

            // Last, so the hellfire glow draws over the table without blocking clicks.
            _finalStretch = FinalStretchEffect.Create(root, background, felt, new Vector2(CenterX, -135f));
        }

        private Button CreateBetButton(Transform root, string name, string label, BetAction action, float x, Color color, out Text labelText)
        {
            Button button = UiFactory.CreateButton(name, root, label, 32, out labelText);
            ((RectTransform)button.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(x, ControlsY), new Vector2(280f, 84f));
            ((Image)button.targetGraphic).color = color;
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
            _raiseButton.gameObject.SetActive(controls.Visible);
            _passButton.gameObject.SetActive(controls.Visible);
            _foldButton.gameObject.SetActive(controls.Visible);
            _raiseLabel.text = controls.RaiseLabel ?? "";
            _raiseButton.interactable = controls.CanRaise;
            _passButton.interactable = controls.CanPass;
        }

        public void SetPot(int years)
        {
            _sequencer.Do(() => _pot.text = years > 0 ? string.Format(UiText.PotFormat, years) : "");
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
