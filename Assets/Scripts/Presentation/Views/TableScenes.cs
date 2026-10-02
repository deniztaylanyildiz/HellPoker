using System;
using System.Collections;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The big table changes, in whole pixels and without any transparency blending:
    /// - summoned: once the old demon has had their last word, darkness creeps over the hall in growing Bayer patterns,
    ///   the hall and the demon change in the dark, and the dark lifts on the Morning Star's eyes;
    /// - cast down: the whole screen drops away (the view rushes up), and the player lands in the old demon's hall.
    /// Each scene is split in two queue steps — into the change, and out of it — so the table's other updates (payouts,
    /// counter, soul) land while the screen is dark or away. A tremor shakes the screen by one pixel when Lucifer speaks.
    /// Every step runs on the animation clock and gives way to a skip.
    /// </summary>
    public sealed class TableScenes : MonoBehaviour
    {
        private const float HoldAfterLine = 0.7f;
        private const float LongestLineWait = 6f;
        private const float FadeStep = 0.35f;
        private const float LiftStep = 0.2f;
        private const float DarkHold = 0.5f;
        private const float FallSeconds = 0.5f;
        private const int FallStep = 6;

        private AnimationSequencer _sequencer;
        private RectTransform _screen;
        private DealerView _dealer;
        private Image _shroud;
        private Sprite[] _fade;
        private Func<IEnumerator> _pendingOut;

        public static TableScenes Create(RectTransform screen, AnimationSequencer sequencer, DealerView dealer)
        {
            var scenes = screen.gameObject.AddComponent<TableScenes>();
            scenes._sequencer = sequencer;
            scenes._screen = screen;
            scenes._dealer = dealer;
            scenes._fade = UiArt.Strip(UiArt.Fade, UiArt.FadeWidth);

            scenes._shroud = UiFactory.CreateImage("Shroud", screen, Palette.Black);
            scenes._shroud.rectTransform.Stretch();
            scenes._shroud.raycastTarget = false;
            scenes._shroud.enabled = false;
            return scenes;
        }

        /// <summary>True while the second half of a scene waits for the table to be redressed.</summary>
        public bool IsMidScene => _pendingOut != null;

        /// <summary>Plays the first half of a scene; <paramref name="change"/> swaps the hall and the demon at its darkest.</summary>
        public void Begin(SeatChange change, Action changeOver)
        {
            if (change == SeatChange.Instant)
            {
                _sequencer.Do(changeOver);
                return;
            }

            Finish();
            if (change == SeatChange.Summoned)
            {
                _sequencer.Play(IntoDarkness(changeOver));
                _pendingOut = OutOfDarkness;
            }
            else
            {
                _sequencer.Play(FallAway(changeOver));
                _pendingOut = Land;
            }
        }

        /// <summary>Queues the second half of a scene, if one is waiting (the new demon is about to speak, or the table is ready).</summary>
        public void End()
        {
            if (_pendingOut == null) return;
            Func<IEnumerator> outro = _pendingOut;
            _pendingOut = null;
            _sequencer.Play(outro());
        }

        /// <summary>A one-pixel shudder of the whole screen (the Morning Star speaks).</summary>
        public void Tremor()
        {
            _sequencer.Play(Shudder());
        }

        /// <summary>Whatever is still showing ends now (skip).</summary>
        public void Finish()
        {
            if (_shroud == null) return;
            if (_pendingOut == null)
            {
                _shroud.enabled = false;
                _screen.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>A scene cut short by an instant change (a new run, a chosen table): nothing of it stays on screen.</summary>
        public void Abort()
        {
            _pendingOut = null;
            if (_shroud == null) return;
            _shroud.enabled = false;
            _screen.anchoredPosition = Vector2.zero;
        }

        private IEnumerator WaitForTheLastWord()
        {
            for (float waited = 0f; _dealer.IsSpeaking && waited < LongestLineWait && !AnimationClock.IsSkipping; waited += AnimationClock.DeltaTime)
                yield return null;
            yield return Tween.Wait(HoldAfterLine);
        }

        private IEnumerator IntoDarkness(Action changeOver)
        {
            yield return WaitForTheLastWord();
            _shroud.transform.SetAsLastSibling();
            int steps = _fade != null ? _fade.Length : 1;
            for (int i = 0; i < steps; i++)
            {
                Shroud(i);
                yield return Tween.Wait(FadeStep);
            }
            Shroud(steps - 1);
            changeOver();
            yield return Tween.Wait(DarkHold);
        }

        private IEnumerator OutOfDarkness()
        {
            int steps = _fade != null ? _fade.Length : 1;
            for (int i = steps - 2; i >= 0; i--)
            {
                Shroud(i);
                yield return Tween.Wait(LiftStep);
            }
            _shroud.enabled = false;
        }

        /// <summary>Darkness at step <paramref name="step"/> (0 = a quarter, last = all).</summary>
        private void Shroud(int step)
        {
            _shroud.enabled = true;
            if (_fade == null)
            {
                _shroud.sprite = null;
                _shroud.color = Palette.Black;
                return;
            }
            _shroud.sprite = _fade[Mathf.Clamp(step, 0, _fade.Length - 1)];
            _shroud.color = Color.white;
        }

        private IEnumerator FallAway(Action changeOver)
        {
            yield return WaitForTheLastWord();
            // The player falls: the hall rushes up and out of the top of the screen.
            yield return Tween.Run(FallSeconds, t => Drop(Mathf.Lerp(0f, PixelScreen.Height, t * t)));
            Drop(PixelScreen.Height);
            changeOver();
            Drop(-PixelScreen.Height);
        }

        private IEnumerator Land()
        {
            // ...and the old demon's hall comes up from below, landing with a jolt.
            yield return Tween.Run(FallSeconds, t => Drop(Mathf.Lerp(-PixelScreen.Height, 0f, t)));
            Drop(0f);
            foreach (int bump in new[] { -3, 2, -1, 0 })
            {
                Drop(bump);
                yield return Tween.Wait(0.05f);
            }
        }

        /// <summary>Moves the screen to a height, in whole steps of <see cref="FallStep"/> pixels.</summary>
        private void Drop(float y)
        {
            float snapped = Mathf.Abs(y) >= PixelScreen.Height ? y : Mathf.Round(y / FallStep) * FallStep;
            _screen.anchoredPosition = new Vector2(0f, Mathf.Round(snapped));
        }

        private IEnumerator Shudder()
        {
            foreach (int x in new[] { 1, -1, 1, 0 })
            {
                _screen.anchoredPosition = new Vector2(x, 0f);
                yield return Tween.Wait(0.04f);
            }
            _screen.anchoredPosition = Vector2.zero;
        }
    }
}
