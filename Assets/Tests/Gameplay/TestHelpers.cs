using System;
using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay.Tests
{
    // 공용 fixture helper — 각 테스트가 *기본값 + override* 패턴으로 state 생성.
    internal static class TestHelpers
    {
        public static GameplayRuntimeState MakeState(
            GameSessionState? session = null,
            BarState? bar = null,
            BallState[]? balls = null,
            BlockState[]? blocks = null,
            IReadOnlyList<BorderBlockState>? borders = null,
            IReadOnlyList<DoorState>? doors = null,
            ItemDropState[]? itemDrops = null,
            bool isStageCleared = false,
            float magnetRemainingTime = 0f,
            int? magnetRemainingUses = null,
            IReadOnlyList<string>? attachedBallIds = null,
            float laserCooldownRemaining = 0f,
            float? laserRemainingTime = null,
            IReadOnlyList<LaserShotState>? laserShots = null,
            IReadOnlyList<SpinnerRuntimeState>? spinnerStates = null,
            string? currentTrailStyle = null)
        {
            return new GameplayRuntimeState
            {
                Session = session ?? new GameSessionState(0, 0, 3, 0),
                Bar = bar ?? new BarState(480f, 660f, 120f, 420f, BarEffect.None),
                Balls = balls ?? Array.Empty<BallState>(),
                Blocks = blocks ?? Array.Empty<BlockState>(),
                Borders = borders ?? Array.Empty<BorderBlockState>(),
                Doors = doors ?? Array.Empty<DoorState>(),
                ItemDrops = itemDrops ?? Array.Empty<ItemDropState>(),
                IsStageCleared = isStageCleared,
                MagnetRemainingTime = magnetRemainingTime,
                MagnetRemainingUses = magnetRemainingUses,
                AttachedBallIds = attachedBallIds ?? Array.Empty<string>(),
                LaserCooldownRemaining = laserCooldownRemaining,
                LaserRemainingTime = laserRemainingTime,
                LaserShots = laserShots ?? Array.Empty<LaserShotState>(),
                SpinnerStates = spinnerStates ?? Array.Empty<SpinnerRuntimeState>(),
                CurrentTrailStyle = currentTrailStyle,
            };
        }

        public static BlockState LivingBlock(string id, float x = 100f, float y = 100f, int hits = 1, string defId = "basic") =>
            new(id, x, y, hits, false, defId);

        public static BlockState DestroyedBlock(string id, float x = 100f, float y = 100f, string defId = "basic") =>
            new(id, x, y, 0, true, defId);

        public static BallState Ball(string id = "ball_0", float x = 480f, float y = 640f, float vx = 0f, float vy = 0f, bool isActive = false) =>
            new(id, x, y, vx, vy, isActive);

        public static PhysicsConfig DefaultPhysics =>
            new(SubStepSize: 4f, MaxSubSteps: 32, PushOutEpsilon: 0.5f, MinAngleDeg: 15f, BarContactBias: 0.7f);

        public static GameplayConfig DefaultConfig =>
            new(
                InitialLives: 3,
                BaseBarWidth: 120f,
                BarMoveSpeed: 420f,
                BallInitialSpeed: 480f,
                BallInitialAngleDeg: -60f,
                RoundIntroDurationMs: 1500f,
                BlockHitFlashDurationMs: 100f,
                BarBreakDurationMs: 800f,
                ExpandMultiplier: 1.5f,
                AutoLaunchDelayMs: 7000f,
                Physics: DefaultPhysics);
    }
}
