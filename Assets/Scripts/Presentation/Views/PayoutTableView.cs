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
    /// <summary>Lists what each winning hand forgives under the current dealer, and highlights the last winner.</summary>
    public sealed class PayoutTableView : MonoBehaviour, IPayoutView
    {
        private const float RowHeight = 36f;

        private readonly Dictionary<HandCategory, (Image row, Text name, Text value)> _rows =
            new Dictionary<HandCategory, (Image, Text, Text)>();

        private AnimationSequencer _sequencer;
        private Text _loss;

        public static PayoutTableView Create(Transform parent, Vector2 anchor, Vector2 position, Vector2 pivot, AnimationSequencer sequencer)
        {
            HandCategory[] categories = System.Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();
            var size = new Vector2(400f, 96f + RowHeight * categories.Length + 74f);

            Image panel = UiFactory.CreatePanel("Payouts", parent);
            panel.rectTransform.Place(anchor, position, size, pivot);

            var view = panel.gameObject.AddComponent<PayoutTableView>();
            view._sequencer = sequencer;

            UiFactory.CreateText("Title", panel.transform, UiText.PayoutsTitle, 24, Palette.Gold, style: FontStyle.Bold).WithShadow()
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(size.x, 36f));
            UiFactory.CreateSprite("Divider", panel.transform, UiArt.Divider)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(300f, 24f));

            for (int i = 0; i < categories.Length; i++)
            {
                HandCategory category = categories[i];
                float y = -96f - i * RowHeight;

                Image row = UiFactory.CreateImage(category.ToString(), panel.transform, Color.clear);
                row.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(size.x - 36f, RowHeight - 4f));

                Text name = UiFactory.CreateText("Name", row.transform, UiText.CategoryName(category), 23, Palette.Bone, TextAnchor.MiddleLeft);
                name.rectTransform.Stretch(10f);
                Text value = UiFactory.CreateText("Value", row.transform, "", 21, Palette.Bone, TextAnchor.MiddleRight, FontStyle.Bold);
                value.rectTransform.Stretch(10f);

                view._rows[category] = (row, name, value);
            }

            view._loss = UiFactory.CreateText("Loss", panel.transform, "", 18, Palette.MutedText, style: FontStyle.Italic);
            view._loss.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(size.x - 24f, 56f));

            return view;
        }

        public void SetTable(IPayoutInfo payouts)
        {
            _sequencer.Do(() =>
            {
                foreach (var pair in _rows)
                {
                    bool absolution = payouts.IsAbsolution(pair.Key);
                    Color color = absolution ? Palette.Gold : Palette.Bone;
                    pair.Value.name.color = color;
                    pair.Value.value.color = color;
                    pair.Value.value.text = absolution ? UiText.Absolution : "×" + payouts.GetMultiplier(pair.Key);
                }

                string loss = UiText.StakeShare(payouts.LossPercent);
                _loss.text = payouts.FoldPercentBeforeDraw == payouts.FoldPercentAfterDraw
                    ? string.Format(UiText.PayoutLossSameFoldFormat, loss, UiText.StakeShare(payouts.FoldPercentAfterDraw))
                    : string.Format(UiText.PayoutLossFormat, loss, UiText.StakeShare(payouts.FoldPercentBeforeDraw),
                        UiText.StakeShare(payouts.FoldPercentAfterDraw));
            });
        }

        public void Highlight(HandCategory? category)
        {
            _sequencer.Do(() =>
            {
                foreach (var pair in _rows)
                    pair.Value.row.color = pair.Key == category ? new Color(Palette.Ember.r, Palette.Ember.g, Palette.Ember.b, 0.35f) : Color.clear;
            });
        }
    }
}
