using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The bug report / idea box over the title menu: BUG or IDEA, a short text, SEND and BACK, and a line saying where the report went.
    /// Its own canvas above the menu's; a dark layer under the box swallows the clicks meant for the menu.
    /// </summary>
    public sealed class FeedbackView : MonoBehaviour, IFeedbackView
    {
        private const int SortingOrder = 130;
        private const int BoxWidth = 300, BoxHeight = 168;

        /// <summary>The longest report the box takes.</summary>
        public const int MaxCharacters = 400;

        private Canvas _canvas;
        private InputField _input;
        private Text _status, _bugLabel, _ideaLabel;
        private bool _bug = true;

        public event Action<bool, string> SendPressed;
        public event Action BackPressed;

        public bool IsOpen => _canvas.enabled;

        /// <summary>The text typed so far (for tests).</summary>
        public string Text
        {
            get => _input.text;
            set => _input.text = value ?? "";
        }

        public bool IsBug => _bug;
        public string Status => _status.text;

        public static FeedbackView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("FeedbackCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var view = canvas.gameObject.AddComponent<FeedbackView>();
            view._canvas = canvas;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            Image shade = UiFactory.CreateImage("FeedbackShade", screen, Color.clear);
            shade.rectTransform.Stretch();
            shade.raycastTarget = true;

            Image box = UiFactory.CreatePanel("FeedbackBox", screen, hot: true);
            box.rectTransform.PlaceTL((PixelScreen.Width - BoxWidth) / 2, (PixelScreen.Height - BoxHeight) / 2, BoxWidth, BoxHeight);
            Transform t = box.transform;

            UiFactory.CreateText("Title", t, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.FeedbackTitle)
                .rectTransform.PlaceTL(0, 8, BoxWidth, 8);

            Button bug = UiFactory.CreateButton("FeedbackBugButton", t, "", 8, out _bugLabel, ButtonSkin.Ash);
            ((RectTransform)bug.transform).PlaceTL(BoxWidth / 2 - 92, 22, 88, 16);
            bug.onClick.AddListener(() => SetKind(true));
            Button idea = UiFactory.CreateButton("FeedbackIdeaButton", t, "", 8, out _ideaLabel, ButtonSkin.Ash);
            ((RectTransform)idea.transform).PlaceTL(BoxWidth / 2 + 4, 22, 88, 16);
            idea.onClick.AddListener(() => SetKind(false));

            Image field = UiFactory.CreateImage("FeedbackInput", t, Palette.Black);
            field.rectTransform.PlaceTL(10, 44, BoxWidth - 20, 72);
            UiFactory.AddBorder(field.gameObject, Palette.BoneDark, 1f);
            Text text = UiFactory.CreateText("Text", field.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            text.rectTransform.PlaceTL(4, 3, BoxWidth - 28, 66);
            text.supportRichText = false;
            Text placeholder = UiFactory.CreateText("Placeholder", field.transform, "", 8, Palette.BoneDark, TextAnchor.UpperLeft)
                .Localized(() => UiText.FeedbackPlaceholder);
            placeholder.rectTransform.PlaceTL(4, 3, BoxWidth - 28, 66);
            _input = field.gameObject.AddComponent<InputField>();
            _input.targetGraphic = field;
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.lineType = InputField.LineType.MultiLineNewline;
            _input.characterLimit = MaxCharacters;
            _input.caretColor = Palette.GoldLight;
            _input.selectionColor = new Color(Palette.Plum.r, Palette.Plum.g, Palette.Plum.b, 0.8f);
            _input.customCaretColor = true;

            _status = UiFactory.CreateText("Status", t, "", 8, Palette.LilacLight, TextAnchor.UpperCenter).WithOutline();
            _status.rectTransform.PlaceTL(8, 120, BoxWidth - 16, 20);

            Button send = UiFactory.CreateButton("FeedbackSendButton", t, "", 8, out Text sendLabel, ButtonSkin.Ember);
            sendLabel.Localized(() => UiText.FeedbackSend);
            ((RectTransform)send.transform).PlaceTL(16, BoxHeight - 24, 104, 16);
            send.onClick.AddListener(() => SendPressed?.Invoke(_bug, _input.text));

            Button back = UiFactory.CreateButton("FeedbackBackButton", t, "", 8, out Text backLabel, ButtonSkin.Ash);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL(BoxWidth - 16 - 104, BoxHeight - 24, 104, 16);
            back.onClick.AddListener(() => BackPressed?.Invoke());

            Lang.Changed += Relabel;
        }

        private void OnDestroy() => Lang.Changed -= Relabel;

        private void SetKind(bool bug)
        {
            _bug = bug;
            Relabel();
        }

        /// <summary>The chosen kind is marked: &gt; BUG &lt;.</summary>
        private void Relabel()
        {
            _bugLabel.text = _bug ? "> " + UiText.FeedbackBug + " <" : UiText.FeedbackBug;
            _ideaLabel.text = !_bug ? "> " + UiText.FeedbackIdea + " <" : UiText.FeedbackIdea;
        }

        public void Show(bool bug)
        {
            SetKind(bug);
            _input.text = "";
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            _input.ActivateInputField();
        }

        public void Hide()
        {
            if (_input != null) _input.DeactivateInputField();
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        public void SetStatus(string status) => _status.text = status ?? "";
    }
}
