using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Phase 3: the title menu sits on the 8 px grid — every menu button the same size, on the grid and in two aligned columns, the logo at a
    /// whole-number scale, the corner buttons on the grid — and no words run into each other, in both languages, in the game and the simulator.
    /// </summary>
    public class MenuGridTests
    {
        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

        private static readonly string[] MenuButtons =
            { "ContinueButton", "ChangeTableButton", "NewGameButton", "SettingsButton", "HowToPlayButton", "RecordsButton", "QuitButton" };

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMenu_IsOnTheGrid_InBothLanguages_InTheGameAndTheSimulator()
        {
            foreach (bool simulator in new[] { false, true })
            foreach (bool turkish in new[] { false, true })
            {
                HellPokerBootstrap.BatchStore.Clear();
                HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
                HellPokerBootstrap.ForceSimulator = simulator;
                yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
                yield return new WaitForSeconds(0.5f);
                string what = (simulator ? "simulator" : "game") + (turkish ? " (tr)" : " (en)");
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

                foreach (string corner in new[] { "MenuFeedbackButton", "MenuLanguageButton" })
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
