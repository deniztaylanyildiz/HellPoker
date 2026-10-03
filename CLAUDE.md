# Hell Poker — Claude için proje rehberi

Bu dosya Claude Code tarafından her oturumun başında otomatik okunur. Projenin kalıcı hafızasıdır;
konuşma geçmişi ise `Docs/DEVLOG.md` dosyasındadır. **Yeni bir oturuma başlarken önce `Docs/DEVLOG.md`'yi oku.**

## Oturum kuralları (Claude için)

- Kullanıcıyla **Türkçe** konuş. Kod, yorumlar, isimler ve oyun içi metinler **İngilizce**.
- Her anlamlı iş bloğunun sonunda ve oturum biterken `Docs/DEVLOG.md`'ye yeni bir kayıt ekle:
  tarih, kullanıcının isteği (kendi sözleriyle kısa özet), yapılanlar, alınan kararlar ve nedenleri, açık sorular, sıradaki adımlar.
- Mimari bir karar değiştiğinde bu dosyadaki ilgili bölümü de güncelle.
- Kod **SOLID** prensiplerine uygun olmalı (aşağıdaki mimari kurallara bak). Yeni kod için test yaz.

## Oyun

Unity 6 (6000.0.25f1), 2D URP. Tek oyunculu **5 Card Draw** poker, oyuncu **kasaya (House)** karşı oynar.

- Oyuncu **1000 yıl** cehennem cezasıyla başlar; amaç cezayı **0**'a indirmek (Absolved) — ama bu sadece **Lucifer'in masasında** olur.
- **Lucifer (final, `DealerRoster.Lucifer`, id `lucifer`):**
  - Eller arasında ceza ≤ `GameRules.LuciferGateYears` (250) olunca oyuncu nerede olursa olsun onun masasına **çağrılır**.
    Ceza taşınır, gelinen şeytan `LuciferGate.OriginDealerId`'de saklanır, her çağrılma bir deneme (`Attempts`).
  - Kontrol `TablePresenter.PassThroughGate`'te, her `Refresh` başında, sadece Betting'de (sonuç ekranında değil, NEXT HAND'de).
  - Masasından kalkılamaz: "NO ESCAPE", `CanLeaveTable` false, kendi repliği.
  - Masasında ceza 250'yi geçerse **düşer**: ceza en az `LuciferCastDownYears` (500), gelinen şeytanın masasına döner.
    250'ye inince yeniden çağrılır.
  - Zafer: onun masasında ceza 0.
  - Sıradan masada kazanç cezayı bitiremez, son 1 yıl kalır (`GameRules.KeepsTheLastYear`).
    O an şeytan "The last year is not mine to take. He is waiting." der, sayaç altında "The last year is his" yazar.
  - Dead Man's Hand her yerde anında aklar. Hiç çağrılmamışsa "WILD BILL'S ESCAPE", Lucifer'den düşmüşse ABSOLVED +
    "The Morning Star will remember this.".
  - Lucifer beklerken sıradan masada "son 250" modu açılmaz.
  - Son 250 kuralı onun masasında yok (`ForcedRaiseYears` 0). Sıradan masalarda da pratikte görülmüyor, çünkü 250'de çağrılıyor.
  - **Son anları:** ceza ≤ 150 (tek el bitirebilir): sıcak salon, alev damlayan gözler, "ONE HAND FROM FREEDOM".
  - `luciferGateYears: 0` = Lucifer'siz oyun. Presenter'a `finalDealer` verilmezse de Lucifer yok (eski testler böyle çalışır).
- **Ruh çizgisi** (`Dealer.SoulThreshold`): Mammon **2000**, Belial **1750**, Lilith **1500**. Ceza çizgiye ulaşınca oyuncunun
  **ruhu masaya** konur. Ruhun değeri `GameRules.SoulWorthYears` = **1000** yıl, oyuncuya asla gösterilmez.
  - Kalan ruh = 1000 − (ceza − çizgi). Ruh biterse (ceza ≥ çizgi + 1000 = `DamnationYears`) sonsuz lanet: oyun biter (Damned).
  - Ruh elinde (`IsSoulHand`) birim ruhun 1/10'u (100), tavan %30 (300), ikisi de kalan ruhu asla aşmaz.
    Kasa re-raise'i tavanı aşabilir ama kalan ruhu aşamaz.
  - Ruh elinde kayıp **ve çekilme** ×1.5 (`SoulLossPercent` = 150; kayıpta şeytanın `LossPercent`'iyle, çekilmede fold yüzdesiyle
    çarpılır, tek seferde yukarı yuvarlanır).
  - Kazanç ruhu doldurur; ceza çizginin altına inince ruh geri verilir. Son 250 yıl kuralı değişmedi.
  - Ruh bölgesinde hiçbir view'a yıl sayısı gitmez (pot / ante / sayaç gizli). Ruh barı (`SoulGauge`: kalan + masadaki pay)
    kullanılır; artırma "WAGER MORE", karşılama "MATCH IT".
- **Masa değiştirme:** sadece eller arasında (Betting), masadaki **LEAVE TABLE** ya da menüdeki **CHANGE TABLE** ile şeytan seçimine
  gidilir. Ceza yeni masaya taşınır (`IHellPokerGame.TakeOver`).
  - Ruh masadayken kilitli: buton "SOUL BOUND", basınca şeytan `SoulLocked` repliği söyler. Lucifer masasında "NO ESCAPE".
  - Seçim ekranında Lucifer dördüncü, **kilitli** kart: karanlık portre, "THE MORNING STAR", "Waits below 250 years", "LOCKED".
    Tıklanınca kimse oturmaz, ayrıntı panelinde "Not yet. Come down to me." çıkar.
  - Çizgisi geçilmiş şeytana oturmak uyarıyla (`SoulWarning`, SIT ANYWAY) serbest; ruh hemen o masaya konur.
  - Seçim ekranı her şeytanın çizgisini ve SAFE / SOUL AT STAKE durumunu gösterir. Continue şeytanı ve cezayı birlikte korur.
- **Bahis birimi** (`StakeScale`): elin başındaki cezanın 1/10'u, okunaklı adıma **aşağı** yuvarlanır:
  ceza ≥1000 → 100'ün katı, ≥500 → 50, ≥250 → 25, altı → 10 (en az 10; ceza daha azsa all-in).
  Örnek: 1000 → 100, 650 → 50, 340 → 25, 180 → 10. **Ante = 1 birim** (seçici yok; "ANTE X YEARS" + DEAL).
- **Masa tavanı:** masadaki toplam bahis elin başındaki cezanın en fazla **%30**'u (ante'den az olamaz). Tavanda artırma kilitlenir.
  Masaya konan yıllar sayaçtan anında düşer (`YearsOffTable`).
