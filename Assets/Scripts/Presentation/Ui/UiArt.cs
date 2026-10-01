using System.Collections.Generic;
using HellPoker.Presentation.Animation;
using UnityEngine;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// The generated pixel art and fonts in Resources (made by Tools/ArtGen). Missing files return null, and the views
    /// fall back to flat colours and the built-in font, so the game still runs without them.
    /// </summary>
    internal static class UiArt
    {
        public const string Background = "Ui/background";
        public const string BackgroundHell = "Ui/background_hell";
        public const string Panel = "Ui/panel";
        public const string PanelHot = "Ui/panel_hot";
        public const string Dialog = "Ui/dialog";
        public const string CardFace = "Ui/card_face";
        public const string CardBack = "Ui/card_back";
        public const string CardSlot = "Ui/card_slot";
        public const string Title = "Ui/title";
        public const string Divider = "Ui/divider";
        public const string Coin = "Ui/coin";
        public const string Flames = "Ui/flames";
        public const string Digits = "Ui/digits";
        public const string SoulLamp = "Ui/soul_lamp";
        public const int SoulLampWidth = 16;

        public const int FlameFrameWidth = 32;
        public const int DigitWidth = 12;

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> Strips = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        /// <summary>Animations of the demon dealers, loaded from Resources/Art.</summary>
        public static readonly DealerAnimationLibrary Dealers = new DealerAnimationLibrary(path => Resources.Load<Texture2D>("Art/" + path));

        /// <summary>The demons' halls, loaded from Resources/Art.</summary>
        public static readonly SalonLibrary Salons = new SalonLibrary(path => Resources.Load<Texture2D>("Art/" + path));

        public static Sprite Sprite(string name)
        {
            if (!Sprites.TryGetValue(name, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>("Art/" + name);
                Sprites[name] = sprite;
            }
            return sprite;
        }

        /// <summary>A strip cut into frames of the given width (flames, digits); null when the art is missing.</summary>
        public static Sprite[] Strip(string name, int frameWidth)
        {
            if (!Strips.TryGetValue(name, out Sprite[] frames))
            {
                frames = SpriteSheet.Slice(Resources.Load<Texture2D>("Art/" + name), frameWidth);
                Strips[name] = frames;
            }
            return frames;
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

        public static Sprite Suit(Core.Cards.Suit suit, bool small)
        {
            return Sprite("Ui/suit_" + suit.ToString().ToLowerInvariant() + (small ? "_small" : ""));
        }

        /// <summary>Blocky capitals for titles, numbers and buttons (Press Start 2P, 8 px grid).</summary>
        public static Font Display => LoadFont("Fonts/HellPokerPixelTitle");

        /// <summary>Small pixel text for everything else (Tiny5, 8 px grid).</summary>
        public static Font Body => LoadFont("Fonts/HellPokerPixel");

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
