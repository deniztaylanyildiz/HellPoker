using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>The records across every run, in a menu box: runs, freedoms, damnations, the fastest freedom, and freedoms per demon.</summary>
    public sealed class RecordsView : MonoBehaviour, IRecordsView
    {
        private const int SortingOrder = 105;

        private Canvas _canvas;
        private Text _body;

        public event Action BackPressed;

        public bool IsVisible => _canvas.enabled;

        public static RecordsView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("RecordsCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<RecordsView>();
            view._canvas = canvas;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            MenuBackdrop.Create(screen);   // the title screen's art stays behind its sub-screens

            _panel = UiFactory.CreatePanel("RecordsPanel", screen);
            _panel.rectTransform.PlaceTL(96, 24, PixelScreen.Width - 192, PixelScreen.Height - 48);

            UiFactory.CreateText("Title", screen, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.RecordsTitle)
                .rectTransform.PlaceTL(0, 36, PixelScreen.Width, 16);

            _body = UiFactory.CreateText("Body", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter);
            _body.rectTransform.PlaceTL(104, 58, PixelScreen.Width - 208, 152);
            _body.lineSpacing = 1.1f;
            _right = UiFactory.CreateText("BodyRight", screen, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _right.lineSpacing = 1.1f;
            _right.gameObject.SetActive(false);

            Button back = UiFactory.CreateButton("RecordsBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Blood);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 80) / 2, PixelScreen.Height - 56, 80, 20);
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        /// <summary>Splits the lines into two columns (the game's records: the runs left, the circles and the locks right).</summary>
        public const string ColumnBreak = RecordsLayout.ColumnBreak;

        private Image _panel;
        private Text _right;

        public void ShowLines(IReadOnlyList<string> lines)
        {
            lines = lines ?? new string[0];
            int split = lines.ToList().IndexOf(ColumnBreak);
            bool two = split >= 0;
            LayOut(two);
            _right.text = two ? string.Join("\n", lines.Skip(split + 1)) : "";
            _body.text = string.Join("\n", two ? lines.Take(split) : lines);
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        /// <summary>Two columns need the wide panel; one keeps the narrow, centred one (the Boss Simulator's).</summary>
        private void LayOut(bool two)
        {
            _panel.rectTransform.PlaceTL(two ? 12 : 96, 24, two ? PixelScreen.Width - 24 : PixelScreen.Width - 192, PixelScreen.Height - 48);
            _body.alignment = two ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            _body.rectTransform.PlaceTL(two ? 24 : 104, 58, two ? 212 : PixelScreen.Width - 208, 152);
            _right.rectTransform.PlaceTL(248, 58, 212, 152);
            _right.gameObject.SetActive(two);
        }

        public void Show(RecordBook records, IReadOnlyList<DealerCard> dealers, string phase2 = null)
        {
            LayOut(false);
            var lines = new List<string>
            {
                string.Format(UiText.RecordsRunsFormat, records.RunsStarted),
                string.Format(UiText.RecordsAbsolvedFormat, records.Absolutions),
                string.Format(UiText.RecordsDamnedFormat, records.Damnations),
                records.FastestAbsolution.HasValue ? string.Format(UiText.RecordsFastestFormat, records.FastestAbsolution.Value) : UiText.RecordsFastestNone,
                "",
                string.Format(UiText.RecordsLuciferReachedFormat, records.LuciferReached),
                string.Format(UiText.RecordsLuciferDefeatedFormat, records.LuciferDefeated),
                records.FewestLuciferAttempts.HasValue
                    ? string.Format(UiText.RecordsFewestAttemptsFormat, records.FewestLuciferAttempts.Value)
                    : UiText.RecordsFewestAttemptsNone,
                string.Format(UiText.RecordsWildBillFormat, records.WildBillEscapes),
                string.Format(UiText.RecordsBackfiresFormat, records.BackfiresSeen),
                ""
            };
            foreach (DealerCard dealer in dealers)
                lines.Add(string.Format(UiText.RecordsDealerFormat, UiText.GenitiveOf(UiText.Dealer(dealer.Id)), records.AbsolutionsAt(dealer.Id)));
            lines.Add(string.Format(UiText.RecordsClassesFormat, string.Join(" · ",
                HellPoker.Core.Sinners.SinnerRoster.All.Select(c => UiText.SinnerName(c.Id) + " " + records.AbsolutionsAs(c.Id)))));
            if (!string.IsNullOrEmpty(phase2))
            {
                lines.Add("");
                lines.Add(phase2);
            }
            _body.text = string.Join("\n", lines);

            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }
    }
}
