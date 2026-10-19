# Hell Poker — değişiklikler

## Demo 3.1 (3.1.0-demo) — 2026-10-19 — Oyuncu paneli ve el animasyonları

### Yeni
- **Oyuncu paneli:** bölüm masalarında sağda, sayacın altında: sınıfının hareketli portresi (nefes alır, göz kırpar, sınıfına özgü küçük
  bir hareket yapar: Köylü heybesini yoklar, Kâhin'in üçüncü gözü parlar, Kral tacını düzeltir, Soytarı'nın çanları sallanır, Çağırıcı'nın
  iblisi omzunda kıpırdar, Hırsız gözlerini kaçırır), seviyesi, pasif gücünün adı (çalışınca parlar) ve takılı emanetlerin. Portreye ya da
  pasif satırına tıklamak K ile aynı. Şeytanın portresinin üstündeki rozet ve emanetler kalktı: portre temiz.
- **Portre elin gidişini yaşar:** kazanınca gülümser, güçlü elde sevinçle zıplar, kaybedince yüzünü buruşturur, mühürlü elde ve ruh
  çizgisine yaklaşınca gerilir.
- **El gücüne göre animasyon:** yüksek kart / çiftte coin'ler uçar; iki çift / üçlüde kazanan kartlar vurur ve elin adı yazılır; kent /
  renkte kartlar tek tek yanar; Full'de üçlü ve çift ayrı vurur, ekran sarsılır; Karede beyaz flaş ve coin yağmuru; Floş / Floş Royal'de
  ekran kararır, kenarlar yanar, kartlar tek tek tutuşur. Kasanın üçlü ve üstüyle kaybedince kasanın kartları vurur. Her kademenin kendi
  sesi var. Şeytan masasında barın vuruşu animasyondan sonra gelir. Herhangi bir tuş animasyonu bitirir.
- **EKRAN SARSINTISI** ayarı (Ayarlar ▸ GÖRÜNTÜ): kapalıyken sarsıntı ve flaş olmaz.

### Düzeltilen
- Şeytanın barından yükselen yüzde artık adının ve kötülük göstergesinin üstüne binmiyor.

### Denge
- Değişiklik yok.

## Demo 3.0.2 (3.0.2-demo) — 2026-10-19 — Siyah kutu düzeltmesi

### Düzeltilen
- Haritada ve bölüm panellerinde (ör. "1. BÖLÜM: MAMMON'UN KASASI") metnin üstünde duran boş, siyah, ince leylak çerçeveli dikdörtgen
  kalktı: kapalı emanet çantasının açıklama kutusuydu. Açıklama kutuları artık yalnız bir şeyin üstüne gelindiğinde görünür, ekranları
  kapanınca onlar da kapanır.

### Denge
- Değişiklik yok.

## Demo 3.0.1 (3.0.1-demo) — 2026-10-19 — Görüntü düzeltmesi

### Değişen
- **How to Play resimleri büyüdü:** her sayfanın resmi oyunun 640×360 ekranından daha geniş bir kesit (256×144), pikselleri ekranın
  kendi pikselleriyle aynı boyda (büyütülmüş, bulanık ya da çift boy piksel yok). Kart değiştirme resminde KART DEĞİŞ düğmesi, sınıf
  resminde kartların başlıkları da görünür.

### Denge
- Değişiklik yok.

## Demo 3.0 (3.0.0-demo) — 2026-10-19 — Phase 3 başlangıcı

