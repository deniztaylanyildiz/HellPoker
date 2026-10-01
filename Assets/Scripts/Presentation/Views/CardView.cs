using System;
using System.Collections;
using HellPoker.Core.Cards;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>A single card slot: empty, face down, or face up, optionally marked for discard. Animates its changes.</summary>
    public sealed class CardView : MonoBehaviour
    {
        private const float SelectedLift = 28f;
        private const float DealDistance = 320f;
        private const float DealDuration = 0.16f;
        private const float HalfFlipDuration = 0.1f;
        private const float FadeDuration = 0.18f;

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

        /// <summary>The state this card will be in once all queued animations have played.</summary>
        public CardSlot Planned { get; private set; } = CardSlot.Empty;

        public event Action Clicked;

        public static CardView Create(Transform parent, Vector2 size)
        {
            RectTransform root = UiFactory.CreateRect("Card", parent);
            root.sizeDelta = size;
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            var view = root.gameObject.AddComponent<CardView>();
            view.Build(root, size);
            return view;
        }

        private void Build(RectTransform root, Vector2 size)
        {
            // Invisible hit area so the card stays clickable while lifted or empty.
            Image hitArea = UiFactory.CreateImage("HitArea", root, Color.clear);
            hitArea.rectTransform.Stretch();
            _button = root.gameObject.AddComponent<Button>();
            _button.targetGraphic = hitArea;
            _button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(_button);
            _button.onClick.AddListener(() => Clicked?.Invoke());

            _slot = UiFactory.CreateSprite("Slot", root, UiArt.CardSlot, Palette.Slot).gameObject;
            ((RectTransform)_slot.transform).Stretch();

            _content = UiFactory.CreateRect("Content", root).Stretch();
            _contentGroup = _content.gameObject.AddComponent<CanvasGroup>();
            _contentGroup.blocksRaycasts = false;

            // Soft shadow under the card, so it lifts off the felt.
            Image shadow = UiFactory.CreateSprite("Shadow", _content, UiArt.CardSlot, new Color(0f, 0f, 0f, 0.4f));
            shadow.color = new Color(0f, 0f, 0f, 0.55f);
            shadow.rectTransform.Stretch();
            shadow.rectTransform.anchoredPosition = new Vector2(5f, -7f);

            BuildFace(size);
            BuildBack();

            Image tag = UiFactory.CreateImage("DiscardTag", _content, new Color(0.2f, 0.01f, 0.01f, 0.92f));
            tag.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 8f, size.y * 0.2f));
            UiFactory.AddBorder(tag.gameObject, Palette.Gold, 1.5f);
            UiFactory.CreateText("Label", tag.transform, UiText.DiscardTag, Mathf.RoundToInt(size.y * 0.085f), Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Stretch();
            _discardTag = tag.gameObject;

            Apply(CardSlot.Empty);
            SetSelected(false);
        }

        private void BuildFace(Vector2 size)
        {
            Image face = UiFactory.CreateSprite("Face", _content, UiArt.CardFace, Palette.Bone);
            face.rectTransform.Stretch();
            if (face.sprite == null)
                UiFactory.AddBorder(face.gameObject, Palette.Ink, 2f);
            _faceGroup = face.gameObject;

            int rankSize = Mathf.RoundToInt(size.y * 0.15f);
            float pip = size.y * 0.1f;
            (_rankTop, _suitTop) = BuildCorner(face.transform, "CornerTop", rankSize, pip, size);
            (_rankBottom, _suitBottom) = BuildCorner(face.transform, "CornerBottom", rankSize, pip, size);
            _rankBottom.transform.parent.localRotation = Quaternion.Euler(0f, 0f, 180f);

            _centerSuit = UiFactory.CreateImage("Suit", face.transform, Color.white);
            _centerSuit.raycastTarget = false;
            _centerSuit.preserveAspect = true;
            _centerSuit.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size.x * 0.52f);

            // Text fallback when the suit sprites are missing.
            _centerSymbol = UiFactory.CreateText("SuitSymbol", face.transform, "", Mathf.RoundToInt(size.y * 0.42f), Palette.Ink);
            _centerSymbol.rectTransform.Stretch();
        }

        private static (Text rank, Image suit) BuildCorner(Transform face, string name, int rankSize, float pip, Vector2 size)
        {
            RectTransform corner = UiFactory.CreateRect(name, face).Stretch();
            Text rank = UiFactory.CreateText("Rank", corner, "", rankSize, Palette.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            rank.rectTransform.Place(new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(size.x * 0.24f, rankSize * 1.2f), new Vector2(0f, 1f));
            rank.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image suit = UiFactory.CreateImage("Pip", corner, Color.white);
            suit.raycastTarget = false;
            suit.preserveAspect = true;
            suit.rectTransform.Place(new Vector2(0f, 1f), new Vector2(14f + size.x * 0.12f, -14f - rankSize * 1.15f), Vector2.one * pip, new Vector2(0.5f, 1f));
            return (rank, suit);
        }

        private void BuildBack()
        {
            Image back = UiFactory.CreateSprite("Back", _content, UiArt.CardBack, Palette.CardBack);
            back.rectTransform.Stretch();
            _backGroup = back.gameObject;

            if (back.sprite == null)
            {
                Image inner = UiFactory.CreateImage("BackInner", back.transform, Palette.CardBackInner);
                inner.rectTransform.Stretch(12f);
                UiFactory.AddBorder(inner.gameObject, Palette.Ember, 2f);
            }
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
            float tilt = UnityEngine.Random.Range(-9f, 9f);
            yield return Tween.Run(DealDuration, t =>
            {
                _content.anchoredPosition = new Vector2(0f, Mathf.Lerp(DealDistance, 0f, t));
                _content.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(tilt, 0f, t));
                _contentGroup.alpha = t;
            });
            _content.localRotation = Quaternion.identity;
        }

        private IEnumerator FlipTo(CardSlot target)
        {
            yield return Tween.Run(HalfFlipDuration, t => _content.localScale = new Vector3(1f - t, 1f + 0.06f * t, 1f));
            Apply(target);
            yield return Tween.Run(HalfFlipDuration, t => _content.localScale = new Vector3(t, 1.06f - 0.06f * t, 1f));
        }

        private IEnumerator FadeOut()
        {
            if (_backGroup.activeSelf || _faceGroup.activeSelf)
                yield return Tween.Run(FadeDuration, t => _contentGroup.alpha = 1f - t);
            Apply(CardSlot.Empty);
        }

        private static IEnumerator Sequence(params IEnumerator[] steps)
        {
            foreach (IEnumerator step in steps)
                yield return step;
        }

        private void Apply(CardSlot slot)
        {
            _content.localScale = Vector3.one;
            _content.localRotation = Quaternion.identity;
            _content.anchoredPosition = Vector2.zero;
            _contentGroup.alpha = 1f;
            _discardTag.SetActive(false);

            bool empty = slot.Kind == CardSlot.SlotKind.Empty;
            _slot.SetActive(empty);
            _content.gameObject.SetActive(!empty);
            _backGroup.SetActive(slot.Kind == CardSlot.SlotKind.Back);
            _faceGroup.SetActive(slot.Kind == CardSlot.SlotKind.Face);
            if (slot.Kind != CardSlot.SlotKind.Face) return;

            Card card = slot.Card;
            Color ink = card.Suit.IsBlack() ? Palette.Ink : Palette.Blood;
            Sprite suit = UiArt.Suit(card.Suit);
            string rank = card.Rank.ToShortString();

            foreach (Text text in new[] { _rankTop, _rankBottom })
            {
                text.text = rank;
                text.color = ink;
            }

            foreach (Image image in new[] { _suitTop, _suitBottom, _centerSuit })
            {
                image.sprite = suit;
                image.color = ink;
                image.enabled = suit != null;
            }

            _centerSymbol.text = suit == null ? card.Suit.ToSymbol().ToString() : "";
            _centerSymbol.color = ink;
        }

        public void SetSelected(bool selected)
        {
            _content.anchoredPosition = new Vector2(0f, selected ? SelectedLift : 0f);
            _discardTag.SetActive(selected);
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }
    }
}
