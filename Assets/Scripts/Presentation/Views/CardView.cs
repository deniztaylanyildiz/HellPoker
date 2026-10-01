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
        private Image _face;
        private GameObject _faceGroup;
        private GameObject _backGroup;
        private Text _cornerTop;
        private Text _cornerBottom;
        private Text _centerSuit;
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

            _content = UiFactory.CreateRect("Content", root).Stretch();
            _contentGroup = _content.gameObject.AddComponent<CanvasGroup>();
            _contentGroup.blocksRaycasts = false;

            _face = UiFactory.CreateImage("Face", _content, Palette.Bone);
            _face.rectTransform.Stretch();
            UiFactory.AddBorder(_face.gameObject, Palette.Ink, 2f);

            _faceGroup = UiFactory.CreateRect("FaceGroup", _content).Stretch().gameObject;
            int cornerSize = Mathf.RoundToInt(size.y * 0.15f);
            _cornerTop = UiFactory.CreateText("CornerTop", _faceGroup.transform, "", cornerSize, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            _cornerTop.rectTransform.Stretch(10f);
            _cornerTop.lineSpacing = 0.8f;
            _cornerBottom = UiFactory.CreateText("CornerBottom", _faceGroup.transform, "", cornerSize, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            _cornerBottom.rectTransform.Stretch(10f);
            _cornerBottom.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            _cornerBottom.lineSpacing = 0.8f;
            _centerSuit = UiFactory.CreateText("Suit", _faceGroup.transform, "", Mathf.RoundToInt(size.y * 0.45f), Palette.Ink);
            _centerSuit.rectTransform.Stretch();

            _backGroup = UiFactory.CreateRect("Back", _content).Stretch().gameObject;
            UiFactory.CreateImage("BackFill", _backGroup.transform, Palette.CardBack).rectTransform.Stretch();
            Image inner = UiFactory.CreateImage("BackInner", _backGroup.transform, Palette.CardBackInner);
            inner.rectTransform.Stretch(12f);
            UiFactory.AddBorder(inner.gameObject, Palette.Ember, 2f);
            UiFactory.CreateText("Mark", _backGroup.transform, "666", Mathf.RoundToInt(size.y * 0.17f), Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Stretch();

            Image tag = UiFactory.CreateImage("DiscardTag", _content, new Color(0.25f, 0.015f, 0.015f, 0.93f));
            tag.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x, size.y * 0.2f));
            UiFactory.CreateText("Label", tag.transform, UiText.DiscardTag, Mathf.RoundToInt(size.y * 0.09f), Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Stretch();
            _discardTag = tag.gameObject;

            Apply(CardSlot.Empty);
            SetSelected(false);
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
                _content.anchoredPosition = new Vector2(0f, Mathf.Lerp(DealDistance, 0f, t));
                _contentGroup.alpha = t;
            });
        }

        private IEnumerator FlipTo(CardSlot target)
        {
            yield return Tween.Run(HalfFlipDuration, t => _content.localScale = new Vector3(1f - t, 1f, 1f));
            Apply(target);
            yield return Tween.Run(HalfFlipDuration, t => _content.localScale = new Vector3(t, 1f, 1f));
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
            _content.anchoredPosition = Vector2.zero;
            _contentGroup.alpha = 1f;
            _discardTag.SetActive(false);

            switch (slot.Kind)
            {
                case CardSlot.SlotKind.Empty:
                    _face.color = Palette.Slot;
                    _faceGroup.SetActive(false);
                    _backGroup.SetActive(false);
                    break;

                case CardSlot.SlotKind.Back:
                    _face.color = Palette.Bone;
                    _faceGroup.SetActive(false);
                    _backGroup.SetActive(true);
                    break;

                default:
                    Card card = slot.Card;
                    _face.color = Palette.Bone;
                    _backGroup.SetActive(false);
                    _faceGroup.SetActive(true);

                    Color ink = card.Suit.IsBlack() ? Palette.Ink : Palette.Blood;
                    string corner = card.Rank.ToShortString() + "\n" + card.Suit.ToSymbol();
                    _cornerTop.text = corner;
                    _cornerBottom.text = corner;
                    _centerSuit.text = card.Suit.ToSymbol().ToString();
                    _cornerTop.color = ink;
                    _cornerBottom.color = ink;
                    _centerSuit.color = ink;
                    break;
            }
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
