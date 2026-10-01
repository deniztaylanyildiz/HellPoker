using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The curtain between screens, in the same language as the hall wipe: the screen starts black and the curtain rises
    /// in whole 8 px rows. The new screen is already in place underneath, so nothing waits on the effect except input:
    /// the curtain swallows clicks while it moves, and <see cref="IsPlaying"/> tells the keyboard to wait.
    /// </summary>
    public sealed class ScreenTransitionView : MonoBehaviour, IScreenTransition
    {
        private const int SortingOrder = 300;
        private const int Step = 8;
        private const float Seconds = 0.18f;

        private Image _curtain;
        private float _elapsed = Seconds;

        public bool IsPlaying => _elapsed < Seconds;

        public static ScreenTransitionView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("TransitionCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var view = canvas.gameObject.AddComponent<ScreenTransitionView>();
            view._curtain = UiFactory.CreateImage("Curtain", screen, Palette.Black);
            RectTransform rect = view._curtain.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            view.Draw(0f);
            return view;
        }

        public void Play()
        {
            _elapsed = 0f;
            Draw(1f);
        }

        private void Update()
        {
            if (!IsPlaying) return;
            _elapsed += Time.unscaledDeltaTime * AnimationClock.Speed;
            Draw(1f - Mathf.Clamp01(_elapsed / Seconds));
        }

        private void Draw(float covered)
        {
            float rows = Mathf.Ceil(covered * PixelScreen.Height / Step) * Step;
            _curtain.rectTransform.sizeDelta = new Vector2(0f, Mathf.Min(rows, PixelScreen.Height));
            _curtain.enabled = rows > 0f;
            _curtain.raycastTarget = rows > 0f;
        }
    }
}
