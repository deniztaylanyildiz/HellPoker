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

        /// <summary>The ability in one sentence (the card, the badge's hover box).</summary>
        public static string SinnerAbility(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("An honest heart: the run's first fold costs nothing.", "Dürüst kalp: koşudaki ilk çekilme bedava.");
                case Warlock.ClassId: return L("Sees through lies and hidden hands. Wards off one minor cheat at each demon's table.",
                    "Yalanları ve saklanan elleri görür. Her şeytanın masasında bir küçük hileyi savuşturur.");
                case King.ClassId: return L("The crown pays: wins forgive a quarter ante more. Once at each demon's table, K protects a card from every cheat.",
                    "Taç öder: kazanç çeyrek ante fazla siler. Her şeytanın masasında bir kez K ile bir kartı her hileden korur.");
                default: return "";
            }
        }

        /// <summary>The price (or the comfort) of the class: the starting sentence and what it cannot do.</summary>
        public static string SinnerDetail(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("No tricks — the easy way down.", "Hilesi yok — en kolay yol.");
                case Warlock.ClassId: return L("Sees 2 House cards at Belial's. Major cheats get through.", "Belial'de kasanın 2 kartını görür. Büyük hileler geçer.");
                case King.ClassId: return L("A heavier sin: the crown weighs on the sentence.", "Daha ağır günah: taç cezaya biner.");
                default: return "";
            }
        }

        public static string SinnerStartFormat => L("STARTS AT {0} YEARS", "{0} YILLA BAŞLAR");
        public static string ChooseSinnerTitle => L("CHOOSE YOUR SIN", "GÜNAHINI SEÇ");
        public static string ChooseSinnerSubtitle => L("Who were you, up there?", "Yukarıda kimdin?");
        public static string ChooseSinner => L("CHOOSE", "SEÇ");

        /// <summary>The badge's hover box: the ability, and what is left of it. {0} left, {1} in all.</summary>
        public static string SinnerChargesFormat => L("Left: {0} of {1}", "Kalan: {0} / {1}");

        public static string WardFlash => L("WARD", "KORUMA");
        public static string FreeFoldMessage => L("An honest heart: this fold costs nothing (once a run).", "Dürüst kalp: bu çekilme bedava (koşuda bir kez).");
        public static string ProtectPrompt => L("Pick the card the crown protects (K again: cancel).", "Tacın koruyacağı kartı seç (yine K: vazgeç).");
        /// <summary>{0}: the card.</summary>
        public static string ProtectedFormat => L("The crown protects your {0}: no cheat may touch it this hand.",
            "Taç {0} kartını koruyor: bu el hiçbir hile ona dokunamaz.");
        public static string ProtectSpent => L("The crown has spent its protection at this demon's table.", "Taç bu şeytanın masasındaki korumasını kullandı.");
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
                            "An ability \"at each demon's table\" is spent per demon: changing tables refills nothing, a demon never sat " +
                            "with starts full, and Lucifer's table is full again at every summons.\n\n",
                "İlk elden önce kim olduğunu seçersin. Sınıf bütün koşu boyunca, her masada seninledir.\n" +
                "\"Her şeytanın masasında\" olan bir yetenek şeytan başına harcanır: masa değiştirmek doldurmaz, hiç oturulmamış " +
                "şeytanın masası dolu başlar, Lucifer'in masası her çağrılmada yeniden dolar.\n\n");
            foreach (SinnerClass c in SinnerRoster.All)
                page += Block(c) + "\n";
            return page.TrimEnd();
        }
    }
}
