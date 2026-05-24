namespace Arkanoid.Definitions
{
    // BlockDefinition.DropItemType 의 Inspector-friendly enum (Unity Serializer 가 nullable enum 안 지원).
    // None = no drop. 나머지는 ItemType 과 1:1.
    public enum DropItemKind { None, Expand, Magnet, Laser }

    public static class DropItemKindExtensions
    {
        public static ItemType? ToItemTypeOrNull(this DropItemKind k) => k switch
        {
            DropItemKind.None => null,
            DropItemKind.Expand => ItemType.Expand,
            DropItemKind.Magnet => ItemType.Magnet,
            DropItemKind.Laser => ItemType.Laser,
            _ => null,
        };
    }
}
