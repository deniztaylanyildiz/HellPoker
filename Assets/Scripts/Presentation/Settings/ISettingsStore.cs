using System.Collections.Generic;
using UnityEngine;

namespace HellPoker.Presentation.Settings
{
    /// <summary>Somewhere small values survive between sessions (PlayerPrefs in the game, memory in tests).</summary>
    public interface ISettingsStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        void Delete(string key);
        void Save();
    }

    public sealed class PlayerPrefsStore : ISettingsStore
    {
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }

    public sealed class MemoryStore : ISettingsStore
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public int Saves { get; private set; }

        public int GetInt(string key, int fallback) =>
            _values.TryGetValue(key, out string value) && int.TryParse(value, out int number) ? number : fallback;

        public void SetInt(string key, int value) => _values[key] = value.ToString();
        public string GetString(string key, string fallback) => _values.TryGetValue(key, out string value) ? value : fallback;
        public void SetString(string key, string value) => _values[key] = value;
        public void Delete(string key) => _values.Remove(key);
        public void Save() => Saves++;

        public void Clear() => _values.Clear();
    }
}
