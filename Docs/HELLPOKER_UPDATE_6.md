# Hell Poker — Güncelleme Planı 6 (0.1.0 testinden sonra: hile düzeltmeleri, akıcılık, test build'i 0.1.1)

Son build'den (0.1.0) sonra test ederken bulduklarım. Üç bölüm var; **SIRAYLA** yap, her bölümün sonunda testleri geçir
(derleme hatası kontrolü dahil) ve DEVLOG'a kayıt ekle. Mevcut mimari kurallara uy (CLAUDE.md). Daha önce yapılmış bir madde
varsa atla ve DEVLOG'da belirt. Gereken görselleri Tools/ArtGen piksel sistemiyle sen üret.

---

## BÖLÜM A — Şeytan hileleri düzeltmeleri (Core + testler)

Test ederken bazı hilelerin oyunda işe yaramadığını gördüm. Ayrıca yeni bir kural: bazı hileler **geri tepebilir** (oyuncuya yarayabilir);
bu bilerek bırakılan bir mekanik, ama sadece şeytanın karakterine uyan hilelerde.

1. **Lilith – Gece Örtüsü:** şu an Timing BeforeDraw; oyuncu beş kartı tek tek görüp karar verdikten sonra kapanıyor, yani kart
   zaten görülmüş oluyor ("kartlar geç kapanıyor"). Düzeltme: Timing AfterDeal olsun, hedef sadece henüz açılmamış kartlar
   (açılış kartlarından sonraki 3-5. kartlar). O kart sırası gelince yüzü hiç görünmeden, sırtı dönük ve örtü işaretiyle açılsın;
   showdown'a kadar öyle kalsın, draw'da kör seçilebilsin. Niyet açıklaması: "One of your cards will come to you in the dark".

