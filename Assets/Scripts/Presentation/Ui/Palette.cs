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
        public static readonly Color Bone = new Color(0.96f, 0.93f, 0.86f);
        public static readonly Color Ink = new Color(0.1f, 0.08f, 0.08f);
        public static readonly Color Blood = new Color(0.78f, 0.06f, 0.06f);
        public static readonly Color CardBack = new Color(0.42f, 0.03f, 0.03f);
        public static readonly Color CardBackInner = new Color(0.25f, 0.015f, 0.015f);
        public static readonly Color MutedText = new Color(0.78f, 0.62f, 0.56f);
        public static readonly Color Button = new Color(0.55f, 0.08f, 0.05f);
        public static readonly Color ButtonDisabled = new Color(0.25f, 0.12f, 0.1f);
        public static readonly Color Slot = new Color(0.12f, 0.02f, 0.02f);
        public static readonly Color Forgiven = new Color(0.55f, 0.95f, 0.55f);
        public static readonly Color HellBackground = new Color(0.2f, 0.03f, 0.005f);
        public static readonly Color HellFelt = new Color(0.32f, 0.06f, 0.02f);
        public static readonly Color Fold = new Color(0.22f, 0.07f, 0.06f);

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
    }
}
