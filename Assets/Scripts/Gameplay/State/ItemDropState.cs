namespace Arkanoid.Gameplay
{
    public enum ItemType { Expand, Magnet, Laser }

    public readonly record struct ItemDropState(
        string Id,
        ItemType ItemType,
        float X,
        float Y,
        float FallSpeed,
        bool IsCollected);
}
