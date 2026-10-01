# Hell Poker — Claude için proje rehberi

Bu dosya Claude Code tarafından her oturumun başında otomatik okunur. Projenin kalıcı hafızasıdır;
konuşma geçmişi ise `Docs/DEVLOG.md` dosyasındadır. **Yeni bir oturuma başlarken önce `Docs/DEVLOG.md`'yi oku.**

## Oturum kuralları (Claude için)

- Kullanıcıyla **Türkçe** konuş. Kod, yorumlar, isimler ve oyun içi metinler **İngilizce**.
- Her anlamlı iş bloğunun sonunda ve oturum biterken `Docs/DEVLOG.md`'ye yeni bir kayıt ekle:
  tarih, kullanıcının isteği (kendi sözleriyle kısa özet), yapılanlar, alınan kararlar ve nedenleri, açık sorular, sıradaki adımlar.
- Mimari bir karar değiştiğinde bu dosyadaki ilgili bölümü de güncelle.
- Kod **SOLID** prensiplerine uygun olmalı (aşağıdaki mimari kurallara bak). Yeni kod için test yaz.

## Oyun

Unity 6 (6000.0.25f1), 2D URP. Tek oyunculu **5 Card Draw** poker, oyuncu **kasaya (House)** karşı oynar.

- Oyuncu **1000 yıl** cehennem cezasıyla başlar; amaç cezayı **0**'a indirmek (Absolved).
- Ceza **2000 yıla** ulaşırsa sonsuz lanet: oyun biter (Damned).
- Her elin akışı:
  1. **Ante** seçilir (yıl olarak, 10/25/50/100/200).
  2. Kartlar kapalı dağıtılır; oyuncunun kartları **tek tek açılır**, her kartta **Artır / Pas / Çekil**.
  3. Oyuncu en fazla **3** kart değiştirir; kasa kendi stratejisiyle (`HouseDrawStrategy`) değiştirir.
  4. Kasanın kartları tek tek açılır; ilk **3** kartta yine Artır / Pas / Çekil, son 2 kart doğrudan showdown'a açılır.
- **Artır** = ante kadar daha ekle. **Pas** = artırmadan devam. **Çekil (Fold)** = eli bırak, toplam bahsin yarısı (yukarı yuvarlanır) cezaya eklenir.
- **Bahis sınırı:** toplam bahis asla mevcut cezayı geçemez. Masaya konan yıllar sayaçtan anında düşer
  (`YearsOffTable`); kalan ante'den azsa artırma "ALL IN +X" olur, her şey masadaysa artırma kilitlenir.
  Ante seçenekleri de cezayı aşamaz (en küçük ante her zaman serbesttir, gerekirse all-in olur).
- **Son 250 yıl** (ceza ≤ 250): all-in olana kadar Pas yasak, her kararda artırmak (ya da çekilmek) zorunlu.
  Ekran "cehennem ateşi" moduna geçer.
- Oyun **ana menüde** açılır (New Game / Continue / How to Play / Quit). Masada MENU butonu ya da Esc menüye döner.
- **New Game → kurpiyer şeytan seçimi** (Mammon / Belial / Lilith). Her şeytanın kendi ev kuralları var (`DealerRoster`):
  - **Mammon** (Tefeci): klasik kurallar — 3 kart değiştir, kasanın 3 kartında karar, standart ödeme.
  - **Belial** (Gümüş Dil): yüksek çarpanlar, kayıp bahsin **1.5 katı**, kasanın sadece **1** kartında karar.
  - **Lilith** (Gecenin Kraliçesi): **4** kart değiştir, ama çekilmek bahsin **tamamına** mal olur.
  - Şeytan masada portresiyle oturur ve el sonuçlarına göre replik söyler (`UiText.Dealers.cs`).
