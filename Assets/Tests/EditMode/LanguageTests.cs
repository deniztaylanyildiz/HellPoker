using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;
using UnityEngine;

/// <summary>Every EditMode test starts in English (the language is static: a test that switches it must not leak).</summary>
[SetUpFixture]
public class EnglishByDefault
{
    [OneTimeSetUp]
    public void SpeakEnglish() => Lang.Set(Language.English);
}

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// English and Turkish: the setting (saved, read back, the first launch), every string in both languages (no empty
    /// Turkish, the same {n} in every format), and the table changing language mid-hand without touching the game.
    /// </summary>
    public class LanguageTests
    {
        [TearDown]
        public void BackToEnglish() => Lang.Set(Language.English);

        // ------------------------------------------------------------------ the setting

        [Test]
        public void TheLanguage_IsSaved_AndReadBack()
        {
            var store = new MemoryStore();
            var settings = new GameSettings(store);
            Assert.AreEqual(Language.English, settings.Language);

            settings.CycleLanguage();

            Assert.AreEqual(Language.Turkish, settings.Language);
            Assert.AreEqual("Turkish", store.GetString("settings.language", null), "Saved by name.");
            Assert.AreEqual(Language.Turkish, Lang.Current);
            Assert.AreEqual(Language.Turkish, new GameSettings(store).Language, "The next launch speaks it.");
        }

        [TestCase("Klingon")]
        [TestCase("")]
        [TestCase("7")]
        public void AGarbledLanguage_IsEnglish(string saved)
        {
            var store = new MemoryStore();
            store.SetString("settings.language", saved);

            Assert.AreEqual(Language.English, new GameSettings(store, Language.Turkish).Language);
        }

        [Test]
        public void AFirstLaunch_SpeaksTheSystemsLanguage_ThenTheSavedOne()
        {
            Assert.AreEqual(Language.Turkish, new GameSettings(new MemoryStore(), Language.Turkish).Language, "Nothing saved: the system's.");

            var store = new MemoryStore();
            store.SetString("settings.language", "English");
            Assert.AreEqual(Language.English, new GameSettings(store, Language.Turkish).Language, "The player's choice wins.");
        }

        [Test]
        public void TheFirstLanguage_TurkishSystemsGetTurkish_BatchRunsAlwaysEnglish()
        {
            Assert.AreEqual(Language.Turkish, HellPokerBootstrap.FirstLanguageFor(false, SystemLanguage.Turkish));
            Assert.AreEqual(Language.English, HellPokerBootstrap.FirstLanguageFor(false, SystemLanguage.German));
            Assert.AreEqual(Language.English, HellPokerBootstrap.FirstLanguageFor(false, SystemLanguage.English));
            Assert.AreEqual(Language.English, HellPokerBootstrap.FirstLanguageFor(true, SystemLanguage.Turkish), "Tests speak English.");
        }

        [Test]
        public void ALanguageButton_CyclesAndSaves()
        {
            var store = new MemoryStore();
            var settings = new GameSettings(store);
            var settingsView = new MainMenuPresenterTests.FakeSettingsView();
            var menu = new MainMenuPresenterTests.FakeMenuView();
            using var presenter = new SettingsPresenter(settings, settingsView, new NoDisplay(), menu);
            Assert.AreEqual("ENGLISH", settingsView.Language);

            menu.PressLanguage();

            Assert.AreEqual(Language.Turkish, settings.Language);
            Assert.AreEqual("Turkish", store.GetString("settings.language", null));
            Assert.AreEqual("TÜRKÇE", settingsView.Language);
            Assert.AreEqual("HIZLI", UiText.SpeedName(AnimationSpeed.Fast));
            Assert.AreEqual("NORMAL", settingsView.Speed);

            settingsView.PressLanguage();
            Assert.AreEqual(Language.English, settings.Language, "Round and round.");
        }

        private sealed class NoDisplay : IDisplayMode
        {
            public void SetFullscreen(bool fullscreen) { }
        }

        // ------------------------------------------------------------------ the words

        [Test]
        public void InTurkish_TheTableSpeaksTurkish()
        {
            Lang.Set(Language.Turkish);

            Assert.AreEqual("ÇEKİL", UiText.Fold);
            Assert.AreEqual("ARTIR +100", string.Format(UiText.RaiseFormat, 100));
            Assert.AreEqual("GÖR +50", string.Format(UiText.CallFormat, 50));
            Assert.AreEqual("PAS", UiText.Pass);
            Assert.AreEqual("KART DEĞİŞ 2", string.Format(UiText.DrawFormat, 2));
            Assert.AreEqual("Kent", UiText.CategoryName(HandCategory.Straight));
            Assert.AreEqual("Renk", UiText.CategoryName(HandCategory.Flush));
            Assert.AreEqual("Full", UiText.CategoryName(HandCategory.FullHouse));
            Assert.AreEqual("Kare", UiText.CategoryName(HandCategory.FourOfAKind));
            Assert.AreEqual("Floş Royal", UiText.CategoryName(HandCategory.RoyalFlush));
            Assert.AreEqual("Ölü Adamın Eli", UiText.CategoryName(HandCategory.DeadMansHand));
            Assert.AreEqual("ÖLÜ ADAMIN ELİ", UiText.CategoryNameUpper(HandCategory.DeadMansHand), "Turkish capitals: i → İ.");
            Assert.AreEqual("SABAH YILDIZI", UiText.Dealer(DealerRoster.LuciferId).Name);

            Lang.Set(Language.English);
            Assert.AreEqual("FOLD", UiText.Fold);
            Assert.AreEqual("DEAD MAN'S HAND", UiText.CategoryNameUpper(HandCategory.DeadMansHand));
        }

        [Test]
        public void TheKeys_AreNeverTranslated()
        {
            Lang.Set(Language.Turkish);

            Assert.AreEqual("tip.decision", UiText.TipFirstDecision);
            Assert.AreEqual("tip.cheat.mammon", UiText.TipCheatFor(DealerRoster.MammonId));
            Assert.AreEqual("MAMMON", UiText.Dealer(DealerRoster.MammonId).Name);
        }

        /// <summary>Every player-facing string property of UiText, as a name and its English and Turkish words.</summary>
        private static IEnumerable<(string name, string english, string turkish)> EveryString()
        {
            foreach (PropertyInfo property in typeof(UiText).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(p => p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0))
            {
                Lang.Set(Language.English);
                string english = (string)property.GetValue(null);
                Lang.Set(Language.Turkish);
                string turkish = (string)property.GetValue(null);
                yield return (property.Name, english, turkish);
            }

            // ... and the words picked by a key.
            var calls = new List<(string name, Func<string> words)>();
            foreach (HandCategory category in Enum.GetValues(typeof(HandCategory)))
            {
                calls.Add(("CategoryName." + category, () => UiText.CategoryName(category)));
                calls.Add(("HandExample." + category, () => UiText.HandExample(category)));
            }
            foreach (string id in AllCheatIds())
            {
                calls.Add(("CheatName." + id, () => UiText.CheatName(id)));
                calls.Add(("CheatDescription." + id, () => UiText.CheatDescription(id)));
                calls.Add(("CheatLine." + id, () => UiText.CheatLine(id)));
            }
            foreach (AnimationSpeed speed in Enum.GetValues(typeof(AnimationSpeed)))
                calls.Add(("SpeedName." + speed, () => UiText.SpeedName(speed)));
            foreach (int share in new[] { 0, 25, 50, 100, 150, 200 })
            {
                calls.Add(("StakeShare." + share, () => UiText.StakeShare(share)));
                calls.Add(("ShareShort." + share, () => UiText.ShareShort(share)));
            }
            foreach (string tip in new[] { UiText.TipFirstDecision, UiText.TipFirstDraw, UiText.TipFirstReRaise, UiText.TipFinalStretch,
                         UiText.TipSoul, UiText.TipLucifer }.Concat(DealerRoster.All.Select(d => UiText.TipCheatFor(d.Id))))
                calls.Add(("TipText." + tip, () => UiText.TipText(tip, 250)));
            calls.Add(("CheatsPage", UiText.CheatsPage));

            foreach (var (name, words) in calls)
            {
                Lang.Set(Language.English);
                string english = words();
                Lang.Set(Language.Turkish);
                string turkish = words();
                yield return (name, english, turkish);
            }
        }

        private static IEnumerable<string> AllCheatIds() =>
            typeof(CheatIds).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral).Select(f => (string)f.GetValue(null));

        private static string Placeholders(string format) =>
            string.Join(",", Regex.Matches(format ?? "", @"\{(\d+)(?:[,:][^}]*)?\}").Cast<Match>().Select(m => m.Groups[1].Value)
                .Distinct().OrderBy(s => s));

        [Test]
        public void EveryFormat_HasTheSamePlaceholders_InBothLanguages()
        {
            var wrong = EveryString().Where(s => Placeholders(s.english) != Placeholders(s.turkish))
                .Select(s => $"{s.name}: [{Placeholders(s.english)}] vs [{Placeholders(s.turkish)}]").ToList();

            Assert.IsEmpty(wrong, string.Join("\n", wrong));
            Assert.Greater(EveryString().Count(), 250, "Every UiText string was found.");
        }

        [Test]
        public void NoTurkishText_IsEmpty()
        {
            var empty = EveryString().Where(s => !string.IsNullOrEmpty(s.english) && string.IsNullOrWhiteSpace(s.turkish)).Select(s => s.name).ToList();

            Assert.IsEmpty(empty, string.Join(", ", empty));
        }

        [Test]
        public void EveryDemon_SpeaksTurkish_EverywhereTheySpeakEnglish()
        {
            var missing = new List<string>();
            foreach (string id in DealerRoster.All.Select(d => d.Id).Append(DealerRoster.LuciferId).Append("nobody"))
            {
                Lang.Set(Language.English);
                DealerText english = UiText.Dealer(id);
                Lang.Set(Language.Turkish);
                DealerText turkish = UiText.Dealer(id);
                Assert.AreNotSame(english, turkish, id);

                foreach (FieldInfo field in typeof(DealerText).GetFields())
                {
                    object en = field.GetValue(english), tr = field.GetValue(turkish);
                    if (en is string enLine && enLine.Length > 0 && string.IsNullOrWhiteSpace(tr as string))
                        missing.Add($"{id}.{field.Name}");
                    if (en is string[] enLines && enLines.Any(l => l.Length > 0))
                    {
                        var trLines = tr as string[];
                        if (trLines == null || trLines.Length == 0 || trLines.Any(string.IsNullOrWhiteSpace))
                            missing.Add($"{id}.{field.Name}[]");
                    }
                }
            }

            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        // ------------------------------------------------------------------ the demon's (Turkish suffixes, by hand)

        private static IEnumerable<string> EveryDemonId() => DealerRoster.All.Select(d => d.Id).Append(DealerRoster.LuciferId).Append("nobody");

        [Test]
        public void EveryDemon_HasTheirTurkishPossessive_WrittenByHand()
        {
            Lang.Set(Language.Turkish);
            foreach (string id in EveryDemonId())
            {
                DealerText demon = UiText.Dealer(id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(demon.Genitive), id + " Genitive");
                Assert.IsFalse(string.IsNullOrWhiteSpace(demon.Called), id + " Called");
                Assert.IsFalse(string.IsNullOrWhiteSpace(demon.CalledGenitive), id + " CalledGenitive");
            }

            Assert.AreEqual("MAMMON'UN", UiText.GenitiveOf(UiText.Dealer(DealerRoster.MammonId)));
            Assert.AreEqual("Belial'in", UiText.GenitiveInSentence(UiText.Dealer(DealerRoster.BelialId)));
            Assert.AreEqual("Lilith", UiText.NameInSentence(UiText.Dealer(DealerRoster.LilithId)), "Never \"Lılıth\".");
            Assert.AreEqual("Sabah Yıldızı'nın", UiText.GenitiveInSentence(UiText.Dealer(DealerRoster.LuciferId)));
        }

        [Test]
        public void English_NeedsNoHandWrittenPossessive_ItAddsApostropheS()
        {
            foreach (string id in EveryDemonId())
                Assert.IsNull(UiText.Dealer(id).Genitive, id);

            Assert.AreEqual("Freed at BELIAL's table: 2",
                string.Format(UiText.RecordsDealerFormat, UiText.GenitiveOf(UiText.Dealer(DealerRoster.BelialId)), 2));
            StringAssert.Contains("Lilith's thorn cost you 100 years.", Log(DealerRoster.LilithId, CheatIds.Thorn, thornYears: 100));
            StringAssert.Contains("The Morning Star's cheat backfired!", Log(DealerRoster.LuciferId, CheatIds.Gaze, backfired: true));
        }

        [Test]
        public void Turkish_PossessiveWhereTheSentenceNeedsIt_PlainNameWhereItDoesNot()
        {
            Lang.Set(Language.Turkish);

            Assert.AreEqual("BELIAL'IN masasında aklanma: 2",
                string.Format(UiText.RecordsDealerFormat, UiText.GenitiveOf(UiText.Dealer(DealerRoster.BelialId)), 2));
            Assert.AreEqual("Lilith'in dikeni sana 100 yıla mal oldu.", Log(DealerRoster.LilithId, CheatIds.Thorn, thornYears: 100));
            Assert.AreEqual("Belial'in yılanı K♠ kartını çaldı.", Log(DealerRoster.BelialId, CheatIds.SerpentSwap));
            Assert.AreEqual("Sabah Yıldızı'nın hilesi geri tepti!", Log(DealerRoster.LuciferId, CheatIds.Gaze, backfired: true));
            StringAssert.StartsWith("Belial'in dili kaydı:", Log(DealerRoster.BelialId, CheatIds.ForkedTongue, backfired: true));
            Assert.AreEqual("Mammon, K♠ kartını rehin olarak zincirledi.", Log(DealerRoster.MammonId, CheatIds.Collateral), "The doer: no suffix.");
            Assert.AreEqual("Mammon haraç aldı: kazancından 25 yıl.", Log(DealerRoster.MammonId, CheatIds.Tithe, titheYears: 25));
        }

        private static string Log(string dealerId, string cheatId, int thornYears = 0, int titheYears = 0, bool backfired = false)
        {
            var result = new CheatResult(cheatId, CheatOutcome.Played, new[] { 0 }, null,
                new HellPoker.Core.Cards.Card(HellPoker.Core.Cards.Rank.King, HellPoker.Core.Cards.Suit.Spades),
                new HellPoker.Core.Cards.Card(HellPoker.Core.Cards.Rank.Two, HellPoker.Core.Cards.Suit.Hearts), backfired: backfired);
            return UiText.CheatLog(UiText.Dealer(dealerId), result, false, thornYears, titheYears);
        }
        // ------------------------------------------------------------------ the language changes on the title menu only

        private FakeTableView _view;
        private HellPokerGame _game;
        private MemoryStore _store;
        private MainMenuPresenterTests.FakeMenuView _menu;
        private MainMenuPresenterTests.FakeDealerSelectView _choice;
        private TablePresenter _table;
        private MainMenuPresenter _menus;
        private SettingsPresenter _settings;

        private TablePresenter Table(RunArchive archive = null, Dealer finalDealer = null)
        {
            _view = new FakeTableView();
            return new TablePresenter(d => _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: finalDealer == null ? 0 : 250)),
                TestDecks.Stacked("2C 9C JC 4C KC 2D 2H 5S 7H 9D 3S 6C JD QC 10S 2S 4H 5C 6D 7S"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()), new HouseDrawStrategy(), d.Payouts), _view, null, archive, finalDealer);
        }

        /// <summary>The whole game as the player meets it: the title menu, the table, the settings (language buttons on the menu).</summary>
        private void Game()
        {
            _store = new MemoryStore();
            _table = Table(new RunArchive(_store));
            _menu = new MainMenuPresenterTests.FakeMenuView();
            _choice = new MainMenuPresenterTests.FakeDealerSelectView();
            var settingsView = new MainMenuPresenterTests.FakeSettingsView();
            _menus = new MainMenuPresenter(_menu, _choice, settingsView, new MainMenuPresenterTests.FakeEndScreen(),
                new MainMenuPresenterTests.FakeRecords(), _view, _table, new MainMenuPresenterTests.FakeQuitter(),
                new MainMenuPresenterTests.FakeTransition(), DealerRoster.All);
            _settings = new SettingsPresenter(new GameSettings(_store), settingsView, new NoDisplay(), _menu);
        }

        [TearDown]
        public void DisposeGame()
        {
            _settings?.Dispose();
            _menus?.Dispose();
            _table?.Dispose();
            _settings = null;
            _menus = null;
            _table = null;
        }

        [Test]
        public void TheTable_HasNoLanguageButton()
        {
            Assert.IsFalse(typeof(ILanguageButton).IsAssignableFrom(typeof(ITableView)));
            Assert.IsNull(typeof(ITableView).GetEvent("LanguagePressed"));
        }

        [Test]
        public void TheLanguageKey_WorksOnTheTitleMenuOnly()
        {
            Game();
            Assert.IsTrue(_menus.IsAtMenuRoot, "The title menu.");

            _menu.PressNewGame();
            Assert.IsFalse(_menus.IsAtMenuRoot, "The dealer choice.");
            _choice.Choose(0);
            Assert.IsFalse(_menus.IsAtMenuRoot, "The table.");
            _view.PressAction();
            Assert.IsFalse(_menus.IsAtMenuRoot, "Mid-hand.");

            _view.PressMenu();
            Assert.IsTrue(_menus.IsAtMenuRoot);
            _menu.RulesOpen = true;
            Assert.IsFalse(_menus.IsAtMenuRoot, "Not under the rules.");
            _menu.RulesOpen = false;
            _menu.PressNewGame();
            Assert.IsTrue(_menu.IsConfirming);
            Assert.IsFalse(_menus.IsAtMenuRoot, "Not under a warning.");
        }

        [Test]
        public void OnTheMenu_MidDraw_TheLanguageChanges_AndTheTableComesBackInIt_Untouched()
        {
            Game();
            _menu.PressNewGame();
            _choice.Choose(0);
            _view.PressAction();
            _table.CheckToDraw();
            _table.ToggleDiscard(1);
            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            int years = _game.Years, stake = _game.CurrentStake, round = _game.RoundNumber, saves = _store.Saves;
            string hand = _game.PlayerHand.ToString();
            int lines = _view.DealerView.LinesSaid;
            _view.PressMenu();

            _menu.PressLanguage();
            _menu.PressContinue();

            Assert.AreEqual(Language.Turkish, Lang.Current);
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            Assert.AreEqual(years, _game.Years);
            Assert.AreEqual(stake, _game.CurrentStake);
            Assert.AreEqual(round, _game.RoundNumber);
            Assert.AreEqual(hand, _game.PlayerHand.ToString(), "The same cards: nothing dealt again.");
            CollectionAssert.AreEqual(new[] { 1 }, _table.SelectedDiscards.ToArray(), "The chosen card stays chosen.");
            StringAssert.Contains("Ateşe atmak için", _view.Message);
            Assert.AreEqual("KART DEĞİŞ 1", _view.ActionLabel);
            Assert.AreEqual("Dokuzuncu Kasanın Tefecisi", _view.DealerView.Relabelled.Title);
            Assert.AreEqual(lines, _view.DealerView.LinesSaid, "The demon does not speak again: no typing.");
            Assert.AreEqual("Otur, otur. Borçlu olduğun her yıl burada yazılı. Bakalım kaçını geri alabileceksin.",
                _view.DealerView.LinesSet.Last(), "The line on screen, swapped in place.");
            Assert.AreEqual(saves + 1, _store.Saves, "Only the language was saved: not the run.");

            _table.PerformAction();   // the draw still works
            Assert.AreEqual(GamePhase.DrawReveal, _game.Phase);
        }

        [Test]
        public void OnTheMenu_AfterAResult_NothingReplays()
        {
            Game();
            _menu.PressNewGame();
            _choice.Choose(0);
            _view.PressAction();
            _view.PressBet(BetAction.Fold);
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            int moments = _view.Moments.Count, hands = _table.Stats.HandsPlayed, years = _game.Years;
            _view.PressMenu();

            _menu.PressLanguage();
            _menu.PressContinue();

            Assert.AreEqual(moments, _view.Moments.Count, "No moment plays again.");
            Assert.AreEqual(hands, _table.Stats.HandsPlayed, "The hand is not counted twice.");
            Assert.AreEqual(years, _game.Years);
            StringAssert.Contains("Çekilip masadan sıvışıyorsun", _view.Message);
            Assert.AreEqual("SONRAKİ EL", _view.ActionLabel);
        }

        [Test]
        public void AtLucifersGate_ALanguageChange_SummonsNobody_AndSavesNothing()
        {
            var store = new MemoryStore();
            _table = Table(new RunArchive(store), DealerRoster.Lucifer);
            _table.StartNewRun(DealerRoster.Mammon);
            _game.TakeOver(250, 3);   // at the gate, between hands, before the table looked again
            int saves = store.Saves, attempts = _table.Gate.Attempts;

            Lang.Set(Language.Turkish);

            Assert.AreEqual(DealerRoster.MammonId, _table.CurrentDealerId, "No summons from a language change.");
            Assert.AreEqual(attempts, _table.Gate.Attempts);
            Assert.AreEqual(saves, store.Saves, "Nothing written to the save.");
            Assert.AreEqual(GamePhase.Betting, _game.Phase);
        }

        [Test]
        public void ChangingLanguage_WhileAnimating_LetsTheAnimationFinishFirst()
        {
            _table = Table();
            _table.StartNewRun(DealerRoster.Mammon);
            _view.IsBusy = true;
            int skips = _view.Skips;

            Lang.Set(Language.Turkish);

            Assert.Greater(_view.Skips, skips);
        }
    }
}