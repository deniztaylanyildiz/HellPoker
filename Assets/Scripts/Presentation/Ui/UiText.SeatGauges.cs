namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 3.5.1: the gauges under the demon's name (the imp's purse, the ante and its dots, the demon's rule icons).</summary>
    internal static partial class UiText
    {
        public static string GaugePurse => L("PURSE", "KESE");

        /// <summary>The ante gauge: {0} the ante (coins, or the bar's percent at a demon's table).</summary>
        public static string GaugeAnteFormat => L("ANTE {0}", "ANTE {0}");
        public static string GaugeAnteBarFormat => L("ANTE {0}%", "ANTE %{0}");

        /// <summary>Free hands left: {0}.</summary>
        public static string GaugeNoAnteFormat => L("NO ANTE: {0}", "ANTE'SİZ: {0}");

        public static string GaugePurseTip => L("The imp's purse. Empty it to win the table.", "İblisin kesesi. 0 olunca masayı kazanırsın.");
        public static string GaugeWardenPurseTip => L("The warden's purse. Empty it to win the table — and his relic.",
            "Bekçinin kesesi. 0 olunca masayı ve emanetini kazanırsın.");

        /// <summary>{0} the hands before it grows, {1} what it becomes.</summary>
        public static string GaugeAnteTipFormat => L("Each hand's forced bet. Becomes {1} in {0} hands.", "Her elin zorunlu bahsi. {0} el sonra {1} olur.");
        public static string GaugeAnteNextTipFormat => L("Each hand's forced bet. Becomes {1} next hand.", "Her elin zorunlu bahsi. Sonraki elde {1} olur.");
        public static string GaugeAnteFixedTip => L("Each hand's forced bet: a share of the bar, fixed at this table.",
            "Her elin zorunlu bahsi: barın bir payı, bu masada sabit.");

        /// <summary>{0} the free hands left.</summary>
        public static string GaugeNoAnteTipFormat => L("No ante and no raise for {0} more hands: this table's mark.",
            "{0} el daha ante yok, artırma yok: bu masanın işareti.");

        /// <summary>Lilith's gate (Demo 3.5.1, the Last Bribe): {0} the coins left after the tribute, {1} the percent her bar shrinks.</summary>
        public static string GateBribeFormat => L("{0} coins left bribe Lilith: her bar shrinks {1}%.", "Kalan {0} coin Lilith'e rüşvet: barı %{1} küçüldü.");

        public static string BossBarExplain => L("The boss's life: his share of your sentence. Every hand you win shrinks it, every hand you lose grows it; at 0 he falls.",
            "Boss'un canı: senin cezandan payı. Kazandığın her el azaltır, kaybettiğin her el büyütür; 0 olunca boss düşer.");
    }
}
