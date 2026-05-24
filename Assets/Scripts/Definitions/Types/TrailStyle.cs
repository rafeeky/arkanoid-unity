namespace Arkanoid.Definitions
{
    // 공 파워 트레일 시각 스타일 — createBallTrail(style) 컴포넌트 config.
    // Unity 매핑: ScriptableObject 한 장 (TrailStyleSO).
    public readonly record struct TrailStyle(
        // 머리(head, 공 가까운 끝) 색 — 또렷. 16진수 RGB.
        int HeadColor,
        // 꼬리(tail) 색 — fade 마지막.
        int TailColor,
        // glow 색 (postFX). WebGL only / Unity 의 emission.
        int GlowColor,
        // 점 개수 — 길이 결정.
        int SegmentCount,
        // 머리쪽 알파 (또렷). 0..1.
        float HeadAlpha,
        // 점 반지름 (px). 머리에서 꼬리로 갈수록 축소.
        float SegmentRadius,
        // push 간격 (ms) — 시간 기반 샘플링. 16~20ms 권장.
        float PushIntervalMs);
}
