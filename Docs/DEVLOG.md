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

---

## 2026-10-03 — Plan 6 / Bölüm A: hile düzeltmeleri ve geri tepme

**İstek (0.1.0 testinden sonra):** Bazı hileler oyunda işe yaramıyor. Gece Örtüsü geç kapanıyor; Çatal Dil %80 eli bozsun, %20 "dil
kaysın" (oyuncuya yarayabilir, `BackfirePercent`); Bakış kesin bilgi vermesin (kaybedene %100, kazanana %50 re-raise); Rehin ve
Diken atılacak kartlara; Yanan Kart kombinasyonun kartına; gizli kartların yüzü hiçbir yoldan sızmasın; geri tepme görünür bir an
olsun (BACKFIRE, angry, replik, günlük, kayıt / rekor); her hile için "etkili" testi; PlayMode uçtan uca; simülasyon; CLAUDE.md ve
CHEATS sayfası.

### Yapılanlar — Core
- `CheatTable`: `PlayerCardsSeen` (oyuncunun gördüğü kart sayısı; dağıtımda açılış kartları), `BackfirePercent`, `Advice`
  (kasanın `IDrawStrategy`'si) + `AdvisedDiscards()`, `Improves(hand)`, `CombinationCards()`.
- `CheatResult.Backfired` (+ `AsBackfire()`). `CheatSession.Strike` hile öncesi / sonrası oyuncu elini karşılaştırır, güçlendiyse
  işaretler. `CheatSession(…, backfirePercent)`, `Dealer.BackfirePercent` (+ `WithBackfirePercent`), Belial 20
  (`DealerRoster.BelialBackfirePercent`); fabrika şeytandan geçirir.
- Hileler:
  1. **Gece Örtüsü:** `AfterDeal`, hedef sadece `i ≥ PlayerCardsSeen` (açılış kartlarından sonraki 3.-5.). Kart sırası gelince sırtı
     dönük + örtüyle açılır, showdown'a kadar kalır, kör atılabilir. Açıklama: "One of your cards will come to you in the dark."
  2. **Çatal Dil:** zar `BackfirePercent`'in altına düşerse kayar (rastgele renk değişimi). Değilse nişanlı: önce renk / 4 aynı renk
     bozulur, yoksa kategoriyi düşüren, yoksa elini iyileştirmeyen bir değişim (hiçbiri yoksa boşa gider).
  3. **Bakış:** kaybedecek ele her zaman, kazanacak ele `GazeBluffPercent` (50) ihtimalle re-raise. Açıklama: "He sees your hand. His
     raises will hurt."
  4. **Rehin:** `AdvisedDiscards`'ın en yükseği; atılacak kart yoksa en düşük kart. Açıklama değişti.
  5. **Diken:** atılacak kartlardan biri, yoksa rastgele.
  6. **Yanan Kart:** en iyi kombinasyonun en yüksek kartı, yoksa en yüksek kart; yerine desteden rastgele (geri tepebilir).
  7. **Satın Al** ve **Yılan Takası** artık oyuncuya yarayacak kartı seçmiyor (ör. Satın Al'ın verdiği düşük kart oyuncunun bir
     kartıyla çift yapıyorsa o kart atlanır). Bu ikisi eskiden nadiren yarayabiliyordu.
- `IHellPokerGame.WasPlayerCardHidden(i)`: el bitse de (sonraki dağıtıma kadar) gizli kartı bilir.
- Kayıt / rekor: `RunStats.Backfires` + `NoteBackfires`, kayıtta `backfires` (v=3 içinde isteğe bağlı, yoksa 0), `RecordBook.BackfiresSeen`
  (`backfires`, eski defter 0).

### Yapılanlar — sunum
- Sızıntı bulundu ve kapatıldı: **mühürlü el draw'dan sonra kendi kendine oynarken** oyuncunun gizli kartları (Aysız Gece / Yılan
  Takası) kasa kartlarını açmadan önce yüzüyle gösteriliyordu (el bittiği için `IsPlayerCardHidden` false dönüyordu).
  `PlayerSlots(keepHidden)` + `WasPlayerCardHidden` ile showdown'a kadar sırtı dönük. Sonuç ekranında artık önce kasa, sonra oyuncu döner.
- Gizli kart varken draw ipucu çerçevesi hiç yok (eskiden "hepsini tut" diye beş kartı da çerçeveliyordu).
- Örtü işareti sadece kartın sırası gelince görünür (henüz açılmamış kart düz sırt).
- `PlayCheatStrikes`: her vuruştan önce niyet şeridi gösterilir (dağıtımda vuran hile de bir an görünür). Geri tepmede: yeni kart hemen döner,
  `TableMoment.Backfire` ("BACKFIRE" koyu şerit üzerinde, kartın üstünde yanıp söner — `CheatEffects.PlayBackfire`), şeytan
  `DealerMood.Annoyed` (angry) ve `DealerText.Backfire` repliği (her şeytana 2-3; Belial "My tongue... slipped.").
- `UiText.CheatLog` geri tepmeyi söyler ("…tongue slipped: your 9♥ became the 9♣. It backfired!"). Rekorlar ekranında "Backfires seen".
- CHEATS sayfası: yeni açıklamalar + "A cheat left to chance may BACKFIRE and help you".

