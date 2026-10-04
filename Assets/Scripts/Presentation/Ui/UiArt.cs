using System;
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
        public const string DialogLucifer = "Ui/dialog_lucifer";

        /// <summary>Full-screen black in growing Bayer patterns (¼ … all), for a slow fall into darkness.</summary>
        public const string Fade = "Ui/fade";
        public const int FadeWidth = 480;
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

        /// <summary>Overlays for cards a cheat marked (32×48 frames: chained, thorned, veiled, false face).</summary>
        public const string CardMarks = "Ui/card_marks";
        public const string CheatIcons = "Ui/cheat_icons";
        public const string MalicePips = "Ui/malice_pips";
        public const int CheatIconSize = 16;
        public const int MalicePipSize = 8;

        /// <summary>The order of the icons in cheat_icons.png (Tools/ArtGen/pixel_ui.py CHEAT_ICON_IDS).</summary>
        private static readonly string[] CheatIconIds =
        {
            "collateral", "tithe", "buyout", "false_face", "forked_tongue", "serpent_swap", "night_veil", "thorn", "moonless", "gaze",
            "rewrite", "burning_card", "the_fall"
        };

        /// <summary>The order of the demons in malice_pips.png (empty, full each).</summary>
        private static readonly string[] MalicePipOrder = { "mammon", "belial", "lilith", "lucifer" };

        /// <summary>A cheat's 16×16 icon; null when the art (or the id) is missing.</summary>
        public static Sprite CheatIcon(string cheatId)
        {
            int index = Array.IndexOf(CheatIconIds, cheatId);
            Sprite[] icons = Strip(CheatIcons, CheatIconSize);
            return index >= 0 && icons != null && index < icons.Length ? icons[index] : null;
        }

        /// <summary>One pip of a demon's malice gauge (a coin, a scale, a thorn, an ember); null when missing.</summary>
        public const string RelicIcons = "Ui/relic_icons";

        /// <summary>A relic's 16×16 icon (strip in <see cref="HellPoker.Core.Relics.RelicRoster"/> order); null if missing.</summary>
        public static Sprite RelicIcon(string relicId)
        {
            int index = -1;
            for (int i = 0; i < HellPoker.Core.Relics.RelicRoster.All.Count; i++)
                if (HellPoker.Core.Relics.RelicRoster.All[i].Id == relicId) index = i;
            Sprite[] icons = Strip(RelicIcons, 16);
            return index >= 0 && icons != null && index < icons.Length ? icons[index] : null;
        }

        public const string SinnerIcons = "Ui/sinner_icons";
        public const int SinnerIconSize = 16;
        public const int SinnerPortraitSize = 48;

        /// <summary>The class's badge icon (16×16, from the strip in <see cref="HellPoker.Core.Sinners.SinnerRoster"/> order); null if missing.</summary>
        public static Sprite SinnerIcon(string classId)
        {
            int index = -1;
            for (int i = 0; i < HellPoker.Core.Sinners.SinnerRoster.All.Count; i++)
                if (HellPoker.Core.Sinners.SinnerRoster.All[i].Id == classId) index = i;
            Sprite[] icons = Strip(SinnerIcons, SinnerIconSize);
            return index >= 0 && icons != null && index < icons.Length ? icons[index] : null;
        }

        /// <summary>The class's portrait (48×48, "Sinners/&lt;id&gt;"); null if missing.</summary>
        public static Sprite SinnerPortrait(string classId) => Sprite("Sinners/" + classId);

        public static Sprite MalicePip(string dealerId, bool full)
        {
            int index = Array.IndexOf(MalicePipOrder, dealerId);
            if (index < 0) index = 0;
            Sprite[] pips = Strip(MalicePips, MalicePipSize);
            int frame = index * 2 + (full ? 1 : 0);
            return pips != null && frame < pips.Length ? pips[frame] : null;
        }

        public const int FlameFrameWidth = 32;
        public const int DigitWidth = 12;

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> Strips = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        /// <summary>Animations of the demon dealers, loaded from Resources/Art.</summary>
        public static readonly DealerAnimationLibrary Dealers = new DealerAnimationLibrary(path => Resources.Load<Texture2D>("Art/" + path));

        /// <summary>The title screen's animated backdrop (also behind the rules, settings and records), loaded from Resources/Art.</summary>
        public static readonly MenuBackdropLibrary MenuBackdrop = new MenuBackdropLibrary(path => Resources.Load<Texture2D>("Art/" + path), LoadText);

        /// <summary>The demons' halls, loaded from Resources/Art.</summary>
        public static readonly SalonLibrary Salons = new SalonLibrary(path => Resources.Load<Texture2D>("Art/" + path), LoadText);

        /// <summary>A text file under Resources/Art (the backdrops' motion manifests); null when missing.</summary>
        private static string LoadText(string path) => Resources.Load<TextAsset>("Art/" + path)?.text;

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
