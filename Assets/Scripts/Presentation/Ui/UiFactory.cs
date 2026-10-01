using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Small helpers for building the pixel-art uGUI from code. Everything is laid out in pixels of the 480×270 screen
    /// (see <see cref="PixelScreen"/>); keep positions and sizes whole numbers and text sizes multiples of 8.
    /// </summary>
    internal static class UiFactory
    {
        /// <summary>Both pixel fonts are drawn on an 8 px em.</summary>
        public const int FontGrid = 8;

        private static Font _builtinFont;

        public static Font BuiltinFont
        {
            get
            {
                if (_builtinFont == null)
                    _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _builtinFont;
            }
        }

        // ------------------------------------------------------------------ screens and rects

        /// <summary>
        /// A screen-space canvas holding a centred, whole-number-scaled 480×270 screen with black bars around it.
        /// Build the content inside <paramref name="screen"/>.
        /// </summary>
        public static Canvas CreateScreen(string name, Transform parent, int sortingOrder, out RectTransform screen)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<PixelScreen>();

            CreateImage("Letterbox", go.transform, Color.black).rectTransform.Stretch();

            screen = CreateRect("Screen", go.transform).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PixelScreen.Width, PixelScreen.Height));
            var mask = screen.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect to a single point of its parent and sets position and size.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Places a rect by its top-left corner, in pixels from the parent's top-left (y grows downward).</summary>
        public static RectTransform PlaceTL(this RectTransform rect, int x, int y, int width, int height)
        {
            return rect.Place(new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height), new Vector2(0f, 1f));
        }

        public static RectTransform Stretch(this RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        // ------------------------------------------------------------------ images

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>An image of a generated sprite; a flat <paramref name="fallback"/> colour if the art is missing.</summary>
        public static Image CreateSprite(string name, Transform parent, string art, Color? fallback = null, bool sliced = false)
        {
            Sprite sprite = UiArt.Sprite(art);
            Image image = CreateImage(name, parent, sprite != null ? Color.white : fallback ?? Color.clear);
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sliced && sprite != null)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        /// <summary>An old-RPG menu box: black line, gold frame, dark fill. <paramref name="hot"/> for the selected one.</summary>
        public static Image CreatePanel(string name, Transform parent, bool hot = false)
        {
            Image panel = CreateSprite(name, parent, hot ? UiArt.PanelHot : UiArt.Panel, Palette.Night, sliced: true);
            if (panel.sprite == null)
                AddBorder(panel.gameObject, hot ? Palette.Ember : Palette.Gold, 1f);
            return panel;
        }

        /// <summary>The dialogue box the dealer speaks in: bone-white frame on dusk.</summary>
        public static Image CreateDialog(string name, Transform parent)
        {
            Image box = CreateSprite(name, parent, UiArt.Dialog, Palette.Dusk, sliced: true);
            if (box.sprite == null)
                AddBorder(box.gameObject, Palette.Bone, 1f);
            return box;
        }

        // ------------------------------------------------------------------ text

        /// <summary>
        /// Pixel text: <see cref="FontStyle.Bold"/> picks the blocky display capitals, anything else the small body font.
        /// The size is kept on the fonts' 8 px grid so every glyph pixel is a whole screen pixel.
        /// </summary>
        public static Text CreateText(string name, Transform parent, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            Font font = style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? UiArt.Display : UiArt.Body;
            text.font = font != null ? font : BuiltinFont;
            text.fontStyle = FontStyle.Normal;
            text.text = content;
            text.fontSize = Mathf.Max(FontGrid, size / FontGrid * FontGrid);
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A hard 1 px drop shadow in palette black.</summary>
        public static T WithShadow<T>(this T graphic) where T : Graphic
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Palette.Black;
            shadow.effectDistance = new Vector2(1f, -1f);
            return graphic;
        }

        // ------------------------------------------------------------------ buttons

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, out Text labelText,
            ButtonSkin skin = ButtonSkin.Blood)
        {
            Sprite sprite = UiArt.Button(skin);
            Image background = CreateImage(name, parent, sprite != null ? Color.white : FallbackColor(skin));
            background.sprite = sprite;
            if (sprite != null)
            {
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = 1f;
            }

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            button.colors = colors;

            if (sprite == null)
                AddBorder(background.gameObject, Palette.Black, 1f);
            MakeClickOnly(button);

            labelText = CreateText("Label", background.transform, label, fontSize, Palette.Bone, style: FontStyle.Bold).WithShadow();
            labelText.rectTransform.Stretch();
            labelText.rectTransform.offsetMin = new Vector2(4f, 0f);
            labelText.rectTransform.offsetMax = new Vector2(-4f, 0f);
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            return button;
        }

        private static Color FallbackColor(ButtonSkin skin)
        {
            switch (skin)
            {
                case ButtonSkin.Ember: return Palette.Hell;
                case ButtonSkin.Ash: return Palette.Plum;
                default: return Palette.Crimson;
            }
        }

        /// <summary>
        /// Stops a clicked button from staying "selected". Otherwise uGUI fires it again on Space/Enter (Submit),
        /// on top of our own keyboard shortcuts, and one key press triggers two actions.
        /// </summary>
        public static void MakeClickOnly(Button button)
        {
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
            });
        }

        public static void AddBorder(GameObject target, Color color, float width)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
        }
    }
}
