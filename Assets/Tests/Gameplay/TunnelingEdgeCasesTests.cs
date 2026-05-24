using System;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 터널링 핵심 엣지 케이스 — 의심 1 (MAX_BOUNCE_COUNT) / 3 (hitBlockIds skip) / 4 (sanityCheck 경계).
    [TestFixture]
    public class TunnelingEdgeCasesTests
    {
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float BallRadius = 8f;

        private static bool IsBallInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        [Test]
        public void SimpleScenario_TwoStacked_NotInside()
        {
            var blockA = new BlockState("blockA", 460f, 200f, 1, false, "basic");
            var blockB = new BlockState("blockB", 460f, 168f, 1, false, "basic");
            var ball = new BallState("b0", 492f, 240f, 0f, -3000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { blockA, blockB },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, blockA));
            Assert.IsFalse(IsBallInsideBlock(r.Ball, blockB));
        }

        [Test]
        public void FiveBlockStack_3000pxs_NoTunneling()
        {
            var blocks = new BlockState[6];
            for (var i = 0; i < 6; i++)
                blocks[i] = new BlockState($"stack_{i}", 460f, 220f - i * (BlockHeight + 4f), 1, false, "basic");

            var ball = new BallState("ball_0", 492f, 270f, 0f, -3000f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 20; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                        if (current[i].Id == f.BlockId) current[i] = current[i] with { IsDestroyed = true };
                }
                foreach (var b in current)
                    Assert.IsFalse(IsBallInsideBlock(ball, b), $"tick={tick} ball.y={ball.Y:F2} inside {b.Id}(y={b.Y})");
                if (ball.Y < 0f) break;
            }
        }

        [Test]
        public void MaxBounceCount4_5thBlock_NoFreeAdvanceThrough()
        {
            var blocks = new BlockState[6];
            for (var i = 0; i < 6; i++)
                blocks[i] = new BlockState($"cap_{i}", 460f, 80f + i * (BlockHeight + 4f), 1, false, "basic");

            var ball = new BallState("ball_0", 492f, 260f, 0f, -6000f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 30; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                        if (current[i].Id == f.BlockId) current[i] = current[i] with { IsDestroyed = true };
                }
                foreach (var b in current)
                    Assert.IsFalse(IsBallInsideBlock(ball, b),
                        $"tick={tick} ball=({ball.X:F2},{ball.Y:F2}) vy={ball.Vy:F0} inside {b.Id}(y={b.Y})");
                if (ball.Y < 0f || ball.Y > 720f) break;
            }
        }

        [Test]
        public void ExtremeSpeed_10000pxs_6Stack_NoTunneling()
        {
            var blocks = new BlockState[6];
            for (var i = 0; i < 6; i++)
                blocks[i] = new BlockState($"extreme_{i}", 460f, 80f + i * (BlockHeight + 4f), 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 270f, 0f, -10000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, blocks,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in blocks)
                Assert.IsFalse(IsBallInsideBlock(r.Ball, b), $"ball.y={r.Ball.Y:F2} inside {b.Id}(y={b.Y})");
        }

        [Test]
        public void SanityCheck_BoundaryLeft_NotInside()
        {
            var block = new BlockState("boundary_block", 460f, 300f, 1, false, "basic");
            var ball = new BallState("ball_0", 445f, 312f, 600f, 0f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block));
            Assert.Less(r.Ball.Vx, 0f);
        }

        [Test]
        public void RealGameSpeed_420pxs_300Ticks_NoTunneling()
        {
            var blocks = new System.Collections.Generic.List<BlockState>();
            var idx = 0;
            for (var row = 0; row < 5; row++)
                for (var col = 0; col < 13; col++)
                    blocks.Add(new BlockState($"grid_{idx++}", 40f + col * (BlockWidth + 4f), 80f + row * (BlockHeight + 4f), 1, false, "basic"));

            var angle = -60f * MathF.PI / 180f;
            var ball = new BallState("ball_0", 480f, 640f, MathF.Cos(angle) * 420f, MathF.Sin(angle) * 420f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 300; tick++)
            {
                if (!ball.IsActive || ball.Y > 720f) break;
                var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                        if (current[i].Id == f.BlockId) current[i] = current[i] with { IsDestroyed = true };
                }
                if (ball.X - BallRadius <= 0f) ball = ball with { Vx = MathF.Abs(ball.Vx) };
                if (ball.X + BallRadius >= 960f) ball = ball with { Vx = -MathF.Abs(ball.Vx) };
                if (ball.Y - BallRadius <= 0f) ball = ball with { Vy = MathF.Abs(ball.Vy) };

                foreach (var b in current)
                    Assert.IsFalse(IsBallInsideBlock(ball, b),
                        $"tick={tick} ball=({ball.X:F2},{ball.Y:F2}) inside {b.Id} at ({b.X},{b.Y})");
            }
        }
    }
}
