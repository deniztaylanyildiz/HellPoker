namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// Demo 3.3: the run that waits, in short, for the title menu's card beside CONTINUE — who the player is, where they stand, what they
    /// carry. Null: no run waits (no card).
    /// </summary>
    public sealed class ContinueCard
    {
        public string ClassId { get; }

        /// <summary>The chapter (1-3); 4 at Lucifer's.</summary>
        public int Chapter { get; }

        /// <summary>The floor the run stands on (1 up); 0 before the first step.</summary>
        public int Floor { get; }

        public int Coins { get; }

        /// <summary>The run's sentence in years; -1 when the save did not keep it.</summary>
        public int Sentence { get; }

        public int Circle { get; }

        public ContinueCard(string classId, int chapter, int floor, int coins, int sentence, int circle)
        {
            ClassId = classId;
            Chapter = chapter;
            Floor = floor;
            Coins = coins;
            Sentence = sentence;
            Circle = circle;
        }
    }
}
