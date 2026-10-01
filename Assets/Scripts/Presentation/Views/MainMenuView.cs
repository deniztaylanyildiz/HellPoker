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
        private GameObject _continueButton;
        private GameObject _front;
        private GameObject _rulesPanel;
        private RectTransform _logo;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;

        public bool IsVisible => _canvas.enabled;

        public static MainMenuView Create(Transform parent, string tagline, string rules)
        {
            Canvas canvas = UiFactory.CreateScreen("MainMenuCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            view._canvas = canvas;
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

            Transform buttons = UiFactory.CreateRect("Buttons", front).Stretch();
            int x = (PixelScreen.Width - ButtonWidth) / 2;
            _continueButton = CreateMenuButton(buttons, "ContinueButton", UiText.Continue, ButtonSkin.Ember, x, 138, () => ContinuePressed?.Invoke());
            CreateMenuButton(buttons, "NewGameButton", UiText.NewGame, ButtonSkin.Blood, x, 164, () => NewGamePressed?.Invoke());
            CreateMenuButton(buttons, "HowToPlayButton", UiText.HowToPlay, ButtonSkin.Ash, x, 190, () => ShowRules(true));
            CreateMenuButton(buttons, "QuitButton", UiText.Quit, ButtonSkin.Ash, x, 216, () => QuitPressed?.Invoke());

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

            Button back = UiFactory.CreateButton("BackButton", panel.transform, UiText.Back, 8, out _, ButtonSkin.Blood);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 16 - 80) / 2, PixelScreen.Height - 16 - 26, 80, ButtonHeight);
            back.onClick.AddListener(() => ShowRules(false));
            return panel.gameObject;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string label, ButtonSkin skin, int x, int y, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, label, 8, out _, skin);
            ((RectTransform)button.transform).PlaceTL(x, y, ButtonWidth, ButtonHeight);
            button.onClick.AddListener(() => onClick());
            return button.gameObject;
        }

        private void ShowRules(bool show)
        {
            _rulesPanel.SetActive(show);
            _front.SetActive(!show);
        }

        public void Show(bool canContinue)
        {
            _continueButton.SetActive(canContinue);
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
