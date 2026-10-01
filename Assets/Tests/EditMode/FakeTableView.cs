using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;

namespace HellPoker.Core.Tests
{
    /// <summary>Records what the presenter shows, and lets tests raise view events. Never busy (no animations).</summary>
    internal sealed class FakeTableView : ITableView
    {
        public FakeHandView HouseView { get; } = new FakeHandView();
        public FakeHandView PlayerView { get; } = new FakeHandView();
        public FakeSentenceView SentenceView { get; } = new FakeSentenceView();
        public FakeStakeSelectorView StakesView { get; } = new FakeStakeSelectorView();
        public FakePayoutView PayoutsView { get; } = new FakePayoutView();

        public IHandView House => HouseView;
        public IHandView Player => PlayerView;
        public ISentenceView Sentence => SentenceView;
        public IStakeSelectorView Stakes => StakesView;
        public IPayoutView Payouts => PayoutsView;

        public bool IsBusy { get; set; }
        public string Message { get; private set; }
        public Tone MessageTone { get; private set; }
        public string ActionLabel { get; private set; }
        public BetControls BetControls { get; private set; }
        public int Pot { get; private set; }
        public bool FinalStretch { get; private set; }

        public bool Visible { get; private set; } = true;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action MenuPressed;

        public void SetMessage(string text, Tone tone)
        {
            Message = text;
            MessageTone = tone;
        }

        public void SetAction(string label) => ActionLabel = label;
        public void SetBetControls(BetControls controls) => BetControls = controls;
        public void SetPot(int years) => Pot = years;
        public void SetFinalStretch(bool active, string banner) => FinalStretch = active;
        public void Pause(float seconds) { }
        public void SetVisible(bool visible) => Visible = visible;
        public void PressMenu() => MenuPressed?.Invoke();

        public void PressAction() => ActionPressed?.Invoke();
        public void PressBet(BetAction action) => BetPressed?.Invoke(action);
    }

    internal sealed class FakeHandView : IHandView
    {
        public CardSlot[] Slots { get; private set; } = Enumerable.Repeat(CardSlot.Empty, 5).ToArray();
        public string Caption { get; private set; }
        public Tone CaptionTone { get; private set; }
        public HashSet<int> Selection { get; } = new HashSet<int>();
        public bool Interactable { get; private set; }

        public int FaceUpCount => Slots.Count(slot => slot.Kind == CardSlot.SlotKind.Face);
        public bool IsEmpty => Slots.All(slot => slot.Kind == CardSlot.SlotKind.Empty);

        public event Action<int> CardClicked;

        public void Click(int index) => CardClicked?.Invoke(index);

        public void SetCaption(string text, Tone tone)
        {
            Caption = text;
            CaptionTone = tone;
        }

        public void Show(IReadOnlyList<CardSlot> slots) => Slots = slots.ToArray();

        public void SetSelection(ICollection<int> selectedIndices)
        {
            Selection.Clear();
            if (selectedIndices != null) Selection.UnionWith(selectedIndices);
        }

        public void SetInteractable(bool interactable) => Interactable = interactable;
    }

    internal sealed class FakeSentenceView : ISentenceView
    {
        public int DamnationLimit { get; private set; }
        public int Years { get; private set; }

        public void SetDamnationLimit(int years) => DamnationLimit = years;
        public void SetYears(int years, bool animate) => Years = years;
    }

    internal sealed class FakeStakeSelectorView : IStakeSelectorView
    {
        public int Selected { get; private set; }
        public bool Visible { get; private set; }

        public event Action<int> StakeChosen;

        public void Choose(int stake) => StakeChosen?.Invoke(stake);
        public HashSet<int> Available { get; } = new HashSet<int>();

        public void SetSelected(int stake) => Selected = stake;

        public void SetAvailable(ICollection<int> stakes)
        {
            Available.Clear();
            Available.UnionWith(stakes);
        }
        public void SetVisible(bool visible) => Visible = visible;
    }

    internal sealed class FakePayoutView : IPayoutView
    {
        public HandCategory? Highlighted { get; private set; }

        public void Highlight(HandCategory? category) => Highlighted = category;
    }
}
