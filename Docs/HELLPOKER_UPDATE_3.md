# Hell Poker — Güncelleme Planı 3 (Akıcılık ve Cila)

Bugünün hedefi: yeni içerik eklemeden oyunu **akıcı, anlaşılır ve hatasız** hâle getirmek.
Sınıflar, hileler, ses, lanetli emanetler, event'ler gibi içerikler bu planın kapsamında **değil** (en altta "Sonraya" listesinde).

Bölümleri sırayla yap; her bölümün sonunda testleri geçir ve DEVLOG'a kayıt ekle.
Mevcut mimari kurallara uy (CLAUDE.md): Core Unity'siz, prefab yok, UI koddan kurulur, presenter animasyon bilmez,
tüm oyuncu metinleri UiText'te, rastgelelik IRandomSource'tan, görseller Tools/ArtGen'deki piksel sistemle ve paletle üretilir,
fontlar 8 px ızgarada. Oyun kurallarına ve denge sayılarına **dokunma**.

---

## BÖLÜM 1 — Arka plan (salon) hatası

**Sorun:** Şeytan seçim ekranında salonlar değişiyor, ama menüye geri dönünce ya da menüden oyunu yeniden başlatınca arka plan
eski şeytanda takılı kalıyor. Doğru resim için şeytanı tekrar seçmek gerekiyor.

**Beklenen davranış:**
1. Seçim ekranı açılınca vurgulanan şeytan ile arka plan **her zaman** aynı olsun. Masa değiştirirken vurgulu olan şu anki şeytan,
   yeni oyunda ilk şeytan.
2. Seçim ekranında başka şeytanlara bakıp Geri / Esc denirse menü kendi arka planına dönsün. Continue denirse masa, şu anki şeytanın salonuna dönsün.
3. Yeni oyun ya da masa değişikliğinde masa, seçilen şeytanın salonunu portreyle **aynı anda** göstersin. Mod da doğru olsun:
   önceki koşudan kalan hell / soul modu taşınmasın.

**Şüphelenilen yerler** (önce hatayı yeniden üret, sonra düzelt):
4. `SalonView.SetSalon`: `_dealerId` geçiş efekti bitmeden güncelleniyor. Efekt yarıda kalırsa resim eski kalıyor, sonraki çağrıları da
   "aynı şeytan" erken dönüşü yutuyor. Resim değişene kadar id güncellenmesin, ya da kesilen efekt sonunda mutlaka doğru resmi göstersin.
5. `TableView.Stage.SetDealer`: salon `_sequencer.Do` ile kuyruğa giriyor. Kuyrukta eski animasyon varsa gecikiyor;
   `AnimationSequencer.OnDisable` kuyruğu temizlerse tamamen kayboluyor. Portre ise anında değişiyor. Masa kurulumunda salon ve mod anında ayarlansın.
6. Mümkünse "şu anki salon" tek bir yerden yönetilsin. Seçim ekranı sadece önizleme yapsın, kapanınca geri alsın.

**Regresyon testleri** (PlayMode; arka planın gösterdiği dealerId ve mod kontrol edilsin):
7. Menü → New Game → Lilith'e tıkla → Esc → New Game: vurgu ve arka plan ilk şeytanda.
8. Mammon masası → menü → Change Table → Lilith'e bak → Geri → Continue: arka plan Mammon.
9. Mammon'la son 250 yıl / ruh bölgesindeyken → menü → New Game → Belial: arka plan Belial, mod Normal.
10. Masa değiştirme Mammon → Lilith: arka plan anında Lilith.

---

## BÖLÜM 2 — Akıcılık ve kontrol

11. **Animasyonu hızlandırma / atlama:** animasyon sürerken bir tuşa ya da tıklamaya basınca kuyruktaki animasyonlar hızlıca sonuna
    gitsin (girdiyi yok saymak yerine). Bu, oyunun "takılıyor" hissini en çok azaltan şey. Atlanan animasyonlar da son durumu doğru göstersin.
12. **Animasyon hızı ayarı:** Normal / Hızlı / Çok hızlı (tüm animasyon süreleri bir çarpanla). Varsayılan Normal.
13. **Tutarlı geri dönüş:** Esc her ekranda bir üst ekrana dönsün (kurallar → menü, seçim → menü, onay kutusu → kapat, masa → menü).
    Fare için her alt ekranda görünür bir Back butonu olsun.
14. **Girdi kaybı olmasın:** hızlı art arda basılan tuşlar çift aksiyon üretmesin, ama hiçbir geçerli basış da yutulmasın.
    Kilitli bir butona basılınca neden kilitli olduğu kısa bir mesajla söylensin (ör. "TABLE FULL", "SOUL BOUND").
15. **Ekran geçişleri:** menü, seçim ve masa arasında kısa, tutarlı bir geçiş efekti (salon değişimindeki perde efektiyle aynı dil).
    Geçiş sırasında girdi beklensin.
16. **Pencere ayarları:** tam ekran / pencere (Alt+Enter da çalışsın), piksel ölçeği her durumda tam sayı katı kalsın.
    Ayarlar PlayerPrefs'te saklansın.