2. **Belial – Çatal Dil (yalancının dili kayabilir):** %80 ihtimalle oyuncunun elini bozacak şekilde seçilsin
   (öncelik: flush'ı / flush çekişini bozan, yoksa kategoriyi düşüren, yoksa nötr). %20 ihtimalle "dili kayar": rastgele bir renk
   değişikliği olur ve bu oyuncunun elini iyileştirebilir (flush dahil). Geri tepme oranı Dealer'dan ayarlanabilir olsun (`BackfirePercent`).

3. **Lucifer – Bakış:** şu an re-raise = kesin kayıp, re-raise yok = kesin kazanç; dikkatli oyuncuya kesin bilgi veriyor.
   Yeni hâli: oyuncu kaybedecekse %100 re-raise, kazanacaksa %50 re-raise (blöf). Niyet açıklaması: "He sees your hand. His raises will hurt."

4. **Mammon – Rehin:** zincir en yüksek karta değil, oyuncunun atmak isteyeceği bir karta vurulsun: `SuggestedDiscards`'ın atacağı
   kartlardan en yükseği; atılacak kart yoksa (hazır el) en düşük kart. Niyet açıklaması buna göre.

5. **Lilith – Diken:** diken atılması mantıklı kartlardan birine (`SuggestedDiscards`) batsın; yoksa rastgele.

6. **Lucifer – Yanan Kart:** hedef, oyuncunun en iyi kombinasyonunun (çift, üçlü vb.) parçası olan en yüksek kart; kombinasyon yoksa
   en yüksek kart. Yerine gelen kart rastgele kalır (geri tepebilir, bkz. madde 9).

7. **Lilith – Aysız Gece ve Belial – Yılan Takası:** gelen / takas edilen kartın animasyon sırasında bir an bile yüzü görünmesin.

8. **Gizlilik testi:** oyuncudan gizlenen bir kartın yüzü showdown'dan önce hiçbir yoldan sızmasın: view'a giden CardSlot'lar
   (dağıtım, açılma, değiştirme, hile vuruşu, mühürlü otomatik açılış), "NOW: ..." el adı, draw ipucu çerçeveleri, mesaj ve replikler.
   Fake view'la her Show çağrısını kaydedip doğrulayan test yaz (Gece Örtüsü, Aysız Gece, Yılan Takası).

9. **Geri tepme (backfire) kuralı ve testleri:**
   - Oyuncunun elini iyileştirebilecek hileler: Belial'in Çatal Dil'i (%20 dil kayması), Lucifer'in Yanan Kart'ı (rastgele yeni kart)
     ve Düşüş'ü (yeniden dağıtım).
   - Diğer tüm hileler (Mammon'un hepsi, Lilith'in hepsi, Belial'in Sahte Yüz ve Yılan Takası, Lucifer'in Bakış ve Yeniden Yazma'sı)
     **asla** oyuncunun elini iyileştirmesin; bunun testi olsun.
   - Bir hile oyuncunun elini iyileştirdiğinde (el kategorisi ya da aynı kategoride güç arttığında) bu görünür bir an olsun:
     kartın üstünde "BACKFIRE" yazısı, şeytanın angry animasyonu ve şeytana özel bir geri tepme repliği
     (`UiText.Dealers.cs`, her şeytana 2-3 replik; Belial için "My tongue... slipped." gibi). Sonuç ekranındaki hile günlüğü de bunu söylesin.
   - Geri tepmeler kayda ve rekorlara geçsin ("Backfires seen").
   - Her hile için "en az bir durumda gerçekten etkili" bir test olsun; geri tepebilen hileler için geri tepme testi de olsun.

10. **PlayMode:** yığılmış destelerle Lilith (örtü + aysız gece), Belial (çatal dil flush bozma ve dil kayması), Lucifer (bakış) uçtan uca;
    gizli kartlar her karede sırtı dönük görünsün.

11. **Denge:** BalanceSimulation'ı tekrar çalıştır; simülasyon oyuncusu Bakış'ta re-raise'i kesin bilgi saymasın. Raporda geri tepme
    sıklığı da görünsün. Hedefler aynı: Mammon ~%80, Belial ~%70, Lilith ~%55, Lucifer ilk denemede ~%35. Sapma olursa sadece gösterge
    (`MaliceMax`) ile ayarla, nedenini DEVLOG'a yaz.

12. CLAUDE.md hile açıklamaları (geri tepme kuralı dahil) ve How to Play CHEATS sayfası güncellensin.

---

## BÖLÜM B — Animasyon akıcılığı ve okunabilirlik

Ana menü ve salonlar düşük FPS'li görünüyor; Belial salonundaki yılanlı sütunlar özellikle kesik kesik dönüyor. Bazı yazılar bu
hareketli bölgelere denk gelince okunmuyor.

Neden: menü 4 kare / 4 FPS, salonlar 3 kare / 4 FPS tam ekran şerit (2048 px doku sınırı yüzünden kare sayısı az). Hareket büyük
olduğu için kareler arası zıplama göze batıyor. Bazı hareketler döngüde düzgün kapanmıyor (ör. Belial sütunlarında
`sin(y*0.12 + frame*0.8)` 3 karede başa dönmüyor, son kareden ilk kareye geçişte sıçrama oluyor).

13. **Katmanlı arka plan:** her salonun ve menünün zemini TEK karelik sabit resim olsun. Hareket eden parçalar (Belial'in sütunları
    ve perdeleri, Mammon'un sikke parıltıları, Lilith'in kuşları / sarmaşıkları, Lucifer'in zincirleri ve gözleri, menüdeki gözler ve
    ateş denizi dalgaları) ayrı küçük sprite şeritleri olarak üstüne konsun: 8-12 kare, 8-12 FPS. Hell / soul varyantları da bu yapıya uysun.
14. **Döngüler kusursuz kapansın:** periyodik hareketlerde faz = 2π · kare / kare_sayısı. ArtGen'de kontrol ekle: ilk ve son kare
    arasındaki fark ardışık kareler arasındaki farktan büyük olmasın.
15. **Parçacıklar kodla:** kor / kıvılcım gibi parçacıklar sprite karesi yerine basit bir piksel parçacık sistemiyle hareket etsin
    (her karede tam piksel adımlarla, `AnimationClock` hız ayarına uyarak).
16. **Hareket genliği küçük:** kare başına en fazla 1-2 piksel kayma; yavaş ama sürekli hareket.
17. **Okunabilirlik:** oyun ekranındaki tüm yazılar (mesajlar, el adı, büyük el yazısı, sonuç, ipuçları, niyet şeridi, hile günlüğü,
    BACKFIRE yazısı) ya bir panel / koyu şerit üzerinde dursun ya da 1 piksel siyah dış çizgi (outline) alsın. Hareketli katmanlar
    yazıların durduğu bölgelere girmesin; girmesi gerekiyorsa o bölge koyulaştırılsın. Menüde de aynı kontrol.
18. **FPS ölçümü:** development build'de F3 ile açılıp kapanan küçük bir FPS göstergesi. Menüde ve her salonda ölç; hedef sabit 60 FPS.
19. Ekran görüntüleri ve `preview.py`: yeni katmanlı salonları ve menüyü kontrol et; preview sayfası animasyonları gerçek hızında oynatsın.
20. CLAUDE.md görsel kurallarını güncelle (katman yapısı, döngü kuralı, yazı okunabilirliği kuralı).

---

## BÖLÜM C — Arkadaşlarım için test build'i (0.1.1)

21. **Temizlik:** repoda kök dizinde test çıktısı (ör. `cshots.xml`) kalmasın; .gitignore kökteki tüm test xml'lerini kapsasın (`/*.xml`),
    projenin ihtiyaç duyduğu bir xml varsa hariç tutulsun.
22. **Player Settings:** Company Name "Deniz", Product Name "Hell Poker", Version "0.1.1" (DefaultCompany kalmasın; log klasörü buna göre değişir).
23. Sürüm numarası ana menünün alt köşesinde küçük harflerle görünsün (`Application.version`).
24. Build script'i Windows x64 build'in ardından `Builds/HellPoker-0.1.1-win64.zip` oluştursun (klasörün tamamı: .exe + _Data + dll'ler
    + MonoBleedingEdge). Builds/ ve zip .gitignore'da.
25. Zip'e **OKUBENI.txt** (Türkçe): nasıl açılır; Windows "bilgisayarınızı korudu" uyarısında "Ek bilgi → Yine de çalıştır";
    temel kontroller (Space, R, F, D, C, 1-5, H, Esc, Alt+Enter); hata olursa Player.log'un tam yolu (yeni şirket / ürün adıyla)
    ve bana göndermeleri.
26. Zip'e **GERI_BILDIRIM.txt**: arkadaşlarımın doldurabileceği 8-10 açık uçlu soru, poker bilmeyen biri için de anlaşılır
    (ilk izlenim, kurallar anlaşıldı mı, tempo, en zor şeytan, hileler adil mi / anlaşılır mı, Belial'in sahte kartına kandın mı,
    bir hile sana yaradı mı (backfire), en sevdiğin / en sıkıcı an, hata gördün mü).
27. Belial'in göstergesine Bölüm A'daki simülasyon dışında dokunma; son kararı test geri bildiriminden sonra vereceğim.
28. Yeni build al, açılışta ve menü / salon geçişlerinde Player.log'da hata olmadığını kontrol et, DEVLOG'a yaz.
    Bitince bana: test sayıları, simülasyon tablosu (geri tepme sıklığı dahil), FPS ölçümleri ve zip'in yolu.
