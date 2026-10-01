using System;
using System.Collections;
using System.Collections.Generic;
using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Plays view updates strictly in the order they were requested. Instant updates run immediately when nothing is
    /// animating, otherwise they wait their turn — so a result message never appears before the cards are turned.
    /// </summary>
    public sealed class AnimationSequencer : MonoBehaviour
    {
        private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();
        private bool _running;

        public bool IsBusy => _running || _queue.Count > 0;

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
            if (!_running)
                StartCoroutine(Run());
        }

        public void Wait(float seconds)
        {
            Play(WaitRoutine(seconds));
        }

        private IEnumerator Run()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                IEnumerator step = Tween.Flatten(_queue.Dequeue());
                while (step.MoveNext())
                    yield return step.Current;
            }
            _running = false;
        }

        private static IEnumerator Instant(Action update)
        {
            update();
            yield break;
        }

        private static IEnumerator WaitRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        private void OnDisable()
        {
            _queue.Clear();
            _running = false;
        }
    }
}
