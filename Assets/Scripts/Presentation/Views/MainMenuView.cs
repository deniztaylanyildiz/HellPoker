using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Title screen drawn above the table: New Game / Continue / How to Play / Quit, and a rules panel.
    /// The rules panel is pure view state, so it is handled here rather than in a presenter.
    /// Demo 3.3: the logo drops in once and glints now and then; the buttons sit two to a row in an iron box, a small flame (the cursor)
    /// beside the one the keys point at (the mouse moves it too); the run that waits shows on a card left of the box; BUG / IDEA sits in
    /// the bottom left corner, across from the version. No language button: the language is chosen in Settings ▸ LANGUAGE.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        private const int SortingOrder = 100;

        // Phase 3: the title screen on the 8 px grid, measured from the screen's size.
        private const int Grid = 8;
        private const int ButtonWidth = 128;
        private const int ButtonHeight = 24;
        private const int GridGap = 16;                   // between the two columns
        private const int RowStep = ButtonHeight + Grid;  // a row and the gap under it
        private const int BoxSide = 2 * Grid;             // the box's margin left and right of the buttons (the cursor sits in it)
        private const int BoxTopBottom = Grid;            // and over and under them
        private const int CursorGap = 4;                  // between the cursor and its button
        private const int CornerHeight = 16;
        private const int RulesButtonHeight = 20;
        private const int RulesBodyHeight = PixelScreen.Height - 76;   // the rules' text down to the pager
        private const int CardWidth = 152;
        private const int CardHeight = 80;
        private static int W => PixelScreen.Width;
        private static int H => PixelScreen.Height;

        /// <summary>Down to the 8 px grid.</summary>
        private static int Snap(int v) => v / Grid * Grid;

        /// <summary>The logo's whole-number scale: the largest that keeps it within four fifths of the screen's width.</summary>
        private static int LogoScale(int width) => Mathf.Max(1, W * 4 / 5 / Mathf.Max(1, width));

        private const int LogoTop = 3 * Grid;
        private int _logoBottom;
        // Under the logo, each on the grid: DEMO, the two lines of the tagline, the divider, the subtagline, the box of buttons.
        private int DemoTop => Snap(_logoBottom + Grid - 1);
        private int TaglineTop => DemoTop + 2 * Grid;
        private int DividerTop => TaglineTop + 20;
        private int SubtaglineTop => DividerTop + Grid;
        private int GridTop => Snap(SubtaglineTop + 3 * Grid - 1);
        private static int GridLeft => Snap((W - 2 * ButtonWidth - GridGap) / 2);

        private Canvas _canvas;

        private GameObject _continueButton;
        private GameObject _changeTableButton;
        private readonly List<GameObject> _grid = new List<GameObject>();
        private GameObject _quitButton;
        private GameObject _front;
        private GameObject _rulesPanel;
        private GameObject _rulesBody;
        private GameObject _cheatsBody;
        private GameObject _sinnersBody;
        private GameObject _chapterBody;
        private GameObject _gameBody;
        private GameObject _simulatorLabel;
        private GameObject _demoLabel;
        private Text _tagline;
        private Text _subtagline;
        private RectTransform _box;

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

        /// <summary>BUG / IDEA in the bottom left corner (the title menu is also the game's pause menu): the report box.</summary>
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

        // ---------------------------------------------------------------- the cursor

        private GameObject _selected;
        private Image _cursor;
        private Sprite[] _cursorFrames;
        private const float CursorFps = 6f;

        /// <summary>The button the keys point at (its name), null when none (for tests).</summary>
        public string SelectedButton => _selected != null ? _selected.name : null;

        /// <summary>The cursor's place on the 640×360 screen (top left), and whether it shows (for tests).</summary>
        public Vector2Int CursorAt => new Vector2Int((int)_cursor.rectTransform.anchoredPosition.x, -(int)_cursor.rectTransform.anchoredPosition.y);
        public bool CursorShown => _cursor.gameObject.activeInHierarchy && _canvas.enabled;

        // ---------------------------------------------------------------- the run's card

        private GameObject _card;
        private Image _cardPortrait;
        private Text _cardName;
        private Text _cardLines;
        private ContinueCard _cardData;

        /// <summary>True while the card of the run that waits shows beside CONTINUE (for tests).</summary>
        public bool ContinueCardShown => _card.activeInHierarchy && _canvas.enabled;

        /// <summary>The card's words: the class's name, then its lines (for tests).</summary>
        public string ContinueCardText => _cardName.text + "\n" + _cardLines.text;

        // ---------------------------------------------------------------- the logo's intro and glint

        /// <summary>The logo drops in only the first time the menu shows in a session.</summary>
        private static bool _introPlayed;
        private const float IntroSeconds = 0.7f;
        private float _introStart = -1f;
        private int _logoHeight;

        /// <summary>True while the logo drops in (for tests).</summary>
        public bool IntroPlaying => _introStart >= 0f;

        private Image _glint;
        private Sprite[] _glintFrames;
        private float _nextGlint;
        private float _glintStart = -1f;
        private const float GlintFps = 12f;
        private readonly System.Random _glintDice = new System.Random(7);
        // Where a glint may catch the logo (the letters' upper strokes, in the logo's own pixels).
        private static readonly Vector2Int[] GlintSpots =
        {
            new Vector2Int(8, 14), new Vector2Int(48, 13), new Vector2Int(90, 14), new Vector2Int(128, 13), new Vector2Int(210, 14),
            new Vector2Int(250, 13), new Vector2Int(290, 14), new Vector2Int(330, 13), new Vector2Int(372, 14)
        };

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

            // The logo at a whole-number scale (square pixels); the title screen's own (Ui/menu_logo), else the table's title.
            Sprite logoArt = UiArt.Sprite(UiArt.MenuLogo) ?? UiArt.Sprite(UiArt.Title);
            Image logo = UiFactory.CreateImage("Title", front, logoArt != null ? Color.white : Color.clear);
            logo.sprite = logoArt;
            logo.raycastTarget = false;
            _logo = logo.rectTransform;
            if (logoArt != null)
            {
                int scale = LogoScale((int)logoArt.rect.width);
                Vector2Int size = new Vector2Int((int)logoArt.rect.width * scale, (int)logoArt.rect.height * scale);
                _logo.PlaceTL((W - size.x) / 2, LogoTop, size.x, size.y);
                _logoHeight = size.y;
                _logoBottom = LogoTop + size.y;

                // A glint runs over the letters now and then.
                _glintFrames = UiArt.Strip(UiArt.MenuGlint, UiArt.MenuGlintSize);
                _glint = UiFactory.CreateImage("LogoGlint", _logo, Color.white);
                _glint.raycastTarget = false;
                _glint.rectTransform.PlaceTL(0, 0, UiArt.MenuGlintSize * scale, UiArt.MenuGlintSize * scale);
                _glint.gameObject.SetActive(false);
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
            {
                Text demo = UiFactory.CreateText("DemoLabel", front, UiText.DemoLabel, 8, Palette.Ember, style: FontStyle.Bold).WithOutline();
                demo.rectTransform.PlaceTL((W - 64) / 2, DemoTop, 64, Grid);
                _demoLabel = demo.gameObject;
            }

            // The Boss Simulator says so in DEMO's place, under the title (F9 again: back to the game).
            Text simulator = UiFactory.CreateText("SimulatorLabel", front, "", 8, Palette.Ember, style: FontStyle.Bold).WithOutline()
                .Localized(() => UiText.SimulatorLabel);
            simulator.rectTransform.PlaceTL(0, DemoTop, W, 9);
            _simulatorLabel = simulator.gameObject;
            _simulatorLabel.SetActive(false);

            // The game's words — or, in the Boss Simulator, the old demo's (its sentence of years).
            int textWidth = W - 10 * Grid;
            _tagline = UiFactory.CreateText("Tagline", front, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline()
                .Localized(() => _simulator ? tagline() : UiText.GameTagline);
            _tagline.rectTransform.PlaceTL((W - textWidth) / 2, TaglineTop, textWidth, 18);
            UiFactory.CreateSprite("Divider", front, UiArt.Divider).rectTransform.PlaceTL((W - 48) / 2, DividerTop, 48, 3);
            _subtagline = UiFactory.CreateText("Subtagline", front, "", 8, Palette.BoneMid).WithOutline()
                .Localized(() => _simulator ? UiText.MenuSubtagline : UiText.GameSubtagline);
            _subtagline.rectTransform.PlaceTL((W - textWidth) / 2, SubtaglineTop, textWidth, 9);

            // The menu's iron box (riveted corners, a dark dither inside), the buttons two to a row in it.
            Image box = UiFactory.CreateSprite("MenuBox", front, UiArt.MenuBox, Palette.Night);
            if (box.sprite != null)
            {
                box.type = Image.Type.Tiled;
                box.pixelsPerUnitMultiplier = 1f;
            }
            _box = box.rectTransform;

            Transform buttons = UiFactory.CreateRect("Buttons", front).Stretch();
            _continueButton = CreateMenuButton(buttons, "ContinueButton", () => UiText.Continue, ButtonSkin.Ember, () => ContinuePressed?.Invoke());
            _changeTableButton = CreateMenuButton(buttons, "ChangeTableButton", () => UiText.ChangeTable, ButtonSkin.Ash, () => ChangeTablePressed?.Invoke());
            GameObject newGame = CreateMenuButton(buttons, "NewGameButton", () => UiText.NewGame, ButtonSkin.Blood, () => NewGamePressed?.Invoke());
            GameObject settings = CreateMenuButton(buttons, "SettingsButton", () => UiText.SettingsButton, ButtonSkin.Ash, () => SettingsPressed?.Invoke());
            GameObject howToPlay = CreateMenuButton(buttons, "HowToPlayButton", () => UiText.HowToPlay, ButtonSkin.Ash, () => ShowRules(true));
            GameObject records = CreateMenuButton(buttons, "RecordsButton", () => UiText.Records, ButtonSkin.Ash, () => RecordsPressed?.Invoke());
            _grid.AddRange(new[] { _continueButton, _changeTableButton, newGame, settings, howToPlay, records });
            _quitButton = CreateMenuButton(buttons, "QuitButton", () => UiText.Quit, ButtonSkin.Ash, () => QuitPressed?.Invoke());

            // The cursor: a small flame left of the button the keys point at.
            _cursorFrames = UiArt.Strip(UiArt.MenuCursor, UiArt.MenuCursorSize);
            _cursor = UiFactory.CreateImage("MenuCursor", buttons, _cursorFrames != null ? Color.white : Palette.Ember);
            _cursor.raycastTarget = false;
            if (_cursorFrames != null && _cursorFrames.Length > 0) _cursor.sprite = _cursorFrames[0];
            _cursor.rectTransform.PlaceTL(0, 0, UiArt.MenuCursorSize, UiArt.MenuCursorSize);
            LayOut();

            BuildCard(front);

            // BUG / IDEA in the bottom left corner, across from the version.
            Button feedback = UiFactory.CreateButton("MenuFeedbackButton", front, "", 8, out Text feedbackLabel, ButtonSkin.Ash);
            feedbackLabel.Localized(() => UiText.FeedbackButton);
            ((RectTransform)feedback.transform).PlaceTL(Grid, H - 3 * Grid, 13 * Grid, CornerHeight);
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

        /// <summary>The card of the run that waits, left of the box: the class's portrait and name, where the run stands, what it carries.</summary>
        private void BuildCard(Transform front)
        {
            Image card = UiFactory.CreatePanel("ContinueCard", front);
            card.rectTransform.PlaceTL(Grid, GridTop - BoxTopBottom, CardWidth, CardHeight);
            _card = card.gameObject;

            // The portrait in its own small frame (its dark backdrop reads as a picture, not a hole in the card).
            int portraitY = (CardHeight - UiArt.SinnerPortraitSize) / 2;
            UiFactory.CreatePanel("PortraitFrame", card.transform).rectTransform
                .PlaceTL(4, portraitY - 2, UiArt.SinnerPortraitSize + 4, UiArt.SinnerPortraitSize + 4);
            _cardPortrait = UiFactory.CreateImage("Portrait", card.transform, Color.white);
            _cardPortrait.raycastTarget = false;
            _cardPortrait.rectTransform.PlaceTL(6, portraitY, UiArt.SinnerPortraitSize, UiArt.SinnerPortraitSize);

            int textX = 6 + UiArt.SinnerPortraitSize + 6;
            _cardName = UiFactory.CreateText("Name", card.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithOutline();
            _cardName.rectTransform.PlaceTL(textX, 7, CardWidth - textX - 4, 9);
            _cardLines = UiFactory.CreateText("Lines", card.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).WithOutline();
            _cardLines.rectTransform.PlaceTL(textX, 19, CardWidth - textX - 4, CardHeight - 23);
            _cardLines.lineSpacing = 1.1f;
            _card.SetActive(false);
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
            body.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, RulesBodyHeight);
            body.lineSpacing = 1f;
            _rulesBody = body.gameObject;

            Text cheatsText = UiFactory.CreateText("Cheats", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(cheats ?? (() => ""));
            cheatsText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, RulesBodyHeight);
            cheatsText.lineSpacing = 1f;
            _cheatsBody = cheatsText.gameObject;
            _cheatsBody.SetActive(false);

            Text sinnersText = UiFactory.CreateText("Sinners", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(sinners ?? (() => ""));
            sinnersText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, RulesBodyHeight);
            sinnersText.lineSpacing = 1f;
            _sinnersBody = sinnersText.gameObject;
            _sinnersBody.SetActive(false);

            // Phase 2: the classes' chapter powers and the demons' own rules.
            Text chapterText = UiFactory.CreateText("ChapterRules", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft)
                .Localized(UiText.ChapterRulesPage);
            chapterText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, RulesBodyHeight);
            chapterText.lineSpacing = 1f;
            _chapterBody = chapterText.gameObject;
            _chapterBody.SetActive(false);

            // The game's own first page: the chapters, the map, the purse, the bar, Lucifer.
            Text gameText = UiFactory.CreateText("GameRules", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).Localized(UiText.GameRulesPage);
            gameText.rectTransform.PlaceTL(10, 28, PixelScreen.Width - 36, RulesBodyHeight);
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

        private GameObject CreateMenuButton(Transform parent, string name, Func<string> label, ButtonSkin skin, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, "", 8, out Text text, skin);
            text.Localized(label);
            ((RectTransform)button.transform).PlaceTL(0, 0, ButtonWidth, ButtonHeight);
            button.onClick.AddListener(() => onClick());
            ButtonFeel feel = button.GetComponent<ButtonFeel>();
            if (feel != null)
            {
                // Lit (the mouse or the cursor on it): the ember skin, the label a pixel up; the mouse brings the cursor along.
                feel.LightUp(UiArt.Button(ButtonSkin.Ember));
                feel.Entered += () => Select(button.gameObject, sound: true);
            }
            return button.gameObject;
        }

        /// <summary>
        /// Places the visible buttons two to a row, in order, every one the same size on the 8 px grid; Quit takes the last row's empty
        /// place, or is centred on a row of its own. The box closes round them.
        /// </summary>
        private void LayOut()
        {
            int left = GridLeft;
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
            int rows = slot / 2 + 1;
            _box.PlaceTL(left - BoxSide, GridTop - BoxTopBottom, 2 * ButtonWidth + GridGap + 2 * BoxSide, rows * RowStep - Grid + 2 * BoxTopBottom);
            if (_selected == null || !_selected.activeSelf) Select(FirstButton(), sound: false);
            else PlaceCursor();
        }

        private static void Place(GameObject button, int x, int y) => ((RectTransform)button.transform).PlaceTL(x, y, ButtonWidth, ButtonHeight);

        private static Vector2Int TopLeftOf(GameObject button)
        {
            Vector2 at = ((RectTransform)button.transform).anchoredPosition;
            return new Vector2Int(Mathf.RoundToInt(at.x), -Mathf.RoundToInt(at.y));
        }

        /// <summary>The visible buttons in reading order (the grid's, then Quit).</summary>
        private IEnumerable<GameObject> Buttons()
        {
            foreach (GameObject button in _grid)
                if (button.activeSelf) yield return button;
            yield return _quitButton;
        }

        private GameObject FirstButton()
        {
            foreach (GameObject button in Buttons()) return button;
            return _quitButton;
        }

        private void Select(GameObject button, bool sound)
        {
            if (button == null) return;
            bool changed = button != _selected;
            if (_selected != null) _selected.GetComponent<ButtonFeel>().Selected = false;
            _selected = button;
            ButtonFeel feel = button.GetComponent<ButtonFeel>();
            if (feel != null) feel.Selected = true;
            PlaceCursor();
            if (changed && sound) UiFactory.PlayClick();
        }

        private void PlaceCursor()
        {
            if (_selected == null) return;
            Vector2Int at = TopLeftOf(_selected);
            _cursor.rectTransform.PlaceTL(at.x - CursorGap - UiArt.MenuCursorSize, at.y + (ButtonHeight - UiArt.MenuCursorSize) / 2,
                UiArt.MenuCursorSize, UiArt.MenuCursorSize);
        }

        /// <summary>
        /// The arrows: the nearest button that way. Up / down go along the column (the nearest button across when the next row has
        /// none there) and round from the bottom to the top; left / right stay on the row.
        /// </summary>
        public void MoveSelection(int dx, int dy)
        {
            if (!_canvas.enabled || !_front.activeSelf || IsConfirming) return;
            if (_selected == null || !_selected.activeSelf)
            {
                Select(FirstButton(), sound: true);
                return;
            }
            Vector2Int from = TopLeftOf(_selected);
            GameObject best = null;
            int bestScore = int.MaxValue;
            foreach (GameObject button in Buttons())
            {
                if (button == _selected) continue;
                Vector2Int at = TopLeftOf(button);
                int along = dy != 0 ? (at.y - from.y) * dy : (at.x - from.x) * dx;
                int across = dy != 0 ? Mathf.Abs(at.x - from.x) : Mathf.Abs(at.y - from.y);
                if (along <= 0 || dx != 0 && across != 0) continue;
                int score = along * 4 + across;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = button;
                }
            }
            if (best == null && dy != 0)
            {
                // Round: from the last row to the first (or back), the nearest button across.
                foreach (GameObject button in Buttons())
                {
                    if (button == _selected) continue;
                    Vector2Int at = TopLeftOf(button);
                    int score = (dy > 0 ? at.y : -at.y) * 4 + Mathf.Abs(at.x - from.x);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = button;
                    }
                }
            }
            if (best != null) Select(best, sound: true);
        }

        /// <summary>Enter / Space: the button under the cursor (as a click: the same sound, the same action).</summary>
        public void PressSelection()
        {
            FinishIntro();
            if (!_canvas.enabled || !_front.activeSelf || IsConfirming || _selected == null || !_selected.activeSelf) return;
            _selected.GetComponent<Button>().onClick.Invoke();
        }

        public void SetContinueCard(ContinueCard card)
        {
            _cardData = card;
            RenderCard();
        }

        private void RenderCard()
        {
            ContinueCard card = _cardData;
            bool show = card != null && !_simulator && _continueButton.activeSelf;
            _card.SetActive(show);
            if (!show) return;
            Sprite portrait = UiArt.SinnerPortrait(card.ClassId) ?? UiArt.SinnerIcon(card.ClassId);
            _cardPortrait.sprite = portrait;
            _cardPortrait.color = portrait != null ? Color.white : Color.clear;
            _cardName.text = UiText.SinnerName(card.ClassId);
            _cardLines.text = UiText.ContinueCardLines(card.Chapter, card.Floor, card.Coins, card.Sentence, card.Circle);
        }

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
            if (_demoLabel != null) _demoLabel.SetActive(!simulator);
            _tagline.GetComponent<LocalizedText>()?.Rewrite();
            _subtagline.GetComponent<LocalizedText>()?.Rewrite();
            _changeTableButton.SetActive(simulator && _continueButton.activeSelf);
            LayOut();
            RenderCard();
            if (_rulesPanel != null) ShowPage(MainPage);
        }

        public void Show(bool canContinue)
        {
            _continueButton.SetActive(canContinue);
            _changeTableButton.SetActive(canContinue && _simulator);
            _selected = null;
            foreach (GameObject button in Buttons()) button.GetComponent<ButtonFeel>().Selected = false;
            LayOut();
            RenderCard();
            ShowRules(false);
            _confirm.SetActive(false);
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            StartIntro();
        }

        public void Hide()
        {
            _confirm.SetActive(false);
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
            FinishIntro();
        }

        private void Awake() => Lang.Changed += OnLanguageChanged;

        private void OnDestroy() => Lang.Changed -= OnLanguageChanged;

        /// <summary>The page switch names the next page, the hands page shows its values, the run's card its words: all in words.</summary>
        private void OnLanguageChanged()
        {
            if (_pageLabel != null) ShowPage(_page);
            if (_card != null) RenderCard();
        }

        // ---------------------------------------------------------------- motion

        /// <summary>The first time the menu shows (not in batchmode: the tests and the tours see it at rest), the logo drops in.</summary>
        private void StartIntro()
        {
            if (_introPlayed || _logoHeight == 0) return;
            _introPlayed = true;
            if (Application.isBatchMode) return;
            _introStart = Time.unscaledTime;
            PlaceLogo(-_logoHeight - LogoTop);
        }

        private void FinishIntro()
        {
            if (_introStart < 0f) return;
            _introStart = -1f;
            PlaceLogo(0);
        }

        /// <summary>The logo <paramref name="offset"/> whole pixels above its place.</summary>
        private void PlaceLogo(int offset) => _logo.anchoredPosition = new Vector2(_logo.anchoredPosition.x, -(LogoTop + offset));

        private void Update()
        {
            if (!_canvas.enabled) return;
            float now = Time.unscaledTime;

            if (_cursorFrames != null && _cursorFrames.Length > 0)
            {
                _cursor.gameObject.SetActive(_selected != null && _selected.activeSelf);
                _cursor.sprite = _cursorFrames[(int)(now * CursorFps) % _cursorFrames.Length];
            }

            if (_introStart >= 0f)
            {
                // Any key or click lets it land at once.
                bool skip = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame
                            || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                float t = (now - _introStart) / IntroSeconds;
                if (skip || t >= 1f)
                {
                    FinishIntro();
                    _nextGlint = now + 0.4f;
                }
                else
                {
                    // Falls, eases in, and lands in whole pixels (a 2 px bounce at the end).
                    float fall = 1f - (1f - t) * (1f - t);
                    int offset = Mathf.RoundToInt((1f - fall) * (_logoHeight + LogoTop));
                    if (t > 0.85f) offset = -Mathf.RoundToInt(2f * Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI));
                    PlaceLogo(offset);
                }
                return;
            }

            if (_glint == null || _glintFrames == null || _glintFrames.Length == 0) return;
            if (_glintStart < 0f)
            {
                if (now < _nextGlint) return;
                Vector2Int spot = GlintSpots[_glintDice.Next(GlintSpots.Length)];
                int scale = Mathf.Max(1, Mathf.RoundToInt(_glint.rectTransform.sizeDelta.x / UiArt.MenuGlintSize));
                _glint.rectTransform.anchoredPosition = new Vector2((spot.x - UiArt.MenuGlintSize / 2) * scale, -(spot.y - UiArt.MenuGlintSize / 2) * scale);
                _glint.gameObject.SetActive(true);
                _glintStart = now;
            }
            int frame = (int)((now - _glintStart) * GlintFps);
            if (frame >= _glintFrames.Length)
            {
                _glint.gameObject.SetActive(false);
                _glintStart = -1f;
                _nextGlint = now + 1.5f + (float)_glintDice.NextDouble() * 2.5f;
                return;
            }
            _glint.sprite = _glintFrames[frame];
        }
    }
}
