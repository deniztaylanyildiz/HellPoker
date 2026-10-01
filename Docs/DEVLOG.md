# Hell Poker — Geliştirme Günlüğü

Claude Code ile yapılan konuşmaların özeti. Yeni kayıtlar en alta eklenir.
Başka bir bilgisayarda devam ederken: repoyu çek, Claude Code'u proje klasöründe aç ve
"DEVLOG'u oku, kaldığımız yerden devam edelim" de.

---

## 2026-10-01 — Oturum 1: Poker çekirdeği, oyun döngüsü, Unity arayüzü, SOLID düzenlemesi

### İstekler (kullanıcı)
1. "Bu bir Unity 2D projesi: Hell Poker. 5 Card Draw poker, oyuncu kasaya karşı oynuyor, 1000 yıl cehennem cezasıyla
   başlıyor ve sıfırlamaya çalışıyor. Dead Man's Hand en güçlü el. SOLID prensiplerine uygun, önce Unity'den bağımsız
   poker çekirdeğini yaz: kart, deste, el değerlendirme, kart değiştirme turu."
2. "Sen nasıl ilerlemek istersen sen kararı ver ve devam et."
3. "Oyunu SOLID prensiplerine uygun hale getirelim ve konuşmalarımızı kaydedebileceğimiz bir dosya ekleyelim ki başka
   bilgisayardan da devam edebileyim."

