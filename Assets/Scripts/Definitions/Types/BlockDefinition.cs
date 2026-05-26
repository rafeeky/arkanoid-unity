namespace Arkanoid.Definitions
{
    public readonly record struct BlockDefinition(
        string DefinitionId,
        int MaxHits,
        int Score,
        // null = no drop. TS 의 'none' 대신 C# nullable 패턴.
        ItemType? DropItemType,
        int BaseColor);
}
