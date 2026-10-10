# Phase 2 — Bölümler, katlar ve coin ekonomisi (tasarım notu)

Durum: **tasarım**, henüz kod yok. Kaynak: kullanıcının taslağı ve cevapları (2026-10-10, iki tur).
Kararlar koda girince ilgili kısımlar CLAUDE.md'ye taşınır.

## Koşunun akışı

Sıra sabit: **harita → şeytan → harita → şeytan → harita → şeytan → (Lucifer)**. Serbest masa değiştirme (LEAVE TABLE /
CHANGE TABLE) kalkar. "Ceza 250'ye inince her yerden çağrılma" kuralı kalkar.

| Bölüm | Harita | Boss | Boss masası | Ante (kat) | Haraç | Kara Pazar fiyatı |
|---|---|---|---|---|---|---|
| 1 — Mammon'un Kasası | 8 kat | Mammon | 8 el | 5 | 70 | ×1 |
| 2 | 8 kat (?) | Belial | 10 el | 8 | 85 | ×1.2 |
| 3 | 8 kat (?) | Lilith | 12 el | 10 | 100 | ×1.4 |

Koşu 1000 yılla başlar (sınıfın başlangıç cezası). **Lilith'in 12 eli bittiğinde:**
- ceza ≤ 250 → **Lucifer** (final; doğrudan masa, koridor yok; bugünkü kurallar);
- 251–999 → **"Araf" sonu**: koşu biter, skor = kalan yıl;
- ≥ 1000 → koşu **kaybedilir**.

Lucifer'den **düşüş** artık şeytan masasına döndürmez: koşu o anki yılla "Araf" sonuyla biter. Lucifer'de ceza 0 = zafer.

## İki katman

| | Katlar | Boss masası |
|---|---|---|
| Para | **coin** | **yıl** (ceza) |
| Ceza | hiç değişmez | bugünkü kurallar (ruh çizgisi, mühür, son 250...) |
| Kötülük göstergesi | küçük iblis: hafif, sadece küçük hileler, her düğümde sıfırdan; Bekçi: tam gösterge | şeytanın göstergesi, bölümle biter |
| Ruh çizgisi | yok | var |

Kat ile boss arasındaki köprü: **kapıdaki haraç** ve bazı olaylar (ör. Araf Tefecisi).

## Bölüm 1 haritası: Mammon'un Kasası

- **K1:** hep Masa. Oyuncu 6 başlangıç noktasından birini seçer.
- **K2–K4:** Masa ve Olay ağırlıklı. K3'ten itibaren Kara Pazar, K4'ten itibaren Bekçi çıkabilir.
- **K5:** hep Hazine (+20 coin).
- **K6–K7:** Bekçi, Kara Pazar ve Olay karışık; harita en fazla 2 Bekçi içerir.
- **K8:** hep Araf Ateşi.
- **Kapı:** haraç, sonra Mammon.
- Tema: para ve borç.
  - **Araf Tefecisi** (olay): şimdi 30 coin, bossa +100 yılla oturursun.
  - **Mammon'un Defteri** (olay): yılını şimdi sil, bossta fazlasıyla geri yazılır.
  - **Kumarbaz Hayalet** (olay): coin'inin yarısını tek ele koy.
  - **Bekçi — Altın Gözlü Tahsildar:** kazandığın her elde coin'inin bir kısmını alır.

## Kat masası kuralları

