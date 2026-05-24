using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Presentation
{
    // 실제 Unity AudioSource 어댑터. NoopAudio 대체용.
    // Inspector 에서 ResourceId → AudioClip 매핑 등록.
    // BGM / SFX 카테고리별 AudioSource 분리 (음소거 토글 위해).
    public sealed class UnityAudio : MonoBehaviour, IArkanoidAudio
    {
        [System.Serializable]
        private struct ClipEntry { public string ResourceId; public AudioClip Clip; }

        [SerializeField] private ClipEntry[] clipTable;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        private readonly Dictionary<string, AudioClip> _clips = new();
        // cueId → 현재 재생 중인 source (Stop(cueId) 지원용).
        private readonly Dictionary<string, AudioSource> _playing = new();
        private bool _initialized;

        public bool IsBgmMuted { get; private set; }
        public bool IsSfxMuted { get; private set; }

        private void Awake() => EnsureInit();

        private void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;
            if (clipTable != null)
            {
                foreach (var e in clipTable)
                {
                    if (string.IsNullOrEmpty(e.ResourceId) || e.Clip == null) continue;
                    _clips[e.ResourceId] = e.Clip;
                }
            }
        }

        public void Play(AudioCueEntry cue)
        {
            EnsureInit();
            var isBgm = cue.PlaybackType == PlaybackType.Bgm;
            if (isBgm && IsBgmMuted) return;
            if (!isBgm && IsSfxMuted) return;

            if (!_clips.TryGetValue(cue.ResourceId, out var clip)) return;

            var src = isBgm ? bgmSource : sfxSource;
            if (src == null) return;

            src.clip = clip;
            src.pitch = cue.Pitch ?? 1f;
            src.volume = cue.Volume ?? 1f;
            src.loop = cue.PlaybackType == PlaybackType.Bgm;
            src.Play();
            _playing[cue.CueId] = src;

            // PlayDurationMs 있으면 그만큼 후 정지 (Coroutine).
            if (cue.PlayDurationMs.HasValue)
            {
                StartCoroutine(StopAfter(cue.CueId, src, cue.PlayDurationMs.Value / 1000f));
            }
        }

        public void Stop(string cueId)
        {
            if (_playing.TryGetValue(cueId, out var src) && src != null)
            {
                src.Stop();
                _playing.Remove(cueId);
            }
        }

        public void StopAll()
        {
            if (bgmSource != null) bgmSource.Stop();
            if (sfxSource != null) sfxSource.Stop();
            _playing.Clear();
        }

        public void SetBgmMuted(bool muted)
        {
            IsBgmMuted = muted;
            if (muted && bgmSource != null) bgmSource.Stop();
        }

        public void SetSfxMuted(bool muted)
        {
            IsSfxMuted = muted;
            if (muted && sfxSource != null) sfxSource.Stop();
        }

        private System.Collections.IEnumerator StopAfter(string cueId, AudioSource src, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (src != null && _playing.TryGetValue(cueId, out var current) && current == src)
            {
                src.Stop();
                _playing.Remove(cueId);
            }
        }
    }
}
