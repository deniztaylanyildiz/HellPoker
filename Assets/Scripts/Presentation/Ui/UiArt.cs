using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// The generated art and fonts in Resources (made by Tools/ArtGen). Missing files return null, and the views
    /// fall back to flat colours and the built-in font, so the game still runs without them.
    /// </summary>
    internal static class UiArt
    {
        public const string Background = "Ui/background";
        public const string Table = "Ui/table";
        public const string Frame = "Ui/frame";
        public const string Panel = "Ui/panel";
        public const string Chip = "Ui/chip";
        public const string ChipSelected = "Ui/chip_selected";
        public const string CardFace = "Ui/card_face";
        public const string CardBack = "Ui/card_back";
        public const string CardSlot = "Ui/card_slot";
        public const string Title = "Ui/title";
        public const string Divider = "Ui/divider";
        public const string Speech = "Ui/speech";
        public const string Glow = "Ui/glow";

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static Sprite Sprite(string name)
        {
            if (!Sprites.TryGetValue(name, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>("Art/" + name);
                Sprites[name] = sprite;
            }
            return sprite;
        }

        public static Sprite Button(ButtonSkin skin)
        {
            switch (skin)
            {
                case ButtonSkin.Ember: return Sprite("Ui/button_ember");
                case ButtonSkin.Ash: return Sprite("Ui/button_ash");
                default: return Sprite("Ui/button_blood");
            }
        }

        public static Sprite Suit(Core.Cards.Suit suit)
        {
            return Sprite("Ui/suit_" + suit.ToString().ToLowerInvariant());
        }

        public static Sprite Portrait(string dealerId)
        {
            return string.IsNullOrEmpty(dealerId) ? null : Sprite("Demons/" + dealerId);
        }

        /// <summary>Carved capitals for titles, numbers and buttons.</summary>
        public static Font Display => LoadFont("Fonts/HellPokerDisplay");

        /// <summary>Old-print serif for running text.</summary>
        public static Font Serif => LoadFont("Fonts/HellPokerSerif");

        public static Font SerifItalic => LoadFont("Fonts/HellPokerSerif-Italic");

        private static Font LoadFont(string path)
        {
            if (!Fonts.TryGetValue(path, out Font font))
            {
                font = Resources.Load<Font>(path);
                Fonts[path] = font;
            }
            return font;
        }
    }

    internal enum ButtonSkin
    {
        Blood,
        Ember,
        Ash
    }
}
