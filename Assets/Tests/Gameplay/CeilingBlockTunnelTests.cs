using System;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // MVP1 회귀: 천장 반사 직후 블록 통과 버그.
    [TestFixture]
    public class CeilingBlockTunnelTests
    {
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float BallRadius = 8f;

        private static BlockState MakeBlock(string id, float x, float y) =>
            new(id, x, y, 1, false, "basic");

        // 결정론적 재현 시도 1: 천장 스냅 후 row 0 블록 통과 여부 (vy=840).
        [Test]
        public void BallNearCeiling_RowZeroBlock_HitNoTunneling()
        {
            var ball = new BallState("ball_0", 360f, 8.5f, 420f, 840f, true);
            var block = MakeBlock("b_0", 360f - BlockWidth / 2f, 80f);
            var blocks = new[] { block };

            const float dt = 1f / 60f;
            var current = ball;
            var hitCount = 0;
            var tunnelDetected = false;

            for (var tick = 0; tick < 10; tick++)
            {
                var result = MovementSystem.MoveBallWithCollisions(current, dt, blocks,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                foreach (var f in result.BlockFacts)
                {
                    if (f.BlockId == "b_0") hitCount++;
                }
                current = result.Ball;

                if (current.X > block.X && current.X < block.X + BlockWidth
                    && current.Y > block.Y && current.Y < block.Y + BlockHeight)
                {
                    tunnelDetected = true;
                    break;
                }
            }

            Assert.IsFalse(tunnelDetected);
            Assert.GreaterOrEqual(hitCount, 1);
        }

        // 결정론적 재현 시도 2: 고속 공 (vy=2400) 천장→블록 1틱 이내 도달.
        [Test]
        public void HighSpeedBall_NearCeiling_RowZeroBlock_NotSkipped()
        {
            var ball = new BallState("ball_0", 360f, 8.5f, 0f, 2400f, true);
            var block = MakeBlock("b_0", 360f - BlockWidth / 2f, 80f);
            var blocks = new[] { block };

            var result = MovementSystem.MoveBallWithCollisions(ball, 1f / 30f, blocks,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);

            var bx = block.X;
            var by = block.Y;
            var inside = result.Ball.X > bx && result.Ball.X < bx + BlockWidth
                && result.Ball.Y > by && result.Ball.Y < by + BlockHeight;
            Assert.IsFalse(inside);

            var blockHit = false;
            foreach (var f in result.BlockFacts)
            {
                if (f.BlockId == "b_0") { blockHit = true; break; }
            }
            Assert.IsTrue(blockHit);
        }
    }
}