### Yeni
- **2K'da kusursuz görüntü:** oyun artık 640×360 piksellik bir ızgarada. 1920×1080 ×3, 2560×1440 ×4, 3840×2160 ×6 — kenar boşluğu ve
  eşitsiz piksel yok (1366×768'de ×2, ince kenar). Bütün ekranlar yeni ızgaraya taşındı; fazladan alan büyütmeye değil nefese gitti:
  daha geniş orta sütun, daha uzun diyalog kutusu, aralıklı harita. Zeminler (salonlar, katlar, menü, haritalar) 640×360.
- **Yeni ana menü:** düğmeler aynı boyda ve 8 piksellik ızgarada, logo tam katında, zemin sadeleşti (daha az yıldız, pencere ve parıltı;
  ateş denizinin ortası sakin).
- **Ödemeler ELLER panelinde:** bölüm masalarındaki ÖDEMELER paneli kalktı; ELLER / ÖDEMELER düğmesi (H) her elin yanında o masanın
  çarpanını ve altında masanın nasıl ödediğini gösterir (şeytan masasında barın ölçeği, Mammon'un faizi). Bahis düğmeleri genişledi.
- **Her el taze deste:** bölüm masalarında her el karılmış 52'lik desteyle başlar (Soytarı'da jokerli). Deste sayacı, KARIŞTIR ve S yok;
  deste masadan masaya, bölümden bölüme ve kayda taşınmaz. Heybedeki kartlar desteden çıkmış sayılır, ÖNSEZİ karılmış destenin üstünü görür.
- **Yeni kat ve harita müzikleri:** iki dakikalık döngüler, A / B bölümleri ve nefes araları, yumuşak bas, seyrek ve pes örs.

### Düzeltilen
- Hover kutuları (son eller, rozet, emanetler, can barı, harita, sınıf kartı...) artık her zaman en üstte ve ekranın içinde: kat
  masasında son ellerin ipucu ödemeler panelinin arkasında kalıyordu.

### Denge
- Her el taze desteyle (simülasyon, sınıf başına 500 koşu, aynı tohumlar; önceki sayılan desteye göre): Köylü %30 (−9), Kâhin %41,
  Kral %32 (+5), Soytarı %23, Çağırıcı %33, Hırsız %39. Köylü ve Kral değişimi oyun testinden sonra değerlendirilecek.

## Demo 2.3.1 (2.3.1-demo) — 2026-10-17 — Büyüme hızı, Kâhin ve Soytarı dengesi

### Değişen
- **Sınıf gücü daha yavaş büyür:** şeytanın ganimetinde artık sınıf emaneti yok. Sınıf emanetleri ilk bölümde sık gelir (pazar %50,
  bekçi %60), sonraki bölümlerde seyrek (%15). Dövme bir sınıf emanetini tam bir adım büyütür. Ortalama seviye 2. bölüm başında 1.1,
  3. bölüm başında 2.0 (önceden 1.6 / 2.8).
- **Kâhin:** ÖNSEZİ en çok 3 kart (seviyeye göre 1 / 2 / 2 / 3).
- **Soytarı:** her sınıf emaneti her masada 2 kalkan verir (0 / 2 / 4 / 6).
- Sınıf kartının üzerine gelince bütün kurallar yazar (kartta kısa metin kalır); masadaki rozetin hover'ı da tam metni gösterir:
  Kral'ın sarayı (J Q K güvende), Hırsız'ın draw'dan sonra da çaldığı, sınıfın bedeli.
- Denge (simülasyon, sınıf başına 2000 koşu): Köylü %37, Kâhin %41, Kral %29, Soytarı %22, Çağırıcı %32, Hırsız %37.

## Demo 2.3 (2.3.0-demo) — 2026-10-17 — Sınıf güçleri pasif, sınıflar emanetlerle güçlenir, iki renkli kartlar

### Yeni
- **Sınıf güçleri pasif:** K'ye basılan güç yok; her sınıfın ana gücü ve kendiliğinden çalışan bir PASİFİ var (rozet o an kısa parlar).
  Köylü SESSİZ TARLA (elin ilk heybe kartını şeytan görmez), Kâhin GERÇEĞİ GÖRÜR (yalanlar ve tuzaklar), Kral KRALİYET İNDİRİMİ (müsadere
  yarı fiyat), Çağırıcı SADIK İBLİS (elin ilk iblisi bedava), Hırsız GÖZ UCU. Soytarı aynı. K tuşu Hırsız'da ÇAL, diğerlerinde ne
  yaptığını söyler.
- **Hırsız kart değiştirdikten sonra da çalabilir:** dağıtımdan showdown'a kadar, şeytanın artırmasına cevap verirken bile (elde bir kez).
- **Sınıflar zayıf başlar, emanetlerle büyür:** her sınıfın üç sınıf emaneti var (yaygın, nadir, efsanevi; 13 yeni emanet), sırayla
  gelir: pazarda bazen tezgâhta, bazen bekçi ödülü, ganimette her zaman. Takılı her biri ana gücü bir adım büyütür, kampta dövülen bir
  adım daha: heybe ve ÖNSEZİ 1 → 4 kart, hazine 5 → 20 vergi, Hırsız %45 → %15 yakalanma, Çağırıcı önce yalnız GÖZ (EL ve FISILTI
  açılır, sonra yarı fiyat), Soytarı her masada kendi iki jokerinin kaybını berabere sayan kalkanlar. Rozetin noktaları gücün
  seviyesi; haritada "ÖNSEZİ 2/4" yazar, güç büyüyünce söylenir.
- Çağırıcı'nın dört yükseltmesi: İki Sadık İblis, Evcil İblisler, Keskin Göz, Kan Yemini.

### Değişen
- KADERİ BÜK (Kâhin'in okları), HASAT, LEJYON ve yarı fiyat K müsaderesi kalktı. Kader Dokumacısı artık şeytanın bildiği kartı
  gösterir; Ucuz Müsadere indirimi dörtte bire indirir; Geniş Heybe, Üçüncü Göz, Taşan Hazine, Hafif Parmaklar gücü bir adım büyütür.
  Kâhinin Gözlüğü'nün laneti: şeytan düşük kartınla daha sık zorbalık yapar. Lucifer'in Aynası Kâhin'e karşı desteyi dibe yollamaz.
- **Kartlar klasik iki renk** (kupa / karo kırmızı, maça / sinek siyah); dört renk Ayarlar > OYUN'da. Eski oyuncularda bir kez iki renge döner.
- Kâhin'in ÖNSEZİ ve Köylü'nün heybe kartları büyüdü (24×36): değer ve renk masadaki gibi okunur.
- Rozetin ve sınıf kartının metni aynı: ana güç, PASİF, yükseltmeler; HAZIR / BU ELDE KULLANILDI yazısı kalktı.
- How to Play: "Günahkârlar ve Güçler" sayfası ve resimleri yenilendi.
- Denge (simülasyon, sınıf başına 2000 koşu): Köylü %40, Kâhin %47, Kral %25, Soytarı %16, Çağırıcı %31, Hırsız %36.

## Demo 2.2.1 (2.2.1-demo) — 2026-10-17

### Yeni
- **Hırsız (HIRSIZ):** altıncı sınıf. GÖZ UCU: şeytanın kapalı kartlarından biri açık gelir. ÇAL (K ya da düğme, elde bir kez): bir kartını
  şeytanın kapalı kartlarından biriyle körlemesine değiş. Dörtte bir yakalanırsın: takas olmaz, ceza ödersin. Dört yükseltme; Lucifer de çalar.

### Değişen
- Bütün sınıflar baştan oynanabilir (Çağırıcı'nın kilidi kalktı).
- Sınıf kartları altı sınıfa göre yeniden düzenlendi, metinler kısaldı.
- **Köylü'nün heybesi 3 → 2 kart** (Geniş Heybe ile 3): kurtuluşu %51'den %41'e indi (simülasyon, 2000 koşu).

## Demo 2.2 (2.2.0-demo) — 2026-10-16

### Yeni
- **Büyücü → KÂHİN (Oracle):** gözleri bağlı, alnında üçüncü göz. ÖNSEZİ: dağıtımdan sonra kart değiştirmede gelecek 2 kartı görür.
  GERÇEĞİ GÖRÜR: yalanları ve dolandırıcıların tuzağını görür. KADERİ BÜK (K ya da kartın üstündeki ▲ / ▼, elde bir kez): gördüğün bir
  kartı en üste çek (ilk o gelir) ya da dibe yolla — bedelsiz. Bedeli AYNA LANETİ: şeytan bir kartını bilir. Yeni yükseltmeler: Üçüncü
  Göz, Kör Ayna, Kader Dokumacısı, Kehanet Mührü. Eski kayıtlar ve rekorlar aynen açılır.
- **Resimli How to Play:** 10 kısa sayfa, her birinde oyundan bir resim; oklar ya da Q / E ile gezilir, sonuncusu el tablosu.
- Haritada iki Kara Pazar ya da iki kamp bir yol üzerinde arka arkaya gelmez (aralarında en az iki kat; bir yolda en çok iki pazar).
- Bölüm masalarının elleri, güç kullanımları ve reddedilen K basışları artık koşu günlüğünde.

### Değişen
- "Kör Kâhin" olayı Türkçede **Kör Falcı**.
- Oyun aynı anda iki kez açılamaz (iki kopya aynı kayda yazıyordu).

### Düzeltilen
- Çağırıcı: LEJYON'un bedava iblisinden sonra katta coin'i yetmeyen iblis bedava geliyordu.
- MÜSADERE / EL seçimi açıkken kart değiştirilince seçim açık kalıyor, sonraki her tıklama takılıyordu.
- Lucifer'in Aynası Çağırıcı'ya "jokerlerin benim de jokerim" diyordu; Göz ve 5. dairenin ikinci kopyası "üstteki kartı çekti" diye
  anlatılıyordu.
- Hasat yapılamadığında "hasat yapıldı" deniyordu.
- Panel ipuçları yükseltmeleri (Ucuz Müsadere, Kraliyet Fermanı, Kader Dokumacısı) ve iblis emanetinin bar bedelini göstermiyordu.

## Demo 2.1.1 (2.1.1-demo) — 2026-10-14 — oyun testi düzeltmeleri

### Yeni
- **El sonucu belirgin:** her el bitince masanın ortasında büyük bant (KAZANDIN / KAYBETTİN / BERABERE / ÇEKİLDİN ve miktar), ekran
  kenarı kısa parlar, kese / bar değişimi yukarı süzülen rakamla görünür; sayacın çubuğunun yanında son 5 elin pipleri (üzerine gel: el
  ve miktar).
- **Dört renkli deste** (Ayarlar ▸ OYUN, varsayılan açık): maça siyah, kupa kırmızı, sinek yeşil, karo mavi. Küçük kartlarda değerin
  yanında sembol ve renkli kenar; 5×5 maça ve sinek yeniden çizildi.
- **Şeytanın Pusulası** (nadir emanet, "bir bölümde 3 bekçi yen" ile açılır): haritada bulunduğun kattaki başka bir düğüme geç
  (bölümde iki kez, her geçiş cezaya +40 yıl).
- Boss masasında **SON DARBE**: bar bir kazançla bitecek kadar azaldığında yazı çıkar, bar parlar.

### Değişen
- **Sınıf güçleri her elde bir kez** (şarj göstergesi Phase 2'de yok; Boss Simülatörü aynı). Rozette HAZIR / BU ELDE KULLANILDI;
  kullanılamazsa nedeni yazar. Denge için: Büyücü dibe 1 kart yollar (bar %1 ya da 2 coin), Kral'ın K müsaderesi yarı vergiye (2),
  LEJYON elin ilk iblisini bedava yapar.
- **Kehanet** sadece dağıtımdan sonra, draw'dan önce açık (eller arasında destenin üstü değişiyordu).
- **Boss masasında bahis** barın başlangıcına göre sabit: son %5-10 artık uzamıyor, az kalınca tek kazanç bitirir.
- Masa düzeni: DESTE sayacı başlığın soluna taşındı, pot satırı mesajın altına indi, mesaj en çok 3 satır (fazlası birleşir ya da
  üzerine gelince görünür), boss ante'si kısaldı ("%6 BAR"), oyuncu başlığı kartlardan bir piksel uzak.

### Düzeltilen
- Hile vurduğu kartı artık kartın kendisiyle izliyor: aynı anda başka bir kural kart oynatsa da masada yanlış kart dönmüyor.
- Günah Takası'nda karşılık çıkmazsa verilen emanet artık gitmiyor. Emanet bırakma / satma / takasında mesaj ve günlük satırı.

## Demo 2.1 (2.1.0-demo) — 2026-10-13

### Yeni
- **Ceza ve şeytanın barı ayrıldı:** cezan (yıl) koşu boyunca senin; haritada, kapıda ve şeytan masasında sağ üstte yazar. Şeytan masasında
  kazanç yıllarını hem bardan hem cezadan siler; kayıp cezana tam, şeytanın barına yarısı kadar yazılır. Sonuç satırında ikisi ayrı ayrı.
  Kurtulduğunda kalan cezan puanındır ("Kurtuldun. Ceza: 420 yıl", hiç kalmadıysa TERTEMİZ); rekorlarda sınıf ve daire başına en düşüğü.
- **Daha rastgele harita:** katlarda 2-5 düğüm, ayrılıp birleşen yollar, bölümün ortasında bir kamp, 1-2 hazine ve yeni **BİLİNMEYEN (?)**
  düğüm (içinde ne olduğu girince belli olur).
- **16 yeni olay:** yardım, dolandırıcılık (sözlerinde küçük bir ipucu; Büyücü tuzağı görür, Çağırıcı 5 coin'e iblisine baktırır) ve
  cehennemin kendisi; iki-üç seçenek, bazıları zara bağlı, sonuç kendi panelinde.
- **Araf Kampı ve emanet çantası:** 5 emanet takılı, 5'i çantada; kampta bir eylem (DİNLEN, RUHUNU BİLE, TÖREN, DÖV) ve istediğin kadar
  emanet düzeni. Haritada EMANETLER düğmesi.
- **Kara Pazar'ın altı satıcısı:** kaçakçı, Mammon'un rehincisi, kemik tüccarı, gezgin simyacı, kumarbaz tezgâhı, şeytanın terzisi;
  her pazarda bir kez pazarlık ve vitrin yenileme.
- **15 yeni emanet** (toplam 39), bir kısmı sınıfına özel, beşi kilitli.

### Değişen
- Soytarı: 20 jokerde jokerler silinmez, sayı 10'a iner; Çıngırak'tan sonra her 20'de bir emanet ya da 30 coin.
- Kara Pazar artık 2 emanetle sınırlı değil; emanet fiyatları 55 / 65 / 75.
- Mammon'un Altın Dişi kapıya getirdiğin keseyi sayar (haraçtan önce); laneti haraç +%15. Düşmüş Melek'in bedeli 100 yıl.

## Demo 2.0.1 (2.0.1-demo) — 2026-10-13

### Değişen
- **Cehennem Daireleri:** her daire şeytana karşı kazancı %5 kırpar (5. dairede −%25). Daireler artık gerçekten sırayla zorlaşıyor
  (simülasyonda ortalama kurtuluş 0. dairede %25, 5. dairede %10). Kural sınıf seçimindeki daire notunda ve How to Play'de yazıyor.

### Temizlik
- Eski ayrı Phase 2 test build'i kaldırıldı (artık tek oyun, tek build).

### Onaylananlar (değişmedi)
- Çağırıcı: kese 200, ceza 5800. Kara Pazar en çok 2 emanete kadar satar. "Barlar +%15" kalın bar olarak, 5. dairede kötülük
  göstergesi bir kısa. 24 emanetin dağılımı.

## Demo 2.0 (2.0.0-demo) — 2026-10-13

Phase 2'nin ilk oyun testi demosu: tek oyun (YENİ OYUN / DEVAM ET), üç bölüm ve Lucifer.

### Yeni
- **Hata / öneri butonu:** ana menünün sol üstünde (oyun sırasında Esc ile menüye dönülür). Hata ya da öneri seçilir, kısa bir metin
  yazılır; e-posta uygulaması caveengine@gmail.com adresine hazır bir e-postayla açılır (konu "[Hell Poker HATA] ..." /
  "[Hell Poker ÖNERİ] ..."; içinde sürüm, sınıf, daire, bölüm, kat ve koşunun son satırları). Rapor her zaman panoya da kopyalanır.
- **Çağırıcı** (beşinci sınıf, ilk kurtuluşla açılır): iblisleri çağırır — GÖZ (şeytanın kapalı bir kartı), EL (bir kartını onun açık
  bir kartıyla değiş), FISILTI (sıradaki hile, doğrusu). Katta coin, şeytan masasında bar ister; onda bir iblis azar. Güç: LEJYON.
- **Cehennem Daireleri:** bir sınıfla kurtulunca o sınıf için bir sonraki daire açılır (5 daire, sınıf kartında seçilir).
- **Kilitler:** 14 emanet ve 4 sınıfla başlanır; diğerleri kilometre taşlarıyla açılır (REKORLAR'da yazar).
- **24 emanet**, nadirlikleriyle (yaygın, nadir, efsanevi lanetli, şeytanın kendi ganimeti); **3 emanet yuvası** (Kara Pazar ikiye kadar satar).
- **Sınıf yükseltmeleri:** her sınıfa 4; şeytanın ganimetinde (2 emanet + 1 yükseltme ya da 30 coin) ve Araf Ateşi'nin yeni
  seçeneği RUHUNU BİLE'de (yükseltme, ama bar %10 uzun).
- **Phase 2 sesleri:** bölüm başına harita müziği, katların müziği, Lucifer çağrısı, kurtuluş / kese boşaldı bitişleri; coin, can barı,
  ruh uyarısı, düğüm, ganimet, Kara Pazar, ateş, heybe, kehanet, vergi, müsadere ve dört şeytan kuralının efektleri; ekranlar arası geçiş.
- **Denge:** 2. ve 3. bölümün haracı başlangıç +180 / +270, Köylü'nün heybe riski %30.

### Bilinen
- Sınıflar arasında fark sürüyor (Soytarı çok zor); derin dairelerin zorluğu henüz hedefinde değil.

## 1.4.0 — 2026-10-12
- Tek oyun: YENİ OYUN / DEVAM ET Phase 2'yi açar; eski demo "Boss Simülatörü" oldu (F9 ya da HELLPOKER_DEV=1).
- Şeytanların gerçek can barı (rakamsız; ruh çizgisi, Lucifer'de düşüş çizgisi, Mammon'un faizi ayrı adım).

## 1.3.0-phase2 — 2026-10-12
- Sınıf güçleri (heybe, kehanet, hazine) ve şeytan kuralları (faiz, rol takası, baştan çıkarma, ayna).

## Demo 1.0 (1.0.0-demo) — v1.0-demo
- Kasaya karşı 5 kart poker, üç şeytan ve Lucifer; ruh, hileler, sınıflar, olaylar, emanetler.
