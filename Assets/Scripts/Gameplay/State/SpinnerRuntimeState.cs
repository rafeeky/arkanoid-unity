namespace Arkanoid.Gameplay
{
    // Spawning   — gate 열림 연출 시간 (400ms). spinner 는 y=0 고정, 충돌 비활성 (ghost).
    // Descending — 느린 선형 하강 (80 px/s). y = 0 → DescentEndY. x = SpawnX 고정. ghost.
    // Circling   — 원 궤도 이동. CircleCenter 주위 radius=150, 1.5 rad/s. solid.
    public enum SpinnerPhase { Spawning, Descending, Circling }

    // 회전체 1개의 런타임 상태. AngleRad (자체 회전) 은 모든 phase 에서 계속 증가.
    public readonly record struct SpinnerRuntimeState(
        string Id,
        string DefinitionId,
        float X,
        float Y,
        float AngleRad,
        SpinnerPhase Phase,
        float SpawnElapsedMs,
        float DescentEndY,
        float CircleCenterX,
        float CircleCenterY,
        float CircleRadius,
        float CircleAngleRad,
        float SpawnX);
}
