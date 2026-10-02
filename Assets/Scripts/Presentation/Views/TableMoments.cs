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
    /// The table's big moments, all in whole pixels and palette colours: a shake for a heavy loss, a hand name flaring up
    /// for a good win, and the Dead Man's Hand scene (darkness, its four cards lighting up one by one). Every effect runs
    /// on the animation clock (speed setting) and gives way to a skip.
    /// </summary>
    public sealed class TableMoments : MonoBehaviour
    {
        private const float ShakeStep = 0.05f;
        private const float FlareSeconds = 1.4f;
        private const float GlintStep = 0.35f;

        private static readonly Vector2Int[] ShakePattern =
        {
            new Vector2Int(3, 0), new Vector2Int(-3, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1),
            new Vector2Int(1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 0)
        };

        private AnimationSequencer _sequencer;
        private RectTransform _screen;
        private HandView _player;
        private SentenceView _sentence;
        private Image _darkness;
        private Image _flareBand;
        private Text _flare;
        private float _flareLeft;

        public static TableMoments Create(RectTransform screen, AnimationSequencer sequencer, HandView player, SentenceView sentence)
        {
            var moments = screen.gameObject.AddComponent<TableMoments>();
            moments._sequencer = sequencer;
            moments._screen = screen;
            moments._player = player;
            moments._sentence = sentence;

            // Darkness sits just under the player's cards, so only they stay lit.
            moments._darkness = UiFactory.CreateImage("Darkness", screen, Palette.Black);
            moments._darkness.rectTransform.Stretch();
            moments._darkness.raycastTarget = false;
            moments._darkness.transform.SetSiblingIndex(player.transform.GetSiblingIndex());
            moments._darkness.enabled = false;

            // The flare sits on a black band across the middle, so the words under it never mix with it.
            moments._flareBand = UiFactory.CreateImage("HandFlareBand", screen, Palette.Black);
            moments._flareBand.raycastTarget = false;
            moments._flareBand.rectTransform.PlaceTL(112, 88, 256, 24);
            UiFactory.AddBorder(moments._flareBand.gameObject, Palette.Gold, 1f);
            moments._flareBand.enabled = false;

            moments._flare = UiFactory.CreateText("HandFlare", moments._flareBand.transform, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithShadow();
            moments._flare.rectTransform.PlaceTL(0, 4, 256, 16);
            moments._flare.horizontalOverflow = HorizontalWrapMode.Overflow;
            moments._flare.enabled = false;
            return moments;
        }

        public void Play(TableMoment moment, string text, IReadOnlyList<int> playerCards)
        {
            switch (moment)
            {
                case TableMoment.BigLoss:
                    _sequencer.Do(_sentence.FlashLoss);
                    _sequencer.Play(Shake());
                    break;
                case TableMoment.GoodHand:
                case TableMoment.PactSealed:
                    _sequencer.Do(() => Flare(text));
                    break;
                case TableMoment.DeadMansHand:
                    _sequencer.Play(DeadMansHand(playerCards ?? new int[0]));
                    break;
            }
        }

        /// <summary>Whatever is still showing ends now (skip).</summary>
        public void Finish()
        {
            _flareLeft = 0f;
            _flare.enabled = false;
            _flareBand.enabled = false;
            _darkness.enabled = false;
            _screen.anchoredPosition = Vector2.zero;
        }

        private IEnumerator Shake()
        {
            foreach (Vector2Int offset in ShakePattern)
            {
                _screen.anchoredPosition = offset;
                yield return Tween.Wait(ShakeStep);
            }
            _screen.anchoredPosition = Vector2.zero;
        }

        private void Flare(string text)
        {
            _flare.text = text ?? "";
            _flare.enabled = true;
            _flareBand.enabled = true;
            _flareBand.transform.SetAsLastSibling();
            _flareLeft = FlareSeconds;
        }

        private IEnumerator DeadMansHand(IReadOnlyList<int> cards)
        {
            _darkness.enabled = true;
            yield return Tween.Wait(GlintStep);
            foreach (int index in cards)
            {
                _player.SetGlint(index, true);
                yield return Tween.Wait(GlintStep);
            }
            yield return Tween.Wait(GlintStep * 2f);
            foreach (int index in cards)
                _player.SetGlint(index, false);
            _darkness.enabled = false;
        }

        private void Update()
        {
            if (_flareLeft <= 0f) return;

            _flareLeft -= Time.unscaledDeltaTime * AnimationClock.Speed;
            // Pops in with two hard blinks, then holds.
            float shown = FlareSeconds - _flareLeft;
            _flare.color = shown < 0.3f && Mathf.Repeat(shown, 0.15f) < 0.075f ? Palette.Bone : Palette.GoldLight;
            if (_flareLeft <= 0f)
            {
                _flare.enabled = false;
                _flareBand.enabled = false;
            }
        }
    }
}
