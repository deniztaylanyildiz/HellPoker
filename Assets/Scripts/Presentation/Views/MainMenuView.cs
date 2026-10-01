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
        private Text _title;

        public event Action NewGamePressed;
        public event Action ContinuePressed;
        public event Action QuitPressed;

        public bool IsVisible => _canvas.enabled;

        public static MainMenuView Create(Transform parent, string tagline, string rules)
        {
            var go = new GameObject("MainMenuCanvas", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var view = go.AddComponent<MainMenuView>();
            view._canvas = canvas;
            view.Build(go.transform, tagline, rules);
            return view;
        }

        private void Build(Transform root, string tagline, string rules)
        {
            // Opaque background also blocks clicks from reaching the table underneath.
            UiFactory.CreateImage("Background", root, Palette.Background).rectTransform.Stretch();
            _glow = UiFactory.CreateImage("Glow", root, Color.clear);
            _glow.rectTransform.Stretch();
            _glow.sprite = UiFactory.CreateVignetteSprite();
            _glow.raycastTarget = false;

            _title = UiFactory.CreateText("Title", root, UiText.Title, 150, Palette.Ember, style: FontStyle.Bold);
            _title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1600f, 180f));
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;

            UiFactory.CreateText("Tagline", root, tagline, 32, Palette.Bone)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(1400f, 90f));
            UiFactory.CreateText("Subtagline", root, UiText.MenuSubtagline, 24, Palette.MutedText)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(1400f, 36f));

            RectTransform buttons = UiFactory.CreateRect("Buttons", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(440f, 400f));
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _buttons = buttons.gameObject;

            _continueButton = CreateMenuButton(buttons, "ContinueButton", UiText.Continue, Palette.Ember, () => ContinuePressed?.Invoke());
            CreateMenuButton(buttons, "NewGameButton", UiText.NewGame, Palette.Button, () => NewGamePressed?.Invoke());
            CreateMenuButton(buttons, "HowToPlayButton", UiText.HowToPlay, Palette.Fold, () => ShowRules(true));
            CreateMenuButton(buttons, "QuitButton", UiText.Quit, Palette.Fold, () => QuitPressed?.Invoke());

            UiFactory.CreateText("Footer", root, UiText.MenuFooter, 18, Palette.MutedText)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(600f, 30f));

            _rulesPanel = BuildRulesPanel(root, rules);
            ShowRules(false);
        }

        private GameObject BuildRulesPanel(Transform root, string rules)
        {
            Image panel = UiFactory.CreateImage("RulesPanel", root, Palette.Felt);
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1240f, 640f));
            UiFactory.AddBorder(panel.gameObject, Palette.Ember, 2f);

            UiFactory.CreateText("Title", panel.transform, UiText.RulesTitle, 32, Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(1200f, 50f));

            Text body = UiFactory.CreateText("Body", panel.transform, rules, 23, Palette.Bone, TextAnchor.UpperLeft);
            body.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(1140f, 440f));
            body.lineSpacing = 1.1f;

            Button back = UiFactory.CreateButton("BackButton", panel.transform, UiText.Back, 28, out _);
            ((RectTransform)back.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(260f, 70f));
            back.onClick.AddListener(() => ShowRules(false));
            return panel.gameObject;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string label, Color color, Action onClick)
        {
            Button button = UiFactory.CreateButton(name, parent, label, 34, out _);
            ((Image)button.targetGraphic).color = color;
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = 80f;
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

            // A slow, breathing ember glow around the edges.
            float breath = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.3f);
            _glow.color = new Color(1f, 0.25f + 0.1f * breath, 0.04f, 0.18f + 0.14f * breath);
            _title.color = Color.Lerp(Palette.Ember, Palette.Gold, breath * 0.35f);
        }
    }
}
