using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// "Choose your sin": after the demon, before the first hand — a row of class cards (portrait, name, who they were, the
    /// ability and its price, the starting sentence) and a CHOOSE button on each, over the chosen demon's hall.
    /// Built on <see cref="Show"/>, so it adapts to however many classes the roster holds.
    /// </summary>
    public sealed class SinnerSelectView : MonoBehaviour, ISinnerSelectView
    {
        private const int SortingOrder = 112;
        private const int CardWidth = 148;
        private const int CardSpacing = 8;
        private const int CardsY = 34;
        private const int CardHeight = 222;

        /// <summary>Where the ability text starts, its margin, and the top of CHOOSE: the ability and the price live between.</summary>
        public const int AbilityTop = 152;
        public const int TextX = 8;
        public const int ChooseTop = CardHeight - 24;

        private Canvas _canvas;
        private SalonView _salon;
        private RectTransform _cards;

        public event Action<int> SinnerChosen;
        public event Action BackPressed;

        public bool IsVisible => _canvas.enabled;

        public static SinnerSelectView Create(Transform parent, SalonLibrary salons)
        {
            Canvas canvas = UiFactory.CreateScreen("SinnerSelectCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<SinnerSelectView>();
            view._canvas = canvas;
            view.Build(screen, salons);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen, SalonLibrary salons)
        {
            _salon = SalonView.Create(screen, salons);

            UiFactory.CreateText("Title", screen, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.ChooseSinnerTitle)
                .rectTransform.PlaceTL(0, 4, PixelScreen.Width, 16);
            UiFactory.CreateText("Subtitle", screen, "", 8, Palette.BoneMid).WithOutline().Localized(() => UiText.ChooseSinnerSubtitle)
                .rectTransform.PlaceTL(0, 21, PixelScreen.Width, 9);

            _cards = UiFactory.CreateRect("Cards", screen).Stretch();

            Button back = UiFactory.CreateButton("SinnerBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Ash);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL(8, 4, 56, 16);
            back.onClick.AddListener(() => BackPressed?.Invoke());
        }

        public void Show(IReadOnlyList<SinnerCard> sinners, string dealerId)
        {
            foreach (Transform child in _cards)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (!string.IsNullOrEmpty(dealerId))
                _salon.SetSalon(dealerId, SalonMode.Normal, false);

            int total = sinners.Count * CardWidth + (sinners.Count - 1) * CardSpacing;
            int left = (PixelScreen.Width - total) / 2;
            for (int i = 0; i < sinners.Count; i++)
                BuildCard(sinners[i], i, left + i * (CardWidth + CardSpacing));

            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildCard(SinnerCard sinner, int index, int x)
        {
            Image box = UiFactory.CreatePanel($"Sinner_{sinner.Id}", _cards);
            box.rectTransform.PlaceTL(x, CardsY, CardWidth, CardHeight);
            Transform card = box.transform;

            // The portrait at twice its size (48 → 96): whole pixels.
            Image frame = UiFactory.CreateImage("Frame", card, Palette.Black);
            frame.rectTransform.PlaceTL((CardWidth - 100) / 2, 6, 100, 100);
            UiFactory.AddBorder(frame.gameObject, Palette.Gold, 1f);
            Sprite portrait = UiArt.SinnerPortrait(sinner.Id);
            Image picture = UiFactory.CreateImage("Portrait", frame.transform, portrait != null ? Color.white : Palette.Plum);
            picture.sprite = portrait;
            picture.raycastTarget = false;
            picture.rectTransform.PlaceTL(2, 2, 96, 96);

            UiFactory.CreateText("Name", card, sinner.Name, 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline()
                .rectTransform.PlaceTL(0, 110, CardWidth, 8);
            UiFactory.CreateText("Title", card, sinner.Title, 8, Palette.BoneMid, TextAnchor.UpperCenter)
                .rectTransform.PlaceTL(6, 121, CardWidth - 12, 18);
            UiFactory.CreateText("Start", card, sinner.Start, 8, Palette.Ember, TextAnchor.UpperCenter)
                .rectTransform.PlaceTL(0, 140, CardWidth, 9);
            // The ability, then the price right under its real height (not a fixed one: a long line wraps), both above CHOOSE.
            Text ability = UiFactory.CreateText("Ability", card, sinner.Ability, 8, Palette.Bone, TextAnchor.UpperLeft);
            ability.rectTransform.PlaceTL(TextX, AbilityTop, CardWidth - 2 * TextX, 9);
            int abilityHeight = Mathf.CeilToInt(ability.preferredHeight);
            ability.rectTransform.PlaceTL(TextX, AbilityTop, CardWidth - 2 * TextX, abilityHeight);
            Text detail = UiFactory.CreateText("Detail", card, sinner.Detail, 8, Palette.LilacLight, TextAnchor.UpperLeft);
            detail.rectTransform.PlaceTL(TextX, AbilityTop + abilityHeight + 2, CardWidth - 2 * TextX, 9);
            detail.rectTransform.PlaceTL(TextX, AbilityTop + abilityHeight + 2, CardWidth - 2 * TextX, Mathf.CeilToInt(detail.preferredHeight));

            Button choose = UiFactory.CreateButton($"ChooseSinner{index}", card, UiText.ChooseSinner, 8, out _, ButtonSkin.Ember);
            ((RectTransform)choose.transform).PlaceTL((CardWidth - 88) / 2, ChooseTop, 88, 18);
            choose.onClick.AddListener(() => SinnerChosen?.Invoke(index));
        }
    }
}
