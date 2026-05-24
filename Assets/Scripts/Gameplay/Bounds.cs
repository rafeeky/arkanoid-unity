namespace Arkanoid.Gameplay
{
    // 공용 AABB. TS 의 entity 별 `Bounds` type 을 통합.
    // (X, Y) 는 좌상단. Width/Height 는 양수.
    public readonly record struct Bounds(float X, float Y, float Width, float Height);
}
