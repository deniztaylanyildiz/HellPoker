using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// "Choose your dealer": one framed card per demon with portrait, name, description, house rules and a button.
    /// Cards are built on <see cref="Show"/>, so the screen adapts to however many demons the roster holds.
    /// </summary>
    public sealed class DealerSelectView : MonoBehaviour, IDealerSelectView
    {
        private const int SortingOrder = 110;
        private static readonly Vector2 CardSize = new Vector2(500f, 820f);
        private const float CardSpacing = 60f;

        private Canvas _canvas;
        private RectTransform _cards;
        private readonly List<RectTransform> _builtCards = new List<RectTransform>();
        private float _shownAt;

        public event Action<int> DealerChosen;
        public event Action BackPressed;

        public bool IsVisible => _canvas.enabled;

        public static DealerSelectView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateCanvas("DealerSelectCanvas", parent, SortingOrder);
            var view = canvas.gameObject.AddComponent<DealerSelectView>();
            view._canvas = canvas;
            view.Build(canvas.transform);
            view.Hide();
            return view;
        }

        private void Build(Transform root)
        {
            Image background = UiFactory.CreateSprite("Background", root, UiArt.Background, Palette.Background);
            background.rectTransform.Stretch();
            background.raycastTarget = true;
            UiFactory.CreateImage("Shade", root, new Color(0f, 0f, 0f, 0.35f)).rectTransform.Stretch();

            UiFactory.CreateText("Title", root, UiText.ChooseDealerTitle, 54, Palette.Gold, style: FontStyle.Bold).WithShadow(3f)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1600f, 70f));
            UiFactory.CreateText("Subtitle", root, UiText.ChooseDealerSubtitle, 26, Palette.MutedText, style: FontStyle.Italic).WithShadow()
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(1600f, 36f));

            _cards = UiFactory.CreateRect("Cards", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1800f, CardSize.y));

            Button back = UiFactory.CreateButton("DealerBackButton", root, UiText.Back, 24, out _, ButtonSkin.Ash);
            ((RectTransform)back.transform).Place(new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(180f, 58f), new Vector2(0f, 0f));
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        public void Show(IReadOnlyList<DealerCard> dealers)
        {
            foreach (RectTransform card in _builtCards)
                Destroy(card.gameObject);
            _builtCards.Clear();

            float totalWidth = dealers.Count * CardSize.x + (dealers.Count - 1) * CardSpacing;
            for (int i = 0; i < dealers.Count; i++)
            {
                float x = -totalWidth / 2f + CardSize.x / 2f + i * (CardSize.x + CardSpacing);
                _builtCards.Add(BuildCard(dealers[i], i, x));
            }

            _shownAt = Time.unscaledTime;
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private RectTransform BuildCard(DealerCard dealer, int index, float x)
        {
            Image card = UiFactory.CreatePanel($"Dealer_{dealer.Id}", _cards);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(x, 0f), CardSize);
            Transform t = card.transform;

            var portraitSize = new Vector2(272f, 340f);
            const float portraitY = -40f;
            Sprite sprite = UiArt.Portrait(dealer.Id);
            Image portrait = UiFactory.CreateImage("Portrait", t, sprite != null ? Color.white : Palette.Slot);
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, portraitY), portraitSize, new Vector2(0.5f, 1f));
            UiFactory.CreateFrame("Frame", t, 0.75f).rectTransform
                .Place(new Vector2(0.5f, 1f), new Vector2(0f, portraitY + 11f), portraitSize + new Vector2(22f, 22f), new Vector2(0.5f, 1f));

            float y = portraitY - portraitSize.y - 40f;
            UiFactory.CreateText("Name", t, dealer.Name, 42, Palette.Gold, style: FontStyle.Bold).WithShadow(3f)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(CardSize.x, 50f));
            UiFactory.CreateText("Title", t, dealer.Title, 23, Palette.MutedText, style: FontStyle.Italic)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y - 36f), new Vector2(CardSize.x, 30f));
            UiFactory.CreateText("Description", t, dealer.Description, 20, Palette.Bone, TextAnchor.UpperCenter)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y - 60f), new Vector2(CardSize.x - 60f, 80f), new Vector2(0.5f, 1f));
            UiFactory.CreateSprite("Divider", t, UiArt.Divider)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y - 146f), new Vector2(320f, 24f));

            Text traits = UiFactory.CreateText("Traits", t, "• " + string.Join("\n• ", dealer.Traits), 18, Palette.PaleGold, TextAnchor.UpperLeft);
            traits.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y - 162f), new Vector2(CardSize.x - 50f, 150f), new Vector2(0.5f, 1f));
            traits.lineSpacing = 1f;

            Button choose = UiFactory.CreateButton($"ChooseDealer{index}", t, UiText.Challenge, 28, out _, ButtonSkin.Ember);
            ((RectTransform)choose.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(280f, 70f));
            choose.onClick.AddListener(() => DealerChosen?.Invoke(index));

            card.gameObject.AddComponent<CanvasGroup>();
            return card.rectTransform;
        }

        private void Update()
        {
            if (!_canvas.enabled) return;

            // Cards rise into place one after another.
            for (int i = 0; i < _builtCards.Count; i++)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _shownAt - i * 0.12f) / 0.35f);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                RectTransform card = _builtCards[i];
                card.anchoredPosition = new Vector2(card.anchoredPosition.x, Mathf.Lerp(-60f, 0f, ease));
                card.GetComponent<CanvasGroup>().alpha = ease;
            }
        }
    }
}
