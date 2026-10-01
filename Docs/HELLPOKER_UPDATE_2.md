# Hell Poker — Güncelleme Planı 2 (Düzeltmeler + Şeytan Salonları + Ruh Mekaniği)

Bu dosyayı baştan sona uygula. Üç bölüm var. **Bölümleri sırayla yap; her bölümün sonunda testleri geçir ve DEVLOG'a kayıt ekle.**
Mevcut mimari kurallara uy (CLAUDE.md): Core Unity'siz, prefab yok, UI koddan kurulur, presenter animasyon bilmez,
tüm oyuncu metinleri UiText'te, rastgelelik IRandomSource'tan, görseller Tools/ArtGen'deki piksel sistemle ve 31 renklik
paletle üretilir. Tüm yeni sayılar ayarlanabilir olsun (GameRules / Dealer / StakeScale / PayoutTable).

---

## BÖLÜM 1 — İki küçük düzeltme

1. **Kasanın re-raise'i masa tavanını aşabilsin.** Bu sadece kasanın re-raise'i için geçerli; oyuncunun artırmaları tavana
   bağlı kalsın. Re-raise yine cezayı (YearsOffTable) aşamaz. Oyuncu re-raise'i karşılarsa toplam bahis tavanın üstüne çıkabilir.
   Nedeni: tavan %30 iken ante + artırmalar tavanı dolduruyor, re-raise ellerin sadece ~%1-2'sinde görülüyor.
   Testleri güncelle. BalanceSimulation re-raise sıklığını da raporlasın. Beklenen: re-raise ellerin ~%4-9'u,
   aklanma oranları değişmemeli (Mammon ~%79, Belial ~%70-74, Lilith ~%64).
2. **Lilith'in portresi zeminde kayboluyor:** mor yüz mor zemin üzerinde, kanatlar bulanık leke gibi, hilal taç neredeyse görünmüyor.
   Paletten daha açık ve kontrastlı bir ten rengi (ör. soluk kemik-leylak), kanatlara net bir kenar çizgisi ve parlak bir hilal taç ver.
   Siluet Mammon ve Belial kadar okunur olsun. Diğer şeytanlara dokunma.

---

## BÖLÜM 2 — Her şeytanın kendi salonu (arka planlar)

Her şeytan farklı bir cehennem kumarhanesinde ("hell house") oturuyor. Masa yok; arka plan o salonu anlatır.

3. **Salonlar** (piksel art, 480×270, aynı palet, şeytanın karakteriyle uyumlu):

   | Şeytan | Salon | İçerik fikirleri | Baskın renk |
   |---|---|---|---|
   | **Mammon** | Tefecinin Hazine Odası | altın yığınları, kilitli kasalar, büyük terazi, borç defterleri, sikke sütunları | altın, koyu yeşil |
   | **Belial** | Gümüş Dilin Tiyatrosu | gümüş perdeler, maskeler, yılan sarmallı sütunlar, boş tiyatro koltukları | gümüş, kan kırmızısı |
   | **Lilith** | Gecenin Bahçesi / Ay Mahzeni | dev hilal ay, ölü ağaçlar, gece kuşları, mor sarmaşıklı mezar taşları | gece mavisi, leylak |

4. **Okunabilirlik önce gelir:** kartların, diyalog kutusunun ve sayacın durduğu bölgeler sade ve koyu kalsın
   (kenarlarda detay, ortada koyu vinyet). Kartlar her zaman arka plandan net ayrılsın.
5. **Varyantlar:** her salon için `normal` + `hell` (son 250 yıl: kızıllaşmış / alevli) + `soul` (Bölüm 3'teki ruh bölgesi:
   soğuk, solgun, hayaletimsi ton). Dosya düzeni: `Art/Backgrounds/<dealerId>/normal.png`, `hell.png`, `soul.png`.
6. **Hafif hareket (isteğe bağlı ama tercih edilir):** 2-4 karelik ortam animasyonu: Mammon'da sikke parıltısı,
   Belial'de dalgalanan perde, Lilith'te uçan kuş / titreyen mum. Mevcut SpriteFrameAnimator kullanılsın.
7. **Yedek zinciri:** salon dosyası yoksa mevcut `Ui/background.png`, o da yoksa düz koyu renk. Oyun eksik görselle bozulmasın; test yaz.
8. **Şeytan seçim ekranında** seçili şeytanın salonu arka planda görünsün (seçim değişince geçiş efektiyle).
9. Önizleme sayfasına salonları da ekle. Tüm ekranların ekran görüntülerini her üç salonla al ve kontrol et.

---

## BÖLÜM 3 — Ruh mekaniği ve masa değiştirme

### Kavram
Ceza tek ve ortaktır; oyuncu şeytanlar arasında gezebilir. Her şeytanın bir **ruh eşiği** var. Ceza o eşiğe ulaşınca oyuncunun
ruhu masaya konur. Ruhun değeri **1000 yıl**, ama oyuncu bu sayıyı **hiç görmez**; sadece bir bar görür.

