using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The demons' cheats in words: names, what they do, what the demon says, what the result line tells.</summary>
    internal static partial class UiText
    {
        public static string MaliceLabel => L("MALICE", "KÖTÜLÜK");
        public static string LiarFlash => L("LIAR", "YALANCI");
        public static string FallAwaits => L("THE FALL AWAITS", "DÜŞÜŞ BEKLİYOR");
        public static string CheatsButton => L("CHEATS", "HİLELER");
        public static string CheatsTitle => L("THE DEMONS' CHEATS", "ŞEYTANLARIN HİLELERİ");

        public static string CheatName(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return L("COLLATERAL", "REHİN");
                case CheatIds.Tithe: return L("TITHE", "HARAÇ");
                case CheatIds.Buyout: return L("BUYOUT", "SATIN AL");
                case CheatIds.FalseFace: return L("FALSE FACE", "SAHTE YÜZ");
                case CheatIds.ForkedTongue: return L("FORKED TONGUE", "ÇATAL DİL");
                case CheatIds.SerpentSwap: return L("SERPENT SWAP", "YILAN TAKASI");
                case CheatIds.NightVeil: return L("NIGHT VEIL", "GECE ÖRTÜSÜ");
                case CheatIds.Thorn: return L("THORN", "DİKEN");
                case CheatIds.Moonless: return L("MOONLESS", "AYSIZ GECE");
                case CheatIds.Gaze: return L("GAZE", "BAKIŞ");
                case CheatIds.Rewrite: return L("REWRITE", "YENİDEN YAZ");
                case CheatIds.BurningCard: return L("BURNING CARD", "YANAN KART");
                case CheatIds.TheFall: return FallAwaits;
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        /// <summary>What a cheat does, in one sentence (the intent's tooltip, the H panel, the Cheats page). No numbers.</summary>
        public static string CheatDescription(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return L("A card you would throw back is chained: it must stay.", "Atacağın bir kart zincirlenir: kalmak zorunda.");
                case CheatIds.Tithe: return L("If you win this hand, a unit of what you win is his.", "Bu eli kazanırsan, kazancından bir birim onun.");
                case CheatIds.Buyout: return L("Your highest card is traded for one of the House's lowest.", "En yüksek kartın, kasanın en düşüklerinden biriyle takas edilir.");
                case CheatIds.FalseFace: return L("One card the House shows is not what it seems — until the showdown.",
                    "Kasanın açtığı bir kart göründüğü gibi değil — eller açılana kadar.");
                case CheatIds.ForkedTongue: return L("After the draw a card changes suit. Flushes beware. His tongue may slip.",
                    "Değişten sonra bir kartın rengi değişir. Renkler dikkat. Dili kayabilir.");
                case CheatIds.SerpentSwap: return L("A card of yours slips to the House; what comes back stays dark.",
                    "Bir kartın kasaya kayar; yerine gelen karanlıkta kalır.");
                case CheatIds.NightVeil: return L("One of your cards will come to you in the dark.", "Kartlarından biri sana karanlıkta gelecek.");
                case CheatIds.Thorn: return L("A thorn in a card you would throw back: letting it go costs a unit.",
                    "Atacağın bir kartta diken var: onu bırakmak bir birime mal olur.");
                case CheatIds.Moonless: return L("The cards you draw stay dark until the showdown.", "Çektiğin kartlar eller açılana kadar karanlıkta kalır.");
                case CheatIds.Gaze: return L("He sees your hand. His raises will hurt.", "Elini görüyor. Artırmaları acıtacak.");
                case CheatIds.Rewrite: return L("After the draw a card is rewritten into something weaker.", "Değişten sonra bir kart daha zayıf bir şeye yeniden yazılır.");
                case CheatIds.BurningCard: return L("Your best combination's top card burns into a random one.",
                    "En iyi kombinasyonunun en yüksek kartı yanıp rastgele bir karta döner.");
                case CheatIds.TheFall: return L("If you would win, both best cards are dealt again. You may fold.",
                    "Kazanacaksan iki elin en iyi kartı yeniden dağıtılır. Çekilebilirsin.");
                default: return "";
            }
        }

        /// <summary>What the demon says the moment a cheat strikes.</summary>
        public static string CheatLine(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return L("Collateral. This one stays with you — on my terms.", "Teminat. Bu sende kalıyor — benim şartlarımla.");
                case CheatIds.Tithe: return L("A small tithe on your good fortune.", "Şansına küçük bir haraç.");
                case CheatIds.Buyout: return L("I'll buy that from you. My price, of course.", "Onu senden satın alayım. Fiyatı benden, tabii.");
                case CheatIds.FalseFace: return L("Look closer, darling. Or don't.", "Daha yakından bak, canım. Ya da bakma.");
                case CheatIds.ForkedTongue: return L("Did I say spades? I meant hearts.", "Maça mı dedim? Kupa demek istemiştim.");
                case CheatIds.SerpentSwap: return L("A little trade between friends.", "Dostlar arasında küçük bir takas.");
                case CheatIds.NightVeil: return L("Let the night keep this one.", "Bunu gece saklasın.");
                case CheatIds.Thorn: return L("Careful. It bites if you let go.", "Dikkat. Bırakırsan ısırır.");
                case CheatIds.Moonless: return L("No moon tonight. Draw in the dark.", "Bu gece ay yok. Karanlıkta çek.");
                case CheatIds.Gaze: return L("I see everything you hold.", "Elindeki her şeyi görüyorum.");
                case CheatIds.Rewrite: return L("That is not what you had. It is what I say you had.", "Elindeki o değildi. Elinde ne olduğunu ben söylerim.");
                case CheatIds.BurningCard: return L("Burn.", "Yan.");
                case CheatIds.TheFall: return L("Fall.", "Düş.");
                default: return "";
            }
        }

        /// <summary>Belial, caught announcing one cheat and playing another.</summary>
        public static string LiarLine => L("Did you believe me? How sweet.", "Bana inandın mı? Ne tatlı.");

        /// <summary>Over the card a cheat helped instead of hurt.</summary>
        public static string BackfireFlash => L("BACKFIRE", "GERİ TEPTİ");

        /// <summary>The result screen's line about the hand's cheat. Never a number while the soul is on the table.</summary>
        public static string CheatLog(string dealerName, CheatResult result, bool soul, int thornYears, int titheYears)
        {
            if (result == null || result.Outcome != CheatOutcome.Played) return null;
            string who = Capitalised(dealerName);
            string lost = result.Lost.HasValue ? result.Lost.Value.ToString() : L("a card", "bir kart");
            string gained = result.Gained.HasValue ? result.Gained.Value.ToString() : L("a card", "bir kart");
            bool tr = Lang.IsTurkish;
            if (result.Backfired)
            {
                // The cheat turned on its demon: said so, plainly.
                switch (result.CheatId)
                {
                    case CheatIds.ForkedTongue: return tr ? $"{who} dili kaydı: {lost} kartın {gained} oldu. Geri tepti!"
                        : $"{who}'s tongue slipped: your {lost} became the {gained}. It backfired!";
                    case CheatIds.BurningCard: return tr ? $"{who} {lost} kartını yaktı — küllerden {gained} doğdu. Geri tepti!"
                        : $"{who} burned your {lost} — the {gained} rose from the ashes. It backfired!";
                    case CheatIds.TheFall: return tr ? $"{who}: {lost} düştü, {gained} yükseldi. Geri tepti!"
                        : $"{who}: the {lost} fell, and the {gained} rose. It backfired!";
                    default: return tr ? $"{who} hilesi geri tepti!" : $"{who}'s cheat backfired!";
                }
            }
            switch (result.CheatId)
            {
                case CheatIds.Collateral: return tr ? $"{who}, {lost} kartını rehin olarak zincirledi." : $"{who} chained your {lost} as collateral.";
                case CheatIds.Tithe:
                    if (soul || titheYears <= 0) return tr ? $"{who} kazancından haraç aldı." : $"{who} kept a tithe of your win.";
                    return tr ? $"{who} haraç aldı: kazancından {titheYears} yıl." : $"{who} kept a tithe: {titheYears} years of your win.";
                case CheatIds.Buyout: return tr ? $"{who}, {lost} kartını {gained} karşılığında aldı." : $"{who} took your {lost} for a {gained}.";
                case CheatIds.FalseFace: return tr ? $"{who} sana sahte bir yüz gösterdi." : $"{who} showed you a false face.";
                case CheatIds.ForkedTongue: return tr ? $"{who} dili {lost} kartını {gained} yaptı." : $"{who}'s tongue turned your {lost} into the {gained}.";
                case CheatIds.SerpentSwap: return tr ? $"{who} yılanı {lost} kartını çaldı." : $"{who}'s serpent stole your {lost}.";
                case CheatIds.NightVeil: return tr ? $"{who} kartlarından birini örttü." : $"{who} veiled one of your cards.";
                case CheatIds.Thorn:
                    if (thornYears <= 0) return tr ? $"{who} dikeni batacak el bulamadı." : $"{who}'s thorn found no hand to prick.";
                    if (soul) return tr ? $"{who} dikeni ruhunu kanattı." : $"{who}'s thorn drew blood from your soul.";
                    return tr ? $"{who} dikeni sana {thornYears} yıla mal oldu." : $"{who}'s thorn cost you {thornYears} years.";
                case CheatIds.Moonless: return tr ? $"{who} yeni kartlarını karanlıkta tuttu." : $"{who} kept your new cards in the dark.";
                case CheatIds.Gaze: return tr ? $"{who} kartlarını gördü." : $"{who} saw your cards.";
                case CheatIds.Rewrite: return tr ? $"{who}, {lost} kartını {gained} olarak yeniden yazdı." : $"{who} rewrote your {lost} as the {gained}.";
                case CheatIds.BurningCard: return tr ? $"{who}, {lost} kartını yakıp {gained} yaptı." : $"{who} burned your {lost} into the {gained}.";
                case CheatIds.TheFall: return tr ? $"{who}: {lost} düştü. Eller yeniden yargılandı." : $"{who}: the {lost} fell. The hands were judged anew.";
                default: return null;
            }
        }

        private static string Capitalised(string name)
        {
            if (string.IsNullOrEmpty(name)) return L("The House", "Kasa");
            // Every word: "THE MORNING STAR" → "The Morning Star" ("SABAH YILDIZI" → "Sabah Yıldızı").
            string[] words = name.Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0)
                    words[i] = words[i].Substring(0, 1) + Lower(words[i].Substring(1));
            return string.Join(" ", words);
        }

        /// <summary>Lower case that knows Turkish (I → ı, İ → i) without ever changing the current culture.</summary>
        public static string Lower(string text)
        {
            if (string.IsNullOrEmpty(text) || !Lang.IsTurkish) return text?.ToLowerInvariant();
            return text.Replace('I', 'ı').Replace('İ', 'i').ToLowerInvariant();
        }

        // ------------------------------------------------------------------ first-game tips: each demon's first cheat, in their voice

        /// <summary>Key prefix (saved): never translated.</summary>
        public const string TipCheatPrefix = "tip.cheat.";

        public static string TipCheatFor(string dealerId) => TipCheatPrefix + dealerId;

        public static string CheatTipText(string dealerId)
        {
            switch (dealerId)
            {
                case Core.Dealers.DealerRoster.MammonId: return L("When my purse of malice is full, I collect. The sign above me says how.",
                    "Kötülük kesem dolunca tahsil ederim. Nasıl olacağını üstümdeki işaret söyler.");
                case Core.Dealers.DealerRoster.BelialId: return L("My malice is full. That sign says what I will do... most of the time.",
                    "Kötülüğüm doldu. O işaret ne yapacağımı söyler... çoğu zaman.");
                case Core.Dealers.DealerRoster.LilithId: return L("My thorns are full. Read the sign, little soul — then suffer it.",
                    "Dikenlerim doldu. İşareti oku, küçük ruh — sonra çek cezasını.");
                case Core.Dealers.DealerRoster.LuciferId: return L("Every hand, I take something. I will always tell you what.",
                    "Her elde bir şey alırım. Ne olduğunu hep söylerim.");
                default: return null;
            }
        }

        // ------------------------------------------------------------------ How to Play: the Cheats page

        public static string CheatsPage()
        {
            string Line(string id) => $"   {CheatName(id)} — {CheatDescription(id)}";
            return
                L("Every demon has a MALICE gauge under the portrait. It fills every hand (faster when you win, and under 500 years).\n" +
                  "Full, the demon announces a cheat above the portrait, then plays it on the cards. Under 400 the big ones come out.\n" +
                  "A♠ A♣ 8♠ 8♣ are beyond any cheat. A cheat left to chance may BACKFIRE and help you (tongue, fire, The Fall).\n\n",
                  "Her şeytanın portresinin altında bir KÖTÜLÜK göstergesi var. Her el dolar (kazanınca ve 500 yılın altında daha hızlı).\n" +
                  "Dolunca şeytan portrenin üstünde bir hile duyurur, sonra kartlarda oynar. 400'ün altında büyükleri gelir.\n" +
                  "A♠ A♣ 8♠ 8♣ hiçbir hileden etkilenmez. Şansa kalan bir hile GERİ TEPEBİLİR ve sana yarar (dil, ateş, Düşüş).\n\n") +
                L("MAMMON (always honest)\n", "MAMMON (hep dürüst)\n") + Line(CheatIds.Collateral) + "\n" + Line(CheatIds.Tithe) + "\n" + Line(CheatIds.Buyout) + "\n" +
                L("BELIAL (a quarter of his intents are lies)\n", "BELIAL (niyetlerinin dörtte biri yalan)\n") + Line(CheatIds.FalseFace) + "\n" +
                Line(CheatIds.ForkedTongue) + "\n" + Line(CheatIds.SerpentSwap) + "\n" +
                "LILITH\n" + Line(CheatIds.NightVeil) + "\n" + Line(CheatIds.Thorn) + "\n" + Line(CheatIds.Moonless) + "\n" +
                L("THE MORNING STAR (every hand)\n", "SABAH YILDIZI (her el)\n") + Line(CheatIds.Gaze) + "\n" + Line(CheatIds.Rewrite) + "\n" +
                Line(CheatIds.BurningCard) + "\n" + Line(CheatIds.TheFall);
        }
    }
}
