# Hell Poker — Güncelleme Planı 4 (Final Boss: Lucifer)

Oyuna bir final ekle: ceza 250 yılın altına düşünce oyuncu, nerede olursa olsun **Lucifer'in masasına çağrılır.**
Aklanmanın (cezayı 0'a indirmenin) tek yolu onu yenmektir. Lucifer ekranda **asla tam olarak görünmez.**

Bölümleri sırayla yap; her bölümün sonunda testleri geçir ve DEVLOG'a kayıt ekle. Mevcut mimari kurallara uy (CLAUDE.md).
Şeytan hileleri, sınıflar, ses vb. bu planın kapsamında değil.

---

## BÖLÜM 1 — Kurallar (Core)

1. **Lucifer** yeni bir `Dealer` (id `lucifer`), `DealerRoster`'da ama seçim ekranında oturulabilir bir şeytan değil (bkz. Bölüm 3).
2. **Çağrılma:** eller arasında (Betting) ceza ≤ `GameRules.LuciferGateYears` (= 250) olursa oyuncu otomatik olarak Lucifer'in masasına geçer.
   Ceza taşınır (`TakeOver`), gelinen şeytan `OriginDealerId` olarak saklanır. Masa değiştirme kurallarıyla aynı altyapı.
3. **Masadan kalkılamaz:** Lucifer'in masasındayken LEAVE TABLE / CHANGE TABLE kilitli (ruh masadayken olduğu gibi), kendine özel replik.
4. **Ev kuralları** (simülasyonla seçildi):

   | Kural | Değer |
   |---|---|
   | Kart değiştirme | 3 |
   | Kasa gösterir | **0** kart (draw sonrası tek karar, sonra doğrudan showdown) |
   | Ödeme | standart çarpanlar, kayıp **×1.25** |
   | Çekilme (önce / sonra) | %100 / %100 |
   | Re-raise (Two Pair+ / blöf) | %80 / %25 |

5. **Kendi bahis ölçeği:** Lucifer'in masasında birim cezadan hesaplanmaz, **sabit 50 yıl**; ante 1 birim, tavan **150**.
   Ceza daha azsa all-in (ante ve tavan cezayı aşamaz). Neden: son 250 yılda normal birim 10-25 yıl; o ölçekte hangi kural konursa konsun
   Lucifer zorluk eklemiyordu (simülasyonda aklanma oranları değişmedi). `Dealer`'a isteğe bağlı sabit `StakeScale` eklensin.
6. **Son 250 yıl zorunlu artırma kuralı Lucifer'in masasında geçerli değil** (masanın kendi ölçeği var; kural kalkınca denge değişmedi).
   Mühür, CHECK TO DRAW, re-raise ve el ortasında kapatma kuralları aynen geçerli.
7. **Düşüş (cast down):** Lucifer'in masasında ceza 250'nin **üstüne** çıkarsa oyuncu masadan atılır: ceza **en az 500** olur
   (`GameRules.LuciferCastDownYears` = 500; zaten üstündeyse aynen kalır) ve gelinen şeytanın masasına geri döner.
   Ceza tekrar 250'ye inerse yeniden çağrılır. Deneme sayısı (`LuciferAttempts`) tutulsun.
8. **Zafer:** Lucifer'in masasında ceza 0 → Absolved (gerçek final). Gelinen yerden bağımsız.
9. **Dead Man's Hand istisnası:** 250'nin üstündeyken Dead Man's Hand ile kazanıp cezayı tek elde silen oyuncu Lucifer'i hiç görmeden aklanır.
   Bu ayrı bir son olarak işaretlensin (Bölüm 3'te "Wild Bill" sonu).
10. Ruh mekaniği değişmez (ruh bölgesi her zaman 250'nin çok üstünde, çakışmaz).

### Beklenen denge (Python kopya simülasyon, 2000 koşu, akıllı oyuncu)

| Gelinen şeytan | Aklanma (şimdi → Lucifer'le) | Ort. el | Lucifer'i ilk denemede yenme | Ort. deneme |
|---|---|---|---|---|
| Mammon | ~%87 → ~%87 | ~30 | ~%44 | ~2.2 |
| Belial | ~%77 → ~%74 | ~16 | ~%49 | ~1.9 |
| Lilith | ~%66 → ~%63 | ~24 | ~%46 | ~2.0 |

BalanceSimulation'ı Lucifer'le çalıştır, yeni sütunları da raporla (Lucifer'e ulaşan, ilk denemede yenme, ortalama deneme, düşüş sayısı).
Sonuç çok saparsa nedenini DEVLOG'a yaz, kuralı kendi başına değiştirme. Düşüş cezası ayarlanabilir: 750'de aklanma ~%85 / %72 / %58,
1000'de ~%80 / %66 / %54 çıkıyor.

### Testler
11. Çağrılma (eşikte, eşiğin üstünde değil, sadece eller arasında), masadan kalkma kilidi, sabit birim / tavan / all-in,
    kasanın 0 kart göstermesi, düşüş (500'ün altından ve üstünden), yeniden çağrılma, deneme sayısı, zafer,
    Dead Man's Hand ile Lucifer'siz aklanma, el ortasında kapatma Lucifer masasında, kayıt / yükleme (Lucifer'deyken ve düşüşten sonra).

---

## BÖLÜM 2 — Görünmeyen Lucifer (görsel)

Lucifer çizilmez: o kadar büyük ve korkunç ki ekrana sığmaz. Sadece karanlıkta varlığı hissedilir.

12. **Portre yerine karanlık:** şeytan kutusunda zifiri karanlık; içinde sadece **iki yanan göz** (ve isteğe bağlı kartları dağıtan
    iki pençe ucu ya da kutunun kenarından taşan bir kanat ucu). Tam bir yüz, beden ya da siluet yok.
    Animasyon durumları mevcut sistemle (`Demons/lucifer/<durum>.png`, 96×96):
    - `idle`: gözler yavaşça kırpılır, ara ara karanlıkta kayar
    - `talk`: gözler konuşmayla nabız gibi parlar
    - `gloat`: gözler kısılır, karanlıkta sırıtan bir ağız çizgisi belirip kaybolur
    - `angry`: gözler büyür, kutu kızıl yanar
    - `reraise`: gözler tek bir an kör edici parlar
    - `final`: gözlerden alev damlar
13. **Salonu:** `Backgrounds/lucifer/{normal,hell}.png`: dev bir tahtın sadece alt basamakları ve karanlığa uzanan zincirler;
    tavan / taht görünmez (kadrajın dışında). Varsayılan hâli kızıl, kor parçacıkları yükselir. Orta sütun okunabilirlik için koyu.
14. **Konuşması farklı:** diyalog kutusunda isim yerine **"THE MORNING STAR"**, yazı rengi ve kutu kenarı diğerlerinden farklı (paletten).
    Her repliğinde ekran 1 px titrer.
15. **Çağrılma sahnesi:** ceza 250'ye inince ekran kararır, salon perdeyle değil yavaş kararmayla değişir, şeytanın "He has noticed you"
    tarzı veda repliği, ardından karanlıkta gözler açılır ve Lucifer konuşur. Atlanabilir (mevcut atlama sistemi).
16. **Düşüş sahnesi:** ekran yukarıdan aşağı "düşer" (tam piksel kaydırma), oyuncu eski şeytanın salonuna iner, şeytan alaycı bir replik söyler.
17. **Replikler** (`UiText.Dealers.cs`): Lucifer için karşılama, kazanç, kayıp, re-raise, mühür, düşüş, ikinci / üçüncü deneme (onu hatırlar),
    zafer. Diğer üç şeytana: oyuncu Lucifer'e çağrılırken veda repliği ve düşüşten dönünce karşılama repliği.
    Ton: sakin, kibirli, hiç bağırmaz; kısa cümleler.

---

## BÖLÜM 3 — Akış ve arayüz

18. **Seçim ekranı:** Lucifer dördüncü bir kart olarak görünsün ama **kilitli**: portre karanlık, "???" ya da "THE MORNING STAR",
    altında "Waits below 250 years". Oturulamaz; tıklanınca kısa bir replik.
19. **Masada:** ceza sayacının altında "Lucifer: attempt N" bilgisi; Lucifer masasında sayaç "250" çizgisini göstersin (düşüş sınırı).
20. **Oyun sonu ekranları:**
    - Lucifer'i yenerek aklanma: özel "THE MORNING STAR FALLS" ekranı (gözler söner), kaç denemede yenildiği.
    - Dead Man's Hand ile Lucifer'siz aklanma: "WILD BILL'S ESCAPE" ekranı.
    - Lanet ekranında, oyuncu Lucifer'e ulaşmışsa kaç kez denediği.
21. **Rekorlar:** Lucifer'e ulaşma, Lucifer'i yenme, en az denemede yenme, Wild Bill kaçışları.
22. **Kayıt:** `RunSnapshot`'a Lucifer durumu (masada mı, `OriginDealerId`, `LuciferAttempts`). Sürüm `v=2`; `v=1` kayıtlar okunmaya devam etsin
    (Lucifer alanları yokmuş gibi). Test yaz.
23. **İlk oyun ipucu:** ilk çağrılmada Lucifer kuralları tek satırla anlatsın ("Fall above 250 and you will be cast down").
24. **How to Play** sayfasına Lucifer bölümü ekle.

---

## BÖLÜM 4 — Kontrol

25. Tüm testler geçsin (yeni Core, presenter, kayıt testleri). PlayMode: 260 yılda bir el kazanıp çağrılma → Lucifer'de kayıp → düşüş →
    eski şeytan → tekrar çağrılma → zafer akışı (yığılmış desteyle).
26. Ekran görüntüleri: kilitli Lucifer kartı, çağrılma sahnesi, Lucifer masası (normal ve son anlar), re-raise, düşüş, iki yeni son ekranı.
    Lucifer'in hiçbir görüntüde tam görünmediğini kontrol et.
27. BalanceSimulation sonucunu DEVLOG'a yaz; CLAUDE.md'deki oyun kuralları, şeytan tablosu, görsel kurallar ve kayıt formatı bölümlerini güncelle.
