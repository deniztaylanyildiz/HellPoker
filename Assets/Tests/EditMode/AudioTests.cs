using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Sound: the volume settings (saved, read back, a broken value falls back), the presenters asking for the right effect
    /// at the right moment (in the table's queue), the music following the screen and the demon, the soul's layer, and
    /// hurrying the table cutting long effects.
    /// </summary>
    public class AudioTests
    {
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        internal sealed class FakeAudio : IAudio
        {
            public readonly List<string> Sfx = new List<string>();
            public readonly List<string> Music = new List<string>();
            public bool SoulLayer { get; private set; }
            public int Cuts { get; private set; }
            public float MusicVolume { get; private set; } = -1f;
            public float SfxVolume { get; private set; } = -1f;
            public float MasterVolume { get; private set; } = -1f;

            public void PlaySfx(string id) => Sfx.Add(id);
            public void PlayMusic(string trackId) => Music.Add(trackId);
            public void SetSoulLayer(bool on) => SoulLayer = on;
            public void CutLong() => Cuts++;

            public void SetVolumes(float master, float music, float sfx)
            {
                MasterVolume = master;
                MusicVolume = music;
                SfxVolume = sfx;
            }
        }

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) => _cheat = cheat;
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, null);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        // ------------------------------------------------------------------ the settings

        [Test]
        public void TheVolumes_AreSaved_ReadBack_AndCycle()
        {
            var store = new MemoryStore();
            var settings = new GameSettings(store);
            Assert.AreEqual(7, settings.MusicVolume);
            Assert.AreEqual(7, settings.SfxVolume);

            for (int i = 0; i < 4; i++) settings.CycleMusic();   // 7 → 8 → 9 → 10 → 0
            settings.CycleSfx();

            Assert.AreEqual(0, settings.MusicVolume, "Round and round: after 10, silence.");
            Assert.AreEqual(8, settings.SfxVolume);
            Assert.AreEqual(0, store.GetInt("settings.music", -1));
            Assert.AreEqual(8, store.GetInt("settings.sfx", -1));
            var back = new GameSettings(store);
            Assert.AreEqual(0, back.MusicVolume);
            Assert.AreEqual(8, back.SfxVolume);
        }

        [TestCase(-3)]
        [TestCase(11)]
        public void ABrokenVolume_IsTheDefault(int saved)
        {
            var store = new MemoryStore();
            store.SetInt("settings.music", saved);
            store.SetInt("settings.sfx", saved);

            var settings = new GameSettings(store);

            Assert.AreEqual(GameSettings.DefaultVolume, settings.MusicVolume);
            Assert.AreEqual(GameSettings.DefaultVolume, settings.SfxVolume);
        }

        [Test]
        public void TheSettingsScreen_ShowsAndAppliesTheVolumes()
        {
            var audio = new FakeAudio();
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var settings = new GameSettings(new MemoryStore());
            using var presenter = new SettingsPresenter(settings, view, new NoDisplay(), audio);
            Assert.AreEqual("7 / 10", view.Music);
            Assert.AreEqual(0.7f, audio.MusicVolume, 1e-4f);

            for (int i = 0; i < 4; i++) view.PressMusic();
            view.PressSfx();

            Assert.AreEqual("OFF", view.Music);
            Assert.AreEqual("8 / 10", view.SfxVolume);
            Assert.AreEqual(0f, audio.MusicVolume);
            Assert.AreEqual(0.8f, audio.SfxVolume, 1e-4f);
        }

        private sealed class NoDisplay : IDisplayMode
        {
            public int DisplayWidth => 1920;
            public int DisplayHeight => 1080;
            public void Apply(bool fullscreen, int windowScale, bool fill, bool vSync) { }
        }

        // ------------------------------------------------------------------ at the table

        private FakeTableView _view;
        private FakeAudio _audio;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void Start(string player, string house, ICheat cheat = null, int years = 1000, Dealer finalDealer = null)
        {
            _view = new FakeTableView();
            _audio = new FakeAudio();
            _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: finalDealer == null ? 0 : 250)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(cheat == null ? null : new OnlyCheat(cheat), 1, random), random);
            }, _view, null, null, finalDealer, null, _audio);
            _presenter.StartNewRun(DealerRoster.Mammon);
            if (years != 1000)
            {
                _game.TakeOver(years, 3);
                _presenter.SwitchTable(DealerRoster.Mammon);
            }
        }

        [Test]
        public void AHand_SoundsAsItPlays_Deal_Chips_Flips_AndABigWin()
        {
            Start(Flush, HouseTwos);

            _view.PressAction();
            Assert.AreEqual(SfxIds.Deal, _view.Sfx.Last());
            _view.PressBet(BetAction.Raise);
            CollectionAssert.IsSubsetOf(new[] { SfxIds.Chip, SfxIds.Flip }, _view.Sfx);
            _presenter.CheckToDraw();
            _view.PressAction();   // stand pat
            while (_game.Phase != GamePhase.RoundOver) _view.PressBet(BetAction.Pass);

            Assert.AreEqual(SfxIds.WinBig, _view.Sfx.Last(), "A flush: the fanfare.");
        }

        [Test]
        public void ALoss_SoundsLikeOne()
        {
            Start(Nothing, HouseFullHouse);
            _view.PressAction();
            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(SfxIds.Loss, _view.Sfx.Last());
        }

        [Test]
        public void TheSeal_ACheat_AndTheSoul_EachHaveTheirSound()
        {
            Start(Nothing, HouseFullHouse, new CollateralCheat());
            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            _view.PressBet(BetAction.Raise);   // the table is full: sealed, and the chain strikes as the draw opens

            CollectionAssert.Contains(_view.Sfx, SfxIds.Sealed);
            CollectionAssert.Contains(_view.Sfx, SfxIds.Cheat);

            _presenter.Dispose();
            Start(Nothing, HouseFullHouse, years: 2100);
            CollectionAssert.Contains(_view.Sfx, SfxIds.Soul);
            Assert.IsTrue(_audio.SoulLayer, "The soul's layer under the music.");
        }

        [Test]
        public void HurryingTheTable_CutsLongEffects()
        {
            Start(Flush, HouseTwos);
            _view.IsBusy = true;

            _view.PressAction();

            Assert.AreEqual(1, _audio.Cuts);
        }

        [Test]
        public void TheSummons_HasItsSound_AndHisMusic()
        {
            Start(Flush, HouseTwos, years: 200, finalDealer: DealerRoster.Lucifer);

            CollectionAssert.Contains(_view.Sfx, SfxIds.Summoned);
            Assert.AreEqual(DealerRoster.LuciferId, _audio.Music.Last());
        }

        // ------------------------------------------------------------------ the music follows the screen

        [Test]
        public void TheMenu_PlaysItsTheme_TheTable_TheDemons()
        {
            var audio = new FakeAudio();
            var session = new MainMenuPresenterTests.FakeSession();
            var menu = new MainMenuPresenterTests.FakeMenuView();
            var choice = new MainMenuPresenterTests.FakeDealerSelectView();
            var table = new FakeTableView();
            using var presenter = new MainMenuPresenter(menu, choice, new MainMenuPresenterTests.FakeSettingsView(),
                new MainMenuPresenterTests.FakeEndScreen(), new MainMenuPresenterTests.FakeRecords(), table, session,
                new MainMenuPresenterTests.FakeQuitter(), new MainMenuPresenterTests.FakeTransition(), DealerRoster.All, audio: audio);
            Assert.AreEqual(SfxIds.MenuMusic, audio.Music.Last());

            menu.PressNewGame();
            choice.Choose(1);

            Assert.AreEqual(DealerRoster.BelialId, audio.Music.Last());
            CollectionAssert.Contains(audio.Sfx, SfxIds.Transition);
            table.PressMenu();
            Assert.AreEqual(SfxIds.MenuMusic, audio.Music.Last());
        }
    }
}
