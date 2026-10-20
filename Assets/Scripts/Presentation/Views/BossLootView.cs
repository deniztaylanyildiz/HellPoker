using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Relics;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.5: the spoils of a beaten demon (ARAM's choice), a scene of its own. His hall darkened (a Bayer dither, no alpha), what lies
    /// where he sat (his toppled throne, 2×), "SPOILS" and "Mammon has fallen. Choose one.", the cards coming in one by one — up from below,
    /// their backs first, turning over with a sound, a sparkle by their rarity (the legendary's the most) — then the choice: the chosen card
    /// to the middle, the others dark and falling, its prize flying to the map's side panel (a relic or an upgrade to the relics, the coins
    /// to the purse). REROLL once under the cards (its new faces turn over). The bag full: the carried relics as small cards and BACK. Any
    /// press while it moves hurries it to its end. Whole pixels, the animation clock's speed.
    /// </summary>
    public sealed class BossLootView : MonoBehaviour, IBossLootView
    {
        public const int SortingOrder = 47;
        private const int CardsTop = 64;
        private const float Stagger = 0.28f, Slide = 0.3f, Turn = 0.2f, ToMiddle = 0.3f, Flight = 0.5f;
        private const int SlideFrom = 160;

        /// <summary>Where the prizes land on the map's side panel (its RELICS button, its purse).</summary>
        public static readonly Vector2Int RelicsPoint = new Vector2Int(64, 312), CoinsPoint = new Vector2Int(22, 66);

        private Canvas _canvas;
        private RectTransform _screen;
        private Image _hall;
        private Image _fallen;
        private Text _title;
        private Text _subtitle;
        private RectTransform _cardsRoot;
        private Button _reroll;
        private Text _rerollLabel;
        private Button _back;
        private Image _flyer;
        private readonly List<RelicCardView> _cards = new List<RelicCardView>();
        private readonly List<RelicCardView> _swap = new List<RelicCardView>();
        private readonly List<(Image image, Vector2 from, Vector2 velocity)> _sparks = new List<(Image, Vector2, Vector2)>();
        private BossLootState _state;

        // the timeline: what plays, since when
        private enum Play { Nothing, Deal, Reroll, Chosen }
        private Play _play;
        private float _time;
        private readonly HashSet<int> _turning = new HashSet<int>();
        private readonly HashSet<int> _faced = new HashSet<int>();
        private int _chosen;
        private Vector2Int _chosenFrom;
        private Vector2Int _flightTo;
        private Action _done;
        private float _sparkTime;

        public event Action<int> CardPressed;
        public event Action RerollPressed;
        public event Action BackPressed;

        /// <summary>The sounds of the cards turning over (set by the composition root; none in tests).</summary>
        public IAudio Audio { get; set; }

        public bool IsOpen => _canvas.enabled;
        public bool IsAnimating => _play != Play.Nothing;

        /// <summary>For tests and screenshots: the cards on show, the swap step's small cards, the scene's words.</summary>
        public IReadOnlyList<RelicCardView> Cards => _cards;
        public IReadOnlyList<RelicCardView> SwapCards => _swap;
        public BossLootState State => _state;
        public Text Title => _title;
        public Text Subtitle => _subtitle;
        public Button RerollButton => _reroll;
        public bool FlyerShown => _flyer.gameObject.activeSelf;

        public static BossLootView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("BossLootCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<BossLootView>();
            view._canvas = canvas;
            view._screen = screen;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            _hall = UiFactory.CreateImage("Hall", screen, Palette.Black);
            _hall.rectTransform.PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);
            _hall.raycastTarget = true;
            // A press anywhere while it moves hurries it (the hall takes the click under the cards).
            var hurry = _hall.gameObject.AddComponent<Button>();
            hurry.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(hurry);
            hurry.onClick.AddListener(() => { if (IsAnimating) Skip(); });

            Image dark = UiFactory.CreateImage("Darkness", screen, Color.white);
            dark.raycastTarget = false;
            dark.rectTransform.PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);
            Sprite[] fade = UiArt.Strip(UiArt.Fade, 16);
            if (fade != null && fade.Length >= 3)
            {
                dark.sprite = fade[2];   // three quarters dark
                dark.type = Image.Type.Tiled;
                dark.pixelsPerUnitMultiplier = 1f;
            }
            else dark.color = Color.clear;

            _fallen = UiFactory.CreateImage("Fallen", screen, Color.white);
            _fallen.raycastTarget = false;
            _fallen.rectTransform.PlaceTL((PixelScreen.Width - 320) / 2, PixelScreen.Height - 160, 320, 128);

            _title = UiFactory.CreateText("LootTitle", screen, "", 16, Palette.GoldLight, TextAnchor.UpperCenter, FontStyle.Bold).WithOutline();
            _title.rectTransform.PlaceTL(0, 12, PixelScreen.Width, 16);
            _subtitle = UiFactory.CreateText("LootSubtitle", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter).WithOutline();
            _subtitle.rectTransform.PlaceTL(0, 36, PixelScreen.Width, 9);

            _cardsRoot = UiFactory.CreateRect("Cards", screen).PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);

            _reroll = UiFactory.CreateButton("LootReroll", screen, "", 8, out _rerollLabel, ButtonSkin.Ember);
            ((RectTransform)_reroll.transform).PlaceTL((PixelScreen.Width - 128) / 2, PixelScreen.Height - 28, 128, 20);
            _reroll.onClick.AddListener(() =>
            {
                if (IsAnimating) { Skip(); return; }
                RerollPressed?.Invoke();
            });
            _back = UiFactory.CreateButton("LootBack", screen, "", 8, out Text backLabel, ButtonSkin.Ash);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)_back.transform).PlaceTL((PixelScreen.Width - 96) / 2, PixelScreen.Height - 28, 96, 20);
            _back.onClick.AddListener(() => BackPressed?.Invoke());

            _flyer = UiFactory.CreateImage("Flyer", screen, Color.white);
            _flyer.raycastTarget = false;
            _flyer.rectTransform.PlaceTL(0, 0, 32, 32);
            _flyer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ showing

        public void Show(BossLootState state, bool deal)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            transform.SetAsLastSibling();

            SpriteClip hall = UiArt.Salons?.Get(state.BossId, SalonMode.Normal);
            Sprite still = hall != null && hall.Frames != null && hall.Frames.Length > 0 ? hall.Frames[0] : null;
            _hall.sprite = still;
            _hall.color = still != null ? Color.white : Palette.Black;
            Sprite fallen = UiArt.Sprite(UiArt.LootFallen(state.BossId));
            _fallen.sprite = fallen;
            _fallen.color = fallen != null ? Color.white : Color.clear;
            _title.text = state.Swap != null ? state.SwapTitle : state.Title;
            _subtitle.text = state.Subtitle;

            ClearCards();
            _flyer.gameObject.SetActive(false);
            if (state.Swap != null)
            {
                BuildSwap(state.Swap);
                _reroll.gameObject.SetActive(false);
                _back.gameObject.SetActive(true);
                _play = Play.Nothing;
                Point();
                return;
            }
            _back.gameObject.SetActive(false);
            _reroll.gameObject.SetActive(true);
            _rerollLabel.text = state.RerollLabel;
            _reroll.GetComponent<ButtonFeel>().Locked = !state.CanReroll;

            for (int i = 0; i < state.Cards.Count; i++)
            {
                RelicCardView card = RelicCardView.Create(_cardsRoot, "LootCardView" + i, small: false);
                card.Show(state.Cards[i]);
                card.GetComponentInChildren<Button>().name = "LootCard" + i;
                int index = i;
                card.Pressed += () =>
                {
                    if (IsAnimating) { Skip(); return; }
                    CardPressed?.Invoke(index);
                };
                _cards.Add(card);
                card.PlaceAt(Home(i).x, Home(i).y);
            }

            _faced.Clear();
            _turning.Clear();
            if (deal)
            {
                for (int i = 0; i < _cards.Count; i++)
                {
                    _cards[i].SetBack(true);
                    _cards[i].MoveTo(Home(i).x, Home(i).y + SlideFrom);
                    _cards[i].gameObject.SetActive(false);
                }
                _play = Play.Deal;
                _time = 0f;
            }
            else
            {
                for (int i = 0; i < _cards.Count; i++) _faced.Add(i);
                _play = Play.Nothing;
            }
            Point();
        }

        /// <summary>A card's place in the row: centred on the 8 px grid.</summary>
        private Vector2Int Home(int index)
        {
            int count = _state?.Cards.Count ?? 0;
            int width = count * RelicCardView.Width + Math.Max(0, count - 1) * 8;
            int left = (PixelScreen.Width - width) / 2 / 8 * 8;
            return new Vector2Int(left + index * (RelicCardView.Width + 8), CardsTop);
        }

        private void BuildSwap(IReadOnlyList<RelicCardData> carried)
        {
            int columns = carried.Count > 6 ? 3 : 2;
            int width = columns * RelicCardView.SmallWidth + (columns - 1) * 8;
            int left = (PixelScreen.Width - width) / 2 / 8 * 8;
            for (int i = 0; i < carried.Count; i++)
            {
                RelicCardView card = RelicCardView.Create(_cardsRoot, "LootSwapView" + i, small: true);
                card.Show(carried[i]);
                card.GetComponentInChildren<Button>().name = "LootSwap" + i;
                int index = i;
                card.Pressed += () => CardPressed?.Invoke(index);
                card.PlaceAt(left + i % columns * (RelicCardView.SmallWidth + 8), 72 + i / columns * (RelicCardView.SmallHeight + 8));
                _swap.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (RelicCardView card in _cards.Concat(_swap))
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();
            _swap.Clear();
            foreach (var spark in _sparks)
                if (spark.image != null) Destroy(spark.image.gameObject);
            _sparks.Clear();
        }

        /// <summary>The keys' pick: a card framed in ember, or REROLL lit.</summary>
        private void Point()
        {
            if (_state == null) return;
            IReadOnlyList<RelicCardView> cards = _state.Swap != null ? (IReadOnlyList<RelicCardView>)_swap : _cards;
            for (int i = 0; i < cards.Count; i++) cards[i].SetSelected(i == _state.Selected);
            ButtonFeel feel = _reroll.GetComponent<ButtonFeel>();
            if (feel != null) feel.Selected = _state.Swap == null && _state.Selected == _state.Cards.Count;
            ButtonFeel back = _back.GetComponent<ButtonFeel>();
            if (back != null) back.Selected = _state.Swap != null && _state.Selected == _state.Swap.Count;
        }

        public void Reroll(BossLootState state, IReadOnlyCollection<int> changed)
        {
            _state = state;
            for (int i = 0; i < _cards.Count && i < state.Cards.Count; i++) _cards[i].Show(state.Cards[i]);
            _rerollLabel.text = state.RerollLabel;
            _reroll.GetComponent<ButtonFeel>().Locked = !state.CanReroll;
            _turning.Clear();
            _faced.Clear();
            for (int i = 0; i < _cards.Count; i++)
                if (changed.Contains(i)) _turning.Add(i);
                else _faced.Add(i);
            foreach (int i in _turning) _cards[i].SetBack(true);
            _play = _turning.Count > 0 ? Play.Reroll : Play.Nothing;
            _time = 0f;
            Point();
        }

        public void PlayChosen(int index, LootFlight flight, Action done)
        {
            if (index < 0 || index >= _cards.Count)
            {
                done?.Invoke();
                return;
            }
            if (IsAnimating) Skip();
            _chosen = index;
            _chosenFrom = _cards[index].Place;
            _flightTo = flight == LootFlight.Coins ? CoinsPoint : RelicsPoint;
            RelicCardData data = _state.Cards[index];
            _flyer.sprite = flight == LootFlight.Coins ? UiArt.Sprite(UiArt.Coin) : flight == LootFlight.Upgrade ? UiArt.Sprite(UiArt.CardUpgrade)
                : UiArt.RelicIcon(data.RelicId);
            _flyer.color = _flyer.sprite != null ? Color.white : Color.clear;
            _done = done;
            _play = Play.Chosen;
            _time = 0f;
            _reroll.gameObject.SetActive(false);
            for (int i = 0; i < _cards.Count; i++) _cards[i].SetSelected(i == index);
        }

        public void Skip()
        {
            if (_play == Play.Deal || _play == Play.Reroll)
            {
                for (int i = 0; i < _cards.Count; i++)
                {
                    _cards[i].gameObject.SetActive(true);
                    _cards[i].SetBack(false);
                    _cards[i].SetSqueeze(RelicCardView.Width);
                    _cards[i].MoveTo(Home(i).x, Home(i).y);
                    _faced.Add(i);
                }
                _turning.Clear();
                _play = Play.Nothing;
            }
            else if (_play == Play.Chosen)
            {
                FinishChosen();
            }
            foreach (var spark in _sparks)
                if (spark.image != null) Destroy(spark.image.gameObject);
            _sparks.Clear();
        }

        private void FinishChosen()
        {
            _play = Play.Nothing;
            _flyer.gameObject.SetActive(false);
            Action done = _done;
            _done = null;
            done?.Invoke();
        }

        public void Hide()
        {
            if (_play == Play.Chosen) FinishChosen();
            _play = Play.Nothing;
            ClearCards();
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        /// <summary>The keys' pick moved (the presenter's state carries it).</summary>
        public void Select(BossLootState state)
        {
            _state = state;
            Point();
        }

        // ------------------------------------------------------------------ the timeline

        /// <summary>Screenshots: the scene's clock moves only by <see cref="Advance"/> (a frame grab takes longer than a frame).</summary>
        public bool ManualClock { get; set; }

        /// <summary>Moves the scene's clock by <paramref name="seconds"/> (with <see cref="ManualClock"/>).</summary>
        public void Advance(float seconds) => Tick(seconds);

        private void Update()
        {
            if (!ManualClock) Tick(Time.unscaledDeltaTime * Mathf.Max(1f, AnimationClock.Speed));
        }

        private void Tick(float dt)
        {
            if (_play == Play.Nothing && _sparks.Count == 0) return;
            _time += dt;
            switch (_play)
            {
                case Play.Deal: Deal(); break;
                case Play.Reroll: Turning(_time, _turning); if (_turning.All(_faced.Contains)) _play = Play.Nothing; break;
                case Play.Chosen: Chosen(); break;
            }
            Sparks(dt);
        }

        private void Deal()
        {
            bool all = true;
            for (int i = 0; i < _cards.Count; i++)
            {
                float t = _time - i * Stagger;
                if (t < 0f)
                {
                    all = false;
                    continue;
                }
                RelicCardView card = _cards[i];
                card.gameObject.SetActive(true);
                Vector2Int home = Home(i);
                float slide = Mathf.Clamp01(t / Slide);
                float ease = 1f - (1f - slide) * (1f - slide);
                card.MoveTo(home.x, home.y + Mathf.RoundToInt((1f - ease) * SlideFrom));
                if (slide < 1f)
                {
                    all = false;
                    continue;
                }
                if (!_faced.Contains(i))
                {
                    TurnOver(i, t - Slide);
                    all = false;
                }
            }
            if (all) _play = Play.Nothing;
        }

        private void Turning(float t, IEnumerable<int> which)
        {
            foreach (int i in which.ToList())
                if (!_faced.Contains(i)) TurnOver(i, t);
        }

        /// <summary>A card turning over: its back narrows to nothing, its face widens out of it; a sound and a sparkle as it shows.</summary>
        private void TurnOver(int index, float t)
        {
            RelicCardView card = _cards[index];
            if (t < Turn)
            {
                card.SetBack(true);
                card.SetSqueeze(Mathf.RoundToInt((1f - t / Turn) * RelicCardView.Width));
                return;
            }
            if (card.BackShown)
            {
                card.SetBack(false);
                Audio?.PlaySfx(_state.Cards[index].Rarity == RelicRarity.Legendary ? SfxIds.Loot : SfxIds.Flip);
                Burst(index);
            }
            float open = Mathf.Clamp01((t - Turn) / Turn);
            card.SetSqueeze(Mathf.RoundToInt(open * RelicCardView.Width));
            if (open >= 1f) _faced.Add(index);
        }

        /// <summary>A sparkle by the card's rarity: the legendary's twelve sparks, the rare's six, an upgrade's four.</summary>
        private void Burst(int index)
        {
            RelicCardData data = _state.Cards[index];
            int count = data.Kind == RelicCardKind.Upgrade ? 4 : data.Kind == RelicCardKind.Coins ? 0
                : data.Rarity == RelicRarity.Legendary ? 12 : data.Rarity == RelicRarity.Rare || data.Rarity == RelicRarity.Boss ? 6 : 0;
            Vector2Int home = Home(index);
            Vector2 centre = new Vector2(home.x + RelicCardView.Width / 2, home.y + 40);
            for (int k = 0; k < count; k++)
            {
                float angle = 2f * Mathf.PI * k / count;
                Image spark = UiFactory.CreateImage("Spark", _screen, data.Rarity == RelicRarity.Legendary ? Palette.GoldLight : Palette.Ember);
                spark.raycastTarget = false;
                spark.rectTransform.PlaceTL(Mathf.RoundToInt(centre.x), Mathf.RoundToInt(centre.y), 2, 2);
                _sparks.Add((spark, centre, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 90f));
            }
            _sparkTime = 0f;
        }

        private void Sparks(float dt)
        {
            if (_sparks.Count == 0) return;
            _sparkTime += dt;
            if (_sparkTime > 0.45f)
            {
                foreach (var spark in _sparks)
                    if (spark.image != null) Destroy(spark.image.gameObject);
                _sparks.Clear();
                return;
            }
            foreach (var (image, from, velocity) in _sparks)
            {
                Vector2 at = from + velocity * _sparkTime;
                image.rectTransform.PlaceTL(Mathf.RoundToInt(at.x), Mathf.RoundToInt(at.y), 2, 2);
            }
        }

        private void Chosen()
        {
            float t = _time;
            int middle = (PixelScreen.Width - RelicCardView.Width) / 2 / 8 * 8;
            float move = Mathf.Clamp01(t / ToMiddle);
            _cards[_chosen].MoveTo(Mathf.RoundToInt(Mathf.Lerp(_chosenFrom.x, middle, move)), _chosenFrom.y);
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i == _chosen) continue;
                Vector2Int home = Home(i);
                _cards[i].SetShade(Mathf.Min(3, 1 + Mathf.FloorToInt(move * 2f)));   // dark, never a black box
                _cards[i].MoveTo(home.x, home.y + Mathf.RoundToInt(move * move * 220f));
            }
            if (t < ToMiddle) return;
            float fly = Mathf.Clamp01((t - ToMiddle) / Flight);
            _flyer.gameObject.SetActive(true);
            Vector2 from = new Vector2(middle + RelicCardView.Width / 2 - 16, _chosenFrom.y + 8 + 16);
            Vector2 at = Vector2.Lerp(from, _flightTo - new Vector2(16, 16), fly * fly);
            _flyer.rectTransform.PlaceTL(Mathf.RoundToInt(at.x), Mathf.RoundToInt(at.y), 32, 32);
            if (fly >= 1f) FinishChosen();
        }
    }
}
