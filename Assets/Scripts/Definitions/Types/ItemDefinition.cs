namespace Arkanoid.Definitions
{
    public readonly record struct ItemDefinition(
        ItemType ItemType,
        string DisplayNameTextId,
        string DescriptionTextId,
        string IconId,
        float FallSpeed,
        ItemType EffectType,
        float? ExpandMultiplier = null,
        // [deprecated] 시간 기반 magnet (호환용 유지). 새 동작은 MagnetUseCount.
        float? MagnetDurationMs = null,
        // magnet 효과 동안 공 부착 가능 횟수 (release 1회당 차감).
        int? MagnetUseCount = null,
        float? LaserCooldownMs = null,
        int? LaserShotCount = null,
        // laser 효과 지속 시간 (ms). 0 도달 시 효과 종료.
        float? LaserDurationMs = null);
}
