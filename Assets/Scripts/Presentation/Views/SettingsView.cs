using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>The settings screen: a menu box with one row per setting; the button on each row shows and changes its value.</summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        private const int SortingOrder = 105;
        private const int RowsTop = 44;
        private const int RowHeight = 26;
        private const int LabelX = 64;
        private const int ValueX = 288;
        private const int ValueWidth = 128;

        private Canvas _canvas;
        private Text _speed;
        private Text _fullscreen;
        private Text _handGuide;
        private Text _tips;
        private Text _language;
        private Text _music;
        private Text _sfx;
        private ButtonFeel _tipsFeel;

        public event Action SpeedPressed;
        public event Action FullscreenPressed;
        public event Action HandGuidePressed;
        public event Action ResetTipsPressed;
        public event Action LanguagePressed;
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

            _speed = Row(screen, 0, () => UiText.SettingSpeed, () => UiText.SettingSpeedHint, "SpeedButton", () => SpeedPressed?.Invoke(), out _);
            _fullscreen = Row(screen, 1, () => UiText.SettingFullscreen, () => UiText.SettingFullscreenHint, "FullscreenButton",
                () => FullscreenPressed?.Invoke(), out _);
            _handGuide = Row(screen, 2, () => UiText.SettingHandGuide, () => UiText.SettingHandGuideHint, "HandGuideButton",
                () => HandGuidePressed?.Invoke(), out _);
            _tips = Row(screen, 3, () => UiText.SettingTips, () => UiText.SettingTipsHint, "ResetTipsButton", () => ResetTipsPressed?.Invoke(),
                out Button tips);
            _tipsFeel = tips.GetComponent<ButtonFeel>();
            _language = Row(screen, 4, () => UiText.SettingLanguage, () => UiText.SettingLanguageHint, "SettingsLanguageButton",
                () => LanguagePressed?.Invoke(), out _);
            _music = Row(screen, 5, () => UiText.SettingMusic, () => UiText.SettingMusicHint, "MusicButton", () => MusicPressed?.Invoke(), out _);
            _sfx = Row(screen, 6, () => UiText.SettingSfx, () => UiText.SettingSfxHint, "SfxButton", () => SfxPressed?.Invoke(), out _);

            Button back = UiFactory.CreateButton("SettingsBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Blood);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 80) / 2, PixelScreen.Height - 44, 80, 20);
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        private static Text Row(Transform screen, int index, Func<string> label, Func<string> hint, string buttonName, Action pressed, out Button button)
        {
            int y = RowsTop + index * RowHeight;
            UiFactory.CreateText(buttonName + "Label", screen, "", 8, Palette.Bone, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline().Localized(label)
                .rectTransform.PlaceTL(LabelX, y + 2, 216, 8);
            Text hintText = UiFactory.CreateText(buttonName + "Hint", screen, "", 8, Palette.BoneDark, TextAnchor.UpperLeft).Localized(hint);
            hintText.rectTransform.PlaceTL(LabelX, y + 12, 216, 18);

            button = UiFactory.CreateButton(buttonName, screen, "", 8, out Text value, ButtonSkin.Ember);
            ((RectTransform)button.transform).PlaceTL(ValueX, y, ValueWidth, 18);
            button.onClick.AddListener(() => pressed());
            return value;
        }

        public void Render(string speed, bool fullscreen, bool handGuide, bool tipsLeft, string language, string music, string sfx)
        {
            _music.text = music;
            _sfx.text = sfx;
            _language.text = language;
            _speed.text = speed;
            _fullscreen.text = fullscreen ? UiText.On : UiText.Off;
            _handGuide.text = handGuide ? UiText.On : UiText.Off;
            _tips.text = tipsLeft ? UiText.ResetTips : UiText.TipsFresh;
            _tipsFeel.Locked = !tipsLeft;
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
