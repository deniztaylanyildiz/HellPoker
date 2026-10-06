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
                case Jester.ClassId: return L("JESTER", "SOYTARI");
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
                case Jester.ClassId: return L("Who made a joke of everything", "Her şeyi şakaya vurmuş");
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
                case King.ClassId: return L("Wins pay a quarter ante more. Power: guard your whole hand against an announced cheat.",
                    "Kazanç çeyrek ante fazla. Güç: duyurulan hileye karşı bütün elini koru.");
                case Jester.ClassId: return L("Jokers in the deck: one is any card, two lose. A win adds one.",
                    "Destede joker: biri istediğin kart, ikisi kayıp. Kazanç bir tane ekler.");
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
                case Jester.ClassId: return L("The demon may draw a joker too.", "Şeytan da joker çekebilir.");
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

        /// <summary>The King's crown refused a cheat.</summary>
        public static string CrownFlash => L("CROWN", "TAÇ");
        public static string CrownRaisedMessage => L("The crown guards your hand: no cheat may touch your cards this hand.",
            "Taç elini koruyor: bu el hiçbir hile kartlarına dokunamaz.");
        public static string PowerNoCheatToGuard => L("Nothing to guard against: the demon has announced no cheat.",
            "Korunacak bir hile yok: şeytan henüz hile duyurmadı.");
        public static string PowerHandProtected => L("The crown already guards this hand.", "Taç bu eli zaten koruyor.");
        public static string FreeFoldMessage => L("An honest heart: you walk away for nothing.", "Dürüst kalp: bu eli bedelsiz bıraktın.");

        /// <summary>{0} the charge, {1} full, {2} a win, {3} a loss, {4} a fold.</summary>
        public static string PowerChargingFormat => L("Your power charges: {0} / {1}. Win +{2}, lose +{3}, fold +{4}.",
            "Gücün doluyor: {0} / {1}. Kazanç +{2}, kayıp +{3}, çekilme +{4}.");
        public static string PowerNoHand => L("The power waits for a hand to be played.", "Güç, oynanan bir eli bekler.");
        public static string PowerCannotFold => L("The pact is sealed: this hand cannot be left.", "Mühür vuruldu: bu el bırakılamaz.");
        public static string PowerNoCheat => L("No cheat is announced: the ward waits for one.", "Duyurulan bir hile yok: koruma bir hile bekler.");
        public static string PowerMajorCheat => L("A major cheat is beyond a ward.", "Büyük bir hileye koruma işlemez.");
        public static string PowerWardAlreadyUp => L("The ward is already up.", "Koruma zaten hazır.");

        /// <summary>Why the power cannot be used now, in words.</summary>
        public static string PowerRefused(SinnerAbility ability, HellPoker.Core.Game.PowerRefusal refusal, int charge, int full)
        {
            switch (refusal)
            {
                case HellPoker.Core.Game.PowerRefusal.NotCharged:
                    return string.Format(PowerChargingFormat, charge, full, ChargeRules.Default.PerWin, ChargeRules.Default.PerLoss, ChargeRules.Default.PerFold);
                case HellPoker.Core.Game.PowerRefusal.CannotFold: return PowerCannotFold;
                case HellPoker.Core.Game.PowerRefusal.NoCheatAnnounced: return ability == HellPoker.Core.Sinners.SinnerAbility.Protect ? PowerNoCheatToGuard : PowerNoCheat;
                case HellPoker.Core.Game.PowerRefusal.MajorCheat: return PowerMajorCheat;
                case HellPoker.Core.Game.PowerRefusal.WardAlreadyRaised: return PowerWardAlreadyUp;
                case HellPoker.Core.Game.PowerRefusal.HandAlreadyProtected: return PowerHandProtected;
                default: return PowerNoHand;
            }
        }

        /// <summary>One line for every class: {0} = "PEASANT 2 · WARLOCK 0 · KING 1".</summary>
        public static string RecordsClassesFormat => L("Freed as: {0}", "Sınıfa göre aklanma: {0}");

        public static string SinnersButton => L("SINNERS", "GÜNAHKÂRLAR");

        // ------------------------------------------------------------------ the Jester and his jokers

        /// <summary>The Jester's badge: the jokers in the deck ({0}).</summary>
        public static string JokerCountFormat => L("×{0}", "×{0}");

        /// <summary>The badge's hover line: {0} jokers now, {1} the line above which a loss takes one away, {2} the floor.</summary>
        public static string JokerBadgeFormat => L("Jokers in the deck: {0}. A win +1; above {1}, a loss -1 (never below {2}).",
            "Destede joker: {0}. Kazanç +1; {1} üstündeyken kayıp -1 (en az {2}).");

        public static string JokerBadgeHint(int jokers)
        {
            var jester = (Jester)SinnerRoster.Jester;
            return string.Format(JokerBadgeFormat, jokers, jester.JokerLossLine, jester.StartingJokers);
        }

        /// <summary>K (or the badge) for the Jester: he has no power to switch on.</summary>
        public static string JesterPowerInfo => L("The Jester's power is the jokers in the deck.", "Soytarı'nın gücü destedeki jokerler.");

        /// <summary>Two jokers (or more) in the player's hand during play.</summary>
        public static string TwoJokersWarning => L("Two jokers: you lose at the showdown, discard the extra", "İki joker: el sonunda kaybedersin, fazlasını at");

        /// <summary>The caption while the player's visible cards hold a joker: {0} the best hand it makes.</summary>
        public static string HandNowJokerFormat => L("WITH JOKER: {0}", "JOKER İLE: {0}");

        public static string JokerPrompt => L("Name your joker: left / right rank, up / down suit, Enter.", "Jokerini seç: sol / sağ değer, yukarı / aşağı renk, Enter.");

        /// <summary>The picker's live line: {0} the hand the chosen card makes.</summary>
        public static string JokerResultFormat => L("With this card: {0}", "Bu kartla elin: {0}");
        public static string JokerBestTag => L(" (best)", " (en iyisi)");
        public static string JokerRankLabel => L("RANK", "DEĞER");
        public static string JokerSuitLabel => L("SUIT", "RENK");
        public static string JokerConfirm => L("NAME IT", "SEÇ");

        /// <summary>The demon's two jokers come to light at the showdown.</summary>
        public static string JokerLaughFlash => L("HA! HA! HA!", "HA! HA! HA!");
        public static string TwoJokersCaption => L("TWO JOKERS", "İKİ JOKER");

        /// <summary>{0} the years added.</summary>
        public static string PlayerBustFormat => L("Two jokers in your hand: the hand is lost. +{0} years.", "Elinde iki joker: el kaybedildi. +{0} yıl.");
        public static string PlayerBustSoul => L("Two jokers in your hand: the hand is lost.", "Elinde iki joker: el kaybedildi.");

        /// <summary>{0} the years forgiven.</summary>
        public static string HouseBustFormat => L("Two jokers in the demon's hand: you win! -{0} years.", "Şeytanın elinde iki joker: kazandın! -{0} yıl.");
        public static string HouseBustSoul => L("Two jokers in the demon's hand: you win!", "Şeytanın elinde iki joker: kazandın!");
        /// <summary>Twenty jokers in the Jester's deck.</summary>
        public static string JokerJackpotFlash => L("TWENTY JOKERS!", "YİRMİ JOKER!");

        /// <summary>{0} the relic's name, {1} its gift, {2} its curse.</summary>
        public static string JokerJackpotRattleFormat => L("Twenty jokers: the deck is cleared. {0} is yours. {1} But: {2}",
            "Yirmi joker: deste temizlendi. {0} artık sende. {1} Ama: {2}");
        public static string JokerJackpotAgain => L("Twenty jokers again: the deck is cleared.", "Yine yirmi joker: deste temizlendi.");

        /// <summary>Both sides held two jokers or more: the fewer wins, as many each is a push.</summary>
        public static string JokerDuelWin => L("Too many jokers on both sides: you held fewer, you win.", "İkinizde de joker fazla: daha az jokeri olan sen kazandın.");
        public static string JokerDuelLoss => L("Too many jokers on both sides: the demon held fewer, the demon wins.",
            "İkinizde de joker fazla: daha az jokeri olan şeytan kazandı.");
        public static string JokerDuelPush => L("As many jokers each: a push, no years change.", "Eşit joker: el berabere, yıl değişmez.");

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
                            "stays on until it is used (the gauge empties) or switched off with K again. The Jester has no gauge: his jokers are his power " +
                            "(a single joker at the showdown becomes any card you name; two or more lose the hand).\n\n",
                "İlk elden önce kim olduğunu seçersin. Sınıf bütün koşu boyunca, her masada seninledir.\n" +
                "Her sınıfın bir gücü ve portrenin altında {0} pipli bir göstergesi var: kazanılan el +{1}, kaybedilen +{2}, çekilme +{3}. " +
                "Gösterge koşuya aittir (yeni masa ve Lucifer onu korur). Dolunca K ya da rozet gücü açar; güç kullanılana " +
                "(gösterge boşalır) ya da yine K ile kapatılana kadar açık kalır. Soytarı'nın göstergesi yok: gücü jokerleri " +
                "(showdown'da tek joker seçtiğin karta dönüşür; iki ya da fazlası eli kaybettirir).\n\n"), charge.Full, charge.PerWin, charge.PerLoss, charge.PerFold);
            foreach (SinnerClass c in SinnerRoster.All)
                page += Block(c) + "\n";
            return page.TrimEnd();
        }
    }
}
