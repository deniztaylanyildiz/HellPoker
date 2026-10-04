using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>
    /// The game's sound in Unity: a small pool of AudioSources for effects (round robin, so overlapping effects never cut
    /// each other short), one looping source for the music and one for the soul's layer. Clips load from Resources/Audio
    /// (Sfx/&lt;id&gt;, Music/&lt;id&gt;) on first use; a missing clip is simply silent.
    /// </summary>
    public sealed class UnityAudio : MonoBehaviour, IAudio
    {
        private const int Voices = 8;

        private readonly List<AudioSource> _sfx = new List<AudioSource>();
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private AudioSource _music;
        private AudioSource _soul;
        private string _track;
        private int _next;
        private float _sfxVolume = 0.7f;

        public static UnityAudio Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var audio = go.AddComponent<UnityAudio>();
            for (int i = 0; i < Voices; i++)
                audio._sfx.Add(audio.Source(false));
            audio._music = audio.Source(true);
            audio._soul = audio.Source(true);
            return audio;
        }

        private AudioSource Source(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioClip Clip(string folder, string id)
        {
            string key = folder + "/" + id;
            if (!_clips.TryGetValue(key, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + key);
                _clips[key] = clip;
            }
            return clip;
        }

        public void PlaySfx(string id)
        {
            AudioClip clip = string.IsNullOrEmpty(id) ? null : Clip("Sfx", id);
            if (clip == null || _sfxVolume <= 0f) return;
            AudioSource source = _sfx[_next];
            _next = (_next + 1) % _sfx.Count;
            source.clip = clip;
            source.volume = _sfxVolume;
            source.Play();
        }

        public void PlayMusic(string trackId)
        {
            if (trackId == _track) return;
            _track = trackId;
            AudioClip clip = string.IsNullOrEmpty(trackId) ? null : Clip("Music", trackId);
            _music.Stop();
            _music.clip = clip;
            if (clip != null) _music.Play();
        }

        public void SetSoulLayer(bool on)
        {
            if (on == _soul.isPlaying) return;
            if (!on)
            {
                _soul.Stop();
                return;
            }
            _soul.clip = Clip("Music", SfxIds.SoulLayer);
            if (_soul.clip != null) _soul.Play();
        }

        public void CutLong()
        {
            foreach (AudioSource source in _sfx)
                if (source.isPlaying && source.clip != null && SfxIds.IsLong(source.clip.name))
                    source.Stop();
        }

        public void SetVolumes(float music, float sfx)
        {
            _sfxVolume = Mathf.Clamp01(sfx);
            _music.volume = Mathf.Clamp01(music);
            _soul.volume = Mathf.Clamp01(music);
        }
    }
}