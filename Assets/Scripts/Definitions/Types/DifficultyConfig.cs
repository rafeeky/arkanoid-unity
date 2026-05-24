namespace Arkanoid.Definitions
{
    // 묶음 E (Unity → TS 포팅) — Normal: 5 lives, spinners OFF / Hard: 3 lives, spinners ON.
    public readonly record struct DifficultyConfig(
        int InitialLives,
        bool SpinnersEnabled);
}
