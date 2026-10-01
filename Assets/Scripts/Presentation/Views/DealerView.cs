using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The demon dealer: a framed portrait with a breathing aura, a name plate and a speech scroll.
    /// Lines wait their turn in the table's sequencer (a gloat never comes before the cards turn), then type themselves out
    /// without blocking input. The aura flares in a colour that matches the mood of the line.
    /// </summary>
    public sealed class DealerView : MonoBehaviour, IDealerView
    {
        private const float CharactersPerSecond = 55f;
        private const float FlareSeconds = 1.6f;

        private AnimationSequencer _sequencer;
        private Image _aura;
        private Image _portrait;
        private RectTransform _portraitRect;
        private Text _name;
        private Text _title;
        private CanvasGroup _speech;
        private RectTransform _speechRect;
        private Text _line;

        private string _fullLine = "";
        private float _typed;
        private float _flare;
        private Color _auraColor = Palette.AuraFor(Tone.Neutral);
        private Tone _tone;

        public static DealerView Create(Transform parent, Vector2 anchor, Vector2 position, AnimationSequencer sequencer)
        {
            var size = new Vector2(360f, 760f);
            RectTransform root = UiFactory.CreateRect("Dealer", parent).Place(anchor, position, size, new Vector2(0f, 1f));
            var view = root.gameObject.AddComponent<DealerView>();
            view._sequencer = sequencer;
            view.Build(root, size);
            return view;
        }

        private void Build(RectTransform root, Vector2 size)
        {
            var portraitSize = new Vector2(320f, 400f);
            var portraitCenter = new Vector2(size.x / 2f, -portraitSize.y / 2f - 10f);

            _aura = UiFactory.CreateSprite("Aura", root, UiArt.Glow);
            _aura.rectTransform.Place(new Vector2(0f, 1f), portraitCenter, portraitSize * 1.6f);
            _aura.color = Color.clear;

            Image backing = UiFactory.CreateImage("PortraitBacking", root, new Color(0.05f, 0.01f, 0.01f));
            backing.rectTransform.Place(new Vector2(0f, 1f), portraitCenter, portraitSize);

            _portrait = UiFactory.CreateImage("Portrait", root, Color.white);
            _portrait.raycastTarget = false;
            _portrait.preserveAspect = true;
            _portraitRect = _portrait.rectTransform;
            _portraitRect.Place(new Vector2(0f, 1f), portraitCenter, portraitSize - new Vector2(16f, 16f));

            UiFactory.CreateFrame("PortraitFrame", root, 0.75f).rectTransform.Place(new Vector2(0f, 1f), portraitCenter, portraitSize + new Vector2(22f, 22f));

            float nameY = -portraitSize.y - 52f;
            _name = UiFactory.CreateText("Name", root, "", 40, Palette.Gold, style: FontStyle.Bold).WithShadow(3f);
            _name.rectTransform.Place(new Vector2(0f, 1f), new Vector2(size.x / 2f, nameY), new Vector2(size.x + 40f, 50f));
            _title = UiFactory.CreateText("Title", root, "", 22, Palette.MutedText, style: FontStyle.Italic).WithShadow();
            _title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(size.x / 2f, nameY - 36f), new Vector2(size.x + 40f, 30f));

            Image divider = UiFactory.CreateSprite("Divider", root, UiArt.Divider);
            divider.rectTransform.Place(new Vector2(0f, 1f), new Vector2(size.x / 2f, nameY - 64f), new Vector2(300f, 28f));

            Image scroll = UiFactory.CreateSprite("Speech", root, UiArt.Speech, Palette.Bone, sliced: true);
            _speechRect = scroll.rectTransform;
            _speechRect.Place(new Vector2(0f, 1f), new Vector2(size.x / 2f, nameY - 82f), new Vector2(size.x, 190f), new Vector2(0.5f, 1f));
            _speech = scroll.gameObject.AddComponent<CanvasGroup>();
            _speech.alpha = 0f;

            _line = UiFactory.CreateText("Line", scroll.transform, "", 25, Palette.SepiaInk, TextAnchor.MiddleCenter, FontStyle.Italic);
            _line.rectTransform.Stretch(22f);
            _line.lineSpacing = 0.95f;
        }

        public void SetDealer(DealerCard dealer)
        {
            _sequencer.Do(() =>
            {
                Sprite portrait = UiArt.Portrait(dealer.Id);
                _portrait.sprite = portrait;
                _portrait.color = portrait != null ? Color.white : Palette.Felt;
                _name.text = dealer.Name;
                _title.text = dealer.Title;
                _fullLine = "";
                _line.text = "";
                _speech.alpha = 0f;
            });
        }

        public void Say(string line, Tone tone)
        {
            _sequencer.Do(() =>
            {
                _fullLine = line ?? "";
                _typed = 0f;
                _line.text = "";
                _tone = tone;
                _auraColor = Palette.AuraFor(tone);
                _flare = 1f;
            });
        }

        private void Update()
        {
            float time = Time.unscaledTime;

            if (_typed < _fullLine.Length)
            {
                _typed = Mathf.Min(_fullLine.Length, _typed + Time.unscaledDeltaTime * CharactersPerSecond);
                _line.text = _fullLine.Substring(0, Mathf.FloorToInt(_typed));
            }

            float target = _fullLine.Length > 0 ? 1f : 0f;
            _speech.alpha = Mathf.MoveTowards(_speech.alpha, target, Time.unscaledDeltaTime * 4f);
            _speechRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, _speech.alpha);

            // A slow breathing glow, flaring up whenever the dealer speaks.
            _flare = Mathf.Max(0f, _flare - Time.unscaledDeltaTime / FlareSeconds);
            float breath = 0.5f + 0.5f * Mathf.Sin(time * 1.4f);
            Color aura = _auraColor;
            aura.a = 0.22f + 0.12f * breath + 0.5f * _flare;
            _aura.color = aura;
            _aura.rectTransform.localScale = Vector3.one * (1f + 0.04f * breath + 0.08f * _flare);

            // Gloating dealers lean in; annoyed ones twitch.
            float shake = _tone == Tone.Good ? Mathf.Sin(time * 60f) * 3f * _flare : 0f;
            float lean = _tone == Tone.Bad || _tone == Tone.Doom ? 0.04f * _flare : 0f;
            _portraitRect.localScale = Vector3.one * (1f + lean);
            _portraitRect.localRotation = Quaternion.Euler(0f, 0f, shake * 0.3f);
        }
    }
}
