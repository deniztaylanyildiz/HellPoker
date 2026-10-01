using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// The one clock every view animation runs on. <see cref="Speed"/> is the player's animation speed setting;
    /// while <see cref="IsSkipping"/> is set, tweens and waits finish at once (a sequencer fast-forwarding its queue).
    /// </summary>
    public static class AnimationClock
    {
        private static float _speed = 1f;
        private static int _skipping;

        /// <summary>1 = normal; higher is faster. Never below 0.1.</summary>
        public static float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0.1f, value);
        }

        public static bool IsSkipping => _skipping > 0;

        /// <summary>Seconds of animation time that passed this frame.</summary>
        public static float DeltaTime => Time.deltaTime * _speed;

        /// <summary>Real seconds a step of <paramref name="seconds"/> animation time takes at the current speed.</summary>
        public static float Scaled(float seconds) => seconds / _speed;

        internal static void BeginSkip() => _skipping++;

        internal static void EndSkip() => _skipping = Mathf.Max(0, _skipping - 1);
    }
}
