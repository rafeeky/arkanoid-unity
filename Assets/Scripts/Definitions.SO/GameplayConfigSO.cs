using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    // GameplayConfig — physics 튜닝 + 게임 기본 파라미터.
    // Inspector 편집 가능. Data property 로 record struct 반환.
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Arkanoid/Gameplay/Gameplay Config")]
    public sealed class GameplayConfigSO : ScriptableObject
    {
        [Header("Lives & Bar")]
        [SerializeField] private int initialLives = 3;
        [SerializeField] private float baseBarWidth = 120f;
        [SerializeField] private float barMoveSpeed = 420f;

        [Header("Ball")]
        [SerializeField] private float ballInitialSpeed = 480f;
        [SerializeField] private float ballInitialAngleDeg = -60f;

        [Header("Timing (ms)")]
        [SerializeField] private float roundIntroDurationMs = 1500f;
        [SerializeField] private float blockHitFlashDurationMs = 100f;
        [SerializeField] private float barBreakDurationMs = 800f;
        [SerializeField] private float autoLaunchDelayMs = 7000f;
        [SerializeField] private float expandMultiplier = 1.5f;

        [Header("Physics")]
        [SerializeField] private float physicsSubStepSize = 4f;
        [SerializeField] private int physicsMaxSubSteps = 32;
        [SerializeField] private float physicsPushOutEpsilon = 0.5f;
        [SerializeField] private float physicsMinAngleDeg = 15f;
        [SerializeField] private float physicsBarContactBias = 0.7f;

        public GameplayConfig Data => new(
            InitialLives: initialLives,
            BaseBarWidth: baseBarWidth,
            BarMoveSpeed: barMoveSpeed,
            BallInitialSpeed: ballInitialSpeed,
            BallInitialAngleDeg: ballInitialAngleDeg,
            RoundIntroDurationMs: roundIntroDurationMs,
            BlockHitFlashDurationMs: blockHitFlashDurationMs,
            BarBreakDurationMs: barBreakDurationMs,
            ExpandMultiplier: expandMultiplier,
            AutoLaunchDelayMs: autoLaunchDelayMs,
            Physics: new PhysicsConfig(
                SubStepSize: physicsSubStepSize,
                MaxSubSteps: physicsMaxSubSteps,
                PushOutEpsilon: physicsPushOutEpsilon,
                MinAngleDeg: physicsMinAngleDeg,
                BarContactBias: physicsBarContactBias));
    }
}
