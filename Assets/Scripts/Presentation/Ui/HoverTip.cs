using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Phase 3: a hover box drawn over everything on its screen and kept inside it. The box gets its own canvas sorted
    /// <see cref="AboveItsScreen"/> above the screen's (a sibling order inside one widget is not enough: a panel built later,
    /// the payouts, covered the last hands' tip), and every frame it is shown it is pushed back inside the 640×360 screen by whole pixels.
    /// It takes no clicks (no raycaster): the pointer under it still reaches what it explains.
    /// Demo 3.0.2: a nested canvas draws even when a canvas above it is switched off (the relics' bag, the map hide that way), so the
    /// box follows its screen: while any canvas above it is off it is not drawn and closes; it opens again on the next hover.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverTip : MonoBehaviour
    {
        /// <summary>Over its own screen, under the next screen up (the map is 40 over the table's 0, the curtain 300).</summary>
        public const int AboveItsScreen = 15;

        private Canvas _canvas;

        /// <summary>Makes <paramref name="tip"/> a hover box on top (call once, when it is built).</summary>
        public static HoverTip Make(GameObject tip)
        {
            HoverTip hover = tip.GetComponent<HoverTip>();
            if (hover == null) hover = tip.AddComponent<HoverTip>();
            hover.EnsureCanvas();
            return hover;
        }

        private void EnsureCanvas()
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        }

        private void OnEnable()
        {
            EnsureCanvas();
            Transform parent = transform.parent;
            Canvas screen = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            int order = screen != null ? screen.rootCanvas.sortingOrder : 0;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = order + AboveItsScreen;
            _canvas.enabled = ScreenShown;
            KeepOnScreen();
        }

        private void LateUpdate()
        {
            if (!ScreenShown)
            {
                // Its screen was switched off under the pointer: no exit will come, so the box closes itself.
                _canvas.enabled = false;
                gameObject.SetActive(false);
                return;
            }
            _canvas.enabled = true;
            KeepOnScreen();
        }

        /// <summary>The sorting order it is drawn at (tests).</summary>
        public int SortingOrder => _canvas != null ? _canvas.sortingOrder : 0;

        /// <summary>Every canvas above the box draws: its screen is on.</summary>
        public bool ScreenShown
        {
            get
            {
                for (Transform p = transform.parent; p != null; p = p.parent)
                {
                    Canvas canvas = p.GetComponent<Canvas>();
                    if (canvas != null && !canvas.enabled) return false;
                }
                return true;
            }
        }

        /// <summary>The box is drawn (tests): open with its own canvas on — a nested canvas draws even under a switched-off one.</summary>
        public bool Visible => isActiveAndEnabled && _canvas != null && _canvas.enabled;

        /// <summary>Shifts the box back inside its screen (the pixel screen's "Screen" rect), by whole pixels.</summary>
        public void KeepOnScreen()
        {
            var box = (RectTransform)transform;
            RectTransform screen = ScreenOf(box);
            if (screen == null) return;
            var corners = new Vector3[4];
            box.GetWorldCorners(corners);
            Vector2 min = screen.InverseTransformPoint(corners[0]), max = screen.InverseTransformPoint(corners[2]);
            Rect area = screen.rect;
            float dx = 0f, dy = 0f;
            if (max.x > area.xMax) dx = area.xMax - max.x;
            if (min.x + dx < area.xMin) dx = area.xMin - min.x;
            if (min.y < area.yMin) dy = area.yMin - min.y;
            if (max.y + dy > area.yMax) dy = area.yMax - max.y;
            if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) return;
            // In the parent's pixels (every widget is unscaled inside the screen); outward fractions rounded inward.
            Vector3 scale = box.parent != null ? box.parent.lossyScale : Vector3.one;
            Vector3 screenScale = screen.lossyScale;
            float sx = scale.x != 0f ? screenScale.x / scale.x : 1f, sy = scale.y != 0f ? screenScale.y / scale.y : 1f;
            box.anchoredPosition += new Vector2(dx > 0f ? Mathf.Ceil(dx * sx) : Mathf.Floor(dx * sx), dy > 0f ? Mathf.Ceil(dy * sy) : Mathf.Floor(dy * sy));
        }

        /// <summary>The 640×360 screen a widget is in: the nearest "Screen" rect up its parents.</summary>
        public static RectTransform ScreenOf(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
                if (p.name == "Screen" && p.GetComponent<RectMask2D>() != null) return p as RectTransform;
            return null;
        }
    }
}
