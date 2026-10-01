using System;
using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>The look of a demon's hall: as it is, burning (final stretch), or drained cold (soul on the table).</summary>
    public enum SalonMode
    {
        Normal,
        Hell,
        Soul
    }

    /// <summary>
    /// Finds the backdrops of the demons' halls: Art/Backgrounds/&lt;dealerId&gt;/&lt;mode&gt;.png, frames of 480 px side by side.
    /// Never fails: a missing variant falls back to the hall's normal look, a missing hall to the plain stone wall
    /// (Ui/background_hell for the burning look, Ui/background otherwise), and with nothing at all it returns null —
    /// the view then shows a flat dark colour.
    /// </summary>
    public sealed class SalonLibrary
    {
        public const int FrameWidth = 480;
        public const float Fps = 4f;

        private readonly Func<string, Texture2D> _load;
        private readonly Dictionary<string, Sprite[]> _strips = new Dictionary<string, Sprite[]>();

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Backgrounds/mammon/normal"); null when missing.</param>
        public SalonLibrary(Func<string, Texture2D> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        public static string PathOf(string dealerId, SalonMode mode) => $"Backgrounds/{dealerId}/{mode.ToString().ToLowerInvariant()}";

        public const string StoneWall = "Ui/background";
        public const string BurningStoneWall = "Ui/background_hell";

        /// <summary>The backdrop for a hall in a mood, following the fallback chain. Null only when there is no art at all.</summary>
        public SpriteClip Get(string dealerId, SalonMode mode)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(dealerId))
            {
                candidates.Add(PathOf(dealerId, mode));
                if (mode != SalonMode.Normal)
                    candidates.Add(PathOf(dealerId, SalonMode.Normal));
            }
            if (mode == SalonMode.Hell)
                candidates.Add(BurningStoneWall);
            candidates.Add(StoneWall);

            foreach (string path in candidates)
            {
                Sprite[] frames = Strip(path);
                if (frames != null)
                    return new SpriteClip(frames, Fps, loop: true);
            }
            return null;
        }

        private Sprite[] Strip(string path)
        {
            if (_strips.TryGetValue(path, out Sprite[] frames))
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

            frames = texture != null ? SpriteSheet.Slice(texture, Mathf.Min(FrameWidth, texture.width)) : null;
            _strips[path] = frames;
            return frames;
        }
    }
}
