using System.Collections.Generic;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The moving parts of a backdrop, over its still picture: looping layers (frames swapped at their own rate, a drifting
    /// one moved a whole pixel at a time) and particles — single pixels stepping a whole pixel at a time, on the animation
    /// clock (the speed setting). Nothing here catches clicks. Showing new motion replaces the old; the same motion again
    /// changes nothing (no restart, no flicker).
    /// </summary>
    public sealed class BackdropMotionView : MonoBehaviour
    {
        private sealed class LayerCopy
        {
            public BackdropLayer Layer;
            public Image Image;
            public int Index;
            public int Shown = -1;
            public int X = int.MinValue;
        }

        private sealed class Particle
        {
            public Image Image;
            public BackdropParticles Emitter;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public int Shade = -1;
            public Vector2Int Drawn = new Vector2Int(int.MinValue, int.MinValue);
        }

        private readonly List<LayerCopy> _layers = new List<LayerCopy>();
        private readonly List<Particle> _particles = new List<Particle>();
        private RectTransform _root;
        private BackdropMotion _motion;
        private SalonMode _mode;
        private float _time;
        private System.Random _random = new System.Random(666);

        /// <summary>The motion on screen (for tests).</summary>
        public BackdropMotion Motion => _motion;

        /// <summary>How many layer images and particles are alive (for tests and the FPS check).</summary>
        public int LayerImages => _layers.Count;
        public int ParticleCount => _particles.Count;

        /// <summary>A screen-sized holder for the motion, under <paramref name="parent"/> (put it behind anything that must stay on top).</summary>
        public static BackdropMotionView Create(Transform parent)
        {
            RectTransform root = UiFactory.CreateRect("Motion", parent).Stretch();
            // A canvas of its own: pixels moving every frame rebuild only this, never the table drawn over it.
            root.gameObject.AddComponent<Canvas>();
            var view = root.gameObject.AddComponent<BackdropMotionView>();
            view._root = root;
            return view;
        }

        public void Show(BackdropMotion motion, SalonMode mode = SalonMode.Normal)
        {
            motion = motion ?? BackdropMotion.None;
            if (ReferenceEquals(motion, _motion) && mode == _mode) return;
            _motion = motion;
            _mode = mode;
            Clear();

            foreach (BackdropLayer layer in motion.Layers)
            {
                for (int i = 0; i < layer.Repeat; i++)
                {
                    Image image = UiFactory.CreateImage("Layer", _root, Color.white);
                    image.raycastTarget = false;
                    _layers.Add(new LayerCopy { Layer = layer, Image = image, Index = i });
                }
            }
            foreach (BackdropParticles emitter in motion.Particles)
            {
                for (int i = 0; i < emitter.Count; i++)
                {
                    Image image = UiFactory.CreateImage("Particle", _root, Color.white);
                    image.raycastTarget = false;
                    var particle = new Particle { Image = image, Emitter = emitter };
                    Spawn(particle, anywhere: true);
                    _particles.Add(particle);
                }
            }
            Step(0f);
        }

        private void Clear()
        {
            foreach (LayerCopy copy in _layers) Destroy(copy.Image.gameObject);
            foreach (Particle particle in _particles) Destroy(particle.Image.gameObject);
            _layers.Clear();
            _particles.Clear();
        }

        private void Update()
        {
            // Layers run on real time (they are the hall breathing); particles follow the animation speed setting.
            _time += Time.unscaledDeltaTime;
            Step(AnimationClock.DeltaTime);
        }

        private void Step(float particleSeconds)
        {
            foreach (LayerCopy copy in _layers)
            {
                BackdropLayer layer = copy.Layer;
                int frame = layer.FrameAt(_time);
                if (frame != copy.Shown)
                {
                    copy.Shown = frame;
                    copy.Image.sprite = layer.Clip.Frames[frame];
                }
                int x = layer.XAt(_time) + copy.Index * layer.Size.x;
                if (x != copy.X)
                {
                    copy.X = x;
                    copy.Image.rectTransform.PlaceTL(x, layer.Position.y, layer.Size.x, layer.Size.y);
                }
            }

            foreach (Particle particle in _particles)
            {
                particle.Age += particleSeconds;
                particle.Position += particle.Velocity * particleSeconds;
                RectInt area = particle.Emitter.Area;
                if (particle.Age >= particle.Life || particle.Position.y < area.yMin || particle.Position.x < area.xMin - 2 ||
                    particle.Position.x > area.xMax + 2)
                    Spawn(particle, anywhere: false);
                Draw(particle);
            }
        }

        /// <summary>A particle (re)born: at the bottom of its area — or, the first time, anywhere in it, so the air is never empty.</summary>
        private void Spawn(Particle particle, bool anywhere)
        {
            RectInt area = particle.Emitter.Area;
            float x = area.xMin + (float)_random.NextDouble() * area.width;
            float y = anywhere ? area.yMin + (float)_random.NextDouble() * area.height : area.yMax - 1;
            particle.Position = new Vector2(x, y);
            particle.Age = 0f;
            switch (particle.Emitter.Kind)
            {
                case ParticleKind.Embers:
                    // Up 12-26 px a second, wandering a little; they cool on the way.
                    particle.Velocity = new Vector2(((float)_random.NextDouble() - 0.5f) * 4f, -12f - (float)_random.NextDouble() * 14f);
                    particle.Life = area.height / -particle.Velocity.y;
                    break;
                case ParticleKind.Motes:
                    particle.Velocity = new Vector2(((float)_random.NextDouble() - 0.5f) * 2f, -3f - (float)_random.NextDouble() * 4f);
                    particle.Life = 4f + (float)_random.NextDouble() * 6f;
                    break;
                default:
                    particle.Velocity = new Vector2(((float)_random.NextDouble() - 0.5f) * 6f, -1f - (float)_random.NextDouble() * 2f);
                    particle.Life = 5f + (float)_random.NextDouble() * 5f;
                    break;
            }
            if (anywhere)
                particle.Age = (float)_random.NextDouble() * particle.Life;
            particle.Shade = -1;
        }

        /// <summary>Whole pixels only: a particle moves when it has travelled a full pixel, and recolours only as it cools.</summary>
        private void Draw(Particle particle)
        {
            Color[] ramp = RampOf(particle.Emitter.Kind);
            float t = particle.Life > 0f ? Mathf.Clamp01(particle.Age / particle.Life) : 0f;
            int shade = Mathf.Min(ramp.Length - 1, Mathf.FloorToInt(t * ramp.Length));
            if (shade != particle.Shade)
            {
                particle.Shade = shade;
                particle.Image.color = ramp[shade];
            }

            var at = new Vector2Int(Mathf.FloorToInt(particle.Position.x), Mathf.FloorToInt(particle.Position.y));
            if (at == particle.Drawn) return;
            particle.Drawn = at;
            particle.Image.rectTransform.PlaceTL(at.x, at.y, 1, 1);
        }

        private static readonly Color[] EmberRamp = { Palette.Spark, Palette.Amber, Palette.Ember, Palette.Hell, Palette.Red };
        private static readonly Color[] ColdEmberRamp = { Palette.White, Palette.LilacLight, Palette.Lilac, Palette.Violet };
        private static readonly Color[] MoteRamp = { Palette.GoldLight, Palette.Gold, Palette.GoldLight };
        private static readonly Color[] HotMoteRamp = { Palette.Amber, Palette.Ember, Palette.Hell };
        private static readonly Color[] ColdMoteRamp = { Palette.Silver, Palette.White, Palette.Silver };
        private static readonly Color[] WispRamp = { Palette.LilacLight, Palette.Lilac, Palette.Violet };
        private static readonly Color[] HotWispRamp = { Palette.Ember, Palette.Hell, Palette.Red };

        /// <summary>A particle's colours over its life, from the art palette — cooled down for a soul's chill, hotter in Hell.</summary>
        private Color[] RampOf(ParticleKind kind)
        {
            switch (kind)
            {
                case ParticleKind.Embers:
                    return _mode == SalonMode.Soul ? ColdEmberRamp : EmberRamp;
                case ParticleKind.Motes:
                    return _mode == SalonMode.Soul ? ColdMoteRamp : _mode == SalonMode.Hell ? HotMoteRamp : MoteRamp;
                default:
                    return _mode == SalonMode.Hell ? HotWispRamp : WispRamp;
            }
        }
    }
}
