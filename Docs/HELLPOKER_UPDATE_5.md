# Hell Poker — Güncelleme Planı 5 (Şeytan Hileleri)

Hedef: oyun "sıra tabanlı dövüş" gibi hissettirsin. Kimse kimseye hasar vurmaz; çatışma **kartların üzerinde** yaşanır.
Şeytanın bir sonraki hamlesi önceden görünür (düşman niyeti), hilesi kartlarda görünür bir darbe olur. İleride oyuncunun
yetenekleri bu hilelere karşılık verecek (Plan 6), bu yüzden her hile **engellenebilir / iptal edilebilir** tasarlanır.

Bölümleri sırayla yap; her bölümün sonunda testleri geçir ve DEVLOG'a kayıt ekle. Mevcut mimari kurallara uy (CLAUDE.md).
Görselleri Tools/ArtGen piksel sistemiyle sen üret (ikonlar, efektler).

---

## BÖLÜM 1 — Altyapı (Core)

1. **Kötülük (Malice):** her şeytanın bir göstergesi var (`Dealer.MaliceMax`). Dolma kuralları (`GameRules`'tan ayarlanabilir):
   - Her el başında +1.
   - Oyuncu bir el kazanınca +1.
   - Kurtuluşa yakınlık: ceza ≤ 500 ise her el +1 daha (Lucifer hariç).
   - Gösterge dolunca o elde bir hile **seçilir ve niyet olarak duyurulur**; hile oynanınca gösterge sıfırlanır.
   - Başlangıç değerleri: Mammon max 4, Belial max 3, Lilith max 3, Lucifer **her el bir hile** (max 1).
2. **Hile seçimi:** her şeytanın küçük ve büyük hileleri var. Ceza ≤ `MajorCheatYears` (400) iken büyük hile ihtimali %50, değilse küçük.
   Lucifer'de büyük hile kuralı ayrı (aşağıda). Seçim `IRandomSource` üzerinden.
3. **Mimari:**
   - `ICheat`: `Id`, `Tier` (Minor / Major), `Timing` (hilenin elin hangi anında oynandığı: deal sonrası, draw öncesi, draw sonrası,
     kasa açılışı, showdown öncesi), `CanApply(state)`, `Apply(state) → CheatResult` (hangi kartlar etkilendi, ne değişti).
   - `CheatResult` presenter'a anlamsal bilgi taşır (kart indeksleri, etki türü); animasyonu view seçer.
   - `ICheatPolicy` (şeytanın hile listesi + seçim), `Dealer` paketine eklenir. Somut sınıflar `HellPokerGameFactory`'de seçilir.
   - **Engelleme kancası:** hile oynanmadan önce `ICheatGuard` sorgulanır (şimdilik hep "izin ver"). Plan 6'da oyuncu yetenekleri buraya bağlanacak.
   - **Niyet:** `IHellPokerGame.PendingCheat` (elin başında hangi hile gelecek; Belial için gösterilen niyet sahte olabilir, aşağıda).
4. **Değişmez kurallar:**
   - Showdown sonucu sonradan değiştirilmez (tek istisna Lucifer'in "Düşüş"ü).
   - **Dead Man's Hand'e hile işlemez:** A♠ A♣ 8♠ 8♣ kartları hiçbir hileden etkilenmez; hedef olursa hile başka karta geçer ya da boşa gider.
   - Hileler oyuncunun bilmediği bir kuralı gizlice değiştirmez: her hile oynandığı anda ekranda görünür.
   - Mühür, ruh, Lucifer kapısı ve kayıt kuralları geçerli. Yarım elde kayıt hilenin durumunu da saklasın (kayıt sürümü v=3, v=2 okunmaya devam).

---

## BÖLÜM 2 — Şeytanların hileleri (Core)

### Mammon — Tefeci (dürüst: niyeti her zaman doğru)
5. **Rehin (Collateral)** — küçük, draw öncesi: oyuncunun açık kartlarından en yükseğine altın zincir vurulur; o kart bu elde değiştirilemez.
6. **Haraç (Tithe)** — küçük, showdown öncesi: bu eli oyuncu kazanırsa silinen yıllardan 1 birim kesilir (en az 0).
7. **Kartı Satın Al (Buyout)** — büyük, draw öncesi: oyuncunun en yüksek kartı kasanın en düşük kartıyla takas edilir.

### Belial — Gümüş Dil (yalancı: gösterdiği niyet %25 ihtimalle sahte)
8. **Sahte Yüz (False Face)** — küçük, kasa açılışı: açtığı kartlardan biri sahte görünür (yanıltıcı bir kart); showdown'da gerçeği açılır.
   Sahte kartta çok hafif bir gümüş parıltı olur (dikkatli oyuncu fark edebilir).
9. **Çatal Dil (Forked Tongue)** — küçük, draw sonrası: oyuncunun bir kartının rengi (suit) değişir; flush'ı bozmayı hedefler,
   flush yoksa rastgele bir kart.
10. **Yılan Takası (Serpent Swap)** — büyük, draw sonrası: iki el arasında kapalı bir kart takas edilir; oyuncunun hangi kartı gittiği
    showdown'a kadar gizli (o kart oyuncuya kapalı görünür).
11. **Sahte niyet:** Belial'in duyurduğu niyet %25 ihtimalle başka bir hiledir (gerçeği oynanınca ortaya çıkar, kısa bir "liar" efekti).

### Lilith — Gecenin Kraliçesi (acımasız: alır götürür)
12. **Gece Örtüsü (Night Veil)** — küçük, draw öncesi: oyuncunun bir kartı oyuncuya kapanır, showdown'a kadar görünmez
    (draw'da yine seçilebilir, kör seçim).
13. **Diken (Thorn)** — küçük, draw öncesi: bir karta diken batar; o kartı değiştirmek 1 birim ceza ekler (anında, sayaçta görünür).
14. **Aysız Gece (Moonless)** — büyük, draw: oyuncunun draw'da aldığı yeni kartlar showdown'a kadar kapalı kalır.

### Lucifer — The Morning Star (her el bir hile, niyet her zaman görünür ve doğru)
15. **Bakış (Gaze)** — küçük: bu elde kasanın re-raise kararı oyuncunun elini bilerek verilir (oyuncu kazanacaksa re-raise yok,
    kaybedecekse %100 re-raise).
16. **Yeniden Yazma (Rewrite)** — küçük, draw sonrası: oyuncunun en iyi elini bozacak bir kart başka bir karta dönüşür
    (en iyi elin bir parçası, mümkünse eli bir alt kategoriye düşürecek şekilde).
17. **Yanan Kart (Burning Card)** — küçük, draw öncesi: oyuncunun en yüksek kartı alev alır ve destedeki rastgele bir karta dönüşür.
18. **Düşüş (The Fall)** — büyük, **her denemede en fazla bir kez**, sadece ceza ≤ 150 iken: oyuncu showdown'ı kazanırsa iki eldeki
    birer kart (her elin en yüksek kartı) desteden yeniden dağıtılır ve el yeniden değerlendirilir. Elin başında niyet olarak duyurulur
    ("THE FALL AWAITS"), yani oyuncu bilerek oynar (çekilebilir).

---

## BÖLÜM 3 — Sunum (hissiyat)

19. **Kötülük göstergesi:** şeytan portresinin altında piksel bir gösterge (şeytana göre: Mammon sikke, Belial yılan pulları,
    Lilith dikenler, Lucifer tek bir kor). Dolarken kısa bir efekt.
20. **Niyet ikonu:** gösterge dolunca elin başında portrenin üstünde hilenin ikonu ve kısa adı ("COLLATERAL", "FALSE FACE"...).
    Üzerine gelince / H panelinde ne yaptığını tek cümleyle anlatsın. Belial'in sahte niyeti ortaya çıkınca ikon parçalanıp gerçeğe dönüşsün.
21. **Kart üzerinde darbe:** her hilenin kendi görsel efekti, kart üzerinde:
    zincir (rehin), sikkelerin uçması (haraç), kartların kayarak takası (satın al / yılan), gümüş parıltı (sahte yüz), yılanın kartı ısırması
    (çatal dil), karanlığın kartı örtmesi (gece örtüsü), diken (diken), ay sönmesi (aysız gece), gözlerin parlaması (bakış),
    kartın yazısının titreyip değişmesi (yeniden yazma), kartın yanması (yanan kart), ekranın düşüş sahnesi gibi sarsılması (düşüş).
    Darbe anında kart 1-2 px sarsılır, şeytan `reraise` animasyonunu oynar ve hileye özel repliğini söyler.
22. **Hile günlüğü:** sonuç ekranında bu elde oynanan hile tek satır ("Mammon took your K♠ for a coin").
23. **İlk oyun ipuçları:** her şeytanın ilk hilesinde bir kez açıklama.
24. Tüm efektler hız ayarına ve "atla"ya uysun. Ruh bölgesinde sayı gösterme kuralı geçerli (Haraç / Diken sayısız anlatılsın).
25. How to Play'e "Cheats" sayfası: şeytan başına hileler.

---

## BÖLÜM 4 — Denge ve kontrol

26. BalanceSimulation hilelerle çalışsın. Simülasyon oyuncusu niyete tepki versin (ör. Düşüş duyurulunca zayıf elle çekilsin).
    **Hedef:** aklanma Mammon ~%80, Belial ~%70, Lilith ~%55; Lucifer'i ilk denemede yenme ~%35.
    Hedefe **hile kurallarını değil, kötülük dolma hızını** (MaliceMax, kazanınca +1, ≤500 +1) ayarlayarak ulaş. Ulaşılamıyorsa
    nedenini DEVLOG'a yaz, kuralı kendi başına değiştirme. Raporda şeytan başına el başına hile sıklığı ve hile türlerinin dağılımı olsun.
27. Testler: her hile için Core testi (etkilenen kartlar, Dead Man's Hand bağışıklığı, engelleme kancası, mühür / ruh / Lucifer ile uyum,
    kayıt v=3 ve v=2 okuma), Belial sahte niyet, Lucifer Düşüş'ün denemede bir kez ve ≤150'de çalışması.
    Presenter testleri (niyet, efekt anlamı, ruhta sayı yok). PlayMode: her şeytanla bir hilenin uçtan uca oynanması (yığılmış deste).
28. Ekran görüntüleri: her şeytanın göstergesi ve niyeti, her hilenin darbe anı, Belial'in sahte niyetinin ortaya çıkması, Düşüş.
29. CLAUDE.md (kurallar, şeytan tablosuna hile sütunu, mimari) ve DEVLOG güncelle.

---

## Sonraya (Plan 6): oyuncu yetenekleri
Sınıflar (köylü, kral, büyücü, hırsız...) ve yetenekler bu hilelere karşılık verecek: niyeti görmek / sahteyi açığa çıkarmak,
`ICheatGuard` ile bir hileyi engellemek, kartı korumak, kötülük göstergesini boşaltmak. Bu planda yapma, sadece kancaları hazır bırak.
