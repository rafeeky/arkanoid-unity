using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 터널링 재현 — 의심 1/2/3/4 후보 시나리오.
    [TestFixture]
    public class TunnelingReproductionTests
    {
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float BlockGap = 4f;
        private const float BallRadius = 8f;
        private const float GridLeft = 40f;
        private const float GridTop = 80f;

        private static List<BlockState> BuildDenseGrid(int rows, int cols)
        {
            var blocks = new List<BlockState>(rows * cols);
            var idx = 0;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    blocks.Add(new BlockState($"b_{idx++}",
                        GridLeft + c * (BlockWidth + BlockGap),
                        GridTop + r * (BlockHeight + BlockGap),
                        1, false, "basic"));
                }
            }
            return blocks;
        }

        private static bool IsBallInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        // ─── 고속 공 단일 틱 다중 통과 ───

        [Test]
        public void Vy600_60Ticks_NoTunneling()
        {
            var blocks = BuildDenseGrid(5, 13);
            var ball = new BallState("ball_0", 480f, 230f, 50f, -600f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 60; tick++)
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
                        $"tick={tick} ball=({ball.X:F2},{ball.Y:F2}) block={b.Id}({b.X},{b.Y})");
            }
        }

        [Test]
        public void Vy2000_6ContiguousVertical_NoTunneling()
        {
            var blocks = new BlockState[6];
            for (var i = 0; i < 6; i++)
                blocks[i] = new BlockState($"col_block_{i}", 460f, 100f + i * (BlockHeight + BlockGap), 1, false, "basic");

            var ball = new BallState("ball_0", 492f, 270f, 0f, -2000f, true);
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
                        $"tick={tick} ball=({ball.X:F2},{ball.Y:F2}) vy={ball.Vy:F1} block={b.Id}");
            }
        }

        // ─── hitBlockIds skip 후 free-advance ───

        [Test]
        public void Vy6000_SingleBlock_OneTickFullPass_NotInside()
        {
            var block = new BlockState("single_block", 460f, 300f, 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 340f, 0f, -6000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block));
        }

        // ─── 한 틱 다중 파괴 방지 ───

        [Test]
        public void Vy600_Std_AtMost2BlocksPerTick()
        {
            var blocks = BuildDenseGrid(5, 13);
            var ball = new BallState("ball_0", 480f, 600f, 0f, -600f, true);
            var current = blocks.ToArray();
            var maxPerTick = 0;

            for (var tick = 0; tick < 200; tick++)
            {
                if (!ball.IsActive || ball.Y < 0f) break;
                var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > maxPerTick) maxPerTick = r.BlockFacts.Count;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                        if (current[i].Id == f.BlockId) current[i] = current[i] with { IsDestroyed = true };
                }
                if (ball.X - BallRadius <= 0f) ball = ball with { Vx = MathF.Abs(ball.Vx) };
                if (ball.X + BallRadius >= 960f) ball = ball with { Vx = -MathF.Abs(ball.Vx) };
                if (ball.Y - BallRadius <= 0f) ball = ball with { Vy = MathF.Abs(ball.Vy) };
            }
            Assert.LessOrEqual(maxPerTick, 2);
        }

        [Test]
        public void HighSpeed_1400_AtMost3BlocksPerTick()
        {
            var blocks = BuildDenseGrid(5, 13);
            var angle = -75f * MathF.PI / 180f;
            const float speed = 1400f;
            var ball = new BallState("ball_0", 480f, 600f, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed, true);
            var current = blocks.ToArray();
            var maxDestroyed = 0;

            for (var tick = 0; tick < 200; tick++)
            {
                if (!ball.IsActive || ball.Y < 0f) break;
                var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                if (r.BlockFacts.Count > maxDestroyed) maxDestroyed = r.BlockFacts.Count;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                        if (current[i].Id == f.BlockId) current[i] = current[i] with { IsDestroyed = true };
                }
                if (ball.X - BallRadius <= 0f) ball = ball with { Vx = MathF.Abs(ball.Vx) };
                if (ball.X + BallRadius >= 960f) ball = ball with { Vx = -MathF.Abs(ball.Vx) };
                if (ball.Y - BallRadius <= 0f) ball = ball with { Vy = MathF.Abs(ball.Vy) };
            }
            Assert.Less(maxDestroyed, 3, $"max destroyed per tick={maxDestroyed}");
        }

        // ─── free-advance 가드 ───

        [Test]
        public void FreeAdvanceGuard_BlockNotPenetrated()
        {
            var block = new BlockState("guard_block", 460f, 100f, 2, false, "basic");
            var ball = new BallState("ball_0", 492f, 108f, 0f, 2000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block));
        }

        // ─── 수직 스택 관통 금지 ───

        [Test]
        public void VerticalStack6_Vy3000_NoTunneling()
        {
            var stack = new BlockState[6];
            for (var i = 0; i < 6; i++)
                stack[i] = new BlockState($"vs_{i}", 460f, 80f + i * (BlockHeight + BlockGap), 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 300f, 0f, -3000f, true);
            var current = stack.ToArray();

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
                        $"tick={tick} ball=({ball.X:F2},{ball.Y:F2}) vy={ball.Vy:F1} inside {b.Id}({b.X},{b.Y})");
                if (ball.Y - BallRadius < 0f || ball.Y + BallRadius > 720f) break;
            }
        }
    }
}
