using UnityEditor;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// Import settings for the generated sounds (see Tools/AudioGen): mono, Vorbis. The effects are decompressed on load
    /// (short, played often, no delay); the music loops stream from disk (long, one at a time).
    /// </summary>
    public sealed class HellPokerAudioImporter : AssetPostprocessor
    {
        private const string SfxFolder = "Assets/Resources/Audio/Sfx/";
        private const string MusicFolder = "Assets/Resources/Audio/Music/";

        private void OnPreprocessAudio()
        {
            bool sfx = assetPath.StartsWith(SfxFolder);
            bool music = assetPath.StartsWith(MusicFolder);
            if (!sfx && !music) return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = sfx;
            importer.defaultSampleSettings = settings;
        }
    }
}