### Kurallar (Core)
10. **Ruh eşiği** (Dealer'a yeni alan `SoulThreshold`): **Mammon 2000, Belial 1750, Lilith 1500.**
11. **Ruh değeri** (GameRules: `SoulWorthYears` = 1000). Ceza eşiği geçince fark ruhtan düşer:
    `kalan ruh = 1000 − (ceza − eşik)`. Kalan ruh 0'a inerse (ceza ≥ eşik + 1000) → **Damned** (oyun biter).
    Eski "2000 yılda lanet" kuralı bununla değişir.
12. **Ruh bölgesinde bahis:** birim ruh değerine göre hesaplanır (1000 / 10 = **100**), tavan ruh değerinin **%30**'u (300)
    ve kalan ruhu geçemez. Kasanın re-raise'i Bölüm 1'deki gibi tavanı aşabilir ama kalan ruhu aşamaz.
13. **Ruh bölgesinde kayıplar ×1.5** (GameRules: `SoulLossPercent` = 150, mevcut LossPercent ile çarpılır, yukarı yuvarla).
    Tema: ruh daha hızlı yanar. Nedeni denge: ruh mekaniği oyuna fazladan 1000 yıllık can katıyor ve oyunu kolaylaştırıyor.
    Simülasyonla beklenen sonuç:

    | Şeytan | Ruh eşiği | Aklanma | Ortalama el |
    |---|---|---|---|
    | Mammon | 2000 | ~%87 | ~43 |
    | Belial | 1750 | ~%77 | ~22 |
    | Lilith | 1500 | ~%66 | ~39 |

14. **Ruhu kurtarmak:** ruh bölgesinde kazanılan yıllar önce ruhu doldurur. Ceza eşiğin altına inince ruh geri alınır,
    bar kaybolur, normal oyun ve normal bahis birimi döner.
15. **Son 250 yıl** kuralı aynen kalır (ruh bölgesiyle çakışmaz).
16. **Masa değiştirme:**
    - Sadece eller arasında (Betting fazında) yapılabilir. Masada "LEAVE TABLE" butonu (ve menüden) → şeytan seçim ekranı.
    - Ceza, oturulan yeni şeytanın masasına aynen taşınır. Yeni şeytanın ev kuralları geçerli olur.
    - **Ruh masadayken (ceza ≥ o anki şeytanın eşiği) masadan kalkılamaz.** Buton kilitli olsun, şeytan buna özel bir replik söylesin.
    - Oyuncu, eşiğini zaten geçtiği bir şeytanın masasına oturmak isterse uyarı çıksın ("Your soul will be on her table").
      Onaylarsa oturur ve ruhu hemen masaya konur (artık kalkamaz). Bu bir tuzak ve bilerek bırakılıyor.
    - Şeytan seçim ekranı her şeytanın ruh eşiğini göstersin; mevcut cezaya göre "safe / your soul is at stake" bilgisi olsun.
    - Continue / kayıt mantığı varsa oturulan şeytan ve ceza birlikte saklansın.
17. Oyun başlangıcı değişmez: New Game → şeytan seç → 1000 yılla başla.

### Arayüz
18. **Ruh barı:** ruh bölgesine girilince ceza sayacının yerine (ya da altına) piksel art bir ruh barı çıksın
    (ör. içinde titreyen soluk mavi alev olan bir şişe / kandil). Giriş anı belirgin olsun: kısa bir efekt + şeytanın repliği.
19. **Sayılar gizli:** ruh bölgesinde hiçbir yerde yıl sayısı görünmesin. Ante, artırma, re-raise ve "Kazanırsan / Kaybedersen" satırı
    sadece barın üzerinde gösterilen parçalarla ifade edilsin: risk altındaki kısım yanıp sönsün, kazanınca bar dolsun, kaybedince boşalsın.
    Artır butonunda sayı yerine "WAGER MORE OF YOUR SOUL" gibi bir metin olsun.
20. Ödeme tablosundaki çarpanlar görünmeye devam edebilir (oran bilgisi, sayı değil).
21. Ruh bölgesinde salon `soul` varyantına geçsin; şeytanlar için uygunsa `final` animasyonu ya da yeni bir `soul` durumu kullanılsın
    (yoksa yedek zinciri).
22. Ruh kurtarılınca da belirgin bir an olsun (bar kaybolur, sayaç geri gelir, şeytanın hayal kırıklığı repliği).

### Testler ve kontrol
23. Core testleri: eşik geçişi (giriş/çıkış), ruh bölgesinde birim ve tavan, ×1.5 kayıp, ruh tükenince Damned, ruh bölgesinde kazanç,
    masa değiştirme (izinli / kilitli / eşiği geçilmiş şeytana oturma), cezanın masalar arasında taşınması.
24. Presenter testleri: ruh bölgesinde hiçbir view'a yıl sayısı gitmediğini doğrula (fake view'larla).
25. BalanceSimulation ruh mekaniğiyle çalışsın ve yukarıdaki tabloya yakın sonuç versin; çok saparsa nedenini DEVLOG'a yaz,
    kuralı kendi başına değiştirme.
26. Ekran görüntüleri: ruh bölgesine giriş, ruh bölgesinde karar anı, re-raise, kayıp, ruhun kurtarılması, kilitli LEAVE TABLE,
    eşiği geçilmiş şeytana oturma uyarısı.
27. DEVLOG ve CLAUDE.md'deki kural ve görsel bölümlerini güncelle.
