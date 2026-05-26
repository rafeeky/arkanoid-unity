using System;
using System.Collections.Generic;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class StageRuntimeFactoryTests
    {
        // Minimal stage — Phase 2 의 actual StageDefinitionTable 없이 동작 검증.
        private static StageDefinition MinimalStage(IReadOnlyList<SpinnerPlacement>? spinners = null) =>
            new(
                StageId: "test_stage",
                BarSpawnX: 360f,
                BarSpawnY: 660f,
                Blocks: Array.Empty<BlockPlacement>(),
                Spinners: spinners);

        private static readonly IReadOnlyDictionary<string, BlockDefinition> BlockDefs = new Dictionary<string, BlockDefinition>
        {
            ["basic"] = new("basic", 1, 10, null, 0xCCCCCC),
        };

        // ─── MVP3 새 필드 초기값 ───

        [Test]
        public void MagnetRemainingTime_Zero()
        {
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                MinimalStage(), DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.MagnetRemainingTime);
        }

        [Test]
        public void AttachedBallIds_Empty()
        {
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                MinimalStage(), DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0, state.AttachedBallIds.Count);
        }

        [Test]
        public void LaserCooldownRemaining_Zero()
        {
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                MinimalStage(), DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.LaserCooldownRemaining);
        }

        [Test]
        public void LaserShots_Empty()
        {
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                MinimalStage(), DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0, state.LaserShots.Count);
        }

        // ─── Spinner spawn ───

        [Test]
        public void SpinnersInStage_RuntimeStates_Created()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(X: 100f, Y: 200f, DefinitionId: "spinner_cube"),
                new SpinnerPlacement(X: 300f, Y: 200f, DefinitionId: "spinner_triangle", InitialAngleRad: 1.5f),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(2, state.SpinnerStates.Count);
        }

        [Test]
        public void SpinnerIds_Sequential()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(100f, 200f, "spinner_cube"),
                new SpinnerPlacement(300f, 200f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual("spinner_0", state.SpinnerStates[0].Id);
            Assert.AreEqual("spinner_1", state.SpinnerStates[1].Id);
        }

        [Test]
        public void SpinnerDefinitionId_CopiedFromPlacement()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(100f, 200f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual("spinner_cube", state.SpinnerStates[0].DefinitionId);
        }

        [Test]
        public void SpinnerXCopied_YInitToZero_DescentEndYFromPlacementY()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(150f, 250f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(150f, state.SpinnerStates[0].X);
            Assert.AreEqual(0f, state.SpinnerStates[0].Y);
            Assert.AreEqual(250f, state.SpinnerStates[0].DescentEndY);
        }

        [Test]
        public void NoInitialAngle_AngleRadZero()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(100f, 200f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.SpinnerStates[0].AngleRad);
        }

        [Test]
        public void InitialAngleApplied()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(100f, 200f, "spinner_cube", InitialAngleRad: 1.5f),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(1.5f, state.SpinnerStates[0].AngleRad);
        }

        [Test]
        public void EmptySpinners_EmptyRuntimeStates()
        {
            var stage = MinimalStage(spinners: Array.Empty<SpinnerPlacement>());
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0, state.SpinnerStates.Count);
        }

        // ─── Spinner spawn 초기화 ───

        [Test]
        public void Phase_SpawningInit()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
                new SpinnerPlacement(520f, 350f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(SpinnerPhase.Spawning, state.SpinnerStates[0].Phase);
            Assert.AreEqual(SpinnerPhase.Spawning, state.SpinnerStates[1].Phase);
        }

        [Test]
        public void InitialY_Zero()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
                new SpinnerPlacement(520f, 350f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.SpinnerStates[0].Y);
            Assert.AreEqual(0f, state.SpinnerStates[1].Y);
        }

        [Test]
        public void SpawnElapsedMs_Zero()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.SpinnerStates[0].SpawnElapsedMs);
        }

        [Test]
        public void SpawnX_FromPlacementX()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
                new SpinnerPlacement(520f, 350f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(200f, state.SpinnerStates[0].SpawnX);
            Assert.AreEqual(520f, state.SpinnerStates[1].SpawnX);
        }

        [Test]
        public void CircleCenterX_FromSpawnX()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
                new SpinnerPlacement(520f, 350f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(200f, state.SpinnerStates[0].CircleCenterX);
            Assert.AreEqual(520f, state.SpinnerStates[1].CircleCenterX);
        }

        [Test]
        public void CircleCenterY_ClampedByPlayfield()
        {
            // descentEndY=400 → [380, 760] 범위 내 → 400.
            // descentEndY=350 → 380 으로 clamp.
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
                new SpinnerPlacement(520f, 350f, "spinner_triangle"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(400f, state.SpinnerStates[0].CircleCenterY);
            Assert.AreEqual(380f, state.SpinnerStates[1].CircleCenterY);
        }

        [Test]
        public void CircleRadius_60()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(60f, state.SpinnerStates[0].CircleRadius);
        }

        [Test]
        public void CircleAngleRad_Zero()
        {
            var stage = MinimalStage(spinners: new[]
            {
                new SpinnerPlacement(200f, 400f, "spinner_cube"),
            });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(0f, state.SpinnerStates[0].CircleAngleRad);
        }

        // ─── Clamp tests (playfield 720×900, circleR=60, margin=10, minY=380, barClear=80) ───
        // circleCenterX ∈ [70, 650], circleCenterY ∈ [380, 760]

        [Test]
        public void ClampX_50_To70()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(50f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(70f, state.SpinnerStates[0].CircleCenterX);
        }

        [Test]
        public void ClampX_700_To650()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(700f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(650f, state.SpinnerStates[0].CircleCenterX);
        }

        [Test]
        public void ClampX_360_NoChange()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(360f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(360f, state.SpinnerStates[0].CircleCenterX);
        }

        [Test]
        public void ClampY_800_To760()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(360f, 800f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(760f, state.SpinnerStates[0].CircleCenterY);
        }

        [Test]
        public void ClampY_50_To380()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(360f, 50f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(380f, state.SpinnerStates[0].CircleCenterY);
        }

        [Test]
        public void ClampY_500_NoChange()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(360f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(500f, state.SpinnerStates[0].CircleCenterY);
        }

        [Test]
        public void ClampX_Boundary70()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(70f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(70f, state.SpinnerStates[0].CircleCenterX);
        }

        [Test]
        public void ClampX_Boundary650()
        {
            var stage = MinimalStage(spinners: new[] { new SpinnerPlacement(650f, 500f, "spinner_cube") });
            var state = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, DefaultConfig, BlockDefs, 3);
            Assert.AreEqual(650f, state.SpinnerStates[0].CircleCenterX);
        }
    }
}
