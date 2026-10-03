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
    /// Finds the backdrops of the demons' halls: the still picture Art/Backgrounds/&lt;dealerId&gt;/&lt;mode&gt;.png (480 px
    /// frames; one frame today, an older multi-frame strip still plays) and its motion — the looping layers and particles
    /// listed in Art/Backgrounds/&lt;dealerId&gt;/motion.txt, each layer's strip in the mode's own look (&lt;layer&gt;_&lt;mode&gt;)
    /// or the normal one.
    /// Never fails: a missing variant falls back to the hall's normal look, a missing hall to the plain stone wall
    /// (Ui/background_hell for the burning look, Ui/background otherwise), and with nothing at all it returns null —
    /// the view then shows a flat dark colour. Missing motion is no motion.
    /// </summary>
    public sealed class SalonLibrary
    {
        public const int FrameWidth = 480;
        public const float Fps = 4f;

        private readonly Func<string, Texture2D> _load;
        private readonly Func<string, string> _loadText;
        private readonly Dictionary<string, Sprite[]> _strips = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, BackdropMotion> _motions = new Dictionary<string, BackdropMotion>();

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Backgrounds/mammon/normal"); null when missing.</param>
        /// <param name="loadText">Loads a text by its path under the art folder (the motion manifests); null for halls without motion.</param>
        public SalonLibrary(Func<string, Texture2D> load, Func<string, string> loadText = null)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _loadText = loadText;
        }

        public static string MotionPathOf(string dealerId) => $"Backgrounds/{dealerId}/motion";

        /// <summary>
        /// Loads every look of these halls now (pictures and moving layers), so showing a hall for the first time never
        /// stalls a frame on loading its art.
        /// </summary>
        public void Preload(IEnumerable<string> dealerIds)
        {
            foreach (string id in dealerIds)
                foreach (SalonMode mode in (SalonMode[])Enum.GetValues(typeof(SalonMode)))
                {
                    Get(id, mode);
                    Motion(id, mode);
                }
        }

        /// <summary>The hall's moving parts in a mood; <see cref="BackdropMotion.None"/> when it has none (or no art at all).</summary>
        public BackdropMotion Motion(string dealerId, SalonMode mode)
        {
            if (string.IsNullOrEmpty(dealerId) || _loadText == null) return BackdropMotion.None;
            string key = dealerId + "/" + mode;
            if (_motions.TryGetValue(key, out BackdropMotion motion)) return motion;

            string folder = $"Backgrounds/{dealerId}/";
            string suffix = mode == SalonMode.Normal ? "" : "_" + mode.ToString().ToLowerInvariant();
            motion = BackdropMotion.Parse(LoadText(MotionPathOf(dealerId)),
                (file, frames) => Frames(folder + file + suffix, frames) ?? Frames(folder + file, frames));
            _motions[key] = motion;
            return motion;
        }

        private string LoadText(string path)
        {
            try
            {
                return _loadText(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: could not load '{path}': {exception.Message}");
                return null;
            }
        }

        /// <summary>A layer strip cut into its frames (the frame width is the strip's width over the count).</summary>
        private Sprite[] Frames(string path, int count)
        {
            string key = path + "#" + count;
            if (_strips.TryGetValue(key, out Sprite[] frames)) return frames;
            Texture2D texture = null;
            try
            {
                texture = _load(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: could not load '{path}': {exception.Message}");
            }
            frames = texture != null && count > 0 ? SpriteSheet.Slice(texture, texture.width / count) : null;
            _strips[key] = frames;
            return frames;
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
