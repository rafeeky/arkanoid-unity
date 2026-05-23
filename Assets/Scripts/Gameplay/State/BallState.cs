namespace Arkanoid.Gameplay
{
    public readonly record struct BallState(
        string Id,
        float X,
        float Y,
        float Vx,
        float Vy,
        bool IsActive,
        // 자석 효과로 바에 부착 중일 때, 바 중심 대비 x 오프셋(px). 부착 시 세팅, 해제 시 null.
        float? AttachedOffsetX = null,
        // 바(paddle) 충돌 *이후* 깬 블록 개수. 바에 맞으면 0 reset. 벽 튕김은 reset 안 함.
        // 임계점 (2) 도달 시 IsPowered=true. null → fallback 0.
        int? BlocksSincePaddle = null,
        // 파워 상태 — BlocksSincePaddle ≥ 2 일 때 true. 바 충돌 시 false. 시각 (트레일) 표시 전용.
        bool? IsPowered = null);
}
