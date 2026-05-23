namespace Arkanoid.Gameplay
{
    public readonly record struct BlockState(
        string Id,
        float X,
        float Y,
        int RemainingHits,
        bool IsDestroyed,
        string DefinitionId);
}
