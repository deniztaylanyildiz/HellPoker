// GEÇİCİ TEST KISAYOLU: testler bitince silinecek.
// Silmek için: bu dosyayı (DevJumpShortcut.cs ve .meta'sı) sil ve HellPokerBootstrap.Awake'teki tek satırı
// ("DevJumpShortcut.Install(...)") kaldır; başka hiçbir şey gerekmez. ChapterPresenter'daki iz bırakmayan koşu kancaları partial
// metotlardır (Traceless / TracelessEnds): bu dosya gidince derleyici onları düşürür, oyun hiç iz bırakmayan koşu açmaz.
// Testleri (Tests/EditMode/DevJumpShortcutTests.cs, Tests/PlayMode/DevJumpShortcutPlayTests.cs) kısayolu yansımayla bulur: bu dosya
// gidince derleme bozulmaz, onlar kendiliğinden atlanır; istersen onları da sil.

using System;
using System.Collections;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HellPoker.Presentation
{
    /// <summary>
    /// TEMPORARY (Demo 3.4.1, a tester's shortcut — the Boss Simulator is gone): F9 on the title menu — only in the editor, a development
    /// build or with HELLPOKER_DEV=1 — opens a list of the four demons (Mammon, Belial, Lilith, Lucifer). A choice (a click, or 1-4) takes a
    /// run straight to that demon's table through the chapters' own flow: a Peasant at Medium, placed at the chapter's gate, the tribute
    /// paid the game's way (Lucifer: his call answered). The table is the game's own. The run leaves no trace — no save written over the
    /// player's, nothing in the records, no unlock, no run log — and when its table ends (won or lost) the menu comes back. A player's
    /// build has none of it: F9 does nothing.
    /// </summary>
    public sealed class DevJumpShortcut : MonoBehaviour
    {
        /// <summary>The environment variable that opens the shortcut in any build: "1".</summary>
        public const string DevVariable = "HELLPOKER_DEV";

        /// <summary>A development build started with it shoots the list and a jump to Belial's table (persistentDataPath/tour), then quits.</summary>
        public const string ShotsArgument = "-devjumpshots";

        /// <summary>The four places F9 goes, in the list's order.</summary>
        public static readonly string[] Targets = { DealerRoster.MammonId, DealerRoster.BelialId, DealerRoster.LilithId, DealerRoster.LuciferId };

        private const int SortingOrder = 140;   // over the title menu (100) and its sub-screens
        private const int PanelWidth = 208;
        private const int ButtonWidth = 160;
        private const int ButtonHeight = 18;
        private const int RowStep = 24;

        private ChapterPresenter _chapters;
        private IMenuCommands _menu;
        private bool _allowed;
        private Canvas _canvas;

        /// <summary>Whether this build has the shortcut at all: the editor, a development build, HELLPOKER_DEV=1.</summary>
        public static bool Allowed(bool debugBuild, bool editor, string devValue) => debugBuild || editor || devValue == "1";

        /// <summary>The list is open (for tests).</summary>
        public bool IsOpen => _canvas != null && _canvas.enabled;

        /// <summary>The shortcut works in this build (for tests).</summary>
        public bool IsAllowed => _allowed;

        /// <summary>The one call that puts the shortcut in the game (HellPokerBootstrap); it decides for itself whether the build has it.</summary>
        public static DevJumpShortcut Install(GameObject host, ChapterPresenter chapters, IMenuCommands menu) =>
            Install(host, chapters, menu, Allowed(Debug.isDebugBuild, Application.isEditor, Environment.GetEnvironmentVariable(DevVariable)));

        /// <summary>Tests: a player's build (<paramref name="allowed"/> false) or a developer's.</summary>
        public static DevJumpShortcut Install(GameObject host, ChapterPresenter chapters, IMenuCommands menu, bool allowed)
        {
            var shortcut = host.AddComponent<DevJumpShortcut>();
            shortcut._chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            shortcut._menu = menu ?? throw new ArgumentNullException(nameof(menu));
            shortcut._allowed = allowed;
            if (!allowed) return shortcut;
            shortcut.Build(host.transform);
            if (Environment.GetCommandLineArgs().Contains(ShotsArgument)) shortcut.StartCoroutine(shortcut.Shots());
            return shortcut;
        }

        /// <summary>F9: the list opens on the title menu's root (and closes again); nothing in a player's build.</summary>
        public void Toggle()
        {
            if (!_allowed) return;
            if (IsOpen) Close();
            else if (_menu.IsAtMenuRoot && !_menu.IsTransitioning) Open();
        }

        /// <summary>A demon from the list (0 Mammon ... 3 Lucifer): his table, through the chapters' flow, leaving no trace.</summary>
        public void Jump(int target)
        {
            if (!_allowed || target < 0 || target >= Targets.Length) return;
            Close();
            _chapters.JumpTraceless(Targets[target]);
            _menu.GoBack();   // the menu's Esc: back into the run that now waits — the menu goes, the chapter's table comes, with the curtain
        }

        private void Open()
        {
            _canvas.enabled = true;
            _canvas.GetComponent<GraphicRaycaster>().enabled = true;
            SetKeyboard(false);   // the game's keys wait while the list is open (Esc closes it, 1-4 choose)
        }

        private void Close()
        {
            if (_canvas == null) return;
            _canvas.enabled = false;
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            SetKeyboard(true);
        }

        private void SetKeyboard(bool on)
        {
            var keys = GetComponent<KeyboardInput>();
            if (keys != null) keys.enabled = on;
        }

        private void Update()
        {
            if (!_allowed) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                Toggle();
                return;
            }
            if (!IsOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (keyboard.digit1Key.wasPressedThisFrame) Jump(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) Jump(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) Jump(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) Jump(3);
        }

        private void OnDestroy() => SetKeyboard(true);

        // ------------------------------------------------------------------ the list

        private void Build(Transform parent)
        {
            _canvas = UiFactory.CreateScreen("DevJumpCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            int height = 40 + Targets.Length * RowStep + 8 + RowStep + 8;
            Image panel = UiFactory.CreatePanel("DevJumpPanel", screen, hot: true);
            panel.raycastTarget = true;
            int left = (PixelScreen.Width - PanelWidth) / 2 / 8 * 8, top = (PixelScreen.Height - height) / 2 / 8 * 8;
            panel.rectTransform.PlaceTL(left, top, PanelWidth, height);

            UiFactory.CreateText("DevJumpTitle", panel.transform, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline()
                .Localized(() => Lang.Pick("F9 · TEST JUMP", "F9 · TEST ATLAMA")).rectTransform.PlaceTL(0, 8, PanelWidth, 8);
            UiFactory.CreateText("DevJumpNote", panel.transform, "", 8, Palette.BoneMid).WithOutline()
                .Localized(() => Lang.Pick("Not saved · no records", "Kaydedilmez · rekora girmez")).rectTransform.PlaceTL(0, 22, PanelWidth, 9);

            for (int i = 0; i < Targets.Length; i++)
            {
                int target = i;
                string id = Targets[i];
                Button button = UiFactory.CreateButton("DevJump" + i, panel.transform, "", 8, out Text label, i == 3 ? ButtonSkin.Blood : ButtonSkin.Ash);
                label.Localized(() => (target + 1) + "  " + UiText.Dealer(id).Name);
                ((RectTransform)button.transform).PlaceTL((PanelWidth - ButtonWidth) / 2, 40 + i * RowStep, ButtonWidth, ButtonHeight);
                button.onClick.AddListener(() => Jump(target));
            }

            Button close = UiFactory.CreateButton("DevJumpClose", panel.transform, "", 8, out Text closeLabel, ButtonSkin.Ash);
            closeLabel.Localized(() => UiText.Back);
            ((RectTransform)close.transform).PlaceTL((PanelWidth - 80) / 2, 40 + Targets.Length * RowStep + 8, 80, ButtonHeight);
            close.onClick.AddListener(Close);

            Close();
        }

        // ------------------------------------------------------------------ the tester's screenshots

        /// <summary>
        /// <see cref="ShotsArgument"/>: the list on the title menu, then F9 ▸ Belial and his table after the deal (persistentDataPath/tour/
        /// devjump_*.png), a log line ("Hell Poker dev jump ..."), then the game quits.
        /// </summary>
        private IEnumerator Shots()
        {
            yield return new WaitForSecondsRealtime(2f);
            Toggle();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("devjump_list");
            bool listed = IsOpen;
            Jump(1);
            yield return new WaitForSecondsRealtime(2f);
            _chapters.Table?.PerformAction();   // the deal
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("devjump_belial");
            Debug.Log($"Hell Poker dev jump: list {listed}, at the table {_chapters.IsAtTable}, demon {_chapters.Run?.Rules.BossId}, " +
                      $"traceless {_chapters.IsTracelessRun}");
            Application.Quit();
        }

        private static IEnumerator Shot(string name)
        {
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "tour");
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, name + ".png"));
            yield return new WaitForEndOfFrame();
            yield return null;
        }
    }

    /// <summary>The chapters' side of the temporary shortcut: a run that leaves no trace (see the top of this file).</summary>
    public sealed partial class ChapterPresenter
    {
        /// <summary>The player's own records while a traceless run plays on a copy of them; null: no such run.</summary>
        private ChapterRecords _playersRecords;

        /// <summary>A traceless run is going on (for tests).</summary>
        public bool IsTracelessRun => _playersRecords != null;

        partial void Traceless(ref bool yes)
        {
            yes = _playersRecords != null;
        }

        partial void TracelessEnds(ref bool handled)
        {
            if (_playersRecords == null) return;
            handled = true;
            EndTraceless();
        }

        /// <summary>
        /// A run straight to <paramref name="bossId"/>'s table through the chapters' own flow: a Peasant at Medium placed at the gate of
        /// that demon's chapter, the tribute paid as the gate pays it (Lucifer: after Lilith, his call answered). Whatever it does stays on
        /// a copy of the records; the run in memory is let go (the player's save waits in the archive, untouched).
        /// </summary>
        internal void JumpTraceless(string bossId)
        {
            int boss = DifficultyRules.BossIndex(bossId);
            if (boss < 0) throw new ArgumentException("Not a demon of the chapters: " + bossId, nameof(bossId));
            if (_playersRecords == null) _playersRecords = _records;   // a jump from a jump keeps the player's, not the copy
            _records = ChapterRecords.Decode(_playersRecords.Encode());
            EnsureTable();
            if (_panel.IsOpen) ClosePanel();
            _journey = null;
            _ended = false;
            _match = null;
            _bossGame = null;
            _atTable = false;

            bool lucifer = boss == 3;
            ChapterSave save = PlacedSave(SinnerRoster.Peasant, _newSeed(), lucifer ? ChapterRules.Chapters : boss + 1, lucifer ? "lucifer" : "gate");
            save.Difficulty = DifficultyRules.Default;
            Resume(save);   // the gate's panel (Lucifer's call) — then the game's own step to his table
            if (lucifer) SitWithLucifer();
            else SitWithTheDemon();
        }

        /// <summary>The traceless run is over: its copy of the records goes, the player's come back, the menu shows.</summary>
        private void EndTraceless()
        {
            _records = _playersRecords;
            _playersRecords = null;
            _log = null;
            _journey = null;
            _ended = false;
            _match = null;
            _bossGame = null;
            _atTable = false;
            if (_panel.IsOpen) ClosePanel();
            Hide();
            MenuRequested?.Invoke();
        }
    }
}
