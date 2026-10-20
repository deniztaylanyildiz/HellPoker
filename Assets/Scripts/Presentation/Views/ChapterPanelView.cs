using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The panel over a chapter's map: who speaks (a 48×48 portrait — a stranger's own art or a face's first idle frame), their name,
    /// a title and the words, then the answers as buttons from the bottom up (two columns when there are many). What the pointed
    /// answer means shows in a line above them (hovered with the mouse, or picked with the arrows — framed in ember). A locked answer
    /// stays clickable but dim. Clicks behind the panel are swallowed while it is open. Demo 3.5: cards — the ones only to look at
    /// (<see cref="PanelCard.Showcase"/>) and the answers that are cards — stand in a row under the words (the panel widens for them), the
    /// small cards (a carried relic to leave behind) in a grid under them, the other answers as buttons at the foot.
    /// </summary>
    public sealed class ChapterPanelView : MonoBehaviour, IChapterPanelView
    {
        public const int SortingOrder = 45;

        /// <summary>The area the panel sits in (over the map, beside its side panel); the panel is as tall as its words need, centred in it.</summary>
        // Phase 3 (640×360): wider and taller, centred over the map (right of its side panel).
        private const int Width = 400, Height = PixelScreen.Height - 16, MinHeight = 104;
        private const int X = 132 + (PixelScreen.Width - 132 - Width) / 2, Y = 8;
        private const int Portrait = 48;
        private const int Row = 20;

        private Canvas _canvas;
        private RectTransform _box;
        private DealerAnimationLibrary _dealers;
        private GameObject _frame;
        private Image _portrait;
        private Text _owner;
        private Text _title;
        private Text _text;
        private Text _detail;
        private Text _barLabel;
        private BossBarWidget _bar;

        /// <summary>The bar under the words (for tests and screenshots).</summary>
        public BossBarWidget Bar => _bar;
        private RectTransform _options;
        private Image _pick;
        private readonly List<Button> _buttons = new List<Button>();

        /// <summary>Each answer's button or card (by its index; one of the two is null), and the cards only to look at.</summary>
        private readonly List<RelicCardView> _optionCards = new List<RelicCardView>();
        private readonly List<RelicCardView> _showcase = new List<RelicCardView>();
        private int _hovered = -1;
        private int _width = Width;

        /// <summary>The cards on show — the row's and the small ones, by answer (null where an answer is a button), then those only to look
        /// at (for tests and screenshots).</summary>
        public IReadOnlyList<RelicCardView> OptionCards => _optionCards;
        public IReadOnlyList<RelicCardView> ShowcaseCards => _showcase;
        public RectTransform Box => _box;

        public event Action<int> OptionPressed;

        public bool IsOpen => _canvas.enabled;

        /// <summary>The card on show (for tests and screenshots); null when closed.</summary>
        public PanelCard Card { get; private set; }

        public static ChapterPanelView Create(Transform parent, DealerAnimationLibrary dealers)
        {
            Canvas canvas = UiFactory.CreateScreen("ChapterPanelCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var view = canvas.gameObject.AddComponent<ChapterPanelView>();
            view._canvas = canvas;
            view._dealers = dealers;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            // A clear layer over the whole screen swallows clicks while the panel waits.
            Image shade = UiFactory.CreateImage("Shade", screen, Color.clear);
            shade.rectTransform.Stretch();
            shade.raycastTarget = true;

            Image panel = UiFactory.CreatePanel("Panel", screen, hot: true);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(X, Y, Width, Height);
            _box = panel.rectTransform;

            Image frame = UiFactory.CreateImage("PortraitFrame", panel.transform, Palette.Black);
            frame.rectTransform.PlaceTL(8, 8, Portrait + 4, Portrait + 4);
            UiFactory.AddBorder(frame.gameObject, Palette.Gold, 1f);
            _frame = frame.gameObject;
            _portrait = UiFactory.CreateImage("Portrait", frame.transform, Color.white);
            _portrait.raycastTarget = false;
            _portrait.rectTransform.PlaceTL(2, 2, Portrait, Portrait);

            _owner = UiFactory.CreateText("Owner", panel.transform, "", 8, Palette.Ember, TextAnchor.UpperLeft);
            _title = UiFactory.CreateText("Title", panel.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithOutline();
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text = UiFactory.CreateText("Text", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _text.lineSpacing = 1f;
            _detail = UiFactory.CreateText("Detail", panel.transform, "", 8, Palette.GoldLight, TextAnchor.LowerLeft);
            _detail.lineSpacing = 1f;

            // A demon's bar under the words (the gate's), small and without a number.
            _barLabel = UiFactory.CreateText("BarLabel", panel.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            _bar = BossBarWidget.Create(panel.transform, "PanelBossBar", 0, 0, 160, 6);

            _options = UiFactory.CreateRect("Options", panel.transform).PlaceTL(0, 0, Width, Height);
            _pick = UiFactory.CreateImage("Pick", _options, Palette.Ember);
            _pick.raycastTarget = false;
        }

        private const int CardGap = 8, SmallGap = 8, MaxWidth = PixelScreen.Width - 16;

        /// <summary>Down to the 8 px grid (the cards' places in the panel).</summary>
        private static int Snap(int v) => v / 8 * 8;

        public void Show(PanelCard card)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;

            // What is a card and what is a button: the row (cards only to look at, then the answers that are big cards), the small cards, the rest.
            var bigOptions = new List<int>();
            var smallOptions = new List<int>();
            var buttons = new List<int>();
            for (int i = 0; i < card.Options.Count; i++)
            {
                RelicCardData data = card.Options[i].Card;
                if (data == null) buttons.Add(i);
                else if (data.Small) smallOptions.Add(i);
                else bigOptions.Add(i);
            }
            int row = card.Showcase.Count + bigOptions.Count;
            int smallColumns = smallOptions.Count > 6 ? 3 : smallOptions.Count > 1 ? 2 : 1;
            int smallRows = (smallOptions.Count + smallColumns - 1) / Math.Max(1, smallColumns);
            int rowWidth = row > 0 ? row * RelicCardView.Width + (row - 1) * CardGap + 16 : 0;
            int smallWidth = smallOptions.Count > 0 ? smallColumns * RelicCardView.SmallWidth + (smallColumns - 1) * CardGap + 16 : 0;
            _width = Mathf.Clamp(Math.Max(Width, Math.Max(rowWidth, smallWidth)), Width, MaxWidth);
            int x = _width == Width ? X : (PixelScreen.Width - _width) / 2 / 8 * 8;
            _options.PlaceTL(0, 0, _width, Height);

            Sprite portrait = PortraitOf(card.PortraitId);
            _frame.SetActive(card.PortraitId != null);
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            int left = card.PortraitId != null ? Portrait + 18 : 10;
            _owner.rectTransform.PlaceTL(left, 9, _width - left - 8, 9);
            _owner.text = card.Owner;
            _title.rectTransform.PlaceTL(left, card.Owner.Length > 0 ? 21 : 12, _width - left - 8, 8);
            _title.text = card.Title;

            int columns = buttons.Count > 4 ? 2 : 1;
            int rows = (buttons.Count + columns - 1) / columns;

            // As tall as the words need: the portrait or the text, the cards, the line of what an answer means (when any has one), the answers.
            int textTop = card.Owner.Length > 0 ? 33 : 24;
            _text.text = card.Text;
            _text.rectTransform.PlaceTL(left, textTop, _width - left - 8, 10);
            int textBottom = textTop + Mathf.CeilToInt(_text.preferredHeight);
            bool bar = !card.Bar.IsHidden;
            _barLabel.gameObject.SetActive(bar);
            if (bar)
            {
                _barLabel.text = card.BarLabel;
                _barLabel.rectTransform.PlaceTL(left, textBottom + 4, _width - left - 8, 9);
                ((RectTransform)_bar.transform).PlaceTL(left, textBottom + 14, 160, 6);
                textBottom += 22;
            }
            _bar.Apply(card.Bar, animate: false);
            int header = Math.Max(card.PortraitId != null ? 8 + Portrait + 4 : 0, textBottom);
            bool details = false;
            foreach (int i in buttons) details |= card.Options[i].Detail.Length > 0;
            int cardsHeight = (row > 0 || smallRows > 0 ? 8 : 0) + (row > 0 ? RelicCardView.Height + CardGap + 2 : 0)
                              + (smallRows > 0 ? smallRows * (RelicCardView.SmallHeight + SmallGap) + 4 : 0);
            int height = Mathf.Clamp(header + 8 + cardsHeight + (details ? 28 : 0) + rows * Row + 6, MinHeight, Height);
            _box.PlaceTL(x, Y + (Height - height) / 2, _width, height);

            int top = height - 8 - rows * Row;
            BuildOptions(card, buttons, columns, top);
            BuildCards(card, bigOptions, smallOptions, smallColumns, Snap(header + 8 + 7) + 2);
            _detail.rectTransform.PlaceTL(10, top - 28, _width - 20, 26);
            int textLimit = cardsHeight > 0 ? header + 8 : top - (details ? 30 : 2);
            _text.rectTransform.PlaceTL(left, textTop, _width - left - 8, Math.Max(10, textLimit - textTop));
            _hovered = -1;
            ShowDetail();
        }

        /// <summary>The row of cards (those only to look at, then the answers that are cards), centred; the small cards under it.</summary>
        private void BuildCards(PanelCard card, List<int> bigOptions, List<int> smallOptions, int smallColumns, int top)
        {
            foreach (RelicCardView old in _optionCards)
                if (old != null) Destroy(old.gameObject);
            foreach (RelicCardView old in _showcase)
                if (old != null) Destroy(old.gameObject);
            _optionCards.Clear();
            _showcase.Clear();
            for (int i = 0; i < card.Options.Count; i++) _optionCards.Add(null);

            int row = card.Showcase.Count + bigOptions.Count;
            int rowWidth = row * RelicCardView.Width + (row - 1) * CardGap;
            int x = Snap((_width - rowWidth) / 2);
            int slot = 0;
            foreach (RelicCardData data in card.Showcase)
            {
                RelicCardView view = RelicCardView.Create(_options, "ShowCard" + _showcase.Count, small: false);
                view.Show(data);
                view.PlaceAt(x + slot++ * (RelicCardView.Width + CardGap), top);
                _showcase.Add(view);
            }
            foreach (int index in bigOptions)
            {
                RelicCardView view = RelicCardView.Create(_options, "PanelCard" + index, small: false);
                view.Show(card.Options[index].Card);
                view.PlaceAt(x + slot++ * (RelicCardView.Width + CardGap), top);
                int pressed = index;
                view.Pressed += () => OptionPressed?.Invoke(pressed);
                view.GetComponentInChildren<Button>().name = "PanelOption" + index;   // pressed by name like any answer (tests, the tour)
                _optionCards[index] = view;
            }
            int smallTop = top + (row > 0 ? RelicCardView.Height + CardGap : 0);
            int smallWidth = smallColumns * RelicCardView.SmallWidth + (smallColumns - 1) * CardGap;
            int smallLeft = Snap((_width - smallWidth) / 2);
            for (int k = 0; k < smallOptions.Count; k++)
            {
                int index = smallOptions[k];
                RelicCardView view = RelicCardView.Create(_options, "PanelCard" + index, small: true);
                view.Show(card.Options[index].Card);
                view.PlaceAt(smallLeft + k % smallColumns * (RelicCardView.SmallWidth + CardGap), smallTop + k / smallColumns * (RelicCardView.SmallHeight + SmallGap));
                int pressed = index;
                view.Pressed += () => OptionPressed?.Invoke(pressed);
                view.GetComponentInChildren<Button>().name = "PanelOption" + index;   // pressed by name like any answer (tests, the tour)
                _optionCards[index] = view;
            }
        }

        public void Hide()
        {
            Card = null;
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildOptions(PanelCard card, List<int> indices, int columns, int top)
        {
            foreach (Button old in _buttons)
                if (old != null) Destroy(old.gameObject);
            _buttons.Clear();
            for (int i = 0; i < card.Options.Count; i++) _buttons.Add(null);

            int width = (_width - 16 - (columns - 1) * 6) / columns;
            for (int k = 0; k < indices.Count; k++)
            {
                int i = k;
                int index = indices[k];
                PanelOption option = card.Options[index];
                Button button = UiFactory.CreateButton("PanelOption" + index, _options, option.Label, 8, out Text label,
                    option.IsLeave ? ButtonSkin.Ash : ButtonSkin.Ember);
                if (UiArt.Body != null) label.font = UiArt.Body;   // the text font: the answers are long
                int column = i % columns, row = i / columns;
                ((RectTransform)button.transform).PlaceTL(8 + column * (width + 6), top + row * Row, width, Row - 2);
                button.onClick.AddListener(() => OptionPressed?.Invoke(index));
                button.GetComponent<ButtonFeel>().Locked = !option.Enabled;
                AddHover(button.gameObject, () => { _hovered = index; ShowDetail(); }, () => { if (_hovered == index) { _hovered = -1; ShowDetail(); } });
                _buttons[index] = button;
            }
        }

        /// <summary>The pointed answer's meaning, and its ember frame (the keyboard's pick).</summary>
        private void ShowDetail()
        {
            if (Card == null) return;
            int shown = _hovered >= 0 ? _hovered : Card.Selected;
            _detail.text = shown >= 0 && shown < Card.Options.Count ? Card.Options[shown].Detail : "";
            int selected = Card.Selected;
            for (int i = 0; i < _optionCards.Count; i++)
                if (_optionCards[i] != null) _optionCards[i].SetSelected(i == selected && Card.Options.Count > 1);
            bool framed = selected >= 0 && selected < _buttons.Count && _buttons[selected] != null && Card.Options.Count > 1;
            _pick.enabled = framed;
            if (!framed) return;
            var rect = (RectTransform)_buttons[selected].transform;
            Vector2 at = rect.anchoredPosition;
            _pick.rectTransform.PlaceTL(Mathf.RoundToInt(at.x) - 2, Mathf.RoundToInt(-at.y) - 2, Mathf.RoundToInt(rect.sizeDelta.x) + 4, Mathf.RoundToInt(rect.sizeDelta.y) + 4);
            _pick.transform.SetAsFirstSibling();
        }

        private static void AddHover(GameObject target, Action enter, Action exit)
        {
            var trigger = target.AddComponent<EventTrigger>();
            var on = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            on.callback.AddListener(_ => enter());
            var off = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            off.callback.AddListener(_ => exit());
            trigger.triggers.Add(on);
            trigger.triggers.Add(off);
        }

        private Sprite PortraitOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id.StartsWith("Events/", StringComparison.Ordinal)) return UiArt.Sprite(id);
            SpriteClip clip = _dealers?.Get(id, DealerAnimation.Idle);
            return clip != null && clip.Frames != null && clip.Frames.Length > 0 ? clip.Frames[0] : null;
        }
    }
}
