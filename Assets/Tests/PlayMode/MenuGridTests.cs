using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Phase 3: the title menu sits on the 8 px grid — every menu button the same size, on the grid and in two aligned columns, the logo at a
    /// whole-number scale, the corner buttons on the grid — and no words run into each other, in both languages, with and without a run waiting.
    /// </summary>
    public class MenuGridTests
    {
        [TearDown]
        public void BackToTheUsualMenu() => ForgetTheKeyboard();

        /// <summary>The test keyboard goes, the input settings come back (every test that tapped a key).</summary>
        internal static void ForgetTheKeyboard()
        {
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
                InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
#endif
            }
            _keyboard = null;
            Lang.Set(Language.English);
        }

        private static readonly string[] MenuButtons =
            { "ContinueButton", "NewGameButton", "SettingsButton", "HowToPlayButton", "RecordsButton", "QuitButton" };

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMenu_IsOnTheGrid_InBothLanguages_WithAndWithoutARun()
        {
            foreach (bool saved in new[] { false, true })
            foreach (bool turkish in new[] { false, true })
            {
                HellPokerBootstrap.BatchStore.Clear();
                HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
                if (saved)
                    new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore)
                        .SaveRun(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 2, ""));
                yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
                yield return new WaitForSeconds(0.5f);
                string what = (saved ? "a run waits" : "no run") + (turkish ? " (tr)" : " (en)");
                var menu = Object.FindFirstObjectByType<MainMenuView>();
                Canvas.ForceUpdateCanvases();

                var buttons = menu.GetComponentsInChildren<Button>(false).Where(b => MenuButtons.Contains(b.name)).Select(b => (RectTransform)b.transform).ToList();
                Assert.GreaterOrEqual(buttons.Count, 5, what + ": the menu's buttons");
                Vector2 size = buttons[0].sizeDelta;
                foreach (RectTransform b in buttons)
                {
                    Assert.AreEqual(size, b.sizeDelta, $"{what}: {b.name} the same size as the others");
                    Assert.AreEqual(0, Mathf.RoundToInt(b.anchoredPosition.x) % 8, $"{what}: {b.name} on the grid (x)");
                    Assert.AreEqual(0, Mathf.RoundToInt(-b.anchoredPosition.y) % 8, $"{what}: {b.name} on the grid (y)");
                    Text label = b.GetComponentInChildren<Text>();
                    Assert.LessOrEqual(label.preferredWidth, b.rect.width - 4f, $"{what}: '{label.text}' fits {b.name}");
                }
                var columns = buttons.Select(b => Mathf.RoundToInt(b.anchoredPosition.x)).Distinct().ToList();
                Assert.LessOrEqual(columns.Count, 3, what + ": two columns (and Quit centred)");
                Assert.AreEqual(size.y % 8, 0f, what + ": the buttons' height on the grid");

                foreach (string corner in new[] { "MenuFeedbackButton" })
                {
                    var rect = (RectTransform)menu.GetComponentsInChildren<Button>(false).Single(b => b.name == corner).transform;
                    Assert.AreEqual(0, Mathf.RoundToInt(-rect.anchoredPosition.y) % 8, $"{what}: {corner} on the grid");
                    Text label = rect.GetComponentInChildren<Text>();
                    Assert.LessOrEqual(label.preferredWidth, rect.rect.width - 2f, $"{what}: '{label.text}' fits {corner}");
                }

                Image logo = menu.GetComponentsInChildren<Image>(false).FirstOrDefault(i => i.name == "Title" && i.sprite != null);
                if (logo != null)
                {
                    float scale = logo.rectTransform.sizeDelta.x / logo.sprite.rect.width;
                    Assert.AreEqual(Mathf.Round(scale), scale, 0.001f, what + ": the logo at a whole-number scale");
                    Assert.AreEqual(scale, logo.rectTransform.sizeDelta.y / logo.sprite.rect.height, 0.001f, what + ": square pixels");
                }

                // No words over each other or over a button that is not theirs.
                var texts = menu.GetComponentsInChildren<Text>(false).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)
                    && t.GetComponentInParent<Button>() == null && t.transform.lossyScale.x > 0.01f && !Under(t.transform, "RulesPanel") && !Under(t.transform, "ConfirmNewGame")).ToList();
                var all = menu.GetComponentsInChildren<Button>(false).Select(b => (RectTransform)b.transform).ToList();
                var problems = new List<string>();
                for (int i = 0; i < texts.Count; i++)
                {
                    Rect a = Box(texts[i].rectTransform, texts[i]);
                    for (int j = i + 1; j < texts.Count; j++)
                        if (Overlaps(a, Box(texts[j].rectTransform, texts[j]))) problems.Add($"'{texts[i].text}' over '{texts[j].text}'");
                    foreach (RectTransform b in all)
                        if (Overlaps(a, Box(b, null))) problems.Add($"'{texts[i].text}' under {b.name}");
                }
                Assert.IsEmpty(problems, what + ":\n" + string.Join("\n", problems));
            }
        }

        // ---------------------------------------------------------------- Demo 3.3

        private static IEnumerator LoadMenu(bool turkish = false, ChapterSave save = null)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (turkish) HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            if (save != null) new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            Canvas.ForceUpdateCanvases();
        }

        private static Keyboard _keyboard;
        private static InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private static InputSettings.EditorInputBehaviorInPlayMode _editorInput;
