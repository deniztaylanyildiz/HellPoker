using System;
using System.Collections.Generic;
using HellPoker.Core.Relics;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.5: the one relic card every screen uses (the warden's relic, a treasure, the spoils, the black market). A frame in the rarity's
    /// colour (common bone, rare blood, legendary gold with corner pieces; a demon's own his colour), the big picture at 2× (64×64: a
    /// relic's, a class's portrait with the upgrade's sign, a pile of coins), the name, the rarity, the gift (+) and the curse (−), and at
    /// its foot a price or a state. Hovered it lifts 2 px and its frame burns; picked with the keys its frame burns; a card not to be had is
    /// dim with the reason at its foot; a taken one wears a "SOLD" stamp. A small card (a carried relic to leave behind) is its icon and
    /// name, its words in a hover box. A legendary's picture sparkles now and then. Whole pixels, on the 8 px grid.
    /// </summary>
    public sealed class RelicCardView : MonoBehaviour
    {
        public const int Width = 128, Height = 200;
        public const int SmallWidth = 136, SmallHeight = 24;
        private const int Art = 64;
        private const int Lift = 2;
        private const float GlintFps = 8f;

        private RectTransform _rect;
        private readonly List<Image> _ring = new List<Image>();
        private readonly List<GameObject> _corners = new List<GameObject>();
        private Image _art;
        private Image _badge;
        private Text _name;
        private Text _rarity;
        private Text _gift;
        private Text _curse;
        private Text _footer;
        private GameObject _stamp;
        private GameObject _tip;
        private Text _tipText;
        private Vector2 _home;
        private bool _hovered;
        private bool _selected;
        private Sprite[] _glint;
        private float _nextGlint;
        private float _glintStart = -1f;
        private readonly System.Random _dice = new System.Random(5);

        public event Action Pressed;

        /// <summary>The card as last shown (for tests).</summary>
        public RelicCardData Data { get; private set; }

        public bool IsSmall { get; private set; }
        public bool IsHovered => _hovered;
        public bool IsSelected => _selected;

        /// <summary>The texts (for the tests that measure them).</summary>
        public Text NameText => _name;
        public Text RarityText => _rarity;
        public Text GiftText => _gift;
        public Text CurseText => _curse;
        public Text FooterText => _footer;
        public bool StampShown => _stamp != null && _stamp.activeSelf;
        public Image Picture => _art;

        public static RelicCardView Create(Transform parent, string name, bool small)
        {
            RectTransform rect = UiFactory.CreateRect(name, parent);
            var view = rect.gameObject.AddComponent<RelicCardView>();
            view._rect = rect;
            view.IsSmall = small;
            view.Build(small);
            return view;
        }

        /// <summary>Its place on its parent (top left, whole pixels).</summary>
        public void PlaceAt(int x, int y)
        {
            _home = new Vector2(x, -y);
            _rect.PlaceTL(x, y, IsSmall ? SmallWidth : Width, IsSmall ? SmallHeight : Height);
            ApplyLift();
        }

        private Image Bar(Transform parent, string name, int x, int y, int w, int h, Color color)
        {
            Image image = UiFactory.CreateImage(name, parent, color);
            image.raycastTarget = false;
            image.rectTransform.PlaceTL(x, y, w, h);
            return image;
        }

        private void Build(bool small)
        {
            int w = small ? SmallWidth : Width, h = small ? SmallHeight : Height;
            Image back = UiFactory.CreateImage("Back", _rect, Palette.Black);
            back.rectTransform.PlaceTL(0, 0, w, h);
            back.raycastTarget = true;   // the whole card takes the pointer
            int ring = small ? 1 : 2;
            _ring.Add(Bar(_rect, "RingTop", 1, 1, w - 2, ring, Palette.BoneMid));
            _ring.Add(Bar(_rect, "RingBottom", 1, h - 1 - ring, w - 2, ring, Palette.BoneMid));
            _ring.Add(Bar(_rect, "RingLeft", 1, 1, ring, h - 2, Palette.BoneMid));
            _ring.Add(Bar(_rect, "RingRight", w - 1 - ring, 1, ring, h - 2, Palette.BoneMid));
            Bar(_rect, "Fill", 1 + ring, 1 + ring, w - 2 - 2 * ring, h - 2 - 2 * ring, Palette.Night);

            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(button);
            button.onClick.AddListener(() =>
            {
                UiFactory.PlayClick();
                Pressed?.Invoke();
            });
            var trigger = back.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => SetHovered(true));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => SetHovered(false));
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);

            if (small)
            {
                _art = Bar(_rect, "Icon", 4, 4, 16, 16, Color.white);
                _name = UiFactory.CreateText("Name", _rect, "", 8, Palette.Bone, TextAnchor.MiddleLeft);
                _name.rectTransform.PlaceTL(24, 7, SmallWidth - 28, 10);
                _name.horizontalOverflow = HorizontalWrapMode.Overflow;

                Image tip = UiFactory.CreateImage("CardTip", _rect, Palette.Black);
                tip.raycastTarget = false;
                HoverTip.Make(tip.gameObject);
                UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
                _tipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
                _tipText.rectTransform.PlaceTL(4, 3, 180, 40);
                tip.rectTransform.PlaceTL(0, SmallHeight + 2, 188, 46);
                _tip = tip.gameObject;
                _tip.SetActive(false);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                int cx = i % 2 == 0 ? 3 : Width - 9, cy = i < 2 ? 3 : Height - 9;
                Image corner = Bar(_rect, "Corner" + i, cx, cy, 6, 6, Palette.Gold);
                Bar(corner.transform, "Pin", 2, 2, 2, 2, Palette.Black);
                _corners.Add(corner.gameObject);
            }
            _art = Bar(_rect, "Picture", (Width - Art) / 2, 8, Art, Art, Color.white);
            _badge = Bar(_rect, "UpgradeSign", (Width - Art) / 2 + Art - 16, 8 + Art - 16, 16, 16, Color.white);

            int textW = Width - 12;
            _name = UiFactory.CreateText("Name", _rect, "", 8, Palette.GoldLight, TextAnchor.UpperCenter).WithOutline();
            _name.rectTransform.PlaceTL(6, 76, textW, 18);
            _rarity = UiFactory.CreateText("Rarity", _rect, "", 8, Palette.BoneMid, TextAnchor.UpperCenter);
            _rarity.rectTransform.PlaceTL(6, 95, textW, 9);
            _gift = UiFactory.CreateText("Gift", _rect, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _gift.lineSpacing = 1f;
            _curse = UiFactory.CreateText("Curse", _rect, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            _curse.lineSpacing = 1f;
            _footer = UiFactory.CreateText("Footer", _rect, "", 8, Palette.GoldLight, TextAnchor.MiddleCenter).WithOutline();
            _footer.rectTransform.PlaceTL(6, Height - 16, textW, 10);

            Image stamp = Bar(_rect, "SoldStamp", 12, 28, Width - 24, 22, Palette.Crimson);
            Bar(stamp.transform, "Edge", 0, 0, Width - 24, 1, Palette.Black);
            Bar(stamp.transform, "Edge2", 0, 21, Width - 24, 1, Palette.Black);
            Text sold = UiFactory.CreateText("Text", stamp.transform, "", 8, Palette.Bone, TextAnchor.MiddleCenter, FontStyle.Bold).WithOutline()
                .Localized(() => UiText.CardSoldStamp);
            sold.rectTransform.PlaceTL(0, 7, Width - 24, 9);
            _stamp = stamp.gameObject;
            _stamp.SetActive(false);
        }

        public void Show(RelicCardData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            bool dim = data.State == RelicCardState.Unavailable;
            Color tint = dim ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;

            Sprite picture = PictureOf(data);
            _art.sprite = picture;
            _art.color = picture != null ? tint : Color.clear;
            if (IsSmall)
            {
                _name.text = data.Name;
                _name.color = dim ? Palette.BoneDark : Palette.Bone;
                _tipText.text = data.Hover;
                RefreshRing();
                return;
            }

            Sprite sign = data.Kind == RelicCardKind.Upgrade ? UiArt.Sprite(UiArt.CardUpgrade) : null;
            _badge.sprite = sign;
            _badge.color = sign != null ? tint : Color.clear;
            if (data.Kind == RelicCardKind.Upgrade)
                _art.rectTransform.PlaceTL((Width - 48) / 2, 16, 48, 48);   // the 48 px portrait at its own size, never stretched
            else
                _art.rectTransform.PlaceTL((Width - Art) / 2, 8, Art, Art);

            _name.text = data.Name;
            _name.color = dim ? Palette.BoneDark : Palette.GoldLight;
            _rarity.text = data.RarityLabel;
            _rarity.color = dim ? Palette.BoneDark : RarityInk(data.Rarity);
            _gift.text = data.Gift;
            _gift.color = dim ? Palette.BoneDark : Palette.Bone;
            _curse.text = data.Curse;
            _curse.color = dim ? Palette.BoneDark : Palette.BoneMid;
            int textW = Width - 12;
            int giftTop = data.RarityLabel.Length > 0 ? 107 : 97;
            _gift.rectTransform.PlaceTL(6, giftTop, textW, 10);
            int giftH = Mathf.CeilToInt(_gift.preferredHeight);
            _gift.rectTransform.PlaceTL(6, giftTop, textW, Math.Max(9, giftH));
            int curseTop = giftTop + giftH + 3;
            _curse.rectTransform.PlaceTL(6, curseTop, textW, Math.Max(9, Height - 18 - curseTop));
            _footer.text = data.Footer;
            _footer.color = dim ? Palette.Hell : Palette.GoldLight;
            _stamp.SetActive(data.State == RelicCardState.Taken);
            foreach (GameObject corner in _corners) corner.SetActive(data.Rarity == RelicRarity.Legendary && data.Kind == RelicCardKind.Relic);

            _glint = data.Kind == RelicCardKind.Relic && data.Rarity == RelicRarity.Legendary ? UiArt.RelicGlint(data.RelicId) : null;
            _glintStart = -1f;
            _nextGlint = Time.unscaledTime + 1f + (float)_dice.NextDouble() * 2f;
            RefreshRing();
        }

        private static Sprite PictureOf(RelicCardData data)
        {
            switch (data.Kind)
            {
                case RelicCardKind.Upgrade: return UiArt.SinnerPortrait(data.ClassId) ?? UiArt.SinnerIcon(data.ClassId);
                case RelicCardKind.Coins: return UiArt.Sprite(UiArt.CardCoins) ?? UiArt.Sprite(UiArt.Coin);
                default: return data.Small ? UiArt.RelicIcon(data.RelicId) : UiArt.RelicArt(data.RelicId) ?? UiArt.RelicIcon(data.RelicId);
            }
        }

        /// <summary>The rarity's colour: common bone, rare blood, legendary gold; a demon's own (and an earned one) ember.</summary>
        public static Color RarityInk(RelicRarity rarity)
        {
            switch (rarity)
            {
                case RelicRarity.Rare: return Palette.Red;
                case RelicRarity.Legendary: return Palette.Gold;
                case RelicRarity.Boss: return Palette.Ember;
                case RelicRarity.Reward: return Palette.Amber;
                default: return Palette.BoneMid;
            }
        }

        private Color RingColor()
        {
            if (Data == null) return Palette.BoneMid;
            if (_hovered || _selected) return Data.State == RelicCardState.Unavailable ? Palette.BoneDark : Palette.Ember;
            if (Data.Kind == RelicCardKind.Coins) return Palette.Gold;
            if (Data.Kind == RelicCardKind.Upgrade) return Palette.Lilac;
            Color ink = RarityInk(Data.Rarity);
            return Data.State == RelicCardState.Unavailable ? Palette.BoneDark : ink;
        }

        private void RefreshRing()
        {
            Color color = RingColor();
            foreach (Image bar in _ring) bar.color = color;
        }

        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            if (_tip != null) _tip.SetActive(hovered && !string.IsNullOrEmpty(_tipText.text));
            ApplyLift();
            RefreshRing();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshRing();
        }

        private void ApplyLift()
        {
            if (_rect == null) return;
            _rect.anchoredPosition = _home + new Vector2(0, _hovered && !IsSmall ? Lift : 0);
        }

        private void Update()
        {
            if (_glint == null || _glint.Length == 0 || Data == null) return;
            float now = Time.unscaledTime;
            if (_glintStart < 0f)
            {
                if (now < _nextGlint) return;
                _glintStart = now;
            }
            int frame = (int)((now - _glintStart) * GlintFps);
            if (frame >= _glint.Length)
            {
                _art.sprite = _glint[0];
                _glintStart = -1f;
                _nextGlint = now + 2.5f + (float)_dice.NextDouble() * 2.5f;   // seldom: a burst, then a long rest
                return;
            }
            _art.sprite = _glint[frame];
        }
    }
}
