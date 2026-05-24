namespace Arkanoid.Gameplay
{
    public enum WallSide { Left, Right, Top }

    // 충돌 fact union — TS discriminated union → C# sealed record hierarchy.
    // pattern matching `switch` 가능. detection 알고리즘 (T1.5) 이 채워줌.
    public abstract record CollisionFact;

    public sealed record BallHitWallFact(string BallId, WallSide Side) : CollisionFact;

    public sealed record BallHitBarFact(
        string BallId,
        // 정규화 접촉점: -1 = 좌 끝, 0 = 중앙, +1 = 우 끝
        float BarContactX) : CollisionFact;

    public sealed record BallHitBlockFact(
        string BallId,
        string BlockId,
        BlockSide Side) : CollisionFact;

    public sealed record BallHitFloorFact(string BallId) : CollisionFact;

    public sealed record ItemPickedUpFact(string ItemId) : CollisionFact;

    public sealed record ItemFellOffFloorFact(string ItemId) : CollisionFact;
}
