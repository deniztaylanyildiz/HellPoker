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

        private Canvas _canvas;
        private GameObject _buttons;
        private GameObject _continueButton;
        private GameObject _rulesPanel;
        private Image _glow;
        private RectTransform _logo;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;

        public bool IsVisible => _canvas.enabled;

        public static MainMenuView Create(Transform parent, string tagline, string rules)
        {
            Canvas canvas = UiFactory.CreateCanvas("MainMenuCanvas", parent, SortingOrder);
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            view._canvas = canvas;
            view.Build(canvas.transform, tagline, rules);
            return view;
        }

        private void Build(Transform root, string tagline, string rules)
        {
            // Opaque background also blocks clicks from reaching the table underneath.
            Image background = UiFactory.CreateSprite("Background", root, UiArt.Background, Palette.Background);
            background.rectTransform.Stretch();
            background.raycastTarget = true;

            _glow = UiFactory.CreateImage("Glow", root, Color.clear);
            _glow.rectTransform.Stretch();
            _glow.sprite = UiFactory.CreateVignetteSprite();
            _glow.raycastTarget = false;

            Image logo = UiFactory.CreateSprite("Title", root, UiArt.Title);
            logo.preserveAspect = true;
            _logo = logo.rectTransform;
            _logo.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1300f, 330f));
            if (logo.sprite == null)
            {
                logo.enabled = false;
                UiFactory.CreateText("TitleText", logo.transform, UiText.Title, 150, Palette.Ember, style: FontStyle.Bold).rectTransform.Stretch();
            }

            UiFactory.CreateText("Tagline", root, tagline, 34, Palette.Bone, style: FontStyle.Italic).WithShadow(2f)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1400f, 96f));
            UiFactory.CreateSprite("Divider", root, UiArt.Divider)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(520f, 40f));
            UiFactory.CreateText("Subtagline", root, UiText.MenuSubtagline, 24, Palette.MutedText).WithShadow()
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(1400f, 36f));

            RectTransform buttons = UiFactory.CreateRect("Buttons", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(420f, 380f));
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _buttons = buttons.gameObject;

            _continueButton = CreateMenuButton(buttons, "ContinueButton", UiText.Continue, ButtonSkin.Ember, () => ContinuePressed?.Invoke());
            CreateMenuButton(buttons, "NewGameButton", UiText.NewGame, ButtonSkin.Blood, () => NewGamePressed?.Invoke());
            CreateMenuButton(buttons, "HowToPlayButton", UiText.HowToPlay, ButtonSkin.Ash, () => ShowRules(true));
            CreateMenuButton(buttons, "QuitButton", UiText.Quit, ButtonSkin.Ash, () => QuitPressed?.Invoke());

            UiFactory.CreateText("Footer", root, UiText.MenuFooter, 18, Palette.MutedText, style: FontStyle.Italic)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(600f, 30f));

            _rulesPanel = BuildRulesPanel(root, rules);
            ShowRules(false);
        }

        private GameObject BuildRulesPanel(Transform root, string rules)
        {
            Image panel = UiFactory.CreatePanel("RulesPanel", root);
            panel.raycastTarget = true;
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(1300f, 650f));
            UiFactory.CreateFrame("Frame", panel.transform, 0.8f).rectTransform.Stretch(-8f);

            UiFactory.CreateText("Title", panel.transform, UiText.RulesTitle, 34, Palette.Gold, style: FontStyle.Bold).WithShadow()
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1200f, 50f));
            UiFactory.CreateSprite("Divider", panel.transform, UiArt.Divider)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(420f, 30f));

            Text body = UiFactory.CreateText("Body", panel.transform, rules, 25, Palette.Bone, TextAnchor.UpperLeft);
            body.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(1180f, 440f));
            body.lineSpacing = 1.05f;

            Button back = UiFactory.CreateButton("BackButton", panel.transform, UiText.Back, 28, out _, ButtonSkin.Blood);
            ((RectTransform)back.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(260f, 72f));
            back.onClick.AddListener(() => ShowRules(false));
            return panel.gameObject;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string label, ButtonSkin skin, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, label, 34, out _, skin);
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = 78f;
            button.onClick.AddListener(() => onClick());
            return button.gameObject;
        }

        private void ShowRules(bool show)
        {
            _rulesPanel.SetActive(show);
            _buttons.SetActive(!show);
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
            if (!_canvas.enabled) return;

            // A slow, breathing ember glow around the edges; the title smoulders with it.
            // The project blends in linear space, so small alphas already read strongly on screen.
            float breath = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.3f);
            _glow.color = new Color(1f, 0.25f + 0.1f * breath, 0.04f, 0.015f + 0.02f * breath);
            _logo.localScale = Vector3.one * (1f + 0.012f * breath);
        }
    }
}
