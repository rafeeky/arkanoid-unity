using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 블록 코너 + 벽 직후 시나리오 회귀.
    [TestFixture]
    public class CornerAndWallBugsTests
    {
        private const float BallRadius = 8f;
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float CanvasWidth = 720f;

        private static BallState MakeBall(float x = 480f, float y = 400f, float vx = 100f, float vy = -100f) =>
            new("ball_0", x, y, vx, vy, true);

        private static BlockState MakeBlock(string id = "block_0", float x = 200f, float y = 200f, bool destroyed = false) =>
            new(id, x, y, 1, destroyed, "basic");

        private static bool IsBallCenterInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        // ─── Test 1: 코너 45° 진입 ───

        [Test]
        public void TopRightCorner_45deg_NotInside()
        {
            var block = MakeBlock(id: "corner_block", x: 400f, y: 200f);
            var ball = MakeBall(x: 448f, y: 216f, vx: 300f, vy: -300f);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
            if (r.BlockFacts.Count > 0) Assert.AreEqual("corner_block", r.BlockFacts[0].BlockId);
        }

        [Test]
        public void TopRightCorner_5Ticks_NotInside()
        {
            var block = MakeBlock(id: "corner_block_2", x: 300f, y: 150f);
            const float speed = 420f;
            var ball = MakeBall(x: 340f, y: 190f, vx: speed, vy: -speed);
            var blocks = new[] { block };

            for (var tick = 0; tick < 5; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > 0)
                {
                    for (var i = 0; i < blocks.Length; i++)
                    {
                        if (r.BlockFacts.Any(f => f.BlockId == blocks[i].Id))
                            blocks[i] = blocks[i] with { IsDestroyed = true };
                    }
                }
                foreach (var b in blocks)
                    if (!b.IsDestroyed) Assert.IsFalse(IsBallCenterInsideBlock(ball, b));
            }
        }

        // ─── Test 2: 벽 근처 블록 — playfield 안 유지 ───

        [Test]
        public void NearRightWall_StayInPlayfield()
        {
            var block = MakeBlock(id: "wall_block", x: 887f, y: 190f);
            var ball = MakeBall(x: 870f, y: 202f, vx: 300f, vy: -100f);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 30f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.GreaterOrEqual(r.Ball.X, 0f);
            Assert.LessOrEqual(r.Ball.X, CanvasWidth);
        }

        [Test]
        public void NearRightWall_10Ticks_StayInPlayfield()
        {
            var block = MakeBlock(id: "right_wall_block", x: 888f, y: 180f);
            var ball = MakeBall(x: 880f, y: 192f, vx: 400f, vy: -200f);
            var blocks = new[] { block };
            for (var tick = 0; tick < 10; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                Assert.GreaterOrEqual(ball.X, 0f);
                Assert.LessOrEqual(ball.X, CanvasWidth);
            }
        }

        // ─── Test 3: 벽 반사 후 블록 통과 없음 ───

        [Test]
        public void AfterRightWallReflect_NotInsideBlock()
        {
            var block = MakeBlock(id: "post_wall_block", x: 800f, y: 190f);
            var ball = MakeBall(x: CanvasWidth - BallRadius - 1f, y: 202f, vx: -300f, vy: -100f);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, block));
        }

        [Test]
        public void AfterRightWallReflect_10Ticks_NoBlockEntry()
        {
            var block = MakeBlock(id: "near_wall_block", x: 860f, y: 180f);
            var ball = MakeBall(x: CanvasWidth - BallRadius - 2f, y: 192f, vx: -420f, vy: -100f);
            var blocks = new[] { block };
            for (var tick = 0; tick < 10; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > 0)
                {
                    for (var i = 0; i < blocks.Length; i++)
                    {
                        if (r.BlockFacts.Any(f => f.BlockId == blocks[i].Id))
                            blocks[i] = blocks[i] with { IsDestroyed = true };
                    }
                }
                foreach (var b in blocks)
                    if (!b.IsDestroyed) Assert.IsFalse(IsBallCenterInsideBlock(ball, b));
            }
        }

        // ─── Test 4: tight 배치 모서리 충돌 ───

        [Test]
        public void TightGap1px_NotInsideEither()
        {
            var b0 = MakeBlock(id: "block_left", x: 200f, y: 200f);
            var b1 = MakeBlock(id: "block_right", x: 265f, y: 200f);
            var blocks = new[] { b0, b1 };
            var ball = MakeBall(x: 264.5f, y: 260f, vx: 10f, vy: -420f);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in blocks)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));
        }

        [Test]
        public void TightGap0_NotInsideEither()
        {
            var b0 = MakeBlock(id: "block_a", x: 200f, y: 200f);
            var b1 = MakeBlock(id: "block_b", x: 264f, y: 200f);
            var blocks = new[] { b0, b1 };
            var ball = MakeBall(x: 264f, y: 260f, vx: 0f, vy: -420f);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in blocks)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));
        }

        // ─── Test 5: 100 tick 벽 + 블록 동시 ───

        [Test]
        public void RightWallBlocks_100Ticks_XInRange()
        {
            var blocks = new[]
            {
                MakeBlock(id: "b0", x: 880f, y: 100f),
                MakeBlock(id: "b1", x: 880f, y: 132f),
                MakeBlock(id: "b2", x: 880f, y: 164f),
            };
            var ball = MakeBall(x: 840f, y: 150f, vx: 500f, vy: -200f);
            for (var tick = 0; tick < 100; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > 0)
                {
                    for (var i = 0; i < blocks.Length; i++)
                    {
                        if (r.BlockFacts.Any(f => f.BlockId == blocks[i].Id))
                            blocks[i] = blocks[i] with { IsDestroyed = true };
                    }
                }
                Assert.GreaterOrEqual(ball.X, 0f);
                Assert.LessOrEqual(ball.X, CanvasWidth);
            }
        }

        [Test]
        public void LeftWallBlocks_100Ticks_XInRange()
        {
            var blocks = new[]
            {
                MakeBlock(id: "b0", x: 0f, y: 100f),
                MakeBlock(id: "b1", x: 0f, y: 132f),
            };
            var ball = MakeBall(x: 120f, y: 150f, vx: -500f, vy: -200f);
            for (var tick = 0; tick < 100; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > 0)
                {
                    for (var i = 0; i < blocks.Length; i++)
                    {
                        if (r.BlockFacts.Any(f => f.BlockId == blocks[i].Id))
                            blocks[i] = blocks[i] with { IsDestroyed = true };
                    }
                }
                Assert.GreaterOrEqual(ball.X, 0f);
                Assert.LessOrEqual(ball.X, CanvasWidth);
            }
        }
    }
}
