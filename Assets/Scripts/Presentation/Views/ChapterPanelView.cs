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
    /// stays clickable but dim. Clicks behind the panel are swallowed while it is open.
    /// </summary>
    public sealed class ChapterPanelView : MonoBehaviour, IChapterPanelView
    {
        public const int SortingOrder = 45;

        /// <summary>The area the panel sits in (over the map, beside its side panel); the panel is as tall as its words need, centred in it.</summary>
        private const int X = 136, Y = 8, Width = 336, Height = 254, MinHeight = 104;
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
        private RectTransform _options;
        private Image _pick;
        private readonly List<Button> _buttons = new List<Button>();
        private int _hovered = -1;

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

            _options = UiFactory.CreateRect("Options", panel.transform).PlaceTL(0, 0, Width, Height);
            _pick = UiFactory.CreateImage("Pick", _options, Palette.Ember);
            _pick.raycastTarget = false;
        }

        public void Show(PanelCard card)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;

            Sprite portrait = PortraitOf(card.PortraitId);
            _frame.SetActive(card.PortraitId != null);
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            int left = card.PortraitId != null ? Portrait + 18 : 10;
            _owner.rectTransform.PlaceTL(left, 9, Width - left - 8, 9);
            _owner.text = card.Owner;
            _title.rectTransform.PlaceTL(left, card.Owner.Length > 0 ? 21 : 12, Width - left - 8, 8);
            _title.text = card.Title;

            int count = card.Options.Count;
            int columns = count > 4 ? 2 : 1;
            int rows = (count + columns - 1) / columns;

            // As tall as the words need: the portrait or the text, the line of what an answer means (when any has one), the answers.
            int textTop = card.Owner.Length > 0 ? 33 : 24;
            _text.text = card.Text;
            _text.rectTransform.PlaceTL(left, textTop, Width - left - 8, 10);
            int textBottom = textTop + Mathf.CeilToInt(_text.preferredHeight);
            int header = Math.Max(card.PortraitId != null ? 8 + Portrait + 4 : 0, textBottom);
            bool details = false;
            foreach (PanelOption option in card.Options) details |= option.Detail.Length > 0;
            int height = Mathf.Clamp(header + 8 + (details ? 28 : 0) + rows * Row + 6, MinHeight, Height);
            _box.PlaceTL(X, Y + (Height - height) / 2, Width, height);

            int top = height - 8 - rows * Row;
            BuildOptions(card.Options, columns, top);
            _detail.rectTransform.PlaceTL(10, top - 28, Width - 20, 26);
            _text.rectTransform.PlaceTL(left, textTop, Width - left - 8, Math.Max(10, top - (details ? 30 : 2) - textTop));
            _hovered = -1;
            ShowDetail();
        }

        public void Hide()
        {
            Card = null;
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildOptions(IReadOnlyList<PanelOption> options, int columns, int top)
        {
            foreach (Button old in _buttons)
                if (old != null) Destroy(old.gameObject);
            _buttons.Clear();

            int width = (Width - 16 - (columns - 1) * 6) / columns;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                PanelOption option = options[i];
                Button button = UiFactory.CreateButton("PanelOption" + i, _options, option.Label, 8, out Text label,
                    option.IsLeave ? ButtonSkin.Ash : ButtonSkin.Ember);
                if (UiArt.Body != null) label.font = UiArt.Body;   // the text font: the answers are long
                int column = i % columns, row = i / columns;
                ((RectTransform)button.transform).PlaceTL(8 + column * (width + 6), top + row * Row, width, Row - 2);
                button.onClick.AddListener(() => OptionPressed?.Invoke(index));
                button.GetComponent<ButtonFeel>().Locked = !option.Enabled;
                AddHover(button.gameObject, () => { _hovered = index; ShowDetail(); }, () => { if (_hovered == index) { _hovered = -1; ShowDetail(); } });
                _buttons.Add(button);
            }
        }

        /// <summary>The pointed answer's meaning, and its ember frame (the keyboard's pick).</summary>
        private void ShowDetail()
        {
            if (Card == null) return;
            int shown = _hovered >= 0 ? _hovered : Card.Selected;
            _detail.text = shown >= 0 && shown < Card.Options.Count ? Card.Options[shown].Detail : "";
            int selected = Card.Selected;
            bool framed = selected >= 0 && selected < _buttons.Count && Card.Options.Count > 1;
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