### Yapılanlar
- **Core (Unity'siz):** `Card`, `Deck`, `Hand`, Fisher-Yates karıştırma, kural tabanlı `HandEvaluator`
  (her el türü bir `IHandRule`), `CardExchanger` + `MaxDiscardPolicy`.
- **Oyun katmanı (Core):** `HellPokerGame` (Betting → Drawing → RoundOver / Absolved / Damned), `HouseDrawStrategy`
  (kasa AI), `PayoutTable`, `PunishmentLedger`, `GameRules`, `HellPokerGameFactory`.
- **Unity katmanı:** MVP yapısı — `TablePresenter` + uGUI view'ları (koddan kurulan arayüz), `KeyboardInput`,
  `HellPokerBootstrap` (Inspector'dan ayarlanabilir). `Assets/Scenes/HellPoker.unity` sahnesi editör aracıyla üretildi
  ve Build Settings'te ilk sırada.
- **SOLID gözden geçirmesi:** presenter'ı arayüzlere bağlama (`IHellPokerGame`, `ITableView`...), girdi ile kurulumu ayırma,
  `IPayoutInfo` (ISP), oyun kurulumunu tek fabrikada toplama, renk yerine `Tone`.
- **Testler:** 72 EditMode testi, hepsi geçiyor (el değerlendirme, deste, kart değiştirme, kasa AI, oyun akışı, presenter).
- Arayüz ekran görüntüleriyle kontrol edildi (bahis, kart seçme, showdown ekranları).

### Kararlar
| Karar | Neden |
|---|---|
| Dead Man's Hand = A♠ A♣ 8♠ 8♣ + herhangi 5. kart, Royal Flush'ın üstünde | Kullanıcı "en güçlü el" dedi; 5. kart tarihte bilinmiyor, kicker sayılıyor |
| DMH ile kazanmak tüm cezayı siler | Tema: Wild Bill'in eli ile "aklanma" |
| Kart değiştirme limiti 3 (klasik kural) | Inspector'dan `_maxDiscards` ile değiştirilebilir |
| Kayıp = bahis kadar yıl eklenir; 2000 yılda oyun biter | Bir kaybetme koşulu gerekiyordu; `GameRules`'tan ayarlanır |
| Arayüz koddan kuruluyor (prefab/sprite yok) | Henüz görsel asset yok; sanat gelince view'lar prefab'a taşınabilir |
| Arayüz metinleri İngilizce, `UiText`'te toplu | Poker terimleri İngilizce; ileride yerelleştirme kolay |
| UI için legacy `Text` (TextMeshPro değil) | TMP Essential Resources import edilmemişti; ileride TMP'ye geçilebilir |

### Sıradaki adımlar (öneriler)
- Görsel cila: kart sprite'ları, dağıtma/çevirme animasyonları, ses efektleri, TextMeshPro'ya geçiş.
- Oyun tasarımı: kasanın "şeytani" özel yetenekleri, ceza arttıkça zorlaşan kasa, kayıt/yükleme (en iyi skor).
- Ayar ekranı / ana menü.
- Yerelleştirme (Türkçe arayüz) — `UiText` hazır.

### Açık sorular
- Arayüz dili Türkçe mi olsun, İngilizce mi?
- Kaybetme koşulu (2000 yıl) ve çarpanlar oynanış testine göre ayarlanmalı.

---

## 2026-10-01 — Oturum 1 (devam): Sahne ve oynanabilir hale getirme

### İstek (kullanıcı)
"Oyunu kendim test etmek istiyorum, sahne tarafını da halleder misin?"

### Yapılanlar
- `Assets/Scenes/HellPoker.unity` kontrol edildi: `HellPokerBootstrap` bağlı, Build Settings'te ilk sırada.
- **PlayMode testi** eklendi (`Assets/Tests/PlayMode/HellPokerSceneTests.cs`): sahneyi yükler, gerçek butonlara basarak
  bahis → dağıt → kart seç → çek → sonraki el akışını oynar. 2/2 geçti, log'da hata yok.
- **Menü kısayolu:** `Hell Poker ▸ Play` (Ctrl+Shift+P) — sahneyi açar ve Play moduna girer.
- Unity bu bilgisayarda açılışta doğrudan HellPoker sahnesini açacak şekilde ayarlandı (Library, git'e girmez).

### Kullanıcı testi için not
Diğer bilgisayarda: projeyi Unity Hub'dan aç → menüden **Hell Poker ▸ Play**.
Kullanıcının test geri bildirimleri bir sonraki kayıtta buraya yazılmalı.
---

## 2026-10-01 — Oturum 1 (devam): Gerilim — kart kart bahis, animasyonlar, son 250 yıl

### Kullanıcı geri bildirimi (ilk oyun testi)
"Sadece bir tuşa basabiliyorum ve kartlar lap lap diye geliyor. Her kart açıldığında bet arttır ya da pas imkânı olmalı,
gerginlik artsın."

Tasarım soruları ve kullanıcının cevapları:
- Kararlar hangi kartlarda? → **İkisi birden** (oyuncunun kartları dağıtımda + kasanın kartları showdown'da).
- Pas ne demek? → "Pas artırmadan devam etsin; çekilmek ayrı bir şey olsun. Artırmak zorunlu değil, ancak **son 250 yıl**
  kaldığında beti artırmak zorunlu olsun ve bu bölgeye girince oyunun sonuna yaklaşıldığını gösteren **görsel bir değişim** olsun."

### Yapılanlar
- **Core:** yeni fazlar `PlayerReveal` ve `HouseReveal`, `BetAction` (Raise/Pass/Fold), `Bet()`/`CanBet()`.
  Raise = ante kadar ekle; Fold = toplam bahsin yarısı ceza; ceza ≤ `ForcedRaiseYears` (250) iken Pas yasak.
  `HouseRevealDecisions` (3): kasanın ilk 3 kartından sonra karar, son 2 kart sürpriz.
- **Animasyonlar:** kartlar kapalı tek tek dağıtılıyor, tek tek çevriliyor; değiştirilen kartlar çevrilip yenisi geliyor.
  `AnimationSequencer` ile tüm ekran güncellemeleri sırayla oynuyor; animasyon sırasında girdi bekletiliyor.
- **Arayüz:** Artır (+ante) / Pas / Çekil butonları, "ON THE TABLE" toplam bahis göstergesi. Kısayollar: R artır, F çekil, Space pas.
- **Son 250 yıl görseli:** masanın arkasında nabız gibi atan ateş çerçevesi, ısınan masa rengi, kalp atışı gibi atan yıl sayacı,
  "THE GATES ARE IN SIGHT · UNDER 250 YEARS, NO PASSING" bandı. Ekran görüntüleriyle kontrol edildi (ilk deneme fazla yoğundu, yumuşatıldı).
- **Hata düzeltmesi:** tıklanan buton seçili kalıyordu; Space'e basınca uGUI onu tekrar tetikliyordu (bir tuş = iki aksiyon).
  `UiFactory.MakeClickOnly` ile düzeltildi.
- Testler: **85 EditMode + 3 PlayMode, hepsi geçiyor** (PlayMode testi animasyonları bekleyip gerçek butonlarla tam bir el ve bir fold oynuyor).

### Açık sorular / denge
- Kasanın kartları açılırken artırmak, kasanın zayıf göründüğü durumlarda oyuncuya avantaj sağlıyor; bu yüzden son 2 kart
  kararsız açılıyor. Oynanışa göre `HouseRevealDecisions` azaltılabilir.
- Son 250 yılda zorunlu artırma büyük antelerle çok sert olabilir (ör. 200 ante × 8 karar). Oyun testine göre
  artırma miktarına üst sınır gerekebilir.
---

## 2026-10-01 — Oturum 1 (devam): "Oyunu çalıştırınca mavi ekran"

### Sorun
Kullanıcı Play'e basınca mavi ekran gördü. Editor.log'da hata yok; Play, kaydedilmemiş boş bir `Untitled` sahnede
başlatılmıştı (Unity'nin varsayılan kamerası = mavi). Neden: batchmode test çalıştırmaları `Library/LastSceneManagerSetup.txt`
dosyasını boşaltıyor, editör sonraki açılışta boş sahneyle geliyor.

### Çözüm
`Assets/Scripts/Editor/HellPokerEditorStartup.cs` ([InitializeOnLoad]):
- `EditorSceneManager.playModeStartScene` = HellPoker sahnesi → Play hangi sahne açık olursa olsun oyunu başlatır.
- Editör boş/isimsiz bir sahneyle açılırsa (oturum başına bir kez) HellPoker sahnesini açar.
Not: başka bir sahneyi Play ile test etmek gerekirse bu davranış kapatılmalı (şimdilik tek sahne var).
---

## 2026-10-01 — Oturum 1 (devam): Ana menü ve bahis matematiği

### Kullanıcı geri bildirimi
"Oyunun ana ekranı okey ama oyuna bu menüde başlamayalım. 1000 yıl için 200 yıllık artırmayı her kartta yaparsak
2000 yıllık bet atabiliyoruz, mantığa ters; her bet arttığında aşağıda yazan yıl da azalsın istiyorum. Matematiksel
problemler çözülsün. Çeşitli kurpiyeler (şeytanlar) olsun, başka bir menüden seçebilelim — ama bunlar sonranın işleri."

### Yapılanlar
- **Bahis matematiği (Core):** toplam bahis ≤ mevcut ceza. `YearsOffTable` (ceza − masadaki bahis) ve `RaiseAmount`
  (ante ya da kalan; 0 = all-in). Ante seçenekleri cezayı aşamaz. Son 250 yılda zorunlu artırma all-in olunca biter
  (önceden 200 yılla 1800 yıllık bahis mümkündü). All-in kaybedilirse ceza ikiye katlanır (1000 → 2000 = lanet).
- **Sayaç:** masaya konan her yıl sağ üstteki sayaçtan anında düşüyor; el bitince gerçek sonuca animasyonla gidiyor.
- **Artır butonu:** "RAISE +X" → kalan azsa "ALL IN +X" → her şey masadaysa kilitli "ALL IN".
- **Ana menü:** New Game / Continue (sadece süren bir oyun varsa) / How to Play (kurallar paneli) / Quit.
  Masada MENU butonu ve Esc. `MainMenuPresenter` + `IMainMenuView`, `IRunSession`, `IMenuCommands`, `IApplicationQuitter`.
- **Düzeltme:** mavi ekran çözümündeki `playModeStartScene` PlayMode testlerini kilitliyordu; kaldırıldı.
  Artık sadece "editör boş sahneyle açılırsa oyun sahnesini aç" var (batchmode'da devre dışı).
- Testler: **103 EditMode + 3 PlayMode, hepsi geçiyor.** Menü, kurallar ve masa ekran görüntüleriyle kontrol edildi.

### Sıradaki (kullanıcının istediği, sonraya)
- **Kurpiyer şeytanlar:** farklı kişilikte kasalar, ayrı bir menüden seçilecek. Mimari hazır noktalar:
  kasanın oyun stili `IDrawStrategy`, ödeme/ceza kuralları `IPayoutTable`, kurallar `GameRules`; menüye yeni bir
  ekran (`IDealerSelectView` gibi) ve `MainMenuPresenter`'a bir geçiş eklenebilir. Her şeytan = bu parçaların bir paketi
  (+ isim, portre, replikler).
---

## 2026-10-01 — Oturum 2: Kurpiyer şeytanlar ve gotik görsel yenileme

### İstekler (kullanıcı)
- "Şeytanlardan devam edelim, onların da resimleri olsun. Oyun ekranının görünüşü çok standart geldi; ileride hile tarzı
  şeyler yapacak şeytanlar/eventler olsun istiyorum, bunun için oyun içi görseller güzel gözükmüyor, biraz değiştirelim."
- Seçimler: portreler **Python ile PNG üretilsin**, stil **gotik cehennem kumarhanesi**, şeytanları **Claude önersin**.
- Ara mesaj: "Bunları hallettikten sonra son 250 yılda 25'in katları halinde bahis yapabilelim; 500'de 50, 1000'de 100 —
  cezanın 1/10'u şeklinde betler yükselsin." Sonra: "**Bet oranlarına girme**, görselleştirmeyi yaptıysan beni bekle,
  o mantığı oturtup direkt sana atarım." → Bahis ölçeğine dokunulmadı.

### Yapılanlar
- **Core / Dealers:** `Dealer` (şeytanın ev kuralları: kart değiştirme limiti, kasanın kararlı kart sayısı, ödeme tablosu)
  ve `DealerRoster` (Mammon, Belial, Lilith). `PayoutTable` artık `LossPercent` / `FoldPercent` alıyor (yukarı yuvarlar;
  varsayılanlar eski davranışla aynı). `HellPokerGameFactory.Create(masaKuralları, dealer, seed)`.
- **Akış:** New Game → **şeytan seçim ekranı** (`DealerSelectView`) → masa. `TablePresenter` her yeni koşuda seçilen şeytan
  için oyunu fabrikadan kurar; ilk koşudan önce girdiyi yok sayar. Esc seçim ekranından menüye döner.
- **Masada şeytan:** `DealerView` — altın çerçeveli portre, isim/unvan, parşömen konuşma balonu (daktilo efekti),
  ruh haline göre renk değiştiren aura. Karşılama, kazanma/kaybetme/çekilme/beraberlik, son 250 yıl, aklanma ve lanet replikleri.
  Repliklar sırayla seçiliyor (rastgele değil, test edilebilir).
- **Görseller (`Tools/ArtGen`, Python):** 3 şeytan portresi, gotik salon arka planı, ahşap+altın kenarlı kadife masa,
  altın çerçeve/panel/buton (9-slice), kumarhane fişleri, parşömen kart yüzü, pentagramlı kart sırtı, renk sembolleri,
  ateşli altın logo. **Fontlar:** Cinzel (başlık) + IM Fell English (metin), OFL; ♠♥♦♣↑↓ glifleri script ile eklendi.
- **Arayüz yeniden düzeni:** solda şeytan, ortada oval masa, sağda ceza sayacı + şeytanın ödeme tablosu, altta fişler/butonlar.
  Kartlar dağıtılırken hafif dönerek geliyor. Son 250 yıl modu artık art üzerine sıcak renk tonu + ateş çerçevesi.
- **Ekran görüntüsü aracı:** `HellPokerScreenshots` ([Explicit] PlayMode testi) tüm ekranları 1920×1080 PNG olarak kaydediyor;
  görsel kontrol bununla yapıldı (menü, kurallar, şeytan seçimi, karar, kart değiştirme, sonuç, 3 şeytan, son 250 yıl).
- Testler: **122 EditMode + 3 PlayMode, hepsi geçiyor** (yeni: DealerTests, şeytan replikleri, menü→seçim→masa akışı).

### Kararlar
| Karar | Neden |
|---|---|
| Şeytan farkları mevcut genişleme noktalarıyla (`GameRules`, `IPayoutTable`) | Yeni oyun mantığı yazmadan kişilik; hileler için `Dealer` paketi hazır |
| İsim/replik Presentation'da (`UiText.Dealers.cs`), kurallar Core'da | Oyuncu metinleri tek yerde (yerelleştirme), Core Unity'siz |
| Özellik metinleri (`DealerCards`) kurallardan türetiliyor | Kural değişince açıklama yalan söylemesin |
| Görseller Resources'ta, kod sprite yoksa düz renge düşüyor | Prefab'sız mimari korunur, asset eksikse oyun yine çalışır |
| Bootstrap'tan `_maxDiscards` / `_houseRevealDecisions` kaldırıldı | Artık şeytanın ev kuralı |

### Açık sorular / sıradaki
- **Bahis ölçeği (kullanıcı tasarlayacak, bekleniyor):** ante/artırma cezanın ~1/10'u olsun (250'de 25'in katları, 500'de 50,
  1000'de 100). Dokunulacak yerler: `GameRules` (MinStake/MaxStake), `HellPokerGame.RaiseAmount`/`IsValidStake`,
  `TablePresenter` stake seçenekleri, `StakeSelectorView` fişleri.
- Belial'in ×1.5 kaybı / Lilith'in tam çekilme cezası dengelenmeli mi? (oyun testine göre)
- Şeytan hileleri/eventleri: `Dealer`'a yeni parçalar (ör. `IDealerTrick`) + `DealerView` üzerinden görsel geri bildirim.
- Portreler prosedürel; ileride gerçek çizim aynı dosya adlarıyla değiştirilebilir.

---

## 2026-10-01 — Oturum 2 (devam): Bahis matematiği, 5 kararlı el akışı, kasa re-raise

### İstek (kullanıcı)
Kullanıcı bahis mantığını madde madde yazıp gönderdi (A-G). Özet:
- **A)** Bahis birimi = elin başındaki cezanın 1/10'u, okunaklı adıma yuvarlanmış (≥1000 → 100'ün katı, ≥500 → 50, ≥250 → 25, altı → 10).
  Ante = 1 birim (seçici kalkar). Masada en fazla cezanın %50'si. Son 250 yıl: zorunlu artırma sadece tavana kadar.
- **B)** Simetrik ödeme: kayıpta `bahis × kasanın el çarpanı × LossPercent`. Çarpanlar Quads ×10 / SF ×15 / Royal ×20;
  Belial Quads ×15 / SF ×25 / Royal ×30 ve LossPercent 150 → 100 (simülasyonda %74 lanet çıkmış).
- **C)** En fazla 5 karar: ilk 2 kart kararsız, 3-4-5'te karar; draw sonrası 1 karar; kasa `HouseCardsShown` (2/1/2) kart açınca 1 karar.
  Artırma draw'dan önce 1, sonra 2 birim.
- **D)** Çekilme: `FoldPercentBeforeDraw` / `FoldPercentAfterDraw` (Mammon, Belial 50/100; Lilith 100/100).
- **E)** Draw sonrası oyuncu artırırsa kasa re-raise yapabilsin (`IHouseBettingStrategy`, `IRandomSource`):
  Mammon %70 / blöf %5, Belial %60 / %30, Lilith %90 / %10. Şeytan replik söylesin, Karşıla / Çekil çıksın.
