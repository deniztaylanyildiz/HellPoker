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
        public FakePayoutView PayoutsView { get; } = new FakePayoutView();
        public FakeDealerView DealerView { get; } = new FakeDealerView();

        public IHandView House => HouseView;
        public IHandView Player => PlayerView;
        public ISentenceView Sentence => SentenceView;
        public IPayoutView Payouts => PayoutsView;
        public IDealerView Dealer => DealerView;

        public bool IsBusy { get; set; }
        public int Skips { get; private set; }

        public void SkipAnimations() => Skips++;

        public List<(TableMoment moment, string text, IReadOnlyList<int> cards)> Moments { get; } =
            new List<(TableMoment, string, IReadOnlyList<int>)>();

        public void PlayMoment(TableMoment moment, string text = null, IReadOnlyList<int> playerCards = null)
        {
            if (text != null) TextLog.Add(text);
            Moments.Add((moment, text, playerCards));
        }
        public string Message { get; private set; }
        public Tone MessageTone { get; private set; }
        public string ActionLabel { get; private set; }
        public BetControls BetControls { get; private set; }
        public int Pot { get; private set; }
        public string StakeInfo { get; private set; }
        public int Ante { get; private set; }
        public bool FinalStretch { get; private set; }
        public SoulGauge Soul { get; private set; } = SoulGauge.Hidden;
        public LeaveState Leave { get; private set; }

        /// <summary>Every text the table was told to show, in order (messages, labels, info lines, dealer lines).</summary>
        public List<string> TextLog { get; } = new List<string>();

        /// <summary>Every year amount the table was told to show (pot, ante, sentence), in order.</summary>
        public List<int> NumberLog { get; } = new List<int>();

        public bool Visible { get; private set; } = true;

        public event Action ActionPressed;
        public event Action<BetAction> BetPressed;
        public event Action MenuPressed;
        public event Action LeavePressed;
        public event Action HandRanksPressed;

        public bool HandRanksOpen { get; private set; }
        public IPayoutInfo HandRanksPayouts { get; private set; }

        public void ShowHandRanks(IPayoutInfo payouts)
        {
            HandRanksOpen = true;
            HandRanksPayouts = payouts;
        }

        public void HideHandRanks() => HandRanksOpen = false;
        public void PressHandRanks() => HandRanksPressed?.Invoke();

        public FakeTableView()
        {
            SentenceView.Log = NumberLog;
            DealerView.Log = TextLog;
            HouseView.Log = TextLog;
            PlayerView.Log = TextLog;
        }

        public void SetMessage(string text, Tone tone)
        {
            TextLog.Add(text);
            Message = text;
            MessageTone = tone;
        }

        public void SetAction(string label)
        {
            TextLog.Add(label);
            ActionLabel = label;
        }

        public void SetBetControls(BetControls controls)
        {
            TextLog.Add(controls.RaiseLabel);
            TextLog.Add(controls.CallLabel);
            BetControls = controls;
        }

        public void SetPot(int years)
        {
            NumberLog.Add(years);
            Pot = years;
        }

        public void SetStakeInfo(string text)
        {
            TextLog.Add(text);
            StakeInfo = text;
        }

        public void SetAnte(int years)
        {
            NumberLog.Add(years);
            Ante = years;
        }

        public void SetSoul(SoulGauge gauge) => Soul = gauge;
        public void SetLeave(LeaveState state) => Leave = state;
        public void PressLeave() => LeavePressed?.Invoke();
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
        public HashSet<int> Hints { get; } = new HashSet<int>();
        public bool Interactable { get; private set; }

        public int FaceUpCount => Slots.Count(slot => slot.Kind == CardSlot.SlotKind.Face);
        public bool IsEmpty => Slots.All(slot => slot.Kind == CardSlot.SlotKind.Empty);

        public event Action<int> CardClicked;
        public List<string> Log { get; set; }

        public void Click(int index) => CardClicked?.Invoke(index);

        public void SetCaption(string text, Tone tone)
        {
            Log?.Add(text);
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

        public void SetHints(ICollection<int> keepIndices)
        {
            Hints.Clear();
            if (keepIndices != null) Hints.UnionWith(keepIndices);
        }
    }

    internal sealed class FakeSentenceView : ISentenceView
    {
        public int SoulLine { get; private set; }
        public int Years { get; private set; }
        public List<int> Log { get; set; }

        public void SetSoulLine(int years)
        {
            Log?.Add(years);
            SoulLine = years;
        }

        public void SetYears(int years, bool animate)
        {
            Log?.Add(years);
            Years = years;
        }
    }

    internal sealed class FakePayoutView : IPayoutView
    {
        public HandCategory? Highlighted { get; private set; }
        public IPayoutInfo Table { get; private set; }

        public void SetTable(IPayoutInfo payouts) => Table = payouts;
        public void Highlight(HandCategory? category) => Highlighted = category;
    }

    internal sealed class FakeDealerView : IDealerView
    {
        public DealerCard Dealer { get; private set; }
        public string LastLine { get; private set; }
        public DealerMood LastMood { get; private set; }
        public int LinesSaid { get; private set; }
        public List<string> Log { get; set; }

        public void SetDealer(DealerCard dealer) => Dealer = dealer;

        public void Say(string line, DealerMood mood)
        {
            Log?.Add(line);
            LastLine = line;
            LastMood = mood;
            LinesSaid++;
        }
    }
}