- **Düğümler:** Masa 3 el, Bekçi 5 el.
- **Coin:** bölüm başında +20 (artan coin'e eklenir). Ante, artırma, kazanç, kayıp hepsi coin.
- **Bahis:** artırma draw'dan önce 1 ante, draw'dan sonra 2 ante. **Tavan sabit: el başına en fazla 6 ante** (B1'de 30 coin);
  coin'e bağlı değil (borç mümkün).
- **Ödeme:** bugünkü el kategorisi çarpanları, ama **katta çarpan en fazla ×3** (royal tek elde haracı kapatmasın).
- **Çekilme:** o ana kadar potta olan coin gider, ek bedel yok (Köylü'nün gücü bunu sıfırlar). **Kayıp:** pot.
- **Borç:** coin eksiye düşebilir, sınırı yok. Ante'yi karşılayamayan oyuncu yine oturur, eksik borç yazılır. Eksideyken **kırmızı**.
- **Maç bonusu** (sıfır toplamın dışından gelir): Masayı kazanmak (3 elin 2'si) **+5 coin**; Bekçiyi kazanmak (5 elin 3'ü)
  **+15 coin ve emanet**.
- **Küçük iblis yapay zekası** (şeytanlardan zayıf): re-raise ihtimali B1 %10, B2 %20, B3 %30; blöf yok; draw'dan sonra eli
  çiftten zayıfsa %40 ihtimalle çekilir. **Bekçi şeytan yapay zekasıyla** oynar.

## Kapı ve boss

- **Haraç:** eksik kalan her coin (borç dahil) **+5 yıl**; oyuncu geri dönmez, bossa o cezayla oturur.
  Örnek: 50 coin ile 20 eksik → +100 yıl; −10 coin ile 80 eksik → +400 yıl.
- Haraçtan artan coin bossta işe yaramaz, **sonraki bölüme taşınır**. Lilith'ten sonra artan coin **skora** yazılır.
- **Boss masası:** erken kalkma yok; çekilen el de el sayılır. Son el bitince ceza olduğu gibi sonraki bölüme taşınır.
- **Ruh masadayken son el biterse masa bitmez:** ruh geri alınana (ceza çizginin altına inene) ya da lanete kadar el sürer.
  Arayüz: "Ruhun masada: şeytan seni bırakmıyor".

## Kara Pazar (fiyatlar B1; B2 ×1.2, B3 ×1.4, yuvarlanır)

- Her ziyarette 3 rastgele lanetli emanet: **45 / 50 / 60** coin (artısı güçlü olan pahalı).
- Emanet at: **25** (slot boşaltmak için).
- Şarjı 5'e doldur: **30**.
- Soytarı'ya özel: joker ekle ya da çıkar: **20**.
- Coin'i yıla çeviren hiçbir şey satılmaz.

## Araf Ateşi (K8, birini seç; coin vermez)

- Şarjı 5'e doldur.
- Bir emanetin eksisini bu bölümün sonuna kadar sustur.
- Desteyi karıştır ve sonraki masada ilk el ante'siz.

## Sınıflar, emanetler, hileler katlarda

- **Şarj koşuya ait**, kat elleri de doldurur (kazanç +1, kayıp +2, çekilme +1, 5'te dolu). Güçler katlarda da kullanılır.
- **Pasifler coin'e çevrilir:** Kral'ın tacı kat kazancında ante'nin %25'i kadar fazla coin.
- **Emanetler her yerde çalışır.** Soytarı'nın joker sayısı koşuya ait, katlarda da geçerli.

## Simülasyon hedefi (ayar noktası, kural değil)

- Bölüm 1'de koşuların **%40–60**'ı haracı tam ödesin.
- Kapıya ortalama **55–75 coin** ile gelinsin.
- Kara Pazar'dan emanet alan oyuncu ortalamada **20–40 coin eksik** kalsın.
- Küçük iblise karşı el kazanma oranı **~%55**.
- Tutmazsa önce maç bonusu ve ante, sonra iblis yapay zekası ayarlanır.

## Açık sorular

1. **Lanet sınırı:** ruh masadayken "3000'de lanet" dendi; bugün lanet = çizgi + 1000 (Mammon 3000, Belial 2750, Lilith 2500).
   Her şeytanda kendi sınırı mı, yoksa Phase 2'de hepsinde 3000 mü?
2. **Bölüm 2 / 3 haritaları:** 8 kat ve aynı düğüm düzeni mi? Temaları ve olayları (Belial: yalan / sahne, Lilith: gece)?
3. **Dead Man's Hand katta:** ×3 tavanına mı takılır, yoksa coin dışı bir ödülü mü var (ör. bütün borç silinir)? Bossta bugünkü
   gibi bütün cezayı siler mi (Phase 2'de koşuyu tek elde bitirir)?
4. **Küçük iblislerin hileleri:** hangi küçük hileler (mevcutlardan mı, iblise özel yeni mi)? Gösterge boyu?
5. **Tahsildar** kazançta coin'in yüzde kaçını alır? **Kumarbaz Hayalet** borçtayken ne yapar?
6. **Bekçi'nin emanet ödülü** iki emanet taşınırken ne olur (değiş tokuş / reddet / coin'e çevir)?
7. **Soytarı'nın Kara Pazar jokeri:** çıkarma 2'nin altına inebilir mi? 20 jokerde Çıngırak kuralı katlarda da geçerli mi?
8. **Kemik Zar** "masa başına 1": katta düğüm başına mı, bölüm başına mı?
9. **Büyücü katta:** yalan görme ve kasa kartı görme küçük iblislerde de mi? Küçük iblis kaç kart gösteriyor, kaç kart değiştiriliyor?
10. **Deste:** sayılan deste katlarda da koşuya ait mi? Ateşin "karıştır"ı bedava mı (bugün +10 yıl)?
11. **Mevcut olay zarı** (%12, el arası) kalkıp sadece Olay düğümüne mi dönüşüyor? Mevcut olaylar ve emanet teklifleri (Mezar
    Soyguncusu, Lanetli Sandık) nereye gidiyor? "Mammon'un Defteri" mevcut Şeytanın Defteri'nin (Mammon) yerine mi?
12. **Eski mod:** bugünkü serbest oyun ayrı bir mod olarak kalıyor mu, yoksa tamamen yerini mi alıyor? Eski kayıtlar (v=4)?
13. **Kayıt `v=5`:** harita, düğüm, coin, bölüm. El ortasında kapatma katta coin'le forfeit edilir (çekilme kuralı) — onay?
14. **Sınıf başlangıç cezaları** (Kral 1250, Soytarı 750) aynen mi?
15. **Rekorlar / son ekranlar:** "Araf" sonu ve skor (kalan yıl, artan coin) rekor defterine nasıl yazılır?

## Üçüncü tur kararlar (2026-10-10)

- Varsayılanlar onaylı: küçük iblis hilesiz; Bekçi şeytanın küçük hileleriyle; Tahsildar kazanılan elde coin'in %10'u; Kumarbaz
  borçtayken çıkmaz; katta Dead Man's Hand ×3 tavanına takılır; Kemik Zar düğüm başına 1.
- **Kural:** küçük iblisler her zaman o bölümün şeytanının masa kurallarıyla oynar (B1 Mammon, B2 Belial, B3 Lilith: kart değiştirme,
  kasa kartı, ödeme tablosu ×3 tavanıyla).
- **Bekçi ödülü, slotlar doluyken:** emanet yerine +10 coin; ya da taşınan bir emanet yenisiyle değiştirilir (o zaman +10 yok).
  Simülasyonda oyuncu hep coin'i seçer.
- Maç bonusunun haraç sınırını belirlemesi bilinçli; ayarın ilk düğmesi.

## Uygulama (Core/Chapters, sunum yok)

- `ChapterRules` (sayılar, `For(1..3)`), `ChapterMap` (6 şerit × 8 kat, tohumlu; düz yol her zaman, %35 yana; yollar kesişmez;
  K2–K4 Masa 55 / Olay 30 / Pazar 15 (K3+) / Bekçi 10 (K4+), K6–K7 20 / 30 / 25 / 25; haritada en çok 2 Bekçi),
  `FloorTable` (kat maçı: oyunun kendi `HellPokerGame`'i, 1 000 000'luk "ceza" kese olarak: coin = keseden düşen / eklenen),
  `FloorPayoutTable` (×3 tavan, kayıp = pot, çekilme = masadaki bahis, aklama yok), `CoinPurse` (borç), `BlackMarket`, `FloorEvents`
  (Araf Tefecisi, Mammon'un Defteri, Kumarbaz Hayalet), `ChapterRun` (yol, düğümler, ateş, kapı).
- Demo koduna eklenen kancalar (varsayılan kapalı, davranış aynı; 818 eski test geçiyor): `IHouseFoldStrategy` (HellPokerGame'e isteğe
  bağlı; `RoundResult.HouseFolded`), `IRelic.Boon` + `RunEffects.SilenceCurse / CombinedRelics / RemoveRelic`, `Sinner.FillCharge /
  ChangeJokers`.
- **Uygulamada verilen küçük kararlar** (itiraz edilebilir):
  - İblis sadece oyuncu **draw'dan sonra artırınca** çekilebilir (o zaman oyuncu masadaki bahsi 1'e 1 kazanır, artırma geri döner).
  - İblis re-raise'i İki Çift+ ile (şeytanlardaki gibi), blöf yok.
  - Ante'siz el: o el kaybedilir ya da çekilirse ante kaybedilmez; kazanırsa ödeme normal.
  - Mammon'un Defteri: şimdi −200 yıl, kapıda +300 (mevcut Şeytanın Defteri'nin sayıları).
  - Kumarbaz Hayalet: kesenin yarısı (aşağı yuvarlanır) tek elde ante; artırma yok; ×3 tavanı.
  - Tahsildar: kazanılan elden sonra kesenin %10'u, aşağı yuvarlanır.
  - Kara Pazar fiyat sırası (zayıftan güçlüye): Dikenli Tespih, Paslı Taç, Kayıkçı Sikkesi, Kemik Zar. Masadaki 3 emanet 45 / 50 / 60.
  - Satın alma borçla yapılmaz (kese fiyatı karşılamalı).
- **Dikkat:** Ateş K8'de; "sonraki masada ilk el ante'siz" Bölüm 1'de Mammon'un ilk eli olur (yılla). Kat simülasyonunda bu seçeneğin
  coin değeri yok.

## Bölüm 1 simülasyonu (2026-10-10, sınıf başına 2000 koşu)

`ChapterSimulation` (Explicit). Oyuncu: masalarda denge simülasyonunun oyuncusu; haritada önce Bekçi (bir kez), sonra Masa, alım
yapabileceği Pazar, Olay; alabildiği en güçlü emaneti alır; şarjı sadece haraç güvendeyken alır; Tefeci ve Kumarbaz'ı sadece haraç
temposunun gerisindeyken kabul eder, Defter'i hiç; ateşte şarj < 4 ise şarj, emanet taşıyorsa susturma, değilse karıştırma.

| Sınıf | Haracı tam ödeyen | Kapıda coin (ort.; p10/p50/p90) | Kapıda yıl (ort.) | Borçlu | Emanet alan | Alanların eksiği | İblise el kazanma |
|---|---|---|---|---|---|---|---|
| Köylü | %46.2 | 70.2 (0 / 63 / 160) | 147 | %7.6 | %46.8 | 29.6 | %50.5 |
| Büyücü | %46.5 | 69.8 (0 / 63 / 160) | 149 | %8.2 | %46.7 | 29.9 | %50.4 |
| Kral | %50.2 | 78.5 (2 / 70 / 175) | 130 | %6.5 | %48.6 | 26.4 | %50.4 |
| Soytarı | %44.2 | 64.0 (−20 / 55 / 169) | 190 | %16.2 | %43.5 | 30.3 | %49.7 |
| **Hepsi** | **%46.8** | **70.6** | **154** | %9.6 | %46.4 | **29.0** | **%50.2** |

Maç bonusu karşılaştırması (hepsi): +3 → %44.7 / 68.1 coin / eksik 29.7; **+5 → %46.8 / 70.6 / 29.0**; +8 → %48.2 / 74.8 / 27.2.

- Ateş: şarj %5.6, susturma %54.0, karıştırma %40.4. Kara Pazar: koşu başına 0.7 ziyaret, ziyaretlerin %70.6'sında alışveriş
  (koşuların %46.4'ünde emanet alındı; şarj alımı neredeyse yok).
- Yol (koşu başına): Masa 3.6, Olay 1.2, Pazar 0.7, Bekçi 0.4, Hazine 1, Ateş 1.
- Olaylar (çıkan / alınan): Tefeci 3451 / 1549, Kumarbaz 2958 / 902, Defter 3344 / 0.
- Bekçi: elin %49.4'ü kazanılıyor, el başına +4.0 coin, maç başına haraç (toll) 22.4 coin.
- Masaların %49.8'i, Bekçilerin %48.2'si kazanılıyor.

**Hedefler:** haracı tam ödeyen %40–60 ✓, kapıda 55–75 coin ✓, emanet alanların eksiği 20–40 ✓, iblise el kazanma ~%55 ✗ (%50.2).

**Bulgular:**
1. İblise karşı %50 simetrik taban; **iblisin yapay zeka düğmeleri bunu değiştirmiyor.** Deneme (Köylü): çekilme %100 → %50.4
   (coin 64.3, haracı tam ödeyen %41.6); re-raise %30 / çekilme %0 → %50.5 (75.7 coin, %49.5). Neden: iblis sadece oyuncu draw'dan sonra
   artırınca çekiliyor, oyuncu bunu güçlü elle yapıyor (zaten kazanacağı el); çekilme kazancı küçültüyor. Re-raise de güçlü eli
   karşılayan oyuncuya yarıyor. Yani bu iki düğme coin'i **ters yönde** oynatıyor.
2. Maç bonusu zayıf bir düğme: +3'ten +8'e haracı tam ödeyen sadece +3.5 puan (koşu başına ~1.8 masa kazanılıyor).
3. Ateşte "şarjı doldur" neredeyse hiç seçilmiyor: K8'e gelindiğinde kat elleri (~17 el) göstergeyi zaten doldurmuş oluyor.
4. Bekçi yolda nadir (koşu başına 0.4): haritada en çok 2 Bekçi var, 6 şeritte çoğu yoldan ulaşılamıyor. "Tipik yol: 1 Bekçi" tutmuyor.
5. Bekçinin haracı ağır: maç başına ~22 coin; kazanılan maçın +15'inden fazla. Coin olarak Bekçi bir masadan kötü (ödül emanet).
6. Soytarı en çok borca düşen sınıf (%16).

## Dördüncü tur kararlar ve ikinci simülasyon (2026-10-10)

**Kararlar (kullanıcı):**
- İblise karşı el kazanma hedefi **%50** (simetrik taban). İblis yapay zekasına dokunulmaz; ekonomi bonus ve ante ile ayarlanır.
- **Araf Ateşi** (K8, boss'tan hemen önce) seçenekleri boss'a dönük, kat ekonomisine dokunmaz (bilinçli):
  - **Şeytanın ilk hilesini boz:** boss masasında vurmaya gelen ilk küçük hile iptal (`FirstCheatBreaker`, `ChapterRun.BossGuard()`).
  - **Laneti sustur:** bir emanetin eksisi boss masası dahil bölüm sonuna kadar susar.
  - **Desteyi karıştır, şeytanın masasındaki ilk el ante'siz** (`ChapterRun.FreeBossAnte`). Metin dürüst olacak.
  - "Şarjı doldur" kalktı.
- **Kara Pazar:** "Şarjı doldur" kalktı; yerine **İblisin Gözü** (20 coin): bir sonraki iblis masasında iblisin bir kartı
  dağıtımdan itibaren açık (`HellPokerGame.HouseCardsOpenAtDeal`, varsayılan 0).
- **Bekçi:** Bölüm 1'de haritada **3 Bekçi** (komşu şerit gruplarına birer tane, 4. kattan itibaren; B2/B3'te 2). Her başlangıç şeridinden
  bir Bekçiye yol garanti (gerekirse üretici yana adım ekler). Hedef koşu başına 0.8–1.0.
- **Tahsildar:** kazanılan elde coin'in **%5'i, el başına en çok 5**.
- Soytarı'nın borç oranı (%16–17) kabul; raporda takip.
- Küçük kararlar (iblisin çekilmesi, ante'siz el, Defter −200 / +300) onaylı.

**İkinci simülasyon** (sınıf başına 2000 koşu; oyuncu ilk Bekçiye kadar Bekçiye giden yolu izler):

| Ante | Bonus | Haracı tam ödeyen | Kapıda coin | Emanet alanların eksiği | Borçlu | Bekçi / koşu |
|---|---|---|---|---|---|---|
| **4** | +5 | **%50.5** | **74.6** | **27.4** | %8.4 | 1.0 |
| 5 | +3 | %54.5 | 83.2 | 24.2 | %10.2 | 1.0 |
| 5 | +5 | %55.7 | 85.5 | 23.8 | %9.7 | 1.0 |
| 5 | +8 | %57.6 | 89.8 | 22.7 | %9.3 | 1.0 |
| 6 | +5 | %58.9 | 95.5 | 21.9 | %11.7 | 1.0 |

- Ante 5'te kapıdaki coin hedefin üstünde (85.5 > 75): Bekçiler artık yolda ve Tahsildar hafifledi. Oyuncunun el başına kazancı
  pozitif (+4 coin, güçlü elle artırıyor), bu yüzden **ante büyüdükçe coin artıyor**. **Bölüm 1 ante'si 4 yapıldı**: dört hedef de
  tutuyor (kapıdaki coin 74.6, üst sınıra yakın).
- Ante 5 sınıf başına (ante 4'te sınıf kırılımı ölçülmedi): Köylü %55.1 / 83.5, Büyücü %55.1 / 84.9, Kral %61.6 / 95.5,
  Soytarı %51.2 / 78.2 (borçlu %17.3).
- Bekçi: koşu başına 1.0 ✓, elin %49.3'ü kazanılıyor, maçların %48.1'i; Tahsildar kesintisi maç başına 8.3 coin (< +15 ✓).
- Ateş (simülasyon politikası; boss oynanmadığı için seçimin değeri ölçülmüyor): susturma %67.6, hile bozma %32.4, karıştırma %0.
- Kara Pazar: koşu başına 0.6 ziyaret, ziyaretlerin %68.9'unda alışveriş; emanet alan koşu %38.9, İblisin Gözü 739 kez alındı.
- İblise el kazanma %50.1 (hedef %50 ✓).

## Sonraki adım (eski plan)

Önce sadece Bölüm 1'in kat ekonomisi (Mammon'a kadar):
1. Core'da `FloorTable` (coin'le kat eli: mevcut `HellPokerGame` akışı, `StakeScale.Fixed(ante, 6 ante)`, ×3 çarpan tavanı, çekilmede
   ek bedel yok, `ImpBettingStrategy`), harita üretici (`ChapterMap`, tohumlu), Kara Pazar ve Ateş seçimleri.
2. `ChapterSimulation` (EditMode, `[Explicit]` değil ama ayrı filtre): akıllı oyuncu + mağaza / ateş / yol seçimi politikası; rapor:
   haracı tam ödeyen %, kapıdaki coin dağılımı, Kara Pazar'dan emanet alanların eksiği, iblise karşı el kazanma oranı.
3. Hedefler tutmazsa sırayla maç bonusu, ante, iblis yapay zekası.
