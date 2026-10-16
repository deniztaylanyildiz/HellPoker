# Phase 2 — Bölümler, katlar ve coin ekonomisi (tasarım notu)

Durum: **1. Bölüm oynanabilir** (2026-10-10: Core + sunum + çizim; ana menüde PHASE 2 TESTİ). 2./3. bölüm, Lucifer finali, kayıt yok.
Katlarda **sikkesi biten kaybeder** (beşinci tur, aşağıda): borç, el sınırı ve maç bonusu kalktı. Aşağıdaki eski bölümlerde geçen
"borç", "3 / 5 el", "maç bonusu", "+20 coin" artık geçerli değil.
Kaynak: kullanıcının taslağı ve cevapları (2026-10-10, dört tur).
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

## Beşinci tur: sikkesi biten kaybeder (2026-10-10)

**Kural (kullanıcı; sadece katlar, bosslar aynı):**
- Kese **0'ın altına inmez**; borç, kırmızı borç gösterimi, borç testleri ve Kumarbaz'ın borç kuralı kalktı.
- Oyuncunun kesesi bir kat masasında 0'a inerse **koşu biter** ("Kesen boşaldı" / "Your purse is empty"; lanet sayılır).
  Ante'yi karşılayamayan kalan sikkesiyle **all-in** oynar (`HellPokerGame.StakeLimit`).
- **Başlangıç kesesi sınıfa göre:** Köylü 30, Soytarı 40, Büyücü 50, Kral 100 (`ChapterRules.StartingCoinsFor`). Bölüm başı +20 kalktı;
  önceki bölümden artan sikke taşınır.
- **İblisin kesesi:** Masa iblisi 30 / 40 / 50, Bekçi 60 / 80 / 100 (B1 / B2 / B3). İsmin altında: "Kesesi 30 · ante 4 (3 elde +1)".
- El sınırı (3 / 5 el, "kazanılan 1, gereken 2") ve **maç bonusu kalktı**; masa bir kese boşalana kadar sürer, masadan kalkmak yok.
  İblisin kesesi boşalınca düğüm geçilir; Bekçi emanet ödülü aynı.
- **Ante her 3 elde +1** (B1: 4, 4, 4, 5, 5, 5, 6...; `ChapterRules.AnteAt`, `HellPokerGame.StakesOverride`); tavan 6 × o elin ante'si.
- **Haraç = sınıfın başlangıç kesesi + 60** (B2 +75, B3 +90): Köylü 90, Soytarı 100, Büyücü 110, Kral 160. Eksik her sikke +5 yıl.

**Uygulamada verilen kararlar (itiraz edilebilir):**
- Bir el, kaybeden kesenin içindekinden fazlasını alamaz; masadaki bahis iki keseden küçüğüyle sınırlı (iblis de all-in olabilir).
- **Kara Pazar son sikkeyi almaz** (fiyat < kese): yoksa oyuncu boş keseyle bir sonraki masada kart görmeden kaybederdi.
- **Kumarbaz Hayalet:** kaybedilen el sadece ortaya konan yarıyı alır (×3 çarpanı sadece kazançta); 1 sikkelik keseye çıkmaz.
- Tahsildar'ın payı kasaya gider (iblisin kesesine değil).
- Kayıt / rekor: Phase 2 koşusu henüz hiç kaydedilmiyor ve rekora yazılmıyor; Core'da `ChapterRun.PurseEmptied` lanet olarak
  işaretli, kayıt gelince lanet diye yazılacak.

