using HellPoker.Core.Relics;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What a card holds: a relic, a class upgrade, or coins.</summary>
    public enum RelicCardKind
    {
        Relic,
        Upgrade,
        Coins
    }

    /// <summary>A card's state: on offer; on show but not to be had (dim, the reason at its foot); already taken ("SOLD").</summary>
    public enum RelicCardState
    {
        Normal,
        Unavailable,
        Taken
    }

    /// <summary>
    /// Demo 3.5: one card as the presenter describes it — the big picture's id, the words (the name, the rarity, the gift and the curse, or
    /// an upgrade's words, or the coins), what stands at its foot (a price, a state: "30 COINS", "+40 years", "Bag full") and its state. A
    /// small card (a carried relic in a "leave what in its place?" choice) shows only its icon and name, its words in its hover box.
    /// </summary>
    public sealed class RelicCardData
    {
        public RelicCardKind Kind { get; }

        /// <summary>The relic (its picture and icon); null for an upgrade or coins.</summary>
        public string RelicId { get; }

        /// <summary>An upgrade's id and the class it belongs to (its portrait on the card); null otherwise.</summary>
        public string UpgradeId { get; }
        public string ClassId { get; }

        public RelicRarity Rarity { get; }
        public string Name { get; }
        public string RarityLabel { get; }
        public string Gift { get; }
        public string Curse { get; }
        public string Footer { get; }
        public RelicCardState State { get; }
        public bool Small { get; }

        /// <summary>The hover box's words (a small card's gift and curse); empty for none.</summary>
        public string Hover { get; }

        public RelicCardData(RelicCardKind kind, string relicId, string upgradeId, string classId, RelicRarity rarity, string name, string rarityLabel,
            string gift, string curse, string footer, RelicCardState state = RelicCardState.Normal, bool small = false, string hover = null)
        {
            Kind = kind;
            RelicId = relicId;
            UpgradeId = upgradeId;
            ClassId = classId;
            Rarity = rarity;
            Name = name ?? "";
            RarityLabel = rarityLabel ?? "";
            Gift = gift ?? "";
            Curse = curse ?? "";
            Footer = footer ?? "";
            State = state;
            Small = small;
            Hover = hover ?? "";
        }

        /// <summary>The same card in another state, with another foot.</summary>
        public RelicCardData With(RelicCardState state, string footer) =>
            new RelicCardData(Kind, RelicId, UpgradeId, ClassId, Rarity, Name, RarityLabel, Gift, Curse, footer, state, Small, Hover);
    }
}
