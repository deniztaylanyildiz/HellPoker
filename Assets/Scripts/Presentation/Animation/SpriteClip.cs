using System;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>A run of sprite frames played at a fixed rate, looping or once.</summary>
    public sealed class SpriteClip
    {
        public Sprite[] Frames { get; }
        public float Fps { get; }
        public bool Loop { get; }

        public SpriteClip(Sprite[] frames, float fps, bool loop)
        {
            if (frames == null || frames.Length == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
            if (fps <= 0f) throw new ArgumentOutOfRangeException(nameof(fps));
            Frames = frames;
            Fps = fps;
            Loop = loop;
        }

        /// <summary>Seconds for one pass through all frames.</summary>
        public float Duration => Frames.Length / Fps;

        /// <summary>The frame to show after <paramref name="seconds"/>; a one-shot clip holds its last frame and reports finished.</summary>
        public int FrameAt(float seconds, out bool finished)
        {
            int frame = Mathf.FloorToInt(Mathf.Max(0f, seconds) * Fps);
            if (Loop)
            {
                finished = false;
                return frame % Frames.Length;
            }

            finished = frame >= Frames.Length;
            return Mathf.Min(frame, Frames.Length - 1);
        }

        public SpriteClip WithLoop(bool loop) => loop == Loop ? this : new SpriteClip(Frames, Fps, loop);
    }

    /// <summary>Cuts a horizontal sprite sheet into square frames: frame count = width / height.</summary>
    public static class SpriteSheet
    {
        public const float PixelsPerUnit = 100f;

        public static Sprite[] Slice(Texture2D sheet)
        {
            if (sheet == null) return null;
            sheet.filterMode = FilterMode.Point;

            int size = sheet.height;
            int count = Mathf.Max(1, sheet.width / Mathf.Max(1, size));
            int width = sheet.width >= size ? size : sheet.width;
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(sheet, new Rect(i * width, 0, width, size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
                frames[i].name = $"{sheet.name}_{i}";
            }
            return frames;
        }

        /// <summary>Cuts a strip into frames of a given width (for strips whose frames are not square).</summary>
        public static Sprite[] Slice(Texture2D sheet, int frameWidth)
        {
            if (sheet == null || frameWidth <= 0) return null;
            sheet.filterMode = FilterMode.Point;

            int count = Mathf.Max(1, sheet.width / frameWidth);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(sheet, new Rect(i * frameWidth, 0, frameWidth, sheet.height), new Vector2(0.5f, 0.5f),
                    PixelsPerUnit, 0, SpriteMeshType.FullRect);
            return frames;
        }
    }
}