**Simülasyon (sınıf başına 2000 koşu; oyuncu Kara Pazar'da en az 30 sikke yedek bırakır):**

| Ayar | Kesesi boşalan (Köylü / Büyücü / Kral / Soytarı) | Hepsi | Tam haraç | Masa eli (ort.) | Bekçi eli |
|---|---|---|---|---|---|
| **Varsayılan** (iblis 30 / 60, +1 her 3 elde) | %59.9 / 44.9 / 20.7 / 68.5 | **%48.5** | %46.4 | 6.4 | 10.7 |
| iblis 20 / 40 | 59.8 / 42.5 / 17.9 / 63.8 | %46.0 | %33.5 | 4.9 | 7.9 |
| iblis 15 / 30 | 56.1 / 42.2 / 17.1 / 61.7 | %44.2 | %27.8 | 4.3 | 6.2 |
| iblis 10 / 20 | 52.2 / 37.5 / 14.0 / 58.0 | %40.4 | %27.3 | 3.6 | 4.9 |
| iblis 5 / 10 | 43.4 / 32.4 / 11.4 / 49.7 | %34.2 | %16.5 | 3.0 | 3.6 |
| ante +1 her 6 elde | 57.9 / 41.5 / 15.5 / 67.3 | %45.5 | %48.2 | 7.3 | 12.7 |
| ante 2 | 54.9 / 41.0 / 16.2 / 66.6 | %44.6 | %47.5 | 9.6 | 14.8 |
| tavan 3 ante | 70.0 / 57.3 / 28.4 / 75.2 | %57.7 | %35.3 | 8.6 | 15.6 |
| başlangıç kesesi ×2 (haraç da +aynı) | 40.2 / 24.7 / 6.8 / 51.7 | %30.8 | %60.9 | 7.1 | 11.3 |
| başlangıç ×3 | 28.7 / 14.7 / 2.8 / 41.1 | %21.8 | %67.9 | 7.5 | 11.9 |
| başlangıç ×4 | 21.6 / 9.0 / 1.5 / 34.5 | %16.6 | %71.9 | 7.7 | 12.2 |
| başlangıç ×3, iblis 20 / 40 | 25.6 / 12.2 / 2.4 / 33.8 | **%18.5** | **%48.0** | 5.8 | 8.9 |

**Bulgular:**
1. İstenen iki düğme (iblisin kesesi, ante artış hızı) kesesi boşalan oranı hedefe (%10-20) indirmiyor: en iyi %34 (iblis 5, ama tam
   haraç %16.5'e düşüyor). Ölümler erken (ort. 2.-3. kat): 1. kat hep Masa, 30 sikkeyle 30 sikkelik iblise oturmak neredeyse
   yazı-tura; el kazanma %50, yani eşit bir oyunda "bir kese boşalana kadar" oynamak küçük keseyle başlayana ağır.
2. Asıl düğme **oyuncunun başlangıç kesesinin iblisin kesesine ve bir elin kaybına oranı.** Bir el (tavan 6 ante + ×3 çarpan) 4'lük
   ante'de ~32 sikke götürebiliyor: Köylü'nün kesesinin tamamı.
3. **Köylü'nün ölüm oranı diğerlerinden çok yüksek** (varsayılanda %60, Kral %21; Soytarı %69). Fark başlangıç keselerinden
   (30 / 40 / 50 / 100); her ayarda sıra aynı.
4. Hedeflere en yakın: başlangıç keseleri ×3 (Köylü 90, Soytarı 120, Büyücü 150, Kral 300; haraç 150 / 180 / 210 / 360) + iblis
   20 / 40 → %18.5 ölüm, %48 tam haraç; ama masa eli 5.8 (hedef 6-10) ve sınıf farkı sürüyor (Köylü %25.6, Kral %2.4).
   Uygulanmadı: başlangıç keseleri ve sınıf farkı kullanıcının kararı.

## Altıncı tur: şeytanların can barı (2026-10-10)

**Kural (kullanıcı; katlar aynı):**
- Ceza bosslara bölünür, oranlar 20 / 30 / 34 / 16 (`BossShares`, tek tablo):

| Sınıf | Toplam | Mammon | Belial | Lilith | Lucifer |
|---|---|---|---|---|---|
| Soytarı | 5000 | 1000 | 1500 | 1700 | 800 |
| Köylü | 6500 | 1300 | 1950 | 2200 | 1050 |
| Büyücü | 6500 | 1300 | 1950 | 2200 | 1050 |
| Kral | 8000 | 1600 | 2400 | 2700 | 1300 |

- Boss masasında oyuncunun o bosstaki payı şeytanın **can barıdır**: sayaçta sadece bar (`Currency.Bar`: rakam yok, "MAMMON'UN BARI",
  sonunda ruh). Kazanılan el barı ödeme kadar kısaltır, kaybedilen uzatır (bugünkü ödeme ve yüzde mantığı, birim = barın 1/10'u).
  Bar 0 → şeytan yenilir (oyunun `Absolved`'u), bölüm geçilir. Kapıdaki haraç eksiği masa başlamadan bara eklenir.
- Sabit el sayısı (8 / 10 / 12) ve Lilith sonrası son hüküm kalktı. Lilith'in barı bitince Lucifer'e; Lucifer'in barı bitince kurtuluş.
- **Ruh çizgisi barın başlangıcına oranla:** Mammon ×2.0, Belial ×1.75, Lilith ×1.5 (`ChapterRules.SoulLinePercent`); ruh kuralları
  bugünkü gibi (değer 1000, lanet = çizgi + 1000). Lucifer bugünkü kurallarıyla; düşüş koşuyu bitirir.
- Boss masası kuralları (`BossTable.Rules`): "son 250" yok, Lucifer kapısı yok (`KeepsTheLastYear` false: kazanç barı 0'a indirebilir).
  Ödeme yüzdesi düğmesi: `ScaledPayoutTable` (kazanç % / kayıp %, `ChapterRules.BossWinPercent / BossLossPercent`).

**Simülasyon** (`BossSimulation`, Explicit; sınıf × boss başına 2000 koşu, haraç eksiği yok; hepsi):

| Ödeme (kazanç / kayıp) | Mammon el · lanet | Belial el · lanet | Lilith el · lanet |
|---|---|---|---|
| 100 / 100 | 54.0 · %17.4 | 33.9 · %24.6 | 35.2 · %46.6 |
| 200 / 200 | 12.0 · %23.0 | 6.1 · %30.7 | 7.6 · %44.9 |
| 300 / 300 | 4.7 · %27.3 | 3.1 · %35.3 | 3.0 · %45.2 |
| 300 / 200 | 5.2 · %17.4 | 3.5 · %25.7 | 3.5 · %35.8 |
| 250 / 150 | 7.5 · %11.7 | 4.4 · %20.6 | 5.3 · %30.5 |
| 150 / 110 (Mammon) | 23.4 · %9.8 | | |
| **175 / 125 (Mammon)** | **17.5 · %11.1** | | |

- **Mammon 175 / 125 uygulandı** (Köylü 20.8 el / %10.2, Büyücü 19.5 / %9.9, Kral 18.7 / %11.3, Soytarı 11.0 / %13.2).
- Belial ve Lilith **100 / 100'de kaldı**: boss başına deneme turu bellek azlığı yüzünden durduruldu. Belgedeki sayılara göre Belial'in
  laneti 250 / 150'de %20.6, Lilith'inki hiçbir ayarda %30'un altına inmiyor (ruh çizgisi ×1.5 dar); Lilith için kayıp yüzdesi 100'ün
  altına inmeli (deneme sırada: 200 / 75, 175 / 65).
- **Lucifer** (bugünkü sabit bahis: ante 50, tavan 150; düşüş = bar başlangıcın üstüne çıkınca): kapı payı +100 yıl → %93 düşüş, +%25 →
  %90, +%50 → %86, +%100 → %82. 800-1300'lük bar sabit 50'lik bahisle bitmiyor; **karar kullanıcıda** (ör. Lucifer'in bahsi de bara
  oranlı, kapı başlangıç +%25; deneme sırada). Kodda kapı şimdilik `BossTable.LuciferGateMargin` = +100.
- Bilinen: masada ante, pot, "Kazanç: en az" ve sonuç mesajları hâlâ yılla yazılıyor; sadece sayaç rakamsız (kullanıcıya soruldu).

## Yedinci tur: Phase 2 tamamlandı — Bölüm 2-3, Lucifer finali, kayıt (2026-10-10)

**Kural (kullanıcı):**
- Keseler Köylü 90 / Soytarı 120 / Büyücü 150 / Kral 300. İblis: Masa 20 / 30 / 40, Bekçi 40 / 60 / 80 (B1 / B2 / B3); 1. kattaki
  iblisin kesesi yarım. İblisin kesesi başlangıcının en çok 2 katı olur, fazlası kasaya gider (`HousePurseCapPercent` 200).
- Şeytan yenilince **Ganimet**: rastgele 3 emanet ya da 30 coin (emanet yeri doluysa değiş). Lucifer'den sonra yok.
- Boss masasında yıl yok: ante, pot, kazanç / kayıp satırı, artırma ve sonuç **barın yüzdesi** (`TablePresenter.BarMode`). Katlar coin'le,
  demo masası aynı.
- Ateş: "karıştır + ilk el ante'siz" yerine **Ateşte dinlen**: bar %10 kısa başlar (`ChapterRun.RestBarPercent`).
- **Bölüm 2 — Belial'in Sahnesi:** 10 kat (K1 masa, K6 hazine +25, K10 ateş), bekçi K4'ten, en çok 2, her başlangıç şeridinden
  ulaşılır; Kara Pazar K3'ten. Ante 5 (+1 / 3 el), fiyatlar ×1.2, haraç = başlangıç + 75. Bekçi **Sahte Peygamber**: her elde açık
  kartlarından biri yalan (Büyücü görür). Olaylar: Yalancı Tanık (15 coin, bir iblis kartını gör, %25 yalan), Gösteri (kasa kart
  açmaz, kazanç ×1.5, en çok ×3), Sahte Sikke (+40 coin, bar +150).
- **Bölüm 3 — Lilith'in Gecesi:** 12 kat (K1 masa, K7 hazine +30, K12 ateş), ante 6, fiyatlar ×1.4, haraç = başlangıç + 90. Bekçi
  **Gece Hemşiresi**: değiştirilen kartlardan biri kapalı gelir, showdown'da açılır. Olaylar: Gece Pazarlığı (+50, bar +200), Arzu (bir
  emanetin lütfu ve laneti bölüm boyunca iki kat), Uykusuzluk (sonraki masa: 3 el ante'siz ve artırmasız).
- **Lucifer:** Lilith'ten sonra ganimet yok, geçiş, doğrudan masası. Bahisleri bara oranlı. Bar başlangıcın %125'ini geçerse düşüş →
  "DÜŞTÜN" (lanet). Bar boşalırsa "KURTULUŞ" + özet.
- Bölümden bölüme kese, emanetler, şarj ve jokerler taşınır; susturma ve Arzu biter; her bölüm taze deste; her bölümü başlık açar.
- Ayrı Phase 2 kaydı (`p2=1`, `phase2.save`), menüde Phase 2 DEVAM; yeni koşu eskisini lanetli sayar (onaylı). Ayrı rekorlar
  (`p2r=1`, `phase2.records`) ve koşu günlüğü.

**Kararlarım (belgede, kullanıcıya bildirildi):**
- Kapı son sikkeyi almaz (`ChapterRun.TributePaid` = en çok kese − 1): yoksa tam haraç ödeyen oyuncu 0 coin'le 2. bölüme iniyor ve hemen
  kaybediyordu. Eksik yine bara yazılır.
- Gece Hemşiresi'nin hilesi Gece Örtüsü'nün ikonunu / adını kullanır (`NightNurseCheat`, AfterDraw). Mammon'un Tahsildar'ı eski
  göstergesiyle (her el değil). Diğer bekçiler her el hile yapar (`WardenMalice` 1).
- Uykusuzluk'un artırmasız elleri kaybedilirse ante iade edilir (ante'siz el).
- El ortasında kapatılan Phase 2 eli açılışta kaybedilmiş sayılır (katta o elin bahsi, boss masasında demo'nun `ForfeitHand`'i).

**Simülasyon** (sınıf başına 2000 koşu, aksi yazmıyorsa).

Bölüm 1 katları (yeni varsayılanlar), kesesi boşalan:

| Ayar | Köylü | Büyücü | Kral | Soytarı | Hepsi | Tam haraç | Masa eli |
|---|---|---|---|---|---|---|---|
| **Varsayılan** | %21.6 | %8.4 | %0.7 | %26.7 | %14.3 | %33.4 | 4.3 |
| 1. kat iblisi 5 | %22.4 | %8.6 | %0.9 | %26.2 | | | |
| Ante +1 / 5 el | %19.0 | %7.5 | %0.7 | %25.4 | | %33.7 | 4.6 |
| Ante +1 / 8 el | %17.6 | %6.6 | %0.5 | %24.4 | | | 4.8 |

- Hedef ortalaması tuttu (%10-25), ama **Köylü–Kral farkı 18-21 puan** (>15): sayıları kullanıcı seçecek. Tam haraç %33 (hedef 40-60),
  masa 4.3 el (hedef 5-10). Varsayılanlar değiştirilmedi.

Boss ödeme yüzdeleri (`BossSimulation`, hepsi):

| Boss | Kazanç / kayıp % | El | Lanet / düşüş | |
|---|---|---|---|---|
| Mammon | 175 / 125 | ~17.5 | %11.1 | aynı kaldı |
| Belial | 175 / 100 | 9.4 | %14.1 | |
| Belial | **140 / 115** | 16.5 | %20.5 | seçildi |
| Belial | 125 / 110 | 21.7 | %21.6 | |
| Lilith | 175 / 75 | 13.9 | %14.0 | |
| Lilith | **130 / 80** | 23.1 | %22.1 | seçildi (ruh çizgisi aynı) |
| Lucifer | 150 / 100 | 10.4 | %66 | |
| Lucifer | 120 / 55 | 18.6 | %50.5 | |
| Lucifer | 100 / 45 | 26.9 | %45.7 | |
| Lucifer | **110 / 45** | 22.6 | %43.1 | seçildi |
| Lucifer | 115 / 40 | 22.5 | %36.1 | |

Bütün koşu (`ChapterSimulation.WholeRun`, sınıf başına 1000 koşu, tasarımcı kuralı):
- 2. bölümde kesesi boşalan **%59-76**, 3. bölümde %48-75: kapıdaki haraç keseyi ~1 coin'e indiriyor, 2. bölüm 1 coin'le başlıyor.
- Lucifer'e ulaşan %7.2, kurtulan %4.4 (Köylü 8.5 / 4.6, Büyücü 7.9 / 4.8, Kral 10.9 / 7.1, Soytarı 1.6 / 1.0).
- Koşu içinde bosslar: Mammon ~20-22 el / %8-11 lanet (Soytarı 12 / %19), Belial ~18-23 / %17-21 (Soytarı %29), Lilith ~29-33 / %14-19
  (Soytarı %38), Lucifer ~26-29 el / %35-46 düşüş.
- Deneme `HELLPOKER_REFILL=1` (her bölüm en az sınıfın başlangıç kesesiyle başlar): 2. bölümde kesesi boşalan Köylü %37.5, Büyücü %17.2,
  Kral %3, Soytarı %38.8; Lucifer'e ulaşan / kurtulan hepsi %31.6 / %20 (Köylü 23.2 / 11.8, Büyücü 38.3 / 26.1, Kral 55.8 / 37.2,
  Soytarı 9.1 / 4.9). **Uygulanmadı**: bölüm başı kese kuralı kullanıcının kararı.

## Sekizinci tur: sınıf güçleri ve şeytan kuralları (2026-10-12)

**Kural (kullanıcı):**
- Taban kese: 2. ve 3. bölüm başında kese sınıfın başlangıç kesesinin altındaysa ona tamamlanır (`ChapterJourney.NextChapter`,
  `PurseToppedUp`; açılış panelinde satır). Başlangıç keseleri Köylü 120, Soytarı 130, Büyücü 150, Kral 200 (haraç formülü aynı).
- Sınıf güçleri (sadece Phase 2, demo aynı; `ClassKit` + `ClassPowers`, `ChapterHooks` ile her masaya bağlanır, kayda girer):
  Köylü HEYBE, Büyücü KEHANET, Kral HAZİNE (vergi, Saray, Müsadere); Soytarı değişmedi. Şarj göstergesi aynı, dolu güç K ile.
- Şeytan kuralları (sadece boss masaları, `DemonRules`): Mammon FAİZ (%2), Belial ROL TAKASI (her 4 elde), Lilith BAŞTAN ÇIKARMA (%50),
  Lucifer AYNA (sınıfa göre), bar başlangıcın %25'inin altında gösterge +1 / el. Masa açılırken şeytan kuralını söyler.

**Kararlarım (belgede):**
- Demo'ya hiçbir davranış değişikliği yok: Phase 2 her şeyi `HellPokerGame.UseHooks(TableHooks)` üzerinden yapar (demo masasında null).
  Kancalar: dağıtım, draw başlangıcı, kasanın draw'u, draw sonrası, kasanın re-raise'i, el sonu, Saray (`CheatTable.Guarded`).
- Heybe: atılan kart draw'dan sonra, bir sonraki dağıtıma kadar panelde seçilir; heybe kartları deste karılınca da dağıtılmaz.
  Heybeden çekilen kart atılan yerlere soldan sağa yerleşir (el için fark etmez). Hasat: heybenin en iyi doldurduğu kartlar.
- Kehanet'in bedeli: şeytan bildiği kart J veya üstüyse İki Çift'in altında re-raise yapmaz, 9 veya altıysa %20 fazladan re-raise yapar;
  Q veya üstünü biliyorsa elinde bir şey yokken draw'da azamî kart atar. Yeniden diz: dolu güç bir kez harcanır, o elde 2 kart.
- Müsadere: şeytanın açık kartları arasından kralın eline en iyi geleni alınır; açık kart yoksa sıradaki kapalı kart açılıp alınır
  (fiyata dahil). Bedava artırma: o elde sıradaki artırma, kayıp / çekilmede sayılmaz (`HellPokerGame.FreeStake`).
- Rol takası, Ayna'nın müsaderesi Saray'a, Ölü Adamın Eli kartlarına, zincirli ve gizli kartlara dokunmaz.
- Ayna (Büyücü'ye karşı): Lucifer üstteki kartı görür; pas geçecekken işine yarıyorsa çeker, çekecekken yaramıyorsa dibe yollar
  (ilk sürüm sadece birincisini yapıyordu, neredeyse hiç tetiklenmiyordu).
- Lilith'in aldığı kart showdown'da dudak işaretiyle (`CardMark.Tempted`, `card_marks` 7. kare).
- How to Play: SINNERS sayfası demo'nunki gibi kaldı (alan dolu); yeni **PHASE 2** sayfası sınıf güçlerini ve şeytan kurallarını anlatır.

**Ayar (kullanıcının sırasıyla):**
- Boss ödeme yüzdeleri (`BossSimulation`, sınıf × boss 2000 masa, güçler ve kurallarla):

| Boss | Kazanç / kayıp % | El | Lanet / düşüş | Kural (masa başına) |
|---|---|---|---|---|
| Mammon | 175 / 125 (aynı) | 20.3 | %12.2 | faiz 12.9 |
| Belial | 140 / 115 (eski) | 14.1 | %19.2 | |
| Belial | **130 / 110** | 16.7 | %19.0 | takas 2.5 |
| Lilith | 130 / 80 (eski) | 19.1 | %43.8 | |
| Lilith | 130 / 60 | 20.8 | %28.9 | |
| Lilith | 130 / 50 | 22.0 | %20.8 | |
| Lilith | **130 / 55** | 21.6 | %24.9 | baştan çıkarma 9.0 |
| Lucifer | 110 / 45 (aynı) | 20.6 | %40.8 düşüş | ayna 5.3 |

- Bölüm 1 katları (`ChapterSimulation.ChapterOne`): yeni keselerle iblis 20 / bekçi 40'ta kesesi boşalan %9.1, tam haraç %36.7, masa 3.9 el.
  **İblis 30 / bekçi 50** (1. kat 15): %11.6 (Köylü 4.4, Büyücü 8.9, Kral 3.0, Soytarı 30.3), tam haraç %52.9, masa 5.2 el. İblis 40 / 60:
  %12.5, %65.5, 6.4 el. Bölüm 2-3 iblis keseleri (30 / 60, 40 / 80) değişmedi.
- Sınıf farkı (≤ 12 puan hedefi) iblis keseleriyle kapanmıyor: Soytarı. Soytarı kesesi 195 → %16.5, 260 → %9.7 (deneme).
  **Kullanıcı kararı: Soytarı değişmedi; Köylü heybesi 3 kart, risk %25** (ilk tasarım 4 kart / %15'te Köylü %56 kurtuluyordu).

Bütün koşu (`WholeRun`, sınıf başına 1000 koşu, son ayarlar):

| Sınıf | B1 / B2 / B3 kesesi boşalan | Tam haraç B1 / B2 / B3 | Lucifer'e ulaşan | Kurtulan | Lucifer'de düşüş |
|---|---|---|---|---|---|
| Köylü | %3.6 / %4.5 / %5.5 | %68.5 / %95.7 / %99.1 | %65.3 | **%43.9** | %32.8 |
| Büyücü | %8.6 / %12.2 / %10.9 | %58.9 / %87.5 / %93.7 | %42.7 | %25.6 | %40.0 |
| Kral | %3.3 / %5.5 / %4.9 | %57.9 / %82.5 / %95.8 | %48.0 | %33.2 | %30.8 |
| Soytarı | %27.4 / %40.0 / %49.2 | %54.3 / %78.3 / %90.8 | %8.5 | **%3.8** | %55.3 |

- Kurtuluşta sınıf farkı 40 puan (hedef ≤ 10; Soytarı hariç 18 puan). Kullanıcı Soytarı'yı değiştirmemeyi seçti.
- Bölüm 2-3'te taban kese ve büyüyen kese yüzünden tam haraç %80-99 (Bölüm 1 hedefi 40-60).
- Koşu başına sınıf gücü kullanımı: Köylü 116 kart sakladı / 75 çekti / 25 hasat; Büyücü 71 kart dibe; Kral 69 vergi kazandı / 6 kaybetti,
  63 kart açtı, 39 müsadere (simülasyon oyuncusu bedava artırmayı kullanmıyor).
- Şeytan kuralları, masa başına (Köylü / Büyücü / Kral / Soytarı): faiz 11.1 / 19.3 / 20.0 / 8.9; takas 2.4 / 3.7 / 3.0 / 1.8; baştan çıkarma
  9.0 / 13.5 / 11.8 / 5.9; ayna 16.4 / 5.9 / 3.4 / 0.0 (Soytarı'ya karşı ayna jokerlerin kendisi). Köşede oynanan el masa başına 5-20.
## Dokuzuncu tur: denge — Soytarı teşhisi ve 2-3. bölüm haracı (2026-10-12)

**Teşhis** (`ChapterSimulation.WholeRun`, Soytarı, 2000 koşu, eski ayarlar; her 100 koşuda biten):

| Nerede | Bölüm 1 | Bölüm 2 | Bölüm 3 | Lucifer |
|---|---|---|---|---|
| Kesesi boşaldı (ortalama kat) | 29.2 (4.1) | 20.4 (4.1) | 11.1 (4.4) | — |
| Ruhunu kaybetti (boss) | 17.3 Mammon | 10.3 Belial | 4.2 Lilith | — |
| Düştü | | | | 3.5 |
| Kurtuldu | | | | 4.1 |

- Joker sayısı (deste): bölüm sonlarında ortalama 12.8 / 11.9 / 12.0, Lucifer'de 12.3 (kazanç +1, 10'un üstünde kayıp −1: 10-13 arasında oturuyor).
- İki jokerle kaybedilen el: katlarda %21.7 (kasanın iki jokeriyle kazanılan %21.8); boss masalarında Mammon %18.5 (%20.5), Belial %17.8
  (%18.2), Lilith %26.9 (%26.5), Lucifer %31.6 (%29.9). İki joker kuralı iki tarafa da işliyor; Soytarı'yı asıl öldüren katlardaki kese
  (koşuların %60'ı katlarda bitiyor) ve sonra Mammon.

**Deneme sırası (kullanıcının), sınıf başına 500 koşu:**

| Deneme | Soytarı kurtuluş | Not |
|---|---|---|
| Varsayılan (130 coin, 5000 yıl) | %4.4 | |
| a) kese 220 | %8.6 | B1 kesesi boşalan %11 |
| a) kese 260 | %9.6 | artık bosslar öldürüyor (ruh %21 / %18 / %9) |
| b) kese 260 + toplam ceza 4500 | %11.8 | b'nin en ucu |
| c) Lucifer'in iki jokerle kaybetmesi | — | zaten geçerli (`ShowdownResult.Judge` iki tarafa da uygular); değişiklik yok |

- **Hedef (%25-40) a-c ile tutmadı; uygulanmadı** (kullanıcı kuralı: en yakın sonuç ve seçenekler). Seçenekler: jokerlerin büyümesini
  yavaşlatmak (10'un üstünde kayıp −1 yerine her kayıp −1, ya da en çok 8 joker), Soytarı'ya kat masalarında iki joker kaybının yarı
  bedeli, ya da kese 260 + toplam 4500 + Lucifer'de bar %10 kısa (denenmedi).
- Köylü %40'ın üstünde: heybe riski **%30** yapıldı (kullanıcı kuralı); 500 koşuda %44.0, 2000'de %43.9 — risk Köylü'yü düşürmüyor.

**Haraç** (Bölüm 2-3'te tam ödeyen %80-99 idi; düğme: başlangıç üstü fark):

| Fark (B2 / B3) | Köylü B2 / B3 | Büyücü | Kral | Soytarı |
|---|---|---|---|---|
| +75 / +90 (eski) | %96 / %99 | %88 / %94 | %83 / %96 | %78 / %91 |
| +145 / +210 | %75 / %83 | %62 / %70 | | |
| +200 / +280 | %42 / %55 | %34 / %44 | | |
| **+180 / +270** (uygulandı) | %55.7 / %62.8 | %42.1 / %48.3 | %39.6 / %52.4 | %32.1 / %35.7 |

**Son durum** (2000 koşu, +180 / +270, heybe riski %30):

| Sınıf | B1 / B2 / B3 kesesi boşalan | Lucifer'e ulaşan | Kurtulan | Lucifer'de düşüş |
|---|---|---|---|---|
| Köylü | %3.9 / %5.3 / %5.6 | %63.1 | %43.9 | %30.5 |
| Büyücü | %8.7 / %11.4 / %13.0 | %39.8 | %22.5 | %43.5 |
| Kral | %3.0 / %4.9 / %5.4 | %49.3 | %32.9 | %33.3 |
| Soytarı | %29.2 / %38.0 / %48.6 | %7.6 | %4.1 | %46.1 |

- Açık (kullanıcıya): Soytarı (seçenekler yukarıda), Köylü %44 (> %40), Büyücü %22.5 (< %25; haraç artışından sonra −3).
## Onuncu tur: emanet havuzu ve sınıf yükseltmeleri (2026-10-13)

**Emanetler (24 + Çıngırak):** 8 yaygın (Kayıkçı Sikkesi, Paslı Taç, Dikenli Tespih, Kanlı Zar, Kurşun Kese, Kül Defteri, Mum Dibi,
Fare Dişi), 8 nadir (Kemik Zar, Demir Yemin, Hacı Feneri, Kuzgun Tüyü, Kanlı Yüzük, Eski Harita, Gümüş Çan, Tuz Çemberi), 4 efsanevi
lanetli (Kara Kitap, Cellat İpi, Lilith'in Gözyaşı, Uçurum Sikkesi), boss ganimeti 2 Mammon (Mühür, Terazi) + 2 Belial (Maske, Dil).
"Boss başına 2" Lilith için yok: Lilith'ten sonra ganimet yok, doğrudan Lucifer (8+8+4+4 = 24). Yeni lütuf / lanetler demo kancalarının
yanında Phase 2'ye özgü yüzdeler: hazine, Kara Pazar fiyatı, haraç, iblis kesesi, boss barının başlangıcı (`RelicEffects`).
- Kara Pazar: üç emanet, nadir %25 / efsanevi %8, fiyat nadirliğe göre 45 / 55 / 65 (bölümle büyür, emanetlerin pazar yüzdesiyle).
  **Pazar en çok 2 emanete kadar satar** (`BlackMarket.SellsUpTo`): üçüncü yuva bekçi ya da ganimetle dolar. (3 yuvayı pazar da
  doldurunca 2. bölümün haracı Köylü'de %49 → %37 düşüyordu; kararım, kullanıcı onayı bekliyor.)
- Bekçi: nadir %40 / efsanevi %15. Ganimet: şeytanın kendi iki emaneti (biri taşınıyorsa yerine nadir) + bir sınıf yükseltmesi, ya da 30 coin.
- Üç yuva (`ChapterRelics.Slots`, `RunEffects.MaxCarried`; demo 2'de kalır).

**Sınıf yükseltmeleri (4 × 4, koşuda birer kez):** Köylü Geniş Heybe (4 kart), Sessiz Heybe (risk yarı), Tohum (her yeni bölümde heybeye
10+), Bereket (değişten sonra iki kart); Büyücü Derin Kehanet (+1 kart), Kör Ayna (ellerin yarısında şeytan kart öğrenmez), Üç Kez Diz
(3 kart dibe), Gerçeğin Gözü (boss masasında bir kart dağıtımdan açık); Kral Taşan Hazine (15 vergi), Ucuz Müsadere (3), Taç Muhafızı
(kayıpta dörtte bir), Kraliyet Fermanı (bedava artırma 1); Soytarı Kontrollü Kaos (her kayıp bir joker siler), Çift Kahkaha (değişte ikinci
joker geri gider), Joker Kesesi (pazarın joker hizmeti yarı fiyat), Şaklaban (tek jokerle şeytan yarı yarıya az artırır).
Araf Ateşi 4. seçenek **Ruhunu bile**: hemen rastgele bir yükseltme, boss barı %10 uzun başlar.

**Simülasyon (2000 koşu; oyuncu: pazarda en pahalısını, ganimette yükseltmeyi, ateşte laneti susturmayı seçer):**

| Sınıf | Kurtuluş (A'da) | Tam haraç B2 / B3 (A'da) |
|---|---|---|
| Köylü | %39.9 (%43.9) | %48.7 / %67.0 (%55.7 / %62.8) |
| Büyücü | %21.4 (%22.5) | %35.8 / %50.5 (%42.1 / %48.3) |
| Kral | %30.9 (%32.9) | %35.5 / %53.8 (%39.6 / %52.4) |
| Soytarı | %8.1 (%4.1) | %33.8 / %51.9 (%32.1 / %35.7) |

2. bölümün haracındaki düşüş coin yerine eşya seçiminden: ganimette yükseltme yerine 30 coin alınınca Köylü'de B2 %87 (Mammon'un
Mührü haracı %25 düşürüyor).
- Emanet / yükseltme etkisi (2. bölüme onu taşıyarak başlayan koşuların kurtuluşu − taşımayanlarınki): ilk ölçümde 10 puanı geçenler
  ayarlandı — Kanlı Yüzük (kayıp ante %40 → kötülük +1, kazanç %112 → %110; −12.9 → −4.6), Cellat İpi (ante %60 → %75; +19.7), Kanlı Zar
  (kayıp ante %25 → %20), Lilith'in Gözyaşı (kazanç %85 → %90). Son ölçümde 10 puanı geçen tek şey **Çift Kahkaha** (Soytarı, +17.2:
  iki jokerle kaybı neredeyse siliyor); Soytarı hedefin çok altında olduğu için bırakıldı.
- En çok alınanlar: yükseltmeler (her sınıfta dördü de ~%20-22), sonra Mum Dibi, Eski Harita, Kanlı Yüzük, Demir Yemin (yaygınlık + fiyat).

## On birinci tur: kilitler, Çağırıcı ve Cehennem Daireleri (2026-10-13)

**Kilitler:** koşu 4 sınıf ve 14 emanetle başlar (`ChapterUnlocks`). Kilitli 10 emanet ve kilometre taşları (rekor defterinde sayılır):
Kanlı Zar 3 koşu, Fare Dişi pazardan 3 emanet, Tuz Çemberi bir bekçiyi yen, Eski Harita Belial'in bölümüne ulaş, Gümüş Çan bir haracı tam
öde, Kuzgun Tüyü Belial'i yen, Kara Kitap Lilith'in bölümüne ulaş, Cellat İpi Lilith'i yen, Lilith'in Gözyaşı Lucifer'e ulaş, Uçurum Sikkesi
bir kez kurtul. İlk kurtuluş ÇAĞIRICI'yı açar (sadece Phase 2; Boss Simülatörü görmez). Kilitli emanet pazarda, bekçide, ganimette çıkmaz;
kilitler sınıf kartında ve REKORLAR'da yazar, açılan her şey koşu sonu panelinde "AÇILDI".

**Çağırıcı:** İblisler GÖZ (şeytanın kapalı bir kartı açılır), EL (bir kartın onun açık en iyi kartıyla değişir), FISILTI (sıradaki hile,
doğrusu); elde birer kez, değişten önce; katta 5 / 15 / 8 coin (son coin alınmaz), boss masasında bar başlangıcının %2 / 5 / 3'ü kadar
uzar (el kapanınca yazılır). Her çağrıda %10 iblis azar: bir kartın bir rütbe düşer (alt kart desteden çıkar). Dolu güç LEJYON: elde
üçü bedava. Lucifer'in Aynası her 3 elde bir GÖZ'ü çağırır (bir kartını bilir, artırmasını ona göre yapar).
- İstenen kese 140 / toplam ceza 6500 ile kurtuluş %19.6 (500) → **kese 200, ceza 5800** (2000 koşu: %26.8). Ara denemeler: 5800/140
  %24.4, 5800/170 %24.7 (2000), 5600/170 %23.8 (2000).

**Cehennem Daireleri** (`Circles`; birikimli; sınıf kartında ‹ DAİRE n ›, sınıf her kurtuluşta en derin açık dairesinin bir sonrakini açar,
`ChapterRecords.CirclesOpen`; rekorlarda sınıf başına en derin kurtulunan daire):
1 iblis keseleri +%25 · 2 haraç +20 · 3 barlar +%15 (**kalın bar**: %15 uzun başlar ve kazanç %15 az siler — tek başına uzun bar bahisler
bara oranlı olduğu için hiçbir şey değiştirmiyordu) · 4 pazar ×1.25, ateş 2 seçenek · 5 şeytanların kötülük göstergesi bir kısa (her hile bir
el erken, bütün masa boyunca; "bir eksik kötülükle başlar"ın tek seferlik yorumu etkisizdi), Lucifer iki mekaniği kopyalar.

Simülasyon (sınıf başına 500 koşu, ortalama kurtuluş; Köylü / Büyücü / Kral / Soytarı / Çağırıcı):

| Daire | Ort. | K / B / Kr / S / Ç |
|---|---|---|
| 0 (2000 koşu) | %25.4 | 39.9 / 21.4 / 30.9 / 8.1 / 26.8 |
| 1 | %26.2 | 40.2 / 23.0 / 32.4 / 7.4 / 27.8 |
| 2 | %25.9 | 40.4 / 21.4 / 30.8 / 7.8 / 29.0 |
| 3 | %20.2 | 34.2 / 17.2 / 26.6 / 5.2 / 17.8 |
| 4 | %20.8 | 35.2 / 17.8 / 24.6 / 8.0 / 18.2 |
| 5 | %21.2 | 38.6 / 16.4 / 26.2 / 7.6 / 17.0 |

**Hedef tutmadı** (her daire daha düşük, 5. daire %5-10). İblis keseleri iyi oynayan için kazanç (eşleşme daha uzun, daha çok coin),
haraç +20 ve pazar küçük. Kullanıcıya seçenek (uygulanmadı, `ChapterTuning.CircleWinStepPercent` / `HELLPOKER_CIRCLE_WIN`): **her daire
şeytanların kazancını %5 kırpsın** → 5. daire ort. %10.0 (26.6 / 5.0 / 9.8 / 3.8 / 5.0); %4 → %12.6, %8 → %2.9.

## On ikinci tur: dairelerin kazanç kesintisi — Demo 2.0.1 (2026-10-13)

Kullanıcı kararı: her daire şeytana karşı kazancı %5 kırpar (`Circles.WinStepPercent`, `ChapterTuning.CircleWinStepPercent` oyunda 5;
`HELLPOKER_CIRCLE_WIN` düğmesi duruyor). Daire 3'ün kalın barıyla birlikte: kazanç yüzdesi × 100 / 115 × (100 − 5 × daire) / 100.

Simülasyon (sınıf başına 500 koşu; daire 0 2000 koşu; Köylü / Büyücü / Kral / Soytarı / Çağırıcı):

| Daire | Ort. | K / B / Kr / S / Ç |
|---|---|---|
| 0 | %25.4 | 40.9 / 21.4 / 31.8 / 8.2 / 26.8 |
| 1 | %24.4 | 40.0 / 20.0 / 28.8 / 7.4 / 25.6 |
| 2 | %22.5 | 40.0 / 17.8 / 25.4 / 6.2 / 23.0 |
| 3 | %15.8 | 30.8 / 11.6 / 20.0 / 4.0 / 12.8 |
| 4 | %14.2 | 31.2 / 9.2 / 16.8 / 4.8 / 9.2 |
| 5 | %10.0 | 26.6 / 5.0 / 9.8 / 3.8 / 5.0 |

Hedef tuttu: her daire bir öncekinden düşük, 5. daire %10.0 (hedef %5-15); %4 / %6 denenmedi.

**Kullanıcı onayladı (değişiklik yok):** Çağırıcı kese 200 / ceza 5800; Kara Pazar en çok 2 emanete kadar satar; "barlar +%15" kalın bar
yorumu ve 5. dairedeki "bir eksik kötülük" (gösterge bir kısa) yorumu; 24 emanetin dağılımı (8 + 8 + 4 + Mammon 2 + Belial 2).
Soytarı ve Büyücü'nün dengesine oyun testinden sonra bakılacak.

## On üçüncü tur: Demo 2.1 — ceza, harita, olaylar, kamp, pazarlar, emanetler (2026-10-13)

Ayrıntı ve iş başına kararlar DEVLOG'da (A–H). Özet:

- **Ceza ve bar ayrı** (`RunSentence`): şeytan masasında kazanç ikisinden aynı, kayıp cezaya tam, bara %50 (`BossTable.BarHealPercent`);
  boss kayıp yüzdeleri ×2 (Mammon 250, Belial 220, Lilith 110, Lucifer 90). Kurtuluşta kalan ceza puan.
- **Harita sürüm 2** (`MapMaker`): katta 2-5 düğüm, kesişmeyen yollar, "?" düğüm, orta kamp, 1-2 hazine. Ağırlıklar erken Masa 55 / Olay 25 /
  ? 10 / Pazar 15, geç 25 / 25 / 25 / 30.
- **16 seçimli olay** (`ChoiceEvents.cs`), **kamp** (DİNLEN / BİLE / TÖREN / DÖV) ve **çanta** (5 takılı + 5), **6 satıcı**, Soytarı 20 → 10,
  **39 emanet**.

### Simülasyon (daire 0, sınıf başına 1000 koşu; Demo 2.0.1: 2000 koşu)

| Sınıf | Demo 2.0.1 | Demo 2.1 | Kurtuluşta kalan ceza ort. (p10 / p50 / p90) | TERTEMİZ |
|---|---|---|---|---|
| Köylü | 40.9 | 49.5 | 2838 (517 / 2285 / 5826) | %2.4 |
| Büyücü | 21.4 | 23.5 | 5553 (1616 / 4513 / 10713) | %0 |
| Kral | 31.8 | 38.0 | 5281 (1554 / 4410 / 9701) | %0.5 |
| Soytarı | 8.2 | 14.3 | 2879 (493 / 2104 / 6388) | %2.8 |
| Çağırıcı | 26.8 | 30.3 | 5015 (1724 / 4038 / 9879) | %0 |
| Hepsi | 25.8 | **31.1** | 4273 (898 / 3358 / 8599) | %1.2 |

İş başına (500 koşu, aynı tohumlar; Demo 2.0.1 28.4): A 28.4, B 27.7, C 27.3, D 28.6, E 28.4, G 32.1 (F: Soytarı 13.0). Artışın kaynağı
yeni emanetler (G) ve F'nin Soytarı kuralı (+6 Soytarı'da, ortalamaya ~+1.2).

**Hedef (+5 puan) ayarı:** 2000 koşuda 32.2 (+6.4) → emanet fiyatı 45 / 55 / 65 → 50 / 60 / 70 ve DİNLEN %6 → %5 (ceza ve bar): 31.6 →
DİNLEN'in bar payı %3: 31.2 → fiyat 55 / 65 / 75: **31.1 (+5.3)**. Fiyat ve DİNLEN etkisi gürültü düzeyinde; F'nin payı (~1.2) çıkınca +4.1.
Daha fazla kısmak emanetlerin kendisine dokunmayı ister (aşağıda, kullanıcıda).

Yol başına ortalama düğüm (bölüm 1 / 2 / 3): Masa 2.7 / 3.6 / 4.5, Olay 0.8 / 1.3 / 1.8, Pazar 0.5 / 0.6 / 0.8, Bekçi 1.2 / 1.1 / 1.1,
Hazine 0.7 / 0.7 / 0.7, Kamp 1.3 / 1.2 / 1.2, ? 0.4 / 0.8 / 1.1.
Kamp seçimleri: DİNLEN %62.2, DÖV %28.4, TÖREN (lanet) %5.5, BİLE %3.9; kampta ort. 2.5 takılı, 0.1 çantada.
Satıcılar (ziyaret / alınan): Kaçakçı 1361 / 626, Rehinci 1239 / 1605, Kemik 1322 / 546, Simyacı 1262 / 546, Kumarbaz 1407 / 204,
Terzi 1298 / 219 (alan / almayan farkları sınıf içinde bile karışık: alım parası olan koşu zaten farklı).
Olaylarda "!" : `fallen_angel` (emanet) −13.4, `soul_insurance` −16.6, `fake_pardon` +5.5 (seçim kendi kendini seçen örneklem).
Emanetlerde "!" (2. bölüme taşıyan koşular): Kan Sözleşmesi (`blood_pact`) +27.6 (her sınıfta +11..+39), Kurşun Mühür Köylü / Kral +11 / +12, Çift Zar +6.4;
`mammons_gold_tooth` −12.6 (haraç +%25).

**Kullanıcıda:** ceza puanı ölçeği (kurtuluşta ort. ~4300 yıl, TERTEMİZ %1); Kan Sözleşmesi'nin gücü; Soytarı / Büyücü farkı; +5.3.

### Kullanıcının kararı ve son ayar (2026-10-13)

Kullanıcı: sınıflar farklı zorlukta olabilir; güçlü emanet dizilimleri (build) ve "kırık" şeyler oyunun parçası. Sınıf dengesine ve Kan
Sözleşmesi'ne dokunulmadı; fiyat 55 / 65 / 75 ve DİNLEN (ceza −%5, bar −%3) kaldı; ceza puanının ölçeği aynı (golf skoru gibi: az olan iyi).
Düzeltilen iki "tuzak gibi çalışan" şey:
- **Mammon'un Altın Dişi:** lütuf haraçtan sonraki keseyi sayıyordu (haraç keseyi boşaltıyor: lütuf ~0, lanet tam). Artık kapıya getirilen
  keseyi sayar (`ChapterRun.GateCoins`, kayıtta `gate.coins`), laneti haraç +%25 → +%15. Simülasyonda farkı −12.6 → −7.0 (örneklem küçük,
  emanete para harcayan koşu karışık).
- **Düşmüş Melek** (yardım olayı): 150 yıl (bara da biniyor) → 100. Alanlar −13.4 geride kalıyordu.

Son simülasyon (1000 koşu): Köylü 50.1, Büyücü 24.6, Kral 38.4, Soytarı 15.0, Çağırıcı 32.4, **hepsi 32.1** (Demo 2.0.1'e göre +6.3;
kullanıcının "kırık build olabilir" kararıyla kabul).
## Sonraki adım (eski plan)

Önce sadece Bölüm 1'in kat ekonomisi (Mammon'a kadar):
1. Core'da `FloorTable` (coin'le kat eli: mevcut `HellPokerGame` akışı, `StakeScale.Fixed(ante, 6 ante)`, ×3 çarpan tavanı, çekilmede
   ek bedel yok, `ImpBettingStrategy`), harita üretici (`ChapterMap`, tohumlu), Kara Pazar ve Ateş seçimleri.
2. `ChapterSimulation` (EditMode, `[Explicit]` değil ama ayrı filtre): akıllı oyuncu + mağaza / ateş / yol seçimi politikası; rapor:
   haracı tam ödeyen %, kapıdaki coin dağılımı, Kara Pazar'dan emanet alanların eksiği, iblise karşı el kazanma oranı.
3. Hedefler tutmazsa sırayla maç bonusu, ante, iblis yapay zekası.

## On dördüncü tur: Demo 2.1.1 — sabit boss bahsi, elde bir güç (2026-10-14)

Simülasyon (WholeRun, daire 0, sınıf başına 500 koşu; Büyücü 1000; her sınıf ayrı çalıştırıldı — 5×1000 tek koşu bellek yüzünden kesildi).
Düğmeler: `HELLPOKER_FIXED_STAKES=0` (bahis kalan bara göre), `HELLPOKER_POWER_HAND=0` (güç dolu şarjla).

| Sınıf | Eski kurallar | Yalnız G (sabit bahis) | G + H (elde bir güç) |
|---|---|---|---|
| Köylü | 49.6 | 47.8 (yeni oyuncu modeliyle 54.8) | 47.2 |
| Büyücü | 23.6 | 20.3 | 20.3 |
| Kral | 37.8 | 35.4 | 33.0 |
| Soytarı | 16.2 | 16.2 | 16.2 |
| Çağırıcı | 34.8 | 37.6 | 35.6 |

- G: sınıf başına −2.6..+2.8 (sınır ±3): yüzde ayarı gerekmedi. Boss başına el ~15-25'ten ~5-13'e indi (son darbe hızlı geliyor).
- H ilk hali (Büyücü 2 kart dibe bedava, Kral bedava müsadere, Lejyon üç iblis bedava): Büyücü +13.0, Kral +11.4, Çağırıcı +10.4 — sınır +5.
  Kısmalar (gücü kısıtlamadan): Büyücü 1 kart + bedel (şeytan masasında bar %1, katta 2 coin; %2 / 5 coin'de 11.2 — fazla ağır), Kral K
  müsaderesi yarı vergiye (2), Lejyon elin ilk iblisini bedava yapar. Son fark: Büyücü 0.0, Kral −2.4, Çağırıcı −2.0, Soytarı 0.
- Köylü'de H −7.6: simülasyon oyuncusu her elde hasat edip heybeyi boşaltıyor (gücün kendisi değil, oyuncu modeli). Oyuncu artık hasadı yalnız
  eli iki çift ve üstüne çıkarıyorsa kullanıyor; Kral'ın oyuncusu müsadere için vergi biriktiriyor (önce her vergiyi AÇ'a harcıyordu).
- **Yeniden ölçüm (2026-10-15, `v2.1.1-demo` kodu, aynı oyuncu modeli; 500 koşu, hepsi ayrı ayrı):**

  | Sınıf | Eski (G ve H kapalı) | Yalnız G | Yalnız H | G + H |
  |---|---|---|---|---|
  | Köylü | 57.4 | 54.8 | 54.2 | 47.2 |
  | Büyücü | 23.2 | – | – | 19.6 |
  | Kral | 37.8 | – | – | 33.0 |
  | Soytarı | 16.2 | – | – | 16.2 |
  | Çağırıcı | 35.0 | – | – | 35.6 |

  Eski kuralların Köylü'sü yeni oyuncu modeliyle 57.4: yukarıdaki 49.6 eski modelindi. Köylü'de G −2.6, H −3.2, ikisi birlikte **−10.2**
  (toplamdan büyük; 500 koşunun gürültüsü ~±3). Yukarıdaki "H −7.6 oyuncu modelinden" açıklaması yetmiyor: G ile elde bir hasat
  birlikte Köylü'yü gerçekten zayıflatıyor. Kral −4.8, Büyücü −3.6. Açık: Köylü'ye telafi (kullanıcı kararı; sınıflar farklı zorlukta olabilir).
- Şeytanın Pusulası (500 koşu, Köylü): 38 alım, 2. bölümde taşıyanların kurtuluşu +0.5 — 10 puan sınırının çok altında.

## On beşinci tur: Demo 2.2 — Kâhin, harita aralığı, hata avı (2026-10-16)

- **Kâhin (Oracle, id "warlock"):** ÖNSEZİ her bölümde 2 kart (Üçüncü Göz +1), KADERİ BÜK elde bir kez bir kartı üste ya da dibe,
  bedelsiz (bedel Ayna Laneti). Izgara (500 koşu, `ChapterSimulation.OracleTuning`): 2.1.1'in bedeliyle %20.6, 3 kartla %23.8,
  bedelsiz %28.2, bedelsiz 3 kartla %32.0; şeytanın bilme oranını %50'ye indirmek simülasyon oyuncusunda fark etmiyor.
- **Harita sürüm 3:** yol üzerinde iki pazar ya da iki kamp arasında en az 2 kat, yolda en çok 2 pazar. Kese / haraç oranları eski
  haritaya göre ±5 puan içinde; 3. bölümde pazar 0.9 → 0.6 uğrama.
- Çağırıcı'nın LEJYON'dan sonraki ücretli iblisi artık kesesi yetmezse gelmiyor (düzeltme; simülasyonda fark gürültü içinde).

### Son simülasyon (WholeRun)
Daire 0, sınıf başına 2000 koşu:

| sınıf | Lucifer'e | kurtuluş | 1. böl. kese boşalan / tam haraç | 2. böl. | 3. böl. | Lucifer'de düşüş |
|---|---|---|---|---|---|---|
| Köylü | %69.7 | **%51.3** | 3.7 / 75.1 | 5.2 / 38.4 | 4.9 / 55.1 | %26.4 |
| Kâhin | %49.5 | **%29.8** | 6.9 / 70.1 | 9.8 / 31.8 | 10.3 / 50.3 | %39.9 |
| Kral | %47.4 | **%31.7** | 4.7 / 61.6 | 8.9 / 24.1 | 6.6 / 39.5 | %33.2 |
| Soytarı | %18.5 | **%13.6** | 30.0 / 56.2 | 34.3 / 22.2 | 24.5 / 46.2 | %26.8 |
| Çağırıcı | %51.6 | **%36.9** | 6.6 / 47.2 | 9.8 / 67.2 | 9.5 / 71.5 | %28.6 |

Boss başına el (ortalama, Köylü / Kâhin / Kral / Soytarı / Çağırıcı): Mammon 8.4 / 10.9 / 11.5 / 9.6 / 10.8, Belial 6.6 / 7.8 / 7.2 /
6.4 / 9.1, Lilith 6.9 / 9.8 / 10.2 / 5.8 / 12.6, Lucifer 5.9 / 7.3 / 8.2 / 5.5 / 12.3. Kamplar: dinlen %59-67, döv %28-31, tören %4-6,
bile %1-5. Hedef %25-40: Kâhin, Kral, Çağırıcı içinde; Köylü üstünde, Soytarı altında (kullanıcı kararı: sınıflar farklı zorlukta
olabilir, zorla eşitlenmedi).

Daireler (sınıf başına 500 koşu; Köylü / Kâhin / Kral / Soytarı / Çağırıcı, ortalama):
- 1: 51.6 / 27.6 / 30.0 / 12.0 / 35.0 → 31.2
- 2: 46.0 / 24.4 / 27.0 / 11.8 / 31.8 → 28.2
- 3: 43.4 / 19.2 / 20.4 / 8.0 / 19.4 → 22.1
- 4: 39.6 / 15.6 / 17.4 / 10.4 / 16.0 → 19.8
- 5: 33.0 / 12.0 / 14.8 / 6.4 / 8.0 → 14.8
