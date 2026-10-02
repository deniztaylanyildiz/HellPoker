using System;
using System.Collections.Generic;
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
            Image background = UiFactory.CreateSprite("Background", screen, UiArt.Background, Palette.Night);
            background.rectTransform.Stretch();
            background.raycastTarget = true;

            Image panel = UiFactory.CreatePanel("RecordsPanel", screen);
            panel.rectTransform.PlaceTL(96, 24, PixelScreen.Width - 192, PixelScreen.Height - 48);

            UiFactory.CreateText("Title", screen, UiText.RecordsTitle, 16, Palette.GoldLight, style: FontStyle.Bold).WithShadow()
                .rectTransform.PlaceTL(0, 36, PixelScreen.Width, 16);

            _body = UiFactory.CreateText("Body", screen, "", 8, Palette.Bone, TextAnchor.UpperCenter);
            _body.rectTransform.PlaceTL(104, 58, PixelScreen.Width - 208, 152);
            _body.lineSpacing = 1.1f;

            Button back = UiFactory.CreateButton("RecordsBackButton", screen, UiText.Back, 8, out _, ButtonSkin.Blood);
            ((RectTransform)back.transform).PlaceTL((PixelScreen.Width - 80) / 2, PixelScreen.Height - 56, 80, 20);
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        public void Show(RecordBook records, IReadOnlyList<DealerCard> dealers)
        {
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
                ""
            };
            foreach (DealerCard dealer in dealers)
                lines.Add(string.Format(UiText.RecordsDealerFormat, dealer.Name, records.AbsolutionsAt(dealer.Id)));
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
