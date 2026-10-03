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
            var table = new FakeTableView();
            using var presenter = new SettingsPresenter(settings, settingsView, new NoDisplay(), table);
            Assert.AreEqual("ENGLISH", settingsView.Language);

            table.PressLanguage();

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

        // ------------------------------------------------------------------ the table changes language mid-hand

        private FakeTableView _view;
        private HellPokerGame _game;

        private TablePresenter Table()
        {
            _view = new FakeTableView();
            return new TablePresenter(d => _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000)),
                TestDecks.Stacked("2C 9C JC 4C KC 2D 2H 5S 7H 9D 3S 6C JD QC 10S 2S 4H 5C 6D 7S"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()), new HouseDrawStrategy(), d.Payouts), _view);
        }

        [Test]
        public void ChangingLanguage_MidDraw_RewritesTheWords_AndTouchesNothingElse()
        {
            using TablePresenter presenter = Table();
            presenter.StartNewRun(DealerRoster.Mammon);
            _view.PressAction();
            presenter.CheckToDraw();
            presenter.ToggleDiscard(1);
            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            int years = _game.Years, stake = _game.CurrentStake, round = _game.RoundNumber;
            string hand = _game.PlayerHand.ToString();
            int lines = _view.DealerView.LinesSaid;

            Lang.Set(Language.Turkish);

            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            Assert.AreEqual(years, _game.Years);
            Assert.AreEqual(stake, _game.CurrentStake);
            Assert.AreEqual(round, _game.RoundNumber);
            Assert.AreEqual(hand, _game.PlayerHand.ToString(), "The same cards: nothing dealt again.");
            CollectionAssert.AreEqual(new[] { 1 }, presenter.SelectedDiscards.ToArray(), "The chosen card stays chosen.");
            StringAssert.Contains("Ateşe atmak için", _view.Message);
            Assert.AreEqual("KART DEĞİŞ 1", _view.ActionLabel);
            Assert.AreEqual("MAMMON", _view.DealerView.Relabelled.Name);
            Assert.AreEqual("Dokuzuncu Kasanın Tefecisi", _view.DealerView.Relabelled.Title);
            Assert.AreEqual(lines + 1, _view.DealerView.LinesSaid, "The last line, once, in the new words.");
            Assert.AreEqual("Otur, otur. Borçlu olduğun her yıl burada yazılı. Bakalım kaçını geri alabileceksin.", _view.DealerView.LastLine);

            presenter.PerformAction();   // the draw still works
            Assert.AreEqual(GamePhase.DrawReveal, _game.Phase);
        }

        [Test]
        public void ChangingLanguage_OnTheResult_ReplaysNothing()
        {
            using TablePresenter presenter = Table();
            presenter.StartNewRun(DealerRoster.Mammon);
            _view.PressAction();
            _view.PressBet(BetAction.Fold);
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            int moments = _view.Moments.Count, hands = presenter.Stats.HandsPlayed, years = _game.Years;

            Lang.Set(Language.Turkish);

            Assert.AreEqual(moments, _view.Moments.Count, "No moment plays again.");
            Assert.AreEqual(hands, presenter.Stats.HandsPlayed, "The hand is not counted twice.");
            Assert.AreEqual(years, _game.Years);
            StringAssert.Contains("Çekilip masadan sıvışıyorsun", _view.Message);
            Assert.AreEqual("SONRAKİ EL", _view.ActionLabel);
        }

        [Test]
        public void ChangingLanguage_WhileAnimating_LetsTheAnimationFinishFirst()
        {
            using TablePresenter presenter = Table();
            presenter.StartNewRun(DealerRoster.Mammon);
            _view.IsBusy = true;
            int skips = _view.Skips;

            Lang.Set(Language.Turkish);

            Assert.Greater(_view.Skips, skips);
        }
    }
}
