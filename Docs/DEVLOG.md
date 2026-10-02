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

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı — Bölüm 1, denge düzeltmesi

### İstek (kullanıcı)
"Güncelleme Planı (Denge + 16-bit Piksel Görseller)" dosyası. Bölüm 1: oyunlar 3-4 elde bitiyor; çarpan sadece ante'ye
uygulansın (artırmalar 1'e 1), tavan %50 → %30, Lilith LossPercent 125 (en zor şeytan), formül testleri, simülasyonu tekrar çalıştır
(beklenen: Mammon ~%79 / 35-40 el, Belial ~%70 / ~20, Lilith ~%65 / ~40); çok saparsa nedenini yaz, kuralı kendi başına değiştirme.

### Yapılanlar
- `IPayoutTable` ante'yi parametre alıyor. Kazanç `bahis + ante × (çarpan − 1)` (cezayı geçemez), kayıp
  `(bahis + ante × (kasa çarpanı − 1)) × LossPercent` (yukarı yuvarlanır). Ante bahsin parçası değilse hata. Çarpanlar en az 1.
  DMH aynı (oyuncu kazanırsa tümü silinir; kasa kazanırsa en yüksek çarpan). "En az" hesapları da ante ile.
- `StakeScale.TableCapPercent` 30 (Bootstrap ve sahne de 30). Lilith `LossPercent` 125.
- Metinler: ödeme tablosu "Multipliers count on the ante; raises pay 1 : 1 / Lose: the same on the House's hand (× 1.25)",
  şeytan kartı "Lose: like a win, on the House's hand × 1.25" (kurallardan türüyor), kurallar ekranı yeni formül.
- Testler: formül (kazanç, kayıp, LossPercent yuvarlama, DMH, cezayı aşmama, ante doğrulama), %30 tavan, kesilen çift artırma,
  re-raise testleri için geniş tavanlı kurallar. **188 EditMode + 3 PlayMode geçiyor.**
- `BalanceSimulation` oyuncusu "artıran / çekilen akıllı oyuncu" oldu: görünen çiftte artırır; 5 kart görünce çekilmesi ucuzsa
  çekilir; draw sonrası Two Pair+ artırır, kasa çift gösterirken high card'la çekilir; re-raise'i çiftle karşılar.

### Simülasyon (2000 koşu)
| Şeytan | Aklanma | Lanet | Ort. el | Beklenen |
|---|---|---|---|---|
| Mammon | %79.0 | %21.0 | 49.0 | ~%79 / 35-40 |
| Belial | %74.2 | %25.8 | 23.2 | ~%70 / ~20 |
| Lilith | %64.4 | %35.6 | 38.3 | ~%65 / ~40 |

- İlk çalıştırmada Lilith **%54 / 56 el** çıktı. Neden kural değil oyuncuydu: oyuncu draw öncesi zayıf eli bırakıyordu, Lilith'te bu
  bahsin tamamına mal oluyor (diğerlerinde yarısı). Çekilme maliyetine bakan oyuncuyla beklentiye oturdu. Kurala dokunulmadı.
- Mammon'da el sayısı (49) beklenenden (35-40) biraz uzun; oran tutuyor. Fark muhtemelen oyuncu politikasının (Python
  simülasyonundakinden daha temkinli artırma) sonucu — kural değiştirilmedi.

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı — Bölüm 2, 16-bit piksel art dönüşümü

### İstek (kullanıcı)
Bölüm 2: SNES / eski DOS havası; masa yok, sade koyu zemin, animasyonlu şeytanlar. Python ile piksel art (≤32 renk tek palet,
kenar yumuşatma yok), şeytan kareleri 96×96 ve 6 durum (idle, talk, gloat, angry, reraise, final), kart 32×48, 9-slice RPG kutuları,
piksel rakamlar, OFL piksel fontlar (Cinzel / IM Fell kalksın), Point filtre, 480×270 tam sayı ölçek, SpriteFrameAnimator,
yedek zinciri (durum → idle → portre → düz renk) + testleri, son 250 yıl piksel alevler, seçim ekranında idle animasyonu,
ekran görüntüleriyle kontrol, önizleme sayfası (.gitignore), DEVLOG ve CLAUDE.md "Görsel kurallar".

### Yapılanlar
- **Tools/ArtGen yeniden:** `pixel.py` (31 renklik tek palet, tam sayı şekiller, dış çizgi, bant gölgeleme, Bayer dithering),
  `pixel_demons.py` (her şeytan poz parametreleriyle çizilir: nefes, göz kırpma, ağız, kaş, sarsılma, kızarma, buhar, parıltı, arkada
  alevler), `pixel_ui.py` (taş zemin + kızıl taş, panel/panel_hot/dialog/3 buton 12×12 9-slice, kartlar, 5×5 ve 11×11 semboller,
  12×16 piksel rakamlar, Press Start 2P'den alev renkli logo, sikke, alev şeridi, ayraç), `preview.py` (CSS ile oynayan HTML önizleme).
  Eski gotik modüller (paint/demons/ui) ve görseller silindi.
- **Şeytanlar (siluetler ayrışık):** Mammon — yuvarlak yeşil surat, taç, monokl, etrafında dönen altın sikkeler (gülerken yağıyor);
  Belial — uzun kırmızı yüz, büyük koç boynuzları, keçi sakalı, konuşurken çıkan gümüş çatal dil, omzunda gümüş yılan;
  Lilith — leylak yüz, hilal taç, uzun siyah saç, çırpınan gece kuşu kanatları. Durum kare sayıları: idle 6, talk 4, gloat 6,
  angry 5, reraise 5, final 4.
- **Fontlar:** Press Start 2P (başlık/buton/sayılar) ve Tiny5 (metin), ikisi de 8 px ızgara, OFL. Tiny5'e 5 px ♠♥♦♣, Press Start'a "−"
  eklendi (`fonts.py`). Fontlar Hinted Raster (yumuşatmasız) içe aktarılıyor.
- **Unity:** `PixelScreen` (480×270, tam sayı ölçek, siyah dolgu, pixelPerfect), `UiFactory.CreateScreen/PlaceTL`, tüm view'lar
  480×270 piksel yerleşimle yeniden: solda şeytan (104×104 kutu, isim, RPG diyalog kutusu, harf harf yazı), ortada kasa / oyuncu
  kartları ve mesajlar, sağda piksel rakamlı ceza sayacı ve ödeme paneli. Kart dağıtma/çevirme tam piksel adımlarla.
  `SpriteClip`/`SpriteSheet`, `DealerAnimationLibrary` (yedek zinciri, yüklemede hata = eksik), `SpriteFrameAnimator`.
  Presenter artık `DealerMood` gönderiyor (Neutral/Gloating/Annoyed/Scheming/Menacing), animasyonu `DealerView` seçiyor.
  Son 250 yıl: zemin kızıl taşa döner, üst/alt kenarda animasyonlu piksel alevler, banner yanıp söner, şeytan final animasyonunda.
  Şeytan seçimi: üstte üç animasyonlu portre + SIT DOWN, portreye tıklayınca alttaki panelde açıklama ve kurallar.
- **Testler:** `DealerAnimationTests` (kare bölme, döngü/tek sefer, eksik durum → idle, eksik idle → portre, hiç görsel yok → null,
  hata veren yükleyici, tek yükleme, boş klipte düz renk, ruh hâli → animasyon, piksel ölçeği). **210 EditMode + 3 PlayMode geçiyor.**
- Ekran görüntüleri kontrol edildi: menü, kurallar, şeytan seçimi, karar, kart değiştirme, re-raise, kasa açılışı, kazanç, kayıp,
  diğer şeytanlar, son 250 yıl. Bulanıklık / yarım piksel yok; "→" glifi eksikti (metin değişti), 5 px maça/sinek ayrıştırıldı.

### Kararlar
| Karar | Neden |
|---|---|
| Metin fontu Tiny5 (5 px büyük harf) | 480×270'te Pixelify Sans çok iri ve ızgarası tam sayı değil; Tiny5 8 px ızgarada, ekrana sığıyor |
| Son 250 yılda saydam kırmızı yerine `background_hell` sprite'ı | Saydam karıştırma palet dışı renk üretir |
| Seçim ekranında ortak detay paneli | 3 kart × 6 özellik 148 px genişliğe sığmıyordu |
| Kısa panel metinleri (`ShareShort`, "FREE", "TOSS", "PAYOUTS") | 104 px'lik sütunlar ve 32 px'lik kart |

### Açık konu — kullanıcının kararı gerekiyor
- **Re-raise neredeyse hiç olamıyor:** birim ≈ cezanın %10'u, tavan %30 → ante (1 birim) + draw sonrası artırma (2 birim) = 3 birim
  = tavan. 1000, 500, 250 gibi cezalarda kasanın re-raise'ine yer kalmıyor; sadece yuvarlamanın boşluk bıraktığı cezalarda (ör. 650:
  birim 50, tavan 195) çıkıyor. Seçenekler: tavanı %40'a çekmek, draw sonrası artırmayı 1 birim yapmak, ya da re-raise'in tavanı
  aşmasına izin vermek. Kural değiştirilmedi.

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı 2 — Bölüm 1, iki düzeltme

### İstek (kullanıcı)
"Güncelleme Planı 2 (Düzeltmeler + Şeytan Salonları + Ruh Mekaniği)". Bölüm 1: kasanın re-raise'i masa tavanını aşabilsin
(oyuncunun artırmaları tavana bağlı, re-raise cezayı aşamaz), simülasyon re-raise sıklığını raporlasın (beklenen ~%4-9);
Lilith'in portresi zeminde kayboluyor — açık ten, net kanat kenarı, parlak hilal taç.

### Yapılanlar
- `HellPokerGame.TryHouseReRaise`: miktar = min(birim × HouseReRaiseUnits, YearsOffTable); tavan yalnızca oyuncunun artırmalarını sınırlar.
  Karşılanan re-raise toplamı tavanın üstüne taşıyabilir; ondan sonra oyuncu artıramaz, Pas serbesttir.
- Testler: re-raise tavanı aşıyor ve karşılanınca masa tavanın üstüne çıkıyor; re-raise cezayı asla aşmıyor; rastgele 500 el testinde
  "tavan aşılırsa sadece kasa aşmıştır" kontrolü. **211 EditMode + 3 PlayMode geçiyor.**
- Lilith: soluk kemik-leylak ten (LILAC → BONE rampası), kanatlara lila kenar çizgisi, gümüş taç bandı üstünde beyaz parlak hilal,
  daha koyu arka plan; öfke kızarması kendi ten rengine göre. Mammon ve Belial'e dokunulmadı.
- Oturum sırasında Unity editörü açıldığı için batchmode testler bir kez çalışmadı; kullanıcı editörü kapattı, testler tekrarlandı.

### Simülasyon
| Şeytan | Aklanma | Ort. el | Re-raise'li el |
|---|---|---|---|
| Mammon | %78.9 | 48.1 | %3.4 |
| Belial | %74.6 | 22.5 | %6.4 |
| Lilith | %64.2 | 37.0 | %6.4 |

Aklanma oranları değişmedi. Mammon'un re-raise oranı beklenen aralığın (%4-9) biraz altında: "dürüst" mizacı (Two Pair+ %70, blöf %5)
ve simülasyon oyuncusunun draw sonrası yalnızca Two Pair+ ile artırması yüzünden. Kural değiştirilmedi.

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı 2 — Bölüm 2, şeytan salonları

### İstek (kullanıcı)
Her şeytanın kendi cehennem kumarhanesi (masa yok): Mammon — tefecinin hazine odası, Belial — gümüş dilin tiyatrosu,
Lilith — gecenin bahçesi / ay mahzeni. Okunabilirlik önce, normal / hell / soul varyantları, hafif ortam animasyonu,
yedek zinciri + test, seçim ekranında seçili şeytanın salonu (geçiş efektiyle), önizleme ve üç salonla ekran görüntüleri.

### Yapılanlar
- `Tools/ArtGen/pixel_salons.py`: 480×270, 3 karelik ortam animasyonu, aynı palet.
  - Mammon: yeşil kasa duvarı, demir bantlar, sallanan büyük terazi (kefelerde sikke), borç defteri rafları, ortanın iki yanında sikke
    sütunları, yerde altın yığınları ve kilitli sandıklar; sikkeler sırayla parıldıyor.
  - Belial: kan kırmızısı duvar, altın işlemeli kırmızı saçak ve gümüş püsküller, dalgalanan gümüş perdeler, komedi / trajedi maskeleri,
    yılan sarmallı sütunlar, boş kırmızı koltuk sıraları.
  - Lilith: menekşe ufka doğru açılan gece göğü, dev hilal ay (hale), kara ölü ağaçlar, mor sarmaşıklı mezar taşları, titreyen mum,
    geçen gece kuşları, yanıp sönen yıldızlar.
  - Okunabilirlik: orta sütun dithering'li yumuşak bir vinyetle bir ton, iki kart sırasının arkası iki ton koyulaşıyor; alttaki
    kısayol satırının arkası da bir ton koyu. Varyantlar palet eşlemesiyle: hell (her şey kızıl), soul (soğuk, solgun, gümüş/leylak).
  - Dosyalar: `Art/Backgrounds/<dealerId>/{normal,hell,soul}.png` (kareler yan yana, 480 px).
