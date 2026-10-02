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
    /// "Choose your dealer": a row of demons breathing in their boxes, each with their soul line and a SIT DOWN button.
    /// Clicking a portrait shows that demon's description and house rules in the panel below. When the player changes
    /// tables, each card also says whether sitting there is safe or puts the soul on the table, and the current demon's
    /// button reads RETURN. Built on <see cref="Show"/>, so it adapts to however many demons the roster holds.
    /// </summary>
    public sealed class DealerSelectView : MonoBehaviour, IDealerSelectView
    {
        private const int SortingOrder = 110;
        private const int MaxCardWidth = 148;
        private const int CardSpacing = 8;
        private int _cardWidth = MaxCardWidth;
        private const int CardsY = 30;
        private const int ConfirmWidth = 248;
        private const int ConfirmHeight = 88;

        private Canvas _canvas;
        private DealerAnimationLibrary _library;
        private SalonView _salon;
        private RectTransform _cards;
        private Text _subtitle;
        private Text _detailTitle;
        private Text _description;
        private Text _traitsLeft;
        private Text _traitsRight;
        private GameObject _confirm;
        private Text _warning;
        private readonly List<(Image box, SpriteFrameAnimator portrait)> _built = new List<(Image, SpriteFrameAnimator)>();
        private IReadOnlyList<DealerChoice> _dealers;

        public event Action<int> DealerChosen;
        public event Action BackPressed;
        public event Action SeatConfirmed;
        public event Action SeatCancelled;

        public bool IsVisible => _canvas.enabled;

        /// <summary>The highlighted card (its details below, its hall behind).</summary>
        public int SelectedIndex { get; private set; }

        /// <summary>True while the "sit anyway?" warning is open.</summary>
        public bool IsConfirming => _confirm.activeSelf;

        public static DealerSelectView Create(Transform parent, DealerAnimationLibrary library, SalonLibrary salons)
        {
            Canvas canvas = UiFactory.CreateScreen("DealerSelectCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<DealerSelectView>();
            view._canvas = canvas;
            view._library = library;
            view.Build(screen, salons);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen, SalonLibrary salons)
        {
            // The selected demon's hall shows behind the choice.
            _salon = SalonView.Create(screen, salons);

            UiFactory.CreateText("Title", screen, UiText.ChooseDealerTitle, 16, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 4, PixelScreen.Width, 16);
            _subtitle = UiFactory.CreateText("Subtitle", screen, UiText.ChooseDealerSubtitle, 8, Palette.BoneMid).WithShadow();
            _subtitle.rectTransform.PlaceTL(0, 20, PixelScreen.Width, 9);

            _cards = UiFactory.CreateRect("Cards", screen).Stretch();

            Image details = UiFactory.CreatePanel("Details", screen);
            details.rectTransform.PlaceTL(8, 190, PixelScreen.Width - 16, 76);
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

            BuildConfirm(screen);
        }

        /// <summary>The "your soul will be on her table" warning: an invisible layer that swallows clicks, and a box.</summary>
        private void BuildConfirm(RectTransform screen)
        {
            Image shade = UiFactory.CreateImage("ConfirmSeat", screen, Color.clear);
            shade.rectTransform.Stretch();
            _confirm = shade.gameObject;

            Image box = UiFactory.CreatePanel("ConfirmBox", shade.transform, hot: true);
            box.rectTransform.PlaceTL((PixelScreen.Width - ConfirmWidth) / 2, (PixelScreen.Height - ConfirmHeight) / 2, ConfirmWidth, ConfirmHeight);

            _warning = UiFactory.CreateText("Warning", box.transform, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithShadow();
            _warning.rectTransform.PlaceTL(8, 8, ConfirmWidth - 16, 44);

            Button sit = UiFactory.CreateButton("ConfirmSeatButton", box.transform, UiText.SitAnyway, 8, out _, ButtonSkin.Blood);
            ((RectTransform)sit.transform).PlaceTL(16, ConfirmHeight - 28, 104, 18);
            sit.onClick.AddListener(() =>
            {
                _confirm.SetActive(false);
                SeatConfirmed?.Invoke();
            });

            Button cancel = UiFactory.CreateButton("CancelSeatButton", box.transform, UiText.Back, 8, out _, ButtonSkin.Ash);
            ((RectTransform)cancel.transform).PlaceTL(ConfirmWidth - 16 - 104, ConfirmHeight - 28, 104, 18);
            cancel.onClick.AddListener(() =>
            {
                _confirm.SetActive(false);
                SeatCancelled?.Invoke();
            });

            _confirm.SetActive(false);
        }

        public void Show(IReadOnlyList<DealerChoice> dealers)
        {
            // Old cards go dark at once (Destroy only happens at the end of the frame).
            foreach (Transform child in _cards)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _built.Clear();
            _dealers = dealers;
            _confirm.SetActive(false);

            bool changingTables = false;
            int current = 0;
            for (int i = 0; i < dealers.Count; i++)
            {
                if (!dealers[i].IsCurrent) continue;
                changingTables = true;
                current = i;
            }
            _subtitle.text = changingTables ? UiText.ChooseTableSubtitle : UiText.ChooseDealerSubtitle;

            // As wide as the row allows (four demons fit in 110 px cards), never narrower than a portrait box.
            _cardWidth = Mathf.Clamp((PixelScreen.Width - 16 - (dealers.Count - 1) * CardSpacing) / Mathf.Max(1, dealers.Count),
                DealerView.PortraitSize + 8, MaxCardWidth);
            int total = dealers.Count * _cardWidth + (dealers.Count - 1) * CardSpacing;
            int left = (PixelScreen.Width - total) / 2;
            for (int i = 0; i < dealers.Count; i++)
                BuildCard(dealers[i], i, left + i * (_cardWidth + CardSpacing), changingTables);

            Select(current, wipe: false);   // the hall behind always matches the highlight from the first frame
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void AskToConfirm(string warning)
        {
            _warning.text = warning ?? "";
            _confirm.SetActive(true);
            _confirm.transform.SetAsLastSibling();
        }

        public void CloseConfirm()
        {
            _confirm.SetActive(false);
        }

        /// <summary>The locked demon is brought forward and answers from the dark, in the details panel.</summary>
        public void ShowLockedLine(int index, string line)
        {
            Select(index, wipe: true);
            _description.text = line ?? "";
            _description.color = Palette.Ember;
            LastLockedLine = line;
        }

        /// <summary>What the locked demon said last (for tests).</summary>
        public string LastLockedLine { get; private set; }

        public void Hide()
        {
            _confirm.SetActive(false);
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildCard(DealerChoice choice, int index, int x, bool changingTables)
        {
            DealerCard dealer = choice.Card;
            int width = _cardWidth;
            RectTransform card = UiFactory.CreateRect($"Dealer_{dealer.Id}", _cards).PlaceTL(x, CardsY, width, 152);

            int boxX = (width - (DealerView.PortraitSize + 8)) / 2;
            SpriteFrameAnimator portrait = DealerView.CreatePortrait(card, boxX, 0);
            portrait.Play(_library.Get(dealer.Id, choice.SoulAtStake ? DealerAnimation.Soul : DealerAnimation.Idle));
            Image box = portrait.transform.parent.GetComponent<Image>();

            // Clicking the portrait shows this demon's rules below.
            var pick = box.gameObject.AddComponent<Button>();
            box.raycastTarget = true;
            pick.targetGraphic = box;
            pick.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(pick);
            pick.onClick.AddListener(() => Select(index, wipe: true));

            // While changing tables: is the soul safe at this table? (Not asked of the locked one.)
            if (changingTables && !choice.IsLocked)
            {
                Text status = UiFactory.CreateText("SoulStatus", box.transform, choice.SoulAtStake ? UiText.SoulAtStake : UiText.Safe, 8,
                    choice.SoulAtStake ? Palette.Hell : Palette.GreenLight, TextAnchor.MiddleCenter).WithShadow();
                status.rectTransform.PlaceTL(0, DealerView.PortraitSize - 7, DealerView.PortraitSize + 8, 9);
                status.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            // The locked one's long name ("THE MORNING STAR") takes the small pixel font, so it fits a narrow card.
            Text name = UiFactory.CreateText("Name", card, dealer.Name, 8, choice.IsLocked ? Palette.Hell : Palette.GoldLight,
                style: choice.IsLocked ? FontStyle.Normal : FontStyle.Bold).WithShadow();
            name.rectTransform.PlaceTL(-8, 106, width + 16, 8);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Narrow cards give the title two lines.
            Text title = UiFactory.CreateText("Title", card, dealer.Title, 8, Palette.BoneMid, TextAnchor.UpperCenter);
            title.rectTransform.PlaceTL(0, 115, width, 17);
            if (dealer.SoulThreshold > 0 && !choice.IsLocked)
                UiFactory.CreateText("SoulLine", card, string.Format(UiText.SoulLineCardFormat, dealer.SoulThreshold), 8, Palette.LilacLight)
                    .rectTransform.PlaceTL(0, 131, width, 9);

            string label = choice.IsLocked ? UiText.Locked : choice.IsCurrent ? UiText.ReturnToTable : UiText.Challenge;
            ButtonSkin skin = choice.IsLocked ? ButtonSkin.Ash : choice.SoulAtStake ? ButtonSkin.Blood : ButtonSkin.Ember;
            Button choose = UiFactory.CreateButton($"ChooseDealer{index}", card, label, 8, out _, skin);
            ((RectTransform)choose.transform).PlaceTL((width - 88) / 2, 141, 88, 18);
            choose.onClick.AddListener(() => DealerChosen?.Invoke(index));
            if (choice.IsLocked)
                choose.GetComponent<ButtonFeel>().Locked = true;   // looks locked; a click still gets his answer

            _built.Add((box, portrait));
        }

        private void Select(int index, bool wipe)
        {
            if (_dealers == null || index < 0 || index >= _dealers.Count) return;
            SelectedIndex = index;

            for (int i = 0; i < _built.Count; i++)
            {
                Sprite sprite = UiArt.Sprite(i == index ? UiArt.PanelHot : UiArt.Panel);
                if (sprite != null)
                    _built[i].box.sprite = sprite;
            }

            DealerCard dealer = _dealers[index].Card;
            _salon.SetSalon(dealer.Id, _dealers[index].SoulAtStake ? SalonMode.Soul : SalonMode.Normal, wipe);
            _detailTitle.text = dealer.Name;
            _detailTitle.color = _dealers[index].IsLocked ? Palette.Hell : Palette.GoldLight;
            _description.text = dealer.Description;
            _description.color = Palette.Bone;

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
