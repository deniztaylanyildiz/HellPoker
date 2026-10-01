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
- **Bahis birimi** (`StakeScale`): elin başındaki cezanın 1/10'u, okunaklı adıma **aşağı** yuvarlanır:
  ceza ≥1000 → 100'ün katı, ≥500 → 50, ≥250 → 25, altı → 10 (en az 10; ceza daha azsa all-in).
  Örnek: 1000 → 100, 650 → 50, 340 → 25, 180 → 10. **Ante = 1 birim** (seçici yok; "ANTE X YEARS" + DEAL).
- **Masa tavanı:** masadaki toplam bahis elin başındaki cezanın en fazla **%50**'si (ante'den az olamaz). Tavanda artırma kilitlenir ("TABLE FULL").
  Masaya konan yıllar sayaçtan anında düşer (`YearsOffTable`).
- Her elin akışı (en fazla **5** karar):
  1. DEAL: ante masaya konur, oyuncunun ilk **2** kartı birlikte açılır (karar yok).
  2. **3., 4. ve 5.** kartta Artır / Pas / Çekil. Artırma = **1 birim**.
  3. Kart değiştirme (şeytana göre 3 ya da 4); kasa `HouseDrawStrategy` ile değiştirir. Yeni elde **1 karar**; artırma artık **2 birim**.
  4. Kasa `HouseCardsShown` kadar kart açar (Mammon 2, Belial 1, Lilith 2) → **1 karar** → kalanlar showdown'a.
  5. Kart değiştirdikten sonra oyuncu artırırsa kasa **re-raise** yapabilir (1 birim, tavan geçerli) → oyuncu **Karşıla (Call) / Çekil**.
     Karar `IHouseBettingStrategy` (`HandStrengthBettingStrategy` + şeytanın `HouseBettingStyle`'ı, zar `IRandomSource`'tan).
- **Pas** = artırmadan devam. **Çekil** = eli bırak; draw'dan önce `FoldPercentBeforeDraw`, sonra `FoldPercentAfterDraw` (yukarı yuvarlanır).
- **Son 250 yıl** (ceza ≤ 250): artırma mümkün olduğu sürece Pas yasak; **tavana ya da all-in'e** ulaşınca Pas serbest.
  Ekran "cehennem ateşi" moduna geçer.
- Masada "Win: at least −X · Lose: at least +Y" satırı (en zayıf ele göre, `LeastYearsForgiven/Added`).
- Oyun **ana menüde** açılır (New Game / Continue / How to Play / Quit). Masada MENU butonu ya da Esc menüye döner.
- **New Game → kurpiyer şeytan seçimi** (Mammon / Belial / Lilith). Her şeytanın kendi ev kuralları var (`DealerRoster`):
  | Şeytan | Kart değiştir | Kasa gösterir | Ödeme | Çekilme (önce/sonra) | Re-raise (Two Pair+ / blöf) |
  |---|---|---|---|---|---|
  | **Mammon** (Tefeci, dürüst) | 3 | 2 | standart | %50 / %100 | %70 / %5 |
  | **Belial** (Gümüş Dil, blöfçü) | 3 | 1 | yüksek (Quads ×15, SF ×25, Royal ×30) | %50 / %100 | %60 / %30 |
  | **Lilith** (Gecenin Kraliçesi, acımasız) | 4 | 2 | standart | %100 / %100 | %90 / %10 |
  - Hepsinde `LossPercent` = 100. Şeytan masada portresiyle oturur ve replik söyler (re-raise dahil, `UiText.Dealers.cs`).
- **Ödeme simetrik:** kazanırsan `toplam bahis × senin el çarpanın` yıl silinir; kaybedersen `toplam bahis × kasanın el çarpanı × LossPercent`
  yıl eklenir (yukarı yuvarlanır). Beraberlik: değişiklik yok.
- **Dead Man's Hand** (A♠ A♣ 8♠ 8♣ + herhangi bir 5. kart) **yenilmez**: Royal Flush dahil her eli yener (testlerle sabit)
  ve oyuncu kazanırsa **tüm cezayı siler**. Kasa onunla kazanırsa en yüksek çarpan sayılır.
- Standart çarpanlar: High Card/Pair ×1, Two Pair ×2, Trips ×3, Straight ×4, Flush ×5, Full House ×8, Quads ×10, Straight Flush ×15, Royal ×20.
- Tüm sayılar ayarlanabilir: `GameRules` (+ `StakeScale`), `Dealer`, `PayoutTable`, `HouseBettingStyle`; Inspector'da `HellPokerBootstrap`.

## Mimari

```
Assets/Scripts/
  Core/          HellPoker.Core.asmdef  — noEngineReferences: true (UnityEngine KULLANILAMAZ)
    Cards/         Card, Rank, Suit, Hand (değişmez), IDeck/Deck
    Randomness/    IRandomSource, IShuffler, FisherYatesShuffler
    Evaluation/    HandEvaluator + Rules/ (her el türü bir IHandRule)
    Draw/          ICardExchanger, IDiscardPolicy, IDrawStrategy (HouseDrawStrategy = kasa AI)
    Game/          IHellPokerGame/HellPokerGame (tur akışı), GameRules, PayoutTable, PunishmentLedger, HellPokerGameFactory
    Betting/       IHouseBettingStrategy, HandStrengthBettingStrategy, HouseBettingStyle (kasanın re-raise / blöf mizacı)
    Dealers/       Dealer (şeytanın ev kuralları paketi: MaxDiscards, HouseCardsShown, PayoutTable, HouseBettingStyle), DealerRoster
                   (Game/ altında ayrıca StakeScale: bahis birimi, ante, masa tavanı)
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

# Denge simülasyonu (her şeytana 2000 koşu; rapor sim.xml'deki test çıktısında)
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter HellPoker.Core.Tests.BalanceSimulation -testResults sim.xml -logFile sim.log

# Görselleri / fontları yeniden üret (py -m pip install --user pillow numpy fonttools)
py Tools/ArtGen/generate_art.py [isim ...]     # ör. belial card_back
py Tools/ArtGen/fonts.py

# Ana sahneyi yeniden oluştur
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build -logFile build.log
```

Editör açıkken: Window ▸ General ▸ Test Runner. Oynamak için menüden **Hell Poker ▸ Play** (Ctrl+Shift+P)
ya da `Assets/Scenes/HellPoker.unity` → Play.
Kısayollar: Space/Enter dağıt · çek · pas · karşıla, R artır, C karşıla, F çekil, 1-5 kart seç, Esc menü.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
