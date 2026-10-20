using HellPoker.Core.Relics;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>Demo 3.5: the cards the chapters show (a relic, an upgrade, the coins, a small carried relic), in the language on show.</summary>
    public static class RelicCards
    {
        /// <summary>A relic's card: its name, rarity, gift and curse; <paramref name="footer"/> its price or state.</summary>
        public static RelicCardData Relic(string relicId, string footer = null, RelicCardState state = RelicCardState.Normal)
        {
            RelicRarity rarity = ChapterRelics.RarityOf(relicId);
            string curse = UiText.RelicCurse(relicId);
            return new RelicCardData(RelicCardKind.Relic, relicId, null, null, rarity, UiText.RelicName(relicId), UiText.RarityName(rarity),
                string.Format(UiText.CardGiftFormat, UiText.RelicGift(relicId)), curse.Length > 0 ? string.Format(UiText.CardCurseFormat, curse) : "",
                footer, state);
        }

        /// <summary>A carried relic in a choice of what to leave behind: its icon and name, its words in the hover box.</summary>
        public static RelicCardData Small(string relicId)
        {
            RelicRarity rarity = ChapterRelics.RarityOf(relicId);
            return new RelicCardData(RelicCardKind.Relic, relicId, null, null, rarity, UiText.RelicName(relicId), UiText.RarityName(rarity),
                UiText.RelicGift(relicId), UiText.RelicCurse(relicId), null, small: true,
                hover: UiText.RarityName(rarity) + "\n" + string.Format(UiText.RelicDescriptionFormat, UiText.RelicGift(relicId), UiText.RelicCurse(relicId)));
        }

        /// <summary>A class upgrade's card: the class's portrait, the upgrade's name and words.</summary>
        public static RelicCardData Upgrade(string upgradeId, string classId, string footer = null, RelicCardState state = RelicCardState.Normal) =>
            new RelicCardData(RelicCardKind.Upgrade, null, upgradeId, classId, RelicRarity.Rare, UiText.UpgradeName(upgradeId), UiText.UpgradeCardLabel,
                UiText.UpgradeText(upgradeId), "", footer, state);

        /// <summary>The gambler's closed box: a card lying face down (its back is all that shows).</summary>
        public static RelicCardData Box(string name) =>
            new RelicCardData(RelicCardKind.Coins, null, null, null, RelicRarity.Common, name, "", "", "", "");

        /// <summary>The coins' card: a pile of coins, "+30 COINS".</summary>
        public static RelicCardData Coins(int coins, string footer = null, RelicCardState state = RelicCardState.Normal) =>
            new RelicCardData(RelicCardKind.Coins, null, null, null, RelicRarity.Common, string.Format(UiText.CoinsCardFormat, coins), "",
                UiText.CoinsCardText, "", footer, state);
    }
}
