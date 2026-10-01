using System;
using HellPoker.Core.Game;

namespace HellPoker.Presentation.Settings
{
    /// <summary>
    /// Keeps the run in progress and the records between sessions, in an <see cref="ISettingsStore"/>. A save that cannot be
    /// read (garbled, another version) is thrown away and the game starts as if there were none.
    /// </summary>
    public sealed class RunArchive
    {
        public const string RunKey = "run.save";
        public const string RecordsKey = "run.records";

        private readonly ISettingsStore _store;

        public RunArchive(ISettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <returns>The saved run, or null when there is none or it is unreadable (and then it is deleted).</returns>
        public RunSnapshot LoadRun()
        {
            string text = _store.GetString(RunKey, null);
            if (string.IsNullOrEmpty(text)) return null;
            if (RunSnapshot.TryDecode(text, out RunSnapshot snapshot)) return snapshot;

            ClearRun();
            return null;
        }

        public void SaveRun(RunSnapshot snapshot)
        {
            _store.SetString(RunKey, snapshot.Encode());
            _store.Save();
        }

        public void ClearRun()
        {
            _store.Delete(RunKey);
            _store.Save();
        }

        public RecordBook LoadRecords() => RecordBook.Decode(_store.GetString(RecordsKey, null));

        public void SaveRecords(RecordBook records)
        {
            _store.SetString(RecordsKey, records.Encode());
            _store.Save();
        }
    }
}
