using System;
using System.Collections;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// An event between hands, in the middle of the table: the owner's small portrait (a stranger's own art, or the demon),
    /// their name, the offer's title and words, and a button for each answer. It opens like a curtain, 8 px at a time from
    /// its middle (any key or click hurries it, like every animation); clicks behind it are swallowed while it is open.
    /// </summary>
    public sealed class EventPanelView : MonoBehaviour
    {
        private const int Width = 256;
        private const int Height = 132;
        private const int Portrait = 48;

        private AnimationSequencer _sequencer;
        private DealerAnimationLibrary _dealers;
        private RectTransform _panel;
        private GameObject _shade;
        private Image _portrait;
        private Text _owner;
        private Text _title;
        private Text _text;
        private RectTransform _buttons;
        private readonly List<Button> _built = new List<Button>();
        private Action<int> _pressed;
        private RectTransform _curtainTop;
        private RectTransform _curtainBottom;
        private int _x, _y;

        /// <summary>The card on show (for tests and screenshots); null when closed.</summary>
        public EventCard Card { get; private set; }

        public bool IsOpen => _shade.activeSelf;

        public static EventPanelView Create(Transform screen, int x, int y, AnimationSequencer sequencer, DealerAnimationLibrary dealers,
            Action<int> pressed)
        {
            // A clear layer over the whole screen swallows clicks while an offer waits.
            Image shade = UiFactory.CreateImage("EventShade", screen, Color.clear);
            shade.rectTransform.Stretch();
            shade.raycastTarget = true;
            var view = shade.gameObject.AddComponent<EventPanelView>();
            view._shade = shade.gameObject;
            view._sequencer = sequencer;
            view._dealers = dealers;
            view._pressed = pressed;
            view.Build(shade.transform, x, y);
            view._shade.SetActive(false);
            return view;
        }

        private void Build(Transform shade, int x, int y)
        {
            Image panel = UiFactory.CreatePanel("EventPanel", shade, hot: true);
            panel.raycastTarget = true;
            _panel = panel.rectTransform;
            _panel.PlaceTL(x, y, Width, Height);

            Image frame = UiFactory.CreateImage("Frame", panel.transform, Palette.Black);
            frame.rectTransform.PlaceTL(8, 8, Portrait + 4, Portrait + 4);
            UiFactory.AddBorder(frame.gameObject, Palette.Gold, 1f);
            _portrait = UiFactory.CreateImage("Portrait", frame.transform, Color.white);
            _portrait.raycastTarget = false;
            _portrait.rectTransform.PlaceTL(2, 2, Portrait, Portrait);

            _owner = UiFactory.CreateText("Owner", panel.transform, "", 8, Palette.Ember, TextAnchor.UpperLeft);
            _owner.rectTransform.PlaceTL(Portrait + 18, 9, Width - Portrait - 26, 9);
            _title = UiFactory.CreateText("Title", panel.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithOutline();
            _title.rectTransform.PlaceTL(Portrait + 18, 21, Width - Portrait - 26, 8);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text = UiFactory.CreateText("Text", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _text.rectTransform.PlaceTL(Portrait + 18, 33, Width - Portrait - 26, 66);
            _text.lineSpacing = 1f;

            _buttons = UiFactory.CreateRect("Buttons", panel.transform).PlaceTL(0, Height - 26, Width, 18);

            // The curtain: two black halves over the panel that draw back from its middle.
            _x = x;
            _y = y;
            _curtainTop = UiFactory.CreateImage("CurtainTop", shade, Palette.Black).rectTransform;
            _curtainBottom = UiFactory.CreateImage("CurtainBottom", shade, Palette.Black).rectTransform;
            SetOpening(Height);
        }

        public void Show(EventCard card)
        {
            if (card == null) return;
            _sequencer.Play(Open(card));
        }

        public void Hide()
        {
            _sequencer.Do(() =>
            {
                Card = null;
                _shade.SetActive(false);
            });
        }

        private IEnumerator Open(EventCard card)
        {
            bool reopening = IsOpen && Card != null && Card.EventId == card.EventId;
            Card = card;
            _portrait.sprite = PortraitOf(card.OwnerId);
            _portrait.enabled = _portrait.sprite != null;
            _owner.text = card.OwnerName;
            _title.text = card.Title;
            _text.text = card.Text;
            BuildButtons(card.Options);

            _shade.SetActive(true);
            _shade.transform.SetAsLastSibling();
            if (reopening) yield break;   // a language change: the same offer, new words, no curtain

            // The curtain: the panel opens from its middle, 8 px a step.
            for (int open = 0; open < Height; open += 16)
            {
                SetOpening(open);
                yield return Tween.Wait(0.02f);
            }
            SetOpening(Height);
        }

        /// <summary>How much of the panel shows, from its middle; the curtain halves cover the rest.</summary>
        private void SetOpening(int open)
        {
            int half = Mathf.Max(0, (Height - open) / 2);
            _curtainTop.PlaceTL(_x, _y, Width, half);
            _curtainBottom.PlaceTL(_x, _y + Height - half, Width, half);
            _curtainTop.gameObject.SetActive(half > 0);
            _curtainBottom.gameObject.SetActive(half > 0);
            _curtainTop.SetAsLastSibling();
            _curtainBottom.SetAsLastSibling();
        }

        private void BuildButtons(IReadOnlyList<string> options)
        {
            foreach (Button old in _built)
                if (old != null) Destroy(old.gameObject);
            _built.Clear();

            const int buttonWidth = 104;
            int total = options.Count * buttonWidth + (options.Count - 1) * 8;
            int left = (Width - total) / 2;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                bool pass = i == options.Count - 1;
                Button button = UiFactory.CreateButton("EventOption" + i, _buttons, options[i], 8, out _, pass ? ButtonSkin.Ash : ButtonSkin.Ember);
                ((RectTransform)button.transform).PlaceTL(left + i * (buttonWidth + 8), 0, buttonWidth, 18);
                button.onClick.AddListener(() => _pressed?.Invoke(index));
                _built.Add(button);
            }
        }

        private Sprite PortraitOf(string ownerId)
        {
            Sprite own = UiArt.Sprite("Events/" + ownerId);
            if (own != null) return own;
            SpriteClip clip = _dealers?.Get(ownerId, DealerAnimation.Idle);
            return clip != null && clip.Frames != null && clip.Frames.Length > 0 ? clip.Frames[0] : null;
        }
    }
}
