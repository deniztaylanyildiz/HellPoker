using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Title screen drawn above the table: New Game / Continue / How to Play / Quit, and a rules panel.
    /// The rules panel is pure view state, so it is handled here rather than in a presenter.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        private const int SortingOrder = 100;
        private const int ButtonWidth = 128;
        private const int ButtonHeight = 20;

        private Canvas _canvas;
        private const int GridTop = 132;
        private const int GridGap = 6;

        private GameObject _continueButton;
        private GameObject _changeTableButton;
        private readonly System.Collections.Generic.List<GameObject> _grid = new System.Collections.Generic.List<GameObject>();
        private GameObject _quitButton;
        private GameObject _front;
        private GameObject _rulesPanel;
        private GameObject _rulesBody;
        private HandRanksPanel _handRanks;
        private Text _pageLabel;
        private Core.Game.IPayoutInfo _payouts;
        private RectTransform _logo;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;
        public event Action ChangeTablePressed;
        public event Action SettingsPressed;
        public event Action RecordsPressed;

        public bool IsVisible => _canvas.enabled;

        /// <param name="payouts">What the hands pay at a standard table, for the hand ranking page of the rules.</param>
        public static MainMenuView Create(Transform parent, string tagline, string rules, Core.Game.IPayoutInfo payouts)
        {
            Canvas canvas = UiFactory.CreateScreen("MainMenuCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            view._canvas = canvas;
            view._payouts = payouts;
            view.Build(screen, tagline, rules);
            return view;
        }

        private void Build(RectTransform screen, string tagline, string rules)
        {
            // Opaque background also blocks clicks from reaching the table underneath.
            Image background = UiFactory.CreateSprite("Background", screen, UiArt.Background, Palette.Night);
            background.rectTransform.Stretch();
            background.raycastTarget = true;

            _front = UiFactory.CreateRect("Front", screen).Stretch().gameObject;
            Transform front = _front.transform;

            // The logo at twice its size: a whole-number scale keeps the pixels square.
            Image logo = UiFactory.CreateSprite("Title", front, UiArt.Title);
            _logo = logo.rectTransform;
            if (logo.sprite != null)
            {
                Vector2 size = logo.sprite.rect.size * 2f;
                _logo.PlaceTL((PixelScreen.Width - (int)size.x) / 2, 30, (int)size.x, (int)size.y);
            }
            else
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", front, UiText.Title, 32, Palette.Hell, style: FontStyle.Bold).rectTransform.PlaceTL(0, 34, PixelScreen.Width, 32);
            }

            Text taglineText = UiFactory.CreateText("Tagline", front, tagline, 8, Palette.Bone, TextAnchor.UpperCenter).WithShadow();
            taglineText.rectTransform.PlaceTL(40, 88, PixelScreen.Width - 80, 18);
            UiFactory.CreateSprite("Divider", front, UiArt.Divider).rectTransform.PlaceTL((PixelScreen.Width - 48) / 2, 110, 48, 3);
            UiFactory.CreateText("Subtagline", front, UiText.MenuSubtagline, 8, Palette.BoneMid).WithShadow()
                .rectTransform.PlaceTL(40, 117, PixelScreen.Width - 80, 9);

            // Two columns in reading order; buttons that are hidden leave no gap (see LayOut).
            Transform buttons = UiFactory.CreateRect("Buttons", front).Stretch();
            _continueButton = CreateMenuButton(buttons, "ContinueButton", UiText.Continue, ButtonSkin.Ember, () => ContinuePressed?.Invoke());
            _changeTableButton = CreateMenuButton(buttons, "ChangeTableButton", UiText.ChangeTable, ButtonSkin.Ash, () => ChangeTablePressed?.Invoke());
            GameObject newGame = CreateMenuButton(buttons, "NewGameButton", UiText.NewGame, ButtonSkin.Blood, () => NewGamePressed?.Invoke());
            GameObject settings = CreateMenuButton(buttons, "SettingsButton", UiText.SettingsButton, ButtonSkin.Ash, () => SettingsPressed?.Invoke());
            GameObject howToPlay = CreateMenuButton(buttons, "HowToPlayButton", UiText.HowToPlay, ButtonSkin.Ash, () => ShowRules(true));
            GameObject records = CreateMenuButton(buttons, "RecordsButton", UiText.Records, ButtonSkin.Ash, () => RecordsPressed?.Invoke());
            _grid.AddRange(new[] { _continueButton, _changeTableButton, newGame, settings, howToPlay, records });
            _quitButton = CreateMenuButton(buttons, "QuitButton", UiText.Quit, ButtonSkin.Ash, () => QuitPressed?.Invoke());
            LayOut();

            UiFactory.CreateText("Footer", front, UiText.MenuFooter, 8, Palette.BoneDark).rectTransform.PlaceTL(0, 254, PixelScreen.Width, 9);

            _rulesPanel = BuildRulesPanel(screen, rules);
            ShowRules(false);
        }

        private GameObject BuildRulesPanel(Transform screen, string rules)
        {
            Image panel = UiFactory.CreatePanel("RulesPanel", screen);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(8, 8, PixelScreen.Width - 16, PixelScreen.Height - 16);

            UiFactory.CreateText("Title", panel.transform, UiText.RulesTitle, 8, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 8, PixelScreen.Width - 16, 8);
            UiFactory.CreateSprite("Divider", panel.transform, UiArt.Divider).rectTransform.PlaceTL((PixelScreen.Width - 16 - 48) / 2, 19, 48, 3);

            Text body = UiFactory.CreateText("Body", panel.transform, rules, 8, Palette.Bone, TextAnchor.UpperLeft);
            body.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            body.lineSpacing = 1f;
            _rulesBody = body.gameObject;

            _handRanks = HandRanksPanel.Create(panel.transform, (PixelScreen.Width - 16 - HandRanksPanel.Width) / 2, 30);

            // RULES / HANDS page switch, then BACK.
            int buttonsY = PixelScreen.Height - 16 - 26;
            Button page = UiFactory.CreateButton("HandRanksPageButton", panel.transform, UiText.HandsButton, 8, out _pageLabel, ButtonSkin.Ash);
            ((RectTransform)page.transform).PlaceTL((PixelScreen.Width - 16) / 2 - 84, buttonsY, 80, ButtonHeight);
            page.onClick.AddListener(() => ShowHandRanks(!_handRanks.IsOpen));

            Button back = UiFactory.CreateButton("BackButton", panel.transform, UiText.Back, 8, out _, ButtonSkin.Blood);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 16) / 2 + 4, buttonsY, 80, ButtonHeight);
            back.onClick.AddListener(() => ShowRules(false));
            return panel.gameObject;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string label, ButtonSkin skin, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, label, 8, out _, skin);
            ((RectTransform)button.transform).PlaceTL(0, 0, ButtonWidth, ButtonHeight);
            button.onClick.AddListener(() => onClick());
            return button.gameObject;
        }

        /// <summary>Places the visible buttons two to a row, in order, with Quit centred on its own row below.</summary>
        private void LayOut()
        {
            int left = (PixelScreen.Width - 2 * ButtonWidth - GridGap) / 2;
            int slot = 0;
            foreach (GameObject button in _grid)
            {
                if (!button.activeSelf) continue;
                int x = left + slot % 2 * (ButtonWidth + GridGap);
                int y = GridTop + slot / 2 * (ButtonHeight + GridGap);
                ((RectTransform)button.transform).PlaceTL(x, y, ButtonWidth, ButtonHeight);
                slot++;
            }

            int quitY = GridTop + (slot + 1) / 2 * (ButtonHeight + GridGap);
            ((RectTransform)_quitButton.transform).PlaceTL((PixelScreen.Width - ButtonWidth) / 2, quitY, ButtonWidth, ButtonHeight);
        }

        public bool CloseOverlay()
        {
            if (!_rulesPanel.activeSelf) return false;
            if (_handRanks.IsOpen)
                ShowHandRanks(false);   // the hands page goes back to the rules first
            else
                ShowRules(false);
            return true;
        }

        private void ShowHandRanks(bool show)
        {
            if (show)
                _handRanks.Show(_payouts);
            else
                _handRanks.Hide();
            _rulesBody.SetActive(!show);
            _pageLabel.text = show ? UiText.RulesButton : UiText.HandsButton;
        }

        private void ShowRules(bool show)
        {
            ShowHandRanks(false);
            _rulesPanel.SetActive(show);
            _front.SetActive(!show);
        }

        public void Show(bool canContinue)
        {
            _continueButton.SetActive(canContinue);
            _changeTableButton.SetActive(canContinue);
            LayOut();
            ShowRules(false);
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void Update()
        {
            if (!_canvas.enabled || _logo == null) return;

            // The logo bobs a pixel up and down, like a slow flame.
            float bob = Mathf.Repeat(Time.unscaledTime, 1.6f) < 0.8f ? 0f : 1f;
            _logo.anchoredPosition = new Vector2(_logo.anchoredPosition.x, -30f - bob);
        }
    }
}
