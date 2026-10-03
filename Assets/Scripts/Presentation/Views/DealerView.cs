using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The demon dealer: an animated 96×96 portrait in a menu box, a name plate and an old-RPG dialogue box.
    /// Moods become animations — a laugh when the player loses, a fit of rage when they win, a sly look on a re-raise —
    /// and the mouth moves while a line types out. In the final stretch the burning animation replaces the idle one.
    /// Lines wait their turn in the table's sequencer (a gloat never comes before the cards turn) but never block input.
    /// </summary>
    public sealed class DealerView : MonoBehaviour, IDealerView
    {
        public const int PortraitSize = 96;
        private const float CharactersPerSecond = 40f;

        private AnimationSequencer _sequencer;
        private DealerAnimationLibrary _library;
        private SpriteFrameAnimator _portrait;
        private Text _name;
        private Text _title;
        private Text _line;
        private GameObject _dialog;
        private Image _dialogBox;
        private Sprite _plainDialog;

        private string _dealerId;
        private string _fullLine = "";
        private float _typed;
        private bool _finalStretch;
        private bool _soul;
        private bool _reacting;

        public static DealerView Create(Transform parent, int x, int y, AnimationSequencer sequencer, DealerAnimationLibrary library)
        {
            RectTransform root = UiFactory.CreateRect("Dealer", parent).PlaceTL(x, y, PortraitSize + 8, 200);
            var view = root.gameObject.AddComponent<DealerView>();
            view._sequencer = sequencer;
            view._library = library;
            view.Build(root);
            return view;
        }

        /// <summary>The animated portrait alone, in its box (also used on the dealer choice screen).</summary>
        public static SpriteFrameAnimator CreatePortrait(Transform parent, int x, int y, bool hot = false)
        {
            Image box = UiFactory.CreatePanel("PortraitBox", parent, hot);
            box.rectTransform.PlaceTL(x, y, PortraitSize + 8, PortraitSize + 8);

            Image portrait = UiFactory.CreateImage("Portrait", box.transform, Palette.Dusk);
            portrait.raycastTarget = false;
            portrait.rectTransform.PlaceTL(4, 4, PortraitSize, PortraitSize);
            var animator = portrait.gameObject.AddComponent<SpriteFrameAnimator>();
            animator.FallbackColor = Palette.Dusk;
            return animator;
        }

        private void Build(RectTransform root)
        {
            _portrait = CreatePortrait(root, 0, 0);

            _name = UiFactory.CreateText("Name", root, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _name.rectTransform.PlaceTL(0, 107, PortraitSize + 8, 8);
            _title = UiFactory.CreateText("Title", root, "", 8, Palette.MutedText).WithOutline();
            _title.rectTransform.PlaceTL(-4, 117, PortraitSize + 16, 9);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image dialog = UiFactory.CreateDialog("Speech", root);
            dialog.rectTransform.PlaceTL(0, 128, PortraitSize + 8, 72);
            _dialog = dialog.gameObject;
            _dialogBox = dialog;
            _plainDialog = dialog.sprite;
            _line = UiFactory.CreateText("Line", dialog.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _line.rectTransform.PlaceTL(6, 5, PortraitSize - 4, 62);
            _line.lineSpacing = 1f;
            _dialog.SetActive(false);
        }

        /// <summary>Takes the new demon's portrait, name and title in turn (the table's stage plays any scene around it).</summary>
        public void SetDealer(DealerCard dealer, SeatChange change = SeatChange.Instant)
        {
            _sequencer.Do(() => ShowDealer(dealer));
        }

        /// <summary>The new demon, right now (a scene calls this at its darkest moment).</summary>
        public void ShowDealer(DealerCard dealer)
        {
            _dealerId = dealer.Id;
            _name.text = dealer.Name;
            // His title ("Waits below 250 years") belongs to the locked card on the choice screen, not to his table.
            _title.text = dealer.IsFinalTable ? "" : dealer.Title;
            _fullLine = "";
            _line.text = "";
            _dialog.SetActive(false);
            _reacting = false;
            SpeakLike(dealer.IsFinalTable);
            PlayBase();
        }

        /// <summary>True while the demon at the table is the Morning Star (his own dialogue colours).</summary>
        public bool IsFinalTable { get; private set; }

        /// <summary>The Morning Star speaks from a black, hellfire-edged box in ember letters; the others from the bone one.</summary>
        private void SpeakLike(bool finalTable)
        {
            IsFinalTable = finalTable;
            Sprite hot = finalTable ? UiArt.Sprite(UiArt.DialogLucifer) : null;
            if (_plainDialog != null)
                _dialogBox.sprite = hot != null ? hot : _plainDialog;
            _line.color = finalTable ? Palette.Ember : Palette.Bone;
            _name.color = finalTable ? Palette.Hell : Palette.GoldLight;
            // "THE MORNING STAR" is too long for the blocky capitals on one line: his name plate uses the small pixel font.
            Font font = finalTable ? UiArt.Body : UiArt.Display;
            if (font != null) _name.font = font;
        }

        /// <summary>True while a line is still typing out.</summary>
        public bool IsSpeaking => IsTyping;

        public void Say(string line, DealerMood mood)
        {
            _sequencer.Do(() =>
            {
                _fullLine = line ?? "";
                _typed = 0f;
                _line.text = "";
                _dialog.SetActive(_fullLine.Length > 0);

                DealerAnimation? reaction = ReactionTo(mood);
                if (reaction.HasValue)
                {
                    _reacting = true;
                    _portrait.Play(_library.Get(_dealerId, reaction.Value), EndReaction);
                }
                else
                {
                    _reacting = false;
                    PlayTalkOrBase();
                }
            });
        }

        /// <summary>The burning look for the end of the sentence (driven by the table's final-stretch switch).</summary>
        public void SetFinalStretch(bool active)
        {
            if (_finalStretch == active) return;
            _finalStretch = active;
            if (!_reacting)
                PlayTalkOrBase();
        }

        /// <summary>The cold, ghostly look while the player's soul is on the table.</summary>
        public void SetSoul(bool active)
        {
            if (_soul == active) return;
            _soul = active;
            if (!_reacting)
                PlayTalkOrBase();
        }

        /// <summary>The line appears whole at once (skip).</summary>
        public void FinishLine()
        {
            if (!IsTyping) return;
            _typed = _fullLine.Length;
            _line.text = _fullLine;
            if (!_reacting)
                PlayBase();
        }

        /// <summary>Which one-shot animation a mood plays before the dealer goes back to talking; null for none.</summary>
        public static DealerAnimation? ReactionTo(DealerMood mood)
        {
            switch (mood)
            {
                case DealerMood.Gloating: return DealerAnimation.Gloat;
                case DealerMood.Annoyed: return DealerAnimation.Angry;
                case DealerMood.Scheming: return DealerAnimation.ReRaise;
                default: return null;
            }
        }

        private void EndReaction()
        {
            _reacting = false;
            PlayTalkOrBase();
        }

        private bool IsTyping => _typed < _fullLine.Length;

        private void PlayTalkOrBase()
        {
            if (IsTyping)
                Play(DealerAnimation.Talk);
            else
                PlayBase();
        }

        private void PlayBase()
        {
            Play(_soul ? DealerAnimation.Soul : _finalStretch ? DealerAnimation.Final : DealerAnimation.Idle);
        }

        private void Play(DealerAnimation animation)
        {
            _portrait.Play(_library.Get(_dealerId, animation));
        }

        private void Update()
        {
            if (!IsTyping) return;

            _typed = Mathf.Min(_fullLine.Length, _typed + Time.unscaledDeltaTime * CharactersPerSecond * AnimationClock.Speed);
            _line.text = _fullLine.Substring(0, Mathf.FloorToInt(_typed));
            if (!IsTyping && !_reacting)
                PlayBase();
        }
    }
}
