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

        // Phase 3: the title screen on the 8 px grid, measured from the screen's size (the same code lays out 480×270 and 640×360).
        private const int Grid = 8;
        private const int ButtonWidth = 128;
        private const int ButtonHeight = 24;
        private const int GridGap = 16;                   // between the two columns
        private const int RowStep = ButtonHeight + Grid;  // a row and the gap under it
        private const int CornerHeight = 16;
        private const int RulesButtonHeight = 20;          // How to Play's pager (its pages are laid out round it)              // BUG / IDEA and the language, in the top corners
        private static int W => PixelScreen.Width;
        private static int H => PixelScreen.Height;

        /// <summary>Down to the 8 px grid.</summary>
        private static int Snap(int v) => v / Grid * Grid;

        /// <summary>The logo's whole-number scale: the largest that keeps it within four fifths of the screen's width.</summary>
        private static int LogoScale(int width) => Mathf.Max(1, W * 4 / 5 / Mathf.Max(1, width));

        private static int LogoTop => Snap(H / 10);
        private int _logoBottom;
        // Under the logo, each on the grid: DEMO, the two lines of the tagline, the divider, the subtagline, the buttons.
        private int DemoTop => Snap(_logoBottom + Grid - 1);
        private int TaglineTop => DemoTop + 2 * Grid;
        private int DividerTop => TaglineTop + 20;
        private int SubtaglineTop => DividerTop + Grid;
        private int GridTop => Snap(SubtaglineTop + 3 * Grid - 1);

        private Canvas _canvas;

        private GameObject _continueButton;
        private GameObject _changeTableButton;
        private readonly System.Collections.Generic.List<GameObject> _grid = new System.Collections.Generic.List<GameObject>();
        private GameObject _quitButton;
        private GameObject _front;
        private GameObject _rulesPanel;
        private GameObject _rulesBody;
        private GameObject _cheatsBody;
        private GameObject _sinnersBody;
        private GameObject _chapterBody;
        private GameObject _gameBody;
        private GameObject _simulatorLabel;
        private Text _tagline;
        private Text _subtagline;

        /// <summary>The menu leads to the Boss Simulator (the old demo) instead of the game.</summary>
        private bool _simulator;

        /// <summary>True while the menu leads to the Boss Simulator (for tests).</summary>
        public bool InSimulator => _simulator;
        private HandRanksPanel _handRanks;

        /// <summary>The pages of How to Play, in the order the page button cycles them.</summary>
        private enum Page { Game, Powers, Rules, Hands, Cheats, Sinners }

        /// <summary>The first page of How to Play: the game's, or the simulator's rules.</summary>
        private Page MainPage => _simulator ? Page.Rules : Page.Game;

        private Page _page;
        private Text _pageLabel;
        private GameObject _pageButton;
        private HowToPlayView _howTo;

        /// <summary>The game's paged How to Play (for tests and screenshots).</summary>
        public HowToPlayView HowTo => _howTo;
        private Core.Game.IPayoutInfo _payouts;
        private RectTransform _logo;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;
        public event Action ChangeTablePressed;
        public event Action SettingsPressed;
        public event Action RecordsPressed;
        public event Action Confirmed;
        public event Action LanguagePressed;

        /// <summary>BUG / IDEA in the top left corner (the title menu is also the game's pause menu): the report box.</summary>
        public event Action FeedbackPressed;

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
                int scale = LogoScale((int)logo.sprite.rect.width);
                Vector2Int size = new Vector2Int((int)logo.sprite.rect.width * scale, (int)logo.sprite.rect.height * scale);
                _logo.PlaceTL((W - size.x) / 2, LogoTop, size.x, size.y);
                _logoBottom = LogoTop + size.y;
            }
            else
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", front, "", 32, Palette.Hell, style: FontStyle.Bold).WithOutline().Localized(() => UiText.Title)
                    .rectTransform.PlaceTL(0, LogoTop, W, 32);
                _logoBottom = LogoTop + 32;
            }

            // A demo build says so, small, under the title (the same word in both languages).
            if (HellPoker.Core.Game.ReleaseVersion.IsDemo(Application.version))
                UiFactory.CreateText("DemoLabel", front, UiText.DemoLabel, 8, Palette.Ember, style: FontStyle.Bold).WithOutline()
                    .rectTransform.PlaceTL((W - 64) / 2, DemoTop, 64, Grid);

            // The game's words — or, in the Boss Simulator, the old demo's (its sentence of years).
            int textWidth = W - 10 * Grid;
            _tagline = UiFactory.CreateText("Tagline", front, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline()
                .Localized(() => _simulator ? tagline() : UiText.GameTagline);
            _tagline.rectTransform.PlaceTL((W - textWidth) / 2, TaglineTop, textWidth, 18);
            UiFactory.CreateSprite("Divider", front, UiArt.Divider).rectTransform.PlaceTL((W - 48) / 2, DividerTop, 48, 3);
            _subtagline = UiFactory.CreateText("Subtagline", front, "", 8, Palette.BoneMid).WithOutline()
                .Localized(() => _simulator ? UiText.MenuSubtagline : UiText.GameSubtagline);
            _subtagline.rectTransform.PlaceTL((W - textWidth) / 2, SubtaglineTop, textWidth, 9);

            // The Boss Simulator says so under the title (F9 again: back to the game).
            Text simulator = UiFactory.CreateText("SimulatorLabel", front, "", 8, Palette.Ember, style: FontStyle.Bold).WithOutline()
                .Localized(() => UiText.SimulatorLabel);
            simulator.rectTransform.PlaceTL(0, Grid + 4, W, 9);
            _simulatorLabel = simulator.gameObject;
            _simulatorLabel.SetActive(false);

            // Two columns in reading order; buttons that are hidden leave no gap (see LayOut).
            Transform buttons = UiFactory.CreateRect("Buttons", front).Stretch();
            _continueButton = CreateMenuButton(buttons, "ContinueButton", () => UiText.Continue, ButtonSkin.Ember, () => ContinuePressed?.Invoke());
            _changeTableButton = CreateMenuButton(buttons, "ChangeTableButton", () => UiText.ChangeTable, ButtonSkin.Ash, () => ChangeTablePressed?.Invoke());
            GameObject newGame = CreateMenuButton(buttons, "NewGameButton", () => UiText.NewGame, ButtonSkin.Blood, () => NewGamePressed?.Invoke());
            GameObject settings = CreateMenuButton(buttons, "SettingsButton", () => UiText.SettingsButton, ButtonSkin.Ash, () => SettingsPressed?.Invoke());
            GameObject howToPlay = CreateMenuButton(buttons, "HowToPlayButton", () => UiText.HowToPlay, ButtonSkin.Ash, () => ShowRules(true));
            GameObject records = CreateMenuButton(buttons, "RecordsButton", () => UiText.Records, ButtonSkin.Ash, () => RecordsPressed?.Invoke());
            _grid.AddRange(new[] { _continueButton, _changeTableButton, newGame, settings, howToPlay, records });
            _quitButton = CreateMenuButton(buttons, "QuitButton", () => UiText.Quit, ButtonSkin.Ash, () => QuitPressed?.Invoke());
            LayOut();

            // The language, in the top right corner (L does the same): shows the one a press switches to.
            Button language = UiFactory.CreateButton("MenuLanguageButton", front, "", 8, out Text languageLabel, ButtonSkin.Ash);
            languageLabel.Localized(() => UiText.LanguageButton);
            ((RectTransform)language.transform).PlaceTL(W - Grid - 4 * Grid, Grid, 4 * Grid, CornerHeight);
            language.onClick.AddListener(() => LanguagePressed?.Invoke());

            Button feedback = UiFactory.CreateButton("MenuFeedbackButton", front, "", 8, out Text feedbackLabel, ButtonSkin.Ash);
            feedbackLabel.Localized(() => UiText.FeedbackButton);
            ((RectTransform)feedback.transform).PlaceTL(Grid, Grid, 13 * Grid, CornerHeight);
            feedback.onClick.AddListener(() => FeedbackPressed?.Invoke());

            UiFactory.CreateText("Footer", front, "", 8, Palette.BoneDark).WithOutline().Localized(() => UiText.MenuFooter)
                .rectTransform.PlaceTL(0, H - 2 * Grid, W, 9);

            // The build's version, small, in the bottom right corner (C.23).
            Text version = UiFactory.CreateText("Version", front, HellPoker.Core.Game.ReleaseVersion.Display(Application.version), 8, Palette.BoneDark,
                TextAnchor.MiddleRight).WithOutline();
            version.rectTransform.PlaceTL(W - Grid - 10 * Grid, H - 2 * Grid, 10 * Grid, 9);

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

            // Phase 2: the classes' chapter powers and the demons' own rules.
            Text chapterText = UiFactory.CreateText("ChapterRules", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft)
                .Localized(UiText.ChapterRulesPage);
            chapterText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            chapterText.lineSpacing = 1f;
            _chapterBody = chapterText.gameObject;
            _chapterBody.SetActive(false);

            // The game's own first page: the chapters, the map, the purse, the bar, Lucifer.
            Text gameText = UiFactory.CreateText("GameRules", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(UiText.GameRulesPage);
            gameText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, 196);
            gameText.lineSpacing = 1f;
            _gameBody = gameText.gameObject;
            _gameBody.SetActive(false);

            _handRanks = HandRanksPanel.Create(panel.transform, (PixelScreen.Width - 16 - HandRanksPanel.Width) / 2, 30);

            // RULES → HANDS → CHEATS → SINNERS page switch (the label names the next page), then BACK.
            int buttonsY = PixelScreen.Height - 16 - 26;
            Button page = UiFactory.CreateButton("HandRanksPageButton", panel.transform, "", 8, out _pageLabel, ButtonSkin.Ash);
            ((RectTransform)page.transform).PlaceTL((PixelScreen.Width - 16) / 2 - 108, buttonsY, 104, RulesButtonHeight);
            page.onClick.AddListener(() => ShowPage(Next(_page)));
            _pageButton = page.gameObject;

            // Demo 2.2: the game's own How to Play — ten short pages with pictures (the simulator keeps the pages above).
            _howTo = HowToPlayView.Create(panel.transform, PixelScreen.Width - 16, _handRanks, _payouts, buttonsY);
            _howTo.Hide();

            Button back = UiFactory.CreateButton("BackButton", panel.transform, "", 8, out Text rulesBackLabel, ButtonSkin.Blood);
            rulesBackLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 16) / 2 + 4, buttonsY, 80, RulesButtonHeight);
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

        /// <summary>
        /// Places the visible buttons two to a row, in order, every one the same size on the 8 px grid; Quit takes the last row's empty
        /// place, or is centred on a row of its own.
        /// </summary>
        private void LayOut()
        {
            int left = Snap((W - 2 * ButtonWidth - GridGap) / 2);
            int slot = 0;
            foreach (GameObject button in _grid)
            {
                if (!button.activeSelf) continue;
                Place(button, left + slot % 2 * (ButtonWidth + GridGap), GridTop + slot / 2 * RowStep);
                slot++;
            }
            if (slot % 2 == 1)
                Place(_quitButton, left + ButtonWidth + GridGap, GridTop + slot / 2 * RowStep);
            else
                Place(_quitButton, (W - ButtonWidth) / 2, GridTop + slot / 2 * RowStep);
        }

        private static void Place(GameObject button, int x, int y) => ((RectTransform)button.transform).PlaceTL(x, y, ButtonWidth, ButtonHeight);

        public bool CloseOverlay()
        {
            if (IsConfirming)
            {
                _confirm.SetActive(false);
                return true;
            }
            if (!_rulesPanel.activeSelf) return false;
            if (!_simulator && _howTo.Page != 0)
                _howTo.Show(0);   // another page goes back to the first one first
            else if (_simulator && _page != MainPage)
                ShowPage(MainPage);
            else
                ShowRules(false);
            return true;
        }

        /// <summary>The arrows / Q-E on How to Play: the game's pages back and forth; the simulator's pages round (either way).</summary>
        public void TurnRulesPage(int step)
        {
            if (!IsShowingRules || step == 0) return;
            if (_simulator) ShowPage(Next(_page));
            else _howTo.Turn(step);
        }

        private Page Next(Page page)
        {
            // The game: GAME → POWERS → HANDS → CHEATS. The Boss Simulator: RULES → HANDS → CHEATS → SINNERS.
            Page[] order = _simulator ? new[] { Page.Rules, Page.Hands, Page.Cheats, Page.Sinners } : new[] { Page.Game, Page.Powers, Page.Hands, Page.Cheats };
            int at = Array.IndexOf(order, page);
            for (int i = 1; i <= order.Length; i++)
            {
                Page next = order[(at + i + order.Length) % order.Length];
                if (next == Page.Cheats && !HasText(_cheatsBody)) continue;
                if (next == Page.Sinners && !HasText(_sinnersBody)) continue;
                return next;
            }
            return MainPage;
        }

        private static bool HasText(GameObject body) => body != null && !string.IsNullOrEmpty(body.GetComponent<Text>().text);

        private void ShowPage(Page page)
        {
            _page = page;
            _pageButton.SetActive(_simulator);
            if (!_simulator)
            {
                // The game: the paged How to Play in place of the long pages.
                _rulesBody.SetActive(false);
                _cheatsBody.SetActive(false);
                _sinnersBody.SetActive(false);
                _chapterBody.SetActive(false);
                _gameBody.SetActive(false);
                _howTo.Show(_howTo.gameObject.activeSelf ? _howTo.Page : 0);
                return;
            }
            _howTo.Hide();
            if (page == Page.Hands)
                _handRanks.Show(_payouts);
            else
                _handRanks.Hide();
            _rulesBody.SetActive(page == Page.Rules);
            _cheatsBody.SetActive(page == Page.Cheats);
            _sinnersBody.SetActive(page == Page.Sinners);
            _chapterBody.SetActive(page == Page.Powers);
            _gameBody.SetActive(page == Page.Game);
            Page next = Next(page);
            _pageLabel.text = next == Page.Hands ? UiText.HandsButton : next == Page.Cheats ? UiText.CheatsButton
                : next == Page.Sinners ? UiText.SinnersButton : next == Page.Powers ? UiText.ChapterRulesButton
                : next == Page.Game ? UiText.GameRulesButton : UiText.RulesButton;
        }

        private void ShowRules(bool show)
        {
            _howTo?.Hide();   // How to Play opens on its first page
            ShowPage(MainPage);
            _rulesPanel.SetActive(show);
            _front.SetActive(!show);
        }

        public void SetSimulator(bool simulator)
        {
            _simulator = simulator;
            _simulatorLabel.SetActive(simulator);
            _tagline.GetComponent<LocalizedText>()?.Rewrite();
            _subtagline.GetComponent<LocalizedText>()?.Rewrite();
            _changeTableButton.SetActive(simulator && _continueButton.activeSelf);
            LayOut();
            if (_rulesPanel != null) ShowPage(MainPage);
        }

        public void Show(bool canContinue)
        {
            _continueButton.SetActive(canContinue);
            _changeTableButton.SetActive(canContinue && _simulator);
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
