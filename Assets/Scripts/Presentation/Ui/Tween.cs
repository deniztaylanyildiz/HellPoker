using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    internal static class Tween
    {
        /// <summary>
        /// Calls <paramref name="apply"/> with an eased 0→1 value over <paramref name="duration"/> seconds of animation time
        /// (<see cref="Animation.AnimationClock"/>); jumps straight to 1 when the animations are being skipped.
        /// </summary>
        public static IEnumerator Run(float duration, Action<float> apply)
        {
            for (float elapsed = 0f; elapsed < duration && !Animation.AnimationClock.IsSkipping; elapsed += Animation.AnimationClock.DeltaTime)
            {
                apply(EaseOutCubic(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        /// <summary>Waits <paramref name="seconds"/> of animation time; no wait at all when skipping.</summary>
        public static IEnumerator Wait(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds && !Animation.AnimationClock.IsSkipping; elapsed += Animation.AnimationClock.DeltaTime)
                yield return null;
        }

        /// <summary>
        /// Steps through nested enumerators in place, so a routine that yields another routine can be driven manually
        /// (sequenced or run in parallel). Only real yield instructions (null, WaitForSeconds...) come out.
        /// </summary>
        public static IEnumerator Flatten(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return top.Current;
            }
        }

        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
