namespace Arkanoid.Gameplay
{
    public readonly record struct GameSessionState(
        int CurrentStageIndex,
        int Score,
        int Lives,
        int HighScore);
}
