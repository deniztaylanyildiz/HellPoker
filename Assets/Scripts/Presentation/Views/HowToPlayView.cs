using System.Collections.Generic;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 2.2's How to Play inside the rules panel: <see cref="UiText.HowToPages"/> short pages, a picture from the game on the left
    /// (Art/HowTo/&lt;page&gt;_&lt;en|tr&gt;.png, shown at its own size — the pixels stay whole) and at most five lines on the right; the
    /// last page is the hands table. Without the picture the lines take the whole width. ‹ / › (the arrows, Q / E) turn the pages.
    /// </summary>
    public sealed class HowToPlayView : MonoBehaviour
    {
        /// <summary>The picture's box: a crop of the 480×270 screen at its own pixels.</summary>
        public const int ImageWidth = 200, ImageHeight = 112;

        private const int Left = 12, Top = 40, Gap = 10, LineGap = 4;

        private int _width;
        private Text _title;
        private Image _image;
        private readonly List<Text> _lines = new List<Text>();
        private Text _pager;
        private HandRanksPanel _hands;
        private Core.Game.IPayoutInfo _payouts;

        public int Page { get; private set; }

        /// <summary>The page's picture shows (for tests).</summary>
        public bool ImageShown => _image.gameObject.activeSelf && _image.sprite != null;

        /// <summary>The page's line boxes as they stand, for the layout tests: each text and its height against its room.</summary>
        public IReadOnlyList<Text> Lines => _lines;

        public Text Title => _title;
        public Text Pager => _pager;
        public RectTransform ImageRect => _image.rectTransform;

        public static HowToPlayView Create(Transform panel, int width, HandRanksPanel hands, Core.Game.IPayoutInfo payouts, int pagerY)
        {
            RectTransform root = UiFactory.CreateRect("HowToPlay", panel).Stretch();
            var view = root.gameObject.AddComponent<HowToPlayView>();
            view._width = width;
            view._hands = hands;
            view._payouts = payouts;
            view.Build(root, pagerY);
            return view;
        }

        private void Build(RectTransform root, int pagerY)
        {
            _title = UiFactory.CreateText("HowToTitle", root, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithOutline();
            _image = UiFactory.CreateImage("HowToImage", root, Color.white);
            _image.rectTransform.PlaceTL(Left, Top, ImageWidth, ImageHeight);
            _image.raycastTarget = false;
            UiFactory.AddBorder(_image.gameObject, Palette.Gold, 1f);
            for (int i = 0; i < 5; i++)
            {
                Text line = UiFactory.CreateText("HowToLine" + i, root, "", 8, Palette.Bone, TextAnchor.UpperLeft);
                line.lineSpacing = 1f;
                _lines.Add(line);
            }

            int centre = _width / 2;
            Button previous = UiFactory.CreateButton("HowToPrevious", root, "<", 8, out _, ButtonSkin.Ash);
            ((RectTransform)previous.transform).PlaceTL(centre - 150, pagerY, 24, 20);
            previous.onClick.AddListener(() => Turn(-1));
            _pager = UiFactory.CreateText("HowToPager", root, "", 8, Palette.Bone).WithOutline();
            _pager.rectTransform.PlaceTL(centre - 124, pagerY, 48, 20);
            Button next = UiFactory.CreateButton("HowToNext", root, ">", 8, out _, ButtonSkin.Ash);
            ((RectTransform)next.transform).PlaceTL(centre - 74, pagerY, 24, 20);
            next.onClick.AddListener(() => Turn(1));
        }

        private void OnEnable() => Lang.Changed += Render;
        private void OnDisable() => Lang.Changed -= Render;

        public void Show(int page)
        {
            Page = (page % UiText.HowToPages + UiText.HowToPages) % UiText.HowToPages;
            gameObject.SetActive(true);
            Render();
        }

        /// <summary>The next (or the previous) page, round and round.</summary>
        public void Turn(int step) => Show(Page + step);

        public void Hide()
        {
            gameObject.SetActive(false);
            _hands?.Hide();
        }

        /// <summary>The picture of a page in the language shown (the other language's, if only that one was made); null for none.</summary>
        public static Sprite ImageOf(int page)
        {
            string name = "HowTo/" + UiText.HowToImage(page);
            string mine = Lang.Current == Language.Turkish ? "_tr" : "_en", other = Lang.Current == Language.Turkish ? "_en" : "_tr";
            return UiArt.Sprite(name + mine) ?? UiArt.Sprite(name + other);
        }

        private void Render()
        {
            if (this == null || !gameObject.activeSelf) return;
            _pager.text = string.Format(UiText.HowToPagerFormat, Page + 1, UiText.HowToPages);
            bool hands = Page == UiText.HowToPages - 1;
            if (hands)
            {
                // The hands table is its own picture: the panel the table shows on H.
                _title.text = "";
                _image.gameObject.SetActive(false);
                foreach (Text line in _lines) line.text = "";
                _hands?.Show(_payouts);
                return;
            }
            _hands?.Hide();
            Sprite picture = ImageOf(Page);
            _image.gameObject.SetActive(picture != null);
            _image.sprite = picture;
            int textLeft = picture != null ? Left + ImageWidth + Gap : Left;
            int textWidth = _width - textLeft - Left;
            _title.text = UiText.HowToTitle(Page);
            _title.rectTransform.PlaceTL(textLeft, Top, textWidth, 9);
            string[] lines = UiText.HowToLines(Page);
            int y = Top + 14;
            for (int i = 0; i < _lines.Count; i++)
            {
                Text text = _lines[i];
                text.text = i < lines.Length ? "· " + lines[i] : "";
                text.rectTransform.PlaceTL(textLeft, y, textWidth, 40);
                int height = i < lines.Length ? Mathf.CeilToInt(text.preferredHeight) : 0;
                text.rectTransform.PlaceTL(textLeft, y, textWidth, Mathf.Max(1, height));
                y += height + LineGap;
            }
        }

        /// <summary>Where the last line ends (for the layout tests: it must stay above the pager).</summary>
        public int TextBottom
        {
            get
            {
                int bottom = 0;
                foreach (Text line in _lines)
                    if (!string.IsNullOrEmpty(line.text))
                        bottom = Mathf.Max(bottom, Mathf.RoundToInt(-line.rectTransform.anchoredPosition.y + line.rectTransform.sizeDelta.y));
                return bottom;
            }
        }
    }
}