- Unity: `SalonLibrary` (yedek zinciri: varyant → salonun normal hali → `Ui/background_hell` (sadece hell) → `Ui/background` → null/düz renk),
  `SalonView` (SpriteFrameAnimator ile 4 FPS, 8 px'lik basamaklarla inip kalkan "perde" geçişi). Masada salon oturulan şeytana göre
  (TableView, presenter'ın `SetDealer`'ını bir sahne adaptörüyle salona da iletiyor), son 250 yılda `hell` varyantı + alevler.
  Şeytan seçim ekranında seçili şeytanın salonu arkada, seçim değişince perde geçişi.
- Testler: `SalonLibraryTests` (480 px kareler, her mod kendi varyantı, eksik varyant → normal, eksik salon → taş duvar / yanan taş duvar,
  hiç görsel yok → null, hata veren yükleyici). **217 EditMode + 3 PlayMode geçiyor.**
- Önizleme sayfasına salonlar (her varyant, animasyonlu) eklendi. Ekran görüntüleri üç salonla alındı ve kontrol edildi
  (seçim ekranı, masa, karar, son 250 yıl). İlk denemede salonlar görünmüyordu: sprite'sız `Filled` Image tüm ekranı kaplıyordu,
  perde boyutla çizilen bir dikdörtgene çevrildi.

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı 2 — Bölüm 3, ruh mekaniği ve masa değiştirme

### İstek (kullanıcı)
Her şeytanın bir ruh çizgisi olsun (Mammon 2000, Belial 1750, Lilith 1500). Ceza bu çizgiyi geçince ruh masaya konur;
ruhun değeri 1000 yıl, oyuncuya hiç gösterilmez. Ruh bölgesinde bahis birimi ruhun 1/10'u, tavan %30 (kalan ruhu asla aşmaz),
kayıplar ×1.5; ruh biterse lanet. Kazanınca ruh dolar, çizginin altına inince geri verilir. Eller arasında masa değiştirilebilsin
(LEAVE TABLE + menü), ceza yeni masaya taşınsın; ruh masadayken kilitli (şeytan özel replik söyler); çizgisi geçilmiş şeytana
oturmak uyarıyla serbest. Ruh barı yılların yerini alsın, ruh bölgesinde hiçbir yıl sayısı görünmesin. Testler, simülasyon,
ekran görüntüleri, DEVLOG / CLAUDE.md.

### Yapılanlar
- **Core**
  - `GameRules`: `SoulThreshold`, `SoulWorthYears` (1000), `SoulLossPercent` (150); `DamnationYears` artık `SoulThreshold + SoulWorthYears`.
  - `Dealer.SoulThreshold`, `TakesSoulAt(years)`; `DealerRoster` çizgileri: 2000 / 1750 / 1500.
  - `HellPokerGame`: `IsSoulAtStake`, `IsSoulHand` (el ruh masadayken dağıtıldıysa), `SoulRemaining`, `WagerLeft`.
    Ruh elinde birim / ante / tavan ruhun değerinden hesaplanır, kalan ruhla sınırlanır. Kasa re-raise'i tavanı aşabilir
    ama kalan ruhu aşamaz. Kayıp: `PayoutTable.GetYearsAdded(..., surchargePercent)`, `LossPercent × SoulLossPercent`,
    tek seferde yukarı yuvarlanır.
  - `CanLeaveTable(out reason)` (sadece Betting, ruh masada değilken) ve `TakeOver(years, roundsPlayed)` (masa değiştirince ceza taşınır).
- **Presentation**
  - `TablePresenter` ayrıca `IRunSession`. Ruh bölgesinde:
    - pot / ante / sayaç sıfır ya da gizli; `SoulGauge` gönderilir;
    - "WAGER MORE / ALL OF IT / MATCH IT" ve sayısız mesajlar kullanılır;
    - girişte şeytan `SoulTaken` (gloat), kurtuluşta `SoulReleased` (angry) söyler, sayaç geri gelir.
  - LEAVE TABLE: Betting'de açık; ruh masadayken "SOUL BOUND" ve basınca şeytan `SoulLocked` repliği. Biten bir elde basılırsa önce sıradaki ele geçer.
  - `MainMenuPresenter`: New Game → bayraksız seçim. LEAVE TABLE / menüdeki CHANGE TABLE → seçim ekranı (her kartta mevcut şeytan,
    SAFE / SOUL AT STAKE). Çizgisi geçilmiş şeytana tıklayınca uyarı (`SoulWarning`), SIT ANYWAY ile oturulur. Geri → masaya.
  - Views:
    - `SoulView` (fener + bar; masadaki pay yanıp söner, kayıpta kırmızı, kazançta kemik rengiyle akar; girişte kutu yanıp söner).
    - `TableView` (SoulView, LeaveButton; ruhta salon `soul` varyantı ve şeytan `soul` animasyonu).
    - `DealerSelectView` (kartlarda "SOUL AT X", durum etiketi, RETURN, uyarı kutusu). `MainMenuView`'a CHANGE TABLE eklendi.
  - Bootstrap: `_damnationYears` kaldırıldı; yerine `_soulWorthYears` ve `_soulLossPercent` (sahne güncellendi). Çizgi şeytandan gelir.
- **Testler:**
  - `SoulTests` (25):
    - çizgiye giriş ve çıkış, kalan ruh, lanet = çizgi + 1000;
    - birim ve tavan ruhtan, kalan ruhu aşmama, küçük kalan = all-in ante;
    - re-raise tavanı aşar ama ruhu aşmaz;
    - ×1.5 kayıp, Lilith'le birleşik yuvarlama;
    - ruh bitince lanet; kazanç ruhu doldurur; çizgi altına inince ruh geri;
    - masa terk izni (aradayken / el içinde / ruh masada);
    - TakeOver: taşıma, çizgisi geçilmiş masaya oturma, sadece eller arası, 0 = özgür;
    - şeytanlar arası ceza taşıma.
  - Presenter: tam bir ruh elinde (artırma + re-raise + kayıp) hiçbir view'a 2+ basamaklı sayı ve sıfırdan farklı yıl gitmiyor;
    çizgiyi geçmiş masaya oturma; LEAVE (açık / gizli / kilitli + replik); masa değişiminde ceza ve çizgi; görünür kurtuluş.
  - Menü: seçim bayrakları, güvenli masaya geçiş, mevcut şeytana dönüş, uyarı / onay / vazgeç, geri, menüden CHANGE TABLE (kilitliyken masada kalır).
  - **259 EditMode + 3 PlayMode geçiyor** (Explicit simülasyon ve ekran görüntüsü hariç).
- Ekran görüntüleri: 11_soul_entry, 12_leave_locked, 13_soul_decision, 14_soul_reraise, 15_soul_result, 16_soul_loss,
  17_soul_rescued, 18_change_table, 19_soul_warning, 20_lilith_soul. Kontrol sonrası düzeltmeler:
  - "SOUL AT STAKE" etiketi başlık fontunda portreden taşıyordu, metin fontuna alındı.
  - Uyarının arkasındaki yarı saydam karartma (piksel kuralına aykırı) kaldırıldı, sadece tıklama engeli kaldı.
  - Görüntü testi 3 dakikayı aştı, `Timeout(600000)` eklendi.

### Simülasyon (2000 koşu, ruh mekaniğiyle)
| Şeytan | Aklanma | Lanet | Ort. el | Re-raise'li el | Ruhu masaya koyan | Ruhu kurtarıp aklanan |
|---|---|---|---|---|---|---|
| Mammon | %88.7 (beklenen ~87) | %11.4 | 55.6 (beklenen ~43) | %3.3 | %21.1 | %9.8 |
| Belial | %81.0 (beklenen ~77) | %19.1 | 25.7 (beklenen ~22) | %6.2 | %31.0 | %11.9 |
| Lilith | %64.5 (beklenen ~66) | %35.5 | 38.1 (beklenen ~39) | %6.1 | %51.9 | %16.4 |

Aklanma oranları beklentiye yakın (Belial +4 puan). Mammon'un el sayısı beklenenden ~12 el uzun. Neden: Mammon'un çizgisi en yüksek
(2000), lanet 3000'de. Ruh bölgesinde bahis ruhun %10'uyla (100) sınırlı olduğu için düşüşler yavaş; ruhu masaya düşen koşuların
yarısı geri dönüyor ve bu koşular uzun sürüyor. Belial'in fazlası da aynı kurtarma etkisi. **Kural değiştirilmedi.**
İstenirse `SoulWorthYears` ya da `SoulLossPercent` ile ayarlanabilir.

### Kararlar
- Ruh bölgesinde çekilme (fold) cezası ×1.5 almıyordu (plan yalnızca kayıpları söylüyordu); sonraki kayıtta kullanıcı isteğiyle eklendi.
- Ruh bölgesine düşüren el "Your soul burns" diye sonuçlanıyor: o an ruh zaten masada, sayı göstermemek kuralı geçerli.
- Kurtaran elin mesajı ruh diliyle ("Your soul mends"), ardından sayaç ve "soul released" repliği geliyor.
- Ödeme tablosundaki çarpanlar ve şeytanın kendi kayıp çarpanı (Lilith × 1.25) ruh bölgesinde de görünüyor; ×1.5 gizli.
- Core'daki `CanLeaveTable` gerekçe metinleri diğer `CanBet` gerekçeleri gibi Core'da (önceki desenle tutarlı).

### Açık sorular
- Mammon'un ortalama el sayısı (55.6) beklenenden uzun. `SoulWorthYears` düşürülsün mü, yoksa bu "kurtarma" hissi isteniyor mu?
- Ruh bölgesinde fold cezası da ×1.5 olsun mu?

### Sıradaki adımlar
- Kullanıcı GitHub Desktop'tan commit / push yapmalı.
- Şeytan hileleri / eventleri (`Dealer` paketine yeni parçalar).

---

## 2026-10-01 — Oturum 2 (devam): ruh değeri ve ruhta çekilme ×1.5

### İstek (kullanıcı)
"ruh değeri ve uygula": Mammon'un uzun koşularını ruh değeriyle ayarla; ruh bölgesinde çekilmeye de ×1.5 uygula.

### Yapılanlar
- **Fold ×1.5:**
  - `IPayoutTable.GetFoldPenalty(stake, afterDraw, surchargePercent = 100)`; tek seferde yukarı yuvarlanıyor.
  - `HellPokerGame` ruh elinde `SoulLossPercent`'i fold'a da veriyor.
  - Testler `SoulFold_CostsHalfAgain` ve `SoulFold_RoundsUpOnce`. **261 EditMode geçiyor.**
- `BalanceSimulation` artık `HELLPOKER_SOUL_WORTH` / `HELLPOKER_SOUL_LOSS` ortam değişkenlerini okuyor (kod değiştirmeden tarama).

### Ruh değeri taraması (fold ×1.5 dahil, 2000 koşu)
| Ruh değeri | Mammon (aklanma / el) | Belial | Lilith |
|---|---|---|---|
| **1000** | %88.0 / 55.1 | %80.5 / 25.4 | %64.4 / 38.0 |
| 800 | %87.8 / 55.6 | %79.2 / 25.4 | %62.6 / 38.0 |
| 600 | %85.4 / 53.7 | %77.0 / 24.3 | %58.8 / 35.1 |
| 500 | %84.0 / 52.6 | %75.7 / 23.6 | %57.2 / 33.4 |
(Hedef: Mammon ~%87 / ~43, Belial ~%77 / ~22, Lilith ~%66 / ~39.)

### Sonuç ve karar
- **Ruh değeri, Mammon'un el sayısını kısaltmak için işe yaramıyor:** 1000 → 500'de bile 55 → 53 el.
  - Neden 1: ruh bölgesinde bahis birimi ruhun 1/10'u. Ruh küçülünce bahisler de küçülüyor ve ruh hep yaklaşık 10 birimlik bir tampon kalıyor.
  - Neden 2: Mammon koşularının sadece %21'i ruh bölgesine giriyor. Uzunluğun asıl kaynağı ruh öncesi oyun
    (ruh mekaniğinden önce de ~48 eldi).
- Ruh değerini düşürmek sadece Lilith'i hedefin (%66) altına itiyor (500'de %57).
- Bu yüzden **ruh değeri 1000'de bırakıldı** (aklanma oranları üç şeytanda da hedefe en yakın bu değer). Kullanıcıya bildirildi.

### Açık sorular
- Mammon'u kısaltmak isteniyorsa asıl kaldıraç ruh dışı bahis büyüklüğü (`StakeScale` birim / tavan). Bu her şeytanı etkiler.
  Diğer seçenek Mammon'a özel bir şey (ör. ruh çizgisini 1750'ye çekmek). Karar kullanıcıda.

---

## 2026-10-01 — Oturum 2 (devam): Güncelleme Planı 3 (Akıcılık ve Cila) — Bölüm 1, salon hatası

### İstek (kullanıcı)
Plan 3: yeni içerik yok; oyun akıcı, anlaşılır ve hatasız olsun. Kural ve denge sayılarına dokunulmayacak.
Bölüm 1: menüye dönünce ya da yeniden başlayınca arka plan eski şeytanda takılı kalıyor. Seçim ekranında vurgu ile arka plan
her zaman aynı olsun; masa portreyle aynı anda doğru salonu ve modu göstersin. Önce yeniden üret, sonra düzelt, regresyon testleri yaz.

