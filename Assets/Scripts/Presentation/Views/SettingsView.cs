using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The settings screen: a menu box with three tabs (game, display, sound) and one row per setting; the button on each
    /// row shows and changes its value. The tab buttons sit under the title, the tab not on show looks dull.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        private const int SortingOrder = 105;
        // Phase 3 (640×360): the rows centred on the screen, a little lower, with room above the tabs.
        private const int TabsTop = 60;
        private const int TabWidth = 96;
        private const int TabGap = 8;
        private const int RowsTop = 88;
        private const int RowHeight = 30;   // five rows on the game tab (2.1.1: the four-colour deck)
        private const int LabelX = PixelScreen.Width / 2 - 176;
        private const int ValueX = PixelScreen.Width / 2 + 48;
        private const int ValueWidth = 128;

        private Canvas _canvas;
        private readonly Dictionary<SettingsTab, GameObject> _pages = new Dictionary<SettingsTab, GameObject>();
        private readonly Dictionary<SettingsTab, ButtonFeel> _tabs = new Dictionary<SettingsTab, ButtonFeel>();

        private Text _speed;
        private Text _handGuide;
        private Text _tips;
        private Text _fourColor;
        private ButtonFeel _tipsFeel;
        private Text _windowMode;
        private Text _windowScale;
        private ButtonFeel _windowScaleFeel;
        private Text _pixelScale;
        private ButtonFeel _pixelScaleFeel;
        private Text _vSync;
        private Text _screenShake;
        private Text _master;
        private Text _music;
        private Text _sfx;

        public event Action<SettingsTab> TabPressed;
        public event Action SpeedPressed;
        public event Action HandGuidePressed;
        public event Action ResetTipsPressed;
        public event Action FourColorPressed;
        public event Action<int> LanguageChosen;
        public event Action FullscreenPressed;
        public event Action WindowScalePressed;
        public event Action PixelScalePressed;
        public event Action VSyncPressed;
        public event Action ScreenShakePressed;
        public event Action MasterPressed;
        public event Action MusicPressed;
        public event Action SfxPressed;
        public event Action BackPressed;

        public bool IsVisible => _canvas.enabled;

        public static SettingsView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("SettingsCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<SettingsView>();
            view._canvas = canvas;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            MenuBackdrop.Create(screen);   // the title screen's art stays behind its sub-screens

            Image panel = UiFactory.CreatePanel("SettingsPanel", screen);
            panel.rectTransform.PlaceTL(32, 16, PixelScreen.Width - 64, PixelScreen.Height - 32);

            UiFactory.CreateText("Title", screen, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.SettingsTitle)
                .rectTransform.PlaceTL(0, 24, PixelScreen.Width, 16);

            Tab(screen, SettingsTab.Game, "SettingsTabGame", () => UiText.SettingsTabGame);
            Tab(screen, SettingsTab.Display, "SettingsTabDisplay", () => UiText.SettingsTabDisplay);
            Tab(screen, SettingsTab.Sound, "SettingsTabSound", () => UiText.SettingsTabSound);
            Tab(screen, SettingsTab.Language, "SettingsTabLanguage", () => UiText.SettingsTabLanguage);

            Transform game = Page(screen, SettingsTab.Game);
            _speed = Row(game, 0, () => UiText.SettingSpeed, () => UiText.SettingSpeedHint, "SpeedButton", () => SpeedPressed?.Invoke(), out _);
            _handGuide = Row(game, 1, () => UiText.SettingHandGuide, () => UiText.SettingHandGuideHint, "HandGuideButton",
                () => HandGuidePressed?.Invoke(), out _);
            _tips = Row(game, 2, () => UiText.SettingTips, () => UiText.SettingTipsHint, "ResetTipsButton", () => ResetTipsPressed?.Invoke(),
                out Button tips);
            _tipsFeel = tips.GetComponent<ButtonFeel>();
            // Demo 3.3: the language has its own tab; the rows close up.
            _fourColor = Row(game, 3, () => UiText.SettingFourColor, () => UiText.SettingFourColorHint, "FourColorButton",
                () => FourColorPressed?.Invoke(), out _);

            Transform display = Page(screen, SettingsTab.Display);
            _windowMode = Row(display, 0, () => UiText.SettingWindowMode, () => UiText.SettingFullscreenHint, "FullscreenButton",
                () => FullscreenPressed?.Invoke(), out _);
            _windowScale = Row(display, 1, () => UiText.SettingWindowScale, () => UiText.SettingWindowScaleHint, "WindowScaleButton",
                () => WindowScalePressed?.Invoke(), out Button windowScale);
            _windowScaleFeel = windowScale.GetComponent<ButtonFeel>();
            _pixelScale = Row(display, 2, () => UiText.SettingPixelScale, () => UiText.SettingPixelScaleHint, "PixelScaleButton",
                () => PixelScalePressed?.Invoke(), out Button pixelScale);
            _pixelScaleFeel = pixelScale.GetComponent<ButtonFeel>();
            _vSync = Row(display, 3, () => UiText.SettingVSync, () => UiText.SettingVSyncHint, "VSyncButton", () => VSyncPressed?.Invoke(), out _);
            _screenShake = Row(display, 4, () => UiText.SettingScreenShake, () => UiText.SettingScreenShakeHint, "ScreenShakeButton",
                () => ScreenShakePressed?.Invoke(), out _);

            _languagePage = Page(screen, SettingsTab.Language);
            UiFactory.CreateText("LanguageHint", _languagePage, "", 8, Palette.BoneDark, TextAnchor.MiddleCenter).Localized(() => UiText.SettingsLanguageListHint)
                .rectTransform.PlaceTL(32, RowsTop - 4, PixelScreen.Width - 64, 9);

            Transform sound = Page(screen, SettingsTab.Sound);
            _master = Row(sound, 0, () => UiText.SettingMaster, () => UiText.SettingMasterHint, "MasterButton", () => MasterPressed?.Invoke(), out _);
            _music = Row(sound, 1, () => UiText.SettingMusic, () => UiText.SettingMusicHint, "MusicButton", () => MusicPressed?.Invoke(), out _);
            _sfx = Row(sound, 2, () => UiText.SettingSfx, () => UiText.SettingSfxHint, "SfxButton", () => SfxPressed?.Invoke(), out _);

            UiFactory.CreateText("SettingsTabsHint", screen, "", 8, Palette.BoneDark, TextAnchor.MiddleCenter).Localized(() => UiText.SettingsTabsHint)
                .rectTransform.PlaceTL(32, RowsTop + 5 * RowHeight + 2, PixelScreen.Width - 64, 8);

            Button back = UiFactory.CreateButton("SettingsBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Blood);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 80) / 2, PixelScreen.Height - 40, 80, 20);
            back.onClick.AddListener(() => BackPressed?.Invoke());

            ShowPage(SettingsTab.Game);
        }

        private void Tab(Transform screen, SettingsTab tab, string name, Func<string> label)
        {
            int count = _tabs.Count;
            int tabs = Enum.GetValues(typeof(SettingsTab)).Length;
            int left = (PixelScreen.Width - tabs * TabWidth - (tabs - 1) * TabGap) / 2;
            Button button = UiFactory.CreateButton(name, screen, "", 8, out Text text, ButtonSkin.Ember);
            text.Localized(label);
            ((RectTransform)button.transform).PlaceTL(left + count * (TabWidth + TabGap), TabsTop, TabWidth, 18);
            button.onClick.AddListener(() => TabPressed?.Invoke(tab));
            _tabs[tab] = button.GetComponent<ButtonFeel>();
        }

        private Transform Page(Transform screen, SettingsTab tab)
        {
            RectTransform page = UiFactory.CreateRect("SettingsPage" + tab, screen).PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);
            _pages[tab] = page.gameObject;
            return page;
        }

        private static Text Row(Transform page, int index, Func<string> label, Func<string> hint, string buttonName, Action pressed, out Button button)
        {
            int y = RowsTop + index * RowHeight;
            UiFactory.CreateText(buttonName + "Label", page, "", 8, Palette.Bone, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline().Localized(label)
                .rectTransform.PlaceTL(LabelX, y + 2, 216, 8);
            Text hintText = UiFactory.CreateText(buttonName + "Hint", page, "", 8, Palette.BoneDark, TextAnchor.UpperLeft).Localized(hint);
            hintText.rectTransform.PlaceTL(LabelX, y + 12, 216, 18);

            button = UiFactory.CreateButton(buttonName, page, "", 8, out Text value, ButtonSkin.Ember);
            ((RectTransform)button.transform).PlaceTL(ValueX, y, ValueWidth, 18);
            button.onClick.AddListener(() => pressed());
            return value;
        }

        private void ShowPage(SettingsTab tab)
        {
            foreach (var page in _pages) page.Value.SetActive(page.Key == tab);
            foreach (var button in _tabs) button.Value.Locked = button.Key != tab;   // the tabs not on show look dull (still clickable)
        }

        public void Render(SettingsScreen screen)
        {
            ShowPage(screen.Tab);
            _speed.text = screen.Speed;
            _handGuide.text = screen.HandGuide ? UiText.On : UiText.Off;
            _tips.text = screen.TipsLeft ? UiText.ResetTips : UiText.TipsFresh;
            _tipsFeel.Locked = !screen.TipsLeft;
            _fourColor.text = screen.FourColor ? UiText.On : UiText.Off;
            _windowMode.text = screen.WindowMode;
            _windowScale.text = screen.WindowScale;
            _windowScaleFeel.Locked = screen.WindowScaleLocked;
            _pixelScale.text = screen.PixelScale;
            _pixelScaleFeel.Locked = screen.PixelScaleLocked;
            _vSync.text = screen.VSync;
            _screenShake.text = screen.ScreenShake ?? "";
            _master.text = screen.Master;
            _music.text = screen.Music;
            _sfx.text = screen.Sfx;
            RenderLanguages(screen);
        }

        // ------------------------------------------------------------------ the LANGUAGE tab (Demo 3.3)

        private const int LanguageWidth = 192, LanguageHeight = 20;
        private Transform _languagePage;
        private readonly List<(Button button, Text label, GameObject arrow, Outline frame)> _languageRows = new List<(Button, Text, GameObject, Outline)>();

        /// <summary>The language list's rows (for tests): each language's button, by its place in the list.</summary>
        public IReadOnlyList<Button> LanguageButtons => _languageRows.ConvertAll(r => r.button);

        /// <summary>Which row the arrow marks (the language on), for tests.</summary>
        public int LanguageMarked { get; private set; } = -1;

        /// <summary>
        /// Every language by its own name, in its own words, one under the other: the one on carries a pixel arrow and a gold frame, the one the
        /// keys point at is bright (the others dull). The rows come from the screen's list — a new language is a new row, not a new button here.
        /// </summary>
        private void RenderLanguages(SettingsScreen screen)
        {
            IReadOnlyList<string> names = screen.Languages ?? new string[0];
            while (_languageRows.Count < names.Count) _languageRows.Add(LanguageRow(_languageRows.Count));
            for (int i = 0; i < _languageRows.Count; i++)
            {
                var (button, label, arrow, frame) = _languageRows[i];
                bool shown = i < names.Count;
                button.gameObject.SetActive(shown);
                if (!shown) continue;
                label.text = names[i];
                bool marked = i == screen.LanguageSelected;
                arrow.SetActive(marked);
                frame.effectColor = marked ? Palette.GoldLight : Palette.Black;
                button.GetComponent<ButtonFeel>().Locked = i != screen.LanguageCursor;
            }
            LanguageMarked = screen.LanguageSelected;
        }

        private (Button, Text, GameObject, Outline) LanguageRow(int index)
        {
            int x = (PixelScreen.Width - LanguageWidth) / 2, y = RowsTop + 12 + index * RowHeight;
            Button button = UiFactory.CreateButton("SettingsLanguage" + index, _languagePage, "", 8, out Text label, ButtonSkin.Ember);
            ((RectTransform)button.transform).PlaceTL(x, y, LanguageWidth, LanguageHeight);
            button.onClick.AddListener(() => LanguageChosen?.Invoke(index));
            Outline frame = UiFactory.AddBorder(button.gameObject, Palette.Black, 1f);
            // A pixel arrow pointing at the language that is on: a 4×7 triangle, whole pixels.
            RectTransform arrow = UiFactory.CreateRect("Marked", button.transform).PlaceTL(-10, (LanguageHeight - 7) / 2, 4, 7);
            int[] widths = { 1, 2, 3, 4, 3, 2, 1 };
            for (int row = 0; row < widths.Length; row++)
            {
                Image pixel = UiFactory.CreateImage("Row" + row, arrow, Palette.GoldLight);
                pixel.raycastTarget = false;
                pixel.rectTransform.PlaceTL(0, row, widths[row], 1);
            }
            return (button, label, arrow.gameObject, frame);
        }

        public void Show()
        {
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }
    }
}
