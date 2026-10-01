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
- Kazanırsan: `toplam bahis × çarpan` yıl silinir. Kaybedersen: toplam bahis kadar yıl eklenir. Beraberlik: değişiklik yok.
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
  Presentation/  HellPoker.Presentation.asmdef — Unity katmanı (MVP)
    Abstractions/  ITableView, IHandView, ... , ITableCommands, Tone
    Views/         uGUI view'ları (UI tamamen koddan kurulur, prefab yok)
    Ui/            UiFactory, Palette, UiText (tüm oyuncu metinleri burada)
    TablePresenter (masa; ayrıca IRunSession), MainMenuPresenter (menü ↔ masa geçişi), KeyboardInput,
    HellPokerBootstrap (composition root)
  Editor/        HellPokerSceneBuilder (menü: Hell Poker ▸ Build Main Scene), HellPokerMenu (Hell Poker ▸ Play, Ctrl+Shift+P),
                 HellPokerEditorStartup (editör boş sahneyle açılırsa HellPoker sahnesini açar — batchmode son açık sahneyi sıfırlıyor).
                 UYARI: `EditorSceneManager.playModeStartScene` KULLANMA — Test Runner'ın PlayMode sahnesini de yönlendirip testleri kilitliyor.
Assets/Tests/EditMode/  NUnit testleri (Core + Presenter, fake view'larla)
Assets/Tests/PlayMode/  Sahneyi yükleyip gerçek butonlarla bir el oynayan uçtan uca test
Assets/Scenes/HellPoker.unity  — ana sahne (kamera + HellPokerBootstrap)
```

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

# Ana sahneyi yeniden oluştur
& "C:\Program Files\Unity\Hub\Editor\6000.0.25f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build -logFile build.log
```

Editör açıkken: Window ▸ General ▸ Test Runner. Oynamak için menüden **Hell Poker ▸ Play** (Ctrl+Shift+P)
ya da `Assets/Scenes/HellPoker.unity` → Play.
Kısayollar: Space/Enter dağıt · çek · pas, R artır, F çekil, 1-5 kart seç, ↑/↓ ante.

Git: GitHub Desktop kullanılıyor (`git` PATH'te yok). Remote: https://github.com/deniztaylanyildiz/HellPoker
