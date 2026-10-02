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
  - Sıradan masada kazanç cezayı bitiremez, son 1 yıl kalır (`GameRules.KeepsTheLastYear`). İstisna: Dead Man's Hand
    her yerde anında aklar ("Wild Bill's escape").
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
    "WILD BILL'S ESCAPE" (sıradan masada Dead Man's Hand), ABSOLVED (Lucifer'siz oyun), DAMNED.
  - Hikâye satırları: el sayısı, en düşük / en yüksek ceza, en iyi el, masalar, ruh, Lucifer'le karşılaşma sayısı.
- **Rekorlar** (`RecordBook`, `run.records`):
  - koşu, aklanma, lanet, şeytan başına aklanma (Lucifer'de biten koşu gelinen şeytana yazılır), en hızlı aklanma;
  - Lucifer'e ulaşma, Lucifer'i yenme, en az denemede yenme, Wild Bill kaçışları (satırlar isteğe bağlı, eski defter okunur).
- **Ayarlar** (`GameSettings`, PlayerPrefs `settings.*`): animasyon hızı, tam ekran (Alt+Enter; pencere 480×270'in tam katı),
  el rehberi, ipuçları. Batchmode'da (testler) ayar / kayıt / rekor süreç boyu tek bir bellek deposunda
  (`HellPokerBootstrap.BatchStore`); PlayMode testleri her testte onu temizler.
- **New Game → kurpiyer şeytan seçimi** (Mammon / Belial / Lilith). Her şeytanın kendi ev kuralları var (`DealerRoster`):
  | Şeytan | Kart değiştir | Kasa gösterir | Ödeme | Çekilme (önce/sonra) | Re-raise (Two Pair+ / blöf) |
  |---|---|---|---|---|---|
  | **Mammon** (Tefeci, dürüst) | 3 | 2 | standart | %50 / %100 | %70 / %5 |
  | **Belial** (Gümüş Dil, blöfçü) | 3 | 1 | yüksek (Quads ×15, SF ×25, Royal ×30) | %50 / %100 | %60 / %30 |
  | **Lilith** (Gecenin Kraliçesi, acımasız, en zor) | 4 | 2 | standart, kayıp **×1.25** | %100 / %100 | %90 / %10 |
  | **Lucifer** (The Morning Star, final; seçilemez) | 3 | **0** | standart, kayıp **×1.25**; **sabit ölçek** ante 50 / tavan 150 | %100 / %100 | %80 / %25 |
  - Lucifer'in sabit ölçeği: `Dealer.Stakes` = `StakeScale.Fixed(50, 150)`, daha azı all-in. `Dealer.IsFinalTable`.
  - Mammon ve Belial'de `LossPercent` = 100, Lilith'te 125. Şeytan masada portresiyle oturur ve replik söyler (re-raise dahil, `UiText.Dealers.cs`).
- **Ödeme simetrik, çarpan sadece ante'ye:** artırmalar ve re-raise'ler 1'e 1 ödenir.
  - Kazanç: `toplam bahis + ante × (oyuncunun çarpanı − 1)` yıl silinir (cezayı geçemez).
  - Kayıp: `(toplam bahis + ante × (kasanın çarpanı − 1)) × LossPercent` yıl eklenir, yukarı yuvarlanır.
  - Örnek: ante 100, toplam 300, Full House (×8) → 300 + 700 = 1000. Beraberlik: değişiklik yok.
- **Denge** (`BalanceSimulation`, 2000 koşu, akıllı oyuncu; ruh, mühür ve Lucifer'le):
  | Şeytan | Aklanma | Ort. el | Lucifer'e ulaşan | İlk denemede yenme | Ort. deneme |
  |---|---|---|---|---|---|
  | Mammon | ~%87 | ~31 | %89 | %48 | 2.1 |
  | Belial | ~%79 | ~18 | %82 | %55 | 1.8 |
  | Lilith | ~%62 | ~22 | %69 | %48 | 1.9 |

  Ruhu masaya koyan koşular %23 / %33 / %55. `HELLPOKER_LUCIFER_GATE` (0 = Lucifer yok) ve `HELLPOKER_CAST_DOWN` ile denenebilir. Ruh değerini düşürmek el sayısını neredeyse değiştirmiyor
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
    Dealers/       Dealer (şeytanın ev kuralları paketi: MaxDiscards, HouseCardsShown, PayoutTable, HouseBettingStyle, SoulThreshold), DealerRoster
                   (Game/ altında ayrıca StakeScale: bahis birimi, ante, masa tavanı)
  Presentation/  HellPoker.Presentation.asmdef — Unity katmanı (MVP)
    Abstractions/  ITableView (+ TableMoment), IHandView, IDealerView (+ DealerMood), IDealerSelectView (+ DealerChoice), IRunSession,
                   IMainMenuView, ISettingsView (+ IDisplayMode, IScreenTransition), IEndScreenView / IRecordsView (+ RunSummary),
                   IGuideSettings, DealerCard, SoulGauge (+ LeaveState), ITableCommands, IMenuCommands, Tone
    Animation/     SpriteClip + SpriteSheet (yatay şeridi kare karelere böler), DealerAnimationLibrary ve SalonLibrary (yedek zincirleri),
                   SpriteFrameAnimator (Image üzerinde kare oynatır; döngü / tek sefer), AnimationClock (hız + atlama; tüm tween'ler)
    Views/         uGUI view'ları (UI tamamen koddan kurulur, prefab yok): TableView, DealerView, DealerSelectView, SalonView,
                   SoulView, SentenceView, CardView, HandRanksPanel, TableMoments, TableScenes (çağrılma kararması / düşüş /
                   Lucifer titremesi), SettingsView, EndScreenView, RecordsView,
                   ScreenTransitionView, AnimationSequencer (Complete = atla; hata veren adım kuyruğu kilitlemez)
    Settings/      GameSettings (+ IGuideSettings), ISettingsStore (PlayerPrefsStore / MemoryStore), RunArchive (kayıt + rekorlar)
    Ui/            UiFactory, PixelScreen (480×270, tam sayı ölçek), UiArt (Resources'tan sprite/font), Palette, UiText + UiText.Dealers,
                   ButtonFeel (hover / 1 px basılma / kilitli görünüm), ClickCatcher
    TablePresenter (masa; IRunSession: yeni koşu, devam (Resume), LEAVE isteği, masa değiştirme (ceza taşınır), kayıt / rekor, RunEnded;
    her masada oyunu Func<Dealer, IHellPokerGame> ile kurar), MainMenuPresenter (tüm ekranlar arası gezinme, Esc, geçişler),
    SettingsPresenter (ayar ekranı ↔ GameSettings ↔ hız / pencere), UnityDisplayMode, DealerCards, KeyboardInput,
    HellPokerBootstrap (composition root)
  Editor/        HellPokerSceneBuilder (menü: Hell Poker ▸ Build Main Scene), HellPokerMenu (Hell Poker ▸ Play, Ctrl+Shift+P),
                 HellPokerArtImporter (Resources/Art: Point filtre, PPU 100, sıkıştırmasız, 9-slice; Resources/Fonts: Hinted Raster),
                 HellPokerEditorStartup (editör boş sahneyle açılırsa HellPoker sahnesini açar — batchmode son açık sahneyi sıfırlıyor).
                 UYARI: `EditorSceneManager.playModeStartScene` KULLANMA — Test Runner'ın PlayMode sahnesini de yönlendirip testleri kilitliyor.
Assets/Tests/EditMode/  NUnit testleri (Core + Presenter, fake view'larla)
Assets/Tests/PlayMode/  Sahneyi yükleyip gerçek butonlarla oynayan testler: HellPokerSceneTests, SalonRegressionTests,
                        RunJourneyTests (yeni oyun → eller → masa değiştir → menü → devam → sahneyi yeniden yükle → devam),
                        LuciferJourneyTests (yığılmış destelerle çağrılma → düşüş → yeniden çağrılma → zafer)
                        + HellPokerScreenshots ([Explicit]: tüm ekranların 1920×1080 görüntüsünü alır)
Assets/Scenes/HellPoker.unity  — ana sahne (kamera + HellPokerBootstrap)
Assets/Resources/Art/   Üretilmiş piksel görseller:
                          Demons/<dealerId>/<durum>.png  (idle, talk, gloat, angry, reraise, final, soul — yatay şerit, kareler 96×96)
                          Backgrounds/<dealerId>/{normal,hell,soul}.png  (şeytan salonları, 480×270 kareler, 4 FPS)
                          Ui/ (background[_hell], panel[_hot], dialog, button_*, card_face/back/slot, suit_*[_small], digits, title, coin,
                               flames, divider, soul_lamp)
Assets/Resources/Fonts/ HellPokerPixelTitle (Press Start 2P), HellPokerPixel (Tiny5) — OFL lisansları yanında; eksik glifler piksel olarak eklendi
Tools/ArtGen/           pixel.py (palet + çizim), pixel_demons.py, pixel_salons.py, pixel_ui.py, fonts.py, generate_art.py, preview.py
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
  - Salonu (`Backgrounds/lucifer/{normal,hell}`, 4 kare): tahtın sadece alt basamakları ve ayakları, zincirler, korlar.
  - Konuşurken isim "THE MORNING STAR" (metin fontu, kızıl), yazı kor turuncusu, kutu `Ui/dialog_lucifer`; her replikte ekran 1 px titrer.
- **Sahneler** (`TableScenes`, `IDealerView.SetDealer(card, SeatChange)`):
  - Çağrılma: eski şeytanın son sözünden sonra `Ui/fade` (¼ / ½ / ¾ / tam Bayer siyahı) ile yavaş kararma, değişim karanlıkta,
    sonra karanlık kalkar.
  - Düşüş: ekran 6 px'lik adımlarla yukarı kayar, eski salon aşağıdan gelir.
  - Her sahne iki kuyruk adımı. İkinci adım yeni şeytan konuşunca ya da `SetAction`'da kuyruğa girer.
  - Değişim anında eski masanın son anlar / çıkış butonu sıfırlanır. Atlama çalışır, anında değişim `Abort` eder.
- Importer 2048 px'ten geniş dokuyu küçültür: salon şeridi en fazla 4 kare (4 × 480).
- Proje **lineer renk uzayında**: UI'da düşük alfa bile ekranda güçlü görünür.
- Hileler/eventler (ileride): `Dealer` paketine yeni parçalar olarak eklenecek; presenter şeytanı yalnızca `Dealer` üzerinden tanır.

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

# Denge simülasyonu (her şeytana 2000 koşu; rapor sim.xml'deki test çıktısında).
# Başka ruh sayılarını kodu değiştirmeden denemek için önce: $env:HELLPOKER_SOUL_WORTH = "800"; $env:HELLPOKER_SOUL_LOSS = "150"
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter HellPoker.Core.Tests.BalanceSimulation -testResults sim.xml -logFile sim.log

# Görselleri / fontları yeniden üret (py -m pip install --user pillow numpy fonttools)
py Tools/ArtGen/generate_art.py [demons] [salons] [ui]
py Tools/ArtGen/fonts.py
py Tools/ArtGen/preview.py        # Tools/ArtGen/preview/index.html: tüm şeytan karelerinin animasyonlu önizlemesi

# Ana sahneyi yeniden oluştur
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build -logFile build.log
```

Editör açıkken: Window ▸ General ▸ Test Runner. Oynamak için menüden **Hell Poker ▸ Play** (Ctrl+Shift+P)
ya da `Assets/Scenes/HellPoker.unity` → Play.
Kısayollar: Space/Enter dağıt · çek · pas · karşıla, R artır, D check to draw, C karşıla, F çekil, 1-5 kart seç, H el tablosu, Esc bir üst ekran,
Alt+Enter tam ekran. Animasyon sürerken herhangi bir tuş / tık animasyonu atlatır.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