- Kazanırsan: `toplam bahis × çarpan` yıl silinir. Kaybedersen: toplam bahis kadar yıl eklenir (şeytana göre `LossPercent`). Beraberlik: değişiklik yok.
- **Dead Man's Hand** (A♠ A♣ 8♠ 8♣ + herhangi bir 5. kart) en güçlü eldir (Royal Flush'tan da güçlü) ve kazanırsa **tüm cezayı siler**.
- Çarpanlar: High Card/Pair ×1, Two Pair ×2, Trips ×3, Straight ×4, Flush ×5, Full House ×8, Quads ×25, Straight Flush ×50, Royal ×100.

## Mimari

```
Assets/Scripts/
  Core/          HellPoker.Core.asmdef  — noEngineReferences: true (UnityEngine KULLANILAMAZ)
    Cards/         Card, Rank, Suit, Hand (değişmez), IDeck/Deck
    Randomness/    IRandomSource, IShuffler, FisherYatesShuffler
    Evaluation/    HandEvaluator + Rules/ (her el türü bir IHandRule)
    Draw/          ICardExchanger, IDiscardPolicy, IDrawStrategy (HouseDrawStrategy = kasa AI)
    Game/          IHellPokerGame/HellPokerGame (tur akışı), GameRules, PayoutTable, PunishmentLedger, HellPokerGameFactory
    Dealers/       Dealer (şeytanın ev kuralları paketi: MaxDiscards, HouseRevealDecisions, PayoutTable), DealerRoster
  Presentation/  HellPoker.Presentation.asmdef — Unity katmanı (MVP)
    Abstractions/  ITableView, IHandView, IDealerView, IDealerSelectView, DealerCard, ... , ITableCommands, Tone
    Views/         uGUI view'ları (UI tamamen koddan kurulur, prefab yok): TableView, DealerView, DealerSelectView, CardView...
    Ui/            UiFactory, UiArt (Resources'tan sprite/font yükler), Palette, UiText + UiText.Dealers (tüm oyuncu metinleri)
    TablePresenter (masa; IRunSession; her koşuda seçilen şeytan için oyunu Func<Dealer, IHellPokerGame> ile kurar),
    MainMenuPresenter (menü → şeytan seçimi → masa), DealerCards (şeytan kurallarından özellik metni üretir), KeyboardInput,
    HellPokerBootstrap (composition root)
  Editor/        HellPokerSceneBuilder (menü: Hell Poker ▸ Build Main Scene), HellPokerMenu (Hell Poker ▸ Play, Ctrl+Shift+P),
                 HellPokerArtImporter (Resources/Art altındaki PNG'leri UI sprite + 9-slice kenarlarıyla içe aktarır),
                 HellPokerEditorStartup (editör boş sahneyle açılırsa HellPoker sahnesini açar — batchmode son açık sahneyi sıfırlıyor).
                 UYARI: `EditorSceneManager.playModeStartScene` KULLANMA — Test Runner'ın PlayMode sahnesini de yönlendirip testleri kilitliyor.
Assets/Tests/EditMode/  NUnit testleri (Core + Presenter, fake view'larla)
Assets/Tests/PlayMode/  Sahneyi yükleyip gerçek butonlarla bir el oynayan uçtan uca test
                        + HellPokerScreenshots ([Explicit]: tüm ekranların 1920×1080 görüntüsünü alır)
Assets/Scenes/HellPoker.unity  — ana sahne (kamera + HellPokerBootstrap)
Assets/Resources/Art/   Üretilmiş görseller: Demons/<dealerId>.png portreler, Ui/ (masa, arka plan, çerçeve, buton, fiş, kart, logo...)
Assets/Resources/Fonts/ HellPokerDisplay (Cinzel), HellPokerSerif(+Italic) (IM Fell) — OFL, ♠♥♦♣↑↓ glifleri eklenmiş
Tools/ArtGen/           Görselleri üreten Python (Pillow+numpy) ve font script'leri — görsel değişince buradan yeniden üret
```

### Görsel kurallar

