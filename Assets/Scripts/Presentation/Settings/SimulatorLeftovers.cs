namespace HellPoker.Presentation.Settings
{
    /// <summary>
    /// Demo 3.4.1: the Boss Simulator is gone from the game. A player who played it still has its run ("run.save") and its records
    /// ("run.records") in the store: they are dropped at launch, never read (a garbled one cannot crash anything). The game's own save
    /// (<see cref="ChapterArchive"/>: "phase2.save", "phase2.records") and the settings stay as they are.
    /// </summary>
    public static class SimulatorLeftovers
    {
        /// <summary>The simulator's keys (the old demo's <see cref="RunArchive"/>).</summary>
        public static readonly string[] Keys = { RunArchive.RunKey, RunArchive.RecordsKey };

        /// <returns>True when something was there and is gone now.</returns>
        public static bool Forget(ISettingsStore store)
        {
            if (store == null) return false;
            bool found = false;
            foreach (string key in Keys)
            {
                if (store.GetString(key, null) == null) continue;
                store.Delete(key);
                found = true;
            }
            if (found) store.Save();
            return found;
        }
    }
}
