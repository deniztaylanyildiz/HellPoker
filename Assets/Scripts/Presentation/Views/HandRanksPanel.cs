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
    /// and what it pays under a dealer. Used on the rules screen and over the table (H).
    /// </summary>
    public sealed class HandRanksPanel : MonoBehaviour
    {
        public const int Width = 312;
        public const int Height = 178;
        private const int RowHeight = 11;
        private const int FirstRow = 26;

        private Text[] _values;
        private Text _footnote;
        private HandCategory[] _categories;

        public bool IsOpen => gameObject.activeSelf;

        public static HandRanksPanel Create(Transform parent, int x, int y, string footer = null)
        {
            Image panel = UiFactory.CreatePanel("HandRanks", parent, hot: true);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(x, y, Width, Height);

            var view = panel.gameObject.AddComponent<HandRanksPanel>();
            view.Build(panel.transform, footer);
            panel.gameObject.SetActive(false);
            return view;
        }

        private void Build(Transform panel, string footer)
        {
            _categories = System.Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();
            _values = new Text[_categories.Length];

            UiFactory.CreateText("Title", panel, UiText.HandRanksTitle, 8, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 6, Width, 8);
            UiFactory.CreateText("Subtitle", panel, UiText.HandRanksSubtitle, 8, Palette.BoneMid).rectTransform.PlaceTL(0, 15, Width, 9);

            for (int i = 0; i < _categories.Length; i++)
            {
                HandCategory category = _categories[i];
                int y = FirstRow + i * RowHeight;
                Color color = category == HandCategory.DeadMansHand ? Palette.GoldLight : Palette.Bone;

                UiFactory.CreateText("Name" + i, panel, UiText.CategoryName(category), 8, color, TextAnchor.MiddleLeft)
                    .rectTransform.PlaceTL(10, y, 112, RowHeight);
                UiFactory.CreateText("Example" + i, panel, UiText.HandExample(category), 8, Palette.LilacLight, TextAnchor.MiddleLeft)
                    .rectTransform.PlaceTL(124, y, 136, RowHeight);
                _values[i] = UiFactory.CreateText("Value" + i, panel, "", 8, color, TextAnchor.MiddleRight);
                _values[i].rectTransform.PlaceTL(Width - 58, y, 48, RowHeight);
            }

            // Under the ranks: the demon's announced cheat and what it does (at the table).
            _footnote = UiFactory.CreateText("Footnote", panel, "", 8, Palette.Ember, TextAnchor.UpperCenter);
            _footnote.rectTransform.PlaceTL(8, FirstRow + _categories.Length * RowHeight + 1, Width - 16, 18);

            if (!string.IsNullOrEmpty(footer))
                UiFactory.CreateText("Footer", panel, footer, 8, Palette.BoneDark).rectTransform.PlaceTL(0, Height - 11, Width, 9);
        }

        /// <summary>Fills in what each hand pays at this table and opens the panel.</summary>
        /// <param name="footnote">A line under the ranks (the demon's announced cheat); null for none.</param>
        public void Show(IPayoutInfo payouts, string footnote = null)
        {
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