- **F)** "Kazanırsan en az −X · Kaybedersen +Y" bilgisi; şeytan kartı özellikleri yeni kurallardan türesin.
- **G)** InitTestScene ve kullanılmayan SampleScene silinsin, InitTestScene `.gitignore`'a.
- Ara mesaj: "Dead Man's Hand yenilmez olsun."

### Yapılanlar
- **Core:** `StakeScale` (birim/ante/tavan), `GameRules` yeniden (min/max stake kalktı; `HouseCardsShown`, `OpeningCardsShown`,
  `RaiseUnitsBeforeDraw/AfterDraw`, `HouseReRaiseUnits`, `Stakes`). Yeni fazlar `DrawReveal`, `HouseReRaise`; `BetAction.Call`.
  `HellPokerGame` yeniden yazıldı: `PlaceBet()` parametresiz; `Unit`, `TableCap`, `UpcomingAnte`, `HouseReRaiseAmount`,
  `IsAfterDraw`, `LeastYearsForgiven/Added`. `PayoutTable` simetrik + iki çekilme oranı + "en az" hesapları.
- **Betting/:** `IHouseBettingStrategy`, `HandStrengthBettingStrategy`, `HouseBettingStyle`; `Dealer` artık mizacı taşıyor,
  somut strateji `HellPokerGameFactory`'de kuruluyor (kasa zarı deste karıştırmasından ayrı bir rastgele akıştan).
