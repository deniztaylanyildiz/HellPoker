using System;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// The title screen's backdrop: Art/Ui/menu.png, frames of 480 px side by side (the bottom of Hell, his eyes in the sky).
    /// Never fails: without it the plain stone wall (Ui/background) stands in, and with nothing at all it returns null —
    /// the view then shows a flat dark colour. The rules, settings and records screens use the same backdrop.
    /// </summary>
    public sealed class MenuBackdropLibrary
    {
        public const string MenuPath = "Ui/menu";
        public const string StoneWallPath = "Ui/background";
        public const int FrameWidth = 480;
        public const float Fps = 4f;

        private readonly Func<string, Texture2D> _load;
        private SpriteClip _clip;
        private bool _loaded;

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Ui/menu"); null when missing.</param>
        public MenuBackdropLibrary(Func<string, Texture2D> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        /// <summary>The backdrop clip, looping; null only when there is no art at all.</summary>
        public SpriteClip Get()
        {
            if (_loaded) return _clip;
            _loaded = true;

            Sprite[] frames = Slice(MenuPath) ?? Slice(StoneWallPath);
            _clip = frames != null ? new SpriteClip(frames, Fps, loop: true) : null;
            return _clip;
        }

        private Sprite[] Slice(string path)
        {
            Texture2D texture = null;
            try
            {
                texture = _load(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: could not load '{path}': {exception.Message}");
            }

            Sprite[] frames = texture != null ? SpriteSheet.Slice(texture, FrameWidth) : null;
            return frames != null && frames.Length > 0 ? frames : null;
        }
    }
}