#endif

        /// <summary>A key pressed and let go on a test keyboard (the game's own KeyboardInput reads it).</summary>
        internal static IEnumerator Tap(Key key)
        {
            if (_keyboard == null)
            {
                // A batch run has no focused game view: let the test keyboard's keys through anyway.
                _background = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                _keyboard = InputSystem.AddDevice<Keyboard>();
            }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private static Vector2Int TopLeft(string name)
        {
            var rect = (RectTransform)Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name).transform;
            return new Vector2Int(Mathf.RoundToInt(rect.anchoredPosition.x), -Mathf.RoundToInt(rect.anchoredPosition.y));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMenu_HasNoLanguageButton_AndLDoesNothing()
        {
            yield return LoadMenu();
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(b => b.name == "MenuLanguageButton"),
                "the language is chosen in Settings ▸ LANGUAGE");
            Language before = Lang.Current;
            yield return Tap(Key.L);
            Assert.AreEqual(before, Lang.Current, "L changes nothing");
            Assert.IsNull(HellPokerBootstrap.BatchStore.GetString("settings.language", null), "nothing saved");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator SettingsLanguageTab_ListsBothLanguages_AChoiceChangesAndSaves()
        {
            yield return LoadMenu();
            HellPokerScreenshots.Press("SettingsButton");
            yield return new WaitForSeconds(0.4f);
            var settings = Object.FindFirstObjectByType<SettingsView>();
            Assert.IsTrue(settings.IsVisible);

            HellPokerScreenshots.Press("SettingsTabLanguage");
            yield return null;
            var rows = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("SettingsLanguage")).OrderBy(b => b.name).ToList();
            Assert.AreEqual(2, rows.Count, "every language in the list");
            Assert.AreEqual("English", rows[0].GetComponentInChildren<Text>().text, "each in its own name");
            Assert.AreEqual("Türkçe", rows[1].GetComponentInChildren<Text>().text);

            // The keys: down to Türkçe, Enter.
            yield return Tap(Key.DownArrow);
            yield return Tap(Key.Enter);
            Assert.AreEqual(Language.Turkish, Lang.Current);
            Assert.AreEqual("Turkish", HellPokerBootstrap.BatchStore.GetString("settings.language", null), "saved");
            Assert.AreEqual("DİL", Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "SettingsTabLanguage")
                .GetComponentInChildren<Text>().text, "the screen rewrites at once");

            // A click on English.
            rows[0].onClick.Invoke();
            yield return null;
            Assert.AreEqual(Language.English, Lang.Current);
            Assert.AreEqual("English", HellPokerBootstrap.BatchStore.GetString("settings.language", null));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMenusCursor_MovesWithTheArrows_EnterPresses_TheMouseMovesIt()
        {
            yield return LoadMenu();
            var menu = Object.FindFirstObjectByType<MainMenuView>();
            Assert.AreEqual("NewGameButton", menu.SelectedButton, "the cursor starts on the first button");
            Assert.IsTrue(menu.CursorShown);
            Vector2Int at = TopLeft("NewGameButton");
            Assert.AreEqual(new Vector2Int(at.x - 12, at.y + 8), menu.CursorAt, "the flame sits left of its button, in the box's margin");

            yield return Tap(Key.RightArrow);
            Assert.AreEqual("SettingsButton", menu.SelectedButton);
            yield return Tap(Key.DownArrow);
            Assert.AreEqual("RecordsButton", menu.SelectedButton);
            yield return Tap(Key.DownArrow);
            Assert.AreEqual("QuitButton", menu.SelectedButton);
            yield return Tap(Key.DownArrow);
            Assert.AreEqual("NewGameButton", menu.SelectedButton, "round from the bottom to the top");
            yield return Tap(Key.LeftArrow);
            Assert.AreEqual("NewGameButton", menu.SelectedButton, "nothing further left");

            // The mouse brings the cursor along.
            var howTo = Object.FindObjectsByType<ButtonFeel>(FindObjectsSortMode.None).Single(f => f.name == "HowToPlayButton");
            howTo.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Assert.AreEqual("HowToPlayButton", menu.SelectedButton);
            Assert.IsTrue(howTo.Lit);
            howTo.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Assert.IsTrue(howTo.Lit, "still under the cursor");

            // Enter presses it: How to Play opens.
            yield return Tap(Key.Enter);
            Assert.IsTrue(menu.IsShowingRules);
            Assert.IsFalse(menu.CursorShown, "no cursor over the rules");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheContinueCard_ShowsTheWaitingRun_AndNoneWithout()
        {
            yield return LoadMenu();
            var menu = Object.FindFirstObjectByType<MainMenuView>();
            Assert.IsFalse(menu.ContinueCardShown, "no run, no card");

            foreach (bool turkish in new[] { false, true })
            {
                ChapterSave save = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 2, "gate");
                save.Coins = 60;
                yield return LoadMenu(turkish, save);
                menu = Object.FindFirstObjectByType<MainMenuView>();
                string what = turkish ? "tr" : "en";
                Assert.IsTrue(menu.ContinueCardShown, what + ": the card beside CONTINUE");
                Assert.AreEqual("ContinueButton", menu.SelectedButton, what + ": the cursor on CONTINUE");
                StringAssert.Contains(turkish ? "KRAL" : "KING", menu.ContinueCardText, what);
                StringAssert.Contains(turkish ? "2. BÖLÜM" : "CHAPTER 2", menu.ContinueCardText, what);
                StringAssert.Contains(turkish ? "60 COIN" : "60 COINS", menu.ContinueCardText, what);

                // Its words fit it, and it keeps clear of the box.
                Transform card = menu.GetComponentsInChildren<Transform>(false).Single(t => t.name == "ContinueCard");
                foreach (Text text in card.GetComponentsInChildren<Text>(false))
                {
                    Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 0.5f, $"{what}: '{text.text}' fits the card's height");
                    Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + 0.5f, $"{what}: '{text.text}' fits the card's width");
                }
                Transform box = menu.GetComponentsInChildren<Transform>(false).Single(t => t.name == "MenuBox");
                Assert.IsFalse(Overlaps(Box((RectTransform)card, null), Box((RectTransform)box, null)), what + ": the card and the box apart");
                var rect = (RectTransform)card;
                Assert.AreEqual(0, Mathf.RoundToInt(rect.anchoredPosition.x) % 8, what + ": the card on the grid");
                Assert.AreEqual(0, Mathf.RoundToInt(-rect.anchoredPosition.y) % 8, what + ": the card on the grid");
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMenusBox_HoldsTheButtons_AndBugIdeaSitsBottomLeft()
        {
            yield return LoadMenu();
            var menu = Object.FindFirstObjectByType<MainMenuView>();
            var box = (RectTransform)menu.GetComponentsInChildren<Transform>(false).Single(t => t.name == "MenuBox");
            Rect inside = Box(box, null);
            foreach (string name in MenuButtons)
            {
                Button button = menu.GetComponentsInChildren<Button>(false).FirstOrDefault(b => b.name == name);
                if (button == null) continue;
                Rect b = Box((RectTransform)button.transform, null);
                Assert.IsTrue(inside.Contains(b.min) && inside.Contains(b.max), name + " inside the box");
            }
            Assert.AreEqual(0, Mathf.RoundToInt(box.anchoredPosition.x) % 8, "the box on the grid");
            Assert.AreEqual(0, Mathf.RoundToInt(-box.anchoredPosition.y) % 8, "the box on the grid");

            Vector2Int feedback = TopLeft("MenuFeedbackButton");
            Assert.Less(feedback.x, PixelScreen.Width / 4, "BUG / IDEA on the left");
            Assert.Greater(feedback.y, PixelScreen.Height * 3 / 4, "at the bottom, across from the version");
        }

        private static bool Under(Transform t, string name)
        {
            for (Transform p = t; p != null; p = p.parent)
                if (p.name == name) return true;
            return false;
        }

        private static bool Overlaps(Rect a, Rect b) =>
            a.width > 0 && b.width > 0 && a.xMin < b.xMax - 0.5f && b.xMin < a.xMax - 0.5f && a.yMin < b.yMax - 0.5f && b.yMin < a.yMax - 0.5f;

        /// <summary>A rect's box on screen; for a text, the part its words take (its preferred width, centred as it is aligned).</summary>
        private static Rect Box(RectTransform rect, Text text)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Rect box = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            if (text == null) return box;
            float scale = box.width / Mathf.Max(1f, rect.rect.width);
            float width = Mathf.Min(rect.rect.width, text.preferredWidth) * scale, height = Mathf.Min(rect.rect.height, text.preferredHeight) * scale;
            float x = text.alignment == TextAnchor.MiddleRight || text.alignment == TextAnchor.UpperRight ? box.xMax - width
                : text.alignment == TextAnchor.MiddleLeft || text.alignment == TextAnchor.UpperLeft ? box.xMin : box.center.x - width / 2;
            float y = text.alignment == TextAnchor.UpperCenter || text.alignment == TextAnchor.UpperLeft || text.alignment == TextAnchor.UpperRight
                ? box.yMax - height : box.center.y - height / 2;
            return new Rect(x, y, width, height);
        }
    }
}
