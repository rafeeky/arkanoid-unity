namespace Arkanoid.Shared
{
    // 2D 벡터 (Unity 0의존). UnityEngine.Vector2 와 다른 단순한 POCO record struct.
    // Gameplay/Flow asmdef 가 Unity 의존 X 라 직접 정의.
    public readonly record struct Vec2(float X, float Y);
}
