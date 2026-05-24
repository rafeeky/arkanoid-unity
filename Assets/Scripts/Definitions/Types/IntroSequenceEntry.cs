namespace Arkanoid.Definitions
{
    public readonly record struct IntroSequenceEntry(
        int PageIndex,
        string Text,
        float TypingSpeedMs,       // 글자당 ms (기본 40)
        float HoldDurationMs,      // 완전 표시 후 유지 (기본 1500)
        float EraseSpeedMs);       // 지우기 속도 (기본 20)
}
