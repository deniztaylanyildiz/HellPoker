using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A chapter's map, on the 480×270 screen: the floors go down from the top (the first) to the purgatory fire, six lanes side by
    /// side, the vault's gate under them. The paths are drawn pixel by pixel into one texture (dotted; the trail walked in gold,
    /// the ways open from here in amber). The nodes are 16×16 icons in 20×20 frames: the ones the player may step to blink their frame,
    /// the keyboard's pick is framed in ember, the trail is lit, the rest of the map is dimmed. Hovering a node tells what waits there.
    /// The side panel on the left: the chapter, the purse, the tribute, the sentence, the relics; MENU at the bottom.
    /// </summary>
    public sealed class ChapterMapView : MonoBehaviour, IChapterMapView
    {
        public const int SortingOrder = 40;

        private const int SideWidth = 124;
        private const int MapLeft = 140;
        private const int LaneStep = 54;
        private const int IconSize = 16;

        /// <summary>The map's bottom: the gate's icon and its words under the last floor end above it.</summary>
        private const int MapBottom = 266;

        /// <summary>Eight floors start under the prompt with room to spare; ten and twelve start higher.</summary>
        private static int FloorTopFor(int floors) => floors > 8 ? 16 : 22;

        /// <summary>Eight floors get 28 pixels each and a 20-pixel frame; ten and twelve are packed closer (twelve: 19, a frame of 18).</summary>
        private static int FloorStepFor(int floors) => Math.Min(28, (MapBottom - FloorTopFor(floors) - IconSize - 2) / Math.Max(1, floors));

        private int _floorStep = 28;
        private int FloorTop => FloorTopFor(_state?.Map.FloorCount ?? 8);
        private int NodeFrame => Math.Min(20, _floorStep - 1);

        /// <summary>The icons' order in Ui/map_nodes.png: the node kinds, then the gate.</summary>
        public const string NodeIcons = "Ui/map_nodes";
        public const int GateIcon = 6;

        private Canvas _canvas;
        private RectTransform _screen;
        private Image _backdrop;
        private int _backdropChapter = 1;

        /// <summary>The map's backdrop of a chapter (Mammon's vault, Belial's curtained stage, Lilith's violet night).</summary>
        public static string BackdropOf(int chapter) => chapter == 2 ? "Ui/chapter_map_belial" : chapter == 3 ? "Ui/chapter_map_lilith" : "Ui/chapter_map";
        private RawImage _paths;
        private Texture2D _pathTexture;
        private Text _title;
        private Text _coins;
        private Text _lines;
        private Text _prompt;
        private GameObject _tip;
        private Text _tipText;
        private RectTransform _tipRect;
        private Image _gate;
        private Text _gateLabel;
        private readonly List<NodeWidget> _nodes = new List<NodeWidget>();
        private ChapterMapState _state;
        private float _blink;

        public event Action<int, int> NodePressed;
        public event Action MenuPressed;

        public bool IsVisible => _canvas.enabled;

        /// <summary>The state on show (for tests and screenshots).</summary>
        public ChapterMapState State => _state;

        private sealed class NodeWidget
        {
            public MapNode Node;
            public Image Frame;
            public Image Icon;
            public Button Button;
        }

        public static ChapterMapView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("ChapterMapCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<ChapterMapView>();
            view._canvas = canvas;
            view._screen = screen;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            _backdrop = UiFactory.CreateSprite("Backdrop", screen, "Ui/chapter_map", Palette.Night);
            _backdrop.rectTransform.Stretch();

            _pathTexture = new Texture2D(PixelScreen.Width, PixelScreen.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _paths = new GameObject("Paths", typeof(RectTransform)).AddComponent<RawImage>();
            _paths.transform.SetParent(screen, false);
            _paths.rectTransform.Stretch();
            _paths.texture = _pathTexture;
            _paths.raycastTarget = false;

            // The side panel.
            Image side = UiFactory.CreatePanel("Side", screen);
            side.rectTransform.PlaceTL(4, 4, SideWidth, PixelScreen.Height - 8);
            _title = UiFactory.CreateText("Title", side.transform, "", 8, Palette.GoldLight, TextAnchor.UpperCenter, FontStyle.Bold).WithOutline();
            _title.rectTransform.PlaceTL(6, 8, SideWidth - 12, 28);
            UiFactory.CreateSprite("Divider", side.transform, UiArt.Divider).rectTransform.PlaceTL((SideWidth - 48) / 2, 38, 48, 3);

            UiFactory.CreateText("CoinsLabel", side.transform, "", 8, Palette.BoneMid, TextAnchor.UpperCenter).WithOutline().Localized(() => UiText.MapCoinsLabel)
                .rectTransform.PlaceTL(6, 46, SideWidth - 12, 9);
            Image coin = UiFactory.CreateSprite("Coin", side.transform, UiArt.Coin, Palette.Gold);
            coin.rectTransform.PlaceTL(14, 58, 16, 16);
            _coins = UiFactory.CreateText("Coins", side.transform, "", 16, Palette.GoldLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline();
            _coins.rectTransform.PlaceTL(34, 58, SideWidth - 40, 16);
            _coins.horizontalOverflow = HorizontalWrapMode.Overflow;

            _lines = UiFactory.CreateText("Lines", side.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft).WithOutline();
            _lines.rectTransform.PlaceTL(8, 82, SideWidth - 16, 140);
            _lines.lineSpacing = 1f;

            UiFactory.CreateText("Keys", side.transform, "", 8, Palette.BoneDark, TextAnchor.UpperLeft).WithOutline().Localized(() => UiText.MapKeysHint)
                .rectTransform.PlaceTL(8, 210, SideWidth - 16, 30);

            Button menu = UiFactory.CreateButton("MapMenuButton", side.transform, "", 8, out Text menuLabel, ButtonSkin.Ash);
            menuLabel.Localized(() => UiText.Menu);
            ((RectTransform)menu.transform).PlaceTL(8, PixelScreen.Height - 8 - 26, SideWidth - 16, 18);
            menu.onClick.AddListener(() => MenuPressed?.Invoke());

            // The gate above the last floor.
            _gate = UiFactory.CreateImage("Gate", screen, Color.white);
            _gate.raycastTarget = false;
            _gateLabel = UiFactory.CreateText("GateLabel", screen, "", 8, Palette.GoldLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline();
            PlaceGate();
            _gateLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            _prompt = UiFactory.CreateText("Prompt", screen, "", 8, Palette.Bone, TextAnchor.MiddleCenter).WithOutline();
            _prompt.rectTransform.PlaceTL(MapLeft, 6, PixelScreen.Width - MapLeft - 4, 9);

            // The hover box (over everything else).
            Image tip = UiFactory.CreateImage("NodeTip", screen, Palette.Black);
            tip.raycastTarget = false;
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tipRect = tip.rectTransform;
            _tipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tipText.rectTransform.PlaceTL(4, 3, 172, 44);
            _tip = tip.gameObject;
            _tip.SetActive(false);
        }

        private static int LaneX(float lane) => MapLeft + 10 + Mathf.RoundToInt(lane * LaneStep) + 10;

        private int FloorY(int floor, int floors) => FloorTop + floor * _floorStep + NodeFrame / 2;

        /// <summary>The gate's icon, under the last floor.</summary>
        private int GateY => FloorTop + (_state?.Map.FloorCount ?? 8) * _floorStep + 2;

        public void Show(ChapterMapState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            if (_nodes.Count == 0 || !ReferenceEquals(_builtFor, state.Map))
            {
                _floorStep = FloorStepFor(state.Map.FloorCount);
                BuildNodes(state.Map);
                PlaceGate();
            }
            if (state.Chapter != _backdropChapter)
            {
                _backdropChapter = state.Chapter;
                Sprite sprite = UiArt.Sprite(BackdropOf(state.Chapter)) ?? UiArt.Sprite("Ui/chapter_map");
                _backdrop.sprite = sprite;
                _backdrop.color = sprite != null ? Color.white : Palette.Night;
            }

            _title.text = state.Title;
            _coins.text = state.Coins.ToString();
            _lines.text = string.Join("\n", state.Lines);
            _prompt.text = state.Prompt;
            Sprite[] icons = UiArt.Strip(NodeIcons, IconSize);
            _gate.sprite = icons != null && icons.Length > GateIcon ? icons[GateIcon] : null;
            _gate.color = _gate.sprite != null ? Color.white : Palette.Gold;
            _gateLabel.text = state.GateLabel;
            DrawPaths(state);
            Paint();
        }

        private void PlaceGate()
        {
            _gate.rectTransform.PlaceTL(LaneX(2.5f) - IconSize / 2, GateY, IconSize, IconSize);
            _gateLabel.rectTransform.PlaceTL(LaneX(2.5f) + IconSize / 2 + 4, GateY + 4, 160, 9);
        }

        /// <summary>The map the node widgets were built for (a new chapter run builds them again).</summary>
        private ChapterMap _builtFor;

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
            _tip.SetActive(false);
        }

        private void BuildNodes(ChapterMap map)
        {
            foreach (NodeWidget old in _nodes) Destroy(old.Frame.gameObject);
            _nodes.Clear();
            _builtFor = map;
            Sprite[] icons = UiArt.Strip(NodeIcons, IconSize);
            foreach (IReadOnlyList<MapNode> floor in map.Floors)
            foreach (MapNode node in floor)
            {
                int x = LaneX(node.Lane) - NodeFrame / 2, y = FloorY(node.Floor, map.FloorCount) - NodeFrame / 2;
                Image frame = UiFactory.CreateImage($"Node{node.Floor}_{node.Lane}", _screen, Palette.Black);
                frame.rectTransform.PlaceTL(x, y, NodeFrame, NodeFrame);
                frame.raycastTarget = true;
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                MapNode captured = node;
                button.onClick.AddListener(() => NodePressed?.Invoke(captured.Floor, captured.Lane));

                // The frame is the node's 1 px border (its colour says what it is to the player); a dark box inside it.
                Image inside = UiFactory.CreateImage("Inside", frame.transform, Palette.Black);
                inside.raycastTarget = false;
                inside.rectTransform.PlaceTL(1, 1, NodeFrame - 2, NodeFrame - 2);
                Image icon = UiFactory.CreateImage("Icon", frame.transform, Color.white);
                icon.raycastTarget = false;
                icon.rectTransform.PlaceTL((NodeFrame - IconSize) / 2, (NodeFrame - IconSize) / 2, IconSize, IconSize);
                int kind = (int)node.Kind;
                icon.sprite = icons != null && kind < icons.Length ? icons[kind] : null;
                if (icon.sprite == null) icon.color = FallbackColor(node.Kind);

                AddHover(frame.gameObject, () => ShowTip(captured), () => _tip.SetActive(false));
                _nodes.Add(new NodeWidget { Node = node, Frame = frame, Icon = icon, Button = button });
            }
            _tip.transform.SetAsLastSibling();
            _prompt.transform.SetAsLastSibling();
        }

        private static Color FallbackColor(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Table: return Palette.BoneMid;
                case NodeKind.Event: return Palette.Lilac;
                case NodeKind.BlackMarket: return Palette.GreenLight;
                case NodeKind.Warden: return Palette.Hell;
                case NodeKind.Treasure: return Palette.Gold;
                default: return Palette.Ember;
            }
        }

        private static void AddHover(GameObject target, Action enter, Action exit)
        {
            var trigger = target.AddComponent<EventTrigger>();
            var on = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            on.callback.AddListener(_ => enter());
            var off = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            off.callback.AddListener(_ => exit());
            trigger.triggers.Add(on);
            trigger.triggers.Add(off);
        }

        private void ShowTip(MapNode node)
        {
            if (_state == null) return;
            _tipText.text = _state.Describe(node);
            int width = 180, height = 50;
            int x = LaneX(node.Lane) + NodeFrame / 2 + 4;
            if (x + width > PixelScreen.Width - 2) x = LaneX(node.Lane) - NodeFrame / 2 - 4 - width;
            int y = Mathf.Clamp(FloorY(node.Floor, _state.Map.FloorCount) - height / 2, 2, PixelScreen.Height - height - 2);
            _tipRect.PlaceTL(x, y, width, height);
            _tip.SetActive(true);
            _tip.transform.SetAsLastSibling();
        }

        /// <summary>Every node's look for the state on show (and the blink of the choices).</summary>
        private void Paint()
        {
            if (_state == null) return;
            bool lit = Mathf.Repeat(_blink, 0.8f) < 0.5f;
            var trail = new HashSet<MapNode>(_state.Trail);
            foreach (NodeWidget w in _nodes)
            {
                MapNode node = w.Node;
                bool choice = _state.Choices.Contains(node);
                bool here = node == _state.Current;
                bool walked = trail.Contains(node);
                bool past = _state.Current != null && node.Floor <= _state.Current.Floor && !walked;
                Color frame = Palette.Black;
                if (choice) frame = node == _state.Selected ? Palette.Ember : lit ? Palette.GoldLight : Palette.Gold;
                else if (here) frame = Palette.Gold;
                else if (walked) frame = Palette.Plum;
                w.Frame.color = frame;
                Color tint = choice || here ? Color.white : walked ? new Color(0.75f, 0.75f, 0.75f) : past ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.5f, 0.5f, 0.5f);
                w.Icon.color = w.Icon.sprite != null ? tint : FallbackColor(node.Kind) * tint;
            }
        }

        private void Update()
        {
            if (!_canvas.enabled || _state == null || _state.Choices.Count == 0) return;
            _blink += Time.unscaledDeltaTime;
            Paint();
        }

        // ------------------------------------------------------------------ the paths, drawn pixel by pixel

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private void DrawPaths(ChapterMapState state)
        {
            var pixels = new Color32[PixelScreen.Width * PixelScreen.Height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
            ChapterMap map = state.Map;
            var trail = state.Trail;
            Color32 dim = Palette.BoneDark, gold = Palette.Gold, amber = Palette.Ember, dark = Palette.Plum;
            foreach (IReadOnlyList<MapNode> floor in map.Floors)
            foreach (MapNode node in floor)
            foreach (MapNode next in map.NextFrom(node))
            {
                bool walked = Walked(trail, node, next);
                bool open = node == state.Current && state.Choices.Contains(next);
                bool behind = state.Current != null && next.Floor <= state.Current.Floor && !walked;
                Color32 color = walked ? gold : open ? amber : behind ? dark : dim;
                Line(pixels, LaneX(node.Lane), FloorY(node.Floor, map.FloorCount) + NodeFrame / 2,
                    LaneX(next.Lane), FloorY(next.Floor, map.FloorCount) - NodeFrame / 2 - 1, color, dotted: !walked && !open);
            }
            // The last floor's fires lead down to the gate.
            int gateX = LaneX(2.5f), gateY = GateY - 1;
            foreach (MapNode fire in map.Floors[map.FloorCount - 1])
            {
                bool walked = trail.Count > 0 && trail[trail.Count - 1] == fire;
                Line(pixels, LaneX(fire.Lane), FloorY(fire.Floor, map.FloorCount) + NodeFrame / 2, gateX, gateY, walked ? gold : dark, dotted: !walked);
            }
            _pathTexture.SetPixels32(pixels);
            _pathTexture.Apply(false);
        }

        private static bool Walked(IReadOnlyList<MapNode> trail, MapNode from, MapNode to)
        {
            for (int i = 0; i + 1 < trail.Count; i++)
                if (trail[i] == from && trail[i + 1] == to) return true;
            return false;
        }

        /// <summary>A pixel line in screen coordinates (y down); dotted lines skip every other pixel.</summary>
        private static void Line(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color, bool dotted)
        {
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, step = 0;
            while (true)
            {
                if (!dotted || step % 3 != 2) Put(pixels, x0, y0, color);
                step++;
                if (x0 == x1 && y0 == y1) return;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void Put(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= PixelScreen.Width || y >= PixelScreen.Height) return;
            pixels[(PixelScreen.Height - 1 - y) * PixelScreen.Width + x] = color;   // the texture's rows run bottom-up
        }

        private void OnDestroy()
        {
            if (_pathTexture != null) Destroy(_pathTexture);
        }
    }
}
