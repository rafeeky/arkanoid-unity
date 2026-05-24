using Arkanoid.Definitions;

namespace Arkanoid.Presentation
{
    // 아무 동작 안 하는 IArkanoidAudio. 테스트/헤드리스 환경 또는 Unity AudioPlayer 준비 전 기본값.
    public sealed class NoopAudio : IArkanoidAudio
    {
        public void Play(AudioCueEntry cue) { /* noop */ }
        public void StopAll() { /* noop */ }
        public void Stop(string cueId) { /* noop */ }
        public void SetBgmMuted(bool muted) { /* noop */ }
        public void SetSfxMuted(bool muted) { /* noop */ }
        public bool IsBgmMuted => false;
        public bool IsSfxMuted => false;
    }
}