- **Arayüz:** fiş seçici kaldırıldı → "ANTE (çip) 100 YEARS" + DEAL. Masada kazan/kaybet satırı. Re-raise'de CALL +X / FOLD,
  şeytanın re-raise replikleri. Şeytan kartlarında 6 özellik satırı (kurallardan türetiliyor). Kısayol: C = karşıla, ↑/↓ kalktı.
- **Dead Man's Hand:** zaten en üst kategoriydi ve her eli yeniyordu (kural + Royal Flush testi vardı). Masada Royal Flush'a
  karşı oynanan bir el testi ve DMH'nin her kategoriyi (iki yönde) yendiği testler eklendi. Kasa DMH ile kazanırsa en yüksek çarpan.
- **Temizlik:** InitTestScene + SampleScene silindi, Build Settings'ten SampleScene çıkarıldı, `.gitignore`'a `/Assets/InitTestScene*.unity*`.
- Testler: **181 EditMode + 3 PlayMode, hepsi geçiyor**. Ekran görüntüleri kontrol edildi (ante, karar, re-raise, son 250 yıl, şeytan seçimi).

### Kararlar
| Karar | Neden |
|---|---|
| Birim aşağı yuvarlanır (249 → 20, 999 → 50) | Spesifikasyon örnekleriyle (650 → 50, 340 → 25, 180 → 10) tutarlı tek kural |
| Tavan ante'den küçük olamaz, cezayı geçemez | Çok küçük cezada (ör. 15) ante tek başına %50'yi aşıyor |
| Re-raise her iki draw-sonrası kararda olabilir (yeni el + kasa açılışı) | "Kart değiştirdikten sonra artırırsa" — ikisi de draw sonrası |
| Re-raise'e çekilmek, re-raise öncesi bahis üzerinden ceza öder | Karşılanmamış re-raise masada değil |
| Space re-raise'de Karşıla demek | Space her yerde "devam" anlamında |
| Kasa DMH ile kazanırsa en yüksek çarpan | Spesifikasyonda yoktu; DMH'nin ayrı çarpanı yok, en güçlü el olduğu için |

