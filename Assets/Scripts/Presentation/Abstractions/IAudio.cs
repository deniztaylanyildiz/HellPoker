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

        /// <summary>Volumes, 0 (silent) to 1.</summary>
        void SetVolumes(float music, float sfx);
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

        public const string MenuMusic = "menu";
        public const string SoulLayer = "soul_layer";

        /// <summary>Effects long enough to be cut when the player hurries the table along.</summary>
        public static bool IsLong(string id) => id == Sealed || id == Soul || id == Summoned || id == Fall || id == WinBig;
    }

    /// <summary>No sound at all (batch runs, tests).</summary>
    public sealed class NullAudio : IAudio
    {
        public static readonly NullAudio Instance = new NullAudio();

        public void PlaySfx(string id) { }
        public void PlayMusic(string trackId) { }
        public void SetSoulLayer(bool on) { }
        public void CutLong() { }
        public void SetVolumes(float music, float sfx) { }
    }
}