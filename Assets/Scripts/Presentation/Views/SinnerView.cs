using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.1: the player's class portrait in the chapter table's panel — the demon's <see cref="DealerView"/> in small. The presenter sends
    /// a <see cref="SinnerMood"/>; the view picks the animation: Calm the idle loop, Tense a tense loop in its place, Pleased / Elated / Hurt a
    /// one-shot (happy, joy, hurt) and back to the loop. The picture keeps its own pixels: a missing state stands in with idle, a missing idle
    /// with the class's 48×48 portrait (centred, not stretched), nothing at all with a flat colour.
    /// </summary>
    public sealed class SinnerView : MonoBehaviour
    {
        public const int Size = SinnerAnimationLibrary.FrameSize;

        private AnimationSequencer _sequencer;
        private SinnerAnimationLibrary _library;
        private SpriteFrameAnimator _portrait;
        private string _classId;
        private bool _tense;
        private bool _reacting;

        /// <summary>The mood last shown and the animation playing (for tests).</summary>
        public SinnerMood Mood { get; private set; } = SinnerMood.Calm;

        public SinnerAnimation Playing { get; private set; } = SinnerAnimation.Idle;

        public SpriteFrameAnimator Portrait => _portrait;

        public static SinnerView Create(Transform parent, int x, int y, AnimationSequencer sequencer, SinnerAnimationLibrary library)
        {
            RectTransform root = UiFactory.CreateRect("SinnerPortrait", parent).PlaceTL(x, y, Size, Size);
            var view = root.gameObject.AddComponent<SinnerView>();
            view._sequencer = sequencer;
            view._library = library;
            Image portrait = UiFactory.CreateImage("Portrait", root, Palette.Dusk);
            portrait.raycastTarget = false;
            portrait.rectTransform.PlaceTL(0, 0, Size, Size);
            view._portrait = portrait.gameObject.AddComponent<SpriteFrameAnimator>();
            view._portrait.FallbackColor = Palette.Dusk;
            return view;
        }

        /// <summary>The class sitting at the table (right away: the panel shows with the table).</summary>
        public void SetClass(string classId)
        {
            if (classId == _classId) return;
            _classId = classId;
            _tense = false;
            _reacting = false;
            Mood = SinnerMood.Calm;
            PlayBase();
        }

        /// <summary>A mood in its turn in the table's queue (a smile never comes before the cards turn).</summary>
        public void SetMood(SinnerMood mood) => _sequencer.Do(() => Apply(mood));

        /// <summary>Which one-shot a mood plays before the portrait goes back to its loop; null for none.</summary>
        public static SinnerAnimation? ReactionTo(SinnerMood mood)
        {
            switch (mood)
            {
                case SinnerMood.Pleased: return SinnerAnimation.Happy;
                case SinnerMood.Elated: return SinnerAnimation.Joy;
                case SinnerMood.Hurt: return SinnerAnimation.Hurt;
                default: return null;
            }
        }

        private void Apply(SinnerMood mood)
        {
            Mood = mood;
            if (mood == SinnerMood.Calm || mood == SinnerMood.Tense) _tense = mood == SinnerMood.Tense;
            SinnerAnimation? reaction = ReactionTo(mood);
            if (reaction.HasValue)
            {
                _reacting = true;
                Play(reaction.Value, EndReaction);
                return;
            }
            _reacting = false;
            PlayBase();
        }

        private void EndReaction()
        {
            _reacting = false;
            PlayBase();
        }

        /// <summary>A one-shot ends at once (skip): back to the loop.</summary>
        public void Finish()
        {
            if (!_reacting) return;
            _reacting = false;
            PlayBase();
        }

        private void PlayBase() => Play(_tense ? SinnerAnimation.Tense : SinnerAnimation.Idle);

        private void Play(SinnerAnimation animation, System.Action done = null)
        {
            Playing = animation;
            SpriteClip clip = _library?.Get(_classId, animation);
            // Whole pixels: the image takes the frame's own size, centred in the box (the 48×48 portrait is never stretched to 64).
            Vector2 size = clip != null && clip.Frames.Length > 0 && clip.Frames[0] != null ? clip.Frames[0].rect.size : new Vector2(Size, Size);
            int w = Mathf.Min(Size, Mathf.RoundToInt(size.x)), h = Mathf.Min(Size, Mathf.RoundToInt(size.y));
            _portrait.GetComponent<RectTransform>().PlaceTL((Size - w) / 2, (Size - h) / 2, w, h);
            _portrait.Play(clip, done);
        }
    }
}
