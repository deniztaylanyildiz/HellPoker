using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Small helpers for building uGUI hierarchies from code, dressed in the gothic-casino art.</summary>
    internal static class UiFactory
    {
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

        public static RectTransform Stretch(this RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

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
                image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>A dark velvet panel with a thin gold edge.</summary>
        public static Image CreatePanel(string name, Transform parent)
        {
            Image panel = CreateSprite(name, parent, UiArt.Panel, Palette.Felt, sliced: true);
            if (panel.sprite == null)
                AddBorder(panel.gameObject, Palette.CardBack, 2f);
            return panel;
        }

        /// <summary>An ornate gold frame, drawn over whatever it surrounds.</summary>
        public static Image CreateFrame(string name, Transform parent, float thickness = 1f)
        {
            Image frame = CreateSprite(name, parent, UiArt.Frame, null, sliced: true);
            frame.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, thickness);
            return frame;
        }

        /// <summary>
        /// Text in the house fonts: <see cref="FontStyle.Bold"/> picks the carved display capitals,
        /// <see cref="FontStyle.Italic"/> the italic serif, anything else the old-print serif.
        /// </summary>
        public static Text CreateText(string name, Transform parent, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            ApplyFont(text, style);
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyFont(Text text, FontStyle style)
        {
            Font font = style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? UiArt.Display
                : style == FontStyle.Italic ? UiArt.SerifItalic
                : UiArt.Serif;

            // The house fonts already carry their weight and slant; the built-in fallback fakes them.
            text.font = font != null ? font : BuiltinFont;
            text.fontStyle = font != null ? FontStyle.Normal : style;
        }

        /// <summary>A soft dark drop shadow, so text reads on busy art.</summary>
        public static T WithShadow<T>(this T graphic, float distance = 2f, float alpha = 0.85f) where T : Graphic
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
            return graphic;
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, out Text labelText,
            ButtonSkin skin = ButtonSkin.Blood)
        {
            Sprite sprite = UiArt.Button(skin);
            Image background = CreateImage(name, parent, sprite != null ? Color.white : FallbackColor(skin));
            background.sprite = sprite;
            if (sprite != null)
                background.type = Image.Type.Sliced;

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.92f, 0.92f, 0.92f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.7f, 0.62f, 0.6f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.42f, 0.38f, 0.38f, 0.9f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (sprite == null)
                AddBorder(background.gameObject, Palette.Ember, 2f);
            MakeClickOnly(button);

            labelText = CreateText("Label", background.transform, label, fontSize, Palette.Bone, style: FontStyle.Bold).WithShadow();
            labelText.rectTransform.Stretch();
            labelText.rectTransform.offsetMin = new Vector2(12f, 2f);
            labelText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            return button;
        }

        private static Color FallbackColor(ButtonSkin skin)
        {
            switch (skin)
            {
                case ButtonSkin.Ember: return Palette.Ember;
                case ButtonSkin.Ash: return Palette.Fold;
                default: return Palette.Button;
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

        /// <summary>White sprite, transparent in the centre fading to opaque edges; tint it with the Image colour.</summary>
        public static Sprite CreateVignetteSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float edge = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.55f + new Vector2(dx, dy).magnitude * 0.45f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1.05f, edge));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public static void AddBorder(GameObject target, Color color, float width)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
        }

        /// <summary>A screen-space canvas scaled for the 1920×1080 reference layout.</summary>
        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
