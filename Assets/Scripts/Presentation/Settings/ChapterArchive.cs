using System;
using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Settings
{
    /// <summary>
    /// Phase 2's run and records between sessions, in an <see cref="ISettingsStore"/> — under keys of their own, apart from the demo's
    /// (<see cref="RunArchive"/>). A save that cannot be read is thrown away.
    /// </summary>
    public sealed class ChapterArchive
    {
        public const string RunKey = "phase2.save";
        public const string RecordsKey = "phase2.records";

        private readonly ISettingsStore _store;

        public ChapterArchive(ISettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool HasRun => !string.IsNullOrEmpty(_store.GetString(RunKey, null));

        /// <returns>The saved run, or null when there is none or it is unreadable (and then it is deleted).</returns>
        public ChapterSave LoadRun()
        {
            string text = _store.GetString(RunKey, null);
            if (string.IsNullOrEmpty(text)) return null;
            if (ChapterSave.TryDecode(text, out ChapterSave save)) return save;
            ClearRun();
            return null;
        }

        public void SaveRun(ChapterSave save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            _store.SetString(RunKey, save.Encode());
            _store.Save();
        }

        public void ClearRun()
        {
            _store.Delete(RunKey);
            _store.Save();
        }

        public ChapterRecords LoadRecords() => ChapterRecords.Decode(_store.GetString(RecordsKey, null));

        public void SaveRecords(ChapterRecords records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            _store.SetString(RecordsKey, records.Encode());
            _store.Save();
        }
    }
}
