using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class GameplayControllerTests
    {
        // ─── Fixtures ───

        private static readonly IReadOnlyDictionary<string, BlockDefinition> BlockDefs = new Dictionary<string, BlockDefinition>
        {
            ["basic"] = new("basic", 1, 10, null, "v"),
            ["basic_drop"] = new("basic_drop", 1, 10, ItemType.Expand, "v"),
            ["tough"] = new("tough", 2, 30, null, "v"),
        };

        private static readonly IReadOnlyDictionary<ItemType, ItemDefinition> ItemDefs = new Dictionary<ItemType, ItemDefinition>
        {
            [ItemType.Expand] = new(ItemType.Expand, "", "", "", 160f, ItemType.Expand, ExpandMultiplier: 1.5f),
        };

        private static GameplayController.Dependencies Deps =>
            new(BlockDefs, ItemDefs, DefaultConfig);

        private static readonly StageDefinition SimpleStage = new(
            StageId: "test",
            BarSpawnX: 480f,
            BarSpawnY: 660f,
            Blocks: new[]
            {
                new BlockPlacement(0, 0, "basic"),
                new BlockPlacement(1, 0, "basic"),
            });

        private static readonly InputSnapshot NoInput = new(false, false, false);
        private static readonly InputSnapshot LeftInput = new(true, false, false);
        private static readonly InputSnapshot RightInput = new(false, true, false);
        private static readonly InputSnapshot SpaceInput = new(false, false, true);

        private static GameplayController MakeController(StageDefinition? stage = null, int lives = 3)
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage ?? SimpleStage, DefaultConfig, BlockDefs, lives);
            return new GameplayController(initialState, Deps);
        }

        private static BallState GetBall(GameplayRuntimeState state) =>
            state.Balls.Length > 0 ? state.Balls[0] : throw new Exception("No ball");

        private static BlockState GetBlock(GameplayRuntimeState state, int idx = 0) =>
            state.Blocks.Length > idx ? state.Blocks[idx] : throw new Exception($"No block at {idx}");

        // ─── 바 이동 ───

        [Test]
        public void Tick_RightInput_BarMovesRight()
        {
            var ctrl = MakeController();
            var initialX = ctrl.GetState().Bar.X;
            ctrl.Tick(RightInput, 1f / 60f);
            Assert.Greater(ctrl.GetState().Bar.X, initialX);
        }

        [Test]
        public void Tick_LeftInput_BarMovesLeft()
        {
            var ctrl = MakeController();
            var initialX = ctrl.GetState().Bar.X;
            ctrl.Tick(LeftInput, 1f / 60f);
            Assert.Less(ctrl.GetState().Bar.X, initialX);
        }

        [Test]
        public void Tick_NoInput_BarStays()
        {
            var ctrl = MakeController();
            var initialX = ctrl.GetState().Bar.X;
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.AreEqual(initialX, ctrl.GetState().Bar.X);
        }

        // ─── 공 발사 ───

        [Test]
        public void Tick_Space_BallLaunched()
        {
            var ctrl = MakeController();
            Assert.IsFalse(GetBall(ctrl.GetState()).IsActive);
            var events = ctrl.Tick(SpaceInput, 1f / 60f);
            Assert.IsTrue(GetBall(ctrl.GetState()).IsActive);
            Assert.IsTrue(events.Any(e => e is BallLaunchedEvent));
        }

        [Test]
        public void Tick_AfterLaunch_BallMovesUp()
        {
            var ctrl = MakeController();
            ctrl.Tick(SpaceInput, 1f / 60f);  // launch
            var yAfterLaunch = GetBall(ctrl.GetState()).Y;
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.Less(GetBall(ctrl.GetState()).Y, yAfterLaunch);
        }

        // ─── 벽 반사 ───

        [Test]
        public void Tick_BallNearRightWall_VxReversed()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Balls = new[]
            {
                GetBall(initialState) with { IsActive = true, X = 716f, Y = 400f, Vx = 200f, Vy = 0f }
            };
            var ctrl = new GameplayController(initialState, Deps);
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.Less(GetBall(ctrl.GetState()).Vx, 0f);
        }

        // ─── 블록 충돌 + 점수 ───

        [Test]
        public void Tick_BallHitsBlock_ScoreIncreases()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var firstBlock = GetBlock(initialState);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = firstBlock.X + 32f, Y = firstBlock.Y + 12f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.Greater(ctrl.GetState().Session.Score, 0);
        }

        [Test]
        public void Tick_BlockDestroyed_EventEmitted()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var firstBlock = GetBlock(initialState);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = firstBlock.X + 32f, Y = firstBlock.Y + 12f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(NoInput, 1f / 60f);
            Assert.IsTrue(events.Any(e => e is BlockDestroyedEvent));
        }

        // ─── 바닥 → 라이프 감소 ───

        [Test]
        public void Tick_BallFalls_LivesDecrease()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = 480f, Y = 950f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.AreEqual(2, ctrl.GetState().Session.Lives);
        }

        [Test]
        public void Tick_BallFalls_LifeLostEventWithRemaining2()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = 480f, Y = 950f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(NoInput, 1f / 60f);
            var lifeLost = events.OfType<LifeLostEvent>().FirstOrDefault();
            Assert.IsNotNull(lifeLost);
            Assert.AreEqual(2, lifeLost!.RemainingLives);
        }

        [Test]
        public void Tick_BallFalls_With1Life_GameOver()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 1);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = 480f, Y = 950f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(NoInput, 1f / 60f);
            Assert.IsTrue(events.Any(e => e is GameOverConditionMetEvent));
            Assert.AreEqual(0, ctrl.GetState().Session.Lives);
        }

        // ─── 스테이지 클리어 ───

        [Test]
        public void Tick_AllBlocksDestroyed_StageCleared()
        {
            var singleBlockStage = SimpleStage with { Blocks = new[] { new BlockPlacement(0, 0, "basic") } };
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                singleBlockStage, DefaultConfig, BlockDefs, 3);
            var firstBlock = GetBlock(initialState);
            var baseBall = GetBall(initialState);
            initialState.Balls = new[]
            {
                baseBall with { Id = "ball_0", X = firstBlock.X + 32f, Y = firstBlock.Y + 12f, Vx = 0f, Vy = 100f, IsActive = true }
            };
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(NoInput, 1f / 60f);
            Assert.IsTrue(events.Any(e => e is StageClearedEvent));
            Assert.IsTrue(ctrl.GetState().IsStageCleared);
        }

        // ─── Tunneling 회귀 ───

        [Test]
        public void Tunneling_DiagonalBall_HitsBlock()
        {
            var blockX = 150f;
            var blockY = 280f;
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage with { Blocks = new[] { new BlockPlacement(0, 0, "basic") } },
                DefaultConfig, BlockDefs, 3);
            initialState.Blocks = new[] { new BlockState("block_target", blockX, blockY, 1, false, "basic") };
            initialState.Balls = new[] { new BallState("ball_0", 100f, 300f, 550f, -300f, true) };
            var ctrl = new GameplayController(initialState, Deps);

            var hitEvents = new List<GameplayEvent>();
            for (var i = 0; i < 10; i++)
            {
                var events = ctrl.Tick(NoInput, 0.016f);
                foreach (var e in events)
                {
                    if (e is BlockHitEvent or BlockDestroyedEvent) hitEvents.Add(e);
                }
                if (hitEvents.Count > 0) break;
            }
            Assert.Greater(hitEvents.Count, 0);
        }

        [Test]
        public void Tunneling_BallNearBlock_HitsInOneTick()
        {
            var modState = MakeState(
                bar: new BarState(480f, 660f, 120f, 420f, BarEffect.None),
                blocks: new[] { new BlockState("blk", 200f, 280f, 1, false, "basic") },
                balls: new[] { new BallState("ball_0", 183f, 292f, 600f, 0f, true) });
            var ctrl = new GameplayController(modState, Deps);
            var events = ctrl.Tick(NoInput, 0.016f);
            var blockEvents = events.Where(e => e is BlockHitEvent or BlockDestroyedEvent).ToList();
            Assert.Greater(blockEvents.Count, 0);
            Assert.Less(GetBall(ctrl.GetState()).Vx, 0f);
        }

        [Test]
        public void Tunneling_TwoBlocks_ReflectsOffFirst()
        {
            var modState = MakeState(
                bar: new BarState(480f, 660f, 120f, 420f, BarEffect.None),
                blocks: new[]
                {
                    new BlockState("blk0", 200f, 280f, 1, false, "basic"),
                    new BlockState("blk1", 268f, 280f, 1, false, "basic"),
                },
                balls: new[] { new BallState("ball_0", 183f, 292f, 600f, 0f, true) });
            var ctrl = new GameplayController(modState, Deps);
            var events = ctrl.Tick(NoInput, 0.016f);
            var blockEvents = events.Where(e => e is BlockHitEvent or BlockDestroyedEvent).ToList();
            Assert.AreEqual(1, blockEvents.Count);
            var blockId = blockEvents[0] switch
            {
                BlockHitEvent bh => bh.BlockId,
                BlockDestroyedEvent bd => bd.BlockId,
                _ => "",
            };
            Assert.AreEqual("blk0", blockId);
            Assert.Less(GetBall(ctrl.GetState()).Vx, 0f);
        }

        // ─── ReleaseAttachedBalls (자석) ───

        [Test]
        public void Magnet_SpaceInput_ReleasesAttached()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Magnet };
            initialState.MagnetRemainingTime = 8000f;
            initialState.AttachedBallIds = new[] { "ball_0" };
            for (var i = 0; i < initialState.Balls.Length; i++)
            {
                if (initialState.Balls[i].Id == "ball_0")
                    initialState.Balls[i] = initialState.Balls[i] with { IsActive = false };
            }
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(SpaceInput, 1f / 60f);

            Assert.AreEqual(0, ctrl.GetState().AttachedBallIds.Count);
            Assert.IsTrue(GetBall(ctrl.GetState()).IsActive);
            Assert.IsTrue(events.Any(e => e is BallsReleasedEvent));
            var released = events.OfType<BallsReleasedEvent>().FirstOrDefault();
            Assert.IsNotNull(released);
            Assert.AreEqual(BallReleaseReason.Space, released!.ReleaseReason);
            CollectionAssert.Contains(released.BallIds, "ball_0");
        }

        [Test]
        public void Magnet_Timeout_ReleasesAttached()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Magnet };
            initialState.MagnetRemainingTime = 50f;
            initialState.AttachedBallIds = new[] { "ball_0" };
            for (var i = 0; i < initialState.Balls.Length; i++)
            {
                if (initialState.Balls[i].Id == "ball_0")
                    initialState.Balls[i] = initialState.Balls[i] with { IsActive = false };
            }
            var ctrl = new GameplayController(initialState, Deps);
            var events = ctrl.Tick(NoInput, 0.1f);  // dt=100ms → 50ms remaining 소진

            Assert.AreEqual(BarEffect.None, ctrl.GetState().Bar.ActiveEffect);
            Assert.AreEqual(0f, ctrl.GetState().MagnetRemainingTime);
            Assert.AreEqual(0, ctrl.GetState().AttachedBallIds.Count);
            Assert.IsTrue(GetBall(ctrl.GetState()).IsActive);
            Assert.IsTrue(events.Any(e => e is BallsReleasedEvent br && br.ReleaseReason == BallReleaseReason.Timeout));
        }

        // ─── FireLaser (자동) ───

        private static GameplayController.Dependencies LaserDeps()
        {
            var itemDefs = new Dictionary<ItemType, ItemDefinition>(ItemDefs)
            {
                [ItemType.Laser] = new(ItemType.Laser, "", "", "", 160f, ItemType.Laser, LaserCooldownMs: 400f, LaserShotCount: 2),
            };
            return new(BlockDefs, itemDefs, DefaultConfig);
        }

        [Test]
        public void Laser_AutoFire_2Shots()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 0f;
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            ctrl.Tick(SpaceInput, 1f / 60f);
            Assert.AreEqual(2, ctrl.GetState().LaserShots.Count);
        }

        [Test]
        public void Laser_AutoFire_LaserFiredEvent()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 0f;
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            var events = ctrl.Tick(SpaceInput, 1f / 60f);
            Assert.IsTrue(events.Any(e => e is LaserFiredEvent));
        }

        [Test]
        public void Laser_AutoFire_CooldownSet()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 0f;
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            ctrl.Tick(SpaceInput, 1f / 60f);
            // 400ms 에서 한 틱(~16.7ms) 감소
            Assert.Greater(ctrl.GetState().LaserCooldownRemaining, 0f);
            Assert.LessOrEqual(ctrl.GetState().LaserCooldownRemaining, 400f);
        }

        [Test]
        public void Laser_InCooldown_NoFire()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 400f;  // 쿨다운 중
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            var shotsBefore = ctrl.GetState().LaserShots.Count;
            var events = ctrl.Tick(SpaceInput, 1f / 60f);
            Assert.AreEqual(shotsBefore, ctrl.GetState().LaserShots.Count);
            Assert.IsFalse(events.Any(e => e is LaserFiredEvent));
        }

        [Test]
        public void Laser_ShotHitsBlock_BlockDestroyed()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var firstBlock = GetBlock(initialState);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 400f;  // 쿨다운 중 (자동 fire 방지)
            initialState.LaserShots = new[] { new LaserShotState("laser_0", firstBlock.X + 32f, firstBlock.Y + 12f, -1200f) };
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            var events = ctrl.Tick(NoInput, 1f / 60f);
            Assert.IsTrue(events.Any(e => e is BlockDestroyedEvent));
        }

        [Test]
        public void Laser_BlockDestroyed_ScoreIncrease()
        {
            var initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                SimpleStage, DefaultConfig, BlockDefs, 3);
            var firstBlock = GetBlock(initialState);
            initialState.Bar = initialState.Bar with { ActiveEffect = BarEffect.Laser };
            initialState.LaserCooldownRemaining = 400f;
            initialState.LaserShots = new[] { new LaserShotState("laser_0", firstBlock.X + 32f, firstBlock.Y + 12f, -1200f) };
            for (var i = 0; i < initialState.Balls.Length; i++)
                initialState.Balls[i] = initialState.Balls[i] with { IsActive = true };

            var ctrl = new GameplayController(initialState, LaserDeps());
            var scoreBefore = ctrl.GetState().Session.Score;
            ctrl.Tick(NoInput, 1f / 60f);
            Assert.Greater(ctrl.GetState().Session.Score, scoreBefore);
        }
    }
}
