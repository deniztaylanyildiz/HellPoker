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
        private Image _hint;
        private bool _bright;
        private Image _pick;
        private PickState _picking = PickState.None;
        private static readonly Color DimTint = new Color(0.45f, 0.45f, 0.45f);

        /// <summary>A power picking a card: this one may be taken (a blinking gold frame), or may not (dimmed).</summary>
        public enum PickState
        {
            None,
            Pickable,
            Dimmed
        }
        private Image _mark;
        private float _sheen;

        /// <summary>The state this card will be in once all queued animations have played.</summary>
        public CardSlot Planned { get; private set; } = CardSlot.Empty;

        /// <summary>True when the face is on screen right now (not just planned).</summary>
        public bool IsFaceUp => _content.gameObject.activeSelf && _faceGroup.activeSelf;

        /// <summary>The card whose face is on screen right now; null when none is (for tests: what the player can see).</summary>
        public Card? FaceShown => IsFaceUp ? _faceCard : (Card?)null;

        private Card _faceCard;

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

            // A power picking: a two-pixel gold frame round a card it may take, blinking (no blending).
            _pick = UiFactory.CreateImage("PickFrame", root, Palette.GoldLight);
            _pick.raycastTarget = false;
            _pick.rectTransform.PlaceTL(-2, -2, Size.x + 4, Size.y + 4);
            _pick.enabled = false;

            // The keep hint: a gold frame one pixel round the card, behind it, glinting slowly (no blending).
            _hint = UiFactory.CreateImage("KeepHint", root, Palette.Gold);
            _hint.raycastTarget = false;
            _hint.rectTransform.PlaceTL(-1, -1, Size.x + 2, Size.y + 2);
            _hint.enabled = false;

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
            UiFactory.CreateText("Label", tag.transform, "", 8, Palette.Ember, style: FontStyle.Bold).Localized(() => UiText.DiscardTag)
                .rectTransform.Stretch();
            _discardTag = tag.gameObject;

            // A cheat's mark lies over the card (chain, thorn, veil, silver sheen); it turns with the card.
            _mark = UiFactory.CreateImage("CheatMark", _content, Color.white);
            _mark.raycastTarget = false;
            _mark.rectTransform.Stretch();
            _mark.enabled = false;

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

            // Only the mark changed: no flip, it simply appears (or goes).
            if (from.SameAs(target))
                return Sequence(MarkNow(target.Mark));
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

        private IEnumerator MarkNow(CardMark mark)
        {
            ShowMark(mark);
            yield break;
        }

        /// <summary>The mark's overlay from the card_marks strip; nothing when the art is missing or there is no mark.</summary>
        private void ShowMark(CardMark mark)
        {
            int frame = mark == CardMark.Chained ? 0 : mark == CardMark.Thorned ? 1 : mark == CardMark.Veiled ? 2 : mark == CardMark.FalseFace ? 3 : mark == CardMark.Protected ? 4 : -1;
            Sprite[] marks = UiArt.Strip(UiArt.CardMarks, Size.x);
            _mark.sprite = frame >= 0 && marks != null && frame < marks.Length ? marks[frame] : null;
            _mark.enabled = _mark.sprite != null;
            _mark.transform.SetAsLastSibling();
            _sheen = mark == CardMark.FalseFace ? 1f : 0f;
        }

        /// <summary>Shakes the card sideways by whole pixels (a cheat's blow); 0 puts it back.</summary>
        public void Nudge(int pixels)
        {
            _content.anchoredPosition = new Vector2(pixels, Lift);
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
            ShowMark(slot.Mark);
            if (slot.Kind != CardSlot.SlotKind.Face) return;

            Card card = slot.Card;
            _faceCard = card;
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

        /// <summary>Marks this card as one the House's logic would keep; <paramref name="bright"/> for a full glow.</summary>
        public void SetHint(bool keep, bool bright = false)
        {
            _hint.enabled = keep;
            _bright = bright;
        }

        public bool IsHinted => _hint.enabled;

        /// <summary>A power picking a card: pickable (framed, blinking), dimmed, or neither.</summary>
        public void SetPick(PickState state)
        {
            _picking = state;
            _pick.enabled = state == PickState.Pickable;
            // Dimmed by tinting what the card draws (as a locked button is), never by blending over it.
            Color tint = state == PickState.Dimmed ? DimTint : Color.white;
            foreach (Graphic graphic in _content.GetComponentsInChildren<Graphic>(true))
                graphic.canvasRenderer.SetColor(tint);
        }

        public PickState Picking => _picking;

        /// <summary>The mark the card carries (for tests and screenshots).</summary>
        public bool HasMark => _mark != null && _mark.sprite != null;

        private void Update()
        {
            // The false face's sheen comes and goes, very faintly (a glint, then nothing for a moment).
            if (_sheen > 0f && _mark.sprite != null)
                _mark.enabled = Mathf.Repeat(Time.unscaledTime, 1.6f) < 1.1f;
            if (_pick.enabled)
            {
                _pick.color = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f ? Palette.GoldLight : Palette.Gold;
                _pick.rectTransform.PlaceTL(-2, -2 - Mathf.RoundToInt(_content.anchoredPosition.y), Size.x + 4, Size.y + 4);
            }
            if (!_hint.enabled) return;
            _hint.color = _bright ? (Mathf.Repeat(Time.unscaledTime, 0.2f) < 0.1f ? Palette.Bone : Palette.GoldLight)
                : Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.6f ? Palette.Gold : Palette.GoldLight;
            // The frame follows the card when it is lifted for discarding.
            _hint.rectTransform.PlaceTL(-1, -1 - Mathf.RoundToInt(_content.anchoredPosition.y), Size.x + 2, Size.y + 2);
        }
    }
}
