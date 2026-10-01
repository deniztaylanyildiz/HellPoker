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
    /// <summary>Lists what each winning hand forgives and highlights the last winner.</summary>
    public sealed class PayoutTableView : MonoBehaviour, IPayoutView
    {
        private readonly Dictionary<HandCategory, (Image row, Text name, Text value)> _rows =
            new Dictionary<HandCategory, (Image, Text, Text)>();

        public static PayoutTableView Create(Transform parent, Vector2 anchor, Vector2 position, Vector2 pivot, IPayoutInfo payouts,
            AnimationSequencer sequencer)
        {
            HandCategory[] categories = System.Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();
            const float rowHeight = 40f;
            var size = new Vector2(380f, 90f + rowHeight * categories.Length + 50f);

            Image panel = UiFactory.CreateImage("Payouts", parent, Palette.Felt);
            panel.rectTransform.Place(anchor, position, size, pivot);
            UiFactory.AddBorder(panel.gameObject, Palette.CardBack, 2f);

            var view = panel.gameObject.AddComponent<PayoutTableView>();
            view._sequencer = sequencer;

            UiFactory.CreateText("Title", panel.transform, UiText.PayoutsTitle, 24, Palette.Ember, style: FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(size.x, 40f));

            for (int i = 0; i < categories.Length; i++)
            {
                HandCategory category = categories[i];
                float y = -85f - i * rowHeight;

                Image row = UiFactory.CreateImage(category.ToString(), panel.transform, Color.clear);
                row.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(size.x - 24f, rowHeight - 4f));

                bool absolution = payouts.IsAbsolution(category);
                Color color = absolution ? Palette.Gold : Palette.Bone;
                Text name = UiFactory.CreateText("Name", row.transform, UiText.CategoryName(category), 22, color, TextAnchor.MiddleLeft,
                    absolution ? FontStyle.Bold : FontStyle.Normal);
                name.rectTransform.Stretch(10f);
                string valueText = absolution ? UiText.Absolution : "×" + payouts.GetMultiplier(category);
                Text value = UiFactory.CreateText("Value", row.transform, valueText, 22, color, TextAnchor.MiddleRight, FontStyle.Bold);
                value.rectTransform.Stretch(10f);

                view._rows[category] = (row, name, value);
            }

            UiFactory.CreateText("Loss", panel.transform, UiText.PayoutLoss, 18, Palette.MutedText)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(size.x, 30f));

            return view;
        }

        private AnimationSequencer _sequencer;

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
