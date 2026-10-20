using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Where a chosen card's prize flies to on the map's side panel: a relic or an upgrade to the relics, coins to the purse.</summary>
    public enum LootFlight
    {
        Relic,
        Upgrade,
        Coins
    }

    /// <summary>
    /// Demo 3.5: the spoils of a beaten demon as a scene (ARAM's choice): his hall darkened, what lies where he sat, "SPOILS" and "Mammon
    /// has fallen. Choose one.", the cards (the loot's relics, the upgrade, the coins) and REROLL under them. The keys point at a card or at
    /// REROLL (<see cref="Selected"/> = <see cref="Cards"/>.Count). With <see cref="Swap"/> the cards give way to the carried relics' small
    /// cards (the bag is full: which one stays behind) and BACK.
    /// </summary>
    public sealed class BossLootState
    {
        public string BossId { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public IReadOnlyList<RelicCardData> Cards { get; }
        public int Selected { get; }
        public string RerollLabel { get; }
        public bool CanReroll { get; }

        /// <summary>The carried relics to leave one behind (small cards); null: the cards themselves.</summary>
        public IReadOnlyList<RelicCardData> Swap { get; }
        public string SwapTitle { get; }

        public BossLootState(string bossId, string title, string subtitle, IReadOnlyList<RelicCardData> cards, int selected, string rerollLabel,
            bool canReroll, IReadOnlyList<RelicCardData> swap = null, string swapTitle = null)
        {
            BossId = bossId;
            Title = title ?? "";
            Subtitle = subtitle ?? "";
            Cards = cards ?? Array.Empty<RelicCardData>();
            Selected = selected;
            RerollLabel = rerollLabel ?? "";
            CanReroll = canReroll;
            Swap = swap;
            SwapTitle = swapTitle ?? "";
        }

        public BossLootState WithSelected(int selected) =>
            new BossLootState(BossId, Title, Subtitle, Cards, selected, RerollLabel, CanReroll, Swap, SwapTitle);
    }

    /// <summary>The spoils scene (Demo 3.5).</summary>
    public interface IBossLootView
    {
        /// <summary>A card clicked (its index; in the swap step a carried relic's).</summary>
        event Action<int> CardPressed;
        event Action RerollPressed;

        /// <summary>BACK in the swap step.</summary>
        event Action BackPressed;

        bool IsOpen { get; }

        /// <summary>True while the cards come in, turn over, or the chosen one plays out (a press hurries it).</summary>
        bool IsAnimating { get; }

        /// <summary>The scene with these cards; <paramref name="deal"/>: they come in one by one (from below, turning over), else at once.</summary>
        void Show(BossLootState state, bool deal);

        /// <summary>The keys point elsewhere (a card, REROLL, BACK): only the frames change.</summary>
        void Select(BossLootState state);

        /// <summary>REROLL: the cards at <paramref name="changed"/> turn over to their new faces.</summary>
        void Reroll(BossLootState state, IReadOnlyCollection<int> changed);

        /// <summary>The choice plays out: the chosen card to the middle, the others dark and falling, its prize flying to the side panel;
        /// then <paramref name="done"/> (at once when hurried).</summary>
        void PlayChosen(int index, LootFlight flight, Action done);

        /// <summary>Every animation to its end at once.</summary>
        void Skip();

        void Hide();
    }
}