### Açık sorular / sıradaki
- Denge: yeni kurallarla simülasyon (lanet / aklanma oranı, ortalama el sayısı) çalıştırılmadı — istenirse Core'da bir simülasyon testi yazılabilir.
- Ödeme tablosunda Lilith için "Fold: + the stake before the draw, + the stake after" yazıyor (şeytan kartındaki gibi "always" yapılabilir).
- Şeytan hileleri/eventleri hâlâ sırada (`Dealer` paketine yeni parçalar).

---

## 2026-10-01 — Oturum 2 (devam): Kontrol turu

### İstek (kullanıcı)
"Son denilenleri yapıp yapmadığını bir çekle, yapılmayan bir şey varsa yap."

### Kontrol
- Bahis spesifikasyonunun A-G maddeleri tek tek kodla karşılaştırıldı: hepsi uygulanmış ve testli.
  Kodda eski API'den (stake seçici, `HouseRevealDecisions`, `MinStake/MaxStake`, `IsValidStake`, `StepStake`) iz kalmadı.
- Bulunan ve düzeltilenler:
  - Sahne dosyasında `HellPokerBootstrap` için eski Inspector alanları (`_stakeOptions`, `_maxDiscards`) duruyordu → yeni alanlarla değiştirildi.
  - Açık bırakılan: ödeme tablosu Lilith için artık "Fold: always + the stake" yazıyor.
  - Açık bırakılan: denge simülasyonu yazıldı (`BalanceSimulation`, [Explicit]).
