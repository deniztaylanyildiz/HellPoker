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
    /// Demo 3.5: the black market as a scene of its own, 640×360. The vendor's stall (his own hall, its lanterns and dust moving), his
    /// portrait large on the left with his words in a bubble under it; at the top the purse and the relics carried; on the right the
    /// stall's cards (the relics, the tailor's upgrades, the gambler's box face down) with their prices at their feet and what else they
    /// cost under them, the services' tiles under the cards (an icon, a name, a price; the words in a hover box), LEAVE at the foot. A pick
    /// (which relic to sell, to throw away, to put in the crucible) shows the carried relics' small cards with their prices, and BACK.
    /// What just happened plays over the new state: bought — the coins fly from the purse, the card shakes, its prize flies to the relics;
    /// paid — the coins fly to the purse; refused — it shakes, the purse flashes; new wares — the cards turn over. Any press hurries it.
    /// Whole pixels, the 8 px grid, the animation clock's speed.
    /// </summary>
    public sealed class MarketView : MonoBehaviour, IMarketView
    {
        public const int SortingOrder = 47;

        // The places (top left, the 640×360 screen).
        public const int Right = 224, RightWidth = 400;
        public const int CardsTop = 32, TilesGap = 8, TileWidth = 128, TileHeight = 24, TileStep = 28;
        public const int PickWidth = 176, PickStep = 32, PriceWidth = 36;
        public const int PortraitSize = 96;
        public const int BubbleX = 16, BubbleY = 160, BubbleWidth = 192;
        public static readonly Vector2Int PursePoint = new Vector2Int(24, 12), RelicsPoint = new Vector2Int(600, 12), VendorPoint = new Vector2Int(112, 80);

        private const float Shake = 0.24f, CoinFlight = 0.3f, CoinStagger = 0.05f, PrizeFlight = 0.4f, Turn = 0.16f, TurnStagger = 0.08f;
        private const int Coins = 5;

        private Canvas _canvas;
        private RectTransform _screen;
        private SalonView _hall;
        private Image _portrait;
        private Text _owner;
        private Text _title;
        private Image _bubble;
        private Text _line;
        private Image _purseIcon;
        private Text _purse;
        private Text _relics;
        private RectTransform _root;
        private MarketScene _scene;

        private readonly List<RectTransform> _places = new List<RectTransform>();
        private readonly List<Vector2Int> _homes = new List<Vector2Int>();
        private readonly Dictionary<int, RelicCardView> _cards = new Dictionary<int, RelicCardView>();
        private readonly Dictionary<int, Tile> _tiles = new Dictionary<int, Tile>();
        private ButtonFeel _leave;
        private readonly List<Image> _flyers = new List<Image>();

        // the moment playing
        private MarketFeel _feel;
        private float _time;
        private bool _playing;

        public event Action<int> SlotPressed;

        public bool IsOpen => _canvas.enabled;
        public bool IsAnimating => _playing;

        /// <summary>For tests and screenshots: the scene as shown, its cards and tiles by place, its words.</summary>
        public MarketScene Scene => _scene;
        public IReadOnlyDictionary<int, RelicCardView> Cards => _cards;
        public IEnumerable<int> TilePlaces => _tiles.Keys;
        public RectTransform PlaceOf(int index) => index >= 0 && index < _places.Count ? _places[index] : null;
        public Text Line => _line;
        public Text Title => _title;
        public Text Owner => _owner;
        public Text Purse => _purse;
        public Text Relics => _relics;
        public Image Portrait => _portrait;
        public Text TileName(int index) => _tiles.TryGetValue(index, out Tile tile) ? tile.Name : null;
        public Text TilePrice(int index) => _tiles.TryGetValue(index, out Tile tile) ? tile.Price : null;
        public SalonView Hall => _hall;

        /// <summary>The sounds of the moments (set by the composition root; none in tests).</summary>
        public IAudio Audio { get; set; }

        public static MarketView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("MarketCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<MarketView>();
            view._canvas = canvas;
            view._screen = screen;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            _hall = SalonView.Create(screen, UiArt.Salons);
            var hurry = _hall.gameObject.AddComponent<Button>();
            hurry.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(hurry);
            hurry.onClick.AddListener(() => { if (IsAnimating) Skip(); });

            // The top: the purse on the left, the title in the middle, the relics carried on the right (on a dark strip).
            Image strip = UiFactory.CreateImage("TopStrip", screen, Palette.Black);
            strip.raycastTarget = false;
            strip.rectTransform.PlaceTL(0, 0, PixelScreen.Width, 24);
            Image edge = UiFactory.CreateImage("TopEdge", screen, Palette.Gold);
            edge.raycastTarget = false;
            edge.rectTransform.PlaceTL(0, 24, PixelScreen.Width, 1);
            _purseIcon = UiFactory.CreateImage("PurseIcon", screen, Color.white);
            _purseIcon.raycastTarget = false;
            _purseIcon.sprite = UiArt.Sprite(UiArt.Coin);
            _purseIcon.color = _purseIcon.sprite != null ? Color.white : Color.clear;
            _purseIcon.rectTransform.PlaceTL(16, 4, 16, 16);
            _purse = UiFactory.CreateText("MarketPurse", screen, "", 8, Palette.GoldLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline();
            _purse.rectTransform.PlaceTL(36, 7, 120, 10);
            _title = UiFactory.CreateText("MarketTitle", screen, "", 8, Palette.Ember, TextAnchor.MiddleCenter, FontStyle.Bold).WithOutline();
            _title.rectTransform.PlaceTL(160, 7, PixelScreen.Width - 320, 10);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _relics = UiFactory.CreateText("MarketRelics", screen, "", 8, Palette.Bone, TextAnchor.MiddleRight, FontStyle.Bold).WithOutline();
            _relics.rectTransform.PlaceTL(PixelScreen.Width - 176, 7, 160, 10);

            // The vendor: his portrait large in a frame, his name under it, his words in a bubble.
            int px = BubbleX + (BubbleWidth - PortraitSize) / 2;
            Image frame = UiFactory.CreatePanel("PortraitFrame", screen);
            frame.raycastTarget = false;
            frame.rectTransform.PlaceTL(px - 4, 36, PortraitSize + 8, PortraitSize + 8);
            _portrait = UiFactory.CreateImage("VendorPortrait", frame.transform, Color.white);
            _portrait.raycastTarget = false;
            _portrait.rectTransform.PlaceTL(4, 4, PortraitSize, PortraitSize);
            _owner = UiFactory.CreateText("VendorName", screen, "", 8, Palette.GoldLight, TextAnchor.UpperCenter).WithOutline();
            _owner.rectTransform.PlaceTL(BubbleX, 36 + PortraitSize + 12, BubbleWidth, 10);

            _bubble = UiFactory.CreateDialog("Bubble", screen);
            _bubble.raycastTarget = false;
            _bubble.rectTransform.PlaceTL(BubbleX, BubbleY, BubbleWidth, PixelScreen.Height - 24 - BubbleY);
            _line = UiFactory.CreateText("VendorLine", _bubble.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _line.rectTransform.PlaceTL(8, 8, BubbleWidth - 16, PixelScreen.Height - 24 - BubbleY - 16);
            _line.lineSpacing = 1f;

            _root = UiFactory.CreateRect("Places", screen).PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);
        }

        // ------------------------------------------------------------------ showing

        public void Show(MarketScene scene, MarketFeel feel)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            if (_playing) Skip();
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            transform.SetAsLastSibling();

            _hall.SetSalon("market_" + scene.VendorId);
            Sprite big = UiArt.Sprite("Market/" + scene.VendorId);
            Sprite small = big == null ? UiArt.Sprite("Events/market_" + scene.VendorId) : null;
            _portrait.sprite = big ?? small;
            _portrait.color = _portrait.sprite != null ? Color.white : Color.clear;
            int size = big != null ? PortraitSize : 48;   // the small portrait at its own size, never stretched
            _portrait.rectTransform.PlaceTL(4 + (PortraitSize - size) / 2, 4 + (PortraitSize - size) / 2, size, size);
            _owner.text = scene.Owner;
            _title.text = scene.Title;
            _line.text = scene.Line;
            // The bubble as tall as its words (at most down to the floor).
            int most = PixelScreen.Height - 24 - BubbleY;
            int tall = Mathf.Clamp((Mathf.CeilToInt(_line.preferredHeight) + 16 + 7) / 8 * 8, 48, most);
            _bubble.rectTransform.PlaceTL(BubbleX, BubbleY, BubbleWidth, tall);
            _line.rectTransform.PlaceTL(8, 8, BubbleWidth - 16, tall - 16);
            _purse.text = scene.Coins.ToString();
            _purse.color = Palette.GoldLight;
            _relics.text = scene.RelicsLabel;

            Rebuild();
            Point();

            _feel = feel;
            _time = 0f;
            _playing = feel.Moment != MarketMoment.None;
            if (feel.Moment == MarketMoment.Bought || feel.Moment == MarketMoment.Paid) Audio?.PlaySfx(feel.Moment == MarketMoment.Paid ? SfxIds.CoinWin : SfxIds.MarketBuy);
            if (feel.Moment == MarketMoment.Rerolled)
                foreach (RelicCardView card in _cards.Values) card.SetBack(true);
        }

        public void Select(MarketScene scene)
        {
            _scene = scene;
            Point();
        }

        public void Hide()
        {
            if (_playing) Skip();
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void Clear()
        {
            foreach (RectTransform place in _places)
                if (place != null) Destroy(place.gameObject);
            _places.Clear();
            _homes.Clear();
            _cards.Clear();
            _tiles.Clear();
            _leave = null;
        }

        /// <summary>The places laid out again: the stall's cards (or the small cards to pick), the tiles under them, LEAVE at the foot.</summary>
        private void Rebuild()
        {
            Clear();
            IReadOnlyList<MarketSlot> slots = _scene.Slots;
            int stall = 0, tiles = 0, picks = 0;
            foreach (MarketSlot slot in slots)
                if (slot.Kind == MarketSlotKind.Stall) stall++;
                else if (slot.Kind == MarketSlotKind.Service) tiles++;
                else if (slot.Kind == MarketSlotKind.Pick) picks++;

            int cardsLeft = Snap(Right + (RightWidth - (stall * RelicCardView.Width + Math.Max(0, stall - 1) * TilesGap)) / 2);
            int tilesTop = stall > 0 ? CardsTop + RelicCardView.Height + 20 : CardsTop;
            int pickLeft = Snap(Right + (RightWidth - (MarketScene.PicksPerRow * PickWidth + TilesGap)) / 2);
            int s = 0, t = 0, p = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                MarketSlot slot = slots[i];
                int index = i;
                switch (slot.Kind)
                {
                    case MarketSlotKind.Stall:
                    {
                        RelicCardView card = RelicCardView.Create(_root, "MarketCard" + i, small: false);
                        card.Show(slot.Card);
                        card.SetBack(slot.FaceDown);
                        card.GetComponentInChildren<Button>().name = "PanelOption" + i;
                        card.Pressed += () => Press(index);
                        Vector2Int home = new Vector2Int(cardsLeft + s * (RelicCardView.Width + TilesGap), CardsTop);
                        card.PlaceAt(home.x, home.y);
                        if (slot.Under.Length > 0)
                        {
                            Text under = UiFactory.CreateText("Under", card.transform, slot.Under, 8, slot.FaceDown ? Palette.GoldLight : Palette.Amber,
                                TextAnchor.UpperCenter).WithOutline();
                            under.rectTransform.PlaceTL(0, RelicCardView.Height + 3, RelicCardView.Width, 10);
                        }
                        Add((RectTransform)card.transform, home);
                        _cards[i] = card;
                        s++;
                        break;
                    }
                    case MarketSlotKind.Pick:
                    {
                        RectTransform place = UiFactory.CreateRect("MarketPick" + i, _root);
                        Vector2Int home = new Vector2Int(pickLeft + p % MarketScene.PicksPerRow * (PickWidth + TilesGap), CardsTop + p / MarketScene.PicksPerRow * PickStep);
                        place.PlaceTL(home.x, home.y, PickWidth, RelicCardView.SmallHeight);
                        RelicCardView card = RelicCardView.Create(place, "Card", small: true);
                        card.Show(slot.Card.With(slot.Enabled ? Abstractions.RelicCardState.Normal : Abstractions.RelicCardState.Unavailable, slot.Card.Footer));
                        card.GetComponentInChildren<Button>().name = "PanelOption" + i;
                        card.Pressed += () => Press(index);
                        card.PlaceAt(0, 0);
                        Text price = UiFactory.CreateText("Price", place, slot.Price, 8, slot.Enabled ? Palette.GoldLight : Palette.Hell, TextAnchor.MiddleLeft).WithOutline();
                        price.rectTransform.PlaceTL(RelicCardView.SmallWidth + 4, 7, PriceWidth, 10);
                        price.horizontalOverflow = HorizontalWrapMode.Overflow;
                        Add(place, home);
                        _cards[i] = card;
                        p++;
                        break;
                    }
                    case MarketSlotKind.Service:
                    {
                        Vector2Int home = new Vector2Int(Right + t % MarketScene.TilesPerRow * (TileWidth + TilesGap), tilesTop + t / MarketScene.TilesPerRow * TileStep);
                        Tile tile = Tile.Create(_root, i, slot, () => Press(index));
                        tile.Rect.PlaceTL(home.x, home.y, TileWidth, TileHeight);
                        Add(tile.Rect, home);
                        _tiles[i] = tile;
                        t++;
                        break;
                    }
                    default:
                    {
                        Button leave = UiFactory.CreateButton("PanelOption" + i, _root, slot.Label, 8, out Text _, ButtonSkin.Ash);
                        var rect = (RectTransform)leave.transform;
                        Vector2Int home = new Vector2Int(Snap(Right + (RightWidth - 96) / 2), PixelScreen.Height - 28);
                        rect.PlaceTL(home.x, home.y, 96, 20);
                        leave.onClick.AddListener(() => Press(index));
                        _leave = leave.GetComponent<ButtonFeel>();
                        Add(rect, home);
                        break;
                    }
                }
            }
        }

        private static int Snap(int x) => x / 8 * 8;

        private void Add(RectTransform place, Vector2Int home)
        {
            _places.Add(place);
            _homes.Add(home);
        }

        private void Press(int index)
        {
            if (IsAnimating)
            {
                Skip();
                return;
            }
            SlotPressed?.Invoke(index);
        }

        /// <summary>The keys' pick: a card's frame, a tile's frame, LEAVE lit.</summary>
        private void Point()
        {
            if (_scene == null) return;
            foreach (var pair in _cards) pair.Value.SetSelected(pair.Key == _scene.Selected);
            foreach (var pair in _tiles) pair.Value.SetSelected(pair.Key == _scene.Selected);
            if (_leave != null) _leave.Selected = _scene.Selected == _scene.Slots.Count - 1 && _scene.Slots.Count > 0 &&
                                                  _scene.Slots[_scene.Slots.Count - 1].Kind == MarketSlotKind.Leave;
        }

        // ------------------------------------------------------------------ the moments

        /// <summary>Screenshots: the scene's clock moves only by <see cref="Advance"/>.</summary>
        public bool ManualClock { get; set; }

        public void Advance(float seconds) => Tick(seconds);

        private void Update()
        {
            if (!ManualClock) Tick(Time.unscaledDeltaTime * Mathf.Max(1f, AnimationClock.Speed));
        }

        public void Skip()
        {
            if (!_playing) return;
            _playing = false;
            for (int i = 0; i < _places.Count; i++)
            {
                if (_places[i] == null) continue;
                if (_cards.TryGetValue(i, out RelicCardView big) && !big.IsSmall) big.PlaceAt(_homes[i].x, _homes[i].y);
                else _places[i].PlaceTL(_homes[i].x, _homes[i].y, (int)_places[i].sizeDelta.x, (int)_places[i].sizeDelta.y);
            }
            foreach (RelicCardView card in _cards.Values)
            {
                if (card.IsSmall) continue;
                int index = IndexOf(card);
                card.SetBack(index >= 0 && _scene.Slots[index].FaceDown);
                card.SetSqueeze(RelicCardView.Width);
            }
            foreach (Image flyer in _flyers)
                if (flyer != null) Destroy(flyer.gameObject);
            _flyers.Clear();
            _purse.color = Palette.GoldLight;
        }

        private int IndexOf(RelicCardView card)
        {
            foreach (var pair in _cards) if (pair.Value == card) return pair.Key;
            return -1;
        }

        private void Tick(float dt)
        {
            if (!_playing) return;
            _time += dt;
            float end;
            switch (_feel.Moment)
            {
                case MarketMoment.Bought:
                    ShakeSlot(_time);
                    end = FlyCoins(_time, PursePoint, SlotCentre(_feel.Slot, VendorPoint));
                    if (_feel.Slot >= 0 && _cards.TryGetValue(_feel.Slot, out RelicCardView bought) && !bought.IsSmall && _scene.Slots[_feel.Slot].Card != null &&
                        _scene.Slots[_feel.Slot].Card.Kind != RelicCardKind.Coins)
                        end = Mathf.Max(end, FlyPrize(_time - CoinFlight, _scene.Slots[_feel.Slot].Card));
                    break;
                case MarketMoment.Paid:
                    ShakeSlot(_time);
                    end = FlyCoins(_time, SlotCentre(_feel.Slot, VendorPoint), PursePoint);
                    _purse.color = _time < end ? Palette.Amber : Palette.GoldLight;
                    break;
                case MarketMoment.Refused:
                    ShakeSlot(_time);
                    _purse.color = (int)(_time / 0.08f) % 2 == 0 ? Palette.Hell : Palette.GoldLight;
                    end = 0.4f;
                    break;
                case MarketMoment.Rerolled:
                    end = TurnCards(_time);
                    break;
                default:
                    end = 0f;
                    break;
            }
            if (_time >= end) Skip();
        }

        private void ShakeSlot(float t)
        {
            if (_feel.Slot < 0 || _feel.Slot >= _places.Count || _places[_feel.Slot] == null) return;
            int dx = t >= Shake ? 0 : ((int)(t / 0.04f) % 2 == 0 ? 1 : -1);
            Vector2Int home = _homes[_feel.Slot];
            RectTransform place = _places[_feel.Slot];
            if (_cards.TryGetValue(_feel.Slot, out RelicCardView card) && !card.IsSmall) card.MoveTo(home.x + dx, home.y);
            else place.PlaceTL(home.x + dx, home.y, (int)place.sizeDelta.x, (int)place.sizeDelta.y);
        }

        private Vector2Int SlotCentre(int slot, Vector2Int otherwise)
        {
            if (slot < 0 || slot >= _places.Count || _places[slot] == null) return otherwise;
            RectTransform place = _places[slot];
            return new Vector2Int(_homes[slot].x + (int)place.sizeDelta.x / 2, _homes[slot].y + Math.Min(40, (int)place.sizeDelta.y / 2));
        }

        /// <summary>Five coins from one point to another, one after the other; returns when the last lands.</summary>
        private float FlyCoins(float t, Vector2Int from, Vector2Int to)
        {
            Sprite coin = UiArt.Sprite(UiArt.Coin);
            while (_flyers.Count < Coins)
            {
                Image flyer = UiFactory.CreateImage("Coin", _screen, Color.white);
                flyer.raycastTarget = false;
                flyer.sprite = coin;
                flyer.color = coin != null ? Color.white : Palette.Gold;
                flyer.gameObject.SetActive(false);
                _flyers.Add(flyer);
            }
            for (int k = 0; k < Coins; k++)
            {
                float u = (t - k * CoinStagger) / CoinFlight;
                Image flyer = _flyers[k];
                flyer.gameObject.SetActive(u > 0f && u < 1f);
                if (u <= 0f || u >= 1f) continue;
                // An arc: up a little, then down to where it lands.
                Vector2 at = Vector2.Lerp(from, to, u) + new Vector2(0f, -Mathf.Sin(u * Mathf.PI) * 24f);
                flyer.rectTransform.PlaceTL(Mathf.RoundToInt(at.x) - 6, Mathf.RoundToInt(at.y) - 6, 12, 12);
            }
            return (Coins - 1) * CoinStagger + CoinFlight;
        }

        private Image _prize;

        /// <summary>The bought thing's icon from its card to the relics at the top; returns when it lands (counted from the coins' start).</summary>
        private float FlyPrize(float t, RelicCardData card)
        {
            if (_prize == null || !_flyers.Contains(_prize))
            {
                _prize = UiFactory.CreateImage("Prize", _screen, Color.white);
                _prize.raycastTarget = false;
                _prize.sprite = card.Kind == RelicCardKind.Upgrade ? UiArt.Sprite(UiArt.CardUpgrade) : UiArt.RelicIcon(card.RelicId);
                _prize.color = _prize.sprite != null ? Color.white : Color.clear;
                _prize.gameObject.SetActive(false);
                _flyers.Add(_prize);
            }
            float u = t / PrizeFlight;
            _prize.gameObject.SetActive(u > 0f && u < 1f);
            if (u > 0f && u < 1f)
            {
                Vector2Int from = SlotCentre(_feel.Slot, VendorPoint);
                Vector2 at = Vector2.Lerp(from, RelicsPoint, u * u);
                _prize.rectTransform.PlaceTL(Mathf.RoundToInt(at.x) - 8, Mathf.RoundToInt(at.y) - 8, 16, 16);
            }
            return CoinFlight + PrizeFlight;
        }

        /// <summary>New wares: each card's back narrows to nothing and its face widens out of it, one after the other.</summary>
        private float TurnCards(float t)
        {
            int k = 0;
            foreach (var pair in _cards)
            {
                RelicCardView card = pair.Value;
                if (card.IsSmall) continue;
                float u = t - k * TurnStagger;
                k++;
                if (u < 0f) continue;
                if (u < Turn)
                {
                    card.SetBack(true);
                    card.SetSqueeze(Mathf.RoundToInt((1f - u / Turn) * RelicCardView.Width));
                    continue;
                }
                if (card.BackShown && !_scene.Slots[pair.Key].FaceDown)
                {
                    card.SetBack(false);
                    Audio?.PlaySfx(SfxIds.Flip);
                }
                card.SetSqueeze(Mathf.RoundToInt(Mathf.Clamp01((u - Turn) / Turn) * RelicCardView.Width));
            }
            return Math.Max(0, k - 1) * TurnStagger + 2 * Turn;
        }

        // ------------------------------------------------------------------ a service's tile

        /// <summary>A service: its icon, its name over its price, a frame that burns on hover or when the keys pick it; its words in a hover box.</summary>
        private sealed class Tile
        {
            public RectTransform Rect;
            public Text Name;
            public Text Price;
            private readonly List<Image> _ring = new List<Image>();
            private GameObject _tip;
            private bool _enabled;
            private bool _hovered;
            private bool _selected;

            public static Tile Create(Transform parent, int index, MarketSlot slot, Action pressed)
            {
                var tile = new Tile { _enabled = slot.Enabled };
                RectTransform rect = UiFactory.CreateRect("MarketTile" + index, parent);
                tile.Rect = rect;
                Image back = UiFactory.CreateImage("Back", rect, Palette.Black);
                back.rectTransform.PlaceTL(0, 0, TileWidth, TileHeight);
                back.raycastTarget = true;
                back.gameObject.name = "PanelOption" + index;
                foreach (var (x, y, w, h) in new[] { (1, 1, TileWidth - 2, 1), (1, TileHeight - 2, TileWidth - 2, 1), (1, 1, 1, TileHeight - 2), (TileWidth - 2, 1, 1, TileHeight - 2) })
                {
                    Image bar = UiFactory.CreateImage("Ring", rect, Palette.BoneMid);
                    bar.raycastTarget = false;
                    bar.rectTransform.PlaceTL(x, y, w, h);
                    tile._ring.Add(bar);
                }
                Image fill = UiFactory.CreateImage("Fill", rect, Palette.Night);
                fill.raycastTarget = false;
                fill.rectTransform.PlaceTL(2, 2, TileWidth - 4, TileHeight - 4);

                Image icon = UiFactory.CreateImage("Icon", rect, Color.white);
                icon.raycastTarget = false;
                Sprite[] icons = UiArt.Strip("Ui/market_services", 16);
                int at = (int)slot.Service;
                icon.sprite = icons != null && at < icons.Length ? icons[at] : null;
                icon.color = icon.sprite == null ? Color.clear : slot.Enabled ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                icon.rectTransform.PlaceTL(4, 4, 16, 16);

                tile.Name = UiFactory.CreateText("Name", rect, slot.Label, 8, slot.Enabled ? Palette.Bone : Palette.BoneDark, TextAnchor.UpperLeft);
                tile.Name.rectTransform.PlaceTL(24, 2, TileWidth - 28, 10);
                tile.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
                tile.Price = UiFactory.CreateText("Price", rect, slot.Price, 8, slot.Enabled ? Palette.GoldLight : Palette.Hell, TextAnchor.UpperLeft);
                tile.Price.rectTransform.PlaceTL(24, 12, TileWidth - 28, 10);
                tile.Price.horizontalOverflow = HorizontalWrapMode.Overflow;

                var button = back.gameObject.AddComponent<Button>();
                button.targetGraphic = back;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() =>
                {
                    UiFactory.PlayClick();
                    pressed();
                });

                if (slot.Detail.Length > 0)
                {
                    Image tip = UiFactory.CreateImage("TileTip", rect, Palette.Black);
                    tip.raycastTarget = false;
                    HoverTip.Make(tip.gameObject);
                    UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
                    Text text = UiFactory.CreateText("Text", tip.transform, slot.Detail, 8, Palette.Bone, TextAnchor.UpperLeft);
                    text.rectTransform.PlaceTL(4, 3, 180, 60);
                    int h = Mathf.Clamp(Mathf.CeilToInt(text.preferredHeight) + 7, 14, 66);
                    tip.rectTransform.PlaceTL(0, -h - 2, 188, h);
                    tile._tip = tip.gameObject;
                    tile._tip.SetActive(false);
                }
                var trigger = back.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => tile.SetHovered(true));
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => tile.SetHovered(false));
                trigger.triggers.Add(enter);
                trigger.triggers.Add(exit);
                tile.Refresh();
                return tile;
            }

            private void SetHovered(bool hovered)
            {
                _hovered = hovered;
                if (_tip != null) _tip.SetActive(hovered);
                Refresh();
            }

            public void SetSelected(bool selected)
            {
                _selected = selected;
                Refresh();
            }

            private void Refresh()
            {
                Color color = _hovered || _selected ? (_enabled ? Palette.Ember : Palette.BoneMid) : _enabled ? Palette.BoneMid : Palette.BoneDark;
                foreach (Image bar in _ring) bar.color = color;
            }
        }
    }
}
