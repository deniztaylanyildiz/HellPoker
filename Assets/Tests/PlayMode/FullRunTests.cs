using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 2.2: whole runs through the real scene and its presenters, class by class, from the class card to the end screen — three
    /// chapters (every kind of node stepped on: a table, an event, an unknown, a market, a warden, a treasure, a camp), three demons,
    /// Lucifer. The player plays like the simulation's: the hand guide's draw, a raise with two pair or better, a call of every re-raise,
    /// the class's power once a chapter at least; offers and markets answered by their first choice a few times, then left. Once a
    /// chapter the scene is reloaded and the run CONTINUEd. Every step is watched: no error in the log (the test framework fails on one),
    /// no empty words on a panel, nothing written over anything at a table's first decision, no step longer than 30 seconds.
    /// The run is helped to its end where luck would stop a test, not a player: the purse is kept above a floor before each node and
    /// table, a demon's bar is kept under its start (the soul never burns), a demon's table that drags on is ended. Explicit: long.
    /// </summary>
    [Explicit, Category("FullRun")]
    public class FullRunTests
    {
        private const float StepLimitSeconds = 30f;
        private const int Purse = 150;

        private static readonly Core.Evaluation.IHandEvaluator Evaluator = Core.Evaluation.HandEvaluator.CreateDefault();

        [TearDown]
        public void BackToTheUsualMenu()
        {
            HellPokerBootstrap.ForceSimulator = null;
            Time.timeScale = 1f;
        }

        [UnityTest, Timeout(3600000)] public IEnumerator Peasant_English() => Run(0, "English", 0);
        [UnityTest, Timeout(3600000)] public IEnumerator Oracle_Turkish() => Run(1, "Turkish", 0);
        [UnityTest, Timeout(3600000)] public IEnumerator King_English() => Run(2, "English", 0);
        [UnityTest, Timeout(3600000)] public IEnumerator Jester_Turkish() => Run(3, "Turkish", 0);
        [UnityTest, Timeout(3600000)] public IEnumerator Summoner_English() => Run(4, "English", 0);
        [UnityTest, Timeout(3600000)] public IEnumerator Peasant_FifthCircle_Turkish() => Run(0, "Turkish", 5);

        private static ChapterPresenter Chapters => ChapterJourneyTests.Chapters;
        private static TablePresenter Table => (TablePresenter)Chapters.Table;

        private static readonly NodeKind[] Kinds =
            { NodeKind.Table, NodeKind.Event, NodeKind.Unknown, NodeKind.BlackMarket, NodeKind.Warden, NodeKind.Treasure, NodeKind.PurgatoryFire };

        /// <summary>The records a player who has been freed before has: the Summoner open, and every circle of the Peasant's.</summary>
        private static string Records()
        {
            var book = new ChapterRecords();
            for (int circle = 0; circle < Circles.Deepest; circle++)
            {
                book.RunStarted();
                book.RunEnded(JourneyEnd.Freed, 4, Core.Sinners.Peasant.ClassId, 140, circle, 100);
            }
            return book.Encode();
        }

        private static IEnumerator LoadScene()
        {
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
        }

        private sealed class Story
        {
            public readonly HashSet<NodeKind> Visited = new HashSet<NodeKind>();
            public readonly HashSet<NodeKind> VisitedHere = new HashSet<NodeKind>();
            public readonly Dictionary<string, int> PanelPresses = new Dictionary<string, int>();
            public readonly List<string> Notes = new List<string>();
            public int Chapter = 1, Reloads, BossHands, PowerChapters;
            public bool ReloadedHere, PowerHere, ReachedLucifer;
            public int KitStatAtChapter;
            public string End;
        }

        private IEnumerator Run(int classIndex, string language, int circle)
        {
            JesterK = 0;
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", language);
            HellPokerBootstrap.BatchStore.SetString(ChapterArchive.RecordsKey, Records());
            yield return LoadScene();
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            for (int c = 0; c < circle; c++)
            {
                HellPokerScreenshots.Press("CircleUp" + classIndex);
                yield return null;
            }
            HellPokerScreenshots.ChooseSinner(classIndex);
            yield return new WaitForSeconds(0.8f);
            Assert.IsNotNull(Chapters.Journey, "the class card opened a run");
            Assert.AreEqual(circle, Chapters.Journey.Circle, "the circle chosen");
            string classId = Chapters.Journey.Sinner.Id;

            var story = new Story { KitStatAtChapter = KitStat(classId) };
            float lastProgress = Time.realtimeSinceStartup;
            string lastState = null;
            int sameState = 0;
            for (int step = 0; step < 40000 && story.End == null; step++)
            {
                float started = Time.realtimeSinceStartup;
                yield return Step(classId, story);
                float took = Time.realtimeSinceStartup - started;
                Assert.Less(took, StepLimitSeconds, $"a step took {took:0.0} s ({State()})");

                string state = State();
                if (state == lastState) sameState++;
                else
                {
                    sameState = 0;
                    lastState = state;
                    lastProgress = Time.realtimeSinceStartup;
                }
                Assert.Less(Time.realtimeSinceStartup - lastProgress, StepLimitSeconds, $"stuck for {StepLimitSeconds} s at {state}");
                Assert.Less(sameState, 400, $"stuck at {state}\n{string.Join("\n", story.Notes.Skip(Math.Max(0, story.Notes.Count - 20)))}");
            }

            Debug.Log($"Full run {classId} ({language}, circle {circle}): {story.End}; nodes {string.Join(", ", story.Visited)}; reloads {story.Reloads}; " +
                      $"power in {story.PowerChapters} chapters\n" + string.Join("\n", story.Notes));
            Assert.IsNotNull(story.End, "the run reached an end screen");
            Assert.IsTrue(story.ReachedLucifer, "the run reached Lucifer (" + story.End + ")");
            CollectionAssert.IsSubsetOf(Kinds, story.Visited, "every kind of node was stepped on: " + string.Join(", ", story.Visited));
            Assert.GreaterOrEqual(story.Reloads, 3, "saved, reloaded and continued once a chapter");
            Assert.GreaterOrEqual(story.PowerChapters, 3, "the class's power in every chapter");
        }

        private static string State()
        {
            ChapterPresenter c = Chapters;
            if (c == null) return "no chapters";
            if (c.Panel != null) return "panel " + c.Panel.Title;
            if (c.IsBagOpen) return "bag";
            if (c.IsAtTable) return $"table {Table.Game?.Phase} hand {Table.Game?.RoundNumber} years {Table.Game?.Years}";
            if (c.IsMapOpen) return $"map ch{c.Journey?.Chapter} floor {c.Run?.Current?.Floor}";
            return "elsewhere";
        }

        private static IEnumerator Step(string classId, Story story)
        {
            ChapterPresenter c = Chapters;
            Assert.IsNotNull(c, "the chapters");
            if (c.Journey != null && c.Journey.Stage == JourneyStage.Lucifer) story.ReachedLucifer = true;
            if (c.Journey != null && c.Journey.Chapter != story.Chapter && c.Journey.Stage != JourneyStage.Lucifer)
                NewChapter(classId, story, c.Journey.Chapter);

            if (c.Panel != null)
            {
                yield return AnswerPanel(c.Panel, story);
                yield break;
            }
            if (c.IsBagOpen)
            {
                HellPokerScreenshots.Press("BagDoneButton");
                yield return null;
                yield break;
            }
            if (c.IsCompassOut)
            {
                c.Back();
                yield return null;
                yield break;
            }
            if (c.IsAtTable)
            {
                yield return PlayTable(classId, story);
                yield break;
            }
            if (c.IsMapOpen)
            {
                yield return OnTheMap(story);
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }

        private static void NewChapter(string classId, Story story, int chapter)
        {
            if (story.PowerHere) story.PowerChapters++;
            story.Notes.Add($"chapter {story.Chapter}: nodes {string.Join(" ", story.VisitedHere)}, power {(story.PowerHere ? "used" : "NOT used")}");
            story.Chapter = chapter;
            story.VisitedHere.Clear();
            story.ReloadedHere = false;
            story.PowerHere = false;
            story.KitStatAtChapter = KitStat(classId);
        }

        // ------------------------------------------------------------------ panels

        private static IEnumerator AnswerPanel(PanelCard card, Story story)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.Title), "a panel without a title");
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.Text), $"the panel '{card.Title}' has no words");
            Assert.IsTrue(card.Options.Count > 0, $"the panel '{card.Title}' has no way on");
            foreach (PanelOption option in card.Options)
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Label), $"an empty option on '{card.Title}'");

            if (card.Title == UiText.FreedTitle || card.Title == UiText.FallTitle || card.Title == UiText.ChapterDamnedTitle ||
                card.Title == UiText.PurseEmptyTitle)
            {
                story.End = card.Title;
                if (story.PowerHere) story.PowerChapters++;
                if (card.Title == UiText.PurseEmptyTitle) Assert.Fail("the purse emptied (the test keeps it full)");
                yield break;
            }
            if (card.Title == UiText.LuciferCallsTitle) story.ReachedLucifer = true;

            story.PanelPresses.TryGetValue(card.Title, out int presses);
            story.PanelPresses[card.Title] = presses + 1;
            int pick = -1;
            if (presses < 3) pick = First(card, i => card.Options[i].Enabled && !card.Options[i].IsLeave);
            if (pick < 0) pick = Last(card, i => card.Options[i].IsLeave);
            if (pick < 0) pick = Math.Max(0, Last(card, i => card.Options[i].Enabled));   // the way on is the last (ON after a camp's act)
            story.Notes.Add($"panel '{card.Title}': {card.Options[pick].Label}");
            KeepThePurse();
            HellPokerScreenshots.Press("PanelOption" + pick);
            yield return null;
            yield return new WaitForSeconds(0.05f);
        }

        private static int First(PanelCard card, Func<int, bool> which)
        {
            for (int i = 0; i < card.Options.Count; i++) if (which(i)) return i;
            return -1;
        }

        private static int Last(PanelCard card, Func<int, bool> which)
        {
            for (int i = card.Options.Count - 1; i >= 0; i--) if (which(i)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ the map

        private static IEnumerator OnTheMap(Story story)
        {
            ChapterPresenter c = Chapters;
            ChapterRun run = c.Run;
            // Once a chapter: the run is saved, the scene reloaded, CONTINUE.
            if (!story.ReloadedHere && run.Current != null)
            {
                story.ReloadedHere = true;
                story.Reloads++;
                story.Notes.Add($"reload in chapter {story.Chapter} at floor {run.Current.Floor + 1}");
                yield return LoadScene();
                HellPokerScreenshots.Press("ContinueButton");
                yield return new WaitForSeconds(0.8f);
                yield break;
            }
            List<MapNode> choices = run.Choices.ToList();
            if (choices.Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
                yield break;
            }
            MapNode next = BestChoice(run.Map, choices, story);
            story.PanelPresses.Clear();   // a new node: its panels answered afresh
            story.Visited.Add(next.Kind);
            story.VisitedHere.Add(next.Kind);
            if (next.Kind == NodeKind.Unknown) story.Notes.Add($"? held {next.Hidden}");
            KeepThePurse();
            HellPokerScreenshots.Find<Button>($"Node{next.Floor}_{next.Lane}").onClick.Invoke();
            yield return new WaitForSeconds(0.7f);
        }

        /// <summary>The step whose best path down steps on the most kinds still missing (the run's first, then the chapter's).</summary>
        private static MapNode BestChoice(ChapterMap map, List<MapNode> choices, Story story)
        {
            int Score(IEnumerable<MapNode> path)
            {
                var kinds = new HashSet<NodeKind>(path.Select(n => n.Kind));
                return kinds.Count(k => !story.Visited.Contains(k)) * 10 + kinds.Count(k => !story.VisitedHere.Contains(k));
            }
            MapNode best = choices[0];
            int bestScore = -1;
            foreach (MapNode start in choices)
            {
                int score = BestPath(map, start, new List<MapNode>(), Score);
                if (score <= bestScore) continue;
                bestScore = score;
                best = start;
            }
            return best;
        }

        private static int BestPath(ChapterMap map, MapNode node, List<MapNode> path, Func<IEnumerable<MapNode>, int> score)
        {
            path.Add(node);
            List<MapNode> next = map.NextFrom(node).ToList();
            int best = next.Count == 0 ? score(path) : next.Max(n => BestPath(map, n, path, score));
            path.RemoveAt(path.Count - 1);
            return best;
        }

        private static void KeepThePurse()
        {
            CoinPurse purse = Chapters.Run?.Purse;
            if (purse != null && purse.Coins < Purse) purse.Add(Purse - purse.Coins);
        }

        // ------------------------------------------------------------------ the tables

        private static IEnumerator PlayTable(string classId, Story story)
        {
            TableView view = ChapterJourneyTests.ChapterTableView;
            TablePresenter table = Table;
            HellPokerGame game = table.Game as HellPokerGame;
            Assert.IsNotNull(game, "a chapter's game");
            if (view.IsBusy)
            {
                view.SkipAnimations();
                yield return null;
                yield break;
            }

            bool boss = table.Floor == null;
            if (table.Floor != null && Chapters.Run.Purse.Coins < 4 * Math.Max(1, game.Ante + 4)) Chapters.Run.Purse.Add(Purse);
            if (boss) HoldTheBar(table, game, story);

            switch (game.Phase)
            {
                case GamePhase.Betting:
                    table.PerformAction();   // the deal (or the next hand)
                    break;
                case GamePhase.PlayerReveal:
                case GamePhase.DrawReveal:
                case GamePhase.HouseReveal:
                    if (game.Phase == GamePhase.PlayerReveal && game.PlayerCardsRevealed == 2)
                        TableLayoutTests.AssertNothingOverlaps(view, $"{classId} ch{story.Chapter} {(boss ? "boss" : "floor")} hand {game.RoundNumber}");
                    TryPower(classId, table, game, story);
                    if (game.Phase == GamePhase.Drawing || !game.HandInPlay) break;
                    KeepFromTheSatchel(table, game);
                    if (Evaluator.Evaluate(game.PlayerHand).Category >= Core.Evaluation.HandCategory.TwoPair && game.CanBet(BetAction.Raise, out _))
                        table.Bet(BetAction.Raise);
                    else table.PerformAction();
                    break;
                case GamePhase.Drawing:
                    foreach (int i in game.SuggestedDiscards()) table.ToggleDiscard(i);
                    TryPower(classId, table, game, story);
                    if (game.Phase == GamePhase.Drawing) table.PerformAction();   // the draw (the harvest made it already)
                    break;
                case GamePhase.HouseReRaise:
                    table.Bet(BetAction.Call);
                    break;
                case GamePhase.NamingJoker:
                    table.ConfirmJoker();
                    break;
                default:
                    KeepFromTheSatchel(table, game);
                    table.PerformAction();   // NEXT HAND, or the table's end
                    break;
            }
            yield return null;
        }

        /// <summary>A demon's bar kept under its start (the soul never burns in a test), and a table that drags on brought to its last blow.</summary>
        private static void HoldTheBar(TablePresenter table, HellPokerGame game, Story story)
        {
            if (game.Phase != GamePhase.Betting) return;
            ChapterJourney journey = Chapters.Journey;
            int start = journey.Stage == JourneyStage.Lucifer ? journey.LuciferBarStart : Chapters.Run.BossBarStart;
            if (game.Years > start)
            {
                story.Notes.Add($"the bar held: {game.Years} -> {start / 2}");
                HellPokerScreenshots.SetSentence(table, Math.Max(1, start / 2));
            }
            else if (game.RoundNumber >= 30 && game.Years > 1)
            {
                story.Notes.Add($"a long demon's table ({game.RoundNumber} hands): the last blow");
                HellPokerScreenshots.SetSentence(table, 1);
            }
        }

        private static void KeepFromTheSatchel(TablePresenter table, HellPokerGame game)
        {
            SatchelPowers satchel = table.Kit?.Satchel;
            if (satchel != null && game.IsAfterDraw && satchel.Offered.Count > 0) table.PressKitCard(0);
        }

        /// <summary>What the class's power has done so far in the run (it grows when the power is used).</summary>
        private static int KitStat(string classId)
        {
            ClassKit kit = Chapters?.Journey?.Kit;
            if (kit == null) return 0;
            return kit.Harvests + kit.Rearranges + kit.Reveals + kit.FreeRaises + kit.Seizures + kit.Summons + kit.PowerUses + JesterK;
        }

        private static int JesterK;

        /// <summary>The class's power, at its moment: the harvest in the draw, a bend of fate, a card opened for a tax (taxes given when the
        /// King has none yet), an imp's eye; the Jester's K says what his jokers are.</summary>
        private static void TryPower(string classId, TablePresenter table, HellPokerGame game, Story story)
        {
            if (story.PowerHere) return;
            ChapterHooks hooks = table.Kit;
            switch (classId)
            {
                case Core.Sinners.Peasant.ClassId:
                    if (game.Phase == GamePhase.Drawing && hooks?.Satchel != null && hooks.Kit.Satchel.Count > 0 && game.SuggestedDiscards().Count > 0)
                        table.UsePower();
                    break;
                case Core.Sinners.Warlock.ClassId:
                    if (hooks?.Prophecy != null && ProphecyPowers.IsOpen(game))
                    {
                        int seen = hooks.Prophecy.TopCards(game).Count;
                        table.PressKitArrow(seen >= 2 ? 1 : 0, seen >= 2);
                    }
                    break;
                case Core.Sinners.King.ClassId:
                    if (hooks?.Treasury != null && game.Phase == GamePhase.PlayerReveal)
                    {
                        if (hooks.Kit.Taxes == 0 && story.BossHands++ > 10) hooks.Kit.AddTax(2);   // no win yet in a while: a tax to spend
                        table.PressKitButton(0);
                    }
                    break;
                case Core.Sinners.Summoner.ClassId:
                    if (hooks?.Summon != null && game.Phase == GamePhase.PlayerReveal) table.PressKitButton(0);
                    break;
                default:
                    if (game.Phase == GamePhase.PlayerReveal)
                    {
                        table.UsePower();   // the Jester: K tells his jokers' rule
                        JesterK++;
                    }
                    break;
            }
            int now = KitStat(classId);
            if (now > story.KitStatAtChapter)
            {
                story.PowerHere = true;
                story.Notes.Add($"power used in chapter {story.Chapter} (hand {game.RoundNumber})");
            }
        }

        // ------------------------------------------------------------------ chaos

        /// <summary>
        /// Two thousand random presses: any visible button (not QUIT, not the language, not the report box's send — it would open a mail),
        /// the table's keys, the map's keys, Esc, the How to Play's pages. Afterwards the game still answers: the menu comes up, a new run
        /// starts and its first table deals. The test framework fails on any error logged on the way.
        /// </summary>
        [UnityTest, Timeout(1800000)]
        public IEnumerator Chaos_TwoThousandPresses_TheGameStillAnswers()
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString(ChapterArchive.RecordsKey, Records());
            yield return LoadScene();
            var random = new System.Random(2026);
            string[] never = { "QuitButton", "MenuLanguageButton", "FeedbackSendButton", "MenuFeedbackButton", "LanguageRow" };
            for (int press = 0; press < 2000; press++)
            {
                int roll = random.Next(100);
                ChapterPresenter c = Chapters;
                if (roll < 60)
                {
                    Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Where(b => b.isActiveAndEnabled && b.interactable && !never.Any(n => b.name.StartsWith(n, StringComparison.Ordinal))).ToArray();
                    if (buttons.Length > 0) buttons[random.Next(buttons.Length)].onClick.Invoke();
                }
                else if (roll < 85 && c != null && c.IsAtTable)
                {
                    ITableCommands t = c.Table;
                    switch (random.Next(8))
                    {
                        case 0: t.PerformAction(); break;
                        case 1: t.Bet(BetAction.Raise); break;
                        case 2: t.CheckToDraw(); break;
                        case 3: t.UsePower(); break;
                        case 4: t.Bet(BetAction.Call); break;
                        case 5: t.Bet(BetAction.Fold); break;
                        case 6: t.ToggleDiscard(random.Next(5)); break;
                        default: t.ToggleHandRanks(); break;
                    }
                }
                else if (roll < 92 && c != null && c.IsMapOpen)
                {
                    if (random.Next(2) == 0) c.Step(random.Next(3) - 1, random.Next(3) - 1);
                    else c.Confirm();
                }
                else if (roll < 96)
                {
                    ChapterJourneyTests.Menu.TurnRulesPage(random.Next(2) == 0 ? 1 : -1);
                }
                else
                {
                    if (c == null || !c.Back()) ChapterJourneyTests.Menu.GoBack();
                }
                if (Chapters?.Run?.Purse != null && Chapters.Run.Purse.Coins < 20) Chapters.Run.Purse.Add(100);
                yield return null;
                if (press % 50 == 0) yield return new WaitForSeconds(0.3f);   // the screen changes finish now and then
            }

            // Still answering: back to the menu, a new run, its first table deals.
            yield return new WaitForSeconds(1f);
            for (int guard = 0; guard < 10 && !ChapterJourneyTests.Menu.IsAtMenuRoot; guard++)
            {
                if (Chapters != null && Chapters.IsMapOpen) HellPokerScreenshots.Find<Button>("MapMenuButton").onClick.Invoke();
                else ChapterJourneyTests.Menu.GoBack();
                yield return new WaitForSeconds(0.7f);
            }
            Assert.IsTrue(ChapterJourneyTests.Menu.IsAtMenuRoot, "the title menu came back");
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            HellPokerScreenshots.ChooseSinner(0);
            yield return new WaitForSeconds(0.8f);
            HellPokerScreenshots.Press("PanelOption0");
            yield return null;
            MapNode start = Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            Assert.IsTrue(Chapters.IsAtTable);
            Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            Assert.AreNotEqual(GamePhase.Betting, Table.Game.Phase, "the new run's first hand is dealt");
        }
    }
}
