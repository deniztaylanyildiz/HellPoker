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
        private GameObject _cheatsBody;
        private GameObject _sinnersBody;
        private HandRanksPanel _handRanks;

        /// <summary>The pages of How to Play, in the order the page button cycles them.</summary>
        private enum Page { Rules, Hands, Cheats, Sinners }

        private Page _page;
        private Text _pageLabel;
        private Core.Game.IPayoutInfo _payouts;
        private RectTransform _logo;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;
        public event Action ChangeTablePressed;
        public event Action SettingsPressed;
        public event Action RecordsPressed;
        public event Action ChaptersPressed;

        public event Action ChaptersContinuePressed;
        private GameObject _chaptersButton;
        private GameObject _chaptersContinueButton;
        public event Action Confirmed;
        public event Action LanguagePressed;

        public bool IsVisible => _canvas.enabled;

        private const int ConfirmWidth = 272;
        private const int ConfirmHeight = 96;
        private GameObject _confirm;
        private Text _taunt;
        private Text _warning;
        private Text _confirmLabel;

        public bool IsConfirming => _confirm != null && _confirm.activeSelf;

        public bool IsShowingRules => _rulesPanel != null && _rulesPanel.activeSelf;

        /// <summary>The demon's mocking line in the New Game warning (for tests).</summary>
        public string LastTaunt => _taunt.text;

        /// <param name="payouts">What the hands pay at a standard table, for the hand ranking page of the rules.</param>
        /// <param name="cheats">The Cheats page of How to Play (every demon's cheats); null for none.</param>
        /// <param name="tagline">, <paramref name="rules"/>, <paramref name="cheats"/>: read again whenever the language changes.</param>
        public static MainMenuView Create(Transform parent, Func<string> tagline, Func<string> rules, Core.Game.IPayoutInfo payouts,
            Func<string> cheats = null, Func<string> sinners = null)
        {
            Canvas canvas = UiFactory.CreateScreen("MainMenuCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            view._canvas = canvas;
            view._payouts = payouts;
            view.Build(screen, tagline, rules, cheats, sinners);
            return view;
        }

        private void Build(RectTransform screen, Func<string> tagline, Func<string> rules, Func<string> cheats, Func<string> sinners)
        {
            // The bottom of Hell, animated; opaque, so it also blocks clicks from reaching the table underneath.
            // It stays behind the rules panel too.
            MenuBackdrop.Create(screen);

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
                UiFactory.CreateText("TitleText", front, "", 32, Palette.Hell, style: FontStyle.Bold).WithOutline().Localized(() => UiText.Title)
                    .rectTransform.PlaceTL(0, 34, PixelScreen.Width, 32);
            }

            // A demo build says so, small, under the title (the same word in both languages).
            if (HellPoker.Core.Game.ReleaseVersion.IsDemo(Application.version))
                UiFactory.CreateText("DemoLabel", front, UiText.DemoLabel, 8, Palette.Ember, style: FontStyle.Bold).WithOutline()
                    .rectTransform.PlaceTL((PixelScreen.Width - 64) / 2, 72, 64, 9);

            Text taglineText = UiFactory.CreateText("Tagline", front, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline().Localized(tagline);
            taglineText.rectTransform.PlaceTL(40, 88, PixelScreen.Width - 80, 18);
            UiFactory.CreateSprite("Divider", front, UiArt.Divider).rectTransform.PlaceTL((PixelScreen.Width - 48) / 2, 110, 48, 3);
            UiFactory.CreateText("Subtagline", front, "", 8, Palette.BoneMid).WithOutline().Localized(() => UiText.MenuSubtagline)
                .rectTransform.PlaceTL(40, 117, PixelScreen.Width - 80, 9);

            // Two columns in reading order; buttons that are hidden leave no gap (see LayOut).
            Transform buttons = UiFactory.CreateRect("Buttons", front).Stretch();
            _continueButton = CreateMenuButton(buttons, "ContinueButton", () => UiText.Continue, ButtonSkin.Ember, () => ContinuePressed?.Invoke());
            _changeTableButton = CreateMenuButton(buttons, "ChangeTableButton", () => UiText.ChangeTable, ButtonSkin.Ash, () => ChangeTablePressed?.Invoke());
            GameObject newGame = CreateMenuButton(buttons, "NewGameButton", () => UiText.NewGame, ButtonSkin.Blood, () => NewGamePressed?.Invoke());
            GameObject settings = CreateMenuButton(buttons, "SettingsButton", () => UiText.SettingsButton, ButtonSkin.Ash, () => SettingsPressed?.Invoke());
            GameObject howToPlay = CreateMenuButton(buttons, "HowToPlayButton", () => UiText.HowToPlay, ButtonSkin.Ash, () => ShowRules(true));
            GameObject records = CreateMenuButton(buttons, "RecordsButton", () => UiText.Records, ButtonSkin.Ash, () => RecordsPressed?.Invoke());
            // Phase 2's chapters, apart from the demo's run (a test build's door to them).
            _chaptersContinueButton = CreateMenuButton(buttons, "ChaptersContinueButton", () => UiText.ChaptersContinueButton, ButtonSkin.Ember,
                () => ChaptersContinuePressed?.Invoke());
            _chaptersContinueButton.SetActive(false);
            _chaptersButton = CreateMenuButton(buttons, "ChaptersButton", () => UiText.ChaptersButton, ButtonSkin.Ember, () => ChaptersPressed?.Invoke());
            _chaptersButton.SetActive(false);
            _grid.AddRange(new[] { _continueButton, _changeTableButton, newGame, _chaptersContinueButton, _chaptersButton, settings, howToPlay, records });
            _quitButton = CreateMenuButton(buttons, "QuitButton", () => UiText.Quit, ButtonSkin.Ash, () => QuitPressed?.Invoke());
            LayOut();

            // The language, in the top right corner (L does the same): shows the one a press switches to.
            Button language = UiFactory.CreateButton("MenuLanguageButton", front, "", 8, out Text languageLabel, ButtonSkin.Ash);
            languageLabel.Localized(() => UiText.LanguageButton);
            ((RectTransform)language.transform).PlaceTL(PixelScreen.Width - 32, 4, 28, 18);
            language.onClick.AddListener(() => LanguagePressed?.Invoke());

            UiFactory.CreateText("Footer", front, "", 8, Palette.BoneDark).WithOutline().Localized(() => UiText.MenuFooter)
                .rectTransform.PlaceTL(0, 254, PixelScreen.Width, 9);

            // The build's version, small, in the bottom right corner (C.23).
            Text version = UiFactory.CreateText("Version", front, HellPoker.Core.Game.ReleaseVersion.Display(Application.version), 8, Palette.BoneDark,
                TextAnchor.MiddleRight).WithOutline();
            version.rectTransform.PlaceTL(PixelScreen.Width - 84, 258, 80, 9);

            _rulesPanel = BuildRulesPanel(screen, rules, cheats, sinners);
            ShowRules(false);
            BuildConfirm(screen);
        }

        /// <summary>The New Game warning over a run in progress: an invisible layer that swallows clicks, and a box.</summary>
        private void BuildConfirm(RectTransform screen)
        {
            Image shade = UiFactory.CreateImage("ConfirmNewGame", screen, Color.clear);
            shade.rectTransform.Stretch();
            _confirm = shade.gameObject;

            Image box = UiFactory.CreatePanel("ConfirmBox", shade.transform, hot: true);
            box.rectTransform.PlaceTL((PixelScreen.Width - ConfirmWidth) / 2, (PixelScreen.Height - ConfirmHeight) / 2 + 40, ConfirmWidth, ConfirmHeight);

            _taunt = UiFactory.CreateText("Taunt", box.transform, "", 8, Palette.Ember, TextAnchor.UpperCenter).WithOutline();
            _taunt.rectTransform.PlaceTL(8, 8, ConfirmWidth - 16, 26);
            _warning = UiFactory.CreateText("Warning", box.transform, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline();
            _warning.rectTransform.PlaceTL(8, 36, ConfirmWidth - 16, 26);

            Button abandon = UiFactory.CreateButton("MenuConfirmButton", box.transform, "", 8, out _confirmLabel, ButtonSkin.Blood);
            ((RectTransform)abandon.transform).PlaceTL(16, ConfirmHeight - 28, 104, 18);
            abandon.onClick.AddListener(() =>
            {
                _confirm.SetActive(false);
                Confirmed?.Invoke();
            });

            Button back = UiFactory.CreateButton("MenuCancelButton", box.transform, "", 8, out Text backLabel, ButtonSkin.Ash);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL(ConfirmWidth - 16 - 104, ConfirmHeight - 28, 104, 18);
            back.onClick.AddListener(() => _confirm.SetActive(false));

            _confirm.SetActive(false);
        }

        public void AskToConfirm(string taunt, string warning, string confirmLabel)
        {
            _confirmLabel.text = confirmLabel ?? "";
            _taunt.text = taunt ?? "";
            _warning.text = warning ?? "";
            _confirm.SetActive(true);
            _confirm.transform.SetAsLastSibling();
        }

        private GameObject BuildRulesPanel(Transform screen, Func<string> rules, Func<string> cheats, Func<string> sinners)
        {
            Image panel = UiFactory.CreatePanel("RulesPanel", screen);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(8, 8, PixelScreen.Width - 16, PixelScreen.Height - 16);

            UiFactory.CreateText("Title", panel.transform, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.RulesTitle)
                .rectTransform.PlaceTL(0, 8, PixelScreen.Width - 16, 8);
            UiFactory.CreateSprite("Divider", panel.transform, UiArt.Divider).rectTransform.PlaceTL((PixelScreen.Width - 16 - 48) / 2, 19, 48, 3);

            Text body = UiFactory.CreateText("Body", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(rules);
            body.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            body.lineSpacing = 1f;
            _rulesBody = body.gameObject;

            Text cheatsText = UiFactory.CreateText("Cheats", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(cheats ?? (() => ""));
            cheatsText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            cheatsText.lineSpacing = 1f;
            _cheatsBody = cheatsText.gameObject;
            _cheatsBody.SetActive(false);

            Text sinnersText = UiFactory.CreateText("Sinners", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(sinners ?? (() => ""));
            sinnersText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            sinnersText.lineSpacing = 1f;
            _sinnersBody = sinnersText.gameObject;
            _sinnersBody.SetActive(false);

            _handRanks = HandRanksPanel.Create(panel.transform, (PixelScreen.Width - 16 - HandRanksPanel.Width) / 2, 30);

            // RULES → HANDS → CHEATS → SINNERS page switch (the label names the next page), then BACK.
            int buttonsY = PixelScreen.Height - 16 - 26;
            Button page = UiFactory.CreateButton("HandRanksPageButton", panel.transform, "", 8, out _pageLabel, ButtonSkin.Ash);
            ((RectTransform)page.transform).PlaceTL((PixelScreen.Width - 16) / 2 - 108, buttonsY, 104, ButtonHeight);
            page.onClick.AddListener(() => ShowPage(Next(_page)));

            Button back = UiFactory.CreateButton("BackButton", panel.transform, "", 8, out Text rulesBackLabel, ButtonSkin.Blood);
            rulesBackLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 16) / 2 + 4, buttonsY, 80, ButtonHeight);
            back.onClick.AddListener(() => ShowRules(false));
            return panel.gameObject;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, Func<string> label, ButtonSkin skin, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, "", 8, out Text text, skin);
            text.Localized(label);
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
            if (IsConfirming)
            {
                _confirm.SetActive(false);
                return true;
            }
            if (!_rulesPanel.activeSelf) return false;
            if (_page != Page.Rules)
                ShowPage(Page.Rules);   // another page goes back to the rules first
            else
                ShowRules(false);
            return true;
        }

        private Page Next(Page page)
        {
            Page next = page;
            for (int i = 0; i < 4; i++)
            {
                next = next == Page.Rules ? Page.Hands : next == Page.Hands ? Page.Cheats : next == Page.Cheats ? Page.Sinners : Page.Rules;
                if (next == Page.Cheats && !HasText(_cheatsBody)) continue;
                if (next == Page.Sinners && !HasText(_sinnersBody)) continue;
                return next;
            }
            return Page.Rules;
        }

        private static bool HasText(GameObject body) => body != null && !string.IsNullOrEmpty(body.GetComponent<Text>().text);

        private void ShowPage(Page page)
        {
            _page = page;
            if (page == Page.Hands)
                _handRanks.Show(_payouts);
            else
                _handRanks.Hide();
            _rulesBody.SetActive(page == Page.Rules);
            _cheatsBody.SetActive(page == Page.Cheats);
            _sinnersBody.SetActive(page == Page.Sinners);
            Page next = Next(page);
            _pageLabel.text = next == Page.Hands ? UiText.HandsButton : next == Page.Cheats ? UiText.CheatsButton
                : next == Page.Sinners ? UiText.SinnersButton : UiText.RulesButton;
        }

        private void ShowRules(bool show)
        {
            ShowPage(Page.Rules);
            _rulesPanel.SetActive(show);
            _front.SetActive(!show);
        }

        public void SetChapters(bool available, bool inProgress)
        {
            _chaptersButton.SetActive(available);
            _chaptersContinueButton.SetActive(available && inProgress);
            LayOut();
        }

        public void Show(bool canContinue)
        {
            _continueButton.SetActive(canContinue);
            _changeTableButton.SetActive(canContinue);
            LayOut();
            ShowRules(false);
            _confirm.SetActive(false);
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _confirm.SetActive(false);
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void Awake() => Lang.Changed += OnLanguageChanged;

        private void OnDestroy() => Lang.Changed -= OnLanguageChanged;

        /// <summary>The page switch names the next page, and the hands page shows its values: both in words.</summary>
        private void OnLanguageChanged()
        {
            if (_pageLabel != null) ShowPage(_page);
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
