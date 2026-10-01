using HellPoker.Presentation.Abstractions;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    /// <summary>UI colours, all taken from the art palette in Tools/ArtGen/pixel.py so text and sprites match.</summary>
    internal static class Palette
    {
        public static readonly Color Black = Hex(0x0b0610);
        public static readonly Color Night = Hex(0x160b1e);
        public static readonly Color Dusk = Hex(0x22122c);
        public static readonly Color Plum = Hex(0x321a3c);
        public static readonly Color Violet = Hex(0x4a2856);
        public static readonly Color BloodDark = Hex(0x3a0a10);
        public static readonly Color Crimson = Hex(0x8c1a1a);
        public static readonly Color Red = Hex(0xb8261c);
        public static readonly Color Hell = Hex(0xe0401c);
        public static readonly Color Ember = Hex(0xff7a1c);
        public static readonly Color Amber = Hex(0xffb02e);
        public static readonly Color Bone = Hex(0xf2e8d0);
        public static readonly Color BoneMid = Hex(0xc8b89a);
        public static readonly Color BoneDark = Hex(0x8e7c64);
        public static readonly Color Gold = Hex(0xe0a828);
        public static readonly Color GoldLight = Hex(0xffd860);
        public static readonly Color GreenLight = Hex(0x8a9a3a);
        public static readonly Color LilacLight = Hex(0xc8a8d4);

        public static readonly Color Background = Black;
        public static readonly Color Felt = Dusk;
        public static readonly Color Ink = Black;
        public static readonly Color MutedText = BoneMid;
        public static readonly Color Slot = Night;

        /// <summary>Card ink: black suits and red suits (matching the suit sprites).</summary>
        public static readonly Color BlackSuit = Black;
        public static readonly Color RedSuit = Crimson;

        public static Color For(Tone tone)
        {
            switch (tone)
            {
                case Tone.Muted: return MutedText;
                case Tone.Good: return GreenLight;
                case Tone.Bad:
                case Tone.Warning: return Ember;
                case Tone.Triumph: return GoldLight;
                case Tone.Doom: return Hell;
                default: return Bone;
            }
        }

        private static Color Hex(int rgb)
        {
            return new Color32((byte)(rgb >> 16), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff), 255);
        }
    }
}
