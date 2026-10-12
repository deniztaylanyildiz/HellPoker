namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// The game's sound: one-shot effects, a music loop per screen or demon, and a tense layer under it while the soul is on
    /// the table. Presenters say which sound belongs to which moment, as they do for animations; the implementation plays it.
    /// The animation speed setting never speeds sounds up.
    /// </summary>
    public interface IAudio
    {
        /// <summary>Plays an effect (<see cref="SfxIds"/>).</summary>
        void PlaySfx(string id);

        /// <summary>The music loop: a demon's id, <see cref="SfxIds.MenuMusic"/>, or null for silence. The same track keeps playing.</summary>
        void PlayMusic(string trackId);

        /// <summary>The soul's layer (a drone and a heartbeat) over the music, on or off.</summary>
        void SetSoulLayer(bool on);

        /// <summary>Animations were hurried along: long effects (the gong, the summons...) stop with them.</summary>
        void CutLong();

        /// <summary>Volumes, 0 (silent) to 1. The music and the effects are each multiplied by <paramref name="master"/>.</summary>
        void SetVolumes(float master, float music, float sfx);
    }

    /// <summary>The effects (and the menu's music) by id: the file names under Resources/Audio.</summary>
    public static class SfxIds
    {
        public const string Deal = "deal";
        public const string Flip = "flip";
        public const string Chip = "chip";
        public const string WinSmall = "win_small";
        public const string WinBig = "win_big";
        public const string Loss = "loss";
        public const string Sealed = "sealed";
        public const string Cheat = "cheat";
        public const string Backfire = "backfire";
        public const string Soul = "soul";
        public const string Summoned = "summoned";
        public const string Fall = "fall";
        public const string Click = "click";
        public const string Transition = "transition";

        // Phase 2: the chapters.
        public const string CoinWin = "coin_win";
        public const string CoinWinBig = "coin_win_big";
        public const string CoinLoss = "coin_loss";
        public const string CoinLossBig = "coin_loss_big";
        public const string PurseEmpty = "purse_empty";
        public const string BarHit = "bar_hit";
        public const string BarGrow = "bar_grow";
        public const string SoulWarning = "soul_warning";
        public const string NodeSelect = "node_select";
        public const string ChapterTitle = "chapter_title";
        public const string Loot = "loot";
        public const string MarketBuy = "market_buy";
        public const string Fire = "fire";
        public const string EventOpen = "event_open";
        public const string SatchelPut = "satchel_put";
        public const string SatchelTake = "satchel_take";
        public const string ProphecySee = "prophecy_see";
        public const string ProphecyBottom = "prophecy_bottom";
        public const string TaxGain = "tax_gain";
        public const string TaxSpend = "tax_spend";
        public const string Seize = "seize";
        public const string RuleInterest = "rule_interest";
        public const string RuleSwap = "rule_swap";
        public const string RuleTemptation = "rule_temptation";
        public const string RuleMirror = "rule_mirror";

        public const string MenuMusic = "menu";
        public const string SoulLayer = "soul_layer";

        // Phase 2's music: a chapter's map and its floors' tables (by the chapter's demon), Lucifer's call, the two endings.
        public static string MapMusic(string bossId) => "map_" + bossId;
        public static string FloorMusic(string bossId) => "floor_" + bossId;
        public const string LuciferCallMusic = "lucifer_call";
        public const string FreedMusic = "end_free";
        public const string DoomMusic = "end_doom";

        /// <summary>Effects long enough to be cut when the player hurries the table along.</summary>
        public static bool IsLong(string id) => id == Sealed || id == Soul || id == Summoned || id == Fall || id == WinBig || id == PurseEmpty
                                                 || id == ChapterTitle || id == Fire;
    }

    /// <summary>No sound at all (batch runs, tests).</summary>
    public sealed class NullAudio : IAudio
    {
        public static readonly NullAudio Instance = new NullAudio();

        public void PlaySfx(string id) { }
        public void PlayMusic(string trackId) { }
        public void SetSoulLayer(bool on) { }
        public void CutLong() { }
        public void SetVolumes(float master, float music, float sfx) { }
    }
}