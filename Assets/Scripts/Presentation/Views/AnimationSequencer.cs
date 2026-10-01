using System;
using System.Collections;
using System.Collections.Generic;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Plays view updates strictly in the order they were requested. Instant updates run immediately when nothing is
    /// animating, otherwise they wait their turn — so a result message never appears before the cards are turned.
    /// <see cref="Complete"/> fast-forwards everything queued to its end state (skip). A step that throws is logged and
    /// dropped, so one bad update can never freeze the queue.
    /// </summary>
    public sealed class AnimationSequencer : MonoBehaviour
    {
        private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();
        private IEnumerator _current;
        private bool _running;

        public bool IsBusy => _current != null || _queue.Count > 0;

        public void Do(Action update)
        {
            if (IsBusy)
                Play(Instant(update));
            else
                update();
        }

        public void Play(IEnumerator animation)
        {
            _queue.Enqueue(animation);
            if (!_running && isActiveAndEnabled)
                StartCoroutine(Run());
            else if (!isActiveAndEnabled)
                Complete();   // nothing would ever play it: show the end state now
        }

        public void Wait(float seconds)
        {
            Play(Tween.Wait(seconds));
        }

        /// <summary>Runs everything queued to its end at once: tweens jump to their last frame, waits are skipped.</summary>
        public void Complete()
        {
            AnimationClock.BeginSkip();
            try
            {
                while (Step(out _)) { }
            }
            finally
            {
                AnimationClock.EndSkip();
            }
        }

        private IEnumerator Run()
        {
            _running = true;
            while (Step(out object yielded))
                yield return yielded;
            _running = false;
        }

        /// <summary>Advances the current step (taking the next one when it ends). False when nothing is left.</summary>
        private bool Step(out object yielded)
        {
            yielded = null;
            while (true)
            {
                if (_current == null)
                {
                    if (_queue.Count == 0) return false;
                    _current = Tween.Flatten(_queue.Dequeue());
                }

                bool more;
                try
                {
                    more = _current.MoveNext();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    more = false;
                }

                if (more)
                {
                    yielded = _current.Current;
                    return true;
                }
                _current = null;
            }
        }

        private static IEnumerator Instant(Action update)
        {
            update();
            yield break;
        }

        private void OnDisable()
        {
            // The coroutine dies with the object (usually the scene closing): drop what is queued. Views that must not lose
            // an update (the hall behind a new table) set it directly instead of queueing it.
            _queue.Clear();
            _current = null;
            _running = false;
        }
    }
}
