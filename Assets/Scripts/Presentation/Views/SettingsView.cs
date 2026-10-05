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
        private const int TabsTop = 44;
        private const int TabWidth = 96;
        private const int TabGap = 8;
        private const int RowsTop = 74;
        private const int RowHeight = 32;
        private const int LabelX = 64;
        private const int ValueX = 288;
        private const int ValueWidth = 128;

        private Canvas _canvas;
        private readonly Dictionary<SettingsTab, GameObject> _pages = new Dictionary<SettingsTab, GameObject>();
        private readonly Dictionary<SettingsTab, ButtonFeel> _tabs = new Dictionary<SettingsTab, ButtonFeel>();

        private Text _speed;
        private Text _handGuide;
        private Text _tips;
        private Text _language;
        private ButtonFeel _tipsFeel;
        private Text _windowMode;
        private Text _windowScale;
        private ButtonFeel _windowScaleFeel;
        private Text _pixelScale;
        private ButtonFeel _pixelScaleFeel;
        private Text _vSync;
        private Text _master;
        private Text _music;
        private Text _sfx;

        public event Action<SettingsTab> TabPressed;
        public event Action SpeedPressed;
        public event Action HandGuidePressed;
        public event Action ResetTipsPressed;
        public event Action LanguagePressed;
        public event Action FullscreenPressed;
        public event Action WindowScalePressed;
        public event Action PixelScalePressed;
        public event Action VSyncPressed;
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

            Transform game = Page(screen, SettingsTab.Game);
            _speed = Row(game, 0, () => UiText.SettingSpeed, () => UiText.SettingSpeedHint, "SpeedButton", () => SpeedPressed?.Invoke(), out _);
            _handGuide = Row(game, 1, () => UiText.SettingHandGuide, () => UiText.SettingHandGuideHint, "HandGuideButton",
                () => HandGuidePressed?.Invoke(), out _);
            _tips = Row(game, 2, () => UiText.SettingTips, () => UiText.SettingTipsHint, "ResetTipsButton", () => ResetTipsPressed?.Invoke(),
                out Button tips);
            _tipsFeel = tips.GetComponent<ButtonFeel>();
            _language = Row(game, 3, () => UiText.SettingLanguage, () => UiText.SettingLanguageHint, "SettingsLanguageButton",
                () => LanguagePressed?.Invoke(), out _);

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

            Transform sound = Page(screen, SettingsTab.Sound);
            _master = Row(sound, 0, () => UiText.SettingMaster, () => UiText.SettingMasterHint, "MasterButton", () => MasterPressed?.Invoke(), out _);
            _music = Row(sound, 1, () => UiText.SettingMusic, () => UiText.SettingMusicHint, "MusicButton", () => MusicPressed?.Invoke(), out _);
            _sfx = Row(sound, 2, () => UiText.SettingSfx, () => UiText.SettingSfxHint, "SfxButton", () => SfxPressed?.Invoke(), out _);

            UiFactory.CreateText("SettingsTabsHint", screen, "", 8, Palette.BoneDark, TextAnchor.MiddleCenter).Localized(() => UiText.SettingsTabsHint)
                .rectTransform.PlaceTL(32, RowsTop + 4 * RowHeight + 2, PixelScreen.Width - 64, 8);

            Button back = UiFactory.CreateButton("SettingsBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Blood);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 80) / 2, PixelScreen.Height - 44, 80, 20);
            back.onClick.AddListener(() => BackPressed?.Invoke());

            ShowPage(SettingsTab.Game);
        }

        private void Tab(Transform screen, SettingsTab tab, string name, Func<string> label)
        {
            int count = _tabs.Count;
            int left = (PixelScreen.Width - 3 * TabWidth - 2 * TabGap) / 2;
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
            _language.text = screen.Language;
            _windowMode.text = screen.WindowMode;
            _windowScale.text = screen.WindowScale;
            _windowScaleFeel.Locked = screen.WindowScaleLocked;
            _pixelScale.text = screen.PixelScale;
            _pixelScaleFeel.Locked = screen.PixelScaleLocked;
            _vSync.text = screen.VSync;
            _master.text = screen.Master;
            _music.text = screen.Music;
            _sfx.text = screen.Sfx;
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
