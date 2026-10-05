using HellPoker.Core.Sinners;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The sinner classes in words: who they were, what they can do, the choice screen, the badge, the How to Play page.</summary>
    internal static partial class UiText
    {
        public static string SinnerName(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("PEASANT", "KÖYLÜ");
                case Warlock.ClassId: return L("WARLOCK", "BÜYÜCÜ");
                case King.ClassId: return L("KING", "KRAL");
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        /// <summary>Who they were, up there (the card's title).</summary>
        public static string SinnerTitle(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Who never had much to lose", "Kaybedecek pek bir şeyi olmamış");
                case Warlock.ClassId: return L("Who dabbled in the dark arts", "Karanlık sanatlarla oynamış");
                case King.ClassId: return L("Who ruled, and sinned in proportion", "Hüküm sürmüş, günahı da ona göre");
                default: return "";
            }
        }

        /// <summary>What the class does, short enough for its card (the card, the badge's hover box): the passive trait, then the power.</summary>
        public static string SinnerAbility(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Power: walk away from a hand for free.", "Güç: bir eli bedelsiz bırak.");
                case Warlock.ClassId: return L("Sees lies, and 2 of Belial's cards. Power: ward off a minor cheat.",
                    "Yalanları ve Belial'in 2 kartını görür. Güç: küçük bir hileyi savuştur.");
                case King.ClassId: return L("Wins pay a quarter ante more. Power: shield a card from cheats.",
                    "Kazanç çeyrek ante fazla. Güç: bir kartı hilelerden koru.");
                default: return "";
            }
        }

        /// <summary>The price (or the comfort) of the class: the starting sentence and what it cannot do.</summary>
        public static string SinnerDetail(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("No tricks — the easy way down.", "Hilesi yok — en kolay yol.");
                case Warlock.ClassId: return L("Major cheats get through.", "Büyük hileler geçer.");
                case King.ClassId: return L("A heavier sin: starts deeper.", "Ağır günah: daha derinden başlar.");
                default: return "";
            }
        }

        public static string SinnerStartFormat => L("STARTS AT {0} YEARS", "{0} YILLA BAŞLAR");
        public static string ChooseSinnerTitle => L("CHOOSE YOUR SIN", "GÜNAHINI SEÇ");
        public static string ChooseSinnerSubtitle => L("Who were you, up there?", "Yukarıda kimdin?");
        public static string ChooseSinner => L("CHOOSE", "SEÇ");

        /// <summary>The badge's hover box: the power's charge. {0} now, {1} full, {2} a win, {3} a loss, {4} a fold.</summary>
        public static string SinnerChargeFormat => L("Power: {0} / {1}  (win +{2}, lose +{3}, fold +{4})", "Güç: {0} / {1}  (kazanç +{2}, kayıp +{3}, çekilme +{4})");

        /// <summary>The badge's charge line, with the rules the gauge really charges by.</summary>
        public static string SinnerCharge(int charge, int full) =>
            string.Format(SinnerChargeFormat, charge, full, ChargeRules.Default.PerWin, ChargeRules.Default.PerLoss, ChargeRules.Default.PerFold);

        /// <summary>Over the badge while a power is switched on (or a ward is up).</summary>
        public static string PowerOnLabel => L("POWER ON", "GÜÇ AÇIK");
        /// <summary>The line that stays on screen while the King picks.</summary>
        public static string ProtectHint => L("Pick a card to protect (K: cancel)", "Korumak için bir kart seç (K: vazgeç)");
        public static string ProtectUnused => L("You did not use the protection; your power is still full.", "Korumayı kullanmadın, gücün dolu duruyor.");
        /// <summary>The line that stays on screen while the Peasant's power is on.</summary>
        public static string FreeFoldHint => L("Power on: your next FOLD is free (K: switch off)", "Güç açık: sıradaki ÇEKİL bedava (K: kapat)");
        /// <summary>The Fold button while the Peasant's power is on: two short lines.</summary>
        public static string FreeFoldButton => L("FREE\nFOLD", "BEDAVA\nÇEKİL");
        public static string PowerOffMessage => L("Power switched off; the gauge stays full.", "Güç kapandı; gösterge dolu duruyor.");

        /// <summary>Over the badge when the power can be used now.</summary>
        public static string PowerReadyHint => L("READY: K", "HAZIR: K");
        public static string WardUpHint => L("WARD UP", "KORUMA HAZIR");
        public static string WardRaisedMessage => L("The ward is up: the cheat announced will be refused.", "Koruma hazır: duyurulan hile engellenecek.");

        public static string WardFlash => L("WARD", "KORUMA");
        public static string FreeFoldMessage => L("An honest heart: you walk away for nothing.", "Dürüst kalp: bu eli bedelsiz bıraktın.");

        /// <summary>{0} the charge, {1} full, {2} a win, {3} a loss, {4} a fold.</summary>
        public static string PowerChargingFormat => L("Your power charges: {0} / {1}. Win +{2}, lose +{3}, fold +{4}.",
            "Gücün doluyor: {0} / {1}. Kazanç +{2}, kayıp +{3}, çekilme +{4}.");
        public static string PowerNoHand => L("The power waits for a hand to be played.", "Güç, oynanan bir eli bekler.");
        public static string PowerCannotFold => L("The pact is sealed: this hand cannot be left.", "Mühür vuruldu: bu el bırakılamaz.");
        public static string PowerNoCheat => L("No cheat is announced: the ward waits for one.", "Duyurulan bir hile yok: koruma bir hile bekler.");
        public static string PowerMajorCheat => L("A major cheat is beyond a ward.", "Büyük bir hileye koruma işlemez.");
        public static string PowerWardAlreadyUp => L("The ward is already up.", "Koruma zaten hazır.");
        public static string PowerNotBeforeDraw => L("The crown protects a card only before the draw.", "Taç bir kartı ancak değişten önce korur.");

        /// <summary>Why the power cannot be used now, in words.</summary>
        public static string PowerRefused(SinnerAbility ability, HellPoker.Core.Game.PowerRefusal refusal, int charge, int full)
        {
            switch (refusal)
            {
                case HellPoker.Core.Game.PowerRefusal.NotCharged:
                    return string.Format(PowerChargingFormat, charge, full, ChargeRules.Default.PerWin, ChargeRules.Default.PerLoss, ChargeRules.Default.PerFold);
                case HellPoker.Core.Game.PowerRefusal.CannotFold: return PowerCannotFold;
                case HellPoker.Core.Game.PowerRefusal.NoCheatAnnounced: return PowerNoCheat;
                case HellPoker.Core.Game.PowerRefusal.MajorCheat: return PowerMajorCheat;
                case HellPoker.Core.Game.PowerRefusal.WardAlreadyRaised: return PowerWardAlreadyUp;
                case HellPoker.Core.Game.PowerRefusal.NotBeforeDraw: return PowerNotBeforeDraw;
                case HellPoker.Core.Game.PowerRefusal.NoCardToProtect: return ProtectNotNow;
                default: return PowerNoHand;
            }
        }
        public static string ProtectPrompt => L("Pick the card the crown protects (K again: cancel).", "Tacın koruyacağı kartı seç (yine K: vazgeç).");
        /// <summary>{0}: the card.</summary>
        public static string ProtectedFormat => L("The crown protects your {0}: no cheat may touch it this hand.",
            "Taç {0} kartını koruyor: bu el hiçbir hile ona dokunamaz.");
        public static string ProtectNotNow => L("The crown protects a card you can see, before the draw.", "Taç ancak gördüğün bir kartı, değişten önce korur.");

        /// <summary>One line for every class: {0} = "PEASANT 2 · WARLOCK 0 · KING 1".</summary>
        public static string RecordsClassesFormat => L("Freed as: {0}", "Sınıfa göre aklanma: {0}");

        public static string SinnersButton => L("SINNERS", "GÜNAHKÂRLAR");

        /// <summary>How to Play: the Sinners page.</summary>
        public static string SinnersPage()
        {
            string Block(SinnerClass c) =>
                $"{SinnerName(c.Id)} — {SinnerTitle(c.Id)}  ({string.Format(SinnerStartFormat, c.StartingYears)})\n" +
                $"   {SinnerAbility(c.Id)}\n   {SinnerDetail(c.Id)}\n";
            ChargeRules charge = ChargeRules.Default;
            string page = string.Format(L("Before the first hand you choose who you were. The class stays for the whole run, at every table.\n" +
                            "Every class has a power and a gauge of {0} pips under the portrait: a won hand +{1}, a lost one +{2}, a fold +{3}. " +
                            "The gauge belongs to the run (new tables and Lucifer keep it). Full, K or the badge switches the power on; it " +
                            "stays on until it is used (the gauge empties) or switched off with K again.\n\n",
                "İlk elden önce kim olduğunu seçersin. Sınıf bütün koşu boyunca, her masada seninledir.\n" +
                "Her sınıfın bir gücü ve portrenin altında {0} pipli bir göstergesi var: kazanılan el +{1}, kaybedilen +{2}, çekilme +{3}. " +
                "Gösterge koşuya aittir (yeni masa ve Lucifer onu korur). Dolunca K ya da rozet gücü açar; güç kullanılana " +
                "(gösterge boşalır) ya da yine K ile kapatılana kadar açık kalır.\n\n"), charge.Full, charge.PerWin, charge.PerLoss, charge.PerFold);
            foreach (SinnerClass c in SinnerRoster.All)
                page += Block(c) + "\n";
            return page.TrimEnd();
        }
    }
}
