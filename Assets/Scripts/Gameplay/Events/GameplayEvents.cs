using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public enum BallReleaseReason { Space, Timeout, Replaced }

    // gameplayEvents.ts 의 union → C# sealed record hierarchy. pattern matching `switch` 가능.
    public abstract record GameplayEvent;

    public sealed record BallLaunchedEvent() : GameplayEvent;

    public sealed record BallAttachedEvent(IReadOnlyList<string> BallIds) : GameplayEvent;

    public sealed record BallsReleasedEvent(
        IReadOnlyList<string> BallIds,
        BallReleaseReason ReleaseReason) : GameplayEvent;

    // 공이 바에 부딪혀 반사 (자석 부착 제외). 바 튕김 사운드 트리거.
    public sealed record BallHitBarEvent(string BallId) : GameplayEvent;

    public sealed record BlockHitEvent(string BlockId, int RemainingHits) : GameplayEvent;

    public sealed record BlockDestroyedEvent(string BlockId, int ScoreDelta) : GameplayEvent;

    public sealed record ItemSpawnedEvent(
        string ItemId,
        ItemType ItemType,
        float X,
        float Y) : GameplayEvent;

    public sealed record ItemCollectedEvent(
        ItemType ItemType,
        BarEffect ReplacedEffect,
        BarEffect NewEffect) : GameplayEvent;

    public sealed record LaserFiredEvent(int ShotCount) : GameplayEvent;

    public sealed record LifeLostEvent(int RemainingLives) : GameplayEvent;

    public sealed record StageClearedEvent() : GameplayEvent;

    public sealed record GameOverConditionMetEvent() : GameplayEvent;
}
