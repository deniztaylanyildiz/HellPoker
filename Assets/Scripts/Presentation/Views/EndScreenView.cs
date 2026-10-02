using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The end of a run, in two moods: ABSOLVED in gold on the stone wall, DAMNED in hellfire red on the burning wall.
    /// Below, the run's story (hands, lowest and highest sentence, best hand, tables, the soul) and NEW GAME / MENU.
    /// </summary>
    public sealed class EndScreenView : MonoBehaviour, IEndScreenView
    {
        private const int SortingOrder = 108;

        private Canvas _canvas;
        private Image _background;
        private Sprite _calm;
        private Sprite _burning;
        private Text _title;
        private Text _subtitle;
        private Text _story;
        private DealerAnimationLibrary _library;
        private SpriteFrameAnimator _eyes;
        private float _eyesLeft;

        /// <summary>How long his eyes burn on the "Morning Star falls" screen before they go out.</summary>
        private const float EyesSeconds = 1.6f;

        public event Action NewGamePressed;
        public event Action MenuPressed;

        public bool IsVisible => _canvas.enabled;

        /// <summary>True once the Morning Star's eyes have gone out on this screen (or were never shown).</summary>
        public bool EyesOut => _eyesLeft <= 0f;

        public static EndScreenView Create(Transform parent, DealerAnimationLibrary library = null)
        {
            Canvas canvas = UiFactory.CreateScreen("EndCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<EndScreenView>();
            view._canvas = canvas;
            view._library = library;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            _background = UiFactory.CreateSprite("Background", screen, UiArt.Background, Palette.Night);
            _background.rectTransform.Stretch();
            _background.raycastTarget = true;
            _calm = _background.sprite;
            _burning = UiArt.Sprite(UiArt.BackgroundHell);

            Image panel = UiFactory.CreatePanel("EndPanel", screen, hot: true);
            // Wide enough for the longest title ("THE MORNING STAR FALLS", 352 px on the 16 px grid).
            panel.rectTransform.PlaceTL(56, 24, PixelScreen.Width - 112, PixelScreen.Height - 48);

            _title = UiFactory.CreateText("Title", screen, "", 32, Palette.GoldLight, style: FontStyle.Bold).WithShadow();
            _title.rectTransform.PlaceTL(0, 36, PixelScreen.Width, 32);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;

            _subtitle = UiFactory.CreateText("Subtitle", screen, "", 8, Palette.Bone).WithShadow();
            _subtitle.rectTransform.PlaceTL(80, 76, PixelScreen.Width - 160, 9);

            UiFactory.CreateSprite("Divider", screen, UiArt.Divider).rectTransform.PlaceTL((PixelScreen.Width - 48) / 2, 90, 48, 3);

            _story = UiFactory.CreateText("Story", screen, "", 8, Palette.BoneMid, TextAnchor.UpperCenter);
            _story.rectTransform.PlaceTL(88, 100, PixelScreen.Width - 176, 100);
            _story.lineSpacing = 1.25f;

            // The Morning Star's eyes, in a dark box beside the story (only when he falls).
            _eyes = DealerView.CreatePortrait(screen, 80, 100);
            _eyes.FallbackColor = Palette.Black;
            _eyes.transform.parent.gameObject.SetActive(false);

            Button newGame = UiFactory.CreateButton("EndNewGameButton", screen, UiText.NewGame, 8, out _, ButtonSkin.Ember);
            ((RectTransform)newGame.transform).PlaceTL(PixelScreen.Width / 2 - 108, PixelScreen.Height - 56, 104, 20);
            newGame.onClick.AddListener(() => NewGamePressed?.Invoke());

            Button menu = UiFactory.CreateButton("EndMenuButton", screen, UiText.ToMenu, 8, out _, ButtonSkin.Ash);
            ((RectTransform)menu.transform).PlaceTL(PixelScreen.Width / 2 + 4, PixelScreen.Height - 56, 104, 20);
            menu.onClick.AddListener(() => MenuPressed?.Invoke());
        }

        public void Show(RunSummary summary)
        {
            bool free = summary.Absolved;
            bool fallen = free && summary.BeatLucifer;
            if (_burning != null)
                _background.sprite = free ? _calm : _burning;
            _title.text = fallen ? UiText.MorningStarFallsTitle
                : free && summary.WildBill ? UiText.WildBillTitle
                : free ? UiText.AbsolvedTitle : UiText.DamnedTitle;
            _title.fontSize = _title.text.Length > 12 ? 16 : 32;   // long titles drop to the 16 px grid to fit the screen
            _title.color = free ? Palette.GoldLight : Palette.Hell;
            _subtitle.text = fallen ? UiText.MorningStarFallsSubtitle
                : free && summary.WildBill ? UiText.WildBillSubtitle
                : free ? UiText.AbsolvedSubtitle : UiText.DamnedSubtitle;

            // His eyes burn a moment longer, then go out; the story moves aside for them.
            _eyes.transform.parent.gameObject.SetActive(fallen);
            if (fallen)
            {
                _eyes.Play(_library?.Get(DealerRoster.LuciferId, DealerAnimation.ReRaise));
                _eyesLeft = EyesSeconds;
            }
            else
            {
                _eyesLeft = 0f;
            }
            _story.alignment = fallen ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            _story.rectTransform.PlaceTL(fallen ? 196 : 88, 100, fallen ? PixelScreen.Width - 196 - 80 : PixelScreen.Width - 176, 100);

            var lines = new List<string>
            {
                string.Format(UiText.EndHandsFormat, summary.HandsPlayed),
                string.Format(UiText.EndLowestFormat, summary.LowestYears),
                summary.SoulStaked ? UiText.EndHighestSoul : string.Format(UiText.EndHighestFormat, summary.HighestYears),
                summary.BestHand.HasValue ? string.Format(UiText.EndBestFormat, UiText.CategoryName(summary.BestHand.Value)) : UiText.EndBestNone,
                string.Format(UiText.EndDealersFormat, string.Join(", ", summary.DealerNames)),
                summary.SoulStaked ? UiText.EndSoulStaked : UiText.EndSoulKept,
                fallen ? string.Format(UiText.EndBeatLuciferFormat, summary.LuciferAttempts)
                    : summary.LuciferAttempts > 0 ? string.Format(UiText.EndLuciferTriedFormat, summary.LuciferAttempts)
                    : UiText.EndNeverMetLucifer
            };
            _story.text = string.Join("\n", lines);

            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        private void Update()
        {
            if (_eyesLeft <= 0f || !_canvas.enabled) return;
            _eyesLeft -= Time.unscaledDeltaTime * AnimationClock.Speed;
            if (_eyesLeft <= 0f)
                _eyes.Play(null);   // the eyes go out: only darkness is left in the box
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }
    }
}
