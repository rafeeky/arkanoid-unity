using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    // ItemType enum 은 Definitions 로 이동 (BlockDefinition/ItemDefinition 도 사용).
    public readonly record struct ItemDropState(
        string Id,
        ItemType ItemType,
        float X,
        float Y,
        float FallSpeed,
        bool IsCollected);
}
