using Arkanoid.Definitions;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    public readonly record struct HudViewModel(
        int Score,
        int HighScore,
        int Lives,
        int Round,
        BarEffect ActiveEffect,
        float MagnetRemainingMs,       // 호환용 — 새 동작은 MagnetRemainingUses
        int? MagnetRemainingUses,      // null → 0
        float LaserCooldownMs,
        float? LaserRemainingMs);      // null → 0
}
