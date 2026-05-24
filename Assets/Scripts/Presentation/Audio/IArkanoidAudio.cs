using Arkanoid.Definitions;

namespace Arkanoid.Presentation
{
    // 오디오 재생 추상화 인터페이스. 구현체: NoopAudio (테스트/헤드리스), UnityAudio (Unity AudioSource).
    public interface IArkanoidAudio
    {
        // 주어진 cue 재생. PlaybackType 에 따라 bgm/jingle/sfx 분기.
        // BGM/SFX 음소거 상태이면 해당 카테고리 재생 스킵.
        void Play(AudioCueEntry cue);

        // 현재 재생 중인 모든 소리 정지.
        void StopAll();

        // 특정 cueId 의 사운드만 정지 (RoundIntro 짧은 BGM 끊기 등).
        // 매핑 없거나 재생 중 아니면 no-op.
        void Stop(string cueId);

        void SetBgmMuted(bool muted);
        void SetSfxMuted(bool muted);
        bool IsBgmMuted { get; }
        bool IsSfxMuted { get; }
    }
}