- Testler: **181 EditMode + 3 PlayMode geçiyor** (+ 2 explicit araç: ekran görüntüsü, simülasyon).

### Simülasyon sonucu (her şeytana 2000 koşu, basit oyuncu: görünen çiftte artır, draw sonrası Two Pair+ ile artır)
| Şeytan | Aklanma | Lanet | Ort. el sayısı |
|---|---|---|---|
| Mammon | %75.7 | %24.3 | 4.4 |
| Belial | %69.1 | %30.9 | 3.2 |
| Lilith | %75.8 | %24.3 | 4.4 |

**Bulgu:** koşular çok kısa (ortalama 3-4 el). Neden: tavan cezanın %50'si (1000'de 500) ve ödeme `bahis × çarpan`;
500 masadayken Two Pair bile 1000 yılı siler, kayıpta da kasanın çarpanıyla ceza katlanıp 2000'e hızla ulaşılıyor.
Lilith ile Mammon neredeyse aynı çıkıyor çünkü simülasyondaki oyuncu hiç çekilmiyor (Lilith'in farkı çekilme cezasında).
Kurallar değiştirilmedi — bahis tasarımı kullanıcıda. Olası ayarlar: tavan %50 → %20-25, çarpanlara üst sınır,
ya da kazanç/kayıpta çarpanı tavanla sınırlamak.
