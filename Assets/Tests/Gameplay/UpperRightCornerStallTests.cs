using System;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 우측 상단 코너 멈춤 + SanityCheck velocity 반전 방향 회귀.
    [TestFixture]
    public class UpperRightCornerStallTests
    {
        private const float BallRadius = 8f;
        private const float CanvasWidth = 720f;
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;

        private static BlockState MakeBlock(string id, float x, float y) =>
            new(id, x, y, 1, false, "basic");

        private static float Speed(BallState b) =>
            MathF.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);

        // ─── 우측 상단 구역 멈춤 ───

        [Test]
        public void UpperRightZone_NoStall()
        {
            var block = MakeBlock("upper_right_block", 870f, 80f);
            var ball = new BallState("ball_0", 920f, 85f, 200f, -300f, true);
            var blocks = new[] { block };
            var initialSpeed = Speed(ball);

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
            }
            Assert.Greater(Speed(ball), initialSpeed * 0.1f);
        }

        [Test]
        public void UpperRightWallCorner_NoStall()
        {
            var ball = new BallState("ball_0", CanvasWidth - 10f, 15f, 200f, -200f, true);
            var initialSpeed = Speed(ball);
            for (var tick = 0; tick < 20; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, Array.Empty<BlockState>(),
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
            }
            Assert.Greater(Speed(ball), initialSpeed * 0.5f);
            Assert.GreaterOrEqual(ball.X, BallRadius);
            Assert.LessOrEqual(ball.X, CanvasWidth - BallRadius);
            Assert.GreaterOrEqual(ball.Y, BallRadius);
        }

        [Test]
        public void Stage1RightmostTopBlock_NormalReflect()
        {
            // col=12, row=0: x = 40 + 12*68 = 856
            var block = MakeBlock("stage1_top_right", 856f, 80f);
            var ball = new BallState("ball_0", 900f, 120f, 200f, -300f, true);
            var blocks = new[] { block };
            var initialSpeed = Speed(ball);

            for (var tick = 0; tick < 30; tick++)
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
            }
            Assert.Greater(Speed(ball), initialSpeed * 0.1f);
            Assert.GreaterOrEqual(ball.X, BallRadius);
            Assert.LessOrEqual(ball.X, CanvasWidth - BallRadius);
        }

        // ─── SanityCheck velocity 반전 방향 정확성 ───

        [Test]
        public void SanityCheck_BallLeftMovingLeft_NoVxReverse()
        {
            var block = MakeBlock("b0", 400f, 200f);
            var ball = new BallState("ball_0", 420f, 212f, -200f, 100f, true);
            var r = MovementSystem.SanityCheckBallBlockSeparation(ball, new[] { block }, DefaultPhysics);
            Assert.IsTrue(r.WasInside);
            Assert.Less(r.Ball.Vx, 0f);  // vx 음수 유지
        }

        [Test]
        public void SanityCheck_BallAboveMovingUp_NoVyReverse()
        {
            var block = MakeBlock("b0", 400f, 200f);
            var ball = new BallState("ball_0", 432f, 205f, 100f, -300f, true);
            var r = MovementSystem.SanityCheckBallBlockSeparation(ball, new[] { block }, DefaultPhysics);
            Assert.IsTrue(r.WasInside);
            Assert.Less(r.Ball.Vy, 0f);  // vy 음수 유지
        }

        [Test]
        public void SanityCheck_BallInside_PushedOut()
        {
            var block = MakeBlock("b0", 400f, 200f);
            var ball = new BallState("ball_0", 432f, 212f, 200f, -300f, true);
            var r1 = MovementSystem.SanityCheckBallBlockSeparation(ball, new[] { block }, DefaultPhysics);
            Assert.IsTrue(r1.WasInside);
            var pushed = r1.Ball;
            var insideAfter = pushed.X > block.X && pushed.X < block.X + BlockWidth
                && pushed.Y > block.Y && pushed.Y < block.Y + BlockHeight;
            Assert.IsFalse(insideAfter);
        }

        // ─── 연속 틱 블록 내부 탈출 ───

        [Test]
        public void BallInside_EscapesIn20Ticks()
        {
            var block = MakeBlock("stuck_block", 880f, 80f);
            var ball = new BallState("ball_0", 900f, 92f, 200f, -300f, true);
            var blocks = new[] { block };
            var initialSpeed = Speed(ball);

            for (var tick = 0; tick < 20; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                var sanity = MovementSystem.SanityCheckBallBlockSeparation(r.Ball, blocks, DefaultPhysics);
                ball = sanity.Ball;

                if (r.BlockFacts.Count > 0 || sanity.WasInside)
                {
                    for (var i = 0; i < blocks.Length; i++)
                    {
                        if (r.BlockFacts.Any(f => f.BlockId == blocks[i].Id))
                            blocks[i] = blocks[i] with { IsDestroyed = true };
                    }
                }
            }
            Assert.Greater(Speed(ball), initialSpeed * 0.1f);
        }
    }
}
