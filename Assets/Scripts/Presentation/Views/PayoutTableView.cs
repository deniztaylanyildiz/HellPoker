using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>A pixel panel listing what each winning hand pays under the current dealer; the last winner is highlighted.</summary>
    public sealed class PayoutTableView : MonoBehaviour, IPayoutView
    {
        public const int Width = 104;
        private const int RowHeight = 10;
        private const int FirstRow = 20;

        private readonly Dictionary<HandCategory, (Image row, Text name, Text value)> _rows =
            new Dictionary<HandCategory, (Image, Text, Text)>();

        private AnimationSequencer _sequencer;
        private Text _loss;

        public static PayoutTableView Create(Transform parent, int x, int y, int height, AnimationSequencer sequencer)
        {
            HandCategory[] categories = System.Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();

            Image panel = UiFactory.CreatePanel("Payouts", parent);
            panel.rectTransform.PlaceTL(x, y, Width, height);

            var view = panel.gameObject.AddComponent<PayoutTableView>();
            view._sequencer = sequencer;

            UiFactory.CreateText("Title", panel.transform, UiText.PayoutsTitle, 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline()
                .rectTransform.PlaceTL(0, 6, Width, 8);
            UiFactory.CreateSprite("Divider", panel.transform, UiArt.Divider).rectTransform.PlaceTL((Width - 48) / 2, 15, 48, 3);

            for (int i = 0; i < categories.Length; i++)
            {
                HandCategory category = categories[i];
                Image row = UiFactory.CreateImage(category.ToString(), panel.transform, Color.clear);
                row.rectTransform.PlaceTL(4, FirstRow + i * RowHeight, Width - 8, RowHeight);

                Text name = UiFactory.CreateText("Name", row.transform, UiText.CategoryName(category), 8, Palette.Bone, TextAnchor.MiddleLeft);
                name.rectTransform.Stretch();
                name.rectTransform.offsetMin = new Vector2(3f, 0f);
                Text value = UiFactory.CreateText("Value", row.transform, "", 8, Palette.Bone, TextAnchor.MiddleRight);
                value.rectTransform.Stretch();
                value.rectTransform.offsetMax = new Vector2(-2f, 0f);

                view._rows[category] = (row, name, value);
            }

            int footerY = FirstRow + categories.Length * RowHeight + 3;
            view._loss = UiFactory.CreateText("Loss", panel.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            view._loss.rectTransform.PlaceTL(6, footerY, Width - 12, height - footerY - 4);
            view._loss.lineSpacing = 1f;

            return view;
        }

        public void SetTable(IPayoutInfo payouts)
        {
            _sequencer.Do(() =>
            {
                foreach (var pair in _rows)
                {
                    bool absolution = payouts.IsAbsolution(pair.Key);
                    Color color = absolution ? Palette.GoldLight : Palette.Bone;
                    pair.Value.name.color = color;
                    pair.Value.value.color = color;
                    pair.Value.value.text = absolution ? UiText.Absolution : "×" + payouts.GetMultiplier(pair.Key);
                }

                string loss = UiText.LossSurcharge(payouts.LossPercent);
                _loss.text = payouts.FoldPercentBeforeDraw == payouts.FoldPercentAfterDraw
                    ? string.Format(UiText.PayoutLossSameFoldFormat, loss, UiText.ShareShort(payouts.FoldPercentAfterDraw))
                    : string.Format(UiText.PayoutLossFormat, loss, UiText.ShareShort(payouts.FoldPercentBeforeDraw),
                        UiText.ShareShort(payouts.FoldPercentAfterDraw));
            });
        }

        public void Highlight(HandCategory? category)
        {
            _sequencer.Do(() =>
            {
                foreach (var pair in _rows)
                {
                    bool lit = pair.Key == category;
                    pair.Value.row.color = lit ? Palette.Crimson : Color.clear;
                    pair.Value.name.color = lit ? Palette.GoldLight : pair.Value.value.color;
                }
            });
        }
    }
}
