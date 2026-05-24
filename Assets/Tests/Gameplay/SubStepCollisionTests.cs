using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // sub-step AABB 알고리즘 회귀 — 고속 공/수직 터널/연속 블록.
    [TestFixture]
    public class SubStepCollisionTests
    {
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float BallRadius = 8f;
        private const float CanvasHeight = 720f;
        private const float CanvasWidth = 720f;

        private static bool IsBallCenterInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        // ─── Test 1: 고속 공 터널링 방지 ───

        [Test]
        public void FastBall_2000pxs_30fps_NoTunneling()
        {
            var block = new BlockState("fast_block", 448f, 240f, 1, false, "basic");
            var ball = new BallState("ball_0", 480f, 300f, 0f, -2000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.033f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
            Assert.GreaterOrEqual(r.BlockFacts.Count, 1);
            Assert.IsTrue(r.BlockFacts.Any(f => f.BlockId == "fast_block"));
            Assert.Greater(r.Ball.Vy, 0f);
        }

        [Test]
        public void FastBall_1500pxs_60fps_NoTunneling()
        {
            var block = new BlockState("fast_block_60", 448f, 400f, 1, false, "basic");
            var ball = new BallState("ball_0", 480f, 450f, 0f, -1500f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
        }

        [Test]
        public void ExtremeBall_3000pxs_NoInside()
        {
            var block = new BlockState("extreme_block", 448f, 300f, 2, false, "basic");
            var ball = new BallState("ball_0", 480f, 450f, 0f, -3000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
        }

        // ─── Test 2: 수직 연속 블록 — 한 틱 1 블록 반사 ───

        [Test]
        public void VerticalChain4Blocks_AtMost1PerTick()
        {
            var blocks = new List<BlockState>();
            for (var i = 0; i < 4; i++)
                blocks.Add(new BlockState($"vert_{i}", 448f, 200f + i * (BlockHeight + 4f), 1, false, "basic"));

            var ball = new BallState("ball_0", 480f, 450f, 0f, -600f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 50; tick++)
            {
                if (ball.Y < 0f || ball.Y > CanvasHeight) break;
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                var uniqueHits = new HashSet<string>(r.BlockFacts.Select(f => f.BlockId));
                Assert.LessOrEqual(uniqueHits.Count, 1, $"tick={tick}: {uniqueHits.Count} blocks reflected");

                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                    {
                        if (current[i].Id == f.BlockId)
                            current[i] = current[i] with { IsDestroyed = true };
                    }
                }
            }
        }

        [Test]
        public void HighSpeed1400_VerticalChain_AtMost1PerTick()
        {
            var blocks = new List<BlockState>();
            for (var i = 0; i < 6; i++)
                blocks.Add(new BlockState($"hspeed_{i}", 448f, 100f + i * (BlockHeight + 4f), 1, false, "basic"));

            var ball = new BallState("ball_0", 480f, 350f, 0f, -1400f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 30; tick++)
            {
                if (ball.Y < 0f) break;
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                var uniqueHits = new HashSet<string>(r.BlockFacts.Select(f => f.BlockId));
                Assert.LessOrEqual(uniqueHits.Count, 1, $"tick={tick}: {uniqueHits.Count} blocks");

                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                    {
                        if (current[i].Id == f.BlockId)
                            current[i] = current[i] with { IsDestroyed = true };
                    }
                }
            }
        }

        // ─── Test 3: 100 tick 시나리오 — 블록 내부 절대 머물지 않음 ───

        private static List<BlockState> BuildGrid(int rows, int cols, float startX, float startY)
        {
            var result = new List<BlockState>(rows * cols);
            var idx = 0;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    result.Add(new BlockState($"g_{idx++}",
                        startX + c * (BlockWidth + 4f),
                        startY + r * (BlockHeight + 4f),
                        1, false, "basic"));
                }
            }
            return result;
        }

        private static void RunScenario(string label, BallState startBall, List<BlockState> blocks)
        {
            var ball = startBall;
            var current = blocks.ToArray();

            for (var tick = 0; tick < 100; tick++)
            {
                if (!ball.IsActive) break;
                if (ball.Y - BallRadius > CanvasHeight) break;
                if (ball.Y < 0f) break;

                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;

                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                    {
                        if (current[i].Id == f.BlockId)
                            current[i] = current[i] with { IsDestroyed = true };
                    }
                }

                if (ball.X - BallRadius <= 0f) ball = ball with { Vx = MathF.Abs(ball.Vx) };
                if (ball.X + BallRadius >= CanvasWidth) ball = ball with { Vx = -MathF.Abs(ball.Vx) };
                if (ball.Y - BallRadius <= 0f) ball = ball with { Vy = MathF.Abs(ball.Vy) };

                foreach (var b in current)
                {
                    if (IsBallCenterInsideBlock(ball, b))
                    {
                        Assert.Fail($"[{label}] tick={tick}: ball=({ball.X:F2},{ball.Y:F2}) inside {b.Id} at ({b.X},{b.Y})");
                    }
                }
            }
        }

        [Test]
        public void Scenario_45Deg_5x3Grid() =>
            RunScenario("45deg 5x3",
                new BallState("ball_0", 480f, 600f, 300f, -300f, true),
                BuildGrid(5, 3, 400f, 80f));

        [Test]
        public void Scenario_VerticalHighSpeed_SingleRow() =>
            RunScenario("vert highspeed 1x5",
                new BallState("ball_0", 480f, 500f, 0f, -900f, true),
                BuildGrid(1, 5, 350f, 200f));

        [Test]
        public void Scenario_NearHorizontal_WallEdge() =>
            RunScenario("near-horizontal wall",
                new BallState("ball_0", 100f, 300f, 800f, -200f, true),
                BuildGrid(3, 2, 600f, 250f));

        [Test]
        public void Scenario_UpLeftHighSpeed() =>
            RunScenario("up-left highspeed",
                new BallState("ball_0", 700f, 600f, -600f, -600f, true),
                BuildGrid(4, 4, 100f, 100f));

        [Test]
        public void Scenario_VerticalTunnel_StagePattern() =>
            RunScenario("vert tunnel stage",
                new BallState("ball_0", 480f, 640f, 10f, -420f, true),
                BuildGrid(5, 13, 40f, 80f));

        // ─── Test 4: sub-step 경계 조건 ───

        [Test]
        public void InactiveBall_Unchanged()
        {
            var ball = new BallState("ball_0", 480f, 600f, 300f, -300f, false);
            var block = new BlockState("b", 448f, 400f, 1, false, "basic");
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.AreEqual(ball, r.Ball);
            Assert.AreEqual(0, r.BlockFacts.Count);
            Assert.AreEqual(0, r.WallFacts.Count);
        }

        [Test]
        public void DestroyedBlock_ExcludedFromDetection()
        {
            var ball = new BallState("ball_0", 480f, 300f, 0f, -600f, true);
            var destroyed = new BlockState("destroyed", 448f, 200f, 0, true, "basic");
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { destroyed },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.AreEqual(0, r.BlockFacts.Count);
        }

        [Test]
        public void NoBlocks_LinearMove()
        {
            var ball = new BallState("ball_0", 480f, 400f, 100f, -100f, true);
            var dt = 1f / 60f;
            var r = MovementSystem.MoveBallWithCollisions(ball, dt, Array.Empty<BlockState>(),
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.That(r.Ball.X, Is.EqualTo(480f + 100f * dt).Within(0.001f));
            Assert.That(r.Ball.Y, Is.EqualTo(400f - 100f * dt).Within(0.001f));
            Assert.AreEqual(0, r.BlockFacts.Count);
        }

        // ─── Test 5: 반사 후 재진입 방지 ───

        [Test]
        public void BlockTopReflect_NoReHitNextFrame()
        {
            var block = new BlockState("reentry_block", 448f, 300f, 2, false, "basic");
            var ball = new BallState("ball_0", 480f, 310f, 0f, -400f, true);
            const float dt = 1f / 20f;

            var f1 = MovementSystem.MoveBallWithCollisions(ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.GreaterOrEqual(f1.BlockFacts.Count, 1);
            Assert.Greater(f1.Ball.Vy, 0f);  // 위→아래

            var f2 = MovementSystem.MoveBallWithCollisions(f1.Ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(f2.Ball, block));
        }

        [Test]
        public void BallInsideExpandedAABB_OverlapStart_NotInsideAfter()
        {
            var block = new BlockState("overlap_block", 448f, 300f, 2, false, "basic");
            var ball = new BallState("ball_0", 480f, 310f, 0f, -200f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
        }
    }
}
