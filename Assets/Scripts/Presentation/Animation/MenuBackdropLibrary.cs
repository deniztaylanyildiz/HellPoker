using System;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// The title screen's backdrop: the still picture Art/Ui/menu.png (480 px frames; one today, an older strip still plays)
    /// and its motion — his eyes, the serpent, the fire's edges, glints and embers — listed in Art/Ui/menu_motion.txt.
    /// Never fails: without it the plain stone wall (Ui/background) stands in, and with nothing at all it returns null —
    /// the view then shows a flat dark colour; missing motion is no motion. The rules, settings and records screens use
    /// the same backdrop.
    /// </summary>
    public sealed class MenuBackdropLibrary
    {
        public const string MenuPath = "Ui/menu";
        public const string MotionPath = "Ui/menu_motion";
        public const string StoneWallPath = "Ui/background";
        public const int FrameWidth = 480;
        public const float Fps = 4f;

        private readonly Func<string, Texture2D> _load;
        private readonly Func<string, string> _loadText;
        private SpriteClip _clip;
        private bool _loaded;
        private BackdropMotion _motion;

        /// <param name="load">Loads a texture by its path under the art folder (e.g. "Ui/menu"); null when missing.</param>
        /// <param name="loadText">Loads a text by its path under the art folder (the motion manifest); null for no motion.</param>
        public MenuBackdropLibrary(Func<string, Texture2D> load, Func<string, string> loadText = null)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _loadText = loadText;
        }

        /// <summary>The backdrop's moving parts; <see cref="BackdropMotion.None"/> without a manifest.</summary>
        public BackdropMotion Motion()
        {
            if (_motion != null) return _motion;
            string manifest = null;
            try
            {
                manifest = _loadText?.Invoke(MotionPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: could not load '{MotionPath}': {exception.Message}");
            }
            _motion = BackdropMotion.Parse(manifest, (file, frames) =>
            {
                Texture2D texture = null;
                try
                {
                    texture = _load("Ui/" + file);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Hell Poker: could not load 'Ui/{file}': {exception.Message}");
                }
                return texture != null && frames > 0 ? SpriteSheet.Slice(texture, texture.width / frames) : null;
            });
            return _motion;
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
