using System;
using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>The animated states of the player's class portrait on a chapter's table (Demo 3.1).</summary>
    public enum SinnerAnimation
    {
        /// <summary>Breathing, a blink, the class's own small gesture (loops, ~5 FPS).</summary>
        Idle,

        /// <summary>A short smile (once).</summary>
        Happy,

        /// <summary>A burst of joy (once).</summary>
        Joy,

        /// <summary>A wince (once).</summary>
        Hurt,

        /// <summary>Tense: a held breath, a bead of sweat; replaces Idle (loops).</summary>
        Tense
    }

    /// <summary>
    /// Finds the strips of a class's portrait: Art/Sinners/&lt;classId&gt;/&lt;state&gt;.png (64×64 frames side by side). Never fails: a missing
    /// state falls back to Idle, a missing Idle to the class's single portrait (Art/Sinners/&lt;classId&gt;.png, 48×48), and with nothing at
    /// all it returns null — the view then shows a flat colour.
    /// </summary>
    public sealed class SinnerAnimationLibrary
    {
        public const int FrameSize = 64;
        public const float DefaultFps = 8f;
        public const float IdleFps = 5f;
        public const float TenseFps = 6f;

        private readonly Func<string, Texture2D> _load;
        private readonly Dictionary<string, Sprite[]> _sheets = new Dictionary<string, Sprite[]>();

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Sinners/king/idle"); null when missing.</param>
        public SinnerAnimationLibrary(Func<string, Texture2D> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        public static string PathOf(string classId, SinnerAnimation animation) => $"Sinners/{classId}/{animation.ToString().ToLowerInvariant()}";

        public static string PortraitPathOf(string classId) => $"Sinners/{classId}";

        public static bool Loops(SinnerAnimation animation) => animation == SinnerAnimation.Idle || animation == SinnerAnimation.Tense;

        public static float FpsOf(SinnerAnimation animation) =>
            animation == SinnerAnimation.Idle ? IdleFps : animation == SinnerAnimation.Tense ? TenseFps : DefaultFps;

        /// <summary>The clip for an animation, following the fallback chain. Null only when the class has no art at all.</summary>
        public SpriteClip Get(string classId, SinnerAnimation animation)
        {
            if (string.IsNullOrEmpty(classId)) return null;

            bool loop = Loops(animation);
            Sprite[] frames = Sheet(PathOf(classId, animation));
            if (frames != null)
                return new SpriteClip(frames, FpsOf(animation), loop);

            if (animation != SinnerAnimation.Idle)
            {
                // Missing state: stand in with idle, but keep the requested looping so one-shots still end.
                SpriteClip idle = Get(classId, SinnerAnimation.Idle);
                return idle?.WithLoop(loop);
            }

            Sprite[] portrait = Sheet(PortraitPathOf(classId));
            return portrait != null ? new SpriteClip(new[] { portrait[0] }, IdleFps, loop) : null;
        }

        private Sprite[] Sheet(string path)
        {
            if (_sheets.TryGetValue(path, out Sprite[] frames))
                return frames;

            Texture2D texture = null;
            try
            {
                texture = _load(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: could not load '{path}': {exception.Message}");
            }

            frames = texture != null ? SpriteSheet.Slice(texture) : null;
            _sheets[path] = frames;
            return frames;
        }
    }
}