### Testler
- `CheatTests` güncellendi (Rehin 7♥'ye, örtü dağıtımda, Bakış blöfü, Yanan Kart kombinasyonu, Diken atılacak karta).
- `CheatBackfireTests` (yeni): Çatal Dil nişanlı / kayınca renk (backfire), Yanan Kart ve Düşüş geri tepmesi, Belial %20, kayıt ve rekor;
  **11 hile × 300 karışık el**: hiçbiri oyuncuya yaramıyor ve her biri en az bir elde gerçekten vuruyor.
- `CheatPresenterTests`: geri tepme anı (BACKFIRE, kızgın şeytan, günlük, rekor), gizlilik: Gece Örtüsü, Aysız Gece (normal +
  **mühürlü**), Yılan Takası — `FakeTableView.ShowLog` her Show'u sırayla kaydeder; kasa beş kartı açmadan hiçbir gizli kartın yüzü yok,
  hiçbir metin gizli kartı adlandırmıyor.
- PlayMode `CheatJourneyTests` (7): Mammon rehin; Belial yalan + flush bozma ve dil kayması (BACKFIRE, rekor); Lilith örtü ve aysız
  gece — `CheatRig.DarkWatch` her karede ekrandaki kart yüzlerini (`CardView.FaceShown`) gizli kartlarla karşılaştırır; Lucifer bakış
  (240 yılda: 150'de masaya konan her şey all-in olduğu için re-raise'e yer kalmıyor) ve Düşüş.
- **537 EditMode (+1 explicit) + 22 PlayMode (+2 explicit) geçiyor.**

### Denge (2000 koşu; gösterge 4 / 2 / 4 / 1)
| Şeytan | Aklanma (hedef) | Ort. el | Lucifer'e ulaşan | İlk denemede (hedef ~35) | Hile / el |
|---|---|---|---|---|---|
| Mammon | %79.7 (~80) | 48.9 | %85 | %33.9 | 0.27 |
| Belial | %74.1 (~70) | 26.4 | %81 | %39.0 | 0.48 |
| Lilith | %52.6 (~55) | 29.3 | %66 | %34.2 | 0.36 |

Lucifer masası: 0.83 hile / el. **Geri tepme:** Yanan Kart %26.3, Düşüş %18.2, Çatal Dil 4 / ~9900 (%0.04). Toplam Lucifer hilelerinin
%10.6'sı geri tepiyor; Mammon ve Lilith'te hiç yok (kuralı gereği).

- Simülasyon oyuncusu Bakış'ta re-raise'i kesin bilgi saymıyordu zaten (çift ve üstüyle karşılar); belgelendi.
- **Lilith:** %52.6, hedefin 2.4 altında. Gösterge 5 denendi: %52.5 (fark yok). Lilith'in kendi hileleri sonucu neredeyse
  değiştirmiyor; düşüş Lucifer'den geliyor: Yanan Kart artık kombinasyonu yakıyor, ilk denemede yenme %37'den %34'e indi. Gösterge 4 kaldı.
  (Lucifer'in göstergesi 1, daha sertleşemez; yumuşatmak tüm şeytanları değiştirir ve ilk deneme hedefini bozardı.)
- **Belial:** gösterge 2 (kullanıcı kararı: oyun testinden sonra). Çatal Dil'in %20 kayması pratikte çok nadir yarıyor: rastgele renk
  değişimi ancak el bir kart eksik renkteyse renk yapar.
- Mammon ve Lucifer hedefte.

### Kararlar
- Geri tepme "el güçlendi mi" ile ölçülür (kategori ya da aynı kategoride güç). Showdown sonucuna bakılmaz: Düşüş'te el güçlenip yine
  kazanmak da geri tepmedir.
- Nişanlı Çatal Dil hiçbir değişim zararsız değilse boşa gider (iyileştiren tek seçenek kalsa bile vurmaz).
- Örtülü kart açılmadan önce düz sırt görünür, örtü işaretini sırası gelince alır: hangi kartın örtüleceği ancak vuruş anında belli olur.

### Açık sorular
- Belial gösterge kararı ve Lilith'in 2.4 puanlık açığı oyun testine kaldı.

---

## 2026-10-03 — Plan 6 / Bölüm B: animasyon akıcılığı ve okunabilirlik

**İstek:** Menü ve salonlar düşük FPS'li görünüyor (Belial'in yılanlı sütunları kesik kesik). Nedeni: tam ekran şeritler 3-4 kare /
4 FPS ve bazı döngüler kapanmıyor. Katmanlı zemin (tek kare sabit resim + 8-12 kare / 8-12 FPS küçük şeritler), kusursuz döngü ve
ArtGen kontrolü, kodla parçacık, kare başına 1-2 px, yazı okunabilirliği (panel ya da 1 px outline; hareket yazıya girmesin),
F3 FPS göstergesi ve ölçüm, preview, CLAUDE.md.

### Yapılanlar — ArtGen
- `pixel_layers.py` (yeni):
  - `Layer` (8-12 kare, şerit ≤ 2048 px) ve `Particles`; `phase(f, n) = 2π·f/n`.
  - `check_loop`: son → ilk geçiş en büyük adımdan büyük olamaz. Ateş denizinde 2317'ye karşı 2310 px çıkınca %2 pay eklendi:
    tam periyodik bir dalga da her fazda biraz farklı nicelenir.
  - `keep_out_of_text`: yazı bölgesinde katman temizlenir, sabit resim orada katmanın 0. karesini alır.
  - `write`: varyantlar (LUT), şeritler, `motion.txt`.
- `pixel_salons.py`:
  - Her salon `<x>_base()` (sabit) + katmanlar + parçacıklar. `readable_middle(img, ox, oy)` katmana kendi ekran yerindeki vinyeti verir.
  - Mammon: kefeler zıt fazda ±1 px, 10 parıltı, altın tozu.
  - Belial: perdelerin dış yarısı (yazıya bakan iç kenar sabit), iki yılanlı sütun (genlik 6 → 3 px: kare başına ≤ 1.6 px).
  - Lilith: mum alevi, 6 yıldız, 3 kuş (9 px/sn kayan), sis.
  - Lucifer: dıştaki 4 zincir ±1 px (ortadaki 2 zincir yazının arkasında, sabit), korlar.
- `pixel_menu.py`:
  - Sabit resim + katmanlar: gözler (12 karede parlıyor, kısılıp kapanıp açılıyor), yılan (genlik 8 → 4), ateş denizinin iki kenarı,
    5 parıltı; kor parçacıkları.
  - Ateş denizi 24 / 12 px periyotlu iki dalga (12 karede kare başına 2 / 1 px) + sabit düzensiz kabarma (düzenli tarama gibi
    görünüyordu). Ortaya doğru 20 px dither ile durulur, ek yeri görünmez.
- `preview.py`: her zemini (4 salon × varyant + menü) JS ile gerçek hızında oynatır: katmanlar kendi FPS / ofset / kaymasıyla,
  parçacıklar, kesikli yazı bölgeleri.

### Yapılanlar — Unity
- `BackdropMotion` (manifest okuyucu; bozuk satır / eksik şerit atlanır), `SalonLibrary.Motion` + `Preload`,
  `MenuBackdropLibrary.Motion`, `UiArt` metin yükleyici.
- `BackdropMotionView`:
  - Katman kopyaları gerçek zamanla döner; parçacıklar tek piksel, tam piksel adım, `AnimationClock` hızıyla.
  - Renkler yaşa göre, değişince atanıyor; rampalar statik (kare başına çöp yok).
  - Kendi iç içe Canvas'ı var. `SalonView` ve `MenuBackdrop` kullanıyor.
- `PixelOutline` + `WithOutline()`: 8 yönlü 1 px siyah çizgi (Unity'nin Outline'ı sadece çaprazlara kayar, düz çizgilerin uçları açık
  kalıyor). Eski `WithShadow` kaldırıldı, 31 çağrı yeniden adlandırıldı (`.NET` IO ile, UTF-8 korunarak).
  Panelsiz kalan yazılar da outline aldı: şeytanın ünvanı, menü altbilgisi, yedek başlıklar, son ekran hikâyesi.
- Menüde sağ alt köşede sürüm (`v{Application.version}`; C.23 burada yapıldı).
- `FpsCounter` (dev build / editör, F3) ve `FpsTour` (`-fpstour`: menü, seçim, 3 salon, Lucifer; her birinde 4 sn ölçüm, log'a yazıp
  çıkar). `HellPokerBuild.WindowsDevelopment` → `Builds/WindowsDev`.
- İlk ölçümde seçim ekranında ve ilk görülen salonlarda takılmalar vardı (dokular ilk gösterimde yükleniyordu).
  `SalonLibrary.Preload` ile tüm salonlar açılışta yükleniyor; takılmalar kayboldu.

### FPS (dev build, 1920×1080 pencere, vSync, AMD Radeon entegre)
| Ekran | Ortalama | En yavaş kare | >25 ms kare |
|---|---|---|---|
| Menü | 60.0 | 45 FPS | 0 |
| Şeytan seçimi | 59.4-59.9 | 31-47 FPS | 0-2 |
| Mammon | 59.7-59.8 | 32-40 FPS | 0-1 |
| Belial | 60.0 | 43-52 FPS | 0 |
| Lilith | 60.0 | 56-58 FPS | 0 |
| Lucifer | 57.7-60.0 | 20-51 FPS | 0-8 |

(İki tur; takılmalar turdan tura yer değiştiriyor, sistem gürültüsü gibi. Lucifer'in 8'i çağrılma sahnesi sürerken ölçülmüştü: bekleme 3 → 6 sn.)

### Testler
- `BackdropMotionTests`: manifest, ofset, kayma / sarma, varyant yedeği.
- Üretilmiş tüm şeritler için tek test: ≤ 2048 px, 8-12 kare / 8-12 FPS, döngü kapanıyor, yazı bölgelerinde hiç piksel yok.
- `MenuBackdropTests` tek kareye güncellendi. PlayMode `BackdropMotionPlayTests`: menü ve 4 salon hareket ediyor, her parça tam pikselde.
- **545 EditMode (+1 explicit) + 24 PlayMode (+2 explicit) geçiyor.** Ekran görüntüleri yeniden alındı (menü, 4 salon kontrol edildi).

### Kararlar
- Katmanlar gerçek zamanla döner (salonun nefesi), parçacıklar oyun hızına uyar (istek böyle).
- Belial'in sütunları büyük ölçüde şeytan panelinin ve ödeme panelinin arkasında kalıyor (eskiden de öyleydi); görünen hareket perde kıvrımları.
- Yazı bölgeleri ArtGen'de sabit (`TABLE_TEXT`, `MENU_TEXT`) ve testte aynısı var. Masa düzeni değişirse ikisi de güncellenmeli.

### Sıradaki
- Bölüm C: 0.1.1 test build'i.

---

## 2026-10-03 — Plan 6 / Bölüm C: arkadaşlar için test build'i 0.1.1

**İstek:** kökte test çıktısı kalmasın (`/*.xml`); Company "Deniz", Product "Hell Poker", Version "0.1.1"; sürüm menünün köşesinde;
build sonrası `Builds/HellPoker-0.1.1-win64.zip` (klasörün tamamı) + Türkçe OKUBENI.txt ve GERI_BILDIRIM.txt; Belial'e dokunma;
yeni build, açılış ve geçişlerde Player.log temiz; test sayıları, simülasyon, FPS ve zip yolu.

### Yapılanlar
- **21 — daha önce yapılmıştı** (2026-10-02 kaydı): `cshots.xml` silinmiş, .gitignore `/*.xml`. Kontrol edildi: kökte sadece
  yok sayılan test çıktıları var, projenin ihtiyaç duyduğu xml yok.
- **22** — `ProjectSettings.asset`: companyName `Deniz`, productName `Hell Poker`, bundleVersion `0.1.1`.
  Sonuçlar:
  - Player.log artık `%USERPROFILE%\AppData\LocalLow\Deniz\Hell Poker\Player.log`.
  - PlayerPrefs de yeni anahtarda (`HKCU\Software\Deniz\Hell Poker`): 0.1.0'daki kayıtlı koşu ve rekorlar 0.1.1'de görünmez,
    oyun temiz başlar.
- **23** — menünün sağ alt köşesinde `v0.1.1` (`Application.version`, outline'lı; köşe menünün yazı bölgelerinde, hareket girmiyor).
  Bölüm B'de yapıldı.
- **24** — `HellPokerBuild.Windows`:
  - Build klasörünü önce temizler (eski dosya zip'e girmesin).
  - Build sonrası `Pack`: tek klasör `HellPoker-0.1.1/` (exe, `HellPoker_Data`, dll'ler, `MonoBleedingEdge`, `D3D12`) + iki metin.
    Unity'nin `*_BurstDebugInformation_DoNotShip` klasörü dağıtılmıyor.
  - Zip adı `PlayerSettings.bundleVersion`'dan. Builds/ zaten .gitignore'da.
- **25** — `Docs/Release/OKUBENI.txt`: nasıl açılır, SmartScreen "Ek bilgi → Yine de çalıştır", oyunun kısa tarifi (poker bilmeyene
  ipuçları), tüm kontroller, Player.log'un yeni tam yolu ve ne gönderecekleri.
- **26** — `Docs/Release/GERI_BILDIRIM.txt`: 10 açık uçlu soru: ilk izlenim, kurallar, tempo, zorluk / en zor şeytan, hileler adil mi /
  işaret anlaşılır mı / en sinir bozucu hile, Belial'in sahte kartına kanma, BACKFIRE, en sevdiğin / en sıkıcı an, hata. Ayrıca
  oynama süresi ve "poker biliyor musun".
  - Metinlerde `{VERSION}` var; zip'e UTF-8 BOM ve CRLF ile yazılıyor (Not Defteri için).
- **27** — Belial'in göstergesi 2'de kaldı (Bölüm A'da sadece simülasyonda denendi).
- **28** — Build alındı, zip açılıp oradan çalıştırıldı:
  - Normal açılış: 12 sn, log temiz.
  - Duman testi `-fpstour` (artık her build'de çalışıyor): menü → seçim → Mammon → Belial → Lilith → Lucifer (çağrılma) iki tur.
    Log'da hata / uyarı / exception yok.

### FPS (0.1.1 release, zip'ten, 1920×1080 pencere, vSync)
| Ekran | 1. tur (soğuk açılış) | 2. tur |
|---|---|---|
| Menü | 58.6 ort, 3 takılma | 60.1, 0 |
| Şeytan seçimi | 59.0, 2 | 60.0, 0 |
| Mammon | 60.0, 0 | 60.0, 0 |
| Belial | 60.1, 0 | 60.1, 0 |
| Lilith | 60.0, 0 | 60.1, 0 |
| Lucifer | 59.9, 0 | 60.0, 0 |

İlk turdaki birkaç takılma yeni açılmış klasörün ilk çalışması (disk / shader ısınması); ikinci turda hiç yok.

### Testler
- **545 EditMode (+1 explicit simülasyon) + 24 PlayMode (+2 explicit ekran görüntüsü) geçiyor.**

### Teslim
- `Builds/HellPoker-0.1.1-win64.zip` (34 MB).

### Açık sorular
- Belial göstergesi ve Lilith'in açığı: test geri bildiriminden sonra.

---

## 2026-10-03 — Stüdyo logosu (Caveman) ve son test build'i

**İstek:** Açılışta stüdyo logosu: firma adı şimdilik "Caveman". 16-bit piksel art, koyu zemin, yanında küçük bir mağara adamı.
`Assets/Art/Splash/caveman_logo.png`, Sprite. Splash açık, Unity logosu kapalı, logo listede, siyah zemin, ~2 sn. Bununla son test
build'i; kullanıcı itch.io'ya yükleyecek.

### Yapılanlar
- `Tools/ArtGen/pixel_splash.py` (+ `generate_art.py splash`):
  - Oyunun paleti ve başlık fontu (Press Start 2P, yumuşatmasız); "CAVEMAN" oyunun başlığı gibi altından kızıla ateş renginde, 1 px
    koyu gölgeli.
  - Solda 22×32'lik mağara adamı: dağınık saç, saçında kemik, benekli post, omzunda topuzlu sopa; altta taş sıra.
  - 148×44 çizilip ×5 en yakın komşuyla büyütüldü (740×220): Unity splash'i ölçeklese de pikseller kare kalıyor. Zemin saydam.
  - "GAMES" gibi bir ek yazı koymadım (firma adı sadece Caveman).
- `HellPokerArtImporter`: `Assets/Art/Splash/` da Sprite (Single, Point, sıkıştırmasız, mipmap yok).
- `HellPokerBuild.ApplySplashScreen` (menü: Hell Poker ▸ Apply Splash Screen; her build'den önce çağrılır), Unity API'siyle:
  show açık, Unity logosu kapalı, arka plan siyah, overlay 0, animasyon sabit (piksel art zoom'lanmasın), logo 2 sn.
  `ProjectSettings.asset`'te doğrulandı.
  Unity 6'da Personal lisansta Unity logosu kapatılabiliyor.
- Build: `Builds/HellPoker-0.1.1-win64.zip` yeniden üretildi; sürüm 0.1.1 kaldı (0.1.1 henüz dağıtılmamıştı).
  Oyun açılıp pencere görüntüsü alındı: siyah üzerinde Caveman logosu, keskin. Player.log temiz. 545 EditMode test geçiyor.

### Kararlar
- Ayarlar elle değil kodla, her build'de: Project Settings'te biri değiştirse de dağıtılan build hep aynı açılışı alır.

### Sıradaki
- Kullanıcı zip'i itch.io'ya yükleyecek; geri bildirimler (GERI_BILDIRIM.txt) gelince Belial / Lilith kararları.

---

## 2026-10-03 — Kaçış açıkları, alaycı şeytanlar, kin, tohum, diken; 0.1.2 build'i

**İstek (kullanıcı):** Bir prompt listesi: (1) menüden New Game ile kaçış: koşu varken onay sorulsun, el ortasındaysa fold sayılıp ceza
eklensin, ruh masadayken koşu kayıp sayılsın; (2) masa değiştirince hile göstergesi sıfırlanmasın, koşuya bağlı kalsın; (4) üç ayrı
`new Random()` yerine tek ana tohumdan türetilmiş tohumlar; (5) dikenli kart seçilince "+X YEARS" uyarısı, buton "DRAW 1 (+X YEARS)",
diken işareti kalın; diken yıllarından sonra lanet kontrolü; (6) CLAUDE.md'deki v=2 → v=3, kayda yazılıp okunmayan alanlar kaldırılsın.
**Düzeltme (kullanıcı):** 3. madde (mühürlü eli kapatma) ve 5'in son cümlesi (diken yılını sonuç satırına katma) atlandı; testlerde gerçek
sorun olmadıkları görülmüş. (Bahsedilen `hellpoker-claude-code-promptlari.md` diskte bulunamadı; mesajdaki düzeltme uygulandı.)
**Ek istek (iş sürerken):** Oyuncuyu oyunda zorla tutmayalım ama çıkarken şeytanlar küçümsesin ("korktun mu?"); hileli elden kaçana ceza
olarak hile ihtimali artsın ve şeytan "kaçarsan nereye, burası cehennem" gibi alay etsin; metinler şeytanın tipine göre.
Sonunda itch.io için yeni build, eski build'ler silinsin.

### Yapılanlar — Core
- `RandomSeeds` (Randomness): `Fresh()` tek kilitli ana `Random`'dan (Guid tohumlu) taze ana tohum; `Derive(master, stream)` SplitMix32
  karıştırması. `HellPokerGameFactory`: `seed ?? Fresh()` → deste / kasa / hile akışlarına (`DeckStream/HouseStream/CheatStream`) ayrı
  tohum. Eski `seed, seed+1, seed+2` komşu oyunların akışlarını çakıştırıyordu (5'in kasası = 6'nın destesi); tohumsuz oyunda üç
  `new Random()` aynı saat tikinde aynı sayıları üretebiliyordu.
- **Kin (grudge):** `GameRules.GrudgeHands` (3), `GrudgeMalicePerHand` (1). `CheatSession.PlayerFled`: gösterge hemen dolar, kin 3 el
  boyunca her el +1 fazla doldurur. `HandInProgress.FledACheat` (planlanmış ama vurmamış hile) olan el çekilince (`ForfeitHand`) devreye girer.
  `Restore(malice, major, grudge)`, `IHellPokerGame.Grudge`, kayıtta `grudge` (v=3 içinde isteğe bağlı).
- `IHellPokerGame.ForfeitHand()`: oynanan eli, oyun kapatılıp açılmış gibi kapatır (aynı fold / mühür kuralları), masa eller arasına döner.
- Diken: `ThornCost(discards)`; diken yılları laneti geçirirse el orada biter (`Phase = Damned`, `RoundResult.ThornDamned`).
- Kayıt temizliği: `hand.shown` (`HandInProgress.ShownCheatId`) ve koşunun `backfires`'ı (`RunStats.Backfires / NoteBackfires`) yazılıyor
  ama hiçbir yerde okunmuyordu → kaldırıldı (rekorlardaki "Backfires seen" `RecordBook`'ta duruyor). Eski kayıtlardaki bu anahtarlar yok sayılır.

### Yapılanlar — sunum
- **New Game onayı:** `IRunSession.AbandonRisk` (None / Run / Hand / Soul) + `AbandonRun()`; `IMainMenuView.AskToConfirmNewGame(taunt,
  warning)` / `NewGameConfirmed` / `IsConfirming`. `MainMenuView`'da kızgın kutu: üstte şeytanın `Scorn` repliği (kor rengi), altında bedel,
  ABANDON / BACK; Esc (`CloseOverlay`) kapatır. Henüz el dağıtılmamışsa ya da koşu bitmişse sormaz. Quit onaysız kaldı (zorla tutmuyoruz).
- `TablePresenter.AbandonRun`: el ortasındaysa `ForfeitHand()` + el istatistiği; ruh masadaysa / lanetliyse rekora lanet; kayıt silinir,
  `_abandoned` ile masa girdi almaz (`Playing`). `StartNewRun` temiz başlar.
- **Gösterge masaya değil koşuya ait:** `SeatAt` yeni oyuna eski oyunun göstergesini ve kinini taşır (masa değişimi, çağrılma, düşüş);
  yeni şeytanın boyuna kırpılır, `cheat.major` masaya özgü olduğu için sıfırlanır.
- Kaçıştan dönüş: hileli elden kaçana `Hunted` repliği + "You ran from a cheat. The demon holds a grudge..." satırı.
- Replikler (`UiText.Dealers`, her şeytana kendi tonunda): `Scorn` (Mammon tefeci: "Afraid of the interest?", Belial oyuncu: "Stage fright,
  darling?", Lilith gece: "Afraid of the dark already, little one?", Lucifer: "I am where everything ends."), `Hunted` ("Where to? This is
  Hell..." çeşitlemeleri).
- Diken: seçilince uyarı "A thorn! Throwing it back costs +X YEARS, at once.", buton "DRAW 1\n(+X YEARS)" (96 px butona iki satır);
  ruhta sayı yok ("THORN BITES"). Lanete götüren dikende oyuncu başlığı "THE THORN BIT".
- `pixel_ui.py`: diken işareti iki kenarda 3 px kalın kırmızı sarmaşık, büyük kemik rengi dikenler, iki kan damlası (rank köşesi ve orta
  sembol açık); `card_marks.png` yeniden üretildi.
- PlayMode test yardımcıları NewGameButton'dan sonra açılan onayı otomatik geçiyor (`ConfirmNewGame`).
- Sürüm 0.1.2.

### Kararlar
- New Game onayı yalnızca menüde; bitmiş koşunun son ekranındaki NEW GAME sormaz (kaybedilecek bir şey yok).
- Eller arasında koşuyu bırakmak lanet sayılmaz, sadece unutulur (ruh masada değilse). El ortasında bırakmak, kapatıp açmakla aynı bedeli öder.
- Kin, koşu devam ettiğinde anlamlı: oyunu kapatıp açınca uygulanır. New Game ile bırakılan koşu zaten bitiyor, orada sadece alay var.
- Tohum türetmesi tohumlu oyunların kart sırasını da değiştirdi; tohuma bağlı testler etkilenmedi.
### Denge (2000 koşu, gösterge 4 / 2 / 4 / 1; simülasyon da göstergeyi masalar arasında taşıyor)
| Şeytan | Aklanma (önce → şimdi) | Ort. el | Lucifer'e ulaşan | İlk denemede |
|---|---|---|---|---|
| Mammon | %79.7 → %78.0 | 50.6 | %85.3 | %32.2 |
| Belial | %74.1 → %74.7 | 27.0 | %81.6 | %39.8 |
| Lilith | %52.6 → %51.2 | 29.5 | %64.9 | %31.6 |

Lucifer: 0.83 hile / el, geri tepme %10.1 (Yanan Kart %24.8, Düşüş %18.1). Farklar küçük; bir kısmı tohum türetmesinin değiştirdiği
desteler (gürültü), bir kısmı Lucifer'den düşen oyuncunun dolu göstergeyle geri gelmesi. Simülasyon kaçış / kin oynamıyor.

### Testler
- Core: `CheatTests` (kin: dolma, 3 el hızlanma, vurmuş hilede kin yok, geri yükleme; oynanan eli kapatma; `ThornCost`; dikenle anında lanet;
  eski anahtarlı kayıt okuma, v=3 gidiş-dönüş `grudge`), `RandomSeedsTests` (600 farklı akış tohumu, tekrar üretilebilirlik, taze tohumlar,
  aynı anda kurulan iki oyun farklı karılır), `CheatBackfireTests` (eski `backfires` satırı yok sayılır).
- Sunum: `MainMenuPresenterTests` (onay + alay, ABANDON, Esc, risk metinleri, koşu yokken doğrudan seçim), `PactPresenterTests`
  (eller arası / el ortası / ruhta bırakma, sonra temiz yeni koşu, bırakılan masa girdi almıyor), `CheatPresenterTests` (gösterge ve kin
  masa değişiminde taşınır ve kırpılır, hileden kaçışta alay + kin, diken butonu ve uyarısı, ruhta sayısız).
- **572 EditMode (+1 explicit) + 24 PlayMode (+2 explicit) geçiyor.**
### Build 0.1.2 (itch.io için)
- Eski build'ler silindi (`Builds/Windows`, `Builds/WindowsDev`, `HellPoker-0.1.1-win64.zip`). `bundleVersion` 0.1.2.
- `Builds/HellPoker-0.1.2-win64.zip` (~35 MB). Zip'ten açılıp duman testi (`-fpstour`): menü → seçim → Mammon → Belial → Lilith → Lucifer,
  Player.log'da hata / uyarı yok, hepsi ~60 FPS.
- Duman testi bir hata yakaladı: kayıtlı koşu varken tur New Game'e basınca onay kutusu açılıyor, tur şeytan seçemiyordu.
  `FpsTour.Press` NewGame'den sonra onayı geçiyor (PlayMode yardımcıları gibi).

### Sıradaki
- Kullanıcı 0.1.2'yi itch.io'ya yükleyecek. Belial / Lilith denge kararları oyun testi geri bildiriminden sonra.
- Açık: Quit (menüden çıkış) onaysız; koşu el ortasındaysa açılışta forfeit + `Fled` / `Hunted` zaten çalışıyor.
---

## 2026-10-03 — Gösterge oranla taşınıyor (gidip gelme açığı)

**İstek:** `SeatAt` göstergeyi adet olarak taşıyıp yeni şeytanın boyuna kırpıyordu: küçük göstergeli masaya (Belial 2) gidip gelerek
göstergeyi boşaltmak mümkündü (Mammon 3/4 → Belial 2/2 → Mammon 2/4); Lucifer'den düşüşte gelinen şeytanın göstergesi Lucifer'in 1'lik
göstergesinden geliyordu. Oranla, yukarı yuvarlayarak taşı; çağrılmada gelinen şeytanın göstergesini sakla, düşüşte geri yükle; saf
fonksiyon + test; simülasyon aynı formülü kullansın; CLAUDE.md ve DEVLOG.

### Yapılanlar
- `CheatSession.Carry(malice, fromMax, toMax)` (Core, saf): `(malice × toMax + fromMax − 1) / fromMax`, 0 / Max 0 → 0, yeni boyla sınırlı.
- `TablePresenter.SeatAt` bununla taşıyor. `BeSummoned` gelinen şeytanın göstergesini, boyunu ve kinini `_originMalice`'te saklıyor;
  `BeCastDown` SeatAt'ten sonra (Betting'de) onu geri yüklüyor. Yeni koşu / devam / yeniden başlatmada sıfırlanıyor.
- `BalanceSimulation` aynı formülü ve aynı düşüş kuralını kullanıyor.
- Testler: `CheatTests` (Carry için 10 durum + gidip gelme hiçbir boyda göstergeyi düşürmüyor), `CheatPresenterTests`
  (Mammon 3/4 → Belial → Mammon = 4/4; Lilith 4/4 → Belial → Lilith = 4/4; boş gösterge boş kalır; çağrılma + düşüş → Mammon'un 3/4'ü
  geri geliyor, Lucifer'in harcanmış göstergesi değil). **587 EditMode (+1 explicit) geçiyor.**

### Denge (2000 koşu)
Mammon %78.2, Belial %74.7, Lilith %51.7; Lucifer'e ulaşan %85 / %82 / %65, ilk denemede %32 / %40 / %32. Bir öncekiyle neredeyse
aynı: simülasyon oyuncusu masa değiştirmiyor, fark sadece düşüşte geri gelen göstergeden.

### Kararlar
- Saklanan köken göstergesi kayda yazılmadı (format değişmesin). Lucifer masasında kapatılıp açılan koşuda düşüş, Lucifer'in
  göstergesini oranla taşır (1/1 → dolu, 0/1 → boş). İstenirse `origin.malice` alanı eklenebilir.
---

## 2026-10-03 — Dil: İngilizce + Türkçe (altyapı, Türkçe metinler, oyun içi buton, ilk açılış)

**İstek (kullanıcı, üç adım + ek):**
1. Altyapı ve Türkçe metinler: `Language` / `Lang` (Current, Set, Changed); UiText'teki her `const string` → `Lang.Pick(en, tr)`; replik
   dizileri de Türkçe; şeytanların tonu korunsun; terimler ARTIR / GÖR / ÇEKİL / PAS / KART DEĞİŞ / ANTE, el adları Türkçe poker
   terimleri (Kent, Renk, Full, Kare, Floş Royal, Ölü Adamın Eli). Core'da oyuncu metni kalmasın; anahtarlar / id'ler çevrilmesin;
   CurrentCulture değişmesin. `GameSettings.Language` ("settings.language", bozuk → English), `CycleLanguage`, ayarlarda 5. satır.
   Testler + Türkçe ekran görüntüleri, taşan metinleri kısalt.
2. Oyun ekranında dil butonu (MENU / HANDS'in yanında, x 124), ana menüde de; L tuşu; `LocalizedText`; dil değişince ekran yeniden
   açılmadan güncellensin, el ortasında oyun durumu / kayıt / hile / deste değişmesin, sahne yeniden yüklenmesin. Testler.
3. (Ek) İlk açılışta dil `Application.systemLanguage`'a göre (Turkish → Türkçe, diğerleri → English), sonra kayıtlı seçim; batchmode'da hep English.

### Yapılanlar — altyapı ve metinler
- `Ui/Lang.cs`: `Language { English, Turkish }`, `Lang.Current / Set / Changed / Pick<T> / Next / IsTurkish`.
- `UiText.cs`, `UiText.Dealers.cs`, `UiText.Cheats.cs`: ~270 metin `L("…", "…")` özelliği oldu. Anahtarlar const kaldı (`tip.*`,
  `TipCheatPrefix`, `VersionFormat`, geliştirici `FpsFormat`). Her şeytanın Türkçe `DealerText`'i ayrı (`MammonTr`, `BelialTr`,
  `LilithTr`, `LuciferTr`, `UnknownTr`); `UiText.Dealer(id)` dile göre seçer. Lucifer Türkçede "SABAH YILDIZI".
- Ton: Mammon tefeci / defter / faiz ("Faizden mi korktun?"), Belial sahne / "canım" ("Sahne korkusu mu, canım?"), Lilith gece / küçük ruh,
  Lucifer kısa ve ağır ("Her şey bende biter.").
- `UiText.Upper / Lower / CategoryNameUpper`: Türkçe i/İ ı/I'yı elle çevirir; `CultureInfo` hiç değişmiyor (eski `ToUpperInvariant`
  çağrıları buna geçti).
- Core'un `out reason` metinleri artık oyuncuya gösterilmiyor: zincirli kart / fazla kart / el ortasında masa değiştirme presenter'da
  `UiText.LockedChained / LockedTooManyFormat / LockedLeaveMidHand`.
- `GameSettings.Language` (adıyla kaydedilir; yoksa ilk açılış dili, bozuksa English), `CycleLanguage`, `SetLanguage`; yüklenince ve her
  değişimde `Lang.Set`. Ayarlarda 5. satır "LANGUAGE / DİL" (satırlar 32 px'e sıkıştırıldı).
- `AssemblyInfo.cs`: `InternalsVisibleTo` test derlemelerine (UiText'i testler doğrudan okuyor).

### Yapılanlar — oyun içi buton ve anında güncelleme
- `ILanguageButton` (`LanguagePressed`): `ITableView`, `IMainMenuView`, `ISettingsView`. Masada x 124'te 28×18 "TR" / "EN" (gideceği dil),
  ipucu satırı 156'ya kaydı; menüde sağ üst köşe. `ISettingsCommands.CycleLanguage` + L tuşu (her ekranda). Hepsi
  `SettingsPresenter.CycleLanguage`'a gider.
- `LocalizedText` + `UiFactory.Localized(() => UiText.X)`: bütün sabit etiketler (MENU, HANDS, PASS, FOLD, HEP PAS, ANTE, başlıklar, ayar
  satırları, menü butonları, onay kutuları, rekorlar / son ekran butonları, el sıralaması, ödeme tablosu, LIAR, TOSS, RUHUN).
  `MainMenuView` tagline / kurallar / hileler sayfasını `Func<string>` olarak alıyor.
- `TablePresenter.OnLanguageChanged`: animasyonu bitirir, şeytanın adını / unvanını (`IDealerView.Relabel`), ödeme tablosunu ve ceza
  satırını yeniler, `_relabelling` ile `Refresh` (sonuçta duraklama, an, replik, kayıt yok), açık el tablosunu yeniler, son repliği yeni
  dilde bir kez söyler. Şeytan konuşması artık tarif: `Say(d => d.X, sayaç, ruh hali)` — kimin ve hangi replik, dil değişince aynısı
  bulunur. Terk edilmiş masa güncellenmez, kaydedilmez.
- `MainMenuPresenter`: şeytan kartlarını yeniden tarif eder; açık seçim / rekor / son ekranı perdesiz yeniden gösterir, New Game uyarısı
  açıksa yeni dilde sorar. `SettingsPresenter` değerleri ("AÇIK", "HIZLI", "TÜRKÇE") yeniden yazar.

### İlk açılış
- `HellPokerBootstrap.FirstLanguageFor(batchMode, systemLanguage)`: batchmode → English; Türkçe sistem → Turkish; diğer → English.
  `GameSettings(store, firstLanguage)`: kayıtlı dil yoksa bu, varsa kayıtlı olan.

### Ekran görüntüleri
- `HELLPOKER_LANG=tr` aynı turu Türkçe çeker; yeni çekim `08e_new_game_warning` (New Game uyarısı + alay). `Screenshots/` yenilendi,
  Türkçeler `Screenshots/tr/`.
- Kısaltılanlar: "PAS GEÇ / DEĞİŞE" iki satıra sığmıyordu → "HEP / PAS"; İngilizce ipucu satırı ikinci satıra taşıyordu → "D check",
  "L lang". Diğer ekranlar (menü, ayarlar, kurallar, seçim, masa, ruh, onay kutuları, rekorlar, son ekranlar, Lucifer) sığıyor.
- Not: başlık fontunun (Press Start 2P) kendi "İ" glifi noktaya yer açmak için I'yı kısaltıyor, büyük başlıklarda "i"ye benziyor
  ("LANETLENDİN"). Okunuyor; istenirse `fonts.py`'de özel glif çizilebilir.

### Testler
- `LanguageTests` (yeni): kaydet / geri oku; bozuk değer English; ilk açılış dili ve kayıtlının önceliği; `FirstLanguageFor`; dil butonu
  ayarı kaydeder ve döngüler; Türkçe terimler (`UiText.Fold == "ÇEKİL"`, el adları, Türkçe büyük harf); anahtarlar çevrilmez; yansımayla
  bütün UiText metinleri (+ el / hile / hız / pay / ipucu metinleri, 250+): {n} kümeleri iki dilde aynı, boş Türkçe yok; her şeytanın her
  repliği iki dilde; masada Drawing'de dil değişince faz / yıl / bahis / kartlar / seçili kart aynı, mesaj ve buton Türkçe, son replik bir
  kez; sonuç ekranında hiçbir an tekrar oynamıyor, el iki kez sayılmıyor; animasyon sürerken önce atlatılıyor.
- `EnglishByDefault` (SetUpFixture): her EditMode koşusu İngilizce başlar.
- PlayMode `TheLanguageButton_TurnsTheTableTurkish_AtOnce_MidHand`: masada butona bas → "MENÜ", "PAS", buton "EN", el sürüyor, ayar kaydedildi.
- **602 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**

### Sıradaki
- Oyun testinde Türkçe metinlerin tonu ve uzunlukları; istenirse başlık fontuna özel "İ".
---

## 2026-10-04 — Türkçe iyelik ekleri (şeytan adları)

**İstek:** Türkçe hile / rekor satırlarında şeytan adı ek almadan yazılıyordu ("MAMMON hilesi", "LILITH dikeni", "BELIAL masasında
aklanma"). `DealerText.Genitive` (Türkçe elle yazılır, İngilizcede ad + "'s"); sadece gerçekten "-in" isteyen cümleler; sayıya bağlı ekleri
kaldır; testler; Türkçe hile ekran görüntüsü; CLAUDE.md kuralı.

### Yapılanlar
- `DealerText.Genitive` (büyük harfli yerler için: "MAMMON'UN", "BELIAL'IN", "LILITH'IN", "SABAH YILDIZI'NIN", "KASANIN") ve cümle içi için
  `Called` ("Mammon", "Belial", "Lilith", "Sabah Yıldızı", "Kasa") + `CalledGenitive` ("Mammon'un", "Belial'in", "Lilith'in",
  "Sabah Yıldızı'nın", "Kasanın"). İki büyüklük gerekti: rekorlar adı büyük harfle, hile satırları cümle içinde yazıyor.
  Türkçe ek adın sesine göre değişip büyük / küçük harfe kurala göre çevrilemediği için (BELIAL'IN ↔ Belial'in) ikisi de elle.
- Yan hata da kapandı: hile satırındaki ad Türkçede Türkçe küçük harfle çevriliyordu ("LILITH" → "Lılıth"); artık `Called`.
- `UiText.NameInSentence / GenitiveInSentence / GenitiveOf`. `CheatLog` artık `DealerText` alıyor; iyelik isteyen satırlar `{whose}`:
  Çatal Dil (dili kaydı / dili ... yaptı), varsayılan geri tepme (hilesi geri tepti), Yılan Takası (yılanı ... çaldı), Diken (üç satır).
  Özne olanlar aynen ("Mammon haraç aldı", "Mammon, K♠ kartını rehin olarak zincirledi"); Yanan Kart geri tepmesine virgül eklendi.
- Rekorlar: `RecordsDealerFormat` {0} = `GenitiveOf` → "BELIAL'IN masasında aklanma: 2" / "Freed at BELIAL's table: 2" (İngilizce aynı kaldı).
- Sayıya bağlı ek: kurallarda "cezanın en fazla %{3}'u" → "en fazla cezanın %{3} kadarı". Diğer ekli sayılar sabit ("0'a", "400'ün").
- Ödeme tablosu: "Çekil: hep hepsi" → "Çekil: her zaman hepsi".
- Ekran görüntüsü betikleri dilden bağımsız: "NEXT HAND" / "STAND PAT" yerine `UiText.Next / Stand` ya da oyun fazı (Türkçede bazı
  adımlar sessizce atlanıyordu, CheatScreenshots Türkçede kırılıyordu). `CheatScreenshots` da `HELLPOKER_LANG=tr` alıyor; Türkçe hile
  görüntüleri `Screenshots/tr/40–57`. Hile / sonuç satırları sığıyor.

### Testler
- `LanguageTests`: her şeytanın (Lucifer ve bilinmeyen dahil) Türkçe `Genitive / Called / CalledGenitive` dolu; İngilizcede `Genitive`
  boş, metinler "'s" ile ("Freed at BELIAL's table", "Lilith's thorn", "The Morning Star's cheat"); Türkçede iyelik isteyen satırlar ekli,
  özne olanlar eksiz; "Lilith", asla "Lılıth".
- **605 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**
---

## 2026-10-04 — İş 1/7: dil sadece ana menüden değişir

**İstek:** Dil oyun sırasında değiştirilmiyor; masadaki dil butonu ve her ekranda çalışan L gereksiz. Masadaki butonu kaldır, ipucu
satırını eski yerine al; L sadece ana menünün kökünde; köşe butonu ve Ayarlar satırı kalsın. Masa görünmezken dil değiştiği için
`OnLanguageChanged` sadeleşsin: metinler sessizce yenilensin, şeytan yeniden konuşmasın (son replik animasyonsuz), kapı / settle / kayıt
çalışmasın. Testler, ekran görüntüleri, CLAUDE.md.

### Yapılanlar
- Masadaki "TR"/"EN" butonu kaldırıldı; `ITableView` artık `ILanguageButton` değil. İpucu satırı x 126 / 350 px'e döndü, "L lang" /
  "L dil" silindi. Bootstrap `SettingsPresenter`'a sadece menüyü dil butonu olarak veriyor.
- `IMenuCommands.IsAtMenuRoot` (menü görünür, uyarı / kurallar açık değil; `IMainMenuView.IsShowingRules` eklendi). `KeyboardInput`
  L'yi sadece orada işliyor. Ayarlar satırının ipucu: "Ana menüde L tuşu da değiştirir."
- `IDealerView.SetLine`: ekrandaki satırı yeni sözlerle tamamen yazılmış olarak koyar (yazma animasyonu ve bakış yok; satır yoksa
  hiçbir şey). `OnLanguageChanged` artık `Say` değil `SetLine` kullanıyor.
- `Refresh` `_relabelling` iken `PassThroughGate`, `SettleHand`, `SaveRun` çağırmıyor; `Say` ve `Tip` de susuyor.

### Testler
- `LanguageTests`: masa `ILanguageButton` değil, `LanguagePressed` yok; L yalnızca menü kökünde (seçim, masa, el ortası, kurallar,
  uyarıda değil); menüde dil değiştir → Continue → Drawing'de faz / yıl / bahis / kartlar / seçili kart aynı, mesaj ve buton Türkçe,
  `LinesSaid` artmadı (konuşma yok), son satır `SetLine` ile Türkçe, sadece dil kaydedildi; sonuç ekranında hiçbir an tekrar
  oynamıyor; Lucifer eşiğinde (250, Betting) dil değişimi çağırmıyor, Attempts aynı, kayıt yazılmıyor.
- PlayMode `TheMenusLanguageButton_BringsThePlayerBackToATurkishTable_MidHand`: masada dil butonu yok; menüde köşe butonu → "YENİ OYUN",
  Continue → "MENÜ" / "PAS", el sürüyor, ayar kaydedildi; geri İngilizce.
- **608 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.** Masa ekran görüntüleri (EN / TR) yenilendi.
---

## 2026-10-04 — İş 2/7: Quit el ortasında onay soruyor

**İstek:** Menüden Quit el sürerken onaysız çıkıyordu; el sonraki açılışta forfeit ediliyor ama oyuncu bilmeden kaybediyordu. New Game
uyarısının kutusunu kullan: Hand / Soul'da şeytanın Fled repliği + "Leave now and the hand is lost." / "Şimdi gidersen el kaybedilir.",
QUIT / BACK, Esc kapatır; eller arasında onaysız.

### Yapılanlar
- Onay kutusu genelleşti: `IMainMenuView.AskToConfirm(taunt, warning, confirmLabel)` ve `Confirmed` (eskiden `AskToConfirmNewGame` /
  `NewGameConfirmed`). Butonlar `MenuConfirmButton` / `MenuCancelButton` (PlayMode yardımcıları ve FpsTour güncellendi); onay butonunun
  yazısı artık presenter'dan (ABANDON / QUIT).
- `MainMenuPresenter.AskToQuit`: `AbandonRisk` Hand ya da Soul ise sorar (taunt = şeytanın `Fled` repliği), değilse doğrudan çıkar.
  `GoAhead` hangi uyarının açık olduğuna göre (`Asking`) ya koşuyu bırakır ya çıkar. Dil değişiminde açık uyarı yeni dilde yeniden sorulur.
- `UiText.QuitHandWarning` iki dilde. Kısa, kutuya rahat sığıyor (Fled replikleri New Game alaylarıyla aynı uzunlukta).

### Testler
- `MainMenuPresenterTests`: el ortasında ve ruhta Quit soruyor (quitter çağrılmıyor, uyarı, QUIT, Mammon'un Fled repliği), onay
  çıkarıyor (koşu bırakılmıyor); Esc kapatıyor, çıkmıyor; eller arasında doğrudan çıkıyor; New Game uyarısı hâlâ bırakıyor, çıkmıyor.
- **613 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**
---

## 2026-10-04 — İş 3/7: başlık fontuna Türkçe "İ"

**İstek:** Press Start 2P "İ"yi noktaya yer açmak için 4 satıra kısaltıyor, büyük başlıklarda "i" gibi duruyor ("LANETLENDİN").
`fonts.py`'de normal I yüksekliğinde, noktası tek piksel boşlukla üstte bir glif; atlası yeniden üret; Türkçe son ekran ve menü görüntüleri.

### Yapılanlar
- `Tools/ArtGen/fonts.py`: `TITLE_DOTTED_I` (U+0130): 7 sütun × 10 satır bitmap — nokta, boşluk, tam boy I (çubuk, 5 satır gövde,
  çubuk), boş taban satırı. I gibi 8 px ilerler; sol boşluk glifin xMin'inden (`add_glyphs` artık lsb'yi glyph'ten alıyor).
- Nokta em'in (1000 birim) üstünde, 1125–1250 aralığında: satır yüksekliğini artırmak yerine noktayı satır kutusunun üstüne taşırdım,
  böylece hiçbir yerleşim kaymadı. Unity kırpmıyor: menüde "YENİ OYUN", son ekranda "LANETLENDİN" doğru görünüyor.
- `py Tools/ArtGen/fonts.py` ile `HellPokerPixelTitle.ttf` yeniden üretildi. Türkçe ekran görüntüleri yenilendi (`Screenshots/tr/`).

### Testler
- **613 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**
---

## 2026-10-04 — İş 4/7: Günahkâr sınıfları (Köylü, Büyücü, Kral) — açık soruyla durdu

**İstek:** Oyuncu sınıfları: Köylü (kolay, ilk çekilme bedava), Büyücü (Belial'in yalanını görür, masa başına bir küçük hileyi engeller,
büyükleri ve Düşüş'ü engelleyemez), Kral (1250 yıl, kazançta ante çarpanı +1, bir kez bir kartı her hileden korur). `Core/Sinners`,
kayıt v=4 (`class`, `class.charges`), rekorlarda sınıf başına aklanma, New Game → şeytan → sınıf seçimi, masada rozet, Kral'ın K'sı,
görseller, metinler iki dilde, şeytanlara sınıf selamları, SINNERS sayfası, simülasyonda 3 × 3 tablo (Köylü bugüne yakın, Büyücü
Belial'de belirgin avantajlı, Kral Lilith'te ~%50), testler.

### Yapılanlar — Core
- `Core/Sinners/`: `SinnerClass` (soyut; Id, `StartingYears`, `WinAntePercent`, `Ability`, `ChargesPerRun` / `ChargesPerTable`,
  `SeesLies`, `Allows`), `Sinner` (koşunun sınıfı + kalan hak; `ICheatGuard`; `SitDown`, `TrySpend`, `CanUse`, `WardsUsed`),
  `Peasant.cs`, `Warlock.cs`, `King.cs` (her biri kendi dosyası, sayılar yapıcıda), `SinnerRoster` (Peasant, Warlock, King).
- `HellPokerGame(…, sinner)`: Köylü'nün bedava çekilmesi (`Finish`, `RoundResult.FreeFold`), Kral'ın tacı (`Forgiven` ve
  `LeastYearsForgiven`'a ante'nin %'si), `CanProtect / Protect / IsPlayerCardProtected`, `PendingCheatTruth`. Fabrika `sinner`'ı
  hem oyuna hem `CheatSession`'a guard olarak veriyor.
- `CheatMarks.Protected` + `CheatTable.IsUntouchable(i)`: `PlayerTargets` korunan kartı atlıyor; Yanan Kart ve Aysız Gece de.
  Bütün hileler hedefi buradan seçtiği için koruma tek noktadan.
- `RunSnapshot` v=4: `class`, `class.charges` (v1–v3 Köylü, hak dolu). `RecordBook`: `free.class.<id>` (şeytanlarınkiyle karışmaz).

### Yapılanlar — sunum
- `TablePresenter`: `Func<Dealer, Sinner, IHellPokerGame>` (eski tek parametreli yapıcı sınıfsız oyun kurar, eski testler değişmedi);
  `StartNewRun(dealer, class)` sınıfın başlangıç cezası ve şeytanın sınıf selamı (`DealerText.GreetingAs…`); yeni masada / Lucifer'de
  `SitDown`; kayıt ve devamda hak korunur. Büyücü: duyuruda `RevealLie` (bir kez), vuruşta tekrar kırılmaz. `Blocked` → WARD anı,
  `Blocked` repliği (kızgın). Kral: `ToggleProtect` (K / rozet) → koruma modu → kart tıklaması korur (kartlar karar anında da
  tıklanabilir olur); kartta taç işareti. Bedava çekilmede "Dürüst kalp" satırı.
- `MainMenuPresenter`: şeytan seçiminden sonra `ISinnerSelectView` (yoksa Köylü), Esc / GERİ şeytan seçimine döner, dil değişiminde
  perdesiz yenilenir. `SinnerSelectView`: 3 kart (portre ×2, ad, unvan, başlangıç, yetenek, bedeli, SEÇ). `SinnerBadgeView`
  (rozet + hover). How to Play'de 4. sayfa SINNERS / GÜNAHKÂRLAR (sayfa butonu 104 px). Rekorlarda tek satır: "Sınıfa göre aklanma: …".
- Metinler `UiText.Sinners.cs` (iki dilde); her şeytana (Lucifer ve bilinmeyen dahil) `Blocked`, üç şeytana üç sınıf selamı
  ("Bir kral! Ne şans. Taç da teminat sayılır.").
- Görseller: `Tools/ArtGen/pixel_sinners.py` (`generate_art.py sinners`): 48×48 portreler (hasır şapkalı, yabalı köylü; kukuletalı,
  asalı büyücü; taçlı, sakallı, kürk yakalı kral), `Ui/sinner_icons.png` (yaba, göz, taç); `card_marks.png`'ye 5. kare: taç + altın çerçeve.
- PlayMode yardımcıları şeytan seçiminden sonra Köylü'yü seçiyor; `FpsTour` da. Ekran görüntüsünde `03c_sinners`, masada Kral rozeti.

### Denge (2000 koşu × 9)
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %79.3 | %76.0 | %52.3 |
| Büyücü | %81.1 | %76.9 | %55.1 |
| Kral (taç %25) | %76.3 | %71.6 | %49.2 |

Denemeler: Kral taç %100 → 84.8 / 77.0 / 64.2; koruma koşu başına → neredeyse aynı (64.0); başlangıç 1450 → Lilith 55.0; taç %50 → 79.7 /
73.0 / 54.8; **taç %25 → Lilith 49.2 (seçilen)**. Büyücü koruma 3 → 84.1 / 78.1 / 59.1; sınırsız → 86.2 / 78.7 / 60.8.

### Kararlar
- Kral'ın koruması "bir kere" → masa başına bir kez (Büyücü gibi; Lucifer de yeni masa). Draw'dan önceki her kararda kullanılabilir;
  yoksa draw'a girerken vuran hilelere (Yanan Kart) karşı koruyamazdı.
- Kral'ın tacı spesifikasyondaki "+1 ante çarpanı" (%100) yerine **%25**: %100'de Kral her masada en güçlü sınıftı (Lilith %64),
  hedef "Lilith'te bile ~%50". Sayı `King(crownPercent)`'te; geri almak tek satır.
- Büyücü boşa gidecek hileye hakkını harcamaz.

### AÇIK SORU (iş burada durdu; 5., 6., 7. işlere geçilmedi)
- **Büyücü Belial'de belirgin avantajlı olamıyor.** Belial'in hileleri simülasyonda zayıf (hilesiz Belial ~%79; hileli %76): hile
  engellemekle kazanılabilecek en fazla ~3 puan. Büyücü sınırsız korumayla bile Belial'de %78.7 (+2.7), Mammon'da +7, Lilith'te +8.5.
  Seçenekler: (a) Büyücü Belial'in sakladığı kartları görür: Belial masasında kasa 1 yerine 2 kart açar (Belial'e özgü, "yalanın
  içini görür" temasına uygun); (b) Sahte Yüz Büyücü'ye işlemez (gerçek oyuncuda fark eder, simülasyon oyuncusu zaten kanmıyor);
  (c) koruma sayısını artırmak (her masada güçlenir, Belial'de yine az); (d) hedefi bırakmak; (e) Belial'in hilelerini sertleştirmek
  (eski açık soru).

### Testler
- `SinnerTests` (24): roster; Köylü bedava çekilme (bir kez, yeni masada dolmaz); Büyücü küçük hileyi engeller ve **gösterge boşalır**,
  masa başına bir kez / yeni masada dolar, büyük hileyi ve Düşüş'ü engellemez (hak harcanmaz), boşa gidecek hileye hak harcamaz; Kral'ın
  tacı (%25 ve %100), korunan karta **Yanan Kart, Rewrite, Yılan Takası ve Düşüş** dokunmuyor, koruma kuralları (sadece draw'dan önce,
  görünen kart, masa başına bir), başkası koruyamaz; kayıt v=4 gidiş-dönüş, **v=3 kayıt Köylü**; rekorlar sınıf başına; masada: Kral
  1250 + selam + rozet, Büyücü yalanı duyuruda görür, WARD anı + kızgın replik, Kral K + kart, Köylü'nün satırı, sınıf masadan masaya
  (hak masa başına dolar, kayıttan dönüşte dolmaz).
- **637 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**
### Açık sorunun cevabı (kullanıcı: "a") — 2026-10-04
- Büyücü saklanan eli görür: sıradan bir masada kasa 2'den az kart gösteriyorsa Büyücü 2 görür (pratikte yalnızca Belial; Lucifer'in
  "kart göstermez"i korunur). `SinnerClass.HouseCardsShownAt(rules)` (varsayılan masanın sayısı), `Warlock(seenHouseCards: 2)`,
  `HellPokerGame.HouseCardsShown` / `IHellPokerGame.HouseCardsShown` (Advance bunu kullanıyor). Metinler: "Yalanları ve saklanan elleri
  görür…", "Belial'de kasanın 2 kartını görür. Büyük hileler geçer."
- Testler: Büyücü Belial'de 2, Mammon / Lilith'te 2, Lucifer'de 0, Köylü Belial'de 1; Belial masasında Büyücü'nün elinde HouseReveal'da
  2 kart açık. **639 EditMode geçiyor.**
- Simülasyon (Büyücü): Mammon %81.1, **Belial %77.4** (önce 76.9; Köylü 76.0), Lilith %55.1. Avantaj +1.4: simülasyon oyuncusu açık
  kasa kartlarını sadece "Yüksek Kart'la kasada çift görürsem çekilirim" diye kullanıyor; fazladan görülen kart gerçek oyuncuya daha çok
  yarar. Oyuncu modelini zenginleştirmek bütün tabloyu kaydırır; yapılmadı.
---

## 2026-10-04 — İş 5/7: eller arası olaylar

**İstek:** Eller arası olaylar: sadece Betting'de, Lucifer'de yok, her olay en az 2 seçenek (biri hep geç); derinde riskli teklifler;
%12 şans, aynı olay koşuda bir kez, en az 4 el arayla, ayrı zar akışı. İlk 5 olay (Kayıkçı, Ruh Simsarı, Kayıp Ruh, Şeytanın Defteri,
Yanan Köprü). `Core/Events` (IHellEvent, EventDeck, EventSession), kancalar, kayıt (v=4 isteğe bağlı anahtarlar), açıkken kapatma =
geç, olay paneli (portre, perde, Esc = Geç, şeytan tepkisi), ruhta sayı yok, simülasyon (EV > 0 ise kabul, ±3 puan), testler, CLAUDE.md.

### Yapılanlar — Core
- `HandModifier` (ante %, ante birimi, kazanç %, kayıp %, tavan yok, kasa kart sayısı, kazanınca ceza, hayalet tohumu; Encode/Decode)
  ve `RunEffects` (sonraki el, ertelenmiş ceza + kalan el, satılan ruh) — koşuya ait, her masanın oyunu paylaşıyor.
- `HellPokerGame`: `UseEffects`, `ThisHand` (dağıtımda `NextHand` alınır), ante / tavan / kasa kartı / kazanç / kayıp değiştiricileri,
  hayalet el (desteden İki Çift ya da Üçlü), `NextRound`'da ertelenmiş ceza (`DeferredPaid`, lanete götürebilir), `DamnationYears`
  (satılan ruh kadar erken; bütün lanet kontrolleri buna geçti), `IEventTable` (`ForgiveYears` son yılı bırakır, `AddYears`, `EmptyMalice`).
- `IHellEvent`, `EventOptions`, `Events.cs` (5 sınıf, `EventDeck.Standard`), `EventSession` (soğuma, görülenler, `Roll`).
  `GameRules.EventChancePercent` (12), `EventCooldownHands` (4). `HellPokerGameFactory.EventStream` = 3.
- `RunSnapshot` v=4 + `RunEventState` (`event.seen`, `event.since`, `effect.next`, `effect.deferred(.hands)`, `soul.sold`).

### Yapılanlar — sunum
- `TablePresenter(…, EventSession events)`: her "eller arası"nda bir kez `OfferEvent` (Refresh'te, relabel değilken); olay gösterilince
  görüldü kaydedilir; açıkken DEAL gizli, bütün masa girdisi yok sayılır; `ChooseEventOption` / Esc (`CloseOverlay` = geç); şeytan
  tepkisi; dil değişiminde panel yeni dilde (perdesiz). Devamda olay yeniden sunulmaz (o "eller arası" zar atmış sayılır).
- `ITableView.ShowEvent / HideEvent / EventOptionPressed`, `EventCard`, `EventPanelView` (iki siyah perde ortadan, 16 px adım).
- Metinler `UiText.Events` (iki dilde, ruh varyantları sayısız), şeytanlara `EventAccepted` / `EventDeclined`.
- Görseller `pixel_events.py` (`generate_art.py events`): Kayıkçı (fenerli, kukuletalı), Ruh Simsarı (silindir şapkalı, ruh kavanozu),
  Kayıp Ruh (beş kart tutan hayalet), Yanan Köprü.
- Bootstrap: koşu başına `EventSession` (akış 3); **batchmode'da olay şansı 0** (düğmelere basarak oynayan testler rastgele bir teklife
  takılmasın; kurallar EditMode testlerinde). Ekran görüntüsünde `08f_event_*` (panel doğrudan çiziliyor).

### Denge
- İlk deneme (Kayıp Ruh kaybı ×2): Köylü 83.2 / 79.8 / 56.9 — olaysıza göre +3.9 / +3.8 / +4.6 (sınır ±3). Kayıp Ruh her seferinde
  kabul ediliyordu.
- Şans %8: +2.5 / +2.6 / +3.6 (Lilith yine fazla). **Kayıp Ruh kaybı ×3 (seçilen)**: Köylü 80.6 / 77.6 / 53.0 (+1.3 / +1.6 / +0.7),
  Büyücü 82.3 / 78.9 / 55.9, Kral 77.7 / 72.9 / 49.9. Şans %12 kaldı.
- Simülasyon oyuncusu: Kayıkçı (EV 0) ve Kayıp Ruh (×3'te EV 0) reddediliyor; Simsar, Köprü, Belial'in gösterisi kabul; Mammon'un
  ertelemesi (−100) ve Lilith'in pazarlığı reddediliyor.

### Kararlar
- Kayıp Ruh'un kaybı spesifikasyondaki ×2 yerine ×3 (±3 puan kuralı). Sayı `LostSoulEvent(lossPercent)`'te.
- Olaylar koşuya ait (`RunEffects`): masa değişiminde ve Lucifer'e çağrılmada kaybolmaz (ertelenmiş borç da gelir).
- Olay hiçbir zaman cezayı bitirmez (affedilen yıllar son yılı bırakır).

### Testler
- `EventTests` (23): Kayıkçı (ante ve kazanç yarı, tek el), geçmek hiçbir şeyi değiştirmez; Simsar sadece ruhta ve kalan ruh yetiyorsa,
  1/4 ruh + 300 yıl, lanet erkene çekilir; Kayıp Ruh hazır el (İki Çift / Üçlü), kayıp ×3, ×2 değiştiricisi tam iki kat; Mammon'un
  defteri (şimdi −200, beşinci elde +300, aynı anda bir borç), Belial'in gösterisi (kasa 0 kart, kazanç ×2), Lilith'in pazarlığı
  (gösterge boşalır +100, gösterge boşsa çıkmaz), defter Lucifer'de yok; Yanan Köprü (2500 altında yok, 3 birim ante, tavan yok, kazanınca
  1000, ruh geri); oturum (soğuma, koşuda bir kez, Lucifer'de yok, şans 0); standart kurallar; kayıt gidiş-dönüş ve eski kayıt;
  değiştirici gidiş-dönüş; masada: olay cevap bekler (DEAL yok, girdi yok), kabul + tepki, Esc = geç, ruhta metinde rakam yok, açıkken
  kapatılan olay devamda geçilmiş sayılır, koşunun izleri yeni masaya gider, ertelenmiş borcun gelişi söylenir.
- **662 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**
---

## 2026-10-04 — İş 6/7: ses ve müzik

**İstek:** Oyunda hiç ses yoktu. Tools/AudioGen (numpy + wave, chiptune sentezi) ile efektler (kart dağıtma / çevirme, fiş, kazanç küçük /
büyük, kayıp, mühür gongu, hile, geri tepme, ruh, çağrılma, düşüş, tık, geçiş) ve müzik (şeytan başına 30-60 sn döngü, menü teması, ruh
katmanı). `IAudio` (+ Unity havuzu, testlerde sahte), presenter'lar sesi tarif etsin, atlama uzun sesleri kessin, hız ayarı SFX'i
hızlandırmasın. Müzik / Efekt düzeyi (0-10), batchmode'da ses yok. Testler, build boyutu, CLAUDE.md.

### Yapılanlar
- `Tools/AudioGen/synth.py` (pulse, triangle, basamaklı gürültü, ADSR / üstel sönüm, sarmalı karıştırma — döngüde taşan ses başa sarılır,
  `loop_lowpass` — süzgecin durumu dikişte tutar, `fade_edges`), `sounds.py` (14 efekt, 6 döngü), `generate_audio.py`. Üretim ~3 sn, 10.3 MB wav.
  - Müzik: Mammon 68 BPM, Am-F-Dm-E, %12.5 darbeli bas + örs vuruşları (56.5 sn); Belial 116 BPM swing, Dm7-G7-Cmaj7-A7, yürüyen üçgen bas,
    fırça, vibratolu solo (33 sn); Lilith 60 BPM Dm-Bb-Gm-A, üçgen arpej + ped + seyrek solo (48 sn); Lucifer 48 BPM Cm-Ab-Fm-G org +
    pedal (40 sn); menü 76 BPM Em-C-Am-B arpej + uzak çan (38 sn); ruh katmanı uğultu + kalp atışı (16 sn).
  - Döngü dikişleri sayısal olarak denetlendi (son → ilk örnek farkı, ardışık örneklerin %99'luk farkıyla aynı düzeyde).
- Unity: `IAudio` + `SfxIds` + `NullAudio`, `UnityAudio` (8 efekt sesi, müzik, ruh katmanı; Resources/Audio), `HellPokerAudioImporter`.
  - `ITableView.PlaySfx` (masanın kuyruğunda), `TableView.Audio`; `TablePresenter(…, audio)`: dağıt, kart değiştir, fiş (artır / gör),
    çevirme, mühür, hile, geri tepme, ruh (+ katman), çağrılma (+ Lucifer müziği), düşüş (+ geri dönülen şeytanın müziği), kazanç
    (İki Çift ve üstü büyük), kayıp (bedava çekilme hariç); Hurry → `CutLong`; yeni masada ruh katmanı sıfırlanır.
  - `MainMenuPresenter(…, audio)`: her perde geçişinde whoosh; müzik ekrana göre (menü / masadaki şeytan, ruh katmanı).
  - `UiFactory.ButtonClicked` → tık sesi (bootstrap bağlar, sahne kapanınca bırakır).
  - Ayarlar: `GameSettings.MusicVolume / SfxVolume` (0-10, varsayılan 7, döngüsel, bozuk → 7), ayarlar ekranında MÜZİK / EFEKTLER
    (satırlar 26 px'e sıkıştı), `SettingsPresenter(…, audio)` düzeyleri hemen uygular. Batchmode'da `NullAudio`.

### Build boyutu
- Release build 114.2 MB (önce ~117), zip 35.5 MB (0.1.1: 34.6). Ses: efektler 0.95 MB (Vorbis, DecompressOnLoad), müzik akışı
  `resources.resource` 0.92 MB (Vorbis 0.5, Streaming) — sesin toplam payı ~1.9 MB. FPS turu: her ekran 60, Player.log temiz.

### Testler
- `AudioTests` (10): düzeyler kaydedilip okunuyor ve döngüsel (10 → KAPALI), bozuk değer varsayılan; ayarlar ekranı düzeyi gösterir ve
  uygular; el boyunca dağıt / fiş / çevir / büyük kazanç; kayıp; mühür, hile, ruh (+ katman); Hurry uzun sesleri keser; çağrılma sesi ve
  Lucifer müziği; menü teması ↔ masada şeytanın müziği, geçiş sesi.
- **672 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**

### Kararlar
- Efektler `IAudio`'ya doğrudan değil, masanın animasyon kuyruğuyla gidiyor (kazanç sesi kartlar açılmadan çalmasın); testler bunu
  `FakeTableView.Sfx` ile, müziği / kesmeyi `FakeAudio` ile görüyor.
- Sesleri dinleyemedim: tını ve denge oyun testinde kulakla kontrol edilmeli (`generate_audio.py` sayıları değiştirip yeniden üretmek tek komut).
---

## 2026-10-05 — "Rebuild errorr": build log'undaki derleme hataları

**İstek:** Önceki oturum terminal kapanınca yarıda kaldı; son commit "Day 5.01 / Rebuild errorr". Önce derleme / build hatasını bul, düzelt,
testleri ve release build'i çalıştır, nedenini yaz.

### Bulgular
- Editör derlemesi temiz: 0 `error CS`, 0 `warning CS`. EditMode 687/688 (1 explicit), PlayMode 25/27 (2 explicit), kırmızı yok.
- 2026-10-04 23:09'daki `build.log`'da `HellPoker.Core.Events` / `HellPoker.Core.Sinners` için `error CS0234 / CS0246` satırları vardı.
  Ama build aynı log'da **"Build Finished, Result: Success."** ile bitmiş ve zip paketlenmiş. Hata gerçek değildi.
- **Neden:** Bee (Unity'nin derleme sistemi) player build'de önce **bir önceki player build'in DAG'ını** (`Library/Bee/artifacts/1900b0aP.dag`)
  kullanıyor. O build'den sonra eklenen script klasörleri (o gün Events, Sinners; bugün Relics) ilk `csc` çağrısının dosya listesinde yok,
  derleme düşüyor (ExitCode 4). Bee değişikliği görüp DAG'ı yeniliyor, ikinci turda doğru listeyle derliyor. Yeniden ürettim: bugünkü ilk
  build'de aynı hatalar `Core/Relics` için çıktı, sonuç yine Success; hemen ardından ikinci build 0 hata. Exe'deki `HellPoker.Core.dll`
  emanet kodunu içeriyor (`RelicEffects`).
- Kod değişikliği gerekmedi. CLAUDE.md'nin Komutlar bölümüne not düştüm: hüküm "Build Finished, Result" satırı ve çıkış kodu.
- Duman testi (`HellPoker.exe -fpstour`): her ekran 60 FPS, Player.log temiz.

## 2026-10-05 — İş 7/7: lanetli emanetler (kapanış)

**İstek:** İş 7 kodda vardı ama kapanmamıştı (DEVLOG kaydı yoktu). CLAUDE.md'deki "Emanetler" bölümüyle karşılaştır, yarım kalanı
tamamla; RelicTests'i doğrula / eksik testleri ekle; emanetlerle denge (3 × 3, emanetsiz haline göre ±3); `08g_relics` ve iki emanet
olayının ekran görüntüleri (TR + EN, taşan metni kısalt); DEVLOG kaydı.

### Önceki oturumda yapılmış olanlar (kodda bulunan)
- `Core/Relics/Relics.cs`: `RelicEffects` (ante %, kazanç %, kasa kart ±, re-raise +birim, her el +kötülük, yeniden çekme, ruh kaybı %;
  `With` = iki emanet birleşir), `IRelic`, `RelicIds`, Kemik Zar / Paslı Taç / Kayıkçı Sikkesi / Dikenli Tespih, `RelicRoster`
  (`MaxCarried` = 2, `Find`, `Combined`).
- `RelicEvent` (Mezar Soyguncusu, Lanetli Sandık) `EventDeck.Standard`'da; `RunEffects.AddRelic / Relics` (tekrar yok, ikiden fazla yok,
  bilinmeyen yok), kayıt `relics` (v=4, isteğe bağlı; bozuk liste temizlenir).
- `HellPokerGame`: `Relic` dağıtımda sabitlenir; ante, kazanç, kasa kart sayısı, re-raise, kötülük, ruh kaybı kancaları; `CanRedraw / Redraw`
  (zincirli, dikenli, korunan, gizli, görülmemiş kart olmaz; draw'dan sonra olmaz; el başına).
- Sunum: `RelicBarView` (20×20 kutular, hover, Kemik Zar'da kalan hak), `TablePresenter.PressRelic` (zar → kart seç → yeniden çek, tekrar tık
  vazgeç, Refresh seçimi sıfırlar), alınca mesaj, `UiText.Relics` (iki dilde), `pixel_relics.py` → `Ui/relic_icons.png`, olay portreleri.
- `BalanceSimulation`: emanet teklifleri olay destesinde; `HELLPOKER_RELICS` (`none` / zorla verilen id'ler); oyuncu Kemik Zar'ı draw'da atar.
- 23:50'deki son simülasyon `HELLPOKER_RELICS=ferrymans_coin`, sadece Köylü ile koşmuştu (Mammon %85.3); tablo güncellenmemişti.

### Bulunan hatalar ve düzeltmeler
1. **Kazanç yüzdesi sınırlanmış kazanca uygulanıyordu (ciddi).** `Forgiven` önce kazancı kalan cezayla sınırlıyor, sonra olayın / emanetin
   yüzdesini alıyordu. Dikenli Tespih (%90) ile 30 yıl kalınca 27 silinir, 3 kalınca 2, 1 kalınca 0: ceza 1'de takılıyor. Tespih'le
   simülasyonda aklanma **%1.4 / %0.9 / %0.7**, Lucifer'e ulaşan %84 ama ilk denemede yenen %0.1. Aynı hata Kayıkçı'nın yarım kazancında da
   vardı (tek el olduğu için görünmüyordu). Düzeltme: yüzdeler sınırsız kazanca (+ Kral'ın tacı) uygulanır, sınır en son
   (`HellPokerGame.Scaled`). Tespih'le aklanma 72.1 / 71.2 / 41.5'e çıktı (bir lanet olarak beklenen düzey).
2. **DEAL butonundaki ante emaneti bilmiyordu.** `UpcomingAnte` kuralın ante'sini gösteriyordu; Kayıkçı Sikkesi'yle "ANTE 100" yazıp 75 koyuyordu
   (olayın ante'si için de aynı). Dağıtımdaki formül `AnteUnder(stakeBase, el, emanet)`'e taşındı, ikisi de onu kullanıyor.
3. **"Win: at least" satırı** (`LeastYearsForgiven`) olayın / emanetlerin kazanç yüzdesini hiç hesaba katmıyordu: elde o elin, eller arasında
   sıradaki elin yüzdeleriyle hesaplanıyor.
4. **Taşan başlık:** Lanetli Sandık'ın İngilizce başlığı "A LID THAT WANTS OPENING" panelin sağ kenarını aşıyordu → "A LID THAT KNOCKS".
   (Türkçe "AÇILMAK İSTEYEN KAPAK" sığıyor; hover kutuları iki dilde de sığıyor.)

### Denge (2000 koşu × 9, olaylar ve emanetlerle)
Emanetsiz taban (`HELLPOKER_RELICS=none`) = CLAUDE.md'deki eski tablo: Köylü 80.6 / 77.6 / 53.0, Büyücü 82.3 / 78.9 / 55.9, Kral 77.7 / 72.9 / 49.9.

Tek emanet koşu başından (Köylü; Mammon / Belial / Lilith, tabana göre):
| Emanet | Fark |
|---|---|
| Kemik Zar | +11.7 / +11.1 / +21.8 |
| Paslı Taç (%110) | +2.6 / +3.6 / +3.0 |
| Kayıkçı Sikkesi (%75) | +4.7 / +4.1 / +3.3 |
| Dikenli Tespih (düzeltmeden sonra) | −8.5 / −6.4 / −11.5 |

Hata düzeltildikten sonra emanetli tablo: Köylü +2.1 / +0.8 / +2.6, Büyücü +1.4 / +0.5 / **+3.4**, Kral +1.7 / +0.4 / +2.6 → Büyücü-Lilith sınırı aştı.
Denenenler (Büyücü, Lilith): Kemik Zar re-raise 3 birim → 59.4 (etkisiz: re-raise ellerin %4'ünde); Kemik Zar +1 kötülük → 58.8 (ikinci bir
lanet olurdu, tasarım değişir); **Paslı Taç %105 + Sikke ante %80 → 58.6 (seçilen)**.

| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %81.6 (+1.0) | %78.1 (+0.5) | %54.8 (+1.8) |
| Büyücü | %83.3 (+1.0) | %79.1 (+0.2) | %58.6 (+2.7) |
| Kral | %78.4 (+0.7) | %73.0 (+0.1) | %51.4 (+1.5) |

Oyuncu teklifleri neredeyse her zaman alıyor (Sandık 6549/6943, Soyguncu 6418/6832): `ExpectedYears` kalan emanetlerin ortalaması (+10).
Ort. el (Köylü) ~54 / ~27 / ~31; hile / el 0.30 / 0.47 / 0.40, Lucifer 0.84.

### Kararlar
- Sayılar ayarlandı, tasarım değil: Paslı Taç kazanç %110 → **%105** ("yirmide bir fazla"), Kayıkçı Sikkesi ante %75 → **%80** ("beşte dört").
  Kemik Zar en güçlü emanet ama ona ikinci bir lanet eklemek yerine diğer iki pozitif emaneti hafiflettim.
- Kayıkçı'nın yarım kazancı da düzeltmeden etkilendi (artık son yılları silebiliyor); olaysız tabloya dokunmuyor.

### Testler
- `RelicTests` 15 → **23**: Kemik Zar dikenli karta, örtülü (gizli) karta ve Kral'ın koruduğu karta atılamaz; emanetler masa değişiminde
  kalır ve orada çalışır; Lucifer'e çağrılınca kalır, zar onun masasında da atılır; ruh masadayken emanet olayı / mesajı / lütuf-lanet
  metinlerinde rakam yok; kesilmiş kazanç son yılları siler (Tespih, Kayıkçı — regresyon); DEAL'daki ante ve "en az" satırı emanetleri bilir.
- Ekran görüntüleri: `08f_event_grave_robber`, `08f_event_cursed_chest`, `08g_relics` (Kemik Zar hover), `08g_relics_crown` (Paslı Taç hover);
  TR (`HELLPOKER_LANG=tr`) ve EN, `Screenshots/en`, `Screenshots/tr`.
- **695 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.** Release build 114 MB, zip 35 MB; duman turu 60 FPS, log temiz.

### Temizlik
- `Tools/AudioGen/__pycache__/` git'ten çıkarıldı (`git rm -r --cached`); `.gitignore`'da `Tools/ArtGen/__pycache__/` yerine genel
  `__pycache__/` ve `*.pyc`.

### Açık sorular
- Kemik Zar tek başına çok güçlü (Lilith +22). Tek emanet olarak oyunda hissedilir; oyun testinde "zar her eli kurtarıyor" denirse seçenekler:
  sadece kart açılırken (5. karttan önce) atılabilsin, ya da el başına değil masa başına bir kez.
- Simülasyon oyuncusu emanet tekliflerini kör kabul ediyor (beklenen değer ortalaması). Gerçek oyuncu Tespih'i istemeyebilir; teklif
  "zarla bir emanet" olduğu için bunu bilemez. Bilinçli tercih mi, yoksa teklif emaneti önceden göstermeli mi?
- Kemik Zar'ın rozeti el bitince 0 gösteriyor (bu elde kalan hak); sıradaki DEAL'da 1'e döner. Kafa karıştırırsa sonuç ekranında da dolu gösterilebilir.
---

## 2026-10-05 — 7 işlik oturumun kapanışı

İş 1–7 tamam: dil sadece ana menüden; Quit el ortasında onay; başlık fontunda Türkçe "İ"; günahkâr sınıfları (Köylü, Büyücü, Kral);
eller arası olaylar; ses ve müzik; lanetli emanetler. Bu oturumda ayrıca: build log'undaki sahte derleme hatalarının nedeni bulundu,
kazanç yüzdesinin sınır sırası (Tespih / Kayıkçı) ve DEAL ante'si düzeltildi, `__pycache__` temizlendi.

**Durum:** 695 EditMode + 25 PlayMode yeşil; release build 0.1.2 (sürüm numarası değişmedi) temiz.

**Sıradaki adımlar:**
- Oyun testi (Docs/PLAYTEST.md): sınıflar, olaylar, emanetler (özellikle Kemik Zar), ses dengesi kulakla.
- Yeni test build'i için Player Settings ▸ Version'ı 0.1.3'e çıkarıp build (zip adı sürümden geliyor).
- Açık sorular: Belial'in hilelerinin sertliği (eski), Kemik Zar'ın gücü, emanet teklifinin içeriğini gösterme.
---

## 2026-10-05 — Kemik Zar zayıflatıldı (masa başına 1) ve 0.1.3 build'i

**İstek:** Kemik Zar tek başına çok güçlü (Köylü: Lilith +21.8, Mammon / Belial +11). Zayıflat. (1) A: sadece kartlar açılırken
(5. kart görünmeden) atılabilsin; (2) Lilith'te fark hâlâ +8 ya da üstündeyse A'yı geri al, B: masa başına 1 kez (yeni masada ve
Lucifer'e çağrılınca dolar), kalan hak kayda (v=4 isteğe bağlı anahtar, eski kayıtta dolu). RelicTests'e yeni kuralın testleri; tam 3 × 3
denge, CLAUDE.md; sürüm 0.1.3, release build, zip'ten açıp duman testi, Player.log; build hükmü "Build Finished, Result" satırından.
(Not: 1. maddenin metni bana ulaşmadı; A'yı önceki kayıttaki açık sorudaki seçenek olarak uyguladım. 3. maddenin sonu kesikti:
"hiçbir hücre" → emanetsiz haline göre ±3'ten fazla oynamasın diye okudum.)

### A: sadece kartlar açılırken — yetmedi, geri alındı
- `CanRedraw` yalnız `PlayerReveal`'da, 5 kart açılmadan; simülasyon oyuncusu son fırsatta (4 kart açık) gördüğü kartlardan eşleşmeyen en
  düşüğünü atıyor. Köylü, Kemik Zar baştan: **91.9 / 86.8 / 68.7 → +11.3 / +9.2 / +15.7**. Lilith +15.7 ≥ 8 → A geri alındı.
  Gücün kaynağı bilgi değil, fazladan kart değişimi: 4 kart görmek ile 5 kart görmek arasında fark yok.

### B: masa başına 1 kez (seçilen)
- Core: `RelicEffects.RedrawsPerHand` → `RedrawsPerTable`. Hak koşuya ait: `RunEffects.RedrawsLeft`, `SitDown()` (yeni masada doldurur),
  `SpendRedraw()`; `AddRelic` yeni zarı dolu getirir; `Restore(..., redrawsLeft)` (-1 = dolu). `HellPokerGame.RedrawsLeft` buradan okur,
  elde artık sıfırlanmaz / dolmaz. Zar yine draw'dan önce (kart açılırken ya da draw ekranında) atılır.
- Presenter: masa değişimi, Lucifer'e çağrılma ve düşüş `_effects.SitDown()` (sınıfın `SitDown`'ı ile aynı yerlerde). Rozet bu masada
  kalan hakkı gösteriyor (el bitince "0"ın kafa karıştırma sorunu da gitti: hak gerçekten 0).
- Kayıt: `relics.redraws` (v=4, isteğe bağlı; sadece emanet varken yazılır; yoksa dolu). Kapatıp açmak hakkı doldurmaz.
- Metinler (iki dilde): "Once a table, before the draw, redraw a card." / "Masa başına bir kez, değişten önce bir kartı yeniden çek.";
  uyarı "rolls once a table (a new table fills it)" / "masa başına bir kez (yeni masada dolar)".
- Simülasyon oyuncusu tek hakkı saklıyor: draw'da, çift ya da daha iyi bir elde atıyor. `BoneDie.ExpectedYears` 60 → 25.
- Ölçüm (Köylü, Kemik Zar baştan): **82.3 / 80.2 / 56.9 → +1.7 / +2.6 / +3.9** (diğer emanetlerle aynı düzey).

### Denge (2000 koşu × 9, olaylar ve emanetlerle; parantezde emanetsiz tabana göre)
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %79.4 (−1.2) | %77.6 (0.0) | %51.1 (−1.9) |
| Büyücü | %82.4 (+0.1) | %78.7 (−0.2) | %55.9 (0.0) |
| Kral | %77.0 (−0.7) | %72.2 (−0.7) | %49.1 (−0.8) |

Hiçbir hücre ±3'ü aşmıyor. Emanetler artık ortalamada hafif eksi (Tespih'in laneti ağır basıyor); oyuncu teklifleri ~%85 alıyor
(Sandık 5986/7032, Soyguncu 5752/6960; zar bir kez alınınca kalanların beklenen değeri eksiye düşüyor, ikinci teklifi reddediyor).

### Testler
- `RelicTests` 23 → **26**: zar bir kez atılınca masa boyunca harcanmış (sonraki eller dolmaz), atılmayan hak sonraki ele kalır,
  `SitDown` doldurur (emanetsiz koşuda 0); masada: at → sonraki elde "once a table" → kayıt `relics.redraws=0` → devamda hâlâ 0 →
  yeni masada 1; Lucifer'e çağrılınca dolu; kayıt gidiş-dönüş, anahtarı olmayan eski kayıtta dolu, zarsız kayıtta sayı anlamsız.
- **698 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**

### 0.1.3 build'i
- `bundleVersion` 0.1.2 → **0.1.3**. `HellPokerBuild.Windows`: "Build Finished, Result: Success", 114 MB, `Builds/HellPoker-0.1.3-win64.zip` 35 MB
  (bu sefer Bee'nin ilk tur CS satırları da yok: yeni klasör eklenmedi).
- Zip açıldı (`HellPoker-0.1.3/`: exe, _Data, MonoBleedingEdge, D3D12, OKUBENI.txt, GERI_BILDIRIM.txt) ve oradaki exe ile `-fpstour`:
  Player.log'da hata / istisna yok. İlk açılışta menüde 4 takılma (en yavaş 8.6 FPS) ve salonlarda birer tane; ikinci açılışta her ekran
  60.0, 0 takılma → yeni açılmış klasörün soğuk açılışı (diskten ilk okuma), oyunla ilgili değil.

### Açık sorular
- 1. maddenin (A) tam metni ulaşmadı; yorumum yanlışsa söyle. Sonuç değişmez: A ölçüldü, yetmedi, B uygulandı.
- Kemik Zar'ın laneti (kasa re-raise'i 2 birim) simülasyonda neredeyse etkisiz (re-raise ellerin ~%4'ü). Masa başına tek hakla
  zar artık dengeli; lanet hissedilmiyorsa oyun testinden sonra düşünülebilir.
- Emanet teklifinin içeriği gizli (zarla): oyuncu Tespih'i istemeyebilir — önceki kayıttaki soru duruyor.
---

## 2026-10-05 — Açık kapatıldı: masa başına haklar şeytan başına (masa zıplamak doldurmaz)

**İstek:** (Önce: "soluk rozet + iki dilde neden" denemesini geri al — kullanıcı istemedi, tamamen geri alındı; dosyalar "day 6.02" ile
aynı hale döndü.) Sonra açığı kapat: `TablePresenter.SwitchTable` her masa değişiminde `_sinner.SitDown()` / `_effects.SitDown()`
çağırıyordu; masa değiştirmek eller arasında serbest ve bedelsiz (ruh masada değilse). Kemik Zar'ı atıp Mammon → Belial → Mammon
gidip gelerek zar her elde yeniden doluyor, az önce kapatılan "el başına zar" gücünü geri getiriyordu; aynısı Büyücü'nün koruması ve
Kral'ın tacı için. Kural: bir şeytanın masasında harcanan hak, o masaya dönünce dolu gelmez; hiç oturulmamış şeytanın masası dolu başlar.
Lucifer her çağrılmada dolu (yeni deneme); düşüşte gelinen şeytanın kalan hakkı. Sinner ve RunEffects için aynı kural, ortak küçük bir
sınıf. Metinler iki dilde ("her şeytanın masasında bir kez (Lucifer'e her çağrılmada dolar)"), How to Play SINNERS sayfası. Testler;
simülasyonda masa zıplayan oyuncu (normal oyuncuya göre +3'ü geçmesin); ana tablo yeniden; CLAUDE.md; DEVLOG; 0.1.3 dağıtılmadıysa aynı
numarayla build.

### Yapılanlar
- `Core/Game/TableCharges`: şeytan başına hak defteri (`SitAt(dealerId, fresh)`, `TrySpend`, `Left`, `Refill`, `Encode` / `Restore`).
  Hiç oturulmamış masa dolu; `fresh` (Lucifer'e çağrılma) o şeytanın kaydını siler; eski kayıttaki tek sayı ilk oturulan masaya gider.
- `Sinner`: masa başına yetenek `TableCharges`'la (koşu başına — Köylü — eskisi gibi tek sayı). `SitDown()` kalktı, yerine `SitAt`.
- `RunEffects`: Kemik Zar hakkı `TableCharges`'la; yeni bir zar her masada dolu gelir (`Refill`). `SitDown()` kalktı, yerine `SitAt`.
- `TablePresenter`: üç dağınık `SitDown` çağrısı kalktı; **tek yer** `SeatAt`: her oturuşta (yeni koşu, devam, masa değişimi, çağrılma,
  düşüş) `SitAt(dealer.Id, fresh: change == Summoned)`. Aynı şeytana yeniden oturmak da bir şey doldurmaz.
- Kayıt (v=4, isteğe bağlı): `class.charges.tables`, `relics.redraws.tables` ("mammon:0", sadece hak harcanmış masalar). Eski kayıt: tek
  sayı (`class.charges` / `relics.redraws`) kaydedildiği masaya, diğerleri dolu. Bozuk girdiler atlanır, değer doluyu aşamaz.
- Metinler (iki dilde): Kemik Zar lütfu ve uyarısı "Once at each demon's table (full again at every summons to Lucifer)..." /
  "Her şeytanın masasında bir kez (Lucifer'e her çağrılmada dolar)..."; Büyücü / Kral yetenekleri "her şeytanın masasında"; Kral'ın
  "korumasını kullandı" uyarısı; SINNERS sayfasının girişi kuralı anlatıyor (sayfa sığıyor: `02c_rules_sinners`).
- Emanet hover kutusu artık metnin boyuna göre (Kemik Zar'ın lütfu 2 satır; sabit 38 px'te taşacaktı). TR / EN `08g_relics` kontrol edildi.
- Ekran görüntüsü turuna `02c_rules_sinners` eklendi.

### Simülasyon (2000 koşu × 9)
- `BalanceSimulation`: oturuşlarda `SitAt` (gerçek masa gibi); `HELLPOKER_HOP=1` masa zıplayan oyuncu (bir masa başına hak harcanınca,
  ruh masada değilse, başka bir sıradan şeytana geçip hemen döner); `HELLPOKER_HOP_OLD=1` aynısı eski "her oturuş doldurur" kuralıyla;
  `HELLPOKER_SEED` tohumları kaydırır (aynı kurulumun ikinci örneği).
- Ana tablo (değişmesi beklenmiyordu): Köylü 79.4 / 77.6 / 50.9, Büyücü 81.5 / 78.5 / 54.9, Kral 77.1 / 72.1 / 48.8 — öncekine göre en fazla
  −1.0 (Büyücü: düşüşte gelinen şeytanın koruması artık kendiliğinden dolmuyor).
- **Açık gerçekti** (eski kural, zıplayan oyuncu − normal): Büyücü +5.4 / +0.2 / +6.1, Köylü +0.4 / 0.0 / +1.5, Kral +0.3 / −0.1 / +0.9;
  Kemik Zar zorlanmış Köylü +5.1 / +4.2 / **+10.0**.
- **Yeni kural** (zıplayan − normal): Köylü −0.4 / −0.5 / +0.3, Büyücü −1.8 / 0.0 / +1.5, Kral +0.9 / −0.6 / −0.2. Kemik Zar zorlanmış Köylü
  ilk örnekte Lilith +4.0 çıktı; iki tohumla tekrar: −0.5 ve −1.3 → 3 örnek ortalaması −1.2 / +0.2 / +0.7 (ilk +4.0 gürültü: fark SE ~1.6).
  Hiçbir hücre +3'ü geçmiyor.

### Testler
- Yeni: `HoppingTables_DoesNotRefillTheDie` (at → Belial dolu → Mammon hâlâ 0), `HoppingTables_DoesNotRefillTheWardOrTheCrown` (Büyücü,
  Kral; Belial ve Lilith dolu, Mammon 0), `ThePerDemonCharges_AreSaved_AndAnOlderSaveKeepsItsOneNumber` (kayıt + devam + eski kayıt),
  `TableCharges_BrokenSaveEntriesAreSkipped`; Lucifer testi: çağrılınca dolu, **düşüşte Mammon'un kalan hakkı (0)**.
- Güncellenen: Büyücü / Kral / Köylü çekirdek testleri `SitAt` ile ("geri dönünce hâlâ 0", Lucifer taze); Kemik Zar "yeni şeytan doldurur".
- **703 EditMode (+1 explicit) + 25 PlayMode (+2 explicit) geçiyor.**

### Build (0.1.3, aynı numara)
- 0.1.3 dağıtılmadı varsayıldı (bu oturumda sadece yerelde build alındı). "Build Finished, Result: Success" (log'da Bee'nin ilk tur iki CS
  satırı: silinen `RedrawRefusal.cs` / yeni `TableCharges.cs`). `Builds/HellPoker-0.1.3-win64.zip` 35 MB.
- Zip `Builds/Smoke-0.1.3-*` altına açılıp oradaki exe ile iki kez `-fpstour`: Player.log temiz; 1. açılışta soğuk açılış takılmaları
  (menü 3, Lucifer 1), 2. açılışta her ekran 60.0 / 0 takılma. Not: zip'i scratchpad'in uzun yoluna açmak 260 karakter sınırına takıldı.

### Açık sorular
- 0.1.3 arkadaşlara gönderildiyse bu build 0.1.4 olmalı (söylersen numarayı çıkarıp yeniden alırım).
- Masa zıplamak hâlâ bedava ve malice'i oranla taşıyor (yukarı yuvarlanarak); simülasyonda zararsız, ama kasıtlı zıplamayı tamamen
  anlamsız kılmak istenirse (ör. aynı el arası ikinci masa değişimi yasak) ayrı bir karar.
---

## 2026-10-05 — Oyun testine hazırlık: test belgeleri ve 0.1.3 zip'i

**İstek:** Geliştirme bitti, oyun test edilecek. Kod ve oyun kurallarına dokunma; sadece test belgelerini güncelle, zip'i yeniden al.
GERI_BILDIRIM.txt'deki soruları koru, sınıf / olay / emanet (Kemik Zar) / ses-müzik / dil sorularını ekle. DEVLOG'a kısa kayıt.

### Yapılanlar (kod değişmedi)
- `Docs/Release/GERI_BILDIRIM.txt`: 1–10 aynen; yeni 11 Sınıf, 12 Eller arası olaylar, 13 Emanetler (Kemik Zar özellikle), 14 Ses ve müzik, 15 Dil.
- `Docs/Release/OKUBENI.txt`: sınıf seçimi, olay paneli ve emanetler (iki cümle), How to Play / Nasıl Oynanır sayfaları, dil (ilk açılış
  Windows diline göre; TR / EN butonu, L), kontrollere K (Kral) ve L, ayarlarda müzik / efekt düzeyi, Kemik Zar'ın kullanımı.
- `Docs/PLAYTEST.md`: başlıkta dil / sınıf / hız / ses alanları ve K, L kısayolları; sınıf seçimi izlenimi; zorluk tablosuna sınıf sütunu;
  yeni bölümler 5 Sınıflar, 6 Olaylar ve emanetler (Kemik Zar ayrıca), 7 Ses ve dil; eski 5–7 → 8–10 (içerik aynı, SINNERS sayfası eklendi).
- Build: "Build Finished, Result: Success"; zip'teki OKUBENI / GERI_BILDIRIM yeni hali, `{VERSION}` → 0.1.3 (kontrol edildi).

### Durum
- Zip: `Builds/HellPoker-0.1.3-win64.zip`, 35.5 MB (174 dosya).
- Testler: kod son test turundan beri değişmedi — **703 EditMode (+1 explicit) + 25 PlayMode (+2 explicit)** geçiyor.
---

## 2026-10-05 — Ayarlar: üç sekme, görüntü seçenekleri, ana ses (0.1.4)

**İstek:** Ayarlar ekranına görüntü seçenekleri; oyun kurallarına ve oyun içi ekranlara dokunma. Sekmeler (buton + Q / E ya da sol / sağ ok,
Esc geri, satırlar 32 px); GÖRÜNTÜ: pencere boyutu (ekranın sığdırabildiği en büyük kata kadar + OTOMATİK, varsayılan; sığmayan kat
listede yok), PİKSEL ÖLÇEĞİ (tam ekranda) TAM PİKSEL (varsayılan) / EKRANI DOLDUR (ipucu: pikseller hafif eşitsiz olabilir; `PixelScreen`'e
mod, tıklama alanları doğru kalsın), DİKEY SENKRON (kapalıyken 60 FPS); SES: ANA SES (müzik ve efekt onun çarpanı, `IAudio.SetVolumes`'a
gitsin); kayıt `settings.display.mode / .scale (0 = otomatik) / .fill`, `settings.vsync`, `settings.master`; testler; TR / EN ekran
görüntüleri; build (0.1.3 dağıtıldıysa 0.1.4), zip'ten duman testi; DEVLOG; CLAUDE.md. Mesaj parça parça geldi (1., 2. maddenin başı,
5. ve 6. madde eksikti); kullanıcı: "sen nasıl uygun görüyorsan öyle yap".

### Kararlar (eksik parçalar için)
- Sekmeler: **OYUN** (hız, el rehberi, ipuçları, dil), **GÖRÜNTÜ** (görüntü modu, pencere boyutu, piksel ölçeği, dikey senkron), **SES** (ana ses,
  müzik, efektler). Son açık sekme hatırlanır; Esc her sekmeden menüye.
- GÖRÜNTÜ MODU: TAM EKRAN (bugünkü kenarlıksız) / PENCERE. Özel (exclusive) tam ekran eklenmedi.
- PENCERE BOYUTU: OTOMATİK + **×2'den** sığan en büyük kata (×1 = 480×270 çok küçük). "Sığmak": genişlik ≤ ekran, yükseklik ≤ ekranın %92'si
  (başlık + görev çubuğu); 1080p'de ×2, ×3 (×4 = ekranın kendisi, sığmaz).
- Kilitli satırlar (soluk, basınca bir şey olmaz): tam ekranda PENCERE BOYUTU, pencerede PİKSEL ÖLÇEĞİ (pencere zaten tam kat).
- ANA SES varsayılan **10** (eski müzik / efekt düzeyleri aynı duyulsun). 5. ve 6. madde için ek bir şey yapılmadı.
- Sürüm **0.1.4**: 0.1.3 test belgeleriyle birlikte dağıtılmak üzere hazırlanmıştı.

### Yapılanlar
- `GameSettings`: `WindowMode` (Fullscreen / Windowed; `Fullscreen` artık ondan), `WindowScale` (0 = otomatik), `FillScreen`, `VSync`,
  `MasterVolume`; anahtarlar `settings.display.mode / scale / fill`, `settings.vsync`, `settings.master`. Eski `settings.fullscreen` yeni anahtar
  yokken taşınır ve hâlâ yazılır (eski build okusun). Bozuk: mod → tam ekran, kat (<2, >32) → otomatik, fill ≠ 1 → kapalı, master → 10.
- `Settings/WindowScales` (saf aritmetik): `Largest`, `Choices`, `Auto` (eski %85 kuralı), `Resolve` (kayıtlı kat bu ekrana sığmıyorsa otomatik).
- `IDisplayMode`: `DisplayWidth / Height` + `Apply(fullscreen, windowScale, fill, vSync)`; `UnityDisplayMode` vSync + `targetFrameRate` (kapalıyken 60),
  `PixelScreen.FillScreen` (sadece tam ekranda), çözünürlük (editörde 1920×1080 sayılır, pencere değişmez). Presenter görüntüyü sadece bir görüntü
  ayarı değişince uygular (otomatik ile aynı kat iki kez uygulanmaz).
- `PixelScreen.FillScreen`: ölçek `min(w/480, h/270)` (kesirli); tıklama alanları RectTransform'larla ölçeklendiği için iki modda da doğru;
  başka kod `Scale`'i konum hesabında kullanmıyor (kontrol edildi).
- `IAudio.SetVolumes(master, music, sfx)`; `UnityAudio` müzik / ruh katmanı / efektleri ana sesle çarpar.
- `ISettingsView`: sekme olayı + yeni satır olayları, `Render(SettingsScreen)` (ekranın bütün kelimeleri tek nesnede). `SettingsView`: üstte üç sekme
  (etkin olmayan soluk), her sekme ayrı kapta, satırlar 32 px, altta "Q / E ya da ok tuşları sekmeyi değiştirir. Esc: geri.".
- `ISettingsCommands.IsSettingsOpen / NextTab / PreviousTab`; `KeyboardInput`: ayarlar açıkken Q / E ve oklar.
- Metinler iki dilde (`UiText`): sekmeler, GÖRÜNTÜ MODU / PENCERE, PENCERE BOYUTU / OTOMATİK / "×3  1440×810", PİKSEL ÖLÇEĞİ / TAM PİKSEL /
  EKRANI DOLDUR, DİKEY SENKRON, ANA SES ve ipuçları. Ekran görüntüsünde PİKSEL ÖLÇEĞİ ipucu iki satıra kayıp alttaki başlığa yapışıyordu → tek satıra
  kısaltıldı ("Tam ekranda. Doldur: pikseller hafif eşitsiz olabilir.").
- OKUBENI.txt (ayarların üç sekmesi) ve PLAYTEST.md (ayarlar satırı) güncellendi; sürüm 0.1.4.

### Testler
- `SettingsTests` 7 → 26: kayıt / geri okuma (yeni anahtarlar, eski anahtar da yazılıyor), bozuk değerler, bozuk pencere katı (−3, 1, 99) otomatik,
  eski `settings.fullscreen` taşınır (0 / 1), yeni anahtar eskisine üstün; sahte ekranlarla pencere listesi (1080p → oto, ×2, ×3; 1440p → ×4'e kadar;
  4K → ×7; 1366×768 / 1280×720 → ×2; 800×600 ve 0×0 → sadece oto), otomatik = eski davranış, sığmayan kayıtlı kat otomatik; presenter: tam ekranda
  pencere boyutu kilitli (basınca değişmez), pencerede döner (×2 → ×3 → ×4 → OTO) ve her yeni boyut uygulanır, pencerede doldur kilitli; doldur ve vSync
  uygulanır; ana sesin çarpanı; sekmeler (buton, ileri / geri döner, ayar değişince sekme kalır); görüntü kelimeleri iki dilde; Alt+Enter.
  (Yeniden yazarken düşen iki eski test — hız döngüsü, ipuçlarını sıfırlama — geri eklendi.)
- `MainMenuPresenterTests`: Esc her sekmeden menüye; ayarlar kapalıyken `IsSettingsOpen` yanlış (Q / E bir şey yapmaz).
- PlayMode `TheSettings_DisplayTab_WindowSize_IsSaved`: AYARLAR → GÖRÜNTÜ sekmesi (oyun sekmesi gizli) → PENCERE → PENCERE BOYUTU → ×2,
  `settings.display.scale = 2` kaydedildi → GERİ.
- Ekran görüntüleri: `01b_settings`, `01c_settings_display`, `01d_settings_sound`, TR ve EN — taşma yok.
- **723 EditMode (+1 explicit) + 26 PlayMode (+2 explicit) geçiyor.**

### Build 0.1.4
- "Build Finished, Result: Success"; `Builds/HellPoker-0.1.4-win64.zip` 35.5 MB. Zip `Builds/Smoke-0.1.4-*` altına açıldı, oradaki exe ile `-fpstour`:
  Player.log'da hata yok. İlk koşu "choose a demon"dan sonra ilerlemedi (240 sn'de kesildi); sonraki 4 koşu normal (her biri 44 sn, 60 FPS).
  Olası neden: Player Settings'te `runInBackground: 0` — pencere ilk açılışta odak alamazsa oyun duraklar, tur da bekler. Kod / kural değil, ayara
  dokunulmadı; o koşunun log'u bir sonraki açılışta üzerine yazıldı.

### Açık sorular
- `runInBackground` açılsın mı? (Arka planda müzik çalmaya devam eder; testçi alt-tab yapınca oyun durmaz.)
- 5. ve 6. maddeler mesajda hiç gelmedi; bir şey isteniyorduysa sonra eklenir.
---

## 2026-10-05 — Sınıf güçleri şarj göstergesine, sınıf kartı çakışması, koşu günlüğü, pencere / odak (0.1.5)

**İstek:** (1) Sınıf seçiminde Kral'ın İngilizce yetenek metni 4 satıra taşıp bedel satırının üstüne biniyordu: metinler içeriğe göre
yerleşsin, kısa yazılsın, iki dilde ölçen test. (2) Sınıf güçleri şeytanların göstergesi gibi bir şarj sistemine: koşuya ait, kazanç +1 /
kayıp +2 / çekilme +1 / beraberlik 0, en fazla 5, 5'te isteğe bağlı kullanım (hiçbir şey kendiliğinden harcanmaz), kullanınca 0; Köylü
bedava çekilme, Büyücü duyurulan küçük hileyi engelleme, Kral draw'dan önce kart koruma; 5 pipli rozet, parlama, "HAZIR: K" ipucu,
reddin nedeni iki dilde, K herkese "gücü kullan"; kayıt `class.charge`, eski anahtarlar yok sayılır; TableCharges sınıflardan kalksın;
metinler, ilk dolduğunda ipucu; simülasyon (±3, gerekirse eşik / şarj ayarı); testler. (3) Koşu günlüğü (runs/run-<tarih>.txt, 50 dosya,
hata oyunu durdurmasın), pencereyi ortala, ilk açılışta odak + runInBackground, 5 duman testi. (4) Testler, TR / EN görüntüler, 0.1.5,
OKUBENI / GERI_BILDIRIM, DEVLOG. Ara mesaj: iş bitince eski build'leri sil.

### 1. Sınıf kartı çakışması
- `SinnerSelectView`: yetenek metni `preferredHeight` kadar, bedel onun hemen altından (2 px), ikisi de SEÇ'in üstünde (`AbilityTop`,
  `ChooseTop`). Metinler kısaldı: Köylü "Power: walk away from a hand for free." / "Güç: bir eli bedelsiz bırak."; Büyücü "Sees lies, and 2 of
  Belial's cards. Power: ward off a minor cheat."; Kral "Wins pay a quarter ante more. Power: shield a card from cheats." (bedeller tek satır).
- PlayMode `TheSinnerCards_TheirWordsFitAboveChoose_InBothLanguages`: sahnede gerçek kartlar, her sınıf × EN / TR: metin kutusuna sığar,
  bedel yeteneğin altından başlar, SEÇ'in üstünde biter. `03c_sinners` TR / EN kontrol edildi.

### 2. Şarj göstergesi
- Core: `Sinner` baştan (`Charge`, `IsCharged`, `HandSettled`, `TryUse`, `WardRaised`, `PowersUsed`, `WardsUsed`; ICheatGuard: kurulu koruma
  bir sonraki *gerçekten vuracak* küçük hileyi reddeder) + `ChargeRules`. `SinnerClass` sadeleşti (`ChargesPerRun / PerTable`, `FullCharges`,
  `Allows` kalktı); Peasant / Warlock / King yapıcıları sadeleşti. `HellPokerGame`: `WhyNoPower()` → `PowerRefusal`, `UsePower()` (Köylü: bedelsiz
  çekilme, Büyücü: koruma), `Protect` göstergeyi ister; her el kapanışında `Sinner.HandSettled` (yarım bırakılan el: çekilme / mühürlüyse kayıp).
- Kayıt: `class.charge`, `class.ward` (isteğe bağlı); eski `class.charges(.tables)` okunmaz. Bozuk (negatif) değer kaydı geçersiz kılar, büyük
  değer 5'e kırpılır. Sınıflar için `TableCharges` kalktı (Kemik Zar kullanmaya devam ediyor).
- Sunum: `SinnerBadgeView` 44 px: ikon + 5 pip (palet renkleri, 4×4); dolunca altın / kor arasında yanıp söner; kullanılabilirken üstünde
  "READY: K" / "HAZIR: K", kurulu korumada "WARD UP" / "KORUMA HAZIR"; hover kutusu metne göre boy alır. `ITableCommands.ToggleProtect` →
  `UsePower` (K, rozet). Red nedenleri `UiText.PowerRefused` (iki dilde). İlk dolduğunda `tip.power` ipucu. SINNERS sayfası yeni kuralı anlatıyor.
- **Denge ve karar:** planlanan şarj (5, kazanç +1, kayıp +2, çekilme +1) Mammon'da Köylü **+3.7**, Büyücü **+3.5** verdi (uzun Mammon koşularında
  güç sık doluyor: koşu başına Köylü 4.2, Büyücü 5.7). Denenenler (Köylü / Büyücü, Mammon): dolu 6 → +3.2 / +2.7; dolu 7 → +2.7 / +2.4 (ama 5 pip
  bozulur); 5,1,1,1 → +3.2 / +3.0; 5,1,2,0 → +3.1 / +1.3; **5,1,1,0 → +2.4 / +0.7 (seçilen)**: her kazanılan ya da kaybedilen el +1, çekilme ve
  beraberlik 0. Çekilmenin hiç şarj vermemesi "çekilip şarj toplamak bedava olmasın" amacını daha da sıkı karşılıyor; 5 pip aynı.

| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %81.8 (+2.4) | %77.1 (−0.5) | %51.6 (+0.7) |
| Büyücü | %82.2 (+0.7) | %77.6 (−0.9) | %54.6 (−0.3) |
| Kral | %77.6 (+0.5) | %72.1 (0.0) | %48.9 (+0.1) |

  (Parantez: önceki "masa başına haklar" tablosuna göre.) Koşu başına güç: Köylü 2.4, Büyücü 3.6 (vuran koruma 3.6), Kral 3.4.
  Simülasyon oyuncusu: Köylü kötü elden çekilirken gücüyle bedava çekilir, Büyücü ilk küçük hile duyurusunda korumayı kurar, Kral hile
  beklenirken draw'dan önce en değerli çifti / kartı korur. `HELLPOKER_CHARGE`; `HELLPOKER_WARDS` kalktı, `HELLPOKER_KING` = "başlangıç,taç%".

### 3. Test öncesi küçük işler
- **Koşu günlüğü:** Core `RunLog` (başlık, el satırları, hileler, notlar, sonuç); Presentation `IRunLogSink` / `FileRunLogSink`
  (`persistentDataPath/runs`, en yeni 50, her hata Player.log'a uyarı) / `MemoryRunLogSink`. `TablePresenter`: yeni koşu / devam ("resumed") /
  el / hile (duyurulan, vuran, engellenen, boşa giden, geri tepen) / masa / çağrılma / düşüş / olay / emanet / güç / sonuç (aklanma, lanet,
  bırakma) ve kapanışta yarım koşu (`CloseLog`, bootstrap OnDestroy). Yazma hatası presenter'da da yakalanıyor. Batchmode'da günlük yok.
  Gerçek build'de duman turu dosyaları yazdı (sonra silindi); örnek: "-- SUMMONED by Lucifer from lilith at 150 years (attempt 1)".
- **Pencere:** pencere moduna geçince / boyut değişince `Screen.MoveMainWindowTo` ile çalışma alanında ortalanır, büyükse sol üstte
  (`WindowScales.Centered`); editörde atlanır.
- **Odak:** `WindowFocus.BringToFront` (Windows: ShowWindow SW_SHOW + BringWindowToTop + SetForegroundWindow; SW_RESTORE kenarlıksız
  tam ekranı bozabileceği için değil); `runInBackground` açık (Player Settings + bootstrap). **5 duman turu art arda: hepsi 44–47 sn'de bitti,
  hata yok, takılma yok** (yalnız 1. koşuda menüde bir soğuk açılış takılması).

### Testler
- `SinnerTests` baştan: şarj kuralları (kazanç / kayıp +1, çekilme / beraberlik 0, en fazla 5, ayarlanabilir), masada her el şarj eder,
  yarım bırakılan el, dolmadan kullanılamaz, kendiliğinden harcama yok (Köylü normal çekilince öder, Büyücü korumasız hile yer), Köylü bedava
  çekilir / mühürlü elde reddedilir / el arasında yok, Büyücü korumayı kurar (reddedilir, şeytanın göstergesi boşalır) / hile yokken, büyük
  hilede ve Düşüş'te reddedilir / boşa giden hilede bekler / ikinci koruma yok, Kral'ın koruması (dolu göstergeyle, draw'dan sonra reddedilir),
  kayıt gidiş-dönüş + eski kayıt + bozuk değer, masada K: Köylü / Büyücü (WARD) / Kral, nedenler iki dilde, gösterge yeni masada / Lucifer'de /
  düşüşte aynı, kayıt ve devam. RelicTests'te sınıf zıplama testi kaldırıldı (anlamsız), kayıt testi Kemik Zar'a indirildi.
- `RunLogTests` (11): günlüğün satırları, sona kadar oynanan koşu, masa / olay / emanet notları + bırakılan koşu, güç notu + devam eden koşu,
  kapanışta yarım koşu, bozuk disk oyunu durdurmaz, dosya sink'i 50'yi tutar ve hatayı yutar, pencerenin ortalanması (4 durum).
- **745 EditMode (+1 explicit) + 27 PlayMode (+2 explicit) geçiyor.** Ekran görüntüleri TR / EN: `03c_sinners`, `05b_power_ready` (dolu rozet,
  HAZIR: K, hover), `04_table_*` (boş rozet), `02c_rules_sinners` — taşma yok; rozet Mammon'un 4 kötülük pipine binmiyor.

### Build 0.1.5
- "Build Finished, Result: Success"; `Builds/HellPoker-0.1.5-win64.zip` 35.5 MB; zip'ten açılıp 5 kez `-fpstour` (yukarıda). OKUBENI (sınıf gücü,
  K, runs klasörü), GERI_BILDIRIM (11. soru göstergeyi soruyor, 10. soruda runs klasörü), PLAYTEST güncellendi.
- İstenildiği gibi eski build'ler silindi: 0.1.2 / 0.1.3 / 0.1.4 zip'leri ve Smoke-* test kopyaları; kalan: `Builds/Windows` + 0.1.5 zip.

### Açık sorular
- Şarj kuralı spesifikasyondan saptı (kayıp +2 → +1, çekilme +1 → 0); spesifikasyondaki hâli istenirse eşik 7 ile ±3 içinde ama 7 pip gerekir.
- Kurulu koruma boşa giden hilede bekliyor (bir sonraki küçük hileye kadar); duyurulan hileye özel kalması istenirse kolay değişir.
- Simülasyon oyuncusunun Kral'ı sadece hile beklenirken koruyor; gerçek oyuncu gücü farklı kullanabilir (oyun testi gösterecek).
## 2026-10-05 — Açık kalan güç, görünürlük, tasarımcının şarj kuralı, pencere ortalama (0.1.5, aynı numara)

### İstek (kullanıcı)
Sınıf gücü bir sonraki hamlede kendi kendine kapanıyordu: kullanılana ya da kapatılana kadar açık kalsın, çok görünür olsun;
oyun kuralı / denge sayısı değişmesin. Köylü de aynı "açılan güç" modeline geçsin (BEDAVA ÇEKİL). Şarj kuralı benim istediğime dönsün
(kazanç +1, kayıp +2, çekilme +1, beraberlik 0, dolu 5), telafi için başka hiçbir ayar değişmesin. Günlük dosya adı çakışması,
pencere ortalamanın gerçek build'de denenmesi. Tüm testler, TR/EN ekran görüntüsü, 0.1.5 build (dağıtılmadı), duman testi.

### Hatanın nedeni
Mod bayrakları (`_protecting`, Kemik Zar seçimi) presenter'da tutuluyor ve her `Refresh` başında sıfırlanıyordu; Pas / kart açılması
`Refresh` çağırınca güç sessizce kapanıyordu.

### Yapılanlar
- **Durum Core'a taşındı:** `Sinner.PowerArmed / Arm / Disarm`; `IHellPokerGame.ArmPower / DisarmPower / PowerArmed / TakePowerLapsed`.
  `Refresh` artık hiçbir şeyi sıfırlamıyor. Mod sadece şunlarla kapanır: kart seçildi, K / rozet (Kemik Zar'da zar) tekrar, el bitti,
  masa değişti, an geçti (Kral'da draw).
- **Kral:** seçmeden draw'ı geçerse hak harcanmaz, gösterge 5 kalır, mesaj: "Korumayı kullanmadın, gücün dolu duruyor." /
  "You did not use the protection; your power is still full." Yanlış karta tık modu kapatmaz.
- **Köylü:** K gücü açar (harcamaz), ÇEKİL düğmesi "BEDAVA ÇEKİL" / "FREE FOLD" olur; çekilme bedava, gösterge 0. Açık güç eller
  boyunca bekler (oynanan el bitince de kapanmaz). Re-raise'e çekilme de bedava. K tekrar: kapanır, normal çekilme bedelli.
  Bedava çekilen el gösterge doldurmaz.
- **Büyücü (doğrulandı, değişmedi):** K = koruma kalkar, hak o an harcanır (geri verilmez). Kalkan bir sonraki **gerçekten vuracak**
  küçük hileyi bekler: el bitse de, hile hiç gelmese de sonraki ellere taşınır; büyük hile / Düşüş onu harcamaz. Testle sabit.
- **Görünürlük:** rozet 1 sn nabızla yanıp söner, üstünde "GÜÇ AÇIK" / "POWER ON"; Kral'da seçilebilen kartlar yanıp sönen altın
  çerçeve, diğerleri soluk (renk tonu, alfa yok); ortada kalıcı satır "Korumak için bir kart seç (K: vazgeç)" (metin fontu — başlık fontu
  sütundan taşıyordu, ekran görüntüsünde görüldü); Büyücü'de niyet şeridinin üstünde küçük kalkan; Kemik Zar kutusu seçimdeyken
  aynı nabız + satır. `ITableView.SetPower(PowerDisplay)`, `IHandView.SetPicking`, `BetControls.FoldLabel`, `SinnerBadge.Armed`,
  `RelicBadge.Selecting`.
- **Şarj kuralı** 5,1,2,1,0'a döndü (`ChargeRules.Default`); rozet hover'ı, SINNERS sayfası, OKUBENI, PLAYTEST, CLAUDE.md güncel.
- **Günlük adı:** aynı saniyede ikinci koşu `run-<zaman>-2.txt`, `-3`...; aynı koşu kendi dosyasında kalır (test).
- **Pencere ortalama (4b):** gerçek build'de, %125 ölçekli 1920×1080 ekranda (çalışma alanı 1920×1020 fiziksel) denendi.
  - İlk ölçüm yanlış görünüyordu (alt boşluk −138): ölçen script DPI-unaware'di, çalışma alanını mantıksal (1536×816), çerçeveyi
    fiziksel aldı. Script DPI-aware yapılınca asıl hata göründü: oyun **istemci alanını** ortalıyordu, `MoveMainWindowTo` ise
    **çerçeveyi** taşıyor → pencere başlık çubuğu yüksekliğinin yarısı kadar (~20 px) aşağıdaydı. Aynı karede taşıma çalışıyordu.
  - Düzeltme: `WindowCentering` (MonoBehaviour) `SetResolution`'dan iki kare sonra çerçevenin gerçek boyutunu (DWM, başlık + kenar)
    ölçer ve **çerçeveyi** ortalar, çalışma alanından taşırmaz.
  - Sonuç: ×2 çerçeve (479,220)-(1441,799): sol / sağ 479, üst 220 / alt 221. ×3 çerçeve (239,85)-(1681,934): sol / sağ 239, üst 85 /
    alt 86. Görev çubuğu açıkta.

### Denge (tasarımcının şarj kuralı, telafi yok; 2000 koşu × 9; parantezde Lucifer'e ulaşan / ilk denemede yenme)
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %82.7 (%88 / %31) | %78.7 (%84 / %41) | %51.7 (%65 / %32) |
| Büyücü | %85.0 (%89 / %37) | %78.2 (%83 / %44) | %55.2 (%66 / %37) |
| Kral | %77.3 (%82 / %33) | %72.2 (%76 / %43) | %49.0 (%58 / %35) |

Masa başına haklı eski tabloya göre: Köylü +3.3 / +1.1 / +0.8, Büyücü +3.5 / −0.3 / +0.3, Kral +0.2 / +0.1 / +0.2. Koşu başına güç:
Köylü 3.8, Büyücü 5.7, Kral 5.5. Mammon'da Köylü / Büyücü hedefin (~80) ~3 üstünde; kullanıcı kararıyla dokunulmadı.

### Testler / build
- EditMode 757 geçti (+1 explicit), 0 hata; PlayMode 27 geçti (+2 explicit), 0 hata. Yeni: `PowerModeTests` (11), günlük adı testi.
- Ekran görüntüsü turuna `05c_power_on` eklendi (Kral, güç açık; TR / EN kontrol edildi).
- 0.1.5 dağıtılmadığı için aynı numara: `Builds/HellPoker-0.1.5-win64.zip` (~35.5 MB). Zip'ten 3 duman turu (`-fpstour`): çıkış 0,
  Player.log'da hata yok, 13 FPS satırı. Duman kopyası ve duman günlükleri silindi.

### Açık sorular / sıradaki
- Oyun testi: güç modu anlaşılıyor mu (GÜÇ AÇIK, BEDAVA ÇEKİL, kalkan)? Mammon'da Köylü / Büyücü kolay mı?
- Köylü BEDAVA ÇEKİL ve Büyücü kalkanının ekran görüntüsü turda yok (Kral'ınki var).
## 2026-10-06 — Oyun ikonu (piksel şeytan)

### İstek (kullanıcı)
Exe'de ve Başlat menüsünde Unity logosu yerine kendi piksel şeytan ikonumuz. `Tools/make_icon.py` verilen kodla birebir; import ayarları
Editor script'iyle; menüde "Hell Poker/İkonu Ayarla".

### Yapılanlar
- `Tools/make_icon.py` (kullanıcının kodu, değiştirilmedi) → `Assets/Art/Icon/hellpoker-icon-{1024..16}.png` + `hellpoker.ico`
  (`py Tools/make_icon.py`; bu makinede `python` Store yönlendiricisi, `py` çalışıyor).
- `Assets/Editor/HellPokerIconImporter.cs` (AssetPostprocessor, sadece `Assets/Art/Icon/`): Default, Point, sıkıştırmasız, mipmap yok,
  Alpha Is Transparency, NPOT ölçeklenmez, Max Size 2048.
- `Assets/Editor/HellPokerIconSetter.cs` (menü Hell Poker ▸ İkonu Ayarla): Default Icon = 1024'lük PNG; Standalone override'ın her
  boyutuna aynı boyuttaki (yoksa en yakın büyük) PNG; sürüm `ProjectVersion.txt`'den okunup loglanır, API derleme anında seçilir
  (`UNITY_2021_2_OR_NEWER` → NamedBuildTarget, değilse SetIconsForTargetGroup); her atama Console'a yazılır, `SaveAssets`.
- **Not:** batchmode'da `GetIconSizes(Standalone)` boş liste döndürüyor ve Standalone override'ı yazılmıyor (yedek boyut listesi
  `1024..16` ile denendi, yine boş kaldı). Varsayılan ikon yazıldı ve build'deki exe şeytan ikonunu taşıyor (çıkarılıp bakıldı).
  Elle çizilmiş 16 / 48'lik sürümlerin override'a girmesi için menü komutu editörde bir kez çalıştırılmalı.
## 2026-10-06 — Dördüncü sınıf: Soytarı (jokerli deste) — 0.1.5, aynı numara

### İstek (kullanıcı)
SOYTARI (Jester, id "jester"): şarj gücü yok; koşusunda her masada (Lucifer dahil) jokerli deste. 2 jokerle başlar, kazanılan el +1,
10'un üstünde kaybedilen el -1, 2'nin altına inmez. Showdown'da 2+ joker kaybettirir (iki tarafta da: beraberlik), tek joker istenen
karta dönüşür (oyuncuya seçici, şeytan en iyisini seçer; sahibinin elindeki karta dönüşemez). Hileler jokere dokunmaz. Diğer üç sınıfın
kuralları ve dengesi değişmez; Soytarı Köylü'den ±3'ten fazla saparsa sadece rapor + tek öneri. Sunum, sanat, iki dil, kayıt
(`class.jokers`), günlük, testler, simülasyon, ekran görüntüleri, build, belgeler. (Önce "joker prompunu unut" denildi, sonra bu
güncellenmiş sürüm verildi: şarj yok, sayaç var.)

### Tasarım kararları
- **Kart:** `Card.Joker(seri)`: Rank 0, suit yok, seri numarasıyla ayrışır (destede çok joker olabilir). 52 kartın eşitliği, hash'i,
  metni birebir aynı (test). Deste `IJokerDeck.SetJokers` (`Deck`); jokerler listenin dibine girer (yığılmış test destelerinin sırası
  korunur). `PlaceBet` her elde `Sinner.Jokers`'ı uygular; Soytarı değilse 0 → 52.
- **Değerlendirme:** `JokerResolver` aramasız, kategori kategori en iyi kartı kurar (Ölü Adamın Eli → floş kent → kare → full → renk →
  kent → üçlü → çift); testte 300 tek jokerli ve 40 iki jokerli rastgele elde her kartı tek tek denemekle aynı sonucu verdiği doğrulandı.
  `WildJokerEvaluator` oyunun her yargısının önünde (kurucu sarar): kasa yapay zekası, bahis, el rehberi, hileler, Bakış hep jokeri en
  iyi kart sayar. Jokerle Ölü Adamın Eli de olur (kural "istediği kart" dediği için; simülasyonda Soytarı'nın DMH kazancı belirgin arttı).
- **Showdown:** `GamePhase.NamingJoker` (sona eklendi): önce The Fall vurur, sonra bütün kartlar döner, oyuncunun tek jokeri için seçici
  açılır; kasa kendi jokerini en iyiye çevirir. `ShowdownResult.Judge` (`PlayerBust` / `HouseBust`). Seçim sırasında kapatma = el ortası
  (forfeit). Çekilen elde kural yok.
- **Hileler:** `CheatRules.IsImmune` jokeri de kapsar (hedef olmaz); `CheatTable` desteden "sıradaki kartı" verirken jokerleri atlar,
  Sahte Yüz joker göstermez, Çatal Dil'in renk sayımı jokeri saymaz. Her şeytanın her hilesi için test.
- **Sayaç:** `Sinner.Jokers` (koşuya ait: masa değişimi / çağrılma / düşüş aynen). `SinnerClass.StartingJokers` / `JokerLossLine`;
  `Jester(başlangıç, joker, sınır)`. Kayıt `class.jokers` (yoksa / bozuksa / 2'nin altıysa 2; bozuk değer kaydı geçersiz kılmaz).
- **Sunum:** joker yüzü `Ui/card_joker`, dönüşte `Ui/joker_sparkle` (6 kare) + köşede külah (`CardMark.Joker`, `card_marks` 6. kare);
  seçici `JokerPickerView` bet satırında (DEĞER / RENK okları, kart, SEÇ; klavyede oklar + Enter / Space; `ITableCommands.StepJoker`),
  canlı satır "Bu kartla elin: ÜÇLÜ (en iyisi)"; elde 2 joker varken kırmızı uyarı satırı (`PowerDisplay.Warning`); rehber
  "JOKER İLE: ..."; şeytanın iki jokeri `TableMoment.JokerLaugh` ("HA! HA! HA!") + kızgın `JokerBust` repliği; rozet pip yerine "×N",
  sayı değişince parlar, hover'da kural. K / rozet: "Soytarı'nın gücü destedeki jokerler." Sınıf seçimi 4 karta göre daralıyor
  (`CardWidthFor`: 112 px). Her şeytana `GreetingAsJester` ve `JokerBust`. SINNERS sayfası joker kurallarını anlatıyor.
- **Günlük:** joker gelişi (kime, dağıtım / draw), adlandırılan kart (ve en iyisi), kasanın kartı, iki joker kaybı / kazancı, sayaç değişimi.

### Denge (2000 koşu, kurallara dokunulmadı)
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %82.7 (%88 / %31) | %78.7 (%84 / %41) | %51.7 (%65 / %32) |
| Büyücü | %85.0 (%89 / %37) | %78.2 (%83 / %44) | %55.2 (%66 / %37) |
| Kral | %77.3 (%82 / %33) | %72.2 (%76 / %43) | %49.0 (%58 / %35) |
| Soytarı | %71.2 (%81 / %45) | %69.7 (%77 / %52) | %47.9 (%60 / %47) |

Soytarı, Köylü'ye göre −11.5 / −9.0 / −3.8: sınırın dışında. Koşular kısa ve oynak (ort. el 24 / 13 / 14; Köylü 57 / 28 / 32). Koşu
sonunda joker ort. 9.0 / 6.6 / 7.3, en çok 24. Showdown'ların %14 / %10 / %13'ü oyuncunun iki jokeriyle kaybedildi, %16 / %9 / %13'ü
şeytanın iki jokeriyle kazanıldı. Denemeler (sadece simülasyonda, `HELLPOKER_JESTER`): kayıp sınırı 4 → 74.2 / 71.5 / 48.8; başlangıç
750 → 76.7 / 77.7 / 57.6. **Öneri (uygulanmadı):** Soytarı'nın başlangıç cezası 750 yıl. Diğer üç sınıfın sayıları değişmedi
(tohumlar aynı, sonuçlar önceki tabloyla birebir).

### Testler / build
- Yeni: `JokerTests` (18: deste 54/52, sayaç, değerlendirme her kategoride + rastgele doğrulama, iki joker kuralı, seçici, kasa yapay
  zekası, hileler, kayıt), `JesterPresenterTests` (6: rozet / K, uyarı, seçici ve okları, şeytanın iki jokeri, günlük), PlayMode
  `JesterJourneyTests` (Soytarı seçilir, seçici çıkar, adlandırılan kart külahlı); sınıf kartı testi 4 kartın ekrana sığdığını da ölçüyor.
- EditMode 781 geçti (+1 explicit), 0 hata; PlayMode 28 geçti (+2 explicit), 0 hata. Simülasyona 1 saatlik zaman aşımı (4 sınıfla
  varsayılan 3 dakikayı aşıyordu; rapor tam yazılıyordu ama test "başarısız" görünüyordu).
- Ekran görüntüleri TR / EN: 03c (4 sınıf), 02c (SINNERS sayfası), `JesterScreenshots` 60–66 (sınıf seçimi, elde joker, iki joker
  uyarısı, seçici, adlandırılan kart, rozet hover'ı, şeytanın iki jokeri).
- `Builds/HellPoker-0.1.5-win64.zip` (~35.5 MB; 0.1.5 dağıtılmadığı varsayıldı). Zip'ten 3 duman turu: çıkış 0, Player.log temiz.
- OKUBENI (Soytarı paragrafı, oklar), GERI_BILDIRIM (Soytarı soruları), PLAYTEST (Soytarı satırı), CLAUDE.md (Sınıflar, mimari, kayıt,
  sanat, testler, denge tablosu) güncel.

### Açık sorular / sıradaki
- Soytarı dengesi: öneri başlangıç 750 (kullanıcı seçecek). Jokerle Ölü Adamın Eli'ne izin kalsın mı?
- Unity, eski .meta dosyalarına `nameFileIdTable` ekleyerek yeniden yazdı (zararsız; commit'e girebilir).
## 2026-10-06 — Soytarı'nın Çıngırağı (20 joker) ve sayılan deste / KARIŞTIR — 0.1.5, aynı numara

### İstekler (kullanıcı)
1. Soytarı isteğinin güncellenmiş sürümü (parçalı geldi): **20 joker** — sayı 20'ye ulaşınca jokerler silinir, sayı 2'ye döner,
   koşuda bir kez "Soytarı'nın Çıngırağı" emaneti (lanet: kaybedilen el ante'nin %50'si kadar fazla yıl ekler; "oynaklığı ikiye
   katlar"), büyük an, emanet çubuğunda ayrı, kayıtta 2 sınırına takılmasın. Bir mesaj okunamaz geldi (karakterler karışık).
2. "İşlemler bittikten sonra": **kart sayma** — deste eller boyunca devam etsin, kalan kart sayısı görünsün, oyuncu bedel ödeyip
   karıştırabilsin; diğer kurallara dokunma. Son olarak iki denge tablosu ve kısa Türkçe özet. (Bu da parçalı geldi: 2. ve 4. bölümün
   bir kısmı ile "...açılır; bunu OKUBENI'de bir cümleyle söyle" cümlesinin başı kayıp.)

### Yorum ve kararlar (eksik parçalar için)
- Çıngırak'ın lütfu metinde kesikti; lanetin simetriği alındı: **kazanılan el ante'nin %50'si kadar fazla siler**. `RelicEffects`'e
  `WinAntePercent` / `LossAntePercent` (yeni kancalar; diğer emanetlerin sayıları aynı). `IRelic.IsReward`: olaylarda çıkmaz
  (`RelicRoster.Offered`), 2 sınırına sayılmaz (`RunEffects.CarriedOffered`), kayıtta "relics" içinde okunur.
- 20 joker anı: `TableMoment.JokerLaugh` + "YİRMİ JOKER!", şeytan şaşkın `JokerJackpot` repliği (10 metin, iki dil), sonuç satırında
  emanet (ya da "yine yirmi joker: deste temizlendi"), emanet kutusu çubukta ilk kutunun solunda, altın çerçeveli, bir an parlıyor.
- Sayılan deste: `GameRules.ContinuousDeck` (açık). Otomatik karıştırma eşiği koddan: `CardsForAHand` = 2 el + iki tarafın en çok
  draw'u + emanetlerin yeniden çekişi + bir hilenin en çok çektiği (`CheatTable.MostCardsACheatDeals` = 2, The Fall) + hayalet elde 5.
  Mammon'da 18, Lilith'te 20, Kemik Zar'la +1. Kasa draw'da desteden fazlasını istemez (boş destede hata yerine az çeker).
- KARIŞTIR: S / buton ("KARIŞTIR +10"), eller arasında, her elden önce bir kez, +10 yıl, 300 altı / ruh masadayken (ya da 10 yıl ruhu
  masaya koyacaksa) yok; ruhta buton gizli (yıl gösterilmez). Sayaç "DESTE 37" kasanın satırının solunda, hover: "Destede kalan: N
  kart. Deste bitince ya da KARIŞTIR ile baştan karılır." Otomatik karıştırma ilk kararda "Deste bitti, şeytan desteyi karıyor."
  Soytarı'da deste her el karılıyor; sayaç ve KARIŞTIR yok.
- Yeni masa / Lucifer / düşüş / devam: yeni oyun = taze deste. `Restart` da desteyi tazeliyor.

### Denge (2000 koşu)
Taze deste (eski, `HELLPOKER_FRESH_DECK=1`):
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %82.8 (%88 / %31) | %78.4 (%84 / %40) | %52.4 (%66 / %32) |
| Büyücü | %84.9 (%89 / %36) | %77.9 (%83 / %45) | %53.9 (%65 / %39) |
| Kral | %77.5 (%82 / %34) | %71.0 (%75 / %44) | %49.3 (%59 / %35) |
| Soytarı | %69.2 (%80 / %45) | %70.0 (%78 / %50) | %47.6 (%59 / %45) |

Sayılan deste (yeni):
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %83.4 (%89 / %31) | %78.0 (%83 / %42) | %50.3 (%66 / %32) |
| Büyücü | %84.9 (%89 / %36) | %77.1 (%82 / %45) | %55.3 (%67 / %39) |
| Kral | %77.4 (%82 / %32) | %72.2 (%77 / %42) | %47.1 (%58 / %34) |
| Soytarı | %69.2 (%80 / %45) | %70.0 (%78 / %50) | %47.6 (%59 / %45) |

Fark −2.2..+1.4 (gürültü payı); simülasyon oyuncusu kart saymıyor ve KARIŞTIR'a basmıyor, gerçek sayan oyuncuya fayda daha çok olabilir.
Soytarı iki tabloda aynı (her el karılıyor). Çıngırak: Soytarı koşularının %0.7 (Mammon) / %0 / %0.2'sinde; 20'ye çıkmak zor (koşu
sonunda joker ort. 8.9 / 6.7 / 7.4, en çok 19). Soytarı hâlâ Köylü'nün 4–14 puan altında; önceki öneri (başlangıç 750) bekliyor.

### Testler / build
- EditMode 797 geçti (+1 explicit), 0 hata. Yeni: Çıngırak (`JokerTests` +4, `JesterPresenterTests` +1, `RelicTests` güncel),
  `DeckCountingTests` (11: deste devam ediyor, otomatik karıştırma ve eşik, yeni masa taze, kural kapalıyken her el taze, KARIŞTIR bedeli
  / bir kez / 300 / ruh, Soytarı'da yok, boş destede hileler ve Kemik Zar hata vermiyor, masa sayacı + buton + günlük).
- PlayMode 28 geçti (+3 explicit), 0 hata. Ekran görüntüleri TR / EN yeniden alındı (masada "DESTE 52", "KARIŞTIR +10").
- `Builds/HellPoker-0.1.5-win64.zip` (~35.5 MB); zip'ten 3 duman turu: çıkış 0, Player.log temiz.
- OKUBENI (deste sayma + KARIŞTIR + S + Çıngırak), GERI_BILDIRIM, PLAYTEST, CLAUDE.md güncel.

### Açık sorular
- Çıngırak'ın lütfu metinde kesikti (yukarıdaki yorum doğru mu?). Okunamayan mesaj ve kesik "...açılır" cümlesi neydi?
- Soytarı başlangıcı 750 olsun mu? Sayaç başlık fontuyla biraz iri; istersen metin fontuna alınır.
## 2026-10-06 — 0.1.6 paketi (devam ediyor): 1. deste koşuya ait, 2. KARIŞTIR nedenleri, 4. Kral bütün eli korur

### İstek (kullanıcı)
0.1.6: altı iş, sırayla, her işten sonra o işin testleri, sonda hepsi. Şarj kuralına, olaylara, emanetlere ve adı geçmeyen kurallara
dokunma. Mesajlar parçalı geldi: 1., 2. ve 4. iş büyük ölçüde okunuyor; 3. işin yalnız test satırı ("10 → sayı, 9 → KARIŞIYOR,
karıştırınca → 52"), 5. ve 6. iş hiç gelmedi. Bir mesaj okunamaz geldi.

### 1. Deste koşuya ait (bedava karıştırma açığı kapandı)
- Masa değişimi (Lucifer'e çağrılma ve düşüş dahil): `SeatAt` eski oyunun `DeckCards`'ını yeni oyuna `RestoreDeck` ile taşır.
- Kayıt: v=4 isteğe bağlı `deck` (`AS,10H,3C`…, sıradaki önce; `CardCodes`), eller arasında ve el ortasında; yarım elin kartları
  desteye dönmez. Yoksa / bozuksa / tekrar eden ya da geçersiz kart varsa taze deste, kayıt geçersiz sayılmaz. Jokerli (Soytarı)
  deste ve "her el taze" kuralı kaydedilmez. `IDeck.Restore`.
- Masa-başı yığılmış deste kullanan iki test (CheatPresenterTests'teki düşüş, PlayMode LuciferJourneyTests) `continuousDeck: false`
  ile kuruldu (oyun kuralı değil, test kurulumu).

### 2. KARIŞTIR'ın nedenleri
`ShuffleRefusal`: `SoulOnTable` (eski `SoulAtStake`), `AlreadyShuffled`, `DeckFull` ("Deste zaten tam."), `ShuffleComing` ("Şeytan bir
sonraki elden önce desteyi zaten karacak."), `TooFewYears` — buton gizli; `WouldStakeSoul` — buton görünür ama soluk, mesaj
"Karıştırmanın 10 yılı seni 1750 yıla taşır: ruhun masaya gelir." (CLAUDE.md kuralı gereği değişken sayıya ek bağlanmadı: "1750'ye"
yerine "1750 yıla"). Testler: 52 → buton yok + "zaten tam"; deste `CardsForAHand` altında → "şeytan karacak"; Belial 1743 →
WouldStakeSoul ve mesaj, 1739 → izin; ruh masadayken SoulOnTable.

### 4. Kral: güç bütün eli korur
- Sadece şeytan bir hile duyurmuşken ve hile henüz vurmamışken (`PendingCheatTruth != null`); yoksa "Korunacak bir hile yok: şeytan
  henüz hile duyurmadı." K anında çalışır, şarj o an 0, draw'dan önce / sonra fark etmez; açık kalan güç modu ve seçim çerçevesi Kral
  için kalktı (Kemik Zar seçimi aynı). El bitince kalkar; korunan kartlar normal atılır (Kemik Zar da çekebilir), yeni gelen kart da
  korunur; taç işareti; engellenince Büyücü'nünki gibi parıltı ("TAÇ") ve şeytanın kızgın repliği. Büyük hileleri de durdurur (eski
  koruma da durduruyordu).
- **Oyuncunun kartına dokunan hileler (koddan):** Rehin (zincir), Satın Al (kart takası), Çatal Dil (renk değişimi), Yılan Takası
  (takas + gizleme), Gece Örtüsü (gizleme), Diken (işaret), Aysız Gece (çekilen kartları gizler), Yeniden Yazma (kart değişimi), Yanan
  Kart (yeniden dağıtım), Düşüş (oyuncunun en yüksek kartı yeniden dağıtılır). **Dokunmayanlar (koruma durdurmaz):** Sahte Yüz (şeytanın
  kartının yüzü), Haraç (kazançtan alır), Bakış (kasanın bahis bilgisi). `CheatRules.TouchesPlayerCards`.
- Sınıf kartı: "Güç: duyurulan hileye karşı bütün elini koru." Rozet hover'ı ve SINNERS sayfası bu metni kullanır; OKUBENI, PLAYTEST.

### Testler
EditMode 800 geçti (+1 explicit), 0 hata; PlayMode 28 geçti (+3 explicit), 0 hata.

### Bekleyen
3. iş (sayaçta "KARIŞIYOR"?), 5. ve 6. iş: metin gelmedi. Sonra: denge tablosu, TR / EN ekran görüntüleri, 0.1.6 build, zip'ten duman testi.
## 2026-10-06 — 0.1.6 paketi tamamlandı (altı iş + son)

### İstek (kullanıcı)
Sürüm 0.1.6, altı iş sırayla, her işten sonra testleri, sonda hepsi; şarj, olay, emanet ve adı geçmeyen kurallara dokunma. Mesajlar
parça parça geldi; 1, 2, 4 (bkz. önceki kayıt) ile 3, 5, 6, 7 burada.

### 3. Deste sayacı: 10'un altında sayı yok
Destede 10'dan az kart varken sayaç sayı yerine "KARIŞIYOR" / "SHUFFLING" yazar (aynı yer, aynı font; hover: "Destede birkaç kart
kaldı: şeytan bir sonraki elden önce 52 kartı karar."). Deste dolunca sayı geri gelir. `ITableView.SetDeck(label, hint)`,
`TablePresenter.DeckCountShownFrom` (10). Otomatik karıştırma kuralı aynı. Test: 10 → sayı, 9 → KARIŞIYOR, karılınca → 42 (yeni elin
dağıtımından sonra; 52 karılır, 10 dağıtılır).

### 4. Kral (önceki kayıttan) — ek
Korunan kartın atılamaması bildirilen hata (`CanRedraw`'da `IsPlayerCardProtected`) kaldırıldı; draw zaten kısıtlamıyordu.

### 5. Soytarı 750 yıl
`Jester` varsayılan başlangıcı 750; sınıf kartı ve SINNERS sayfası sayıyı `StartingYears`'tan okur. OKUBENI, CLAUDE.md, PLAYTEST.

### 6. 2K'da yazılar
- İnceleme: ekran görüntüsü testine `HELLPOKER_SHOT_SIZE` (2560x1440) ve `HELLPOKER_FILL` eklendi; düzeltme kapalıyken "önce",
  açıkken "sonra" görüntüleri TR / EN, 1920×1080 ve 2560×1440 (TAM PİKSEL ve EKRANI DOLDUR) alındı; mesaj satırı 8× büyütülüp
  karşılaştırıldı ve kenar karışımı ölçüldü (aradaki tonların parlak piksellere oranı).
- Bulgu: RenderTexture yakalamasında 2560×1440 ×5 yazıları 1920×1080 ×4 kadar keskin (her font pikseli tam 5×5; oran 0.22 = 0.22).
  EKRANI DOLDUR (×5.33) font piksellerini 5 ve 6 ekran pikseli genişliğinde eşitsiz yapıyor (oran 0.25), bilinear bulaşma yakalamada
  görülmedi. Unity'nin `Canvas.pixelPerfect`'i Text'i ilk köşesinden yuvarladığı için yarım piksel bu sahnelerde ortaya çıkmadı. Oyun
  Windows'ta DPI-aware (Player Settings'te ayar yok; %125 ekranda pencere fiziksel piksellerle ölçülmüştü). **Gerçek 2K ekranda
  görülen bulanıklık bu ortamda yeniden üretilemedi**; en olası nedenler: EKRANI DOLDUR'un kesirli ölçeği, ya da tek ölçekte (×3, ×5)
  yarım oyun pikselinde duran bir metnin yarım ekran pikseline düşmesi + font atlasının bilinear örneklenmesi.
- Düzeltme (iki riski de kapatır): `PixelSnappedText` (mesh düzenleyici: her metin köşesi tam ekran pikseline) ve `PixelText.KeepSharp`
  (font atlası Point filtre, `Font.textureRebuilt`'te yeniden); `UiFactory.CreateText` ikisini de her metne uygular. Ayar ipucu:
  "Doldur: yazılar bulanık olabilir, TAM PİKSEL önerilir." Testler: ×2..×6'da yarım oyun pikselindeki metnin bütün köşeleri tam ekran
  pikselinde; atlas yeniden kurulunca Point kalıyor.
- Önce / sonra ölçüm aynı (yakalamada bulanıklık zaten yoktu). Kullanıcı gerçek 2K ekranda tekrar bakmalı.

### Denge (2000 koşu, sayılan deste)
| Sınıf | Mammon | Belial | Lilith |
|---|---|---|---|
| Köylü | %83.4 (%89 / %31) | %78.0 (%83 / %42) | %50.3 (%66 / %32) |
| Büyücü | %84.9 (%89 / %36) | %77.1 (%82 / %45) | %55.3 (%67 / %39) |
| Kral | %78.8 (%83 / %37) | %72.9 (%77 / %47) | %52.2 (%61 / %40) |
| Soytarı | %75.4 (%87 / %41) | %77.1 (%86 / %50) | %54.7 (%72 / %42) |

Kral +1.4 / +0.7 / +5.1 (bütün el korunuyor; koşu başına güç 4.9). Soytarı 750 yılla +6.2 / +7.1 / +7.1; Köylü'ye göre −8.0 / −0.9 / +4.4.
Köylü ve Büyücü önceki tabloyla birebir aynı.

### 7. Son
- Sürüm 0.1.6. `Builds/HellPoker-0.1.6-win64.zip` (37 199 214 bayt, ~35.5 MB); zip'ten 3 duman turu: çıkış 0, Player.log temiz, 13 FPS
  satırı. 0.1.5 zip'i silindi; duman kopyası ve günlükleri silindi.
- Son test turu: EditMode 804 geçti (+1 explicit), 0 hata; PlayMode 28 geçti (+3 explicit), 0 hata.
- OKUBENI, GERI_BILDIRIM, PLAYTEST, CLAUDE.md güncel.
## 2026-10-07 — Soytarı: sonuç belliyken seçici açılmasın; iki tarafta da 2+ joker (0.1.6, aynı numara)

### İstek (kullanıcı)
1. Hata: şeytanın 2+ jokeri varken (şeytan zaten kaybetmiş) oyuncu tek jokerini yine de seçmek zorunda kalıyordu. Genel kural:
   sonuç jokerin kartına bağlı değilse seçici açılmasın. 2. Yeni kural: iki tarafta da 2+ joker → jokeri az olan kazanır, eşitse
   berabere (yıl değişmez); tek tarafta 2+ → o taraf kaybeder (aynı). Diğer kurallara dokunma.

### Yapılanlar
- `HellPokerGame.JokerCanChangeTheOutcome`: oyuncunun tek jokeri için bütün aday kartlar denenir (48 değerlendirme); hepsi aynı sonucu
  (kazanç / kayıp / beraberlik) veriyorsa seçici açılmaz, joker en iyi karta döner (külahlı görünür), mesaj satırı sonucu söyler.
  Bu genel kural normal ellerde de geçerli: en iyi kartla bile kaybedilen ya da her kartla kazanılan elde artık seçici yok.
- `ShowdownResult.Judge(player, house, oyuncuJoker, kasaJoker)` (bool yerine sayı): `PlayerJokers` / `HouseJokers`, `BothBust`,
  `WinnerBust`. İkisi de 2+: az olan kazanır, eşit beraberlik.
- **Ödeme:** tek taraf battığında bugün kazananın kendi (bozulmamış) elinin kategorisi ödeniyordu — öyle kaldı. İki taraf da battığında
  kazananın eli de bozuk: en zayıf el gibi ödenir (Yüksek Kart; kazançta `Forgiven(HighCard)`, kayıpta `GetYearsAdded(HighCard)`, yani
  `GetLeastYearsForgiven/Added` ile aynı sonuç). Beraberlik yıl değiştirmez. Joker sayacı normal: kazanç +1, kayıp (10 üstü) −1, beraberlik 0.
- Mesajlar (iki dil): "İkinizde de joker fazla: daha az jokeri olan sen kazandın." / şeytan kazandı / "Eşit joker: el berabere, yıl
  değişmez."; kahkaha anı ve şeytanın `JokerBust` repliği sadece şeytan kendi jokerleriyle kaybettiğinde; eşitlikte normal Push repliği.
  Günlük: `JOKERS ON BOTH SIDES: player 2 vs house 2: Push`, kendiliğinden dönen joker için `player joker -> A♦ at once`.
- Kasanın yapay zekası: draw'da fazla jokerleri atmaya devam ediyor; bahiste 2+ jokerle re-raise etmiyor (oyuncunun kartlarını bilmiyor,
  temkinli; Bakış altında yargı yeni kuralı biliyor).
- Seçicinin açıldığı testler artık sonucun gerçekten jokere bağlı olduğu elle kuruldu (kasada K üçlüsü: joker as olursa kazanç, başka
  kart olursa kayıp); PlayMode Soytarı yolculuğu ve ekran görüntüleri de.

### Denge (Soytarı, 2000 koşu)
%75.9 (%87 / %41) / %77.1 (%86 / %50) / %54.7 (%72 / %42) — önceki 75.4 / 77.1 / 54.7: +0.5 / 0 / 0 (iki tarafta 2+ joker nadir).

### Testler / build
- EditMode 808 geçti (+1 explicit), 0 hata; PlayMode 28 geçti (+3 explicit), 0 hata. Yeni: seçicisiz el (oyuncu 1, şeytan 2 → kazanç,
  sayaç +1), seçici açılan el, 2–3 / 3–2 / 2–2 / 3–3 / tek taraf; oyunda 2–2 beraberlik ve 3–2 kayıp (en zayıf el ödemesi);
  presenter: seçicisiz el + kahkaha + günlük, beraberlik mesajı ve günlüğü, Türkçe metinler.
- Ekran görüntüleri TR / EN: `JesterScreenshots` 60–67 (67: seçicisiz el).
- 0.1.6'nın dağıtıldığı bilgisi yok: aynı numara. `Builds/HellPoker-0.1.6-win64.zip` (37 200 043 bayt, ~35.5 MB); zip'ten 3 duman
  turu: çıkış 0, Player.log temiz.
