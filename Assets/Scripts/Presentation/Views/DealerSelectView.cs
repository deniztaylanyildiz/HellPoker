using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// "Choose your dealer": a row of demons breathing in their boxes, each with a SIT DOWN button. Clicking a portrait
    /// shows that demon's description and house rules in the panel below. Built on <see cref="Show"/>, so it adapts to
    /// however many demons the roster holds.
    /// </summary>
    public sealed class DealerSelectView : MonoBehaviour, IDealerSelectView
    {
        private const int SortingOrder = 110;
        private const int CardWidth = 148;
        private const int CardSpacing = 8;
        private const int CardsY = 30;

        private Canvas _canvas;
        private DealerAnimationLibrary _library;
        private RectTransform _cards;
        private Text _detailTitle;
        private Text _description;
        private Text _traitsLeft;
        private Text _traitsRight;
        private readonly List<(Image box, SpriteFrameAnimator portrait)> _built = new List<(Image, SpriteFrameAnimator)>();
        private IReadOnlyList<DealerCard> _dealers;

        public event Action<int> DealerChosen;
        public event Action BackPressed;

        public bool IsVisible => _canvas.enabled;

        public static DealerSelectView Create(Transform parent, DealerAnimationLibrary library)
        {
            Canvas canvas = UiFactory.CreateScreen("DealerSelectCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<DealerSelectView>();
            view._canvas = canvas;
            view._library = library;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            Image background = UiFactory.CreateSprite("Background", screen, UiArt.Background, Palette.Night);
            background.rectTransform.Stretch();
            background.raycastTarget = true;

            UiFactory.CreateText("Title", screen, UiText.ChooseDealerTitle, 16, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 4, PixelScreen.Width, 16);
            UiFactory.CreateText("Subtitle", screen, UiText.ChooseDealerSubtitle, 8, Palette.BoneMid).WithShadow()
                .rectTransform.PlaceTL(0, 20, PixelScreen.Width, 9);

            _cards = UiFactory.CreateRect("Cards", screen).Stretch();

            Image details = UiFactory.CreatePanel("Details", screen);
            details.rectTransform.PlaceTL(8, 186, PixelScreen.Width - 16, 78);
            _detailTitle = UiFactory.CreateText("Name", details.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithShadow();
            _detailTitle.rectTransform.PlaceTL(8, 6, 300, 8);
            _description = UiFactory.CreateText("Description", details.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _description.rectTransform.PlaceTL(8, 16, PixelScreen.Width - 32, 18);
            _traitsLeft = UiFactory.CreateText("TraitsLeft", details.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            _traitsLeft.rectTransform.PlaceTL(8, 38, 216, 36);
            _traitsRight = UiFactory.CreateText("TraitsRight", details.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            _traitsRight.rectTransform.PlaceTL(232, 38, 224, 36);

            Button back = UiFactory.CreateButton("DealerBackButton", screen, UiText.Back, 8, out _, ButtonSkin.Ash);
            ((RectTransform)back.transform).PlaceTL(8, 4, 56, 16);
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        public void Show(IReadOnlyList<DealerCard> dealers)
        {
            foreach (Transform child in _cards)
                Destroy(child.gameObject);
            _built.Clear();
            _dealers = dealers;

            int total = dealers.Count * CardWidth + (dealers.Count - 1) * CardSpacing;
            int left = (PixelScreen.Width - total) / 2;
            for (int i = 0; i < dealers.Count; i++)
                BuildCard(dealers[i], i, left + i * (CardWidth + CardSpacing));

            Select(0);
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildCard(DealerCard dealer, int index, int x)
        {
            RectTransform card = UiFactory.CreateRect($"Dealer_{dealer.Id}", _cards).PlaceTL(x, CardsY, CardWidth, 152);

            int boxX = (CardWidth - (DealerView.PortraitSize + 8)) / 2;
            SpriteFrameAnimator portrait = DealerView.CreatePortrait(card, boxX, 0);
            portrait.Play(_library.Get(dealer.Id, DealerAnimation.Idle));
            Image box = portrait.transform.parent.GetComponent<Image>();

            // Clicking the portrait shows this demon's rules below.
            var pick = box.gameObject.AddComponent<Button>();
            box.raycastTarget = true;
            pick.targetGraphic = box;
            pick.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(pick);
            pick.onClick.AddListener(() => Select(index));

            UiFactory.CreateText("Name", card, dealer.Name, 8, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 108, CardWidth, 8);
            UiFactory.CreateText("Title", card, dealer.Title, 8, Palette.BoneMid).rectTransform.PlaceTL(0, 118, CardWidth, 9);

            Button choose = UiFactory.CreateButton($"ChooseDealer{index}", card, UiText.Challenge, 8, out _, ButtonSkin.Ember);
            ((RectTransform)choose.transform).PlaceTL((CardWidth - 88) / 2, 132, 88, 18);
            choose.onClick.AddListener(() => DealerChosen?.Invoke(index));

            _built.Add((box, portrait));
        }

        private void Select(int index)
        {
            if (_dealers == null || index < 0 || index >= _dealers.Count) return;

            for (int i = 0; i < _built.Count; i++)
            {
                Sprite sprite = UiArt.Sprite(i == index ? UiArt.PanelHot : UiArt.Panel);
                if (sprite != null)
                    _built[i].box.sprite = sprite;
            }

            DealerCard dealer = _dealers[index];
            _detailTitle.text = dealer.Name;
            _description.text = dealer.Description;

            int half = (dealer.Traits.Count + 1) / 2;
            var left = new List<string>();
            var right = new List<string>();
            for (int i = 0; i < dealer.Traits.Count; i++)
                (i < half ? left : right).Add("· " + dealer.Traits[i]);
            _traitsLeft.text = string.Join("\n", left);
            _traitsRight.text = string.Join("\n", right);
        }
    }
}