### Yeniden üretme
- `SalonView`'a ekrandaki gerçek durumu gösteren `ShownDealerId` / `ShownMode` eklendi. Plandaki dört senaryo (7–10) PlayMode
  testi olarak yazıldı. **Dördü de düzeltmeden önce geçti**: hata batchmode'daki bu akışlarda yeniden üretilemedi.
- Koddaki zayıf noktalar planın şüphelendiği yerlerle aynı:
  - `SetSalon` id'yi geçiş bitmeden güncelliyor ve "aynı şeytan" kontrolüyle erken dönüyor. Geçiş coroutine'i ölürse resim eski kalır,
    sonraki çağrılar da yutulur.
  - Masada salon sıralayıcı kuyruğuna giriyordu. Kuyrukta takılan bir adım (exception) bütün kuyruğu kalıcı olarak kilitliyordu
    (`_running` true kalıyordu).
- Bunlar kalıcı olarak kapatıldı (aşağıda).

### Yapılanlar
- `AnimationClock` (yeni): tüm animasyonların tek saati. `Speed` (Bölüm 2'deki hız ayarı için) ve `IsSkipping` (atlama için).
  `Tween.Run` ve yeni `Tween.Wait` bu saatle çalışıyor; `WaitForSeconds` kaldırıldı.
- `AnimationSequencer`:
  - `Complete()`: kuyruktaki her şeyi anında son durumuna götürür.
  - Hata veren adım loglanıp atılıyor, kuyruk asla kilitlenmiyor.
  - Pasif nesnede `Play` her şeyi hemen bitiriyor.
  - `IsBusy` artık gerçek duruma bakıyor.
- `SalonView`:
  - İstenen (`DealerId`/`Mode`) ile gösterilen ayrı tutuluyor.
  - Geçiş yokken ikisi her karede eşitleniyor (güvenlik ağı); `OnDisable` yarıda kalan geçişi tamamlıyor.
  - `SetSalon(id, mode, wipe)` salonu ve modu birlikte ayarlıyor, geçiş doğru resmi açıyor.
- `TableView.Stage.SetDealer`: yeni masada bekleyen animasyonlar bitiriliyor. Salon **normal modda** ve portre aynı anda,
  doğrudan ayarlanıyor (kuyruğa girmiyor). Önceki koşudan hell / soul modu taşınmıyor; presenter doğru modu yeniden uyguluyor.
- `DealerSelectView`:
  - Açılışta vurgulu şeytanın salonu geçişsiz, ilk karede gösteriliyor (yeni oyunda ilk şeytan, masa değiştirirken şu anki şeytan).
  - Portre tıklamaları geçişli.
  - Eski kartlar `Destroy` beklerken aynı karede de pasif (yinelenen isimli buton kalmıyor).
  - `SelectedIndex` eklendi.
- "Şu anki salon" tek yerde: masanın salonu `TableView`'da. Seçim ekranının kendi salonu sadece önizleme, kapanınca görünmez.
  Menünün kendi taş zemini var.

### Testler
- PlayMode `SalonRegressionTests`:
  - Lilith'e bak → Esc → New Game: ilk şeytan vurgulu ve arka planda.
  - Change Table → Lilith'e bak → Geri → Continue: Mammon.
  - Son 250 yıl ve ruh bölgesinden New Game → Belial: Belial, mod Normal.
  - Masa değişimi anında Lilith.
  - Hızlı gezinip geçiş ortasında çıkma.
- EditMode `AnimationSequencerTests`: Complete sırası, boşta `Do`, hata veren adım, hız alt sınırı.
- **265 EditMode + 8 PlayMode geçiyor.**

### Açık sorular
- Hata kullanıcının gördüğü akışta tam olarak hangi adımda oluşuyordu? Yeniden görülürse adımları yazarsa yeni bir test eklenir.

---

## 2026-10-01 — Plan 3, Bölüm 2: akıcılık ve kontrol

### Yapılanlar
- **Atla (11):**
  - Animasyon sürerken basılan her tuş / buton / kart tıklaması `ITableView.SkipAnimations()` çağırıyor; masanın arka planına tıklamak da öyle.
  - Kuyruk anında son durumuna gidiyor: kartlar iniyor, sayaç ve ruh barı varıyor, şeytanın cümlesi tamamlanıyor.
  - **Karar:** o basış atlamaya harcanıyor, aksiyon üretmiyor. Böylece oyuncu görmediği bir masada pas / artırma yapmıyor.
    Animasyon bitince her basış normal çalışıyor; geçerli basış yutulmuyor (14).
  - PlayMode testi: dağıtım sırasında basınca her şey yerinde, faz hâlâ ilk karar.
- **Hız (12):** `AnimationClock.Speed` = Normal ×1 / Fast ×2 / Very Fast ×4.
  Tween'ler, beklemeler, sayaç, ruh barı, şeytanın yazısı ve ekran geçişi bu hıza uyuyor.
- **Esc (13):** `IMenuCommands.GoBack()`:
  - uyarı kutusu → kapanır;
  - kurallar / ayarlar → menü;
  - seçim → geldiği yer (yeni oyunda menü, masa değiştirirken masa);
  - masa → menü; menü → devam eden koşu.
  Her alt ekranda görünür bir BACK butonu var.
- **Kilitli butonlar (14):**
  - RAISE / PASS kilitliyken soluk görünüyor ama tıklanabiliyor; presenter nedenini `UiText`'ten söylüyor
    ("TABLE FULL — …", "ALL IN — …", ruh bölgesinde "ALL OF IT — …", "No passing under 250 years…", re-raise'de "call / match it — or fold").
  - Core'daki İngilizce gerekçe metinleri artık oyuncuya gösterilmiyor.
  - Klavye: bir karede en fazla bir aksiyon.
- **Ekran geçişleri (15):**
  - `ScreenTransitionView`: ekran siyah başlıyor, perde 8 px'lik satırlarla kalkıyor (salon geçişiyle aynı dil).
  - Yeni ekran hemen yerinde; perde hareket ederken tıklamaları yutuyor, klavye `IsTransitioning` ile bekliyor.
- **Pencere (16):**
  - `UnityDisplayMode`: tam ekran (borderless) ya da ekrana sığan en büyük 480×270 katında pencere. Piksel ölçeği her durumda tam sayı.
  - Alt+Enter ayarı değiştiriyor. Unity'nin kendi Alt+Enter'ı kapatıldı (`allowFullscreenSwitch: 0`); pencere boyutlandırılabilir.
- **Ayarlar (17):**
  - `GameSettings` (PlayerPrefs, `ISettingsStore`; bozuk değer → varsayılan).
  - `SettingsView` + `SettingsPresenter`: Animation speed, Full screen, Hand guide (Bölüm 3), First-game tips (sıfırla).
  - Menüye SETTINGS eklendi. Menü butonları artık iki sütunda, görünenlere göre diziliyor; QUIT altta ortada.
- **Buton hissi:** `ButtonFeel` (hover'da altın yazı, basınca 1 px içe, kilitli görünüm). Bölüm 4'teki 25. madde de bununla karşılandı.
- **Testler:**
  - Ayarlar: varsayılanlar, kaydet / yükle, bozuk değer, hız döngüsü, ipucu sıfırlama, presenter uygulaması, Alt+Enter.
  - Esc zinciri; geçişlerin her ekran değişiminde oynaması; kilitli buton mesajları; atlama.
  - **282 EditMode + 9 PlayMode geçiyor.**

---

## 2026-10-01 — Plan 3, Bölüm 3: oyuncuya yol gösterme

### Yapılanlar
- **El adı (18):**
  - Core'da `VisibleHandReader` ve `IHellPokerGame.PlayerHandNow`. Az kartla sadece aynı değerli kartlar birleşir;
    beş kartta tam değerlendirme (Dead Man's Hand dahil).
  - Masada oyuncu başlığı "NOW: ONE PAIR"; her açılan kartta ve kart değiştirdikten sonra güncelleniyor.
  - Showdown'da iki el de adıyla, kazananın adı yanık ve "… WINS" ile işaretli.
- **Kart değiştirme ipucu (19):**
  - `IHellPokerGame.SuggestedDiscards()` kasanın kendi mantığını (`HouseDrawStrategy`) oyuncunun eline uyguluyor.
  - Tutulacak kartların çevresinde 1 px altın çerçeve yavaşça parlıyor (`IHandView.SetHints`, karıştırma yok).
  - Sadece öneri; Ayarlar ▸ Hand Guide ile el adıyla birlikte kapanıyor.
- **El sıralaması (20):**
  - `HandRanksPanel`: en güçlüden aşağı (Dead Man's Hand en üstte), örnek kartlar (♠♣♥♦ fontta var) ve şeytanın çarpanları.
  - Masada H tuşu ya da HANDS butonu (MENU'nün yanında) açıp kapatıyor; Esc önce paneli kapatıyor.
  - Kurallar ekranında RULES / HANDS sayfaları (standart çarpanlarla); Esc önce kurallar sayfasına dönüyor.
  - Kısayol satırı kısaltıldı, "H hands" eklendi.
- **İlk oyun ipuçları (21):**
  - İlk karar, ilk draw, ilk re-raise, son 250 yıl ve ruh bölgesinde şeytan tek satırlık bir açıklama yapıyor.
    Re-raise / son 250 / ruh anlarında o anki normal repliğin yerine geçiyor.
  - Her biri bir kez gösteriliyor (`GameSettings.TipsSeen`, PlayerPrefs); Ayarlar'dan sıfırlanabiliyor.
  - Ruh ipucunda sayı yok.
- `TablePresenter(createGame, view, IGuideSettings guide = null)`: rehber ayarı yoksa el rehberi açık ve ipucu yok (eski testler aynen geçerli).
- Batchmode'da (testler, ekran görüntüleri) ayarlar bellekte tutuluyor; testler oyuncunun kendi PlayerPrefs'ine dokunmuyor.
- **Testler:** `GuideTests` (görünen el okuma, oyunun el adı ve önerisi, başlık, rehber kapalı, ipucu çerçevesi, showdown başlıkları,
  el sıralaması aç / kapa, ipuçlarının bir kez ve sıfırlanınca yeniden gösterilmesi, re-raise / son 250 / ruh ipuçları).
  **304 EditMode + 9 PlayMode geçiyor.**

---

## 2026-10-01 — Plan 3, Bölüm 4: his ve geri bildirim (ses hariç)

### Yapılanlar
- `TableMoment` (BigLoss / GoodHand / DeadMansHand) ve `ITableView.PlayMoment`.
  Presenter sadece anı adlandırıyor; efekti `TableMoments` (görünüm) seçiyor.
- **Büyük kayıp (22):**
  - En az `TablePresenter.BigLossUnits` = 4 birimlik kayıpta (ya da çekilmede) ekran tam piksel adımlarla sarsılıyor (3 px'e kadar, ~0.35 s).
  - Ceza sayacı 1.2 sn kırmızı yanıp sönüyor.
- **İyi el (23):**
  - Two Pair ve üstüyle kazanınca el adı 16 px piksel yazıyla ortada beliriyor ("FULL HOUSE!"): iki sert yanıp sönme, sonra kayboluyor.
  - Dead Man's Hand: ekran kararıyor (sadece oyuncunun kartları aydınlık), A♠ A♣ 8♠ 8♣ sırayla parlıyor.
    Şeytan zaten `angry` animasyonunda (Absolved repliği Annoyed).
- **Sayaç (24):** 0.8 sn ease-out sayma korunuyor; artık hız ayarına uyuyor ve "atla"da hemen varıyor.
- **Buton (25):** Bölüm 2'deki `ButtonFeel` (hover + 1 px basılma).
- **Uyum (26):**
  - Bütün efektler `AnimationClock` ile çalışıyor (hız ayarı).
  - Sarsıntı ve Dead Man's Hand sahnesi sıralayıcıda, atlanınca son duruma geçiyor. El adı yazısı ve kırmızı sayaç atlamada kapanıyor.
- **Testler:** `MomentTests` (büyük kayıp sarsar; küçük kayıp sarsmaz; Two Pair+ yazısı; One Pair yazısız; Dead Man's Hand kart sırası).
  **309 EditMode geçiyor.**

---

## 2026-10-01 — Plan 3, Bölüm 5: kayıt ve oyun sonu

### Yapılanlar
- **Otomatik kayıt (27):**
  - Core'da `RunSnapshot` (şeytan, ceza, el sayısı + `RunStats`). Ruh durumu ayrıca saklanmıyor; ceza ve şeytanın çizgisinden türüyor.
  - Format "key=value" satırları, ilk satır `v=1`. Bozuk, eski / başka sürüm ya da imkânsız sayı → `TryDecode` false; kayıt silinip yok sayılıyor.
  - Deste kaydedilmiyor: devam edilen koşu yeni karılmış desteyle başlıyor.
  - `RunArchive` (PlayerPrefs; batchmode'da süreç boyu tek bir bellek deposu) her el bitiminde, yeni koşuda ve masa değişiminde kaydediyor.
  - Oyun açılınca kayıt varsa `TablePresenter.Resume` aynı şeytan, ceza, el sayısı ve hikâyeyle devam ettiriyor; menüde Continue hazır.
  - Bilinmeyen şeytanlı kayıt atılıyor. İlk oyun ipuçları zaten ayarlarda (PlayerPrefs) kalıcı.
- **Oyun sonu (28):**
  - Absolved / Damned ekranı (`EndScreenView`): altın "ABSOLVED" taş duvarda, kırmızı "DAMNED" yanan duvarda.
  - Ekranda el sayısı, en düşük ceza, en yüksek ceza (ruh masaya konduysa sayı yerine "past the soul line"), en iyi el,
    oturulan masalar, ruhun masaya konup konmadığı. NEW GAME ve MENU butonları; Esc menüye dönüyor.
  - Masada oyun bitince buton "THE END"; basınca `IRunSession.RunEnded` ile oyun sonu ekranı açılıyor
    (dinleyen yoksa eskisi gibi yeni koşu başlıyor).
- **Rekorlar (29):**
  - Core'da `RecordBook`: başlatılan koşu, aklanma, lanet, şeytan başına aklanma, en hızlı aklanma (el). Bozuk kayıt → boş defter.
  - Menüye RECORDS eklendi (`RecordsView`).
- Menü butonları: Continue, Change Table, New Game, Settings, How to Play, Records (iki sütun), Quit altta.
- **Testler:**
  - `SaveTests`:
    - format: gidiş-dönüş; 8 bozuk / eski sürüm örneği yok sayılıyor; rekor gidiş-dönüş ve bozuk defter; istatistikler;
    - akış: yeni koşu kaydı ve sayımı, el sonu kaydı, masa değişimi kaydı, kaldığı yerden devam (Lilith'te ruh geri masada),
      okunamayan kayıt siliniyor, oyun sonu rekorları ve kaydı silme, `RunEnded` özeti, dinleyici yokken yeni koşu.
  - Menü: oyun sonu ekranı, oradan yeni oyun ve menü (Continue yok), rekorlar ekranı.
  - **PlayMode `RunJourneyTests`:** yeni oyun → 2 el → masa değiştir → menü → Continue → sahneyi yeniden yükle (kapat / aç)
    → Continue → aynı şeytan, ceza ve salon → bir el daha. Hata logu olursa test düşüyor.
  - **332 EditMode + 10 PlayMode geçiyor.**

### Açık sorular
- Kayıt sadece eller arasında alınıyor. El ortasında oyunu kapatan oyuncu o eli hiç oynanmamış sayıyor (kayıp da kazanç da yok).
  Bunun bir "kaçış yolu" olarak kapatılması istenirse dağıtımda ante'yi kayıp sayan bir kayıt eklenebilir.

---

## 2026-10-01 — Plan 3, Bölüm 6: kontrol turu

### Yapılanlar
- **Testler (30):** 333 EditMode + 11 PlayMode geçiyor (atlananlar `[Explicit]` simülasyon ve ekran görüntüsü).
  Yeni özelliklerin testleri: atlama (EditMode + PlayMode), kayıt / yükleme, ayarlar, el adı göstergesi, ipuçları, anlar, gezinme.
- **Ekran görüntüleri (31):** `Screenshots/`.
  - Menü, ayarlar, kurallar + el tablosu, şeytan seçimi, masada el tablosu, karar (el adıyla), draw ipucu, re-raise.
  - Sonuçlar, iyi el yazısı, büyük kayıp (sarsıntı anı), her salon, son 250, ruh bölgesi (11–20).
  - Oyun sonu (21 Absolved, 22 Damned), rekorlar (23).
  - Not: 21–23 test kolaylığı için ceza doğrudan 0 / lanete çekilerek alındı, el oynanmadı. Bu yüzden oradaki istatistikler
    (el sayısı, rekor sayıları) gerçek bir koşuyu yansıtmıyor.
- **Görüntülerde bulunan ve düzeltilen hatalar:**
  1. **Her şey siyahtı:**
     - Neden: Bölüm 2'deki geçiş canvas'ı `UiFactory.CreateScreen`'in tam ekran siyah "Letterbox" katmanını da kuruyordu ve en üstte
       (sıra 300) bütün oyunu örtüyordu. Gerçek oyunda da ekran siyah kalacaktı.
     - Düzeltme: `CreateScreen(..., letterbox: false)`.
     - Regresyon testi: `ScreenTransition_CoversOnlyWhilePlaying`. Kural CLAUDE.md'ye yazıldı.
  2. **Oyun bir el oynanmadan bitmişken sonuç ekranı çöküyordu** (NullReference). Örnek: bitmiş bir kayıttan devam.
     `ShowResult` artık el yoksa sadece oyun sonunu gösteriyor. Test: `ASaveOfAFinishedRun_ShowsTheEnd_WithoutCrashing`.
  3. **Büyük el yazısı sonuç mesajıyla üst üste biniyordu.** Altın çerçeveli siyah bir şerit üzerine alındı.
  4. **Test yardımcısı aynı isimli pasif butonu buluyordu** (yeni seçim ekranında eski kartlar artık anında pasif); aktif olan tercih ediliyor.
- **Loglar (33):**
  - PlayMode ve ekran görüntüsü loglarında hiç uyarı / hata yok.
  - EditMode logundaki istisnalar bilerek hata fırlatan testlerin beklenen çıktıları ("broken step", "disk on fire").
  - Derleyici uyarısı yok.
- **CLAUDE.md (34):** yeni ekranlar, Esc zinciri, akıcılık, yol gösterme, his, kayıt formatı, rekorlar, ayarlar, batchmode deposu,
  mimari ağaç, letterbox kuralı ve kısayollar güncellendi.

### Sonraya (plan dışı, dokunulmadı)
Günahkâr sınıfları, şeytan hileleri, ses / müzik, lanetli emanetler, eller arası event'ler, final şeytanı ve açılabilir içerik.

### Açık sorular
- El ortasında oyunu kapatmak o eli yok sayıyor (Bölüm 5'e bakın).
- Bölüm 1'deki salon hatası batchmode'da yeniden üretilemedi. Sağlamlaştırıldı; tekrar görülürse adımlar yazılsın.

---

## 2026-10-02 — Oturum 3: mühür (sealed pact), CHECK TO DRAW, el ortasında kapatma açığı

### İstek (kullanıcı)
"Bahis akışında gereksiz tıklamaları kaldıralım", kural ve denge sayıları değişmeden:
1. Tavana ulaşınca (ya da all-in) el mühürlensin; kalan Pas / Çekil kararları sorulmasın, Core geçsin. Draw yine sorulsun.
   Re-raise karşılanıp tavan geçilirse de mühür. Son 250 ve ruh bölgesiyle uyumlu.
2. "THE PACT IS SEALED" + şeytan repliği, butonlar gizli, kalan kartlar tek tek (~0.6 sn) açılsın, Space / tık hızlandırsın.
3. Pas dışında anlamlı seçenek yoksa karar atlansın (genel kural).
4. Pas'ın yanına CHECK TO DRAW (D); son 250'de kilitli.
5. Simülasyon tekrar, sonuç DEVLOG'a. 6. Testler, CLAUDE.md el akışı.
Sonra: el ortasında oyunu kapatma açığı kapansın. DEAL'da "el sürüyor" kayda yazılsın; açılışta o el o anki bahis / draw durumuna göre
çekilmiş sayılsın (ruhta ×1.5), şeytan özel replik söylesin; mühürlü elde bahsin tamamı kaybedilsin.

Not: Önceki sohbet bu işte hata vererek kesilmişti. Kodda ve DEVLOG'da o denemeden hiçbir iz yoktu, iş sıfırdan yapıldı.

### Yapılanlar — Core
- `HellPokerGame`:
  - `IsCommitted` (mühür). `NoteCommitment()` deal, artırma (re-raise gelmediyse) ve Call'dan sonra bakar:
    `CurrentStake >= TableCap || WagerLeft == 0`. Bayrak sonraki dağıtıma kadar kalır (sonuç ekranında da true).
  - `SkipEmptyDecisions()`: karar fazında Pas dışında gerçek seçenek yoksa (`HasRealChoice`) Core geçer, `DecisionsSkipped` sayar.
    Deal, karar sonrası (`Continue`) ve `Draw` sonrası çalışır. Draw hiç atlanmaz.
  - Mühürlüyken `CanBet` Pas / Çekil / Artır / Call'u "The pact is sealed" ile reddeder (draw fazında da).
  - `HouseReRaise` fazında `IsCommitted` false: re-raise yeni bir bahis, Call / Fold sorulur.
  - `CanCheckToDraw` / `CheckToDraw()`: sadece `PlayerReveal`'da ve Pas serbestken; draw'a kadar Pas'lar.
  - `CurrentHand` (`HandInProgress`: stake, ante, draw, soul, sealed; el yokken null) ve `ForfeitHand(hand)` (sadece eller arasında).
- `RunSnapshot`: isteğe bağlı `hand.*` satırları. `v=1` korundu: eski kayıtlar el yokmuş gibi okunur.
  Bozuk el satırı tüm kaydı geçersiz yapar (silinir).

### Yapılanlar — sunum
- `TablePresenter`:
  - Her komuttan önce masanın durumu alınır (`TableState`). Sonra `PlayOutSealedHand`:
    - mühür anında `AnnounceSeal` (flare + `SealedMessage` + şeytanın `Sealed` repliği, Gloating);
    - draw'dan önce oyuncunun kalan kartları, sonra kasanın kartları tek tek, aralarında `SealedRevealPause` = 0.6 sn.
  - Hepsi sıralayıcı kuyruğunda, bu yüzden her basış / tık mevcut atlama ile sona götürür.
  - Mühürlü draw ekranı "The pact is sealed. Pick up to N cards…" der.
  - `CheckToDraw()` (+ son 250'de "No checking under 250 years" uyarısı); `BetControls.ShowCheckToDraw / CanCheckToDraw`.
  - `SaveRun()` artık her `Refresh`'te: eller arasında ya da el sürerken (`CurrentHand` ile). Sonuçlanan eli `SettleHand` kaydeder (el yok).
  - `Resume`: kayıtta el varsa `ForfeitLeftHand`: ceza, istatistik (el sayılır, round zaten dağıtımda sayılmıştı), rekor (lanetse),
    mesaj (`FledFormat` / `FledSealedFormat`; ruhta sayısız `FledSoul` / `FledSealedSoul`), şeytanın `Fled` repliği.
    Ardından kayıt el olmadan yazılır: tekrar kapatıp açmak ikinci kez ceza kesmez.
- `TableView`: CHECK TO DRAW butonu. Görünürken dört buton yan yana (RAISE 88 · PASS 40 · CHECK/TO DRAW 64 · FOLD 40 px).
  Başlık fontunda tek satır sığmadığı için etiket iki satır.
- `TableMoment.PactSealed` (GoodHand'in yazı efektini kullanır), `KeyboardInput` D tuşu, `ITableCommands.CheckToDraw`, `ITableView.CheckToDrawPressed`.
- `UiText`: mühür, check-to-draw, kaçış metinleri; kurallar sayfasına mühür cümlesi; kısayol satırına "D check to draw".
  `UiText.Dealers`: her şeytana 3 `Sealed` ve 2 `Fled` repliği.

### Kararlar ve nedenleri
- **Tavana çıkan artırmaya re-raise hâlâ gelebilir** (oyuncu Call / Fold der). Mühür oyuncunun kendi bahis kararlarını kapatır.
  Re-raise kasanın yeni bahsi ve kural gereği tavanı aşabilir; bunu kaldırmak dengeyi değiştirirdi. Karşılanınca el mühürlenir.
- **Son karardaki artırmayla dolan masa duyurulmaz:** açılacak kart / atlanacak karar yoksa "PACT IS SEALED" anlamsız.
  Draw'dan önceki son kartta dolarsa duyurulur, çünkü draw sonrası kararlar atlanacak.
- **Mühürlü elde kapatma = en zayıf ele kayıp:** `bahis × LossPercent × ruh çarpanı`, yukarı yuvarlanır
  (`GetLeastYearsAdded`, çarpan ×1). "Bahsin tamamı kaybedilmiş sayılsın" isteğini kayıp kuralıyla okudum, o yüzden Lilith'in ×1.25'i dahil.
  Mühürsüz elde ceza normal çekilme cezası (`GetFoldPenalty`): draw öncesi / sonrası yüzdesi, ruhta ×1.5.
- Kayıtta ceza, elin dağıtıldığı cezadan hesaplanır; `rounds` o eli zaten içerir.
- `LockedTableFull` / "TABLE FULL" etiketi artık normal akışta görünmüyor (tavan = mühür = buton yok). Metinler zarar vermediği için duruyor.

### Testler
- Yeni `PactTests` (Core, 23):
  - mühür: tavanda ve all-in'de mühür, mühürlüyken her bahis reddi, draw'un sorulup elin showdown'a gitmesi, mühürün sonraki dağıtıma kadar sürmesi;
  - re-raise: tavana çıkan artırmaya re-raise cevabı, tavanı geçen Call'da mühür, tavan altındaki Call'da son kararın kalması;
  - son 250 ve ruh all-in;
  - CHECK TO DRAW: geçiş, artırmadan sonra, sadece draw öncesi, son 250'de kilit;
  - `CurrentHand`; `ForfeitHand` (önce / sonra, ruh ×1.5, mühürlü × LossPercent, mühürlü ruh, lanet, sadece eller arasında).
- Yeni `PactPresenterTests` (24 test case):
  - duyuru ve replik; kartların tek tek ve beklemeli açılması; kasanın 0→5 açılışı; son kararda duyuru olmaması; ruhta sayı yok;
  - CHECK TO DRAW butonu (görünür / draw sonrası yok / son 250 kilidi + mesaj / animasyon sırasında sadece atlatma);
  - kayıt: DEAL'da, bahis ve draw ile güncelleme, sonuçlanınca el yok;
  - devam: çekilme (önce / sonra), mühürlü (Lilith ×1.25), ruh (sayısız), kayıt temizlenmesi, lanet + rekor;
  - format: gidiş-dönüş, bozuk el satırları, el satırı olmayan kayıt.
- Güncellenen: tavan / son 250 / re-raise Core testleri ve üç presenter testi. Eski "TABLE FULL kilitli mesajı" testi
  "artırmayla dolan masa eli oynatır" testine dönüştü.
- PlayMode:
  - `ASealedHand_PlaysOutOnItsOwn_WithoutBetButtons` (her karede Pas / Fold gizli, kasa açılışı >2 sn);
  - `ASealedReveal_CanBeHurried`;
  - `RunJourneyTests.ClosingMidHand_AndReopening_ForfeitsTheHand` (deal + raise → sahne yeniden yüklenir → 1100 yıl;
    tekrar açınca yine 1100).
- **380 EditMode + 14 PlayMode geçiyor**; derleyici uyarısı yok.
- Ekran görüntüleri: `05_decision` (dört buton), `07c_pact_sealed` (eski `07c_house_reveal` yerine).
  İlk görüntüde etiketler üst üste biniyordu, iki satırlı etiketle düzeltildi. Mühürlü açılışta sayaç re-raise öncesi değerde
  kalıyordu, düzeltildi.

### Simülasyon (2000 koşu, aynı seed'ler; baz çizgi aynı kodda mühür geçici kapatılarak ölçüldü, önceki DEVLOG değerleriyle birebir aynı)
| Şeytan | Aklanma (önce → sonra) | Ort. el (önce → sonra) | Lanet | Ruhu masaya koyan |
|---|---|---|---|---|
| Mammon | %88.0 → **%88.0** | 55.1 → **49.4** | %12.1 → %12.1 | %21.1 → %20.9 |
| Belial | %80.5 → **%80.5** | 25.4 → **24.3** | %19.6 → %19.5 | %31.0 → %31.0 |
| Lilith | %64.4 → **%64.2** | 38.0 → **39.2** | %35.6 → %35.9 | %51.9 → %52.4 |

- **Aklanma oranları değişmedi** (Lilith −0.2 puan, gürültü düzeyinde). Kullanıcının kendi simülasyonuyla uyumlu.
- Değişen el sayısı: Mammon ~6 el kısaldı. Sim oyuncusu eskiden tavandayken bazen çekiliyordu (draw sonrası kasa çift gösterince
  high card). Artık bu eller showdown'a gidiyor; tavandaki eller daha büyük sonuçlanıyor, koşu daha çabuk bitiyor.
  Bu, önceki oturumdaki "Mammon uzun (55 el, hedef ~43)" açık sorusunu kendiliğinden kısmen yaklaştırıyor.

### Açık sorular
- Mühürlü elde kapatma cezasında Lilith'in ×1.25'i uygulanıyor (kayıp kuralı). Sadece düz bahis (×1) isteniyorsa `ForfeitHand`'de tek satırlık değişiklik.
- Mammon'un el sayısı 49'a indi. Hedef ~43 hâlâ isteniyor mu?

### Sıradaki adımlar
- Kullanıcı GitHub Desktop'tan commit / push yapmalı. Yeni dosyalar: `HandInProgress.cs`, `PactTests.cs`, `PactPresenterTests.cs`
  (+ Unity'nin ürettiği .meta dosyaları).
- Oyunda elle deneme: tavana çıkıp draw → kasanın kartlarının temposu; Fast / Very Fast hızlarında his.

---

## 2026-10-02 — Güncelleme Planı 4 (Final Boss: Lucifer), Bölüm 1: kurallar (Core)

### İstek (kullanıcı)
Ceza 250'nin altına inince oyuncu nerede olursa olsun Lucifer'in masasına çağrılsın. Aklanmanın tek yolu onu yenmek
(Dead Man's Hand istisna: "Wild Bill" sonu). Masadan kalkılamaz. Kendi ev kuralları ve sabit ölçeği (50 / 150) var.
Masasında ceza 250'yi geçerse en az 500 ile gelinen şeytana düşer, tekrar inince yeniden çağrılır. Bölüm 1: kurallar ve testleri.

### Yapılanlar
- `DealerRoster.Lucifer` (id `lucifer`):
  - 3 kart değiştirir, 0 kart gösterir, standart çarpanlar, kayıp ×1.25, çekilme %100 / %100, re-raise Two Pair+ %80 / blöf %25.
  - `DealerRoster.All` hâlâ seçilebilen üç şeytan. `DealerRoster.Find(id)` Lucifer dahil hepsini bulur.
- `Dealer`: isteğe bağlı `Stakes` (`StakeScale.Fixed(50, 150)`: birim 50, tavan 150, daha azı all-in) ve `IsFinalTable`.
  `ApplyTo` bunları `GameRules.WithHouseRules`'a taşır. Son masada `ForcedRaiseYears` = 0: son 250 kuralı yok.
- `GameRules`:
  - `LuciferGateYears` = 250 (0 = Lucifer yok), `LuciferCastDownYears` = 500 ve `IsFinalTable`.
  - `KeepsTheLastYear`: Lucifer varken sıradan masa.
- `HellPokerGame`:
  - Sıradan masada kazanç cezayı bitiremez, son yıl kalır. Dead Man's Hand (`IPayoutTable.IsAbsolution`) hariç.
  - Lucifer'in masasından kalkılamaz (`CanLeaveTable`).
- `LuciferGate` (yeni, Core): koşunun Lucifer'le ilişkisi.
  - `Check(years, phase)` sadece Betting'de ses verir: Stay / Summoned / CastDown.
  - `Summon(origin)` denemeyi sayar; `CastDown(years)` → max(years, 500).
  - `IsAtLucifer`, `OriginDealerId`, `Attempts`, `CastDowns`.
- `RunSnapshot` **v=2**: `lucifer`, `origin`, `attempts`. v=1 kayıtlar Lucifer'i hiç görmemiş koşu olarak okunuyor.
- `TablePresenter` (oturum; görseller Bölüm 2'de):
  - İsteğe bağlı `finalDealer`; bootstrap `DealerRoster.Lucifer` verir, vermeyen testlerde Lucifer yok.
  - Her `Refresh` başında `PassThroughGate()`:
    - çağrılma: eski şeytanın `Farewell`'i, `SeatChange.Summoned`, Lucifer'in karşılaması (2. ve sonraki denemede `Remembers`), ilk seferde ipucu;
    - düşüş: Lucifer'in `CastDown`'u, `SeatChange.CastDown`, eski şeytanın `Returned`'ı.
  - Masadan kalkma isteğine Lucifer kendi sesiyle hayır der; buton "NO ESCAPE" (`LeaveState.Summoned`).
  - Kayıtlar Lucifer durumunu taşır. `Resume(dealer, snapshot, origin)`.
  - El ortasında kapatma cezası da kapıdan geçer: Lucifer masasında düşürebilir.
  - Ölüm / aklanma sonrası yeni koşu (dinleyici yoksa) Lucifer'de değil, gelinen şeytanda başlar.
  - Bootstrap: Lucifer masasında olup nereden geldiği bilinmeyen kayıt silinir.
- `IDealerView.SetDealer(card, SeatChange)`: masa değişiminin türü görünüme gider (sahneler Bölüm 2'de).
- `BalanceSimulation`: kapıyı oynatır (çağrılma / düşüş, her masa yeni seed). Yeni sütunlar: Lucifer'e ulaşan, ilk denemede yenme
  (ulaşanların), ortalama deneme (ulaşanların), toplam düşüş, Wild Bill. `HELLPOKER_LUCIFER_GATE` / `HELLPOKER_CAST_DOWN` ortam değişkenleri.

### Kararlar
- **Sıradan masada son yıl kalır.** "Aklanmanın tek yolu Lucifer" kuralı, 250'nin üstünden tek elde 0'a inen büyük bir kazancı
  (Royal vb.) da kapsamalıydı. Plan bunu söylemiyordu. Kazanç en fazla cezayı 1 yıla indirir, oyuncu sonra çağrılır.
  Dead Man's Hand bunun dışında (Wild Bill). Çok az yılla gelen oyuncu Lucifer'de çok avantajlı (1 yılla all-in, kayıp çok küçük).
  Simülasyonda nadir.
- Son 250 kuralı ve "cehennem ateşi" modu artık sıradan masalarda pratikte hiç görülmüyor: 250'de zaten çağrılıyor.
  Lucifer'in masasında da yok. Kod duruyor (`luciferGateYears: 0` ile Lucifer'siz oynanınca çalışır). Lucifer masasının
  "son anlar"ı Bölüm 2'de ayrıca tanımlanacak.
- Çağrılma kontrolü `Refresh`'te ve sadece Betting'de: sonuç ekranında değil, NEXT HAND'e basınca (ya da masaya oturunca /
  devam edince) olur.

### Testler
- `LuciferTests` (Core, 32 case):
  - kadro, ev kuralları, sabit birim / tavan / all-in / kalanla sınırlı tavan, 0 kart gösterme, son 250 yok, kalkma kilidi, ×1.25 kayıp;
  - masasında zafer, sıradan masada son yıl, Dead Man's Hand'in her yerde aklaması, Lucifer'siz kural;
  - kapı (eşik, eşik üstü, sadece eller arasında, 0), deneme, düşüş (260 / 499 / 500 / 735), yeniden çağrılma, hatalı kullanım;
  - kayıt (Lucifer'de, düşüşten sonra, v=1 uyumu, bozuk Lucifer satırları).
- `LuciferPresenterTests` (12):
  - 260'ta kazanıp NEXT HAND'de çağrılma (sonuç ekranında değil), kalkma reddi;
  - 500'ün üstünden ve altından düşüş, yeniden çağrılma ("Again. I remember…"), masasında zafer + rekor, Wild Bill;
  - 250 altında masaya oturmanın çağırması;
  - kayıt (Lucifer'de / düşüşten sonra), Lucifer'de devam ve düşüş, el ortasında kapatmanın Lucifer'de düşürmesi.
- **424 EditMode geçiyor.** Eski testlerin hiçbiri değişmedi.

### Simülasyon (2000 koşu, akıllı oyuncu)
| Gelinen şeytan | Aklanma (Lucifer'siz → Lucifer'le) | Plan | Ort. el | Plan | Lucifer'e ulaşan | İlk denemede yenme | Plan | Ort. deneme | Plan | Düşüş (toplam) | Wild Bill |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Mammon | %88.0 → **%86.7** | ~%87 | 31.2 | ~30 | %88.8 | %48.0 | ~%44 | 2.10 | ~2.2 | 1992 | 2 |
| Belial | %80.5 → **%79.3** | ~%74 | 18.2 | ~16 | %81.6 | %55.0 | ~%49 | 1.83 | ~1.9 | 1406 | 2 |
| Lilith | %64.2 → **%61.8** | ~%63 | 22.2 | ~24 | %68.8 | %47.5 | ~%46 | 1.94 | ~2.0 | 1441 | 3 |

- Mammon ve Lilith plana çok yakın.
- **Belial +5 puan sapıyor.** Neden: bizim Belial baz çizgimiz Lucifer'siz zaten %80.5; planın Python kopyasında "şimdi ~%77".
  Fark Lucifer'den önce de vardı. Lucifer'in Belial'e etkisi bizde −1.2, Python'da −3 puan.
  İlk denemede yenmenin de yüksek çıkması (%55): Belial'in yüksek ödemeleri oyuncuyu 250'nin epey altına indirip öyle çağırıyor.
  Lucifer az yılla daha kolay. **Kural değiştirilmedi.**
- Ortalama el sayısı çok düştü (Mammon 49 → 31): 250'nin altındaki uzun "son 250 yıl" süreci artık yok, oyuncu Lucifer'le bitiriyor.

### Açık sorular
- Sıradan masada son yılın kalması (yukarıdaki karar) uygun mu?
- Belial'in Lucifer'le aklanma oranı planın 5 puan üstünde. İstenirse düşüş cezası 750 denenebilir (plan: aklanma %72).

---

## 2026-10-02 — Plan 4, Bölüm 2: görünmeyen Lucifer (görsel)

### Yapılanlar
- **Portre** (`pixel_demons.py`, `lucifer_frames`), 96×96. Zifiri karanlık; içinde sadece:
  - iki yanan, yarık göz bebekli göz;
  - kutunun altından kıvrılan iki pençe ucu;
  - sağ üst köşeden taşan bir kanat kenarı.
  - Yüz, beden ya da siluet yok.
- **Durumlar:**

  | Durum | Kare | Ne oluyor |
  |---|---|---|
  | idle | 10 | yavaş kırpma, karanlıkta kayma |
  | talk | 4 | nabız gibi parlama |
  | gloat | 7 | kısılan gözler + beliren / kaybolan sırıtış çizgisi (dişler boşluk) |
  | angry | 5 | büyüyen gözler, kutu kan kırmızısı yanar |
  | reraise | 5 | tek karede kör edici beyaz parlama |
  | final | 6 | gözlerden damlayan ateş |
  | soul | 4 | soğuk gözler; ruh onun masasına hiç gelmiyor, yedek olarak duruyor |

- **Salon** (`pixel_salons.py`, `lucifer`):
  - Görünmeyen dev bir tahtın sadece alt basamakları ve kadrajdan çıkan iki ayağı; karanlığa uzanan zincirler.
  - Kızıl ışık, 4 karede yükselen korlar, koyu orta sütun.
  - `normal` ve daha sıcak `hell` (`HOTTER` haritası). `soul` yok, yedek zinciri normal'e düşüyor.
  - 4 kare, çünkü importer 2048 px üstündeki dokuyu küçültüyor.
- **UI** (`pixel_ui.py`):
  - `dialog_lucifer`: siyah kutu, cehennem turuncusu kenar.
  - `fade`: tam ekran Bayer desenleriyle ¼ / ½ / ¾ / tam siyah. Saydam karıştırma olmadan yavaş kararma.
- **Konuşması** (`DealerView`):
  - Lucifer masadayken isim "THE MORNING STAR" (kızıl), yazı kor turuncusu, kutu `dialog_lucifer`.
  - Her repliğinde ekran 1 px titrer (`TableScenes.Tremor`).
  - `DealerCard.IsFinalTable` taşınıyor. Kartta yeni özellik satırları: "Fixed stakes: ante 50, at most 150 on the table",
    "Shows none of his cards before the showdown".
- **Sahneler** (`TableScenes`, yeni). Her sahne iki kuyruk adımı:
  - içeri: eski şeytanın son sözü bitene kadar bekler, sonra değişim;
  - dışarı: yeni şeytan konuşmadan önce ya da masa hazır olunca (`End`).
  - Arada masanın diğer güncellemeleri (ödeme tablosu, sayaç, ruh) ekran karanlıkken / dışarıdayken oluyor.
  - **Çağrılma:** perde yok; dört Bayer adımıyla yavaş kararma, karanlıkta salon ve portre değişir, karanlık kalkar,
    gözler açılır ve Lucifer konuşur.
  - **Düşüş:** ekran yukarı kayarak (6 px'lik adımlar) düşer. Eski şeytanın salonu aşağıdan gelir, 3-2-1 px'lik sarsıntıyla iner.
  - Atlama (Space / tık) sahneyi sona götürür. Anında masa değişimi (yeni oyun vb.) yarım kalan sahneyi iptal eder (`Abort`).
- **Lucifer'in son anları:** ceza onun masasına sığacak kadar azsa (≤ 150, tek el bitirebilir):
  - salon `hell`, portre `final` (gözlerden ateş damlar);
  - "ONE HAND FROM FREEDOM" bandı ve bir kez "So close. I can hear you hoping.".
  - (Son 250 kuralının yerini tutan sahne. Kural yok, sadece his.)
- **Replikler:** Bölüm 1'de eklendi.
  - Lucifer: karşılama, `Remembers` (2. / 3.+ deneme), kazanç, kayıp, çekilme, re-raise, mühür, kaçış, düşüş, son anlar,
    kalkma reddi, zafer, lanet.
  - Diğer üç şeytan: `Farewell` ve `Returned`.

### Testler
- `LuciferArtTests`:
  - her durumun karelerinin en az %85'i paletin en koyu üç renginde ("hiç görünmez" kuralının otomatik kontrolü; angry hariç, o kutu kızıl);
  - salon varyantları ve 2048 px sınırı; soul yedeği; diyalog kutusu ve kararma şeridi.
- `LuciferPresenterTests`: eski şeytan kararmadan önce konuşur; son anlar 150'de başlar (200 / 151 / 150 / 40).
- PlayMode `SalonRegressionTests`: 200 yılda sıradan masada "hell" modu artık olmadığı için senaryo 140 yıl (Lucifer'in sıcak salonu)
  → yeni oyun → normal salon oldu.
- **439 EditMode + 14 PlayMode geçiyor.**
- Sahnelerin ekran görüntüsüyle kontrolü Bölüm 4'te (madde 26).

---

## 2026-10-02 — Plan 4, Bölüm 3: akış ve arayüz

### Yapılanlar
- **Seçim ekranı (18):**
  - `MainMenuPresenter(..., finalDealer)` Lucifer'i dördüncü, **kilitli** kart olarak ekler (`DealerChoice.IsLocked`).
  - Kartta karanlık portre (gözler), kızıl "THE MORNING STAR", "Waits below 250 years" ve soluk "LOCKED" butonu.
    Ruh çizgisi ve SAFE / SOUL AT STAKE yok.
  - Tıklanınca kimse oturmaz, ayrıntı panelinde onun repliği belirir: "Not yet. Come down to me." (`ShowLockedLine`).
  - Kart genişliği artık kart sayısına göre (dörtte 110 px). Başlık iki satıra sığıyor, ayrıntı paneli 4 px aşağı indi.
- **Masa (19):** Lucifer masasında sayacın altı "LUCIFER: ATTEMPT N", bar 250'ye doğru dolar ve altında "Cast down above 250" yazar.
  `ISentenceView.SetLimit / SetLabel`. Devam edilen kayıtta deneme sayısı kayıttan gelir.
- **Oyun sonu (20), `RunSummary.BeatLucifer / WildBill / LuciferAttempts`:**
  - Lucifer'i yenince "THE MORNING STAR FALLS".
    Solda kutuda gözleri bir an kör edici parlar (reraise animasyonu), 1.6 sn sonra söner; hikâyede "fell on attempt N".
  - Dead Man's Hand ile sıradan masada aklanma: "WILD BILL'S ESCAPE".
  - Lanet ekranında (ve diğerlerinde) "You faced the Morning Star N time(s)" ya da "You never met the Morning Star".
  - Uzun başlıklar 16 px'e iner.
  - Masadaki son mesaj da Lucifer'de "His eyes go dark. The gates of Hell open — you walk free.".
- **Rekorlar (21), `RecordBook`:**
  - Yeni alanlar: Lucifer'e ulaşma, Lucifer'i yenme, en az denemede yenme, Wild Bill kaçışları.
  - Satırlar isteğe bağlı: eski defter okunmaya devam eder.
  - Lucifer'de biten koşu, gelinen şeytana yazılır. Böylece "Freed at X's table" anlamlı kalır.
- **Kayıt (22):** Bölüm 1'de yapıldı (v=2, v=1 uyumu, testler).
- **İpucu (23):** ilk çağrılmada Lucifer kendi ağzından söyler: "Only I can set you free. Fall above 250 and you will be cast down."
  Sonraki çağrılmalarda `Remembers` repliği gelir.
- **How to Play (24):**
  - Kurallar sayfasına "THE MORNING STAR" bölümü (çağrılma, kendi ölçeği 50 / 150, kart göstermez, kalkılamaz, düşüş).
  - Artık geçerli olmayan "Under 250 years passing is forbidden" cümlesi çıkarıldı.
  - Dead Man's Hand'in her yerde anında aklattığı yazıldı.

### Kararlar
- **Wild Bill = sıradan masada Dead Man's Hand ile aklanma.** Daha önce Lucifer'e çıkıp düşmüş oyuncu da sayılır.
  Plan "Lucifer'i hiç görmeden" diyordu. Bu dar tanım yerine kaçışın kendisi sayıldı. Ekrandaki deneme satırı yine gösteriliyor.

### Testler
- `MainMenuPresenterTests`: dördüncü kilitli kart, seçilince kimse oturmaz + replik, masa değiştirirken de kilitli.
- `LuciferPresenterTests`:
  - zaferde özet ve rekorlar (gelinen şeytana yazılır);
  - Wild Bill özeti ve rekoru;
  - sayaç etiketi / çizgisi (1. deneme → düşüş → 2. deneme), kayıttan devamda deneme.
- `LuciferTests`: rekorların gidiş-dönüşü, Lucifer'den önceki defterin okunması.
- **446 EditMode geçiyor.**

---

## 2026-10-02 — Plan 4, Bölüm 4: kontrol

### Yapılanlar
- **PlayMode (25), `LuciferJourneyTests`:**
  - Gerçek sahne ve butonlar, yığılmış desteler (oyun fabrikası yansımayla değiştiriliyor).
  - Akış: 260'ta kazan → NEXT HAND'de çağrılma → Lucifer'de kayıp (260) → düşüş (500, Mammon) → kazan (250) → yeniden çağrılma
    ("ATTEMPT 2") → zafer → "THE MORNING STAR FALLS".
  - Her adımda sahnenin ekranda iz bırakmadığı (kararma kalktı, ekran yerinde) ve doğru salon kontrol ediliyor.
- **Ekran görüntüleri (26):**
  - Yeni kareler: 24 kilitli kart, 25 çağrılma (Bayer kararması), 26 Lucifer masası, 27 karar, 28 re-raise (kör edici gözler),
    29 son anlar, 30 düşüş anı, 31 iniş, 32 / 33 "Morning Star falls" (gözler yanık / sönmüş).
  - 21 artık "WILD BILL'S ESCAPE" (sıradan masada aklanma).
  - Eski `10_*_final_stretch` kareleri kaldırıldı: sıradan masada son 250 artık görülmüyor.
  - **Lucifer hiçbir görüntüde tam görünmüyor:** sadece gözler, pençe uçları, kanat kenarı.
- **Görüntülerde bulunan ve düzeltilen hatalar:**
  1. "THE MORNING STAR" başlık fontunda dar kartta ekranın sağından taşıyordu, masada iki satıra bölünüp başlığın üstüne biniyordu.
     Lucifer'in adı artık metin fontunda (kart ve masa).
  2. Düşüşte iniş sırasında Lucifer'in son anları (alevler, ateşli portre) ve "NO ESCAPE" eski şeytanın masasına taşınıyordu.
     Sahnenin değişim anında bunlar sıfırlanıyor.
  3. "THE MORNING STAR FALLS" başlığı oyun sonu panelinden 8 px taşıyordu; panel genişledi.
  4. Ekran görüntüsü aracı rastgele bir elde takılıyordu (`TakeOver` el ortasında). Kapı adımlarından önce masa eller arasına getiriliyor.
- **Simülasyon (27):** Bölüm 1'deki tabloyla birebir aynı (Core kuralları o günden beri değişmedi). Bkz. Bölüm 1.
- **CLAUDE.md:** oyun kuralları (Lucifer, son yıl, Wild Bill), şeytan tablosu (Lucifer satırı), denge tablosu, kayıt formatı v=2,
  oyun sonu / rekorlar, görsel kurallar (görünmeyen Lucifer, sahneler, 2048 px sınırı), mimari ağaç, testler.
- **Testler:** **446 EditMode + 15 PlayMode geçiyor.** Atlananlar `[Explicit]` simülasyon ve ekran görüntüsü. Derleyici uyarısı yok.

### Açık sorular (Plan 4'ün tamamı)
- Sıradan masada son yılın kalması (Bölüm 1 kararı) uygun mu?
- Belial'in aklanması planın 5 puan üstünde (%79 / plan %74). Düşüş cezası 750 denenebilir.
- Wild Bill tanımı: Lucifer'e çıkıp düşmüş oyuncunun Dead Man's Hand'i de "Wild Bill's escape" sayılıyor.
- Lucifer masasının adı altında "Waits below 250 years" de görünüyor (kartın başlığı). İstenirse masada boş bırakılabilir.

### Sıradaki adımlar
- GitHub Desktop'tan commit / push. Yeni dosyalar: `LuciferGate.cs`, `TableScenes.cs`, `LuciferTests.cs`, `LuciferPresenterTests.cs`,
  `LuciferArtTests.cs`, `LuciferJourneyTests.cs`, `Art/Demons/lucifer/*`, `Art/Backgrounds/lucifer/*`, `Ui/dialog_lucifer.png`,
  `Ui/fade.png` (+ .meta dosyaları).
- Oyunda elle deneme: çağrılma sahnesinin temposu (eski şeytanın son sözü + ~2.5 sn kararma), Fast / Very Fast'ta his.

---

## 2026-10-02 — Plan 4 açık sorularının cevapları

### İstek (kullanıcı)
1. Sıradan masada son yılın kalması doğru, kalsın. Ama hata sanılmasın: ceza sıradan masada 1'e inince şeytan
   "The last year is not mine to take. He is waiting." desin, sayacın altında kısa bir açıklama görünsün.
2. Belial'e dokunma, düşüş cezası 500 kalsın. Denge şeytan hileleriyle yeniden ayarlanacak.
3. Wild Bill'i daralt: sadece hiç çağrılmamış oyuncunun Dead Man's Hand'i "WILD BILL'S ESCAPE". Lucifer'den düşmüş oyuncununki
   normal ABSOLVED, ekranda "The Morning Star will remember this." satırıyla. Rekorlar buna göre.
4. Lucifer masasında adın altındaki "Waits below 250 years" boş kalsın; sadece seçim ekranındaki kilitli kartta görünsün.

### Yapılanlar
1. **Son yıl:**
   - Sonuç ekranında, Lucifer bekliyorken sıradan masada ceza 1 ise (`TablePresenter.IsHoldingTheLastYear`) şeytan normal el
     repliği yerine `UiText.LastYearLine`'ı söyler.
   - Sayacın altındaki satır "The last year is his" olur (`Sentence.SetLimit`).
   - NEXT HAND'de oyuncu 1 yılla çağrılır; satır "Cast down above 250"ye döner.
   - Yan düzeltme: Lucifer beklerken sıradan masada "son 250" modu (cehennem ateşi + "No more cheap bets" repliği) artık hiç açılmıyor.
     Ceza 250'ye indiğinde sonuç ekranında bir an açılıp yeni repliğin üstüne konuşuyordu; bir sonraki el zaten Lucifer'in.
2. Değişiklik yok.
3. **Wild Bill:**
   - `WildBill` artık `Attempts == 0` da istiyor. Düşmüş oyuncunun Dead Man's Hand'i ABSOLVED, rekorlarda Wild Bill sayılmıyor.
   - Oyun sonu ekranında hikâyeye "The Morning Star will remember this." ekleniyor.
   - Başlık ve hikâye seçimi test edilebilir statik metotlara taşındı (`EndScreenView.TitleOf / StoryOf`).
4. **Başlık:** `DealerView` son masada başlığı boş bırakıyor. `DealerText.Title` kilitli kartta görünmeye devam ediyor.

### Testler
- `LuciferPresenterTests`:
  - düşmüş oyuncunun Dead Man's Hand'i (ABSOLVED, Wild Bill değil, rekorlar, "remember" satırı);
  - dört sonun adları ve satırları;
  - sıradan masada son yıl (replik, sayaç notu, sonraki elde 1 yılla çağrılma);
  - sıradan kayıpta bu repliğin söylenmemesi.
- `LuciferJourneyTests` (PlayMode): Lucifer masasında başlık boş.
- **450 EditMode + 15 PlayMode geçiyor.**

### Sıradaki adımlar
- Kullanıcıdan gelecek: menü resmi ve şeytan hileleri (denge, Belial dahil, onunla yeniden ayarlanacak).

---

## 2026-10-02 — İş 1: ana menü arka plan resmi

### İstek (kullanıcı)
- Başka oturumdan kalan `pixel_menu.py` / `menu.png` silinsin, `generate_art.py` son commit'e dönsün.
- Sonra Tools/ArtGen ile 480×270, 4 kare, ~4 FPS animasyonlu bir menü resmi çizilsin.
- Konu: cehennemin dibi, Lucifer asla tam görünmez. Orta sütun sakin. Menü, kurallar, ayarlar ve rekorlarda arkada kalsın.

### Yapılanlar
- **Temizlik:**
  - eski `pixel_menu.py`, `menu.png` (+ .meta) ve `__pycache__` kalıntısı silindi;
  - `generate_art.py` GitHub Desktop'un git'iyle son commit'e döndürüldü;
  - `Docs/HELLPOKER_UPDATE_5.md`'ye dokunulmadı (kullanıcının plan dosyası).
- **`pixel_menu.py` (yeni, sıfırdan):** 4 kare, 1920 px.
  - Gök: duman ve sönük yıldızlar; yukarıdan iki dev kanat (kaburgalı, aydınlık kenarlı, uçlarında pençe).
    Logonun üstünde iki yanan göz, 4. karede kırpılır. Lucifer'in kendisi yok.
  - Ufuk: ateş denizi (kenarlarda vahşi, orta sütunun altında sakin), iki yanda yanan şehir siluetleri.
  - Kenarlar: Mammon'un altın yığını ve açık sandığı (sol alt), Lilith'in hilali (sol), Belial'in yılanlı kırık sütunu (sağ),
    taş üstünde A♠ A♣ 8♠ 8♣ (sağ alt). Yükselen korlar, parlayan sikke ve kart.
  - Orta sütun (x 94-386, y 30-256) iki adım koyulaştırılmış. Gözler koyulaştırmadan sonra çizilir.
- `generate_art.py menu`. `preview.py`'de menü bölümü (orta sütun kesikli çizgiyle işaretli); tam ekran şeritler UI sayfasından çıkarıldı.
- **Unity:**
  - `MenuBackdropLibrary` (Animation, yükleyici enjekte edilebilir), yedek zinciri `Ui/menu` → `Ui/background` → null (düz renk).
  - `MenuBackdrop.Create` menü, ayarlar ve rekorlar ekranlarında taş zeminin yerini aldı. Kurallar paneli menü canvas'ında, zemin arkada.
- **Ekran görüntüleri** (01_menu, 01b_settings, 02_rules, 23_records): yazılar ve butonlar rahat okunuyor.
  İlk denemede orta sütundaki ateş denizi butonların altında fazla parlaktı; kenarlara doğru güçlenecek şekilde yeniden çizildi.
  Kanat kenarları görünmüyordu, aydınlık kenar eklendi.

### Testler
- `MenuBackdropTests`:
  - şerit 4 kare / 4 FPS / döngü;
  - taş duvar yedeği; sanat yokken null ve yükleyici hatasında çökmeme;
  - üretilen dosyanın 4 × 480 ve 2048 sınırı içinde olması;
  - orta sütunun her karede en az %80 koyu olması.
- **455 EditMode + 15 PlayMode geçiyor.**

---

## 2026-10-02 — İş 2 (Şeytan hileleri), Bölüm 1 + 2: altyapı ve hileler (Core)

### İstek (kullanıcı)
Oyun "sıra tabanlı dövüş" gibi hissettirsin: şeytanın hamlesi önceden görünür (niyet), hilesi kartlarda görünür bir darbe olur.
- Bölüm 1: kötülük göstergesi, hile seçimi, `ICheat` / `CheatResult` / `ICheatPolicy` / `ICheatGuard`, niyet (Belial'inki sahte olabilir).
  Dead Man's Hand bağışıklığı, kayıt v=3.
- Bölüm 2: 13 hile (Mammon 3, Belial 3 + sahte niyet, Lilith 3, Lucifer 4).
(Bölüm 1 ve 2 birlikte yazıldı; altyapı hileler olmadan sınanamıyordu. Tek kayıt.)

### Yapılanlar — altyapı (`Assets/Scripts/Core/Cheats/`)
- `ICheat`:
  - Id, `CheatTier` (Minor / Major), `CheatTiming`, CanApply, Apply.
  - Zamanlamalar: AfterDeal, BeforeDraw (beş kart açılıp draw'a girilince), AfterDraw, HouseReveal, BeforeShowdown.
  - `CheatIds`: kimlikler, sunum ve kayıt anahtarı.
- `CheatResult`: hangi oyuncu / kasa kartları, kaybedilen / kazanılan kart, yıl, sonuç (Played / Blocked / Fizzled), `ShownId` (yalan).
- `CheatTable`: hilenin üzerinde çalıştığı el.
  - İçeriği: iki el, deste, değerlendirici, zar, birim, açık kasa kartları, draw'da gelen pozisyonlar, showdown.
  - Yardımcılar: hedef seçimi (bağışık kartları atlar), en yüksek / en düşük, desteden belirli kartı alma, yeniden dağıtma.
- `CheatMarks`: elde kalan işaretler.
  - Zincirli, dikenli, oyuncuya kapalı kartlar, sahte kasa yüzü, haraç, bakış.
  - İşaretler pozisyona değil **karta** bağlı: örtülü kart atılırsa örtü de gider.
- `CheatRules.IsImmune`: A♠ A♣ 8♠ 8♣ hiçbir hileden etkilenmez. Hedef olursa hile başka karta geçer ya da boşa gider.
- `ICheatPolicy` / `DemonCheatPolicy`:
  - ceza ≤ `GameRules.MajorCheatYears` (400) iken büyük hile %50 (`MajorCheatPercent`), değilse küçük;
  - `LiePercent` (Belial %25: duyurulan başka bir hilesi);
  - kendi büyük-hile çizgisi ve "masada bir kez" (Lucifer: 150, deneme başına bir Düşüş).
- `ICheatGuard` / `AllowEveryCheat`: her vuruştan önce sorulur. Engellenen hile harcanır ama hiçbir şey değiştirmez (sonraki planın kancası).
- **`CheatSession`** (yeni; `HellPokerGame` 500 satırı geçtiği için ayrı sınıf). Kötülük göstergesi, seçim, vuruş, işaretler ve sonuçlar burada.
  - Her el +1 (`MalicePerHand`), oyuncu kazanınca +1 (`MalicePerWin`), ceza ≤ 500 ise her el +1 daha (`MaliceLowSentence*`, Lucifer hariç).
  - Dolunca dağıtımda hile seçilir ve niyet olarak duyurulur.
  - Vuran ya da engellenen hile göstergeyi boşaltır. Boşa giden (üzerinde çalışacak bir şey yok) ya da anı hiç gelmeyen (oyuncu önce
    çekildi) hile göstergeyi dolu bırakır, sonraki el yeniden seçilir.
- `HellPokerGame`: beş anda `Strike`. Ayrıca:
  - kasanın re-raise kararına Bakış;
  - draw'da zincir kontrolü ve diken cezası (anında, defterde);
  - kazançta haraç; showdown'ı değiştirebilen tek hile Düşüş.
  - `PlayerHandNow` gizli kartları saymaz; gizli kart varken draw ipucu verilmez (sızıntı olmasın).
- `IHellPokerGame` yeni üyeler: `Malice / MaliceMax / PendingCheat / CheatsThisHand / MajorCheatUsed / ThornYearsThisHand /
  TitheYearsThisHand`, `IsPlayerCardHidden / Chained / Thorned`, `IsHouseCardFalse`, `HouseCardFace`, `RestoreMalice`.
- Kart / deste: `IDeck.Remaining` / `Take(card)`, `Hand.With / IndexOf`.
- `Dealer.MaliceMax` / `Dealer.Cheats` (politika şeytan paketinde). Somut hileler `DealerRoster`'da, oyun `HellPokerGameFactory`'de kuruluyor.
  Hileler ayrı bir zar akışı kullanıyor (seed+2): kart sırası ve kasanın mizacı değişmiyor.
- `GameRules`: `MalicePerHand, MalicePerWin, MaliceLowSentenceYears (500), MaliceLowSentenceBonus, MajorCheatYears (400), MajorCheatPercent (50)`.
- **Kayıt v=3:** `malice`, `cheat.major`; yarım elde `hand.cheat / hand.shown / hand.cheat.done`. v=2 ve v=1 boş göstergeyle okunur.
  Devam edilen koşuda gösterge geri yükleniyor (kapatıp açmak göstergeyi sıfırlamıyor).

### Yapılanlar — hileler
| Şeytan | Küçük | Büyük | Niyet |
|---|---|---|---|
| Mammon (4) | Rehin (BeforeDraw: en yüksek kart zincirli), Haraç (Showdown: kazançtan 1 birim) | Satın Al (BeforeDraw: en yüksek ↔ kasanın en düşüğü) | hep doğru |
| Belial (3) | Sahte Yüz (HouseReveal: açılan kartlardan biri daha zayıf görünür), Çatal Dil (AfterDraw: bir kartın rengi değişir, yenisi desteden) | Yılan Takası (AfterDraw: çiftin kartı kasaya gider, gelen kart oyuncuya kapalı) | %25 sahte |
| Lilith (3) | Gece Örtüsü (BeforeDraw: bir kart kapanır, kör atılabilir), Diken (BeforeDraw: atarsa anında 1 birim) | Aysız Gece (AfterDraw: çekilen kartlar kapalı kalır) | hep doğru |
| Lucifer (1, her el) | Bakış (AfterDeal), Yeniden Yazma (AfterDraw: eli bir alt kategoriye düşürür), Yanan Kart (BeforeDraw) | Düşüş (Showdown, ≤150, denemede bir kez: kazanırsa iki elin en yüksek kartı yeniden dağıtılır) | hep doğru |

### Kararlar
- Hile anı gelmezse (oyuncu çekildi) ya da hilenin işleyecek bir şeyi yoksa gösterge dolu kalır. "Hile oynanınca sıfırlanır" kuralının
  tersinden okunuşu: oynanmayan hile harcanmış sayılmaz. Niyeti gören oyuncu kaçınabilir: Aysız Gece'de kart çekmemek, Düşüş'te çekilmek
  ya da Haraç'ta kaybetmek hileyi boşa çıkarır.
- Haraç sadece kazanılan elde, Düşüş sadece oyuncu kazanırken oynar. Bu yüzden niyet görünür ama "boşa gitti" olabilir; Düşüş o zaman harcanmaz.
- Sahte Yüz, gerçek karttan daha zayıf bir yüz seçer (oyuncuyu artırmaya çekmek için). Yüz desteden ama desteden alınmaz, kimsenin kartı değil.
- Çatal Dil / Yeniden Yazma yeni kartı desteden alıyor; hiçbir kart iki yerde birden olmuyor.
- Lucifer'in ≤ 500 bonusu yok (zaten her el hile).

### Testler
- `CheatTests` (39):
  - gösterge (her el, kazanç, ≤ 500, son masa), niyetin görünmesi / harcanması;
  - büyük hile kuralı, Belial'in yalanı (masada ortaya çıkışı);
  - koruyucu (sorulur, engellenen hiçbir şey değiştirmez), boşa gitme, çekilmede göstergenin dolu kalması;
  - Dead Man's Hand bağışıklığı (yanan kart beşinci karta geçer, satın alma boşa gider, haraç DMH'ye işlemez);
  - 13 hilenin her biri;
  - Düşüş (duyuru, kazancı kayba çevirme, denemede bir kez, 150 üstünde yok, kayıpta harcanmaz), Lucifer her el;
  - mühürlü elde hile, ruhta diken, göstergenin geri yüklenmesi, yarım elin hile bilgisi;
  - kayıt v=3 gidiş-dönüş, v=2 okuma; şeytan başına hile listesi.
- Eski testlerin hepsi geçiyor (testlerin elle kurduğu oyunlarda hile yok; fabrikayla kurulanlarda var).
- **494 EditMode + 15 PlayMode geçiyor.**

---

## 2026-10-02 — İŞ 2 / Bölüm 3: hilelerin sunumu

**İstek:** Gösterge portrenin altında, niyet ikonu + adı portrenin üstünde, açıklama hover'da ve H panelinde; Belial'in yalanı kırılıp
gerçeğe dönüşsün; her hilenin kartta ayrı bir efekti (1-2 px sarsıntı, reraise animasyonu, replik); sonuç ekranında hile kaydı;
şeytan başına ilk hile ipucu; hız / atlama uyumu; ruhta sayı yok; How to Play'de "Cheats" sayfası.

### Yapılanlar
- Görseller (`pixel_ui.py`): `cheat_icons.png` (13 ikon, 16×16, `CheatIds` sırasıyla), `malice_pips.png` (8×8; sikke / terazi /
  diken / kor, boş + dolu), `card_marks.png` (32×48 kart üstü işaretler: zincir, diken, örtü, sahte yüz parıltısı).
- `CheatDisplay.cs` (Abstractions): `MaliceGauge`, `CheatCard` (id, ad, tek cümle açıklama), `CheatImpact`, `CardMark`.
  `CardSlot.Mark` (`SameAs` işareti saymaz; işaret değişince kart dönmeden üstüne biner).
- `ITableView`: `SetMalice`, `SetIntent`, `RevealLie`, `PlayCheat`; `ShowHandRanks(payouts, footnote)`.
- `MaliceView`: portre kutusunun altında pip şeridi (dolarken yanıp söner), üstte niyet şeridi (ikon + ad), hover'da açıklama kutusu.
  Yalan: şerit "LIAR" diye titrer, sarsılır, gerçek hileye döner.
- `CheatEffects`: hedef kartlar `{2,-2,1,-1,2,-1,0}` piksel sarsılır, ikon üstlerinde yanıp söner. Bakış beş kartın hepsine,
  Haraç'ta üç sikke şeytana uçar, Düşüş'te ekran sarsılır. Hepsi `AnimationClock`'tan geçiyor (hız ayarı + atlama).
- `TablePresenter.PlayCheatStrikes`: yalansa önce `RevealLie` + yalan repliği; kart değiştiyse önce **eski hali** gösterilir, sonra
  `PlayCheat` + şeytanın hile repliği (`DealerMood.Scheming` → reraise animasyonu). Kart durumları `PlayerSlots / HouseSlots` ile:
  örtülü kart arkası + örtü, zincir / diken işaretleri, sahte kasa yüzü parıltılı.
- Sonuç mesajına hile satırı (`UiText.CheatLog`, ruhta yıl yok). İlk hile ipucu her şeytan için bir kez (`tip.cheat.<id>`).
  H panelinin altına duyurulan hilenin açıklaması. Menü ▸ How to Play: RULES → HANDS → CHEATS.
- `CheatScreenshots` ([Explicit]): her hile yığılmış destede zorlanıp vuruş anında çekiliyor (40–57). İki düzeltme:
  - Haraç ve Düşüş showdown'da vurduğu için betik ara kararları geçemiyordu; `BetUntil(phase)` eklendi (pas, yasaksa artır, re-raise'de karşıla).
  - Düşüş oyuncuyu masadan attığı için Lucifer'in küçük hileleri çekilmiyordu; her çekimden önce ceza 150'ye çekilip Lucifer yeniden
    çağrılıyor, kaybeden bir elle (kazanç koşuyu bitirirdi).
- Hata: sonuç satırında "The Morning star" yazıyordu; şeytan adı artık kelime kelime büyük harfle başlıyor (test eklendi).

### Testler
- `CheatPresenterTests` (14): gösterge + niyet, vuruş (efekt, replik, işaret), zincirli kart seçilemez, değişen kartın eski hali,
  örtü, sahte yüz showdown'a kadar, Belial'in yalanı, sonuç satırı, haraç, ruhta sayısız diken, ilk hile ipucu, H paneli, devamda gösterge,
  unvanlı şeytan adı.
- **507 EditMode (+1 explicit) + 15 PlayMode geçiyor.** (Düzeltme: bu koşu eski derlemeyle yapılmıştı, bkz. Bölüm 4; düzeltilmiş
  hâliyle 508 geçiyor.)

### Sıradaki
- Bölüm 4: denge simülasyonu hilelerle, niyete tepki veren simülasyon oyuncusu, gösterge hızı ayarı, şeytan başına PlayMode uçtan uca test,
  CLAUDE.md.

---

## 2026-10-02 — İŞ 2 / Bölüm 4: hilelerle denge, uçtan uca testler

**İstek:** BalanceSimulation hilelerle çalışsın; simülasyon oyuncusu niyete tepki versin. Hedef aklanma Mammon ~%80, Belial ~%70,
Lilith ~%55; Lucifer'i ilk denemede yenme ~%35. Sadece gösterge hızıyla (MaliceMax, kazançta +1, ≤ 500 bonusu) ayarla, hile kurallarına
dokunma; ulaşılamıyorsa açıkla. Hile sıklığı ve dağılımı raporlansın. Şeytan başına PlayMode uçtan uca test; CLAUDE.md.

### Yapılanlar
- `BalanceSimulation`:
  - Hileler açık (fabrikanın kurduğu oyunlar). Ortam değişkenleri: `HELLPOKER_MALICE` ("mammon,belial,lilith,lucifer"),
    `HELLPOKER_MALICE_WIN`, `HELLPOKER_MALICE_LOW`, `HELLPOKER_CHEATS=0`.
  - Oyuncu artık **sadece gördüğüyle** oynuyor: elinin gücü `PlayerHandNow` (örtülü kartlar sayılmaz), kasanın açık çifti
    `HouseCardFace` ile ve sahte işaretli yüz sayılmadan. Örtülü kart varken görünen çiftleri tutup gerisini (önce örtülüleri) atıyor.
  - Niyete tepki: zincirli ve dikenli kartı atmaz; Düşüş beklerken artırmaz ve draw'dan sonra High Card'la çekilir.
  - Rapor: masa başına el sayısı, el başına oynanan hile, boşa giden, Belial'in yalanları, hile türü dağılımı (Lucifer masası ayrı satır).
- `CheatJourneyTests` (PlayMode, 4 test, gerçek butonlar, yığılmış deste): Mammon rehin (zincir işareti, kart atılamaz), Belial yalanı
  (Sahte Yüz duyurulur, Çatal Dil vurur), Lilith örtüsü (kapalı kart, ipucu sızmaz, kör atılır), Lucifer Düşüş (THE FALL AWAITS,
  floş düşer, denemede bir kez). Ortak yardımcı `CheatRig` (CheatScreenshots da onu kullanıyor).
- Hata: bir önceki EditMode koşusu test derlemesindeki bir hata yüzünden eski derlemeyle çalışmıştı (UiText internal). Test presenter
  üzerinden yazıldı; artık her test koşusundan sonra log'da `error CS` de kontrol ediliyor.

### Denge — sonuçlar (2000 koşu)
| Ayar (Mammon / Belial / Lilith / Lucifer) | Mammon | Belial | Lilith | Lucifer ilk deneme (M / B / L) |
|---|---|---|---|---|
| Hilesiz (önceki) | %86.7 | %79.3 | %61.8 | %48 / %55 / %47.5 |
| 4 / 3 / 3 / 1 (başlangıç) | %81.8 | %76.1 | %51.8 | %36 / %42 / %37 |
| 4 / 2 / 4 / 1 (**seçilen**) | **%81.8** | **%75.7** | **%54.6** | %36 / %42 / %37 |
| 4 / 1 / 4 / 1 | %81.8 | %74.9 | %54.6 | %36 / %41 / %37 |

Kazançta +1 ve ≤ 500 bonusu 1'de kaldı.

| Masa | El başına hile | Boşa giden | Yalan | Dağılım |
|---|---|---|---|---|
| Mammon | 0.27 | 4900 | – | rehin %55, haraç %31, satın al %14 |
| Belial | 0.48 | 793 | 5076 | çatal dil %46, sahte yüz %42, yılan takası %12 |
| Lilith | 0.35 | 37 | – | diken %44, örtü %44, aysız gece %13 |
| Lucifer | 0.83 | 3173 | – | yanan kart %34, bakış %32, yeniden yazma %24, düşüş %10 |

### Kararlar ve nedenleri
- **Mammon 4:** hedefte (%81.8).
- **Lilith 3 → 4:** hileleri sert (örtü + diken); yavaş gösterge onu %54.6'ya, hedefe getiriyor.
- **Belial 3 → 2: hedef (%70) gösterge hızıyla ulaşılamıyor.** Her el hile yapsa bile (1) %74.9. Neden:
  - Hileleri oyuncunun elini bozmaz. Sahte Yüz parıltıyla işaretli (oyuncu yüze güvenmeyebilir), Çatal Dil sadece rengi değiştirir.
  - Yılan Takası tek gerçek darbe, ama büyük hile ve sadece ≤ 400'de.
  - 2'yi seçtim: 1'le neredeyse aynı sonuç (%0.8 fark), ama niyet her elde değil, ritim hissi korunuyor.
  - %70 için hile kuralı değişmeli (kullanıcı kararı): ör. büyük hile çizgisi Belial'de daha yüksek, Yılan Takası küçük hile,
    ya da Sahte Yüz parıltısız. Gerçek bir oyuncu sahte yüze kanabilir; simülasyon kanmıyor, yani gerçekte Belial biraz daha sert.
- **Lucifer:** gösterge zaten 1 (her el), daha sertleşemez. İlk denemede yenme şeytana göre %36 / %42 / %37, ortalama ~%38, hedefe (~%35) yakın.
- Lucifer'i ilk denemede yenme şeytana göre farklı görünüyor; Lucifer aynı, fark düşülen masadan ve çağrılmaya kalan sürenin
  dağılımından geliyor.

### Testler
- **508 EditMode (+1 explicit simülasyon) + 19 PlayMode (+2 explicit ekran görüntüsü) geçiyor.** CheatScreenshots 40–57 tam.

### Açık sorular
- Belial %70 isteniyorsa hile kuralı değişmeli (yukarıdaki seçenekler).
  **Kullanıcı (2026-10-02): şimdi dokunma, gösterge 2 kalsın.** Gerçek oyuncu Sahte Yüz'e kanabilir; önce oyun testinde görecek
  (`Docs/PLAYTEST.md` ▸ Hileler), karar ondan sonra.
- Sonraki iş (oyuncu sınıfları / yetenekleri) için kancalar hazır: `PendingCheat`, `ICheatGuard`, `CheatMarks`, `Malice`.

---

## 2026-10-02 — Temizlik, Belial notu, oynanabilir Windows build

**İstek:** Repoya yanlışlıkla girmiş `cshots.xml`'i sil, .gitignore'da tek tek xml satırları yerine kökteki tüm test çıktıları için
`/*.xml` (projenin ihtiyaç duyduğu xml varsa hariç). Belial'e dokunma, oyun testinden sonra karar. Windows x64 build (batchmode,
script, komut CLAUDE.md'de, Builds/ ignore), çalıştırıp Player.log'u kontrol et, oyun testi için `Docs/PLAYTEST.md`.

### Yapılanlar
- `cshots.xml` silindi (GitHub Desktop'ta silme olarak commit edilmeli). .gitignore: `/results.xml`, `/play.xml`, `/shots.xml`,
  `/sim.xml` yerine `/*.xml`. Kökte projenin ihtiyaç duyduğu bir xml yok (sadece test çıktıları); gerekirse `!/<ad>.xml` ile açılır.
  Kural sadece kökü kapsıyor, Assets / ProjectSettings etkilenmiyor. `/[Bb]uilds/` zaten vardı.
- Belial: gösterge 2 kaldı; açık soruya kullanıcının notu düşüldü (Bölüm 4).
- `Editor/HellPokerBuild.cs`: `HellPokerBuild.Windows` (menü Hell Poker ▸ Build Windows). Sahne yoksa önce kurar,
  `Builds/Windows/HellPoker.exe` (StandaloneWindows64) üretir, batchmode'da başarısızlıkta çıkış kodu 1.
- Build: başarılı, 117 MB, ~3 dk. Pencereli (960×540) açılıp 15 sn çalıştı. Player.log'da exception / error yok.
  Tek dikkat çeken satır "XInput1_3.dll not found. Trying XInput9_1_0.dll", Unity'nin normal yedek mesajı.
- `Docs/PLAYTEST.md`: doldurulacak kontrol listesi. Bölümler: ilk izlenim, tempo, zorluk (şeytan tablosu, ruh, Lucifer),
  hileler (şeytan başına adillik / niyet anlaşılırlığı, en sinir bozucu hile, Sahte Yüz'e kanma), anlaşılırlık,
  hatalar (adım / olan / beklenen), en sevdiğim / en sıkıcı an.
- CLAUDE.md: build komutu, Player.log yeri, mimaride `HellPokerBuild`.

### Kararlar
- Build `BuildPlayerOptions` ile sadece `HellPoker.unity` sahnesini alıyor (EditorBuildSettings'e bağlı değil). Ayarlar (şirket adı
  "DefaultCompany", ürün "Hellpoker") değişmedi; Player.log ve PlayerPrefs yolu bunlara bağlı.

### Sıradaki
- Kullanıcı build'i oynayıp `Docs/PLAYTEST.md`'yi dolduracak. Belial kararı ve diğer ayarlar ondan sonra.
