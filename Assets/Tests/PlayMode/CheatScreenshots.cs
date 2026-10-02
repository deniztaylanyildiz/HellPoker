using System.Collections;
using HellPoker.Core.Cheats;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static HellPoker.PlayMode.Tests.CheatRig;
using static HellPoker.PlayMode.Tests.HellPokerScreenshots;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Not a real test: the demons' cheats on screen — every demon's gauge and announced intent, every cheat's blow on the
    /// cards (caught mid-blow), Belial's lie coming out and The Fall. Each cheat is forced onto a stacked deck behind the
    /// game's back. Explicit; run with -testFilter HellPoker.PlayMode.Tests.CheatScreenshots. Output as HellPokerScreenshots.
    /// </summary>
    [Explicit, Category("Screenshots")]
    public class CheatScreenshots
    {
        /// <summary>Deals and goes on to the cheat's moment; the shot is taken just as it strikes.</summary>
        private static IEnumerator StrikeAndShoot(ICheat cheat, string shot, ICheat shown = null, string deck = null, int[] discards = null)
        {
            Force(cheat, shown, deck);
            Press("ActionButton");   // deal
            float wait = 0.25f;
            if (cheat.Timing != CheatTiming.AfterDeal)
            {
                yield return WaitForTable();
                Press("CheckToDrawButton");
                if (cheat.Timing != CheatTiming.BeforeDraw)
                {
                    yield return WaitForTable();
                    foreach (int i in discards ?? new int[0])
                        Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[i]);
                    yield return WaitForTable();
                    Press("ActionButton");   // draw
                    if (cheat.Timing == CheatTiming.HouseReveal)
                    {
                        yield return BetUntil(GamePhase.HouseReveal);
                        wait = 0.9f;
                    }
                    else if (cheat.Timing == CheatTiming.BeforeShowdown)
                    {
                        yield return BetUntil(GamePhase.RoundOver);
                        wait = 0.35f;
                    }
                }
            }
            yield return WaitUntilStrike(wait);
            yield return Shot(shot);
            yield return WaitForTable();
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>Lets the queue run up to the blow (the effect's own wait is short).</summary>
        private static IEnumerator WaitUntilStrike(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        private static IEnumerator Finish()
        {
            yield return PlayToShowdown();
            yield return WaitForTable();
            // Keep the ordinary tables far from Lucifer's gate (the stacked flushes win big).
            if (!Presenter.IsAtFinalTable && Presenter.Game.Phase == GamePhase.RoundOver)
                SetSentence(Presenter, 1000);
            yield return WaitForTable();
            if (ActionLabelIs("NEXT HAND")) Press("ActionButton");
            yield return WaitForTable();
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureCheats()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);

            // ---------------------------------------------------------------- Mammon: gauge, intent, his three cheats
            yield return SitWith(0);
            Force(new CollateralCheat());
            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("40_mammon_intent");
            Press("CheckToDrawButton");
            yield return new WaitForSeconds(0.25f);
            yield return Shot("41_collateral");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.4f);
            yield return Shot("41b_collateral_chained");
            yield return Finish();

            yield return StrikeAndShoot(new BuyoutCheat(), "42_buyout");
            yield return Finish();
            yield return StrikeAndShoot(new TitheCheat(), "43_tithe");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("43b_tithe_result");
            yield return Finish();

            // ---------------------------------------------------------------- Belial: his cheats and a lie
            yield return SitWith(1);
            Force(new FalseFaceCheat());
            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("44_belial_intent");
            yield return Finish();
            yield return StrikeAndShoot(new FalseFaceCheat(), "45_false_face");
            yield return Finish();
            yield return StrikeAndShoot(new ForkedTongueCheat(), "46_forked_tongue");
            yield return Finish();
            yield return StrikeAndShoot(new SerpentSwapCheat(), "47_serpent_swap");
            yield return Finish();
            yield return StrikeAndShoot(new CollateralCheat(), "48_belial_lie", shown: new ThornCheat());
            yield return Finish();

            // ---------------------------------------------------------------- Lilith
            yield return SitWith(2);
            Force(new NightVeilCheat());
            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("49_lilith_intent");
            Press("CheckToDrawButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("50_night_veil");
            yield return Finish();
            yield return StrikeAndShoot(new ThornCheat(), "51_thorn");
            yield return Finish();
            yield return StrikeAndShoot(new MoonlessCheat(), "52_moonless", discards: new[] { 0, 1 });
            yield return WaitForTable();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("52b_moonless_dark");
            yield return Finish();

            // ---------------------------------------------------------------- Lucifer: summoned at 150, his cheats and The Fall
            SetSentence(Presenter, 150);
            yield return WaitForTable();
            yield return new WaitForSeconds(1f);
            Force(new TheFallCheat());
            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("53_lucifer_fall_awaits");
            Press("CheckToDrawButton");
            yield return WaitForTable();
            Press("ActionButton");   // stand pat
            yield return BetUntil(GamePhase.RoundOver);   // to the showdown: the flush wins — and falls
            yield return new WaitForSeconds(0.35f);
            yield return Shot("54_the_fall");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("54b_the_fall_result");
            yield return Finish();

            // His minor cheats on a losing pair of kings (a win here would absolve the run).
            yield return SummonLucifer();
            yield return StrikeAndShoot(new GazeCheat(), "55_gaze", deck: LuciferDeck);
            yield return Finish();
            yield return SummonLucifer();
            yield return StrikeAndShoot(new BurningCardCheat(), "56_burning_card", deck: LuciferDeck);
            yield return Finish();
            yield return SummonLucifer();
            yield return StrikeAndShoot(new RewriteCheat(), "57_rewrite", deck: LuciferDeck);
            yield return Finish();
        }

        private const string LuciferDeck = "KC KD 7H 4S 2D AS AH 5D 6C 9H 3S 6D 8S 2S 3H 10D 10C 8H 7C 6H JS 5S";

        /// <summary>Back to 150 years between hands: at the final table, or summoned there again after a cast down.</summary>
        private static IEnumerator SummonLucifer()
        {
            SetSentence(Presenter, 150);
            yield return WaitForTable();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(Presenter.IsAtFinalTable, "Lucifer should be at the table.");
        }
    }
}
