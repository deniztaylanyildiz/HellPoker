using System;
using HellPoker.Core.Game;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A Phase 2 run's sentence in years: the class's whole sentence as the run begins (<see cref="StartFor"/>), carried from the first floor to Lucifer and shown as a number. It moves with the demons' bars but is not one: a won hand
    /// takes its years off both, a lost one puts its whole cost on the sentence and only <see cref="BossTable.BarHealPercent"/> of it on the
    /// bar (<see cref="HellPokerGame.BarHealPercent"/>); years put on outside a hand (the tribute's, an offer's) go on both. Never below
    /// zero; what is left when the run walks free is its score.
    /// </summary>
    public sealed class RunSentence
    {
        public int Years { get; private set; }

        public RunSentence(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = years;
        }

        /// <summary>The sentence a run as <paramref name="classId"/> begins with: the four shares (Demo 3.4: the same at every difficulty).</summary>
        public static int StartFor(string classId) => Math.Max(1, BossShares.Total(classId));

        public void Add(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years += years;
        }

        public void Strike(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = Math.Max(0, Years - years);
        }

        /// <summary>Moves by <paramref name="years"/>: up when positive, down when negative.</summary>
        public void Move(int years)
        {
            if (years >= 0) Add(years);
            else Strike(-years);
        }

        /// <summary>The sentence follows a demon's table: every year off its bar comes off, every year on it goes on — and a loss's
        /// unhealed part (what the bar did not take of it) goes on as well. Undone by <see cref="Unfollow"/>.</summary>
        public void Follow(HellPokerGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Unfollow();
            _followed = game;
            game.YearsMoved += Move;
            game.YearsUnhealed += Add;
        }

        public void Unfollow()
        {
            if (_followed == null) return;
            _followed.YearsMoved -= Move;
            _followed.YearsUnhealed -= Add;
            _followed = null;
        }

        private HellPokerGame _followed;
    }
}
