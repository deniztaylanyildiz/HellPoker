using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    internal static class Tween
    {
        /// <summary>Calls <paramref name="apply"/> with an eased 0→1 value over <paramref name="duration"/> seconds.</summary>
        public static IEnumerator Run(float duration, Action<float> apply)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                apply(EaseOutCubic(elapsed / duration));
                yield return null;
            }
            apply(1f);
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