- Her elin akışı (en fazla **5** karar):
  1. DEAL: ante masaya konur, oyuncunun ilk **2** kartı birlikte açılır (karar yok).
  2. **3., 4. ve 5.** kartta Artır / Pas / Çekil / **CHECK TO DRAW**. Artırma = **1 birim**.
  3. Kart değiştirme (şeytana göre 3 ya da 4); kasa `HouseDrawStrategy` ile değiştirir. Yeni elde **1 karar**; artırma artık **2 birim**.
  4. Kasa `HouseCardsShown` kadar kart açar (Mammon 2, Belial 1, Lilith 2) → **1 karar** → kalanlar showdown'a.
  5. Kart değiştirdikten sonra oyuncu artırırsa kasa **re-raise** yapabilir (1 birim; **tavanı aşabilir**, cezayı / kalan ruhu aşamaz)
     → oyuncu **Karşıla (Call) / Çekil**.
     Karar `IHouseBettingStrategy` (`HandStrengthBettingStrategy` + şeytanın `HouseBettingStyle`'ı, zar `IRandomSource`'tan).
- **Pas** = artırmadan devam. **Çekil** = eli bırak; draw'dan önce `FoldPercentBeforeDraw`, sonra `FoldPercentAfterDraw` (yukarı yuvarlanır).
- **Mühür** (`IHellPokerGame.IsCommitted`): masadaki bahis tavana ulaşınca ya da oyuncu all-in olunca (ruhta: kalan ruhun tamamı) el
  mühürlenir. O elde Pas / Çekil / Artır sorulmaz ve reddedilir; Core kalan kararları kendisi geçer (`DecisionsSkipped`).
  - **Kart değiştirme yine sorulur.** Çekilmek artık mümkün değil (all-in oyuncu gibi).
  - Re-raise yeni bir bahistir: tavana çıkan artırmaya kasa yine re-raise yapabilir, oyuncu Call / Fold der.
    Karşıladığı re-raise tavanı geçirirse el mühürlenir.
  - Genel kural: Pas dışında anlamlı seçenek kalmayan karar atlanır (`SkipEmptyDecisions`).
  - Sunum: "THE PACT IS SEALED" yazısı (`TableMoment.PactSealed`) + şeytanın `Sealed` repliği; butonlar gizlenir, kalan kartlar
    tek tek, aralarında `SealedRevealPause` (0.6 sn) ile açılır. Space / tık animasyonu sona atlatır.
    Son karardaki artırmayla dolan masa (açılacak bir şey kalmamışsa) duyurulmaz.
- **CHECK TO DRAW** (D, `CheckToDraw`): draw'dan önceki kalan kararları tek seferde Pas'la geçer. Sadece draw'dan önce görünür;
  Pas yasakken (son 250 yıl) kilitli.
