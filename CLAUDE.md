# Hell Poker — Claude için proje rehberi

Bu dosya Claude Code tarafından her oturumun başında otomatik okunur. Projenin kalıcı hafızasıdır;
konuşma geçmişi ise `Docs/DEVLOG.md` dosyasındadır. **Yeni bir oturuma başlarken önce `Docs/DEVLOG.md`'yi oku.**

## Oturum kuralları (Claude için)

- Kullanıcıyla **Türkçe** konuş. Kod, yorumlar, isimler ve oyun içi metinler **İngilizce**.
- Her anlamlı iş bloğunun sonunda ve oturum biterken `Docs/DEVLOG.md`'ye yeni bir kayıt ekle:
  tarih, kullanıcının isteği (kendi sözleriyle kısa özet), yapılanlar, alınan kararlar ve nedenleri, açık sorular, sıradaki adımlar.
- Mimari bir karar değiştiğinde bu dosyadaki ilgili bölümü de güncelle.
- Kod **SOLID** prensiplerine uygun olmalı (aşağıdaki mimari kurallara bak). Yeni kod için test yaz.

## Durum

**Demo 1.0 dondu**: etiket `v1.0-demo` (commit `caf79bd167dd4518f63f90c61298fe61e900bf0d`). Phase 2 bundan sonra.

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
- **Deste sayılır** (`GameRules.ContinuousDeck`, varsayılan açık): deste eller boyunca devam eder; dağıtılan, atılan ve çekilen kartlar
  dönmez. **Deste koşuya ait:** masa değişimi, Lucifer'e çağrılma ve düşüş desteyi yeni masaya taşır (`SeatAt` → `RestoreDeck(DeckCards)`),
  kayıt `deck` anahtarıyla saklar (v=4, isteğe bağlı, `AS,10H,3C`…, sıradaki önce; eller arasında ve el ortasında — yarım elin kartları
  dönmez; yoksa / bozuksa / tekrar eden kart varsa taze deste, kayıt geçersiz sayılmaz; `CardCodes`). Yeni koşu taze deste. Yeni elin başında destede `HellPokerGame.CardsForAHand` (iki el + iki tarafın en çok draw'u + emanetlerin yeniden çekişi +
  `CheatTable.MostCardsACheatDeals` (2) + hayalet elde 5) kadar kart yoksa şeytan 52'yi karar (`DeckShuffledThisHand`, ilk kararda
  `DeckRanOut`). Yeni masa / çağrılma / düşüş / devam taze deste (yeni oyun; kartlar kaydedilmez). Masada kasanın satırının üstünde
  `DESTE 37` (hover açıklama; `ITableView.SetDeckCount`). **KARIŞTIR** (S / buton, `ShuffleDeck`; `IHellPokerGame.WhyNoShuffle` →
  `ShuffleRefusal`, `Shuffle`; sayaç 10 kartın altında sayı yerine `KARIŞIYOR` / `SHUFFLING` yazar, `TablePresenter.DeckCountShownFrom`): eller arasında, her elden önce bir kez, +`ShuffleYears` (10) yıl. Red nedenleri: `SoulOnTable`,
  `AlreadyShuffled`, `DeckFull` (deste 52), `ShuffleComing` (deste `CardsForAHand`'in altında: şeytan zaten bedava karacak),
  `TooFewYears` (300 altı) — bunlarda buton gizli; `WouldStakeSoul` (10 yıl ruh çizgisine ulaştırır) — buton görünür ama soluk,
  mesaj `Rules.ShuffleYears` ve `SoulThreshold`'dan. Masa-başı yığılmış deste kullanan testler `continuousDeck: false` ile kurulur. Soytarı'nın destesi her el karılır:
  sayaç ve KARIŞTIR yok. Kasa draw'da desteden fazlasını istemez; boş destede hileler ve Kemik Zar bir şey yapmaz (test).
  Günlük: otomatik ve oyuncunun karıştırması (yıl, kalan kart). `HELLPOKER_FRESH_DECK=1` simülasyonda eski (her el taze) deste.
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
  - Format: `RunSnapshot`, "key=value" satırları, **`v=4`**: dealer, years, rounds, hands, lowest, highest, best, dealers, soul,
    `lucifer` (masasında mı), `origin` (gelinen şeytan), `attempts`, `malice`, `cheat.major`, `grudge` (isteğe bağlı),
    `class` (sınıf id), `class.charge` (sınıf gücünün göstergesi 0–5; isteğe bağlı), `class.ward` (kurulu koruma; isteğe bağlı;
    `class.jokers`: Soytarı'nın joker sayısı, isteğe bağlı;
    eski `class.charges` / `class.charges.tables` yok sayılır); olaylar (isteğe bağlı, `RunEventState`): `event.seen`,
    `event.since`, `effect.next` (`HandModifier`), `effect.deferred`, `effect.deferred.hands`, `soul.sold`, `relics`, `relics.redraws`, `relics.redraws.tables`. v=4 tek seferde büyütülür:
    emanetler de kendi isteğe bağlı anahtarlarıyla aynı sürüme girer.
  - El sürüyorsa ayrıca `hand.stake`, `hand.ante`, `hand.drawn`, `hand.soul`, `hand.sealed`, `hand.cheat`, `hand.cheat.done`
    (`HandInProgress`; isteğe bağlı).
  - Artık yazılmayan anahtarlar (`hand.shown`, koşunun `backfires`'ı: yazılıp hiç okunmuyordu) eski kayıtlarda yok sayılır.
  - **`v=1` / `v=2` / `v=3` kayıtlar okunmaya devam eder**: Lucifer'i hiç görmemiş koşu / boş gösterge / Köylü (hakkı dolu) sayılır.
  - Lucifer masasında olup nereden geldiği bilinmeyen kayıt silinir (bootstrap).
  - Bozuk ya da başka sürüm kayıt silinip yok sayılır. Deste ve kartlar kaydedilmez. Açılışta kayıt varsa Continue ile devam edilir.
  - **El ortasında kapatma:** açılışta yarım el `IHellPokerGame.ForfeitHand` ile kapanır. O anki bahis ve draw durumuna göre
    çekilmiş sayılır (ruh elinde ×1.5). Mühürlü el çekilemeyeceği için kaybedilmiş sayılır: en zayıf ele kayıp
    (bahis × şeytanın `LossPercent`'i × ruh çarpanı). Şeytan `Fled` repliğini söyler. Ceza lanete götürebilir.
  - **Hileden kaçış (kin):** yarım elde planlanmış ama henüz vurmamış bir hile varsa (`HandInProgress.FledACheat`) gösterge
    hemen dolar ve `GameRules.GrudgeHands` (3) el boyunca her el +`GrudgeMalicePerHand` (1) fazla dolar (`CheatSession.Grudge`,
    kayıtta `grudge`). Şeytan `Hunted` repliğiyle alay eder ("Where to? This is Hell."), sonuç satırına kin notu eklenir.
- **Koşu günlüğü** (oyun testi için): her koşu bitince (aklanma, lanet, bırakma) ve oyun kapanırken yarım koşu için okunabilir bir metin
  `Application.persistentDataPath/runs/run-<yyyyMMdd-HHmmss>.txt` (Windows: Player.log'un klasöründe `runs`). Core'da saf `RunLog`
  (başlık: sürüm, dil, şeytan, sınıf, başlangıç / devam; her el: no, masa, ceza önce → sonra, oyuncu / kasa eli ya da çekilme, bahis, mühür, ruh;
  hileler duyurulan / vuran / engellenen / boşa giden / geri tepen; masa değişimi, çağrılma, düşüş, olay ve seçimi, emanet, sınıf gücü; sonuç).
  Presentation'da `IRunLogSink`: `FileRunLogSink` (en yeni 50 dosya, her hata Player.log'a uyarı), `MemoryRunLogSink` (testler); batchmode'da
  günlük yok. `TablePresenter.CloseLog` bootstrap kapanırken çağrılır; devam eden koşu yeni bir dosyada `resumed` diye sürer.
  Aynı saniyede başlayan ikinci koşu `run-<zaman>-2.txt`, `-3`... alır; aynı koşu yeniden yazılınca kendi dosyasında kalır.
- **Pencere:** pencere moduna geçince / boyut değişince **çerçevesiyle birlikte** (başlık çubuğu + kenarlık, Windows'ta DWM'den ölçülür)
  ekranın çalışma alanında ortalanır, dışına / görev çubuğunun altına taşmaz (`WindowCentering`: `SetResolution`'dan iki kare sonra
  `WindowScales.Centered` + `Screen.MoveMainWindowTo`; editörde atlanır). Gerçek build'de %125 ekranda ×2 / ×3 ile ölçüldü (her yanda
  eşit boşluk). Ölçen script DPI-aware olmalı (yoksa çalışma alanı mantıksal, çerçeve fiziksel piksel gelir). Açılışta pencere öne gelir (`WindowFocus`, Windows `SetForegroundWindow`).
  `runInBackground` açık (Player Settings + bootstrap): alt-tab'da oyun ve müzik durmaz.
- **Oyuncu zorla tutulmaz:** Quit eller arasında onaysız (koşu kayıtlı). El ortasında ya da ruh masadayken (`AbandonRisk` Hand / Soul)
  önce sorar: şeytanın `Fled` repliği + "Leave now and the hand is lost." / "Şimdi gidersen el kaybedilir.", QUIT / BACK, Esc kapatır
  (açılışta el zaten forfeit edilir; oyuncu artık bunu bilerek çıkar). Aynı onay kutusu (`IMainMenuView.AskToConfirm(taunt, uyarı,
  düğme)` + `Confirmed`) New Game ve Quit için. Koşu sürerken menüdeki **New Game** önce sorar (`IMainMenuView.AskToConfirmNewGame`):
  şeytanın `Scorn` repliği (her şeytana kendi tonunda alay) + bedeli (`IRunSession.AbandonRisk`: Run / Hand / Soul), ABANDON / BACK, Esc kapatır.
  Onayda `IRunSession.AbandonRun`: el ortasındaysa el `IHellPokerGame.ForfeitHand()` ile kapanış gibi çekilmiş sayılır (ceza eklenir);
  ruh masadaysa koşu lanet olarak rekora yazılır; kayıt silinir, masa artık girdi almaz. Henüz el dağıtılmamışsa sormaz.
- **Oyun sonu:** masada "THE END" → son ekranı. NEW GAME / MENU.
  - Ekranlar: "THE MORNING STAR FALLS" (Lucifer'i yenince; gözleri parlayıp 1.6 sn sonra söner, "fell on attempt N"),
    "WILD BILL'S ESCAPE" (hiç çağrılmadan Dead Man's Hand), ABSOLVED (Lucifer'siz oyun ya da düşmüş oyuncunun Dead Man's Hand'i,
    "The Morning Star will remember this." satırıyla), DAMNED. Seçim: `EndScreenView.TitleOf / StoryOf`.
  - Hikâye satırları: el sayısı, en düşük / en yüksek ceza, en iyi el, masalar, ruh, Lucifer'le karşılaşma sayısı.
- **Rekorlar** (`RecordBook`, `run.records`):
  - koşu, aklanma, lanet, şeytan başına aklanma (Lucifer'de biten koşu gelinen şeytana yazılır), en hızlı aklanma;
  - Lucifer'e ulaşma, Lucifer'i yenme, en az denemede yenme, Wild Bill kaçışları (satırlar isteğe bağlı, eski defter okunur).
- **Ayarlar** (`GameSettings`, PlayerPrefs `settings.*`): üç sekme, sekme butonları ya da **Q / E** (sol / sağ ok) ile döner, Esc menüye döner,
  satırlar 32 px (`SettingsView`, `SettingsPresenter.Tab`, `ISettingsCommands.NextTab / PreviousTab / IsSettingsOpen`):
  - **OYUN:** animasyon hızı, el rehberi, ipuçları, **dil** (`settings.language`, adıyla).
  - **GÖRÜNTÜ:** GÖRÜNTÜ MODU tam ekran (kenarlıksız) / pencere (Alt+Enter; `settings.display.mode` 0 / 1; eski `settings.fullscreen`
    taşınır ve hâlâ yazılır); PENCERE BOYUTU OTOMATİK (varsayılan, ekranın %85'ine sığan en büyük kat) ya da ×2'den ekrana sığan en büyük
    kata kadar (`WindowScales`: yükseklik %92 — başlık çubuğu ve görev çubuğu; sığmayan kat listede yok, kaydedilmiş ama sığmayan kat
    otomatik sayılır; `settings.display.scale`, 0 = otomatik, 1 / bozuk → 0); tam ekranda kilitli. PİKSEL ÖLÇEĞİ TAM PİKSEL (varsayılan) /
    EKRANI DOLDUR (`settings.display.fill`; `PixelScreen.FillScreen`: oran korunur, ölçek kesirli, pikseller hafif eşitsiz olabilir;
    tıklama alanları canvas ölçeğiyle gelir); pencerede kilitli. DİKEY SENKRON açık / kapalı (`settings.vsync`; kapalıyken
    `Application.targetFrameRate` = 60). `IDisplayMode` (`DisplayWidth / Height`, `Apply`) sadece görüntü ayarı değişince çağrılır;
    editörde 1920×1080 sayılır ve pencere boyutu değişmez.
  - **SES:** ANA SES (`settings.master`, 0-10, varsayılan 10, bozuk → 10) × MÜZİK / EFEKTLER (`IAudio.SetVolumes(master, music, sfx)`). Batchmode'da (testler) ayar / kayıt / rekor süreç boyu tek bir bellek deposunda
  (`HellPokerBootstrap.BatchStore`); PlayMode testleri her testte onu temizler.
- **Dil** (İngilizce varsayılan, Türkçe):
  - `Ui/Lang` (`Language` enum, `Lang.Current / Set / Changed / Pick(en, tr)`). Oyuncuya görünen **her** metin `UiText`'te
    `L("İngilizce", "Türkçe")` özelliği (const değil); şeytan replikleri her dil için ayrı `DealerText` (`MammonTr`...), hile metinleri
    `UiText.Cheats`'te. **Yeni metin her zaman iki dilde eklenir**; format string'lerde aynı {n}'ler (`LanguageTests` yansımayla
    hepsini gezer: boş Türkçe yok, {n} kümeleri aynı, her şeytanın her repliği iki dilde).
  - Anahtarlar ASLA çevrilmez: ipucu anahtarları (`tip.*`, const), kayıt alanları, şeytan / hile id'leri, PlayerPrefs anahtarları.
  - `CultureInfo.CurrentCulture` DEĞİŞMEZ. Büyük harf metinler doğrudan büyük yazılır; kod içinde büyük / küçük harf gerekirse
    `UiText.Upper / Lower` (Türkçe i→İ, ı→I). Core'un `out reason` metinleri teşhis içindir, oyuncuya gösterilmez (presenter UiText'ten söyler).
  - Terimler: ARTIR, GÖR, ÇEKİL, PAS, KART DEĞİŞ, HEP PAS (check to draw), ANTE; eller Yüksek Kart, Bir Çift, İki Çift, Üçlü, Kent, Renk,
    Full, Kare, Floş, Floş Royal, Ölü Adamın Eli. Lucifer Türkçede "SABAH YILDIZI". Şeytanların tonu korunur.
  - İlk açılış (kayıtlı dil yok): `Application.systemLanguage` Türkçe ise Türkçe, değilse İngilizce
    (`HellPokerBootstrap.FirstLanguageFor`); sonra kayıtlı seçim. Batchmode'da ilk dil hep İngilizce; EditMode testleri
    `EnglishByDefault` (SetUpFixture) ile İngilizce başlar, dili değiştiren test TearDown'da geri alır.
  - Değiştirme **sadece ana menüden**: menünün sağ üst köşesindeki küçük buton (gideceği dili gösterir: "TR" / "EN"), Ayarlar'daki
    DİL / LANGUAGE satırı (Ayarlar ana menüden açılır) ve **L** tuşu — L yalnızca menünün kök ekranında (`IMenuCommands.IsAtMenuRoot`:
    uyarı ya da kurallar açık değilken) çalışır; masada, onay kutularında, alt ekranlarda hiçbir şey yapmaz. Masada dil butonu yok
    (`ITableView` `ILanguageButton` değil). Hepsi `SettingsPresenter.CycleLanguage` → kayıt → `Lang.Changed`.
  - Güncelleme, sahne yeniden yüklenmez: sabit etiketler `UiFactory.Localized(() => UiText.X)` (`LocalizedText`; OnEnable'da ve
    `Lang.Changed`'da yeniden yazar). Dil masa görünmezken değişir; `TablePresenter.OnLanguageChanged` koşu sürüyorsa metinleri
    **sessizce** yeniler: şeytanın adı / unvanı (`IDealerView.Relabel`), ödeme tablosu, ceza satırı, mesaj, butonlar (`_relabelling`
    ile `Refresh`). `_relabelling` iken `PassThroughGate`, `SettleHand`, `SaveRun`, `Tip` ve `Say` çalışmaz: Lucifer kapısı yeniden
    sorulmaz, kayıt yazılmaz, şeytan konuşmaz. Ekrandaki son replik yeni dilde **animasyonsuz** yerine konur (`IDealerView.SetLine`:
    yazma yok, bakış yok; satır yoksa hiçbir şey). `MainMenuPresenter` şeytan kartlarını yeniden tarif eder, açık ekranı perdesiz yeniden
    gösterir; `SettingsPresenter` değerleri yazar. Şeytan konuşması tarif olarak tutulur (`Say(d => d.X, sayaç, ruh hali)`), böylece
    aynı replik yeni dilde bulunur.
  - Yeni görünümde sabit metin `Localized` ile kurulur; yeni dinamik metin presenter'ın yeniden yazdığı yoldan geçmeli.
  - **Şeytan adına ek gerekiyorsa `DealerText.Genitive`** (Türkçede elle: büyük harfli yerlerde `Genitive` "MAMMON'UN", "BELIAL'IN";
    cümle içinde `Called` "Lilith" / `CalledGenitive` "Lilith'in", "Sabah Yıldızı'nın"). Okuma: `UiText.GenitiveOf`, `NameInSentence`,
    `GenitiveInSentence`; İngilizcede alanlar boş, ad + "'s". Özne olarak geçen ad ek almaz ("Mammon haraç aldı").
    Değişebilen sayıya ek bağlanmaz ("%{3}'u" değil, "%{3} kadarı").
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
  - Gösterge koşuya aittir, masaya değil: masa değişimi, çağrılma ve düşüşte gösterge ve kin yeni masaya taşınır, **doluluk oranıyla,
    yukarı yuvarlanarak** (`CheatSession.Carry(malice, fromMax, toMax)` = ⌈malice × toMax / fromMax⌉; boş gösterge 0, dolu gösterge
    her masada dolu). Küçük göstergeli masaya gidip gelmek göstergeyi boşaltamaz (Mammon 3/4 → Belial 2/2 → Mammon 4/4).
    Lucifer'e çağrılırken gelinen şeytanın göstergesi ve kini saklanır, düşüşte o geri gelir (Lucifer'in 1'lik göstergesi değil).
    `cheat.major` masaya özgü, sıfırlanır. Saklanan gösterge kayda yazılmaz: Lucifer masasında kapatılıp açılan koşu düşüşte Lucifer'den taşır.
  - Vuran / engellenen hile göstergeyi boşaltır; boşa giden (hedef yok) ya da anı gelmeyen (oyuncu önce çekildi) dolu bırakır.
  - Değişmezler: showdown sonucunu sadece Düşüş değiştirir; **Dead Man's Hand kartları bağışık**; her hile görünür;
    eski kurallar (mühür, ruh, Lucifer) geçerli. Her vuruştan önce `ICheatGuard` sorulur (şimdilik `AllowEveryCheat`; ileride sınıf yetenekleri).
  - Hedefler (kart seçen hileler, `CheatTable.AdvisedDiscards` = kasa mantığının atacağı kartlar):
    - Rehin: atılacak kartların en yükseği (hazır elde en düşük kart). Diken: atılacak kartlardan biri (yoksa rastgele).
      Dikenli kart seçilince bedel önceden söylenir: uyarı "+X YEARS", buton "DRAW 1 / (+X YEARS)" (`IHellPokerGame.ThornCost`;
      ruhta sayısız "THORN BITES"); işaret iki kenarda kalın kırmızı dikenli sarmaşık. Diken yılları laneti geçirirse el orada
      biter (`RoundResult.ThornDamned`, "THE THORN BIT").
    - Yanan Kart: en iyi kombinasyonun (çift, üçlü..., kenta / renk ise her kart) en yüksek kartı, yoksa en yüksek kart; yerine desteden rastgele.
    - Gece Örtüsü **dağıtımda** vurur, sadece henüz açılmamış (3.-5.) bir karta: kart sırası gelince yüzü hiç görünmeden, örtüyle açılır.
    - Çatal Dil %80 nişanlı (önce renk / 4 aynı renk bozulur, yoksa eli düşüren, yoksa zararsız değişim), %20 "dil kayar"
      (`Dealer.BackfirePercent`): rastgele renk değişimi, oyuncuya renk verebilir.
    - Bakış: kaybedecek ele kasa %100, kazanacak ele %50 (`HellPokerGame.GazeBluffPercent`) re-raise yapar; re-raise kesin bilgi değildir.
  - **Geri tepme (backfire):** hile oyuncunun elini güçlendirirse (`CheatResult.Backfired`, `CheatSession` vuruş öncesi / sonrası eli
    karşılaştırır). Sadece şansa bırakan hileler geri tepebilir: Çatal Dil'in kayması, Yanan Kart, Düşüş. Diğer hiçbir hile oyuncuya
    yaramaz (Satın Al ve Yılan Takası yarayacak kartı seçmez; testle sabit). Masada: yeni kart hemen döner, üstünde "BACKFIRE",
    şeytan kızgın (angry) ve kendi `Backfire` repliğini söyler; sonuç satırı "It backfired!". Rekorlarda
    "Backfires seen" (`RecordBook`).
  - **Gizlilik:** oyuncudan gizlenen kart (`WasPlayerCardHidden`) showdown'da kasa beş kartını açana kadar hiçbir yoldan yüzünü göstermez
    (mühürlü elin kendi kendine açılışı, hile vuruşu dahil); gizli kart varken draw ipucu çerçevesi yok. Sonuçta önce kasa, sonra oyuncu döner.
  - Kayıt `v=3`: `malice`, `cheat.major`, `grudge`, yarım elde `hand.cheat / hand.cheat.done`; v1/v2 boş göstergeyle okunur.
  - Masada: portrenin altında pip göstergesi, üstünde niyet şeridi (ikon + ad, hover'da açıklama, H panelinde de), yalan "LIAR" diye
    kırılıp gerçeğe döner. Vuruşta kart 1-2 px sarsılır, şeytan reraise animasyonu + hile repliği; işaretler kartta kalır
    (zincir, diken, örtü, sahte yüz parıltısı). Sonuç mesajında hile satırı (ruhta yıl yok). Şeytan başına ilk hile ipucu.
    How to Play'de CHEATS sayfası.
- **Denge** (`BalanceSimulation`, şeytan × sınıf başına 2000 koşu, akıllı oyuncu; ruh, mühür, Lucifer, **hileler ve sınıflarla**;
  oyuncu sadece gördüğüyle oynar, niyete tepki verir; gösterge doluyken Köylü kötü elden çekilirken gücüyle bedava çekilir, Büyücü ilk
  küçük hile duyurusunda korumayı kurar (yalanın arkasını görür), Kral hile beklenirken draw'dan önce en değerli çift / kartı korur; **olaylarda** beklenen değer pozitifse kabul eder, emanet tekliflerini de; Kemik Zar'ı
  draw'da, çift ya da daha iyi bir elde eşleşmeyen en düşük karta atar). Aklanma (Lucifer'e ulaşan / ilk denemede yenme), olaylar ve emanetlerle
  (2026-10-05, sınıf güçleri tasarımcının şarj kuralıyla: kazanç +1, kayıp +2, çekilme +1, beraberlik 0, dolu 5; güç K ile açılır,
  kullanılana kadar açık kalır):
  | Sınıf | Mammon | Belial | Lilith |
  |---|---|---|---|
  | Köylü | %83.4 (%89 / %31) | %78.0 (%83 / %42) | %50.3 (%66 / %32) |
  | Büyücü | %84.9 (%89 / %36) | %77.1 (%82 / %45) | %55.3 (%67 / %39) |
  | Kral | %78.8 (%83 / %37) | %72.9 (%77 / %47) | %52.2 (%61 / %40) |
  | Soytarı | %74.2 (%87 / %42) | %77.3 (%86 / %50) | %54.1 (%72 / %43) |

  (0.1.6: Kral bütün eli korur → +1.4 / +0.7 / +5.1; Soytarı 750 yıl → +6.2 / +7.1 / +7.1, Köylü'ye göre −8.0 / −0.9 / +4.4.)

  (2026-10-06: sayılan deste + Soytarı'nın Çıngırağı ile; simülasyon oyuncusu kart saymaz, hiç KARIŞTIR'a basmaz.) Taze deste
  (`HELLPOKER_FRESH_DECK=1`): Köylü 82.8 / 78.4 / 52.4, Büyücü 84.9 / 77.9 / 53.9, Kral 77.5 / 71.0 / 49.3, Soytarı aynı (her el karılır)
  — fark −2.2..+1.4, 2000 koşunun gürültüsü içinde. Çıngırak çok nadir: Soytarı koşularının %0.7 / %0 / %0.2'sinde (20 jokere çıkmak zor).

  Soytarı (2026-10-06, kurallar kullanıcının, ayarlanmadı): Köylü'ye göre −11.5 / −9.0 / −3.8 (±3 sınırının dışında). Koşular kısa ve
  oynak (ort. el ~24 / ~13 / ~14), jokerle Ölü Adamın Eli sık (Mammon'da 2000 koşuda 29 kez). Koşu sonunda joker ort. 9.0 / 6.6 / 7.3, en
  çok 24; showdown'ların %14 / %10 / %13'ü oyuncunun iki jokeriyle kaybedildi, %16 / %9 / %13'ü şeytanın iki jokeriyle kazanıldı.
  Denemeler (`HELLPOKER_JESTER=`başlangıç,joker,sınır`): sınır 4 → 74.2 / 71.5 / 48.8; başlangıç 750 → 76.7 / 77.7 / 57.6 (ortalamada
  Köylü'ye en yakın). Öneri (uygulanmadı, kullanıcı seçecek): başlangıç cezası 750.

  Önceki (masa başına haklar) tabloya göre: Köylü +3.3 / +1.1 / +0.8, Büyücü +3.5 / −0.3 / +0.3, Kral +0.2 / +0.1 / +0.2. Koşu başına
  güç kullanımı Köylü 3.8, Büyücü 5.7 (vuran koruma 5.7), Kral 5.5. Kullanıcı kararıyla telafi yok: eşikler, sınıf güçleri, başlangıç
  cezaları aynı (Mammon'da Büyücü / Köylü hedefin ~3 üstünde; oyun testinden sonra bakılacak).
  Emanetsiz (`HELLPOKER_RELICS=none`: emanet teklifi yok): Köylü 80.6 / 77.6 / 53.0, Büyücü 82.3 / 78.9 / 55.9, Kral 77.7 / 72.9 / 49.9
  — emanetler −2.1..0.0 (sınır ±3; oyuncu teklifleri ~%85 alıyor).
  **Masa zıplayan oyuncu** (`HELLPOKER_HOP=1`: bir masa başına hakkı harcayınca başka bir sıradan şeytana geçip hemen döner):
  yeni kuralla normal oyuncuya göre −1.8..+1.5 (Kemik Zar zorlanmış Köylü, 3 tohum ortalaması: −1.2 / +0.2 / +0.7; `HELLPOKER_SEED`
  tohumu kaydırır). Eski kuralla (`HELLPOKER_HOP_OLD=1`: her oturuş doldurur) Büyücü +5.4 / +0.2 / +6.1, Kemik Zar Köylü +5.1 / +4.2 / +10.0. Olaysız (`HELLPOKER_EVENTS=0`): Köylü 79.3 / 76.0 / 52.3, Büyücü 81.1 / 77.4 / 55.1, Kral 76.3 / 71.6 / 49.2.
  Tek emanet koşu başından (`HELLPOKER_RELICS=<id>`, Köylü, Paslı Taç %110 / Sikke %75 iken): Kemik Zar el başına +11.7 / +11.1 / +21.8, sadece kartlar açılırken +11.3 / +9.2 / +15.7, **masa başına 1** +1.7 / +2.6 / +3.9,
  Paslı Taç +2.6 / +3.6 / +3.0, Kayıkçı Sikkesi +4.7 / +4.1 / +3.3, Dikenli Tespih −8.5 / −6.4 / −11.5. Oyuncu iki teklifi de neredeyse
  hep kabul ediyor (beklenen değer = kalan emanetlerin ortalaması > 0).

  Ort. el (Köylü) ~57 / ~28 / ~32; hile / el 0.30 / 0.47 / 0.40, Lucifer 0.83. Hedefler: Mammon ~80, Belial ~70, Lilith ~55;
  Kral Lilith'te ~50 (taç %25 ile; %50'de 54.8, %100'de 64.2). Büyücü Belial'de 2 kasa kartı görür (kullanıcı kararı,
  seçenek a): simülasyonda +1.4 (oyuncu modeli açık kartı az kullanıyor; gerçek oyuncuya daha çok yarar). `HELLPOKER_CLASSES`,
  `HELLPOKER_KING` ("başlangıç,taç%"), `HELLPOKER_CHARGE` ("dolu,kazanç,kayıp,çekilme"), `HELLPOKER_JESTER`
  ("başlangıç,joker,kayıp sınırı") ile denenebilir. Simülasyonun zaman aşımı 1 saat (dört sınıfla 3 dakikayı geçiyor).

  (Gösterge masa değişiminde taşınıyor, tohumlar türetiliyor — 2026-10-03.) Lucifer masasında el başına 0.84 hile; geri tepme: Yanan Kart %21, Düşüş %18, Çatal Dil ~%0.04 (kayma nadiren renk verir).
  Belial her el hile yapsa bile ~%75'in altına inmiyor (hileleri hafif); son karar oyun testinden sonra — bkz. DEVLOG.
  Ruhu masaya koyan koşular %30 / %38 / %62. `HELLPOKER_LUCIFER_GATE` (0 = Lucifer yok) ve `HELLPOKER_CAST_DOWN` ile denenebilir. Ruh değerini düşürmek el sayısını neredeyse değiştirmiyor
  (bahis birimi ruhla birlikte küçülüyor), sadece Lilith'i zorlaştırıyor — bkz. DEVLOG.
- **Sınıflar** (günahkârlar, `Core/Sinners`): New Game → şeytan → **sınıf seçimi** (`SinnerSelectView`: portre, ad, unvan, ne yaptığı,
  bedeli, başlangıç cezası; yetenek ve bedel metni içeriğe göre yerleşir — bedel yeteneğin gerçek yüksekliğinin altından başlar, ikisi
  de SEÇ'in üstünde biter, PlayMode testi iki dilde ölçer; kart genişliği sınıf sayısına göre `CardWidthFor`: 4 sınıfta 112 px, hepsi
  480 px'e sığar) → masa. Sınıf bütün koşu boyunca kalır (masa değişimi, Lucifer, Continue).
  - `SinnerClass` (Id, `StartingYears`, `WinAntePercent`, `Ability` = güç, `SeesLies`, `HouseCardsShownAt`); her sınıf kendi dosyasında
    (`Peasant.cs`, `Warlock.cs`, `King.cs`), `SinnerRoster.All` listesinde. **Yeni sınıf = yeni dosya + roster'a bir satır**
    (+ UiText.Sinners metinleri, `pixel_sinners.py` portre ve ikon, şeytanlara `GreetingAs...` repliği; gerekirse `HellPokerGame.WhyNoPower / UsePower`).
  - **Güç ve şarj göstergesi** (`Sinner` + `ChargeRules`): pasifler hep açık, aktif güç göstergeye bağlı. Gösterge **koşuya ait** (masa
    değişimi, Lucifer'e çağrılma ve düşüş aynı göstergeyle sürer), koşu 0 ile başlar. **Kazanç +1, kayıp +2, çekilme +1, beraberlik 0, en fazla 5**
    (`ChargeRules.Default`; metinler oradan okur). Yarım bırakılan el çekilme, mühürlüyse kayıp sayılır; Köylü'nün güçle bedava bıraktığı el
    şarj vermez. `HELLPOKER_CHARGE="dolu,kazanç,kayıp,çekilme"` ile denenebilir. `HellPokerGame.WhyNoPower()` → `PowerRefusal`.
  - **Açık kalan güç** (`Sinner.PowerArmed`; `ArmPower` / `DisarmPower` / `PowerArmed`): K ya da rozet gücü **açar**, şarj henüz
    harcanmaz; mod bahislerde, açılan kartlarda, draw ekranında **açık kalır** (`Refresh` artık hiçbir modu sıfırlamaz). Kapanır: güç
    kullanılınca (şarj 0), K / rozete yeniden basınca (`PowerOffMessage`, şarj dolu). Artık sadece Köylü'nün gücü açık kalır (Kral'ın
    seçim modu 2026-10-06'da kalktı). Masa değişimi Kemik Zar'ın seçimini kapatır. Kayda yazılmaz (el ortasında kapatma eli zaten forfeit eder).
  - **Köylü:** 1000 yıl. Güç "Dürüst kalp": K açar (eller arasında da), ÇEKİL "FREE / FOLD" / "BEDAVA / ÇEKİL" olur; açıkken yapılan ilk
    çekilme (yeniden artırmaya cevap dahil) bedelsizdir ve gösterge o anda 0 olur. Çekilmezse sonraki ellerde de açık kalır. Mühürlü elde
    ÇEKİL yok. Otomatik bedava çekilme yok.
  - **Büyücü:** 1000 yıl. Pasif: yalanları görür (niyet şeridi duyuruda "LIAR" diye kırılır), kasanın 2'den az kart gösterdiği sıradan
    masada 2 kart görür — pratikte Belial (`HouseCardsShownAt`; Lucifer'in karanlığı kalır). Güç "Koruma": duyurulan **küçük** hile
    varken K korumayı hemen kurar (`Sinner.WardRaised`) ve **şarj o anda harcanır**; koruma hile vurmaya gelince reddeder
    (`CheatOutcome.Blocked`, şeytanın göstergesi boşalır, kartta "WARD"/"KORUMA", şeytan kızgın). Hile hiç vurmazsa (çekildi, boşa gitti)
    şarj iade edilmez, koruma bir sonraki küçük hileyi bekler (eller ve masalar boyunca). Büyük hilelere ve Düşüş'e kurulamaz.
  - **Kral:** 1250 yıl (1500 olsa Lilith'te ruh hemen masada olurdu). Pasif: taç, kazanç ante'nin %25'i kadar fazla siler
    (`King.crownPercent`). Güç **Taç**: sadece şeytan bir hile duyurmuşken ve o hile henüz vurmamışken (`PendingCheatTruth != null`;
    yoksa `PowerNoCheatToGuard`), K anında **bütün eli** o el boyunca korur (`Sinner.HandProtected`, şarj 0; seçim yok). Oyuncunun
    kartına dokunan her hile (büyükler, The Fall dahil) reddedilir (`Sinner.Allows` → `CheatOutcome.Blocked`, gösterge boşalır,
    `TableMoment.Ward` + `CrownFlash` `TAÇ`, şeytan kızgın `Blocked`). Sadece şeytan tarafına dokunan hileler (Sahte Yüz, Haraç, Bakış;
    `CheatRules.TouchesPlayerCards`) durmaz. Korunan kartlar normal atılır / değiştirilir (Kemik Zar dahil), yeni gelen kart da
    korunur, kartlarda taç işareti (`CardMark.Protected`). El bitince kalkar (`Sinner.EndHand`); hile hiç gelmezse şarj iade edilmez.
  - **Görünürlük** (güç açıkken ya da koruma kalkmışken): rozet 1 sn nabızla parlar, üstünde "POWER ON" / "GÜÇ AÇIK" (`SinnerBadge.Armed /
    PowerOn`); kullanılabilir ama kapalıyken "READY: K" / "HAZIR: K". Mesajın altındaki satır (kazanç / kayıp satırının yerinde) güç açıkken
    ne yapılacağını söyler (`ITableView.SetPower(PowerDisplay)`: "Güç açık: sıradaki ÇEKİL bedava
    (K: kapat)", Kemik Zar'ın satırı). Seçilebilir kartlar yanıp sönen 2 px altın çerçeveli, diğerleri soluk (`IHandView.SetPicking`,
    `CardView.PickState`). Büyücü'nün koruması kalkınca niyet şeridinin altında 9×10 piksel kalkan (`MaliceView.SetWard`), vurunca gider.
    Kemik Zar'ın kutusu seçim boyunca nabızla parlar (`RelicBadge.Selecting`).
  - Masada portre kutusunun sağ altında rozet (`SinnerBadgeView`, 44 px: ikon + 5 pip; dolunca pipler altın / kor arasında parlar; hover'da
    ad, ne yaptığı, gösterge ve kuralı). **K** tuşu ve rozete tık her sınıf için "gücü aç / kapat" (`ITableCommands.UsePower`). Gösterge
    ilk dolduğunda şeytan ipucu söyler (`tip.power`).
  - **Soytarı** (`Jester`, id `jester`): **750 yıl** (2026-10-06, kullanıcı kararı; simülasyon önerisi), gücü ve şarjı yok (`SinnerAbility.None`; K / rozet sadece `JesterPowerInfo` der).
    Onun özelliği **jokerli deste**: koşu boyunca her masada (Lucifer dahil) destede `Sinner.Jokers` kadar joker (başta 2 =
    `SinnerClass.StartingJokers`; kazanılan el +1, sonraki elden itibaren; sayı `JokerLossLine` (10) üstündeyken kaybedilen el -1, 2'nin altına
    inmez; çekilme / beraberlik değiştirmez; yarıda bırakılan mühürlü el kayıp sayılır; üst sınır yok). Diğer sınıflarda deste 52, joker yok.
    - Kart: `Card.Joker(seri)` (`IsJoker`, `JokerSerial`; Rank 0, Suit yok; seri numarasıyla ayrışır, çok joker aynı destede olabilir).
      52 kartın eşitliği, hash'i, metni aynı. Deste `IJokerDeck.SetJokers` (`Deck`; jokerler listenin dibine, yığılmış deste sırasını korur);
      `HellPokerGame.PlaceBet` her elde `Sinner.Jokers`'ı uygular.
    - Değerlendirme: `JokerResolver` (Core/Evaluation) jokeri arama yapmadan, kategori kategori en iyi karta çevirir (sahibinin elindeki
      karta asla; rakibinkine olabilir; beşli yok; jokerle Ölü Adamın Eli de olur). `WildJokerEvaluator` oyunda her yargının önünde
      (`HellPokerGame` kurucuda sarar): yapay zeka, el rehberi (`VisibleHandReader` jokeri en büyük gruba katar), hileler, Bakış.
      Testte her kart denenerek doğrulanır (1 ve 2 joker).
    - Showdown: tek jokeri olan taraf onu bir karta çevirir. Oyuncununki için `GamePhase.NamingJoker`: bütün kartlar döner, presenter
      seçiciyi açar (`JokerChoices`, `BestJokerCard`, `EvaluateJokerAs`, `CanNameJoker`, `NameJoker`; daha kötü kart seçilirse o sayılır).
      Kasa en iyisini seçer. **2+ joker = o taraf kaybeder** (`ShowdownResult.Judge(..., oyuncuJoker, kasaJoker)`: `PlayerBust` / `HouseBust`);
      **ikisinde de 2+ varsa jokeri az olan kazanır, eşitse berabere** (`BothBust`, `PlayerJokers` / `HouseJokers`); bozuk elle
      kazanan en zayıf el gibi ödenir (`WinnerBust` → Yüksek Kart'ın kazancı / kaybı; tek taraf battığında kazananın kendi eli ödenir).
      Seçici sadece jokerin kartı sonucu (kazanç / kayıp / beraberlik) değiştirebiliyorsa açılır (`JokerCanChangeTheOutcome`: bütün
      adaylar denenir); değilse joker en iyi karta döner. Mesajlar `JokerDuelWin / Loss / Push`; kahkaha anı ve `JokerBust` sadece
      şeytan kendi jokerleriyle kaybettiğinde; günlükte `JOKERS ON BOTH SIDES`.
      **Jokerle kesinleşen el** (`HellPokerGame.JokerOutcomeIsSettled`): draw'dan sonra sayılar kesin, en az bir tarafta 2+ joker varsa el
      draw biter bitmez (DrawReveal'e geçmeden) biter; draw'dan önce oyuncunun ulaşabileceği her sayı (zincirli jokerler kalır, en çok
      MaxDiscards atılır, destedeki jokerler gelebilir) ile şeytanınki (en çok 1 tutar, sonra çekebilir) aynı sonucu veriyorsa biter.
      Bitiş showdown gibi (`Finish(JudgeShowdown(null))`, o anki potla, çekilme değil); `RoundResult.SettledByJokers`, mesaj
      `Settled{Win,Loss,Push}Format` (sayılarla), günlükte `SETTLED BY JOKERS`. Kontrol `SkipEmptyDecisions`'ta ve draw'da. Kasanın bahsi temkinli kalır (2+ jokerle re-raise yok);
      kontrol showdown'da, draw'da fazlasını atan kurtulur, çekilen el kurala girmez. El ortasında kapatma `NamingJoker`'ı da forfeit eder.
    - Kasa yapay zekası (`HouseDrawStrategy`) tek jokeri tutar, fazlasını önce atar, jokeri en iyi kart sayarak plan yapar; 2 jokerle re-raise etmez.
    - Hileler jokere dokunmaz (`CheatRules.IsImmune`: joker de bağışık; `CheatTable` desteden `sıradaki kartı` verirken jokerleri atlar,
      Sahte Yüz joker göstermez). The Fall dahil.
    - Masa: joker kartı `Ui/card_joker` (soytarı yüzü), dönüşünce `Ui/joker_sparkle` (6 kare) ve köşede soytarı külahı (`CardMark.Joker`,
      `card_marks` 6. kare). Elde 2 joker varken mesaj altı satırında kırmızı `TwoJokersWarning` (`PowerDisplay.Warning`); el rehberi
      `JOKER İLE: ÜÇLÜ`. Seçici (`JokerPickerView`, bet satırında: değer / renk okları, kart, SEÇ; oklar `ITableCommands.StepJoker`,
      Enter / Space onay) + canlı satır `Bu kartla elin: ÜÇLÜ (en iyisi)`. Şeytanın iki jokeri: `TableMoment.JokerLaugh` (`HA! HA! HA!`),
      şeytan kızgın `JokerBust` repliği. Rozet pip yerine `×N` (değişince parlar), hover'da kural.
    - **20 joker** (`SinnerClass.JokerJackpot`): kazanılan elin sonunda sayı 20'ye ulaşınca jokerler silinir, sayı 2'ye döner
      (`Sinner.HitJokerJackpot`, `RoundResult.JokerJackpot`) ve koşuda bir kez **Soytarı'nın Çıngırağı** emaneti verilir (`RattleGiven`):
      `TableMoment.JokerLaugh` (`YİRMİ JOKER!`), şeytan şaşkın `JokerJackpot` repliği, sonuç satırında emanet, emanet kutusu parlar.
    - Her şeytanın `GreetingAsJester`, `JokerBust` ve `JokerJackpot` replikleri; koşu günlüğü: joker gelişi (kimde, dağıtım / draw), adlandırılan kart, kasanın
      kartı, iki joker kaybı, sayaç değişimi.
  - Kayıt: `class`, `class.charge` (0–5, isteğe bağlı; yoksa 0), `class.ward` (kurulu koruma, isteğe bağlı), `class.jokers` (Soytarı;
    yoksa / bozuksa / 2'nin altıysa başlangıç). Eski `class.charges` /
    `class.charges.tables` okunmaz, yok sayılır. Rekorlarda sınıf başına aklanma (`free.class.<id>`). How to Play'de SINNERS sayfası.
  - **Masa başına haklar** (artık sadece Kemik Zar; `Core/Game/TableCharges`): şeytan başına tutulur, harcanan hak o masaya dönünce dolu
    gelmez; Lucifer'in masası her çağrılmada dolu. `TablePresenter.SeatAt` her oturuşta `SitAt(dealer.Id, fresh: Summoned)`.
- **Olaylar** (`Core/Events`, eller arası): her el arasında bir kez zar (`EventSession.Roll`), `GameRules.EventChancePercent` (%12),
  son olaydan en az `EventCooldownHands` (4) el sonra, aynı olay koşuda bir kez, sadece Betting'de, **Lucifer masasında yok**.
  Kendi zar akışı (`HellPokerGameFactory.EventStream` = 3, koşu başına; diğer akışlar kaymaz). Batchmode'da (testler) kapalı.
  - `IHellEvent` (Id, `OwnerId`, `CanAppear`, `Options` (son seçenek hep "pass"), `Apply`, `ExpectedYears` — simülasyon oyuncusu için),
    `EventDeck.Standard`, `EventSession` (soğuma, görülenler). Etkiler `IEventTable`'dan (oyun uygular): `ForgiveYears` (olay cezayı
    bitirmez, son yıl kalır), `AddYears`, `EmptyMalice`, `RunEffects` (koşuya ait, her masanın oyunu paylaşır: `UseEffects`):
    `NextHand` (`HandModifier`: ante %, ante birimi, kazanç %, kayıp %, tavan yok, kasa kart sayısı, kazanınca ceza = X, hayalet el),
    ertelenmiş ceza (`Defer`, `HandSettled` → `IHellPokerGame.DeferredPaid`), satılan ruh (`SoulSold` → `DamnationYears` ve kalan ruh küçülür).
  - Olaylar: **Kayıkçı** (sonraki el ante %50, kazanç %50); **Ruh Simsarı** (ruh masadayken, kalan ruh yetiyorsa: ruhun 1/4'ü
    karşılığında 300 yıl); **Kayıp Ruh** (sonraki el oyuncunun eli İki Çift–Üçlü hazır el, ama kayıp **×3** — ×2'de aklanmayı ~4 puan
    artırıyordu); **Şeytanın Defteri** (Mammon: şimdi −200, 5 el sonra +300; Belial: kasa hiç kart açmaz, kazanç ×2; Lilith: gösterge
    boşalır, +100); **Yanan Köprü** (ceza ≥ 2500: tavan yok, ante 3 birim, kazanırsan ceza 1000).
  - Sunum: masanın ortasında `EventPanelView` (sahibin 48×48 portresi — yabancılar `Art/Events/<id>.png`, `pixel_events.py`; şeytanın
    teklifi şeytanın kendisi —, ad, başlık, metin, KABUL ET / GEÇ; iki siyah perde ortadan açılır). Açıkken masa başka girdi almaz,
    Esc = Geç. Şeytan `EventAccepted` / `EventDeclined` repliğiyle tepki verir. Ruh masadayken metinlerde yıl sayısı yok.
    Ertelenmiş ceza gelince mesaj. Olay gösterildiği an "görüldü" kaydedilir: açıkken kapatılırsa açılışta geçilmiş sayılır.
  - **Yeni olay:** `IHellEvent` sınıfı + `EventDeck.Standard`'a bir satır + `UiText.Events` (başlık, metin, ruh metni, sahip adı) +
    yabancıysa `pixel_events.py`'ye portre. Gerekirse `HandModifier`'a yeni bir alan (Encode/Decode ile).
- **Emanetler** (lanetli eşyalar, `Core/Relics`): olay ödülü — **Mezar Soyguncusu** ve **Lanetli Sandık** (`RelicEvent`, KABUL ET:
  zarla henüz taşınmayan bir emanet; koşuda en fazla `RelicRoster.MaxCarried` = **2**, ikisi taşınırken ya da hepsi alınmışken çıkmaz).
  Her emanet bir lütuf + bir lanet, koşu boyunca her masada her ele etki eder.
  - `IRelic` (Id, `Effects`, `ExpectedYears` — simülasyon oyuncusu için), `RelicEffects` (ante %, kazanç %, kasa kart sayısı ±, re-raise
    +birim, her el +kötülük, şeytan başına yeniden çekme, ruh kaybı %), `RelicRoster.All` / `Find` / `Combined` (iki emanet: yüzdeler çarpılır,
    sayılar toplanır, düşük ruh kaybı geçer). Olayın `HandModifier`'ıyla aynı kancalar; `HellPokerGame.Relic` dağıtımda sabitlenir.
  - **Kemik Zar:** **her şeytanın masasında bir kez** (Lucifer'e her çağrılmada dolar; `RunEffects.RedrawsLeft` / `SitAt`, **Masa başına
    haklar** kuralı; kayıtta `relics.redraws` + `relics.redraws.tables`, eski kayıtta dolu), draw'dan önce görünen bir kartı geri at, desteden yenisi (`CanRedraw` / `Redraw`; zincirli, dikenli,
    korunan, gizli kart olmaz) / kasa re-raise'i **2 birim**. **Paslı Taç:** kazanç %105 / gösterge her el +1. **Kayıkçı Sikkesi:**
    ante %80 / kasa bir kart eksik gösterir. **Dikenli Tespih:** ruh elinde kayıp ×1.25 (×1.5 yerine) / kazanç %90.
    **Soytarı'nın Çıngırağı** (`JestersRattle`, ödül: `IRelic.IsReward`): olaylarda çıkmaz (`RelicRoster.Offered`), 2 emanet sınırına
    sayılmaz (`RunEffects.CarriedOffered`; kayıtta `relics` içinde, bozuk kayıtta fazla teklif emaneti atılır ama o kalır). Kazanılan el
    ante'nin %50'si kadar fazla siler (`RelicEffects.WinAntePercent`), kaybedilen el %50'si kadar fazla ekler (`LossAntePercent`).
    Emanet çubuğunda ayrı kutu (ilk kutunun solunda, altın çerçeve); yeni gelen emanet kutusu bir an parlar.
    (Taç %110 / Sikke %75'te Büyücü-Lilith +3.4 çıkıyordu; sayılar dengeyle ayarlandı.)
  - **Kazanç yüzdeleri** (olayın `WinPercent`'i × emanetlerinki) **sınırlanmamış** kazanca uygulanır, ceza sınırı en son konur
    (`HellPokerGame.Forgiven`): önce sınırlayıp sonra kesmek son yılları asla 0'a indirmiyordu (Tespih'le Lucifer yenilemiyordu).
    "Win: at least" satırı (`LeastYearsForgiven`) ve DEAL'daki ante (`UpcomingAnte`) da olayın / emanetlerin yüzdelerini bilir.
  - Kayıt: `RunEffects.Relics` → v=4 kayıtta `relics` (virgüllü; bilinmeyen / fazla / tekrar eden temizlenir; eski kayıtta yok) ve
    `relics.redraws` (bu masada kalan zar hakkı; isteğe bağlı, yoksa dolu), `relics.redraws.tables` (şeytan başına; isteğe bağlı).
  - Masada portre kutusunun sağ kenarında (niyet şeridinin altında) 20×20 kutular (`RelicBarView`: `Ui/relic_icons.png` 16×16,
    `pixel_relics.py`; Kemik Zar'da bu masada kalan hak; hover'da ad + lütuf + lanet). Kemik Zar'a tık → kart seç (tekrar tık: vazgeç).
    Alınınca mesaj: "Artık X sende. Lütuf. Ama: lanet".
  - **Yeni emanet:** `IRelic` sınıfı + `RelicIds` + `RelicRoster.All`'a bir satır (sıra = ikon şeridi sırası) + `UiText.Relics`
    (ad, lütuf, lanet) + `pixel_relics.py`'de ikon. Gerekirse `RelicEffects`'e yeni alan ve `HellPokerGame`'de kancası.
- **Ses** (16-bit chiptune, kodla üretilir: `Tools/AudioGen` — `synth.py` (kare / üçgen dalga, gürültü, zarf, döngü süzgeci),
  `sounds.py`, `generate_audio.py [sfx] [music]` → `Assets/Resources/Audio/Sfx|Music/<id>.wav`, 22 kHz mono). Elle düzenlenmez.
  - Efektler (`SfxIds`): deal, flip, chip, win_small, win_big, loss, sealed (gong), cheat, backfire, soul, summoned, fall, click,
    transition. Müzik döngüleri (30-60 sn, dikişsiz): mammon (ağır, metalik), belial (kabare / swing), lilith (yavaş, minör), lucifer
    (org), menu; `soul_layer` (uğultu + kalp atışı) ruh masadayken müziğin üstünde.
  - `IAudio` (PlaySfx, PlayMusic, SetSoulLayer, CutLong, SetVolumes); `UnityAudio` (8 sesli efekt havuzu + müzik + ruh katmanı),
    `NullAudio` (batchmode / testler: ses yok). Efektler presenter'dan **masanın kuyruğuyla** (`ITableView.PlaySfx`): animasyonuyla birlikte
    çalar. Müziği `MainMenuPresenter` ekrana göre seçer (menü teması / masadaki şeytanınki); çağrılma ve düşüşte `TablePresenter`.
    Atlama (`SkipAnimations` / Hurry) uzun efektleri keser (`CutLong`). Hız ayarı sesi hızlandırmaz. Her buton tıklaması `UiFactory.ButtonClicked`.
  - İçe aktarma (`HellPokerAudioImporter`): mono, Vorbis; efektler DecompressOnLoad (kalite 0.7), müzik Streaming (0.5).
  - Ayarlar: ANA SES (varsayılan 10) × MÜZİK / EFEKTLER (0-10, varsayılan 7, 10'dan sonra KAPALI; `settings.music`, `settings.sfx`, bozuk → 7);
    ayarlar ekranının SES sekmesinde.
- **Dead Man's Hand** (A♠ A♣ 8♠ 8♣ + herhangi bir 5. kart) **yenilmez**: Royal Flush dahil her eli yener (testlerle sabit)
  ve oyuncu kazanırsa **tüm cezayı siler**. Kasa onunla kazanırsa en yüksek çarpan sayılır.
- Standart çarpanlar: High Card/Pair ×1, Two Pair ×2, Trips ×3, Straight ×4, Flush ×5, Full House ×8, Quads ×10, Straight Flush ×15, Royal ×20.
- Tüm sayılar ayarlanabilir: `GameRules` (+ `StakeScale`), `Dealer`, `PayoutTable`, `HouseBettingStyle`; Inspector'da `HellPokerBootstrap`.

## Mimari

```
Assets/Scripts/
  Core/          HellPoker.Core.asmdef  — noEngineReferences: true (UnityEngine KULLANILAMAZ)
    Cards/         Card (+ joker), Rank, Suit, Hand (değişmez), IDeck/Deck, IJokerDeck
    Randomness/    IRandomSource, IShuffler, FisherYatesShuffler, RandomSeeds (ana tohum + akış başına türetme)
    Evaluation/    HandEvaluator + Rules/ (her el türü bir IHandRule), VisibleHandReader (açık kartların şu anki eli),
                   JokerResolver + WildJokerEvaluator (jokerin en iyi kartı)
    Draw/          ICardExchanger, IDiscardPolicy, IDrawStrategy (HouseDrawStrategy = kasa AI)
    Game/          IHellPokerGame/HellPokerGame (tur akışı), GameRules, PayoutTable, PunishmentLedger, HellPokerGameFactory,
                   RunStats / RunSnapshot (kayıt formatı) / HandInProgress (yarım el) / RecordBook (rekorlar),
                   LuciferGate (çağrılma / düşüş / deneme), TableCharges (masa başına hakların şeytan başına defteri), RunLog (koşu günlüğü),
                   PowerRefusal (sınıf gücü neden kullanılamaz)
    Betting/       IHouseBettingStrategy, HandStrengthBettingStrategy, HouseBettingStyle (kasanın re-raise / blöf mizacı)
    Dealers/       Dealer (şeytanın ev kuralları paketi: MaxDiscards, HouseCardsShown, PayoutTable, HouseBettingStyle, SoulThreshold,
                   MaliceMax, ICheatPolicy Cheats), DealerRoster (hile listeleri ve gösterge boyları burada)
                   (Game/ altında ayrıca StakeScale: bahis birimi, ante, masa tavanı)
    Sinners/       SinnerClass + Sinner (guard, şarj, jokerler), Peasant, Warlock, King, Jester, SinnerRoster
    Events/        IHellEvent + IEventTable + EventOptions, Events.cs (5 olay + 2 emanet teklifi + EventDeck), EventSession, HandModifier + RunEffects
    Relics/        Relics.cs: IRelic (+ Boon: lanetsiz hali), RelicEffects, RelicIds, BoneDie / RustyCrown / FerrymansCoin / ThornedRosary, RelicRoster
    Chapters/      Phase 2 (tasarım: Docs/PHASE2_CHAPTERS.md; sunum yok): ChapterRules, ChapterMap, FloorTable (coin'le kat maçı: HellPokerGame
                   1 000 000'luk keseyle), FloorPayoutTable (×3), CoinPurse, BlackMarket, FloorEvents, ChapterRun (yol, ateş, kapıdaki haraç).
                   FirstCheatBreaker (Ateş). Demo'ya kancalar (varsayılan kapalı): IHouseFoldStrategy, HellPokerGame.HouseCardsOpenAtDeal,
                   RunEffects.SilenceCurse / CombinedRelics, Sinner.ChangeJokers.
                   Simülasyon: Tests/EditMode/ChapterSimulation (Explicit, -testFilter HellPoker.Core.Tests.ChapterSimulation)
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
                   SettingsView, EndScreenView, RecordsView, SinnerBadgeView / SinnerSelectView, EventPanelView, RelicBarView,
                   ScreenTransitionView, AnimationSequencer (Complete = atla; hata veren adım kuyruğu kilitlemez)
    Settings/      GameSettings (+ IGuideSettings, WindowMode), WindowScales (pencere katları, saf aritmetik), ISettingsStore
                   (PlayerPrefsStore / MemoryStore), RunArchive (kayıt + rekorlar), RunLogSink (IRunLogSink: dosya / bellek)
    Ui/            UiFactory, PixelScreen (480×270, tam sayı ölçek), UiArt (Resources'tan sprite/font/metin), Palette, UiText + UiText.Dealers
                   + UiText.Cheats (iki dilli), Lang (dil), LocalizedText (`Localized()`: dille değişen sabit etiket), PixelOutline (`WithOutline()`: 8 yönlü 1 px siyah dış çizgi),
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
                        JesterJourneyTests (Soytarı seçilir, jokerli el, seçici, adlandırılan kart), JesterScreenshots ([Explicit], 60–66),
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
                               cheat_icons (16×16, CheatIds sırası), relic_icons (16×16, RelicRoster.All sırası), malice_pips (8×8), card_marks (32×48 kart üstü işaretler; 6. kare joker),
                               card_joker (32×48), joker_sparkle (6 × 32×48), sinner_icons (16×16, SinnerRoster.All sırası))
Assets/Resources/Fonts/ HellPokerPixelTitle (Press Start 2P), HellPokerPixel (Tiny5) — OFL lisansları yanında; eksik glifler piksel olarak eklendi
Tools/AudioGen/         synth.py, sounds.py, generate_audio.py (bütün sesler kodla)
Tools/ArtGen/           pixel.py (palet + çizim), pixel_layers.py (katmanlı zemin: Layer, Particles, döngü kontrolü, yazı bölgeleri),
                        pixel_demons.py, pixel_salons.py, pixel_ui.py, pixel_menu.py, pixel_sinners.py, pixel_events.py, pixel_relics.py, fonts.py, generate_art.py,
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
- **Keskin yazı** (`Ui/PixelText`): `UiFactory.CreateText` her metne `PixelSnappedText` (mesh düzenleyici: her köşe tam ekran
  pikseline yuvarlanır — tek ölçeklerde ×3 / ×5 yarım oyun pikseli yarım ekran pikseli olurdu) ekler ve font atlasını Point filtreye
  alır (`Font.textureRebuilt`'te yeniden). EKRANI DOLDUR'da (2K'da ×5.33) font pikselleri eşitsiz olur: ayarın ipucu söyler.
  Windows oyuncusu DPI-aware (Player Settings'te ayar yok; fiziksel piksellerle ölçüldü). Ekran görüntüsü boyutu
  `HELLPOKER_SHOT_SIZE=2560x1440`, dolgu `HELLPOKER_FILL=1`.
- **Fontlar:** iki font da 8 px ızgarada; boyut her zaman 8'in katı. `UiFactory.CreateText`: `Bold` = başlık fontu
  (Press Start 2P), diğerleri metin fontu (Tiny5). Fontta olmayan karakter kullanma (ör. "→"); gerekirse `fonts.py`'ye piksel glif ekle. Başlık fontunun Türkçe "İ"si `fonts.py`'de elle (`TITLE_DOTTED_I`): tam boy I, noktası 1 px boşlukla em'in üstünde (satır yüksekliği aynı).
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
  Fabrika tek ana tohumdan (`seed` ya da `RandomSeeds.Fresh()`) her akışa (deste / kasa / hile) `RandomSeeds.Derive` ile ayrı tohum
  verir; asla birden fazla `new Random()` (aynı saat tikinde aynı sayılar).
- Oyuncuya görünen tüm metinler `UiText` içinde, iki dilde (bkz. **Dil**). Core'da oyuncuya görünen metin yok.

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
# DİKKAT: son player build'den sonra yeni bir script KLASÖRÜ eklendiyse build.log'da "error CS0234/CS0246" satırları çıkar: Bee eski
# player DAG'ının dosya listesiyle ilk csc'yi dener, sonra listeyi yenileyip derler. Hüküm "Build Finished, Result: ..." satırı ve çıkış kodu.
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
Kısayollar: Space/Enter dağıt · çek · pas · karşıla, R artır, D check to draw, K sınıf gücünü kullan (Kral: kart seç), C karşıla, F çekil, 1-5 kart seç, H el tablosu, L dil (sadece ana menüde), Q / E (ya da oklar) ayar sekmeleri, Esc bir üst ekran,
Alt+Enter tam ekran. Animasyon sürerken herhangi bir tuş / tık animasyonu atlatır.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
