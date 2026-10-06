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
        public event Action CheckToDrawPressed;
        public event Action MenuPressed;
        public event Action LeavePressed;
        public event Action HandRanksPressed;
        public event Action SinnerPressed;
        public event Action<int> EventOptionPressed;
        public event Action<string> RelicPressed;
        public void PressRelic(string id) => RelicPressed?.Invoke(id);

        /// <summary>The relics as last shown.</summary>
        public IReadOnlyList<RelicBadge> Relics { get; private set; } = new RelicBadge[0];
        public void SetRelics(IReadOnlyList<RelicBadge> relics) => Relics = relics;
        /// <summary>Every effect asked for, in order.</summary>
        public List<string> Sfx { get; } = new List<string>();
        public void PlaySfx(string sfxId) => Sfx.Add(sfxId);
        public void PressEventOption(int index) => EventOptionPressed?.Invoke(index);

        /// <summary>The event panel as shown; null when closed.</summary>
        public EventCard Event { get; private set; }
        public void ShowEvent(EventCard card) => Event = card;
        public void HideEvent() => Event = null;
        public void PressSinner() => SinnerPressed?.Invoke();

        /// <summary>The class badge as last shown.</summary>
        public SinnerBadge Sinner { get; private set; } = SinnerBadge.Hidden;
        public void SetSinner(SinnerBadge badge) => Sinner = badge;

        public PowerDisplay Power { get; private set; } = PowerDisplay.None;
        public void SetPower(PowerDisplay power) => Power = power ?? PowerDisplay.None;

        /// <summary>The deck counter (-1: hidden) and the SHUFFLE button (null: hidden).</summary>
        public int DeckCount { get; private set; } = -1;
        public void SetDeckCount(int cards) => DeckCount = cards;
        public string ShuffleLabel { get; private set; }
        public bool ShuffleLocked { get; private set; }
        public void SetShuffle(string label, bool locked)
        {
            ShuffleLabel = label;
            ShuffleLocked = locked;
        }
        public event Action ShufflePressed;
        public void PressShuffle() => ShufflePressed?.Invoke();

        /// <summary>The joker picker as shown; null when closed.</summary>
        public JokerPick JokerPicker { get; private set; }
        public void ShowJokerPicker(JokerPick pick) => JokerPicker = pick;
        public event Action<int, int> JokerStepPressed;
        public event Action JokerConfirmPressed;
        public void PressJokerStep(int rank, int suit) => JokerStepPressed?.Invoke(rank, suit);
        public void PressJokerConfirm() => JokerConfirmPressed?.Invoke();

        public bool HandRanksOpen { get; private set; }
        public IPayoutInfo HandRanksPayouts { get; private set; }

        public string HandRanksFootnote { get; private set; }

        public void ShowHandRanks(IPayoutInfo payouts, string footnote = null)
        {
            HandRanksOpen = true;
            HandRanksPayouts = payouts;
            HandRanksFootnote = footnote;
        }

        public MaliceGauge Malice { get; private set; } = MaliceGauge.Hidden;
        public CheatCard Intent { get; private set; }
        public List<CheatCard> Lies { get; } = new List<CheatCard>();
        public List<CheatImpact> Impacts { get; } = new List<CheatImpact>();

        public void SetMalice(MaliceGauge gauge) => Malice = gauge;

        public void SetIntent(CheatCard intent)
        {
            if (intent != null) TextLog.Add(intent.Name);
            Intent = intent;
        }

        public void RevealLie(CheatCard truth)
        {
            Lies.Add(truth);
            Intent = truth;
        }

        public void PlayCheat(CheatImpact impact) => Impacts.Add(impact);

        public void HideHandRanks() => HandRanksOpen = false;
        public void PressHandRanks() => HandRanksPressed?.Invoke();

        /// <summary>Every row of cards either hand was told to show, in the order the table was told (house or player).</summary>
        public List<(bool house, CardSlot[] slots)> ShowLog { get; } = new List<(bool, CardSlot[])>();

        public FakeTableView()
        {
            SentenceView.Log = NumberLog;
            DealerView.Log = TextLog;
            HouseView.Log = TextLog;
            PlayerView.Log = TextLog;
            HouseView.Shown = slots => ShowLog.Add((true, slots));
            PlayerView.Shown = slots => ShowLog.Add((false, slots));
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
        /// <summary>Every pause the table was asked for, in order.</summary>
        public List<float> Pauses { get; } = new List<float>();

        public void Pause(float seconds) => Pauses.Add(seconds);
        public void SetVisible(bool visible) => Visible = visible;
        public void PressMenu() => MenuPressed?.Invoke();

        public void PressAction() => ActionPressed?.Invoke();
        public void PressBet(BetAction action) => BetPressed?.Invoke(action);
        public void PressCheckToDraw() => CheckToDrawPressed?.Invoke();
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

        /// <summary>Face-up count of every hand the view was told to show, in order.</summary>
        public List<int> ShownFaceUp { get; } = new List<int>();

        /// <summary>Every row of slots the view was told to show, in order.</summary>
        public List<CardSlot[]> History { get; } = new List<CardSlot[]>();

        /// <summary>Told about every row shown (the table's shared log).</summary>
        public Action<CardSlot[]> Shown { get; set; }

        public void Show(IReadOnlyList<CardSlot> slots)
        {
            Slots = slots.ToArray();
            History.Add(Slots);
            ShownFaceUp.Add(FaceUpCount);
            Shown?.Invoke(Slots);
        }

        public void SetSelection(ICollection<int> selectedIndices)
        {
            Selection.Clear();
            if (selectedIndices != null) Selection.UnionWith(selectedIndices);
        }

        public void SetInteractable(bool interactable) => Interactable = interactable;

        /// <summary>The cards a power may pick now (null: no picking).</summary>
        public IReadOnlyCollection<int> Picking { get; private set; }
        public void SetPicking(ICollection<int> pickable) => Picking = pickable == null || pickable.Count == 0 ? null : pickable.ToList();

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
            LimitText = null;
        }

        public string LimitText { get; private set; }
        public string Label { get; private set; } = "YEARS LEFT IN HELL";

        public void SetLimit(int years, string text)
        {
            Log?.Add(years);
            SoulLine = years;
            LimitText = text;
        }

        public void SetLabel(string text) => Label = text;

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

        /// <summary>Every seat change, in order.</summary>
        public List<(string id, SeatChange change)> Seats { get; } = new List<(string, SeatChange)>();

        public void SetDealer(DealerCard dealer, SeatChange change = SeatChange.Instant)
        {
            Dealer = dealer;
            Seats.Add((dealer.Id, change));
        }

        /// <summary>Every line said, with the mood it was said in.</summary>
        public List<(string line, DealerMood mood)> Said { get; } = new List<(string, DealerMood)>();

        /// <summary>The name plate as the language last relabelled it.</summary>
        public DealerCard Relabelled { get; private set; }

        public void Relabel(DealerCard dealer) => Relabelled = dealer;

        /// <summary>Lines swapped in place, without being said again (a language change).</summary>
        public List<string> LinesSet { get; } = new List<string>();

        public void SetLine(string line) => LinesSet.Add(line);

        public void Say(string line, DealerMood mood)
        {
            Log?.Add(line);
            Said.Add((line, mood));
            LastLine = line;
            LastMood = mood;
            LinesSaid++;
        }
    }
}
