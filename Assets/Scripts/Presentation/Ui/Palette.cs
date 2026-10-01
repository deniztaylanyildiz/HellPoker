using HellPoker.Presentation.Abstractions;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    internal static class Palette
    {
        public static readonly Color Background = new Color(0.07f, 0.015f, 0.015f);
        public static readonly Color Felt = new Color(0.17f, 0.03f, 0.025f);
        public static readonly Color Ember = new Color(1f, 0.45f, 0.1f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.3f);
        public static readonly Color PaleGold = new Color(0.93f, 0.82f, 0.58f);
        public static readonly Color Bone = new Color(0.96f, 0.93f, 0.86f);
        public static readonly Color Ink = new Color(0.1f, 0.08f, 0.08f);
        public static readonly Color SepiaInk = new Color(0.22f, 0.12f, 0.06f);
        public static readonly Color Blood = new Color(0.72f, 0.05f, 0.05f);
        public static readonly Color CardBack = new Color(0.42f, 0.03f, 0.03f);
        public static readonly Color CardBackInner = new Color(0.25f, 0.015f, 0.015f);
        public static readonly Color MutedText = new Color(0.8f, 0.66f, 0.56f);
        public static readonly Color Button = new Color(0.55f, 0.08f, 0.05f);
        public static readonly Color Slot = new Color(0.12f, 0.02f, 0.02f);
        public static readonly Color Forgiven = new Color(0.6f, 0.95f, 0.55f);
        public static readonly Color Fold = new Color(0.22f, 0.07f, 0.06f);

        /// <summary>Multiplied over the backdrop and the table in the final stretch: everything runs hotter.</summary>
        public static readonly Color HellTint = new Color(1f, 0.62f, 0.45f);
        public static readonly Color HellFeltTint = new Color(1f, 0.78f, 0.55f);

        public static Color For(Tone tone)
        {
            switch (tone)
            {
                case Tone.Muted: return MutedText;
                case Tone.Good: return Forgiven;
                case Tone.Bad:
                case Tone.Warning: return Ember;
                case Tone.Triumph: return Gold;
                case Tone.Doom: return Blood;
                default: return Bone;
            }
        }

        /// <summary>The light behind the dealer's portrait as they speak.</summary>
        public static Color AuraFor(Tone tone)
        {
            switch (tone)
            {
                case Tone.Good: return new Color(0.45f, 0.6f, 1f);
                case Tone.Bad: return new Color(1f, 0.3f, 0.05f);
                case Tone.Warning: return new Color(1f, 0.55f, 0.1f);
                case Tone.Doom: return new Color(0.85f, 0f, 0f);
                case Tone.Triumph: return Gold;
                default: return new Color(1f, 0.4f, 0.15f);
            }
        }
    }
}
