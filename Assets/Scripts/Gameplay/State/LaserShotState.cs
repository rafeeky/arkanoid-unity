namespace Arkanoid.Gameplay
{
    public readonly record struct LaserShotState(
        string Id,
        float X,
        float Y,
        float Vy);  // 레이저는 위로만 이동 (Unity 좌표 Y+ 위 — vy 양수면 위로)
}
