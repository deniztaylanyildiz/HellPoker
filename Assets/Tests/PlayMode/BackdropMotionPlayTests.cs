using System.Collections;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using HellPoker.Core.Chapters;
using static HellPoker.PlayMode.Tests.HellPokerScreenshots;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// The backdrops move on the real screens: the title screen and every hall show their layers and particles over the
    /// still picture, the layers step through their frames, and particles only ever sit on whole pixels.
    /// </summary>
    public class BackdropMotionPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.3f);
        }

        private static SalonView Salon => Object.FindObjectsByType<SalonView>(FindObjectsSortMode.None)
            .First(s => s.GetComponentInParent<TableView>() != null);

        [UnityTest]
        public IEnumerator TheTitleScreen_Moves()
        {
            BackdropMotionView menu = Object.FindObjectsByType<BackdropMotionView>(FindObjectsSortMode.None)
                .First(v => v.GetComponentInParent<MainMenuView>() != null);
            Assert.Greater(menu.LayerImages, 5, "His eyes, the serpent, the fire's edges, the glints.");
            Assert.Greater(menu.ParticleCount, 0, "Embers.");

            Image eyes = menu.GetComponentsInChildren<Image>().First();
            Sprite before = eyes.sprite;
            yield return new WaitForSeconds(0.4f);
            Assert.AreNotSame(before, eyes.sprite, "The layers step through their frames.");
        }

        /// <summary>Each demon's hall at his own table (the game's: Mammon's, Belial's, Lilith's), then Lucifer's.</summary>
        [UnityTest]
        public IEnumerator EveryHall_HasItsMotion_OnWholePixels()
        {
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                yield return ChapterJourneyTests.LoadWithSave(new ChapterSave
                {
                    ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = chapter, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                    BossBar = 1600, NodeDone = false
                });
                yield return WaitForTable();
                BackdropMotionView motion = Salon.MotionView;
                Assert.Greater(motion.LayerImages, 0, $"Hall {chapter}: nothing moves.");
                yield return new WaitForSeconds(0.5f);
                foreach (RectTransform rect in motion.GetComponentsInChildren<RectTransform>().Where(r => r != motion.transform))
                {
                    Vector2 p = rect.anchoredPosition;
                    Assert.AreEqual(Mathf.Round(p.x), p.x, $"{rect.name} off the pixel grid.");
                    Assert.AreEqual(Mathf.Round(p.y), p.y, $"{rect.name} off the pixel grid.");
                }
            }

            // Lucifer's hall: chains and embers (his call answered).
            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 3, "lucifer"));
            Press("PanelOption0");
            yield return new WaitForSeconds(1.5f);
            yield return WaitForTable();
            Assert.AreEqual(JourneyStage.Lucifer, ChapterJourneyTests.Chapters.Journey.Stage);
            Assert.Greater(Salon.MotionView.LayerImages, 0);
            Assert.Greater(Salon.MotionView.ParticleCount, 0, "Embers rise in his hall.");
        }
    }
}
