using System.Collections;
using System.Collections.Generic;
using HellPoker.Core.Cheats;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A cheat's blow on the cards, in whole pixels: every card it hits shudders a pixel or two while the cheat's sign
    /// (chain, coins, mask, tongue, serpent, moon, thorn, eye, quill, flame, falling star) flashes over it. The tithe sends
    /// coins flying from the player's hand to the demon; the Gaze passes over every card; The Fall shakes the whole screen.
    /// What the cheat changed is shown by the table afterwards (a card turning, a mark appearing). Runs on the animation clock
    /// and gives way to a skip.
    /// </summary>
    public sealed class CheatEffects : MonoBehaviour
    {
        private const float Step = 0.06f;
        private static readonly int[] Shudder = { 2, -2, 1, -1, 2, -1, 0 };
        private static readonly Vector2Int[] FallShake =
        {
            new Vector2Int(0, 4), new Vector2Int(0, -3), new Vector2Int(2, 2), new Vector2Int(-2, -2), new Vector2Int(0, 1), new Vector2Int(0, 0)
        };

        private AnimationSequencer _sequencer;
        private RectTransform _screen;
        private HandView _player;
        private HandView _house;
        private Vector2Int _dealerPoint;
        private readonly Dictionary<CardView, Image> _signs = new Dictionary<CardView, Image>();
        private readonly List<Image> _coins = new List<Image>();
        private readonly List<CardView> _shaking = new List<CardView>();

        /// <summary>The last cheat played (for tests and screenshots).</summary>
        public string LastCheat { get; private set; }

        /// <param name="dealerPoint">Where the tithe's coins fly to (the demon's portrait), in screen pixels.</param>
        public static CheatEffects Create(RectTransform screen, AnimationSequencer sequencer, HandView player, HandView house, Vector2Int dealerPoint)
        {
            var effects = screen.gameObject.AddComponent<CheatEffects>();
            effects._sequencer = sequencer;
            effects._screen = screen;
            effects._player = player;
            effects._house = house;
            effects._dealerPoint = dealerPoint;
            for (int i = 0; i < 3; i++)
            {
                Image coin = UiFactory.CreateImage("TitheCoin" + i, screen, Color.white);
                coin.raycastTarget = false;
                coin.enabled = false;
                effects._coins.Add(coin);
            }
            return effects;
        }

        public void Play(CheatImpact impact)
        {
            if (impact == null) return;
            _sequencer.Play(Strike(impact));
        }

        /// <summary>The cheat turned on its demon: <paramref name="text"/> blinks over the first card it touched.</summary>
        public void PlayBackfire(string text, IReadOnlyList<int> playerCards)
        {
            _sequencer.Play(Backfire(text, playerCards));
        }

        /// <summary>Everything back in place (skip).</summary>
        public void Finish()
        {
            if (_backfire != null) _backfire.SetActive(false);
            foreach (Image sign in _signs.Values) sign.enabled = false;
            foreach (Image coin in _coins) coin.enabled = false;
            foreach (CardView card in _shaking) card.Nudge(0);
            _shaking.Clear();
            _screen.anchoredPosition = Vector2.zero;
        }

        private IEnumerator Strike(CheatImpact impact)
        {
            LastCheat = impact.CheatId;
            Sprite icon = UiArt.CheatIcon(impact.CheatId);

            var targets = new List<CardView>();
            IEnumerable<int> playerCards = impact.CheatId == CheatIds.Gaze ? new[] { 0, 1, 2, 3, 4 } : (IEnumerable<int>)impact.PlayerCards;
            foreach (int i in playerCards)
                if (_player.Card(i) != null) targets.Add(_player.Card(i));
            foreach (int i in impact.HouseCards)
                if (_house.Card(i) != null) targets.Add(_house.Card(i));

            if (impact.CheatId == CheatIds.Tithe)
                yield return FlyCoins(icon);

            _shaking.Clear();
            _shaking.AddRange(targets);
            for (int s = 0; s < Shudder.Length; s++)
            {
                foreach (CardView card in targets)
                {
                    card.Nudge(Shudder[s]);
                    Image sign = SignOn(card);
                    sign.sprite = icon;
                    sign.enabled = icon != null && s % 2 == 0;
                }
                if (impact.CheatId == CheatIds.TheFall && s < FallShake.Length)
                    _screen.anchoredPosition = FallShake[s];
                yield return Tween.Wait(Step);
            }
            foreach (CardView card in targets)
            {
                card.Nudge(0);
                SignOn(card).enabled = false;
            }
            _shaking.Clear();
            _screen.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// "BACKFIRE" on a dark strip across the card (readable over anything), blinking a few times, then gone.
        /// </summary>
        private IEnumerator Backfire(string text, IReadOnlyList<int> playerCards)
        {
            LastBackfire = text;
            CardView card = playerCards != null && playerCards.Count > 0 ? _player.Card(playerCards[0]) : null;
            if (_backfire == null) BuildBackfire();
            _backfireText.text = text;

            // The strip is centred on the card (or on the player's hand), wider than the card so the word fits.
            Vector2 centre = card != null ? ScreenPoint(card) : new Vector2(240, 164);
            ((RectTransform)_backfire.transform).PlaceTL(Mathf.RoundToInt(centre.x) - BackfireWidth / 2, Mathf.RoundToInt(centre.y) - 6,
                BackfireWidth, 12);
            _backfire.transform.SetAsLastSibling();
            for (int i = 0; i < 6; i++)
            {
                _backfire.SetActive(i % 2 == 0 || i == 5);
                yield return Tween.Wait(i == 5 ? 0.5f : 0.12f);
            }
            _backfire.SetActive(false);
        }

        private const int BackfireWidth = 72;
        private GameObject _backfire;
        private Text _backfireText;

        /// <summary>The last backfire shown (for tests and screenshots).</summary>
        public string LastBackfire { get; private set; }

        private void BuildBackfire()
        {
            Image strip = UiFactory.CreateImage("Backfire", _screen, Palette.Black);
            strip.raycastTarget = false;
            UiFactory.AddBorder(strip.gameObject, Palette.Ember, 1f);
            _backfireText = UiFactory.CreateText("Text", strip.transform, "", 8, Palette.GoldLight, TextAnchor.MiddleCenter, FontStyle.Bold)
                .WithOutline();
            _backfireText.rectTransform.Stretch();
            _backfire = strip.gameObject;
            _backfire.SetActive(false);
        }

        /// <summary>The centre of a card in screen pixels (top-left origin), from its place under the screen.</summary>
        private Vector2 ScreenPoint(CardView card)
        {
            var rect = (RectTransform)card.transform;
            Vector3 local = _screen.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            // In PlaceTL terms: x from the screen's left edge, y down from its top.
            Rect screen = _screen.rect;
            return new Vector2(local.x - screen.xMin, screen.yMax - local.y);
        }

        /// <summary>Three coins rise from the player's hand to the demon, one after another.</summary>
        private IEnumerator FlyCoins(Sprite icon)
        {
            Sprite coin = UiArt.MalicePip(Core.Dealers.DealerRoster.MammonId, full: true) ?? icon;
            var from = new Vector2(240, 160);
            var to = new Vector2(_dealerPoint.x, _dealerPoint.y);
            yield return Tween.Run(0.6f, t =>
            {
                for (int i = 0; i < _coins.Count; i++)
                {
                    float k = Mathf.Clamp01(t * 1.6f - i * 0.25f);
                    Vector2 p = Vector2.Lerp(from, to, k);
                    p.y -= Mathf.Sin(k * Mathf.PI) * 30f;   // an arc
                    _coins[i].sprite = coin;
                    _coins[i].enabled = coin != null && k > 0f && k < 1f;
                    _coins[i].rectTransform.PlaceTL(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), 8, 8);
                    _coins[i].transform.SetAsLastSibling();
                }
            });
            foreach (Image c in _coins) c.enabled = false;
        }

        /// <summary>The sign laid over a card (made once per card).</summary>
        private Image SignOn(CardView card)
        {
            if (_signs.TryGetValue(card, out Image sign)) return sign;
            sign = UiFactory.CreateImage("CheatSign", card.transform, Color.white);
            sign.raycastTarget = false;
            sign.rectTransform.PlaceTL((CardView.Size.x - UiArt.CheatIconSize) / 2, (CardView.Size.y - UiArt.CheatIconSize) / 2,
                UiArt.CheatIconSize, UiArt.CheatIconSize);
            sign.enabled = false;
            _signs[card] = sign;
            return sign;
        }
    }
}