- **Son 250 yıl** (ceza ≤ 250): artırma mümkün olduğu sürece Pas yasak; tavana ya da all-in'e ulaşınca el mühürlenir (Pas'a gerek kalmaz).
  Ekran "cehennem ateşi" moduna geçer. Lucifer varken sıradan masada bu noktaya gelinmez (250'de çağrılır); sadece Lucifer'siz oyunda geçerli.
- Masada "Win: at least −X · Lose: at least +Y" satırı (en zayıf ele göre, `LeastYearsForgiven/Added`).
- Oyun **ana menüde** açılır: Continue / Change Table (koşu sürerken), New Game, Settings, How to Play (RULES / HANDS sayfaları),
  Records, Quit. Masada MENU butonu ya da Esc menüye döner.
- **Esc** her ekranda bir üst ekrana gider (`IMenuCommands.GoBack`): uyarı kapanır; masadaki el tablosu kapanır;
  kurallar / ayarlar / rekorlar / oyun sonu → menü; seçim → geldiği yer; masa → menü; menü → koşu. Her alt ekranda BACK butonu var.
  Ekran geçişlerinde 8 px'lik perde kalkar (`ScreenTransitionView`); perde hareket ederken girdi beklenir.
- **Akıcılık:**
  - Animasyon sürerken basılan her tuş / tık animasyonları sonuna atlatır (`ITableView.SkipAnimations`). O basış aksiyon üretmez;
    oyuncu görmediği bir masada karar vermez.
  - Kilitli butonlar soluk ama tıklanabilir; tıklanınca nedeni söylenir.
  - Hız ayarı Normal ×1 / Fast ×2 / Very Fast ×4 (`AnimationClock`).
- **Yol gösterme** (Settings ▸ Hand Guide):
  - Oyuncunun eli "NOW: ONE PAIR" diye adlandırılır (`IHellPokerGame.PlayerHandNow`).
  - Draw'da kasa mantığına göre tutulacak kartlar altın çerçeveyle parlar (`SuggestedDiscards`).
  - H / HANDS: el sıralaması paneli.
  - İlk oyun ipuçları her anı bir kez şeytanın ağzından anlatır (ilk karar, ilk draw, ilk re-raise, son 250, ruh). Ayarlardan sıfırlanır.
- **His:**
  - En az 4 birimlik kayıpta ekran sarsılır, sayaç kırmızı yanar.
  - Two Pair+ kazançta el adı büyük yazıyla belirir.
  - Dead Man's Hand'de ekran kararır, dört kart tek tek parlar (`TableMoment`).
- **Kayıt:** her el sonunda, yeni koşuda, masa değişiminde ve **el sürerken her adımda** (DEAL'dan itibaren) koşu kaydedilir
  (`RunArchive` → PlayerPrefs `run.save`).
  - Format: `RunSnapshot`, "key=value" satırları, **`v=2`**: dealer, years, rounds, hands, lowest, highest, best, dealers, soul,
    `lucifer` (masasında mı), `origin` (gelinen şeytan), `attempts`.
  - El sürüyorsa ayrıca `hand.stake`, `hand.ante`, `hand.drawn`, `hand.soul`, `hand.sealed` (`HandInProgress`; isteğe bağlı).
  - **`v=1` kayıtlar okunmaya devam eder**: Lucifer'i hiç görmemiş koşu sayılır.
  - Lucifer masasında olup nereden geldiği bilinmeyen kayıt silinir (bootstrap).
  - Bozuk ya da başka sürüm kayıt silinip yok sayılır. Deste ve kartlar kaydedilmez. Açılışta kayıt varsa Continue ile devam edilir.
  - **El ortasında kapatma:** açılışta yarım el `IHellPokerGame.ForfeitHand` ile kapanır. O anki bahis ve draw durumuna göre
    çekilmiş sayılır (ruh elinde ×1.5). Mühürlü el çekilemeyeceği için kaybedilmiş sayılır: en zayıf ele kayıp
    (bahis × şeytanın `LossPercent`'i × ruh çarpanı). Şeytan `Fled` repliğini söyler. Ceza lanete götürebilir.
- **Oyun sonu:** masada "THE END" → son ekranı. NEW GAME / MENU.
  - Ekranlar: "THE MORNING STAR FALLS" (Lucifer'i yenince; gözleri parlayıp 1.6 sn sonra söner, "fell on attempt N"),
    "WILD BILL'S ESCAPE" (hiç çağrılmadan Dead Man's Hand), ABSOLVED (Lucifer'siz oyun ya da düşmüş oyuncunun Dead Man's Hand'i,
    "The Morning Star will remember this." satırıyla), DAMNED. Seçim: `EndScreenView.TitleOf / StoryOf`.
  - Hikâye satırları: el sayısı, en düşük / en yüksek ceza, en iyi el, masalar, ruh, Lucifer'le karşılaşma sayısı.
- **Rekorlar** (`RecordBook`, `run.records`):
  - koşu, aklanma, lanet, şeytan başına aklanma (Lucifer'de biten koşu gelinen şeytana yazılır), en hızlı aklanma;
  - Lucifer'e ulaşma, Lucifer'i yenme, en az denemede yenme, Wild Bill kaçışları (satırlar isteğe bağlı, eski defter okunur).
- **Ayarlar** (`GameSettings`, PlayerPrefs `settings.*`): animasyon hızı, tam ekran (Alt+Enter; pencere 480×270'in tam katı),
  el rehberi, ipuçları. Batchmode'da (testler) ayar / kayıt / rekor süreç boyu tek bir bellek deposunda
  (`HellPokerBootstrap.BatchStore`); PlayMode testleri her testte onu temizler.
- **New Game → kurpiyer şeytan seçimi** (Mammon / Belial / Lilith). Her şeytanın kendi ev kuralları var (`DealerRoster`):
  | Şeytan | Kart değiştir | Kasa gösterir | Ödeme | Çekilme (önce/sonra) | Re-raise (Two Pair+ / blöf) | Hileler (gösterge) |
  |---|---|---|---|---|---|---|
  | **Mammon** (Tefeci, dürüst) | 3 | 2 | standart | %50 / %100 | %70 / %5 | Rehin, Haraç / **Satın Al** (4) |
  | **Belial** (Gümüş Dil, blöfçü) | 3 | 1 | yüksek (Quads ×15, SF ×25, Royal ×30) | %50 / %100 | %60 / %30 | Sahte Yüz, Çatal Dil / **Yılan Takası** (2), niyet %25 yalan, dil %20 kayar |
  | **Lilith** (Gecenin Kraliçesi, acımasız, en zor) | 4 | 2 | standart, kayıp **×1.25** | %100 / %100 | %90 / %10 | Gece Örtüsü, Diken / **Aysız Gece** (4) |
  | **Lucifer** (The Morning Star, final; seçilemez) | 3 | **0** | standart, kayıp **×1.25**; **sabit ölçek** ante 50 / tavan 150 | %100 / %100 | %80 / %25 | Bakış, Yeniden Yazma, Yanan Kart / **Düşüş** (1 = her el) |
  - Lucifer'in sabit ölçeği: `Dealer.Stakes` = `StakeScale.Fixed(50, 150)`, daha azı all-in. `Dealer.IsFinalTable`.
  - Mammon ve Belial'de `LossPercent` = 100, Lilith'te 125. Şeytan masada portresiyle oturur ve replik söyler (re-raise dahil, `UiText.Dealers.cs`).
- **Ödeme simetrik, çarpan sadece ante'ye:** artırmalar ve re-raise'ler 1'e 1 ödenir.
  - Kazanç: `toplam bahis + ante × (oyuncunun çarpanı − 1)` yıl silinir (cezayı geçemez).
  - Kayıp: `(toplam bahis + ante × (kasanın çarpanı − 1)) × LossPercent` yıl eklenir, yukarı yuvarlanır.
  - Örnek: ante 100, toplam 300, Full House (×8) → 300 + 700 = 1000. Beraberlik: değişiklik yok.
- **Şeytan hileleri** (`Core/Cheats`, sıra tabanlı dövüş hissi: şeytanın sonraki hamlesi görünür, hile kartlarda görünür vurur):
  - **Kötülük göstergesi** (`Dealer.MaliceMax`, tabloda parantez içinde): her el +1, oyuncu kazanınca +1, ceza ≤ 500 iken her el +1 daha
    (Lucifer hariç). Dolunca dağıtımda hile seçilir ve **niyet** olarak duyurulur (`IHellPokerGame.PendingCheat`; Belial'inki yalan olabilir).
  - Küçük / büyük: ceza ≤ `MajorCheatYears` (400) iken büyük hile %50. Lucifer'in Düşüş'ü sadece ≤ 150, **denemede bir kez**,
    "THE FALL AWAITS" diye duyurulur. Seçimler ayrı zar akışından (`IRandomSource`, seed+2).
  - Vuran / engellenen hile göstergeyi boşaltır; boşa giden (hedef yok) ya da anı gelmeyen (oyuncu önce çekildi) dolu bırakır.
  - Değişmezler: showdown sonucunu sadece Düşüş değiştirir; **Dead Man's Hand kartları bağışık**; her hile görünür;
    eski kurallar (mühür, ruh, Lucifer) geçerli. Her vuruştan önce `ICheatGuard` sorulur (şimdilik `AllowEveryCheat`; ileride sınıf yetenekleri).
  - Hedefler (kart seçen hileler, `CheatTable.AdvisedDiscards` = kasa mantığının atacağı kartlar):
    - Rehin: atılacak kartların en yükseği (hazır elde en düşük kart). Diken: atılacak kartlardan biri (yoksa rastgele).
    - Yanan Kart: en iyi kombinasyonun (çift, üçlü..., kenta / renk ise her kart) en yüksek kartı, yoksa en yüksek kart; yerine desteden rastgele.
    - Gece Örtüsü **dağıtımda** vurur, sadece henüz açılmamış (3.-5.) bir karta: kart sırası gelince yüzü hiç görünmeden, örtüyle açılır.
    - Çatal Dil %80 nişanlı (önce renk / 4 aynı renk bozulur, yoksa eli düşüren, yoksa zararsız değişim), %20 "dil kayar"
      (`Dealer.BackfirePercent`): rastgele renk değişimi, oyuncuya renk verebilir.
    - Bakış: kaybedecek ele kasa %100, kazanacak ele %50 (`HellPokerGame.GazeBluffPercent`) re-raise yapar; re-raise kesin bilgi değildir.
  - **Geri tepme (backfire):** hile oyuncunun elini güçlendirirse (`CheatResult.Backfired`, `CheatSession` vuruş öncesi / sonrası eli
    karşılaştırır). Sadece şansa bırakan hileler geri tepebilir: Çatal Dil'in kayması, Yanan Kart, Düşüş. Diğer hiçbir hile oyuncuya
    yaramaz (Satın Al ve Yılan Takası yarayacak kartı seçmez; testle sabit). Masada: yeni kart hemen döner, üstünde "BACKFIRE",
    şeytan kızgın (angry) ve kendi `Backfire` repliğini söyler; sonuç satırı "It backfired!". Kayıtta `backfires` (koşu), rekorlarda
    "Backfires seen".
  - **Gizlilik:** oyuncudan gizlenen kart (`WasPlayerCardHidden`) showdown'da kasa beş kartını açana kadar hiçbir yoldan yüzünü göstermez
    (mühürlü elin kendi kendine açılışı, hile vuruşu dahil); gizli kart varken draw ipucu çerçevesi yok. Sonuçta önce kasa, sonra oyuncu döner.
  - Kayıt `v=3`: `malice`, `cheat.major`, `backfires`, yarım elde `hand.cheat / hand.shown / hand.cheat.done`; v1/v2 boş göstergeyle okunur.
  - Masada: portrenin altında pip göstergesi, üstünde niyet şeridi (ikon + ad, hover'da açıklama, H panelinde de), yalan "LIAR" diye
    kırılıp gerçeğe döner. Vuruşta kart 1-2 px sarsılır, şeytan reraise animasyonu + hile repliği; işaretler kartta kalır
    (zincir, diken, örtü, sahte yüz parıltısı). Sonuç mesajında hile satırı (ruhta yıl yok). Şeytan başına ilk hile ipucu.
    How to Play'de CHEATS sayfası.
- **Denge** (`BalanceSimulation`, 2000 koşu, akıllı oyuncu; ruh, mühür, Lucifer ve **hilelerle**; oyuncu sadece gördüğüyle oynar,
  niyete tepki verir):
  | Şeytan | Aklanma (hedef) | Ort. el | Lucifer'e ulaşan | İlk denemede yenme | Ort. deneme | Hile / el |
  |---|---|---|---|---|---|---|
  | Mammon | %79.7 (~80) | ~49 | %85 | %34 | 3.0 | 0.27 |
  | Belial | %74.1 (~70) | ~26 | %81 | %39 | 2.5 | 0.48 |
  | Lilith | %52.6 (~55) | ~29 | %66 | %34 | 2.5 | 0.36 |

  Lucifer masasında el başına 0.83 hile; geri tepme: Yanan Kart %26, Düşüş %18, Çatal Dil ~%0.04 (kayma nadiren renk verir).
  Belial her el hile yapsa bile ~%75'in altına inmiyor (hileleri hafif); son karar oyun testinden sonra — bkz. DEVLOG.
  Ruhu masaya koyan koşular %29 / %38 / %61. `HELLPOKER_LUCIFER_GATE` (0 = Lucifer yok) ve `HELLPOKER_CAST_DOWN` ile denenebilir. Ruh değerini düşürmek el sayısını neredeyse değiştirmiyor
  (bahis birimi ruhla birlikte küçülüyor), sadece Lilith'i zorlaştırıyor — bkz. DEVLOG.
- **Dead Man's Hand** (A♠ A♣ 8♠ 8♣ + herhangi bir 5. kart) **yenilmez**: Royal Flush dahil her eli yener (testlerle sabit)
  ve oyuncu kazanırsa **tüm cezayı siler**. Kasa onunla kazanırsa en yüksek çarpan sayılır.
- Standart çarpanlar: High Card/Pair ×1, Two Pair ×2, Trips ×3, Straight ×4, Flush ×5, Full House ×8, Quads ×10, Straight Flush ×15, Royal ×20.
- Tüm sayılar ayarlanabilir: `GameRules` (+ `StakeScale`), `Dealer`, `PayoutTable`, `HouseBettingStyle`; Inspector'da `HellPokerBootstrap`.

## Mimari

```
Assets/Scripts/
  Core/          HellPoker.Core.asmdef  — noEngineReferences: true (UnityEngine KULLANILAMAZ)
    Cards/         Card, Rank, Suit, Hand (değişmez), IDeck/Deck
    Randomness/    IRandomSource, IShuffler, FisherYatesShuffler
    Evaluation/    HandEvaluator + Rules/ (her el türü bir IHandRule), VisibleHandReader (açık kartların şu anki eli)
    Draw/          ICardExchanger, IDiscardPolicy, IDrawStrategy (HouseDrawStrategy = kasa AI)
    Game/          IHellPokerGame/HellPokerGame (tur akışı), GameRules, PayoutTable, PunishmentLedger, HellPokerGameFactory,
                   RunStats / RunSnapshot (kayıt formatı) / HandInProgress (yarım el) / RecordBook (rekorlar),
                   LuciferGate (çağrılma / düşüş / deneme)
    Betting/       IHouseBettingStrategy, HandStrengthBettingStrategy, HouseBettingStyle (kasanın re-raise / blöf mizacı)
    Dealers/       Dealer (şeytanın ev kuralları paketi: MaxDiscards, HouseCardsShown, PayoutTable, HouseBettingStyle, SoulThreshold,
                   MaliceMax, ICheatPolicy Cheats), DealerRoster (hile listeleri ve gösterge boyları burada)
                   (Game/ altında ayrıca StakeScale: bahis birimi, ante, masa tavanı)
    Cheats/        ICheat (Id, Tier, Timing, CanApply, Apply → CheatResult), CheatIds, CheatTable (+ CheatMarks, CheatRules.IsImmune),
                   ICheatPolicy / DemonCheatPolicy (küçük / büyük / yalan), ICheatGuard (engelleme kancası), CheatSession (gösterge,
                   seçim, vuruş, işaretler; HellPokerGame beş anda Strike çağırır), Mammon/Belial/Lilith/LuciferCheats (hile başına bir sınıf)
  Presentation/  HellPoker.Presentation.asmdef — Unity katmanı (MVP)
    Abstractions/  ITableView (+ TableMoment), IHandView, IDealerView (+ DealerMood), IDealerSelectView (+ DealerChoice), IRunSession,
                   IMainMenuView, ISettingsView (+ IDisplayMode, IScreenTransition), IEndScreenView / IRecordsView (+ RunSummary),
                   IGuideSettings, DealerCard, SoulGauge (+ LeaveState), ITableCommands, IMenuCommands, Tone,
                   CheatDisplay (MaliceGauge, CheatCard, CheatImpact, CardMark; CardSlot.Mark)
    Animation/     SpriteClip + SpriteSheet (yatay şeridi kare karelere böler), DealerAnimationLibrary, SalonLibrary ve MenuBackdropLibrary
                   (yedek zincirleri; sabit resim + `Motion`, salonlar açılışta `Preload`), BackdropMotion (motion.txt: BackdropLayer,
                   BackdropParticles), SpriteFrameAnimator (Image üzerinde kare oynatır; döngü / tek sefer), AnimationClock (hız + atlama)
    Views/         uGUI view'ları (UI tamamen koddan kurulur, prefab yok): TableView, DealerView, DealerSelectView, SalonView,
                   SoulView, SentenceView, CardView, HandRanksPanel, TableMoments, TableScenes (çağrılma kararması / düşüş /
                   Lucifer titremesi), MaliceView (gösterge + niyet şeridi + yalanın kırılması), CheatEffects (hile vuruşu, BACKFIRE),
                   MenuBackdrop, BackdropMotionView (katmanlar + piksel parçacıkları, kendi Canvas'ı), FpsCounter (F3, dev build),
                   SettingsView, EndScreenView, RecordsView,
                   ScreenTransitionView, AnimationSequencer (Complete = atla; hata veren adım kuyruğu kilitlemez)
    Settings/      GameSettings (+ IGuideSettings), ISettingsStore (PlayerPrefsStore / MemoryStore), RunArchive (kayıt + rekorlar)
    Ui/            UiFactory, PixelScreen (480×270, tam sayı ölçek), UiArt (Resources'tan sprite/font/metin), Palette, UiText + UiText.Dealers
                   + UiText.Cheats, PixelOutline (`WithOutline()`: 8 yönlü 1 px siyah dış çizgi),
                   ButtonFeel (hover / 1 px basılma / kilitli görünüm), ClickCatcher
    TablePresenter (masa; IRunSession: yeni koşu, devam (Resume), LEAVE isteği, masa değiştirme (ceza taşınır), kayıt / rekor, RunEnded;
    her masada oyunu Func<Dealer, IHellPokerGame> ile kurar), MainMenuPresenter (tüm ekranlar arası gezinme, Esc, geçişler),
    SettingsPresenter (ayar ekranı ↔ GameSettings ↔ hız / pencere), UnityDisplayMode, DealerCards, KeyboardInput,
    FpsTour (dev build + `-fpstour`: her ekranda FPS ölçüp log'a yazar, çıkar), HellPokerBootstrap (composition root)
  Editor/        HellPokerSceneBuilder (menü: Hell Poker ▸ Build Main Scene), HellPokerMenu (Hell Poker ▸ Play, Ctrl+Shift+P),
                 HellPokerBuild (Hell Poker ▸ Build Windows: Builds/Windows/HellPoker.exe, x64; Build Windows (Development): Builds/WindowsDev),
                 HellPokerArtImporter (Resources/Art: Point filtre, PPU 100, sıkıştırmasız, 9-slice; Resources/Fonts: Hinted Raster),
                 HellPokerEditorStartup (editör boş sahneyle açılırsa HellPoker sahnesini açar — batchmode son açık sahneyi sıfırlıyor).
                 UYARI: `EditorSceneManager.playModeStartScene` KULLANMA — Test Runner'ın PlayMode sahnesini de yönlendirip testleri kilitliyor.
Assets/Tests/EditMode/  NUnit testleri (Core + Presenter, fake view'larla)
Assets/Tests/PlayMode/  Sahneyi yükleyip gerçek butonlarla oynayan testler: HellPokerSceneTests, SalonRegressionTests,
                        RunJourneyTests (yeni oyun → eller → masa değiştir → menü → devam → sahneyi yeniden yükle → devam),
                        LuciferJourneyTests (yığılmış destelerle çağrılma → düşüş → yeniden çağrılma → zafer),
                        CheatJourneyTests (her şeytan bir hile; CheatRig hileyi yansımayla masaya zorlar; DarkWatch her karede gizli kartı
                        izler), BackdropMotionPlayTests (menü ve salonlar hareket eder, tam piksel)
                        + HellPokerScreenshots / CheatScreenshots ([Explicit]: 1920×1080 ekran görüntüleri; hileler 40–57)
Assets/Scenes/HellPoker.unity  — ana sahne (kamera + HellPokerBootstrap)
Assets/Resources/Art/   Üretilmiş piksel görseller:
                          Demons/<dealerId>/<durum>.png  (idle, talk, gloat, angry, reraise, final, soul — yatay şerit, kareler 96×96)
                          Backgrounds/<dealerId>/{normal,hell,soul}.png  (şeytan salonları: tek kare 480×270 sabit resim)
                                                 <katman>[_hell|_soul].png + motion.txt  (hareketli katmanlar, parçacıklar)
                          Ui/ (background[_hell], panel[_hot], dialog[_lucifer], button_*, card_face/back/slot, suit_*[_small], digits,
                               title, coin, flames, divider, soul_lamp, fade, menu (+ menu_<katman>, menu_motion.txt),
                               cheat_icons (16×16, CheatIds sırası), malice_pips (8×8), card_marks (32×48 kart üstü işaretler))
Assets/Resources/Fonts/ HellPokerPixelTitle (Press Start 2P), HellPokerPixel (Tiny5) — OFL lisansları yanında; eksik glifler piksel olarak eklendi
Tools/ArtGen/           pixel.py (palet + çizim), pixel_layers.py (katmanlı zemin: Layer, Particles, döngü kontrolü, yazı bölgeleri),
                        pixel_demons.py, pixel_salons.py, pixel_ui.py, pixel_menu.py, fonts.py, generate_art.py,
                        preview.py (katmanlı zeminleri gerçek hızında oynatır)
                        (preview/ çıktısı repoya girmez)
```

### Görsel kurallar (16-bit piksel art)

- Stil: **SNES / eski DOS** — masa yok, sade koyu taş zemin, solda animasyonlu şeytan, eski RPG menü kutuları.
- **Palet:** `Tools/ArtGen/pixel.py` içindeki tek palet, en fazla 32 renk (koyu mor/siyah, cehennem kırmızısı/turuncusu,
  kemik beyazı, eski altın + Mammon yeşili, Lilith leylağı, Belial gümüşü). Her görsel ondan üretilir; kenar yumuşatma yok.
  Unity'deki `Palette` renkleri aynı hex değerleri (metin rengi de palet içinde kalır). Saydam karıştırma yerine sprite değiştir
  (ör. son 250 yılda `background_hell`).
- **Ekran:** her şey 480×270 piksellik `Screen` rect'i içinde, tam sayı piksellerle (`UiFactory.PlaceTL`). `PixelScreen`
  bunu tam sayı katıyla ölçekler (1920×1080'de ×4), kenarda siyah dolgu kalır; `Canvas.pixelPerfect` açık.
- **Boyutlar:** şeytan karesi 96×96, kart 32×48 (köşede 5×5, ortada 11×11 renk sembolü), butonlar / paneller / diyalog 12×12
  9-slice (kenar 4 px), piksel rakam 12×16, zemin 480×270, alev şeridi 32×20 karelik.
- **Fontlar:** iki font da 8 px ızgarada; boyut her zaman 8'in katı. `UiFactory.CreateText`: `Bold` = başlık fontu
  (Press Start 2P), diğerleri metin fontu (Tiny5). Fontta olmayan karakter kullanma (ör. "→"); gerekirse `fonts.py`'ye piksel glif ekle.
- **Animasyon:** kart hareketleri tam piksel adımlarla (dağıtma yukarıdan düşer, çevirme 2'şer piksel daralır).
  Şeytan durumları ~8 FPS (idle 5 FPS); `DealerView` presenter'ın `DealerMood`'unu animasyona çevirir:
  Gloating → gloat, Annoyed → angry, Scheming → reraise (tek sefer, sonra talk/idle); yazı yazılırken talk; dinlenirken
  öncelik soul > final > idle (ruh masadayken soğuk `soul`, son 250 yılda `final`).
- **Salonlar:** masa yok; her şeytanın kendi salonu (Mammon hazine odası, Belial tiyatro, Lilith ay bahçesi). Orta sütun
  okunabilirlik için vinyetle koyu. Son 250 yılda `hell`, ruh masadayken `soul` varyantı (soul, hell'den önceliklidir).
  Seçim ekranında seçili şeytanın salonu arkada, 8 px'lik perde geçişiyle.
- **Katmanlı zemin** (salonlar ve menü, `pixel_layers.py`): tam ekran çok kareli şerit yok (2048 px sınırı 4 kare demek, hareket
  kesik kesik görünüyordu). Zemin **tek kare sabit resim**; hareket eden her parça kendi küçük şeridi:
  - **8-12 kare, 8-12 FPS**, kare başına en fazla 1-2 piksel kayma (yavaş ama sürekli), şerit genişliği ≤ 2048.
  - Liste `motion.txt`'te: `layer <dosya> x y kare fps tekrar kayma(px/sn) sarma ofset`, `particles <tür> x y w h adet`.
    Varyant şeridi `<dosya>_hell` / `_soul`, yoksa normal (`SalonLibrary.Motion`). Kayan katman (Lilith'in kuşları) tam piksel ilerler, sarılır.
  - Kor / toz / sis gibi parçacıklar sprite karesi değil kod: `BackdropMotionView` (tek piksellik Image'lar, tam piksel adım,
    `AnimationClock` hızına uyar; türler embers / motes / wisps, soul'da soğuk, hell'de sıcak renk). Katmanlar gerçek zamanla döner.
  - `BackdropMotionView` kendi iç içe Canvas'ı: her kare hareket eden pikseller masa canvas'ını yeniden kurdurmaz.
  - Şu anki hareketler: Mammon kefeler (zıt fazda 1 px), sikke parıltıları, altın tozu; Belial perde kıvrımlarının dış yarısı,
    yılanlı sütunlar; Lilith mum alevi, yıldızlar, kuşlar, sis; Lucifer dıştaki 4 zincir, korlar; menü gözler (kırpma), yılan,
    ateş denizinin kenarları (ortaya doğru dither ile durulur), parıltılar, korlar.
- **Döngü kuralı:** periyodik hareketin fazı `2π · kare / kare_sayısı`. ArtGen her şeritte kontrol eder (testler de): son kareden ilk
  kareye geçiş, ardışık kareler arasındaki en büyük farktan büyük olamaz (%2 pay: aynı faz farklı kesirlerle nicelenir).
  Parıltı / göz kırpma gibi "patlayan" hareketler büyüyüp söner, 0. kare daima dinlenme hali.
- **Okunabilirlik kuralı:** oyun ve menü ekranındaki her yazı ya panelde / koyu şeritte durur ya da `WithOutline()` (8 yönlü 1 px
  siyah dış çizgi, `PixelOutline`) alır. Hareketli katman ve parçacıklar yazı bölgelerine girmez: `TABLE_TEXT` / `MENU_TEXT`
  (ArtGen) içinde katman temizlenir, sabit resim o bölgede katmanın 0. karesini alır (hiçbir şey eksik görünmez, ama oynamaz).
  `BackdropMotionTests` her şeridi bu bölgelere karşı denetler. Yeni yazı eklenirse bölgeyi de güncelle.
- **Ruh bölgesi görünümü:** sayaç yerine `SoulView` (fener + bar, masadaki pay yanıp söner, kayıpta kırmızı akar, girişte kutu
  yanıp söner). Hiç sayı yok. Solda LEAVE TABLE / SOUL BOUND butonu (dealer diyaloğunun altında).
- **Yedek zinciri:** şeytan: durum dosyası yoksa idle, o da yoksa tek portre (`Demons/<id>.png`), o da yoksa düz renk.
  Salon: varyant → normal → `Ui/background_hell` (sadece hell) → `Ui/background` → düz renk. Eksik görsel
  oyunu asla bozmaz (`DealerAnimationTests`, `SalonLibraryTests`).
- Görseller **elle düzenlenmez**: script'te değiştirip `generate_art.py` ile yeniden üret, `preview.py` ile kontrol et.
- Yeni şeytan: `DealerRoster`'a `Dealer`, `UiText.Dealers.cs`'e metinler, `pixel_demons.py`'ye çizim fonksiyonu (DEMONS'a id ile).
  Diğer şeytanlara Lucifer için `Farewell` (çağrılırken) ve `Returned` (düşüşten dönünce) replikleri de gerekir.
- **Lucifer asla tam görünmez:**
  - Portre zifiri karanlıkta iki yanan göz, iki pençe ucu ve bir kanat kenarı (`pixel_demons.lucifer_frames`). Yüz / beden / siluet yok.
  - `LuciferArtTests` her karede (angry hariç) en az %85 koyu piksel ister.
  - Salonu (`Backgrounds/lucifer/{normal,hell}` + katmanlar): tahtın sadece alt basamakları ve ayakları, zincirler, korlar.
  - Konuşurken isim "THE MORNING STAR" (metin fontu, kızıl), yazı kor turuncusu, kutu `Ui/dialog_lucifer`; her replikte ekran 1 px titrer.
    Masada adının altı boş. "Waits below 250 years" sadece seçim ekranındaki kilitli kartta.
- **Sahneler** (`TableScenes`, `IDealerView.SetDealer(card, SeatChange)`):
  - Çağrılma: eski şeytanın son sözünden sonra `Ui/fade` (¼ / ½ / ¾ / tam Bayer siyahı) ile yavaş kararma, değişim karanlıkta,
    sonra karanlık kalkar.
  - Düşüş: ekran 6 px'lik adımlarla yukarı kayar, eski salon aşağıdan gelir.
  - Her sahne iki kuyruk adımı. İkinci adım yeni şeytan konuşunca ya da `SetAction`'da kuyruğa girer.
  - Değişim anında eski masanın son anlar / çıkış butonu sıfırlanır. Atlama çalışır, anında değişim `Abort` eder.
- Importer 2048 px'ten geniş dokuyu küçültür: her şerit ≤ 2048 px (katmanlı zemin bu yüzden var).
- **Ana menü zemini** (`Ui/menu.png` tek kare + `menu_*` katmanları + `menu_motion.txt`, `pixel_menu.py`):
  - Konu: cehennemin dibi. Gökte Lucifer'in kanatları ve logonun üstünde iki gözü (o kadar), ufukta ateş denizi ve yanan şehir.
  - Kenarlarda Mammon'un altını / sandığı, Lilith'in hilali, Belial'in yılanlı sütunu, Dead Man's Hand kartları.
  - Orta sütun (x 70-410, y 25-260) iki adım koyulaştırılmış, altındaki ateş denizi sakin. `MenuBackdropTests` en az %80 koyu ister.
  - `MenuBackdrop.Create` menü, ayarlar ve rekorlar ekranında oynar (kurallar menü canvas'ında, zemin arkada kalır).
    Yedek zinciri: `Ui/menu` → `Ui/background` → düz renk (`MenuBackdropLibrary`). Sağ alt köşede sürüm (`Application.version`).
- **FPS:** development build'de (ve editörde) F3 sol üstte FPS (ortalama + en yavaş kare) gösterir. Hedef sabit 60 (vSync).
- **Açılış (splash):** stüdyo logosu `Assets/Art/Splash/caveman_logo.png` (`pixel_splash.py`, `generate_art.py splash`; 148×44
  çizilip ×5 büyütülmüş, saydam zemin, başlık fontuyla "CAVEMAN" + mağara adamı). Resources dışında: oyun kodu yüklemez.
  Ayarlar `HellPokerBuild.ApplySplashScreen` ile her build'den önce uygulanır (menü: Hell Poker ▸ Apply Splash Screen):
  splash açık, Unity logosu kapalı, siyah arka plan, sabit (zoom yok), tek logo 2 sn.
- Proje **lineer renk uzayında**: UI'da düşük alfa bile ekranda güçlü görünür.
- Yeni hile: `Core/Cheats`'te `ICheat` sınıfı + `CheatIds` + şeytanın politikasına (`DealerRoster`), `UiText.Cheats`'e ad / açıklama /
  replik / kayıt satırı, `pixel_ui.py`'de `CHEAT_ICON_IDS`'e ikon. Presenter şeytanı yalnızca `Dealer` üzerinden tanır.
- Sonraya (sadece kancalar var): oyuncu sınıfları / yetenekleri — niyeti görme, `ICheatGuard` ile engelleme, kart koruma, göstergeyi boşaltma.

### Mimari kurallar

- **Oyun kuralları sadece Core'da.** Core Unity'ye bağımlı olamaz (asmdef bunu derleyici seviyesinde zorlar).
- **Yeni el türü / özel el** → yeni bir `IHandRule` yaz, `StandardHandRules`'a ekle. `HandEvaluator`'a dokunma (OCP).
- **Yeni kural varyasyonu** (ör. 4 kart değiştirme, farklı ödeme) → yeni `IDiscardPolicy` / `IPayoutTable` implementasyonu.
- **Somut sınıflar sadece composition root'larda seçilir:** `HellPokerGameFactory` (Core) ve `HellPokerBootstrap` (Unity).
- **Presenter sadece arayüzlere bağlıdır** (`IHellPokerGame`, `ITableView`), renk/stil bilmez; anlamsal `Tone` / `DealerMood` gönderir,
  rengi ve animasyonu view seçer.
- Bir ekranın üstüne binen canvas (ör. geçiş perdesi) `UiFactory.CreateScreen(..., letterbox: false)` ile kurulur;
  yoksa tam ekran siyah kenar katmanı altındaki her şeyi örter.
- **Presenter durum tarif eder, animasyon bilmez:** her komuttan sonra masanın olması gereken halini (`CardSlot[]` vb.) gönderir.
  View'lar farkı bulup animasyonla gösterir. Tüm view güncellemeleri tek bir `AnimationSequencer` kuyruğundan geçer
  (sonuç mesajı kartlar açılmadan görünmez). Animasyon sürerken `ITableView.IsBusy` true olur ve presenter girdiyi yok sayar.
- uGUI butonları `UiFactory.MakeClickOnly` ile oluşturulmalı (yoksa Space/Enter son tıklanan butonu tekrar tetikler).
- **Girdi kaynakları** (`KeyboardInput` vb.) sadece `ITableCommands`'a konuşur.
- Rastgelelik her zaman `IRandomSource` üzerinden; testlerde seed veya `TestDecks.Stacked(...)` kullan.
- Oyuncuya görünen tüm metinler `UiText` içinde (ileride yerelleştirme için).

## Komutlar

Unity editörü **kapalıyken** (proje açıksa batchmode kilitlenir):

```powershell
# Testler (sonuç: results.xml). Sahne/UI değişikliğinde PlayMode'u da çalıştır.
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile unity.log
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults play.xml -logFile play.log

# Ekran görüntüleri (Screenshots/ klasörüne ya da $env:HELLPOKER_SHOTS'a)
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter HellPoker.PlayMode.Tests.HellPokerScreenshots -testResults shots.xml -logFile shots.log
# Hilelerin ekran görüntüleri: aynı komut, -testFilter HellPoker.PlayMode.Tests.CheatScreenshots

# Denge simülasyonu (her şeytana 2000 koşu; rapor sim.xml'deki test çıktısında).
# Başka ruh sayılarını kodu değiştirmeden denemek için önce: $env:HELLPOKER_SOUL_WORTH = "800"; $env:HELLPOKER_SOUL_LOSS = "150"
# Hile hızı: $env:HELLPOKER_MALICE = "4,2,4,1" (mammon,belial,lilith,lucifer); HELLPOKER_MALICE_WIN, HELLPOKER_MALICE_LOW; HELLPOKER_CHEATS = "0" hilesiz
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter HellPoker.Core.Tests.BalanceSimulation -testResults sim.xml -logFile sim.log

# Görselleri / fontları yeniden üret (py -m pip install --user pillow numpy fonttools)
py Tools/ArtGen/generate_art.py [demons] [salons] [ui] [menu]
py Tools/ArtGen/fonts.py
py Tools/ArtGen/preview.py        # Tools/ArtGen/preview/index.html: şeytan kareleri + katmanlı zeminler gerçek hızında (yazı bölgeleri kesikli)

# Oynanabilir Windows x64 build → Builds/Windows/HellPoker.exe + test paketi Builds/HellPoker-<sürüm>-win64.zip
# (Builds/ git'e girmez; editörde: Hell Poker ▸ Build Windows). Zip: HellPoker-<sürüm>/ klasörü (oyun, *_DoNotShip hariç) +
# Docs/Release/OKUBENI.txt ve GERI_BILDIRIM.txt ({VERSION} doldurulur, UTF-8 BOM). Sürüm: Player Settings ▸ Version (bundleVersion).
# Start-Process -Wait ile çalıştır ki çıkış kodu (hata = 1) görülsün. Oyun testi kontrol listesi: Docs/PLAYTEST.md
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerBuild.Windows -logFile build.log
# Player log (Company "Deniz", Product "Hell Poker"): %USERPROFILE%\AppData\LocalLow\Deniz\Hell Poker\Player.log
# Duman testi (her build): HellPoker.exe -fpstour → menü, seçim, salonlar, Lucifer; log'da hata olmamalı.
# FPS ölçümü: development build (Builds/WindowsDev, F3 açık), sonra her ekranı gezip log'a yazan tur:
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerBuild.WindowsDevelopment -logFile build.log
Builds\WindowsDev\HellPoker.exe -fpstour -screen-fullscreen 0 -screen-width 1920 -screen-height 1080   # log'da "Hell Poker FPS ..." satırları

# Ana sahneyi yeniden oluştur
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build -logFile build.log
```

Editör açıkken: Window ▸ General ▸ Test Runner. Oynamak için menüden **Hell Poker ▸ Play** (Ctrl+Shift+P)
ya da `Assets/Scenes/HellPoker.unity` → Play.
Kısayollar: Space/Enter dağıt · çek · pas · karşıla, R artır, D check to draw, C karşıla, F çekil, 1-5 kart seç, H el tablosu, Esc bir üst ekran,
Alt+Enter tam ekran. Animasyon sürerken herhangi bir tuş / tık animasyonu atlatır.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
