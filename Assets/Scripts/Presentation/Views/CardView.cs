using System;
using System.Collections;
using HellPoker.Core.Cards;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A single 32×48 pixel card slot: empty, face down, or face up, optionally marked for discard. Animates its changes
    /// in whole-pixel steps (no half-pixel drift): it drops in from above, and flips by narrowing a pixel column at a time.
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        public static readonly Vector2Int Size = new Vector2Int(32, 48);

        private const int SelectedLift = 6;
        private const int DealDistance = 40;
        private const float DealDuration = 0.16f;
        private const float HalfFlipDuration = 0.08f;
        private const float FadeDuration = 0.12f;

        private RectTransform _content;
        private CanvasGroup _contentGroup;
        private GameObject _slot;
        private GameObject _faceGroup;
        private GameObject _backGroup;
        private Text _rankTop;
        private Text _rankBottom;
        private Image _suitTop;
        private Image _suitBottom;
        private Image _centerSuit;
        private Text _centerSymbol;
        private GameObject _discardTag;
        private Button _button;
        private bool _selected;

        /// <summary>The state this card will be in once all queued animations have played.</summary>
        public CardSlot Planned { get; private set; } = CardSlot.Empty;

        public event Action Clicked;

        public static CardView Create(Transform parent)
        {
            RectTransform root = UiFactory.CreateRect("Card", parent);
            root.sizeDelta = Size;
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = Size.x;
            layout.preferredHeight = Size.y;

            var view = root.gameObject.AddComponent<CardView>();
            view.Build(root);
            return view;
        }

        private void Build(RectTransform root)
        {
            // Invisible hit area so the card stays clickable while lifted or empty.
            Image hitArea = UiFactory.CreateImage("HitArea", root, Color.clear);
            hitArea.rectTransform.Stretch();
            _button = root.gameObject.AddComponent<Button>();
            _button.targetGraphic = hitArea;
            _button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(_button);
            _button.onClick.AddListener(() => Clicked?.Invoke());

            _slot = UiFactory.CreateSprite("Slot", root, UiArt.CardSlot, new Color(0f, 0f, 0f, 0f)).gameObject;
            ((RectTransform)_slot.transform).Stretch();

            _content = UiFactory.CreateRect("Content", root).Stretch();
            _contentGroup = _content.gameObject.AddComponent<CanvasGroup>();
            _contentGroup.blocksRaycasts = false;

            BuildFace();
            BuildBack();

            Image tag = UiFactory.CreateImage("DiscardTag", _content, Palette.BloodDark);
            tag.rectTransform.PlaceTL(0, 20, Size.x, 10);
            UiFactory.AddBorder(tag.gameObject, Palette.Black, 1f);
            UiFactory.CreateText("Label", tag.transform, UiText.DiscardTag, 8, Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Stretch();
            _discardTag = tag.gameObject;

            Apply(CardSlot.Empty);
            SetSelected(false);
        }

        private void BuildFace()
        {
            Image face = UiFactory.CreateSprite("Face", _content, UiArt.CardFace, Palette.Bone);
            face.rectTransform.Stretch();
            _faceGroup = face.gameObject;

            (_rankTop, _suitTop) = BuildCorner(face.transform, "CornerTop");
            (_rankBottom, _suitBottom) = BuildCorner(face.transform, "CornerBottom");
            // A 180° turn keeps every pixel on the grid.
            _rankBottom.transform.parent.localRotation = Quaternion.Euler(0f, 0f, 180f);

            _centerSuit = UiFactory.CreateImage("Suit", face.transform, Color.white);
            _centerSuit.raycastTarget = false;
            _centerSuit.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11, 11));

            // Text fallback when the suit sprites are missing.
            _centerSymbol = UiFactory.CreateText("SuitSymbol", face.transform, "", 16, Palette.Ink, style: FontStyle.Bold);
            _centerSymbol.rectTransform.Stretch();
        }

        private static (Text rank, Image suit) BuildCorner(Transform face, string name)
        {
            RectTransform corner = UiFactory.CreateRect(name, face).Stretch();
            Text rank = UiFactory.CreateText("Rank", corner, "", 8, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            rank.rectTransform.PlaceTL(3, 3, 20, 8);
            rank.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image suit = UiFactory.CreateImage("Pip", corner, Color.white);
            suit.raycastTarget = false;
            suit.rectTransform.PlaceTL(3, 12, 5, 5);
            return (rank, suit);
        }

        private void BuildBack()
        {
            Image back = UiFactory.CreateSprite("Back", _content, UiArt.CardBack, Palette.Crimson);
            back.rectTransform.Stretch();
            _backGroup = back.gameObject;
        }

        /// <summary>Records the new target state and returns the animation that gets there.</summary>
        public IEnumerator AnimateTo(CardSlot target)
        {
            CardSlot from = Planned;
            Planned = target;

            if (target.Kind == CardSlot.SlotKind.Empty)
                return FadeOut();
            if (from.Kind == CardSlot.SlotKind.Empty)
                return target.Kind == CardSlot.SlotKind.Back ? DealIn() : Sequence(DealIn(), FlipTo(target));
            if (from.Kind == CardSlot.SlotKind.Face && target.Kind == CardSlot.SlotKind.Face)
                return Sequence(FlipTo(CardSlot.Back), FlipTo(target));
            return FlipTo(target);
        }

        private IEnumerator DealIn()
        {
            Apply(CardSlot.Back);
            _contentGroup.alpha = 0f;
            yield return Tween.Run(DealDuration, t =>
            {
                _content.anchoredPosition = new Vector2(0f, Mathf.Round(Mathf.Lerp(DealDistance, 0f, t)));
                _contentGroup.alpha = t < 0.25f ? 0f : 1f;
            });
            _content.anchoredPosition = new Vector2(0f, Lift);
        }

        private IEnumerator FlipTo(CardSlot target)
        {
            // Narrow by whole pixel columns (width stays an even number, so the card stays centred on the grid).
            yield return Tween.Run(HalfFlipDuration, t => SetWidth(1f - t));
            Apply(target);
            yield return Tween.Run(HalfFlipDuration, SetWidth);
        }

        private void SetWidth(float share)
        {
            float pixels = Mathf.Round(Size.x * Mathf.Clamp01(share) / 2f) * 2f;
            _content.localScale = new Vector3(pixels / Size.x, 1f, 1f);
        }

        private IEnumerator FadeOut()
        {
            if (_backGroup.activeSelf || _faceGroup.activeSelf)
                yield return Tween.Run(FadeDuration, t => _contentGroup.alpha = t < 0.5f ? 1f : 0f);
            Apply(CardSlot.Empty);
        }

        private static IEnumerator Sequence(params IEnumerator[] steps)
        {
            foreach (IEnumerator step in steps)
                yield return step;
        }

        private float Lift => _selected ? SelectedLift : 0f;

        private void Apply(CardSlot slot)
        {
            _content.localScale = Vector3.one;
            _content.anchoredPosition = new Vector2(0f, Lift);
            _contentGroup.alpha = 1f;
            _discardTag.SetActive(false);

            bool empty = slot.Kind == CardSlot.SlotKind.Empty;
            _slot.SetActive(empty);
            _content.gameObject.SetActive(!empty);
            _backGroup.SetActive(slot.Kind == CardSlot.SlotKind.Back);
            _faceGroup.SetActive(slot.Kind == CardSlot.SlotKind.Face);
            if (slot.Kind != CardSlot.SlotKind.Face) return;

            Card card = slot.Card;
            Color ink = card.Suit.IsBlack() ? Palette.BlackSuit : Palette.RedSuit;
            string rank = card.Rank.ToShortString();

            foreach (Text text in new[] { _rankTop, _rankBottom })
            {
                text.text = rank;
                text.color = ink;
            }

            Sprite small = UiArt.Suit(card.Suit, small: true);
            Sprite big = UiArt.Suit(card.Suit, small: false);
            foreach (Image pip in new[] { _suitTop, _suitBottom })
            {
                pip.sprite = small;
                pip.enabled = small != null;
            }
            _centerSuit.sprite = big;
            _centerSuit.enabled = big != null;

            _centerSymbol.text = big == null ? card.Suit.ToSymbol().ToString() : "";
            _centerSymbol.color = ink;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            _content.anchoredPosition = new Vector2(0f, Lift);
            _discardTag.SetActive(selected);
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }
    }
}
