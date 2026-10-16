using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The Thief's quick hand in words (Demo 2.2): his panel, his steal, being caught.</summary>
    internal static partial class UiText
    {
        public static string KitThiefTitle => L("QUICK HAND", "EL ÇABUKLUĞU");
        public static string StealButton => L("STEAL", "ÇAL");

        /// <summary>{0} how often he is caught now (percent), {1} at best, {2} a demon's fine (percent of the bar).</summary>
        public static string KitThiefHintFormat => L(
            "GLANCE: one of the demon's face-down cards plays face up from the deal. STEAL (K or the button, once a hand, from the deal until " +
            "the showdown — after the draw too): click a card of yours, it goes for one of the demon's face-down cards, blind. Caught ({0}%, " +
            "your relics bring it to {1}%): nothing changes hands and you pay a fine — an ante's coins on the floors, {2}% of the bar at a demon's table.",
            "GÖZ UCU: şeytanın kapalı kartlarından biri dağıtımdan açık gelir. ÇAL (K ya da düğme, elde bir kez, dağıtımdan showdown'a kadar — " +
            "draw'dan sonra da): kartlarından birine tıkla, şeytanın kapalı kartlarından biriyle körlemesine yer değişir. Yakalanırsan (%{0}, " +
            "emanetlerin %{1} kadar indirir): takas olmaz, ceza ödersin — katta bir ante coin, şeytan masasında barın %{2} kadarı.");

        public static string KitThiefHint(ClassKit kit) =>
            string.Format(KitThiefHintFormat, kit?.ThiefCaughtPercent ?? ClassKit.CaughtStart, kit?.PowerMax ?? ClassKit.CaughtLeast, TheftPowers.FineBarPercent);

        public static string StealPrompt => L("Steal: click the card you give.", "Çal: vereceğin karta tıkla.");

        /// <summary>{0} the card taken, {1} the card given.</summary>
        public static string StolenFormat => L("Stolen: {0}, for your {1}.", "Çalındı: {1} karşılığında {0} senin.");

        /// <summary>{0} the coins of the fine.</summary>
        public static string CaughtCoinsFormat => L("Caught! Nothing changes hands. The fine: {0} coins.", "Yakalandın! Takas olmadı. Ceza: {0} coin.");

        /// <summary>{0} the fine in percent of the bar.</summary>
        public static string CaughtBarFormat => L("Caught! Nothing changes hands. The fine: the bar grows {0}%.", "Yakalandın! Takas olmadı. Ceza: bar %{0} uzar.");
    }
}
