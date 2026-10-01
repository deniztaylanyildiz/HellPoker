using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Small helpers for building uGUI hierarchies from code.</summary>
    internal static class UiFactory
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
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

        public static Text CreateText(string name, Transform parent, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, out Text labelText)
        {
            Image background = CreateImage(name, parent, Palette.Button);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.15f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.7f);
            colors.disabledColor = new Color(0.5f, 0.45f, 0.45f);
            colors.colorMultiplier = 1.5f;
            button.colors = colors;

            AddBorder(background.gameObject, Palette.Ember, 2f);
            MakeClickOnly(button);

            labelText = CreateText("Label", background.transform, label, fontSize, Palette.Bone, style: FontStyle.Bold);
            labelText.rectTransform.Stretch();
            return button;
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
    }
}
