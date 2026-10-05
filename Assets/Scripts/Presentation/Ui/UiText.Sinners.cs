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

        /// <summary>The badge's hover box: the power's charge. {0} now, {1} full.</summary>
        public static string SinnerChargeFormat => L("Power: {0} / {1}  (a hand won or lost +1)", "Güç: {0} / {1}  (kazanılan ya da kaybedilen el +1)");

        /// <summary>Over the badge when the power can be used now.</summary>
        public static string PowerReadyHint => L("READY: K", "HAZIR: K");
        public static string WardUpHint => L("WARD UP", "KORUMA HAZIR");
        public static string WardRaisedMessage => L("The ward is up: the cheat announced will be refused.", "Koruma hazır: duyurulan hile engellenecek.");

        public static string WardFlash => L("WARD", "KORUMA");
        public static string FreeFoldMessage => L("An honest heart: you walk away for nothing.", "Dürüst kalp: bu eli bedelsiz bıraktın.");

        /// <summary>{0} the charge, {1} full.</summary>
        public static string PowerChargingFormat => L("Your power charges: {0} / {1}. Every hand won or lost +1; a fold nothing.",
            "Gücün doluyor: {0} / {1}. Kazanılan ya da kaybedilen her el +1; çekilme 0.");
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
                case HellPoker.Core.Game.PowerRefusal.NotCharged: return string.Format(PowerChargingFormat, charge, full);
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
            string page = L("Before the first hand you choose who you were. The class stays for the whole run, at every table.\n" +
                            "Every class has a power and a gauge of five pips under the portrait: every hand won or lost +1, a fold nothing. " +
                            "The gauge belongs to the run (new tables and Lucifer keep it). Full, the power waits until you use it — " +
                            "K or the badge — and the gauge empties.\n\n",
                "İlk elden önce kim olduğunu seçersin. Sınıf bütün koşu boyunca, her masada seninledir.\n" +
                "Her sınıfın bir gücü ve portrenin altında beş pipli bir göstergesi var: kazanılan ya da kaybedilen her el +1, çekilme 0. " +
                "Gösterge koşuya aittir (yeni masa ve Lucifer onu korur). Dolunca güç, sen kullanana kadar bekler — " +
                "K ya da rozet — ve gösterge boşalır.\n\n");
            foreach (SinnerClass c in SinnerRoster.All)
                page += Block(c) + "\n";
            return page.TrimEnd();
        }
    }
}
