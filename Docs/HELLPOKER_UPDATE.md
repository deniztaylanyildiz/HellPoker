# Hell Poker — Güncelleme Planı (Denge + 16-bit Piksel Görseller)

Bu dosyayı baştan sona uygula. İki bölüm var. **Önce Bölüm 1'i bitir, testleri geçir, sonra Bölüm 2'ye geç.**
Böylece bir şey bozulursa hangi değişikliğin bozduğu belli olur. Her bölümün sonunda DEVLOG'a kayıt ekle.
Mevcut mimari kurallara uy (CLAUDE.md): Core Unity'siz, prefab yok, UI koddan kurulur, presenter animasyon bilmez,
tüm oyuncu metinleri UiText'te, rastgelelik IRandomSource'tan.

---

## BÖLÜM 1 — Denge düzeltmesi (oyun mantığı)

**Sorun:** BalanceSimulation'da oyunlar ortalama 3-4 elde bitiyor. Neden: tavan cezanın %50'si ve tüm bahis el
çarpanıyla katlanıyor. Masada 500 yıl varken Two Pair ile kazanmak 500 × 2 = 1000 yıl siler, oyun tek elde biter.
Kayıpta da kasanın çarpanıyla aynı hızda 2000'e varılıyor.

**Çözüm** (Python'daki birebir kopya simülasyonla doğrulandı):

1. **Ödeme: el çarpanı sadece ante'ye uygulansın, artırmalar ve re-raise'ler 1'e 1 ödensin.**
   - Kazanınca silinen = `toplam bahis + ante × (oyuncunun çarpanı − 1)` (cezayı geçemez)
   - Kaybedince eklenen = `(toplam bahis + ante × (kasanın çarpanı − 1)) × LossPercent`, yukarı yuvarla
   - Örnek: ante 100, toplam bahis 300, Full House (×8) ile kazanç → 300 + 100 × 7 = 1000 yıl silinir (2400 değil).
   - Dead Man's Hand kuralı aynı kalsın (oyuncu kazanırsa tüm ceza silinir; kasa kazanırsa en yüksek çarpan).
   - Ante'yi PayoutTable hesaplarına parametre olarak geçir. "Kazanırsan en az −X · Kaybedersen +Y" hesabı ve
     ödeme tablosu / şeytan kartı açıklamaları da yeni formüle göre güncellensin.
2. **StakeScale TableCapPercent: 50 → 30.**
3. **Lilith LossPercent: 100 → 125.** En zor şeytan o olsun. Şeytan kartı metni kurallardan türediği için kendiliğinden güncellenmeli.
4. Mevcut ve yeni testleri güncelle. Formül için ayrı testler yaz (kazanç, kayıp, LossPercent yuvarlama, DMH, tavan).
5. **BalanceSimulation'ı tekrar çalıştır.** Beklenen yaklaşık sonuç (artıran / çekilen akıllı oyuncuyla):

   | Şeytan | Aklanma | Ortalama el |
   |---|---|---|
   | Mammon | ~%79 | ~35-40 |
   | Belial | ~%70 | ~20 |
   | Lilith | ~%65 | ~40 |

   Sonuç bundan çok saparsa nedenini DEVLOG'a yaz, kendi başına kural değiştirme.
6. EditMode + PlayMode testleri geçsin, DEVLOG ve CLAUDE.md'deki kural bölümünü güncelle.

---

## BÖLÜM 2 — 16-bit piksel art görsel dönüşümü

Hedef: SNES / eski DOS oyunları havası. **Poker masası yok**, sade karanlık zemin, **animasyonlu şeytanlar**.
Oyun kuralları (Core) bu bölümde değişmeyecek; sadece Presentation, Resources ve Tools/ArtGen.

### A) Görsel üretimi (Tools/ArtGen, Python)

1. Tüm görseller Python ile **piksel art** olarak üretilsin. Eski gotik/boyalı görsel üretimi kaldırılsın ya da yeni sisteme taşınsın.
2. **Sabit palet:** en fazla 32 renk (koyu mor/siyah tonlar, cehennem kırmızısı/turuncusu, kemik beyazı, eski altın).
   Palet ArtGen'de tek bir yerde tanımlı olsun, her görsel ondan üretilsin. Kenar yumuşatma (anti-aliasing) yok.
3. **Boyutlar** (doğal piksel boyutu; ekranda tam sayı katıyla büyütülecek):
   - Şeytan animasyon karesi: 96×96
   - Kart: 32×48 (yüz + sırt; sırtta küçük pentagram / şeytan sembolü)
   - Butonlar, paneller, diyalog kutusu: 9-slice, 1-2 piksel kenarlı eski RPG menü kutusu
   - Ceza sayacı: piksel rakamlar
   - Zemin: çok koyu, hafif desenli doku (ör. koyu taş), göz yormayacak kadar sade
4. **Font:** OFL lisanslı bir piksel font (ör. "Press Start 2P" başlık, "Pixelify Sans" ya da benzeri metin).
   ♠♥♦♣ glifleri yoksa piksel olarak eklensin. Cinzel / IM Fell kalksın. Lisans dosyaları repoda kalsın.

### B) Şeytan animasyonları

5. Her şeytan için yatay sprite sheet'ler: `Assets/Resources/Art/Demons/<dealerId>/<durum>.png`
   (kareler 96×96, kare sayısı = genişlik / yükseklik).
6. Durumlar (her biri 2-6 kare, ~8 FPS):

   | Durum | Ne zaman | Görünüm |
   |---|---|---|
   | `idle` | varsayılan | nefes alma / hafif sallanma, ara ara göz kırpma (döngü) |
   | `talk` | replik yazılırken | ağız hareketi (döngü) |
   | `gloat` | oyuncu kaybedince | gülme (tek sefer, sonra idle) |
   | `angry` | oyuncu kazanınca | öfke (tek sefer, sonra idle) |
   | `reraise` | kasa re-raise yaptığında | sinsi gülümseme, gözlerde parlama (tek sefer) |
   | `final` | son 250 yılda | ateşli / yoğun hâl (idle yerine döngü) |

7. Her şeytanın kendine has detayı olsun: **Mammon** altın sikkeler / tefeci, **Belial** gümüş dil / yılan,
   **Lilith** kanatlar / gece kuşu. Siluetleri birbirinden kolay ayırt edilsin.

### C) Unity tarafı

8. **HellPokerArtImporter:** Art altındaki tüm PNG'ler piksel ayarlarıyla içe aktarılsın: Filter Mode = Point,
   sıkıştırma kapalı, mipmap kapalı, sabit Pixels Per Unit, 9-slice kenarları korunarak.
9. **Piksel düzgün ölçekleme:** referans çözünürlük 480×270, tam sayı katıyla ölçeklensin (×3, ×4),
   kenarda boşluk kalırsa siyah dolgu. Kart hareketleri tam piksel adımlarla olsun (yarım piksel kayma yok).
10. **Masa kalksın:** oval masa ve gotik salon arka planı kaldırılsın. Zemin dokusu, oyuncunun kartları altta,
    kasanın kartları üstte, şeytan sol üstte animasyonlu portre, sağda ceza sayacı ve ödeme tablosu (piksel panel).
    Konuşma balonu eski RPG diyalog kutusu (harf harf yazılma efekti korunsun).
11. **SpriteFrameAnimator** (Presentation): sprite sheet'i eşit karelere böler; FPS ve döngü / tek sefer ayarı alır.
12. **DealerView**, mevcut ruh hâli bilgisini yukarıdaki animasyon durumlarına çevirsin. Bir durumun dosyası yoksa
    `idle`'a, o da yoksa tek portreye, o da yoksa düz renge düşsün. Oyun asla eksik görsel yüzünden bozulmasın.
13. Mevcut animasyon sistemi (AnimationSequencer, kart dağıtma / çevirme) korunsun.
14. **Son 250 yıl efekti** piksel stile uyarlansın: ekran kenarında piksel alevler, kırmızı tonlama, şeytan `final` durumunda.
15. Şeytan seçim ekranında her şeytan `idle` animasyonuyla görünsün.

### D) Kontrol

16. Testleri çalıştır. Eksik görsel / eksik animasyon durumu için test yaz (fallback zinciri).
17. HellPokerScreenshots ile tüm ekranların görüntüsünü al ve kontrol et: menü, şeytan seçimi, karar anı, re-raise,
    sonuç (kazanç ve kayıp), son 250 yıl. Bulanıklık, yarım piksel, okunmayan metin olmasın.
18. Her şeytanın tüm animasyon karelerini gösteren bir önizleme sayfası üret (Tools/ArtGen çıktısı, repoya girmesin, .gitignore).
19. DEVLOG'u ve CLAUDE.md'deki "Görsel kurallar" bölümünü yeni stile göre güncelle (palet, boyutlar, dosya düzeni, fallback zinciri).
