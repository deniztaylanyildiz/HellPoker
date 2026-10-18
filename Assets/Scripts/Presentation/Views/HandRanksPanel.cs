using System.Linq;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The ranking of hands for players new to poker: strongest first (the Dead Man's Hand on top), an example of each,
    /// and what it pays under a dealer. Used on the rules screen and over the table (H). Phase 3: at a chapter's table it is
    /// also the payouts panel — under the ranks, how the table pays (the notes), and it grows to hold them.
    /// </summary>
    public sealed class HandRanksPanel : MonoBehaviour
    {
        public const int Width = 312;
        public const int Height = 178;
        private const int RowHeight = 11;
        private const int FirstRow = 26;
        private const int LineHeight = 9;

        private Text[] _values;
        private Text _notes;
        private Text _footnote;
        private Text _footer;
        private HandCategory[] _categories;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>The value column as shown, strongest first (tests).</summary>
        public string[] Values => _values.Select(v => v.text).ToArray();

        /// <summary>The notes under the ranks as shown (tests); empty for none.</summary>
        public string NotesShown => _notes.text;

        public static HandRanksPanel Create(Transform parent, int x, int y, System.Func<string> footer = null)
        {
            Image panel = UiFactory.CreatePanel("HandRanks", parent, hot: true);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(x, y, Width, Height);

            var view = panel.gameObject.AddComponent<HandRanksPanel>();
            view.Build(panel.transform, footer);
            panel.gameObject.SetActive(false);
            return view;
        }

        private void Build(Transform panel, System.Func<string> footer)
        {
            _categories = System.Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();
            _values = new Text[_categories.Length];

            UiFactory.CreateText("Title", panel, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.HandRanksTitle)
                .rectTransform.PlaceTL(0, 6, Width, 8);
            UiFactory.CreateText("Subtitle", panel, "", 8, Palette.BoneMid).Localized(() => UiText.HandRanksSubtitle).rectTransform.PlaceTL(0, 15, Width, 9);

            for (int i = 0; i < _categories.Length; i++)
            {
                HandCategory category = _categories[i];
                int y = FirstRow + i * RowHeight;
                Color color = category == HandCategory.DeadMansHand ? Palette.GoldLight : Palette.Bone;

                UiFactory.CreateText("Name" + i, panel, "", 8, color, TextAnchor.MiddleLeft).Localized(() => UiText.CategoryName(category))
                    .rectTransform.PlaceTL(10, y, 112, RowHeight);
                UiFactory.CreateText("Example" + i, panel, "", 8, Palette.LilacLight, TextAnchor.MiddleLeft).Localized(() => UiText.HandExample(category))
                    .rectTransform.PlaceTL(124, y, 136, RowHeight);
                _values[i] = UiFactory.CreateText("Value" + i, panel, "", 8, color, TextAnchor.MiddleRight);
                _values[i].rectTransform.PlaceTL(Width - 58, y, 48, RowHeight);
            }

            // Under the ranks: how the table pays (a chapter's table), then the demon's announced cheat and what it does.
            _notes = UiFactory.CreateText("Notes", panel, "", 8, Palette.BoneMid, TextAnchor.UpperCenter);
            _footnote = UiFactory.CreateText("Footnote", panel, "", 8, Palette.Ember, TextAnchor.UpperCenter);
            if (footer != null)
                _footer = UiFactory.CreateText("Footer", panel, "", 8, Palette.BoneDark).Localized(footer);
            LayOut(0);
        }

        /// <summary>The notes' lines under the ranks, the footnote under them, the footer at the bottom; the panel grows by the notes.</summary>
        private void LayOut(int noteLines)
        {
            int ranksEnd = FirstRow + _categories.Length * RowHeight + 1;
            int notesHeight = noteLines * LineHeight;
            _notes.rectTransform.PlaceTL(8, ranksEnd, Width - 16, Mathf.Max(1, notesHeight));
            _footnote.rectTransform.PlaceTL(8, ranksEnd + notesHeight + (noteLines > 0 ? 2 : 0), Width - 16, 18);
            int height = Height + notesHeight + (noteLines > 0 ? 2 : 0);
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(Width, height);
            if (_footer != null) _footer.rectTransform.PlaceTL(0, height - 11, Width, 9);
        }

        /// <summary>Fills in what each hand pays at this table and opens the panel.</summary>
        /// <param name="footnote">A line under the ranks (the demon's announced cheat); null for none.</param>
        /// <param name="notes">How the table pays (lines under the ranks); null for none.</param>
        public void Show(IPayoutInfo payouts, string footnote = null, string notes = null)
        {
            _notes.text = notes ?? "";
            LayOut(string.IsNullOrEmpty(notes) ? 0 : notes.Split('\n').Length);
            _footnote.text = footnote ?? "";
            for (int i = 0; i < _categories.Length; i++)
                _values[i].text = payouts == null ? ""
                    : payouts.IsAbsolution(_categories[i]) ? UiText.Absolution : "×" + payouts.GetMultiplier(_categories[i]);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
