using System;
using System.Collections;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.1: a chapter table's hand ending, played by its <see cref="HandTier"/> — each tier plainly more than the one before. Whole
    /// pixels, palette colours, whole sprites (the white flash is a white image switched on, never a fade). Every tier is one step of the
    /// table's queue: the demon's bar and his words wait for it, a skip ends it at once and leaves nothing on the screen — the message and
    /// the result band stay readable under it. SCREEN SHAKE off: no shake, no flash; everything else plays.
    /// </summary>
    public sealed class HandFanfareView : MonoBehaviour
    {
        private const int CoinSize = 16;
        private const int CoinCount = 20;
        private const int Pop = 3;
        private const int TitleHeight = 32;

        private static readonly Vector2Int[] ShortShake = { new Vector2Int(2, 0), new Vector2Int(-2, 1), new Vector2Int(1, -1), new Vector2Int(-1, 0), Vector2Int.zero };

        private AnimationSequencer _sequencer;
        private RectTransform _screen;
        private HandView _player;
        private HandView _house;
        private Action<Color> _glow;
        private Vector2Int _playerCentre, _houseCentre, _purse, _bar;

        private Image _darkness;
        private Image _flash;
        private Image _titleBand;
        private Text _title;
        private readonly List<Image> _coins = new List<Image>();
        private readonly List<Image> _flames = new List<Image>();
        private Sprite[] _flameFrames;
        private int _playerSibling = -1;
        private readonly List<(RectTransform card, Vector2 at)> _popped = new List<(RectTransform, Vector2)>();
        private readonly List<(HandView row, int index)> _lit = new List<(HandView, int)>();

        /// <summary>The tier playing now (for tests); None when nothing plays.</summary>
        public HandTier Playing { get; private set; } = HandTier.None;

        /// <summary>The last tier played (for tests and screenshots).</summary>
        public HandTier LastPlayed { get; private set; } = HandTier.None;

        // Demo 3.1.1: every hand its own motion — the trips' sparks, the flush's falling suits and its tone, a big loss's red edge.
        private const int SparkCount = 16;
        private const int SymbolCount = 12;
        private const int EdgeBand = 8;
        private readonly List<Image> _sparks = new List<Image>();
        private readonly List<Image> _symbols = new List<Image>();
        private readonly List<Image> _tones = new List<Image>();
        private readonly List<Image> _redEdges = new List<Image>();
        private Sprite[] _redDither;

        /// <summary>True while anything of a fanfare is on the screen (for tests: a skip leaves nothing).</summary>
        public bool AnythingShown => _darkness.enabled || _flash.enabled || _titleBand.enabled || _coins.Exists(c => c.enabled) ||
                                     _flames.Exists(f => f.enabled) || _popped.Count > 0 || _lit.Count > 0 || _sparks.Exists(s => s.enabled) ||
                                     _symbols.Exists(s => s.enabled) || _tones.Exists(t => t.enabled) || _redEdges.Exists(e => e.enabled);

        public bool FlashShown { get; private set; }

        /// <summary>The big loss's red edge flickered, and the screen shook or knocked (for tests: SCREEN SHAKE off — no shake).</summary>
        public bool RedEdgeShown { get; private set; }

        public bool ShookOrKnocked { get; private set; }

        /// <summary>The motions a fanfare made (for tests): sparks, the falling suits, the tone's flash, the wave.</summary>
        public bool SparksShown { get; private set; }

        public bool SuitsRained { get; private set; }

        public bool ToneFlashed { get; private set; }

        public bool Waved { get; private set; }

        /// <param name="playerCentre">, <paramref name="houseCentre"/>: the middle of each row of cards; <paramref name="purse"/>: where coins fly on
        /// a floor (the counter), <paramref name="bar"/>: at a demon's table (his bar); <paramref name="titleY"/>: the big name's band.</param>
        public static HandFanfareView Create(RectTransform screen, AnimationSequencer sequencer, HandView player, HandView house, Action<Color> glow,
            Vector2Int playerCentre, Vector2Int houseCentre, Vector2Int purse, Vector2Int bar, int titleX, int titleY, int titleWidth)
        {
            var view = screen.gameObject.AddComponent<HandFanfareView>();
            view._sequencer = sequencer;
            view._screen = screen;
            view._player = player;
            view._house = house;
            view._glow = glow;
            view._playerCentre = playerCentre;
            view._houseCentre = houseCentre;
            view._purse = purse;
            view._bar = bar;
            view.Build(titleX, titleY, titleWidth);
            return view;
        }

        private void Build(int titleX, int titleY, int titleWidth)
        {
            _darkness = Overlay("FanfareDarkness", Palette.Black);
            _flash = Overlay("FanfareFlash", Palette.White);

            _titleBand = UiFactory.CreateImage("FanfareTitleBand", _screen, Palette.Black);
            _titleBand.raycastTarget = false;
            // Over the whole result band (it shows again, untouched, when the name goes).
            _titleBand.rectTransform.PlaceTL(titleX, titleY, titleWidth, TitleHeight);
            UiFactory.AddBorder(_titleBand.gameObject, Palette.Gold, 1f);
            _titleBand.enabled = false;
            _title = UiFactory.CreateText("FanfareTitle", _titleBand.transform, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _title.rectTransform.PlaceTL(0, (TitleHeight - 16) / 2, titleWidth, 16);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _title.enabled = false;

            Sprite coin = UiArt.Sprite(UiArt.Coin);
            for (int i = 0; i < CoinCount; i++)
            {
                Image c = UiFactory.CreateImage("FanfareCoin" + i, _screen, coin != null ? Color.white : Palette.Gold);
                c.sprite = coin;
                c.raycastTarget = false;
                c.rectTransform.PlaceTL(0, 0, CoinSize, CoinSize);
                c.enabled = false;
                _coins.Add(c);
            }

            _flameFrames = UiArt.Strip(UiArt.Flames, UiArt.FlameFrameWidth);
            int across = PixelScreen.Width / UiArt.FlameFrameWidth;
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < across; i++)
                {
                    Image f = UiFactory.CreateImage("FanfareFlame" + row + "_" + i, _screen, _flameFrames != null ? Color.white : Palette.Hell);
                    f.raycastTarget = false;
                    // The bottom row burns up from the edge, the top row hangs down from it (the same sprite turned over about its top
                    // edge: placed 20 px down, it covers rows 0-19).
                    f.rectTransform.PlaceTL(i * UiArt.FlameFrameWidth, row == 0 ? PixelScreen.Height - 20 : 20, UiArt.FlameFrameWidth, 20);
                    if (row == 1) f.rectTransform.localScale = new Vector3(1f, -1f, 1f);
                    f.enabled = false;
                    _flames.Add(f);
                }
            }

            for (int i = 0; i < SparkCount; i++)
                _sparks.Add(Pixel("FanfareSpark" + i, i % 2 == 0 ? Palette.GoldLight : Palette.White, 2, 2));
            for (int i = 0; i < SymbolCount; i++)
                _symbols.Add(Pixel("FanfareSuit" + i, Color.white, 11, 11));
            // A frame of four 2 px bars round each card: the flush's tone.
            for (int card = 0; card < Core.Cards.Hand.Size; card++)
                for (int side = 0; side < 4; side++)
                    _tones.Add(Pixel("FanfareTone" + card + "_" + side, Palette.Hell, 1, 1));
            // The big loss's edge: a red dither (two densities, flickering), tiled along the four sides.
            _redDither = new[] { Dither(Palette.Hell, half: true), Dither(Palette.Crimson, half: false) };
            (int x, int y, int w, int h)[] sides =
            {
                (0, 0, PixelScreen.Width, EdgeBand), (0, PixelScreen.Height - EdgeBand, PixelScreen.Width, EdgeBand),
                (0, EdgeBand, EdgeBand, PixelScreen.Height - 2 * EdgeBand), (PixelScreen.Width - EdgeBand, EdgeBand, EdgeBand, PixelScreen.Height - 2 * EdgeBand)
            };
            foreach (var (x, y, w, h) in sides)
            {
                Image edge = Pixel("FanfareRedEdge", Color.white, w, h);
                edge.rectTransform.PlaceTL(x, y, w, h);
                edge.sprite = _redDither[0];
                edge.type = Image.Type.Tiled;
                edge.pixelsPerUnitMultiplier = 1f;
                _redEdges.Add(edge);
            }
        }

        private Image Pixel(string name, Color color, int w, int h)
        {
            Image image = UiFactory.CreateImage(name, _screen, color);
            image.raycastTarget = false;
            image.rectTransform.PlaceTL(0, 0, w, h);
            image.enabled = false;
            return image;
        }

        /// <summary>A 4×4 tile of one palette colour on clear: a checker (half) or one pixel in four (a quarter) — a dither, never alpha.</summary>
        private static Sprite Dither(Color colour, bool half)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color32[16];
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pixels[y * 4 + x] = (half ? (x + y) % 2 == 0 : x % 2 == 0 && y % 2 == 0) ? (Color32)colour : new Color32(0, 0, 0, 0);
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private Image Overlay(string name, Color color)
        {
            Image image = UiFactory.CreateImage(name, _screen, color);
            image.rectTransform.PlaceTL(0, 0, PixelScreen.Width, PixelScreen.Height);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        /// <summary>Queues the fanfare: one step of the table's queue (what comes after waits for its end).</summary>
        public void Play(HandFanfare fanfare)
        {
            if (fanfare == null || !fanfare.Plays) return;
            _sequencer.Play(Routine(fanfare));
        }

        /// <summary>A big loss with no tier of its own (lost to the House's pair) is still played: <see cref="HandTier.HouseStrong"/>'s place
        /// in <see cref="Playing"/> says it.</summary>
        private IEnumerator Routine(HandFanfare f)
        {
            Playing = f.Tier != HandTier.None ? f.Tier : HandTier.HouseStrong;
            LastPlayed = f.Tier;
            FlashShown = RedEdgeShown = ShookOrKnocked = SparksShown = SuitsRained = ToneFlashed = Waved = false;
            Vector2Int target = f.CoinsToBar ? _bar : _purse;
            switch (f.Tier)
            {
                case HandTier.Plain:
                    // A pair taps once (a high card does not); the coins fly.
                    if (f.Groups.Count > 0 && f.Groups[0].Count >= 2) yield return Hit(_player, f.Groups[0], 0.05f);
                    yield return CoinsFly(_playerCentre, target, 4, 0.5f);
                    break;
                case HandTier.TwoPair:
                    // Each pair on its own, the higher first.
                    ShowTitle(f.Title);
                    foreach (IReadOnlyList<int> group in f.Groups)
                        yield return Hit(_player, group, 0.18f);
                    yield return CoinsFly(_playerCentre, target, 6, 0.45f);
                    break;
                case HandTier.Trips:
                    // The three together in a ring of gold sparks; the screen knocks a pixel up and down.
                    ShowTitle(f.Title);
                    yield return Hit(_player, f.Groups.Count > 0 ? f.Groups[0] : Array.Empty<int>(), 0f);
                    yield return Knock();
                    yield return Sparks(f.Groups.Count > 0 ? f.Groups[0] : Array.Empty<int>(), 0.45f, SparkCount);
                    yield return CoinsFly(_playerCentre, target, 6, 0.45f);
                    break;
                case HandTier.Straight:
                    // A wave from the left: each card rises and lights as the one before settles; a glint on the last.
                    ShowTitle(f.Title);
                    yield return Wave(f.Groups);
                    yield return CoinsFly(_playerCentre, target, 6, 0.45f);
                    break;
                case HandTier.Flush:
                    // The suit's symbols rain onto the cards, then the cards flash once in the suit's tone.
                    ShowTitle(f.Title);
                    if (f.FlushSuit.HasValue) yield return SuitRain(f.FlushSuit.Value, 0.7f);
                    yield return ToneFlash(f.FlushSuit);
                    yield return CoinsBurst(_playerCentre, target, 8);
                    break;
                case HandTier.Full:
                    ShowTitle(f.Title);
                    foreach (IReadOnlyList<int> group in f.Groups)
                        yield return Hit(_player, group, 0.35f);
                    yield return Shake();
                    yield return CoinsBurst(_playerCentre, target, 12);
                    break;
                case HandTier.Quads:
                    yield return Flash();
                    ShowTitle(f.Title);
                    foreach (IReadOnlyList<int> group in f.Groups)
                        yield return Hit(_player, group, 0.3f);
                    yield return CoinsRain(16, 1.6f);
                    break;
                case HandTier.Blaze:
                    Darken(true);
                    yield return Burning(0.3f);
                    foreach (IReadOnlyList<int> group in f.Groups)
                        yield return Burning(0.2f, () => Light(_player, group));
                    ShowTitle(f.Title);
                    _glow?.Invoke(Palette.Hell);
                    yield return Burning(1.4f, null, rain: true);
                    break;
                case HandTier.HouseStrong:
                    foreach (IReadOnlyList<int> group in f.Groups)
                        yield return Hit(_house, group, 0.25f, down: true);
                    yield return Tween.Wait(0.2f);
                    break;
            }
            // A big loss: after the House's cards (never over them), a short shake and the edge flickering red.
            if (f.BigLoss)
            {
                Settle();
                yield return Shake();
                yield return RedFlicker(0.45f);
            }
            yield return Tween.Wait(0.15f);
            Finish();
        }

        // ------------------------------------------------------------------ Demo 3.1.1's motions

        private const int CardStep = 40;   // a card and the row's spacing

        private int CardLeft(int index) => _playerCentre.x - (CardView.Size.x * Core.Cards.Hand.Size + (CardStep - CardView.Size.x) * (Core.Cards.Hand.Size - 1)) / 2
                                           + index * CardStep;

        private int CardTop => _playerCentre.y - CardView.Size.y / 2;

        /// <summary>The screen knocks a pixel up, then down, then back (SCREEN SHAKE off: no knock).</summary>
        private IEnumerator Knock()
        {
            if (!ScreenShake.Enabled) yield break;
            ShookOrKnocked = true;
            foreach (int dy in new[] { 1, -1, 0 })
            {
                _screen.anchoredPosition = new Vector2(0f, dy);
                yield return Tween.Wait(0.05f);
            }
        }

        /// <summary>Gold sparks circle the cards of <paramref name="group"/>, twinkling, in whole pixels.</summary>
        private IEnumerator Sparks(IReadOnlyList<int> group, float seconds, int count)
        {
            if (group.Count == 0) yield break;
            SparksShown = true;
            int first = int.MaxValue, last = int.MinValue;
            foreach (int i in group)
            {
                first = Mathf.Min(first, i);
                last = Mathf.Max(last, i);
            }
            float left = CardLeft(first), right = CardLeft(last) + CardView.Size.x, top = CardTop, bottom = CardTop + CardView.Size.y;
            float cx = (left + right) / 2f, cy = (top + bottom) / 2f, rx = (right - left) / 2f + 7f, ry = (bottom - top) / 2f + 6f;
            count = Mathf.Min(count, _sparks.Count);
            for (int i = 0; i < count; i++) _sparks[i].transform.SetAsLastSibling();
            for (float t = 0f; t < seconds && !AnimationClock.IsSkipping; t += AnimationClock.DeltaTime)
            {
                int frame = (int)(t * 12f);
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2f / count + t * 2.5f;
                    _sparks[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(cx + Mathf.Cos(a) * rx), -Mathf.Round(cy + Mathf.Sin(a) * ry));
                    _sparks[i].enabled = (i + frame) % 3 != 0;
                }
                yield return null;
            }
            foreach (Image spark in _sparks) spark.enabled = false;
        }

        /// <summary>The straight's wave: card after card rises and lights from the left while the one before settles; a glint on the last.</summary>
        private IEnumerator Wave(IReadOnlyList<IReadOnlyList<int>> groups)
        {
            Waved = true;
            foreach (IReadOnlyList<int> group in groups)
            {
                Settle();
                foreach (int index in group)
                {
                    CardView card = _player.Card(index);
                    if (card == null) continue;
                    var rect = (RectTransform)card.transform;
                    _popped.Add((rect, rect.anchoredPosition));
                    rect.anchoredPosition += new Vector2(0f, Pop);
                    Light(_player, new[] { index });
                }
                yield return Tween.Wait(0.11f);
            }
            Settle();
            if (groups.Count > 0) yield return Sparks(groups[groups.Count - 1], 0.3f, 6);
        }

        /// <summary>The flush's suit falls in symbols over the hand's width onto the cards, staggered, whole pixels.</summary>
        private IEnumerator SuitRain(Core.Cards.Suit suit, float seconds)
        {
            Sprite symbol = UiArt.Suit(suit, small: false);
            if (symbol == null) yield break;
            SuitsRained = true;
            int left = CardLeft(0), width = CardLeft(Core.Cards.Hand.Size - 1) + CardView.Size.x - left;
            foreach (Image s in _symbols)
            {
                s.sprite = symbol;
                s.transform.SetAsLastSibling();
            }
            for (float t = 0f; t < seconds && !AnimationClock.IsSkipping; t += AnimationClock.DeltaTime)
            {
                float k = t / seconds;
                for (int i = 0; i < _symbols.Count; i++)
                {
                    float start = (i * 5 % _symbols.Count) / (float)_symbols.Count * 0.45f;
                    float fall = Mathf.Clamp01((k - start) / 0.55f);
                    float x = left + (i * 37 % Mathf.Max(1, width - 11));
                    float y = CardTop - 70 + fall * 90;
                    _symbols[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
                    _symbols[i].enabled = fall > 0f && fall < 1f;
                }
                yield return null;
            }
            foreach (Image s in _symbols) s.enabled = false;
        }

        /// <summary>The flush's cards flash once in the suit's tone: a 2 px frame round each (a black suit's tone is silver: black would not show).</summary>
        private IEnumerator ToneFlash(Core.Cards.Suit? suit)
        {
            ToneFlashed = true;
            Color tone = suit.HasValue ? Palette.SuitInk(suit.Value, SuitColors.FourColor) : Palette.Gold;
            if (tone == Palette.BlackSuit) tone = Palette.Silver;
            for (int card = 0; card < Core.Cards.Hand.Size; card++)
            {
                int x = CardLeft(card) - 2, y = CardTop - 2, w = CardView.Size.x + 4, h = CardView.Size.y + 4;
                (int px, int py, int pw, int ph)[] bars = { (x, y, w, 2), (x, y + h - 2, w, 2), (x, y, 2, h), (x + w - 2, y, 2, h) };
                for (int side = 0; side < 4; side++)
                {
                    Image bar = _tones[card * 4 + side];
                    bar.color = tone;
                    bar.rectTransform.PlaceTL(bars[side].px, bars[side].py, bars[side].pw, bars[side].ph);
                    bar.enabled = true;
                    bar.transform.SetAsLastSibling();
                }
            }
            yield return Tween.Wait(0.2f);
            foreach (Image bar in _tones) bar.enabled = false;
            yield return Tween.Wait(0.1f);
        }

        /// <summary>The screen's edge flickers red: a dither of two densities switched on and off (sprites, never alpha).</summary>
        private IEnumerator RedFlicker(float seconds)
        {
            RedEdgeShown = true;
            foreach (Image edge in _redEdges) edge.transform.SetAsLastSibling();
            int step = 0;
            for (float t = 0f; t < seconds && !AnimationClock.IsSkipping; t += 0.06f, step++)
            {
                foreach (Image edge in _redEdges)
                {
                    edge.sprite = _redDither[step % 2];
                    edge.enabled = step % 3 != 2;
                }
                yield return Tween.Wait(0.06f);
            }
            foreach (Image edge in _redEdges) edge.enabled = false;
        }

        // ------------------------------------------------------------------ the pieces

        private void ShowTitle(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _title.text = text;
            _title.enabled = true;
            _titleBand.enabled = true;
            _titleBand.transform.SetAsLastSibling();
        }

        /// <summary>A group of cards hits: each pops up a few pixels (the House's down) and lights, then the group holds lit.</summary>
        private IEnumerator Hit(HandView row, IReadOnlyList<int> group, float hold, bool down = false)
        {
            foreach (int index in group)
            {
                CardView card = row.Card(index);
                if (card == null) continue;
                var rect = (RectTransform)card.transform;
                _popped.Add((rect, rect.anchoredPosition));
                rect.anchoredPosition += new Vector2(0f, down ? -Pop : Pop);
                Light(row, new[] { index });
            }
            yield return Tween.Wait(0.08f);
            Settle();
            yield return Tween.Wait(hold);
        }

        private void Light(HandView row, IReadOnlyList<int> group)
        {
            foreach (int index in group)
            {
                row.SetGlint(index, true);
                _lit.Add((row, index));
            }
        }

        /// <summary>Popped cards drop back to their places (they stay lit).</summary>
        private void Settle()
        {
            foreach (var (card, at) in _popped)
                if (card != null) card.anchoredPosition = at;
            _popped.Clear();
        }

        private IEnumerator CoinsFly(Vector2Int from, Vector2Int to, int count, float seconds)
        {
            count = Mathf.Min(count, _coins.Count);
            for (int i = 0; i < count; i++)
            {
                _coins[i].enabled = true;
                _coins[i].transform.SetAsLastSibling();
            }
            const float stagger = 0.06f;
            float total = seconds + stagger * (count - 1);
            for (float t = 0f; t < total && !AnimationClock.IsSkipping; t += AnimationClock.DeltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    float k = Mathf.Clamp01((t - i * stagger) / seconds);
                    // An arc: up from the cards, over to the purse.
                    float x = Mathf.Lerp(from.x + (i - count / 2) * 4, to.x, k);
                    float y = Mathf.Lerp(from.y, to.y, k) - Mathf.Sin(k * Mathf.PI) * 24f;
                    Place(_coins[i], x, y);
                    _coins[i].enabled = k < 1f;
                }
                yield return null;
            }
            HideCoins();
        }

        /// <summary>The coins burst out of the cards in a ring, then fly to the purse.</summary>
        private IEnumerator CoinsBurst(Vector2Int from, Vector2Int to, int count)
        {
            count = Mathf.Min(count, _coins.Count);
            var out_ = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                out_[i] = new Vector2(from.x + Mathf.Cos(a) * 40f, from.y + Mathf.Sin(a) * 22f);
                _coins[i].enabled = true;
                _coins[i].transform.SetAsLastSibling();
            }
            yield return Tween.Run(0.25f, k =>
            {
                for (int i = 0; i < count; i++) Place(_coins[i], Mathf.Lerp(from.x, out_[i].x, k), Mathf.Lerp(from.y, out_[i].y, k));
            });
            yield return Tween.Run(0.5f, k =>
            {
                for (int i = 0; i < count; i++) Place(_coins[i], Mathf.Lerp(out_[i].x, to.x, k * k), Mathf.Lerp(out_[i].y, to.y, k * k));
            });
            HideCoins();
        }

        /// <summary>Coins fall from the top of the screen across the middle, staggered, in whole pixels.</summary>
        private IEnumerator CoinsRain(int count, float seconds)
        {
            count = Mathf.Min(count, _coins.Count);
            for (int i = 0; i < count; i++) _coins[i].transform.SetAsLastSibling();
            for (float t = 0f; t < seconds && !AnimationClock.IsSkipping; t += AnimationClock.DeltaTime)
            {
                RainStep(count, t / seconds);
                yield return null;
            }
            HideCoins();
        }

        private void RainStep(int count, float k)
        {
            for (int i = 0; i < count; i++)
            {
                float start = (i * 7 % count) / (float)count * 0.5f;
                float fall = Mathf.Clamp01((k - start) / 0.5f);
                float x = 120 + (i * 53 % 400);
                Place(_coins[i], x, -CoinSize + fall * (PixelScreen.Height + CoinSize));
                _coins[i].enabled = fall > 0f && fall < 1f;
            }
        }

        private IEnumerator Flash()
        {
            if (!ScreenShake.Enabled) yield break;
            FlashShown = true;
            for (int blink = 0; blink < 2; blink++)
            {
                _flash.enabled = true;
                _flash.transform.SetAsLastSibling();
                yield return Tween.Wait(0.05f);
                _flash.enabled = false;
                yield return Tween.Wait(0.05f);
            }
        }

        private IEnumerator Shake()
        {
            if (!ScreenShake.Enabled) yield break;
            ShookOrKnocked = true;
            foreach (Vector2Int offset in ShortShake)
            {
                _screen.anchoredPosition = offset;
                yield return Tween.Wait(0.04f);
            }
            _screen.anchoredPosition = Vector2.zero;
        }

        /// <summary>The whole screen goes dark but the player's cards; the edges burn.</summary>
        private void Darken(bool on)
        {
            if (on)
            {
                _darkness.enabled = true;
                _darkness.transform.SetAsLastSibling();
                _playerSibling = _player.transform.GetSiblingIndex();
                _player.transform.SetAsLastSibling();
                foreach (Image flame in _flames)
                {
                    flame.enabled = true;
                    flame.transform.SetAsLastSibling();
                }
                return;
            }
            _darkness.enabled = false;
            foreach (Image flame in _flames) flame.enabled = false;
            if (_playerSibling >= 0) _player.transform.SetSiblingIndex(_playerSibling);
            _playerSibling = -1;
        }

        /// <summary>Waits while the edge's flames move (8 frames a second), doing <paramref name="first"/> at its start; with
        /// <paramref name="rain"/> coins fall meanwhile.</summary>
        private IEnumerator Burning(float seconds, Action first = null, bool rain = false)
        {
            first?.Invoke();
            int count = rain ? _coins.Count : 0;
            for (int i = 0; i < count; i++) _coins[i].transform.SetAsLastSibling();
            for (float t = 0f; t < seconds && !AnimationClock.IsSkipping; t += AnimationClock.DeltaTime)
            {
                if (_flameFrames != null && _flameFrames.Length > 0)
                {
                    int frame = (int)(Time.unscaledTime * 8f);
                    for (int i = 0; i < _flames.Count; i++) _flames[i].sprite = _flameFrames[(frame + i) % _flameFrames.Length];
                }
                if (rain) RainStep(count, t / seconds);
                yield return null;
            }
            if (rain) HideCoins();
        }

        private void Place(Image image, float x, float y) =>
            image.rectTransform.anchoredPosition = new Vector2(Mathf.Round(x - CoinSize / 2f), -Mathf.Round(y - CoinSize / 2f));

        private void HideCoins()
        {
            foreach (Image coin in _coins) coin.enabled = false;
        }

        /// <summary>Whatever is still showing ends now (the fanfare's end, and a skip): nothing stays on the screen.</summary>
        public void Finish()
        {
            Settle();
            foreach (var (row, index) in _lit) row.SetGlint(index, false);
            _lit.Clear();
            HideCoins();
            Darken(false);
            foreach (Image image in _sparks) image.enabled = false;
            foreach (Image image in _symbols) image.enabled = false;
            foreach (Image image in _tones) image.enabled = false;
            foreach (Image image in _redEdges) image.enabled = false;
            _flash.enabled = false;
            _title.enabled = false;
            _titleBand.enabled = false;
            if (Playing != HandTier.None) _screen.anchoredPosition = Vector2.zero;
            Playing = HandTier.None;
        }
    }
}
