using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>The remaining sentence: an animated year counter and a bar filling toward damnation.</summary>
    public sealed class SentenceView : MonoBehaviour, ISentenceView
    {
        private const float CountDuration = 0.8f;

        private Text _years;
        private Text _damnation;
        private RectTransform _barFill;
        private float _barWidth;

        private int _damnationYears = 1;
        private float _shown;
        private float _from;
        private int _target;
        private float _elapsed = CountDuration;

        private AnimationSequencer _sequencer;
        private bool _pulsing;

        public static SentenceView Create(Transform parent, Vector2 anchor, Vector2 position, Vector2 pivot, AnimationSequencer sequencer)
        {
            var size = new Vector2(400f, 170f);
            RectTransform root = UiFactory.CreateRect("Sentence", parent).Place(anchor, position, size, pivot);
            var view = root.gameObject.AddComponent<SentenceView>();
            view._sequencer = sequencer;
            view.Build(root, size);
            return view;
        }

        /// <summary>Makes the counter throb like a heartbeat (final stretch).</summary>
        public void SetPulsing(bool pulsing)
        {
            _pulsing = pulsing;
            if (!pulsing)
            {
                _years.color = Palette.Gold;
                _years.rectTransform.localScale = Vector3.one;
            }
        }

        private void Build(RectTransform root, Vector2 size)
        {
            _years = UiFactory.CreateText("Years", root, "", 84, Palette.Gold, style: FontStyle.Bold);
            _years.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(size.x, 96f));

            UiFactory.CreateText("Label", root, UiText.YearsLabel, 22, Palette.MutedText, style: FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(size.x, 30f));

            _barWidth = size.x;
            Image bar = UiFactory.CreateImage("Bar", root, Palette.Slot);
            bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(_barWidth, 12f));
            UiFactory.AddBorder(bar.gameObject, Palette.CardBack, 1f);

            Image fill = UiFactory.CreateImage("Fill", bar.transform, Palette.Ember);
            _barFill = fill.rectTransform;
            _barFill.anchorMin = new Vector2(0f, 0f);
            _barFill.anchorMax = new Vector2(0f, 1f);
            _barFill.pivot = new Vector2(0f, 0.5f);
            _barFill.anchoredPosition = Vector2.zero;

            _damnation = UiFactory.CreateText("Damnation", root, "", 18, Palette.MutedText);
            _damnation.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(size.x, 24f));
        }

        public void SetDamnationLimit(int years)
        {
            _damnationYears = Mathf.Max(1, years);
            _damnation.text = string.Format(UiText.DamnationFormat, years);
        }

        public void SetYears(int years, bool animate)
        {
            _sequencer.Do(() =>
            {
                _target = years;
                _from = _shown;
                _elapsed = animate ? 0f : CountDuration;
                if (!animate)
                    Apply(years);
            });
        }

        private void Update()
        {
            if (_pulsing)
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * 2.6f)), 6f);
                _years.color = Color.Lerp(Palette.Gold, Palette.Bone, beat);
                _years.rectTransform.localScale = Vector3.one * (1f + 0.06f * beat);
            }

            if (_elapsed >= CountDuration) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / CountDuration);
            Apply(Mathf.Lerp(_from, _target, 1f - Mathf.Pow(1f - t, 3f)));
        }

        private void Apply(float years)
        {
            _shown = years;
            _years.text = Mathf.RoundToInt(years).ToString();
            _barFill.sizeDelta = new Vector2(_barWidth * Mathf.Clamp01(years / _damnationYears), 0f);
        }
    }
}
