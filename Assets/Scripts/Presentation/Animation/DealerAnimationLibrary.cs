using System;
using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>The animated states of a demon dealer's portrait.</summary>
    public enum DealerAnimation
    {
        /// <summary>Breathing, the odd blink (loops).</summary>
        Idle,

        /// <summary>Mouth moving while a line types out (loops).</summary>
        Talk,

        /// <summary>Laughing at the player's loss (once).</summary>
        Gloat,

        /// <summary>Fuming at the player's win (once).</summary>
        Angry,

        /// <summary>A sly look, eyes flaring, as the house raises back (once).</summary>
        ReRaise,

        /// <summary>The final stretch: burning, intense; replaces Idle (loops).</summary>
        Final
    }

    /// <summary>
    /// Finds the sprite sheets of a dealer's animations: Art/Demons/&lt;dealerId&gt;/&lt;state&gt;.png, frames side by side.
    /// Never fails: a missing state falls back to Idle, a missing Idle to a single portrait (Art/Demons/&lt;dealerId&gt;.png),
    /// and with nothing at all it returns null — the view then shows a flat colour.
    /// </summary>
    public sealed class DealerAnimationLibrary
    {
        public const float DefaultFps = 8f;
        public const float IdleFps = 5f;

        private readonly Func<string, Texture2D> _load;
        private readonly Dictionary<string, Sprite[]> _sheets = new Dictionary<string, Sprite[]>();

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Demons/mammon/idle"); null when missing.</param>
        public DealerAnimationLibrary(Func<string, Texture2D> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        public static string PathOf(string dealerId, DealerAnimation animation) => $"Demons/{dealerId}/{animation.ToString().ToLowerInvariant()}";

        public static string PortraitPathOf(string dealerId) => $"Demons/{dealerId}";

        public static bool Loops(DealerAnimation animation)
        {
            return animation == DealerAnimation.Idle || animation == DealerAnimation.Talk || animation == DealerAnimation.Final;
        }

        /// <summary>The clip for an animation, following the fallback chain. Null only when the dealer has no art at all.</summary>
        public SpriteClip Get(string dealerId, DealerAnimation animation)
        {
            if (string.IsNullOrEmpty(dealerId)) return null;

            bool loop = Loops(animation);
            Sprite[] frames = Sheet(PathOf(dealerId, animation));
            if (frames != null)
                return new SpriteClip(frames, animation == DealerAnimation.Idle ? IdleFps : DefaultFps, loop);

            if (animation != DealerAnimation.Idle)
            {
                // Missing state: stand in with idle, but keep the requested looping so one-shots still end.
                SpriteClip idle = Get(dealerId, DealerAnimation.Idle);
                return idle?.WithLoop(loop);
            }

            Sprite[] portrait = Sheet(PortraitPathOf(dealerId));
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
