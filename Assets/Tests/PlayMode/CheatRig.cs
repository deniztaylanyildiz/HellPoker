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

        public static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", Flags).GetValue(Object.FindFirstObjectByType<HellPokerBootstrap>());

        /// <summary>Between hands: this cheat every hand (announced as <paramref name="shown"/>), on a stacked deck.</summary>
        public static void Force(ICheat cheat, ICheat shown = null, string deck = null)
        {
            object game = Presenter.Game;
            Type type = game.GetType();
            var random = new SystemRandomSource(3);
            type.GetField("_cheats", Flags).SetValue(game, new CheatSession(new OnlyCheat(cheat, shown), 1, random));
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

        /// <summary>The sign above the demon (null when nothing is announced).</summary>
        public static Presentation.Abstractions.CheatCard Intent => Object.FindFirstObjectByType<MaliceView>().Intent;
    }
}
