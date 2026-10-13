# Hell Poker — değişiklikler

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
