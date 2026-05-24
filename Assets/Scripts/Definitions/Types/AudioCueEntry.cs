namespace Arkanoid.Definitions
{
    public enum PlaybackType { Bgm, Jingle, Sfx }

    public readonly record struct AudioCueEntry(
        string CueId,
        string EventType,
        string ResourceId,
        PlaybackType PlaybackType,
        // 재생 피치 배율 (Phaser sound rate, 1.0 기본). 0.7 저음, 1.35 고음. null → 1.0.
        float? Pitch = null,
        // 재생 길이 강제 컷 (ms). null → 풀 길이. Unity: AudioSource.PlayScheduled(duration).
        float? PlayDurationMs = null,
        // 음량 (0..1). null → 1.0.
        float? Volume = null);
}