17. **Ayarlar ekranı:** menüye "Settings" (animasyon hızı, tam ekran, el rehberi aç/kapa — Bölüm 3).

---

## BÖLÜM 3 — Oyuncuya yol gösterme (poker bilmeyen oyuncu için)

18. **El adı göstergesi:** oyuncunun açık kartlarının şu anki eli her zaman görünsün ("NOW: ONE PAIR"). Kart değiştirdikten sonra
    güncellensin. Showdown'da iki elin adı da büyük ve net görünsün, kazanan el vurgulansın.
19. **Kart değiştirme ipucu:** draw anında, kasanın kullandığı mantıkla (HouseDrawStrategy) "tutulması mantıklı" kartlar hafifçe
    parlasın. Sadece öneri; oyuncu istediğini seçer. Ayarlardan kapatılabilsin.
20. **El sıralaması tablosu:** menüdeki kurallar ekranında ve masada bir tuşla (ör. H) açılan küçük bir panel: el türleri, örnek kartlar,
    şeytanın çarpanları, Dead Man's Hand en üstte.
21. **İlk oyun ipuçları:** ilk koşuda, ilk kez görülen her durumda (ilk karar, ilk draw, ilk re-raise, son 250 yıl, ruh bölgesi)
    şeytanın ağzından tek satırlık açıklama. Bir kez gösterilsin, ayarlardan sıfırlanabilsin.

---

## BÖLÜM 4 — His ve geri bildirim ("juice", ses hariç)

22. **Büyük kayıpta** kısa ekran sarsıntısı (tam piksel adımlarla), ceza sayacı kırmızı yanıp sönsün.
23. **İyi elde** el adı büyük piksel yazıyla belirip kaybolsun (Two Pair ve üstü). Dead Man's Hand çıkınca özel kısa bir sahne:
    ekran kararır, dört kart tek tek parlar, şeytan `angry` animasyonu.
24. **Sayaç animasyonları:** silinen / eklenen yıllar sayıdan sayıya akarak değişsin (zaten varsa hızını gözden geçir).
25. **Buton geri bildirimi:** üzerine gelince ve basınca 1 piksellik içe basılma efekti.
26. Tüm efektler animasyon hızı ayarına uysun ve Bölüm 2'deki "atla"ya cevap versin.

---

## BÖLÜM 5 — Kayıt ve oyun sonu

27. **Otomatik kayıt:** her el bitiminde koşu kaydedilsin (ceza, şeytan, el sayısı, ruh durumu, ilk oyun ipuçları).
    Oyun kapatılıp açılınca menüde Continue çalışsın. Kayıt Core'daki durumdan türesin; seed ile destenin kaldığı yer de saklanabilir.
    Bozuk ya da eski sürüm kayıtta oyun çökmesin, kayıt yok sayılsın (test yaz).
28. **Oyun sonu ekranları:** Absolved ve Damned için ayrı, net ekranlar: kaç el oynandı, en düşük / en yüksek ceza, en iyi el,
    hangi şeytanların masasına oturuldu, ruh masaya kondu mu. "New Game" ve "Menu" butonları.
29. **Basit istatistikler:** menüde "Records": toplam koşu, aklanma sayısı, şeytan başına aklanma, en hızlı aklanma (el sayısı).

---

## BÖLÜM 6 — Kontrol turu

30. Tüm EditMode ve PlayMode testleri geçsin. Yeni özellikler için test yaz (animasyon atlama, kayıt/yükleme, ayarlar, el adı göstergesi).
31. HellPokerScreenshots ile tüm ekranların görüntüsünü al: menü, ayarlar, kurallar + el tablosu, şeytan seçimi, karar anı (el adı göstergesiyle),
    draw ipucu, re-raise, büyük kayıp, iyi el yazısı, ruh bölgesi, oyun sonu ekranları, istatistikler. Okunmayan metin, taşan yazı,
    yarım piksel olmasın.
32. **Uçtan uca PlayMode testi:** yeni oyun → birkaç el → masa değiştir → menüye çık → Continue → oyunu "kapat/aç" (kaydı yeniden yükle) → devam.
    Hiçbir adımda hata logu olmasın.
33. Editor.log / Player.log'da uyarı ve hata kalmasın (eksik sprite, font glifi, null reference).
34. DEVLOG ve CLAUDE.md'yi güncelle (yeni ekranlar, ayarlar, kayıt formatı, kısayollar).

---

## Sonraya (bu planda YAPMA, sadece not)

- Günahkâr sınıfları (köylü, kral, büyücü, hırsız…), pasif + aktif yetenekler
- Şeytan hileleri
- Ses ve müzik (chiptune, kart / sikke sesleri)
- Lanetli emanetler (her 250 yılda 3'ten 1 seçim; Dead Man's Hand'e yol)
- Eller arası event'ler (ruh bölgesinde daha karanlık olanlar)
- Final şeytanı (Lucifer), şeytanların oyuncuyu hatırlaması, açılabilir içerik, başarımlar, günlük seed'li koşu
