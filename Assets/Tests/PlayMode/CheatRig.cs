using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using UnityEngine;
using static HellPoker.PlayMode.Tests.HellPokerScreenshots;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Puts a chosen cheat on the live table behind the game's back: the demon plays it every hand, on a stacked deck.
    /// Shared by the cheat journeys and the cheat screenshots.
    /// </summary>
    internal static class CheatRig
    {
        public const string Flush = "2C 9C JC 4C KC";
        public const string HouseTwos = "2D 2H 5S 7H 9D";
        public const string Rest = "3S 6D 8S 2S 3H QD QC 8H 7C 6H 4S 5C";

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            private readonly ICheat _shown;
            public OnlyCheat(ICheat cheat, ICheat shown) { _cheat = cheat; _shown = shown; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, _shown);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private sealed class NoShuffle : IShuffler
        {
            public void Shuffle<T>(IList<T> items) { }
        }

        /// <summary>Dice that always land on the first option (every "this often" roll succeeds).</summary>
        public sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        public static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", Flags).GetValue(Object.FindFirstObjectByType<HellPokerBootstrap>());

        /// <summary>Between hands: this cheat every hand (announced as <paramref name="shown"/>), on a stacked deck.</summary>
        /// <param name="backfirePercent">How often a slippery cheat slips (Belial's tongue).</param>
        /// <param name="random">The cheat's dice; a fixed seed by default.</param>
        public static void Force(ICheat cheat, ICheat shown = null, string deck = null, int backfirePercent = 0, IRandomSource random = null)
        {
            object game = Presenter.Game;
            Type type = game.GetType();
            random = random ?? new SystemRandomSource(3);
            type.GetField("_cheats", Flags).SetValue(game, new CheatSession(new OnlyCheat(cheat, shown), 1, random, null, backfirePercent));
            type.GetField("_cheatRandom", Flags).SetValue(game, random);
            type.GetField("_deck", Flags).SetValue(game, new Deck(new NoShuffle(), Cards(deck ?? $"{Flush} {HouseTwos} {Rest}").Reverse()));
        }

        public static IEnumerable<Card> Cards(string notation)
        {
            foreach (string token in notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string rank = token.Substring(0, token.Length - 1);
                Rank r = rank == "A" ? Rank.Ace : rank == "K" ? Rank.King : rank == "Q" ? Rank.Queen : rank == "J" ? Rank.Jack : (Rank)int.Parse(rank);
                char s = token[token.Length - 1];
                yield return new Card(r, s == 'C' ? Suit.Clubs : s == 'D' ? Suit.Diamonds : s == 'H' ? Suit.Hearts : Suit.Spades);
            }
        }

        /// <summary>A new run at this demon's table (0 Mammon, 1 Belial, 2 Lilith), through the menu's own buttons.</summary>
        public static IEnumerator SitWith(int dealer)
        {
            if (Presenter.Game != null && Presenter.Game.Phase != GamePhase.Betting && !Presenter.Game.IsGameOver)
            {
                yield return PlayToShowdown();
                yield return WaitForTable();
            }
            Press(Object.FindFirstObjectByType<MainMenuView>().IsVisible ? "NewGameButton" : "MenuButton");
            if (!Object.FindFirstObjectByType<DealerSelectView>().IsVisible) Press("NewGameButton");
            yield return new WaitForSeconds(0.3f);
            Press("ChooseDealer" + dealer);
            yield return WaitForTable();
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>
        /// Passes (or raises where passing is barred, or calls) until the game reaches <paramref name="phase"/>; the last press
        /// is not waited for, so the blow that comes with it can be caught.
        /// </summary>
        public static IEnumerator BetUntil(GamePhase phase)
        {
            for (int guard = 0; guard < 10 && Presenter.Game.Phase != phase; guard++)
            {
                yield return WaitForTable();
                if (IsActive("PassButton") && !IsLocked("PassButton")) Press("PassButton");
                else if (IsActive("CallButton")) Press("CallButton");
                else if (IsActive("RaiseButton")) Press("RaiseButton");
                else break;
            }
        }

        /// <summary>The player's card on screen at this position.</summary>
        public static CardView PlayerCard(int index) => Find<Transform>("PlayerHand").GetComponentsInChildren<CardView>()[index];

        /// <summary>True once every House card shows its face on screen (the showdown is in front of the player).</summary>
        public static bool HouseAllFaceUp => Find<Transform>("HouseHand").GetComponentsInChildren<CardView>().All(c => c.IsFaceUp);

        /// <summary>
        /// Watches every frame of a hand: a card kept from the player (veiled, moonless, swapped in) may not show its face on
        /// screen — not in a deal, a flip, a cheat's blow or a sealed hand playing out — until the hand is over and the House's
        /// cards are all face up. Start it before the deal; read <see cref="DarkWatch.Violation"/> at the end.
        /// </summary>
        public sealed class DarkWatch : MonoBehaviour
        {
            private readonly HashSet<Card> _hidden = new HashSet<Card>();

            /// <summary>The first face seen too early; null while the dark holds.</summary>
            public string Violation { get; private set; }

            /// <summary>True once the showdown is on screen (the watch is over).</summary>
            public bool Done { get; private set; }

            /// <summary>How many cards were kept from the player this hand.</summary>
            public int HiddenSeen => _hidden.Count;

            private void LateUpdate()
            {
                if (Done) return;
                IHellPokerGame game = Presenter.Game;
                if (game.PlayerHand != null)
                    for (int i = 0; i < Core.Cards.Hand.Size; i++)
                        if (game.WasPlayerCardHidden(i)) _hidden.Add(game.PlayerHand[i]);

                bool settled = game.Phase != GamePhase.Betting && !IsInPlay(game.Phase);
                if (settled && HouseAllFaceUp)
                {
                    Done = true;
                    return;
                }

                for (int i = 0; i < Core.Cards.Hand.Size && Violation == null; i++)
                {
                    Card? face = PlayerCard(i).FaceShown;
                    if (face.HasValue && _hidden.Contains(face.Value))
                        Violation = $"{face.Value} (card {i}) showed its face before the showdown.";
                }
            }

            private static bool IsInPlay(GamePhase phase) =>
                phase == GamePhase.PlayerReveal || phase == GamePhase.Drawing || phase == GamePhase.DrawReveal ||
                phase == GamePhase.HouseReveal || phase == GamePhase.HouseReRaise;
        }

        public static DarkWatch WatchTheDark() => new GameObject("DarkWatch").AddComponent<DarkWatch>();

        /// <summary>Waits for the watch to see the showdown, then checks it held.</summary>
        public static IEnumerator AssertTheDarkHeld(DarkWatch watch)
        {
            float started = Time.time;
            while (!watch.Done && Time.time - started < 20f)
                yield return null;
            NUnit.Framework.Assert.IsTrue(watch.Done, "The showdown never came on screen.");
            NUnit.Framework.Assert.Greater(watch.HiddenSeen, 0, "Nothing was kept from the player: the test proves nothing.");
            NUnit.Framework.Assert.IsNull(watch.Violation, watch.Violation);
            Object.Destroy(watch.gameObject);
        }

        /// <summary>The sign above the demon (null when nothing is announced).</summary>
        public static Presentation.Abstractions.CheatCard Intent => Object.FindFirstObjectByType<MaliceView>().Intent;
    }
}