- Stil: **gotik cehennem kumarhanesi** — koyu kadife, altın süsleme, Cinzel başlıklar, IM Fell metin.
- `UiFactory.CreateText` stili fonta çevirir: `Bold` = Display (Cinzel), `Italic` = serif italik, diğerleri serif.
- Görseller **Python ile üretilir** (`Tools/ArtGen`), elle düzenlenmez; değişiklik script'te yapılıp yeniden üretilir.
  Gerçek çizim gelirse aynı isimle `Assets/Resources/Art/...` altına konması yeterli.
- Yeni şeytan: `DealerRoster`'a `Dealer`, `UiText.Dealers.cs`'e metinler, `Tools/ArtGen/demons.py`'ye portre (dosya adı = id).
- Proje **lineer renk uzayında**: UI'da düşük alfa bile ekranda güçlü görünür (ör. %13 turuncu ≈ ekranın yarısı turuncu).
- Hileler/eventler (ileride): `Dealer` paketine yeni parçalar olarak eklenecek; presenter şeytanı yalnızca `Dealer` üzerinden tanır.

### Mimari kurallar

- **Oyun kuralları sadece Core'da.** Core Unity'ye bağımlı olamaz (asmdef bunu derleyici seviyesinde zorlar).
- **Yeni el türü / özel el** → yeni bir `IHandRule` yaz, `StandardHandRules`'a ekle. `HandEvaluator`'a dokunma (OCP).
- **Yeni kural varyasyonu** (ör. 4 kart değiştirme, farklı ödeme) → yeni `IDiscardPolicy` / `IPayoutTable` implementasyonu.
- **Somut sınıflar sadece composition root'larda seçilir:** `HellPokerGameFactory` (Core) ve `HellPokerBootstrap` (Unity).
- **Presenter sadece arayüzlere bağlıdır** (`IHellPokerGame`, `ITableView`), renk/stil bilmez; anlamsal `Tone` gönderir, rengi view seçer.
- **Presenter durum tarif eder, animasyon bilmez:** her komuttan sonra masanın olması gereken halini (`CardSlot[]` vb.) gönderir.
  View'lar farkı bulup animasyonla gösterir. Tüm view güncellemeleri tek bir `AnimationSequencer` kuyruğundan geçer
  (sonuç mesajı kartlar açılmadan görünmez). Animasyon sürerken `ITableView.IsBusy` true olur ve presenter girdiyi yok sayar.
- uGUI butonları `UiFactory.MakeClickOnly` ile oluşturulmalı (yoksa Space/Enter son tıklanan butonu tekrar tetikler).
- **Girdi kaynakları** (`KeyboardInput` vb.) sadece `ITableCommands`'a konuşur.
- Rastgelelik her zaman `IRandomSource` üzerinden; testlerde seed veya `TestDecks.Stacked(...)` kullan.
- Oyuncuya görünen tüm metinler `UiText` içinde (ileride yerelleştirme için).

## Komutlar

Unity editörü **kapalıyken** (proje açıksa batchmode kilitlenir):

```powershell
# Testler (sonuç: results.xml). Sahne/UI değişikliğinde PlayMode'u da çalıştır.
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile unity.log
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults play.xml -logFile play.log

# Ekran görüntüleri (Screenshots/ klasörüne ya da $env:HELLPOKER_SHOTS'a)
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter HellPoker.PlayMode.Tests.HellPokerScreenshots -testResults shots.xml -logFile shots.log

# Görselleri / fontları yeniden üret (py -m pip install --user pillow numpy fonttools)
py Tools/ArtGen/generate_art.py [isim ...]     # ör. belial card_back
py Tools/ArtGen/fonts.py

# Ana sahneyi yeniden oluştur
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build -logFile build.log
```

Editör açıkken: Window ▸ General ▸ Test Runner. Oynamak için menüden **Hell Poker ▸ Play** (Ctrl+Shift+P)
ya da `Assets/Scenes/HellPoker.unity` → Play.
Kısayollar: Space/Enter dağıt · çek · pas, R artır, F çekil, 1-5 kart seç, ↑/↓ ante.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
