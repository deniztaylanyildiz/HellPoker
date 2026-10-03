using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// A small looping strip laid over a still backdrop (a curtain's folds, a chain, a glint), placed in screen pixels.
    /// </summary>
    public sealed class BackdropLayer
    {
        public SpriteClip Clip { get; }

        /// <summary>Top-left corner on the 480×270 screen.</summary>
        public Vector2Int Position { get; }

        /// <summary>Frame size in pixels.</summary>
        public Vector2Int Size { get; }

        /// <summary>Copies laid side by side, each one frame wide.</summary>
        public int Repeat { get; }

        /// <summary>Sideways drift in pixels per second (a bird crossing the sky); 0 for none.</summary>
        public float Drift { get; }

        /// <summary>A drifting layer comes round again every this many pixels.</summary>
        public int Wrap { get; }

        /// <summary>The loop starts this many frames in (so a row of glints does not blink all at once).</summary>
        public int Offset { get; }

        public BackdropLayer(SpriteClip clip, Vector2Int position, Vector2Int size, int repeat = 1, float drift = 0f, int wrap = 0, int offset = 0)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            Position = position;
            Size = size;
            Repeat = Mathf.Max(1, repeat);
            Drift = drift;
            Wrap = wrap;
            Offset = offset;
        }

        /// <summary>The frame shown <paramref name="seconds"/> into the loop.</summary>
        public int FrameAt(float seconds) => (Clip.FrameAt(seconds, out _) + Offset) % Clip.Frames.Length;

        /// <summary>The left edge after <paramref name="seconds"/> of drift, in whole pixels, coming round every <see cref="Wrap"/> px.</summary>
        public int XAt(float seconds)
        {
            if (Drift == 0f || Wrap <= 0) return Position.x;
            int moved = Mathf.FloorToInt(seconds * Drift);
            int x = (Position.x + moved + Size.x) % Wrap;
            if (x < 0) x += Wrap;
            return x - Size.x;
        }
    }

    /// <summary>What kind of pixels a particle emitter lets drift (the view picks colours and speeds).</summary>
    public enum ParticleKind
    {
        /// <summary>Sparks rising and cooling (Lucifer's hall, the title screen).</summary>
        Embers,

        /// <summary>Gold dust floating up slowly (Mammon's vault).</summary>
        Motes,

        /// <summary>Pale wisps wandering low (Lilith's garden).</summary>
        Wisps
    }

    /// <summary>A rectangle of the screen where a number of particles drift.</summary>
    public sealed class BackdropParticles
    {
        public ParticleKind Kind { get; }
        public RectInt Area { get; }
        public int Count { get; }

        public BackdropParticles(ParticleKind kind, RectInt area, int count)
        {
            Kind = kind;
            Area = area;
            Count = Mathf.Max(0, count);
        }
    }

    /// <summary>
    /// The moving parts of a backdrop: looping layers over the still picture and particle emitters. Read from a manifest the
    /// art tools write next to the art (motion.txt):
    /// <code>
    /// layer &lt;file&gt; &lt;x&gt; &lt;y&gt; &lt;frames&gt; &lt;fps&gt; &lt;repeat&gt; &lt;drift&gt; &lt;wrap&gt; &lt;offset&gt;
    /// particles &lt;kind&gt; &lt;x&gt; &lt;y&gt; &lt;w&gt; &lt;h&gt; &lt;count&gt;
    /// </code>
    /// A layer whose strip cannot be loaded, an unknown line or a broken one is skipped: a backdrop never fails to show.
    /// </summary>
    public sealed class BackdropMotion
    {
        public static readonly BackdropMotion None = new BackdropMotion(Array.Empty<BackdropLayer>(), Array.Empty<BackdropParticles>());

        public IReadOnlyList<BackdropLayer> Layers { get; }
        public IReadOnlyList<BackdropParticles> Particles { get; }

        public bool IsEmpty => Layers.Count == 0 && Particles.Count == 0;

        public BackdropMotion(IReadOnlyList<BackdropLayer> layers, IReadOnlyList<BackdropParticles> particles)
        {
            Layers = layers ?? Array.Empty<BackdropLayer>();
            Particles = particles ?? Array.Empty<BackdropParticles>();
        }

        /// <param name="manifest">The manifest's text; null or empty for a backdrop without motion.</param>
        /// <param name="strip">Loads a layer's strip by its file name and cuts it into this many frames; null when missing.</param>
        public static BackdropMotion Parse(string manifest, Func<string, int, Sprite[]> strip)
        {
            if (string.IsNullOrWhiteSpace(manifest) || strip == null) return None;

            var layers = new List<BackdropLayer>();
            var particles = new List<BackdropParticles>();
            foreach (string raw in manifest.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (parts[0] == "layer" && parts.Length >= 10)
                    {
                        int frames = Int(parts[4]);
                        Sprite[] sprites = strip(parts[1], frames);
                        if (sprites == null || sprites.Length == 0) continue;
                        Rect rect = sprites[0].rect;
                        var clip = new SpriteClip(sprites, Float(parts[5]), loop: true);
                        layers.Add(new BackdropLayer(clip, new Vector2Int(Int(parts[2]), Int(parts[3])),
                            new Vector2Int((int)rect.width, (int)rect.height), Int(parts[6]), Float(parts[7]), Int(parts[8]), Int(parts[9])));
                    }
                    else if (parts[0] == "particles" && parts.Length >= 7 && TryKind(parts[1], out ParticleKind kind))
                    {
                        particles.Add(new BackdropParticles(kind, new RectInt(Int(parts[2]), Int(parts[3]), Int(parts[4]), Int(parts[5])),
                            Int(parts[6])));
                    }
                }
                catch (Exception exception) when (exception is FormatException || exception is OverflowException || exception is ArgumentException)
                {
                    Debug.LogWarning($"Hell Poker: skipped a broken backdrop line '{line}': {exception.Message}");
                }
            }
            return layers.Count == 0 && particles.Count == 0 ? None : new BackdropMotion(layers, particles);
        }

        private static bool TryKind(string text, out ParticleKind kind)
        {
            switch (text)
            {
                case "embers": kind = ParticleKind.Embers; return true;
                case "motes": kind = ParticleKind.Motes; return true;
                case "wisps": kind = ParticleKind.Wisps; return true;
                default: kind = default; return false;
            }
        }

        private static int Int(string text) => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);

        private static float Float(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
