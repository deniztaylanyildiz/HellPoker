using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HellPoker.Core.Game;
using UnityEngine;

namespace HellPoker.Presentation.Settings
{
    /// <summary>Where finished (or interrupted) run logs go. Writing must never stop the game.</summary>
    public interface IRunLogSink
    {
        /// <summary>The game's version, for the log's header.</summary>
        string Version { get; }

        void Write(RunLog log);
    }

    /// <summary>
    /// Run logs as text files in the player's save folder (Application.persistentDataPath/runs, next to Player.log's folder):
    /// run-&lt;date-time&gt;.txt, the newest <see cref="Kept"/> kept. Any failure is swallowed with a warning in Player.log.
    /// </summary>
    public sealed class FileRunLogSink : IRunLogSink
    {
        public const int Kept = 50;

        private readonly string _folder;

        public string Version { get; }

        public FileRunLogSink(string folder, string version)
        {
            _folder = folder;
            Version = version;
        }

        public string Folder => _folder;

        public void Write(RunLog log)
        {
            if (log == null) return;
            try
            {
                Directory.CreateDirectory(_folder);
                File.WriteAllText(Path.Combine(_folder, log.FileName), log.ToText(), new UTF8Encoding(true));
                Prune();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: the run log could not be written ({exception.GetType().Name}: {exception.Message}).");
            }
        }

        /// <summary>Only the newest <see cref="Kept"/> logs stay (the names sort by time).</summary>
        private void Prune()
        {
            foreach (string old in Directory.GetFiles(_folder, "run-*.txt").OrderByDescending(Path.GetFileName, StringComparer.Ordinal).Skip(Kept))
            {
                try { File.Delete(old); }
                catch (Exception exception) { Debug.LogWarning($"Hell Poker: an old run log could not be removed ({exception.Message})."); }
            }
        }
    }

    /// <summary>Run logs kept in memory (tests, batch runs): the last text written per file name.</summary>
    public sealed class MemoryRunLogSink : IRunLogSink
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();

        public string Version { get; set; } = "test";

        /// <summary>Makes every write fail, to prove the game goes on.</summary>
        public bool Broken { get; set; }

        public void Write(RunLog log)
        {
            if (Broken) throw new IOException("The disk is full of the damned.");
            Files[log.FileName] = log.ToText();
        }
    }
}
