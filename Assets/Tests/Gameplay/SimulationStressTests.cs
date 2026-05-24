using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 실제 게임플레이 수준 시뮬레이션 스트레스 — 다양한 발사각·속도·dt 조합 터널링 검증.
    [TestFixture]
    public class SimulationStressTests
    {
        private const float BallRadius = 8f;
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        private const float CanvasWidth = 720f;
        private const float CanvasHeight = 720f;
        private const float BlockGridStartY = 80f;
        private const float BlockGridLeftMargin = 56f;
        private const float BlockGap = 4f;

        private static List<BlockState> BuildStage1Blocks()
        {
            const int rows = 5;
            const int cols = 13;
            var blocks = new List<BlockState>(rows * cols);
            var idx = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var x = BlockGridLeftMargin + col * (BlockWidth + BlockGap);
                    var y = BlockGridStartY + row * (BlockHeight + BlockGap);
                    blocks.Add(new BlockState($"block_{idx}", x, y, 1, false, "basic"));
                    idx++;
                }
            }
            return blocks;
        }

        private static bool IsBallCenterInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        private static BallState ApplyWallReflections(BallState ball)
        {
            var vx = ball.Vx;
            var vy = ball.Vy;
            var x = ball.X;
            var y = ball.Y;
            if (x - BallRadius <= 0f) { x = BallRadius + 1f; vx = MathF.Abs(vx); }
            else if (x + BallRadius >= CanvasWidth) { x = CanvasWidth - BallRadius - 1f; vx = -MathF.Abs(vx); }
            if (y - BallRadius <= 0f) { y = BallRadius + 1f; vy = MathF.Abs(vy); }
            return ball with { X = x, Y = y, Vx = vx, Vy = vy };
        }

        // 시뮬레이션 실행 — 터널 발견 시 fail 메시지.
        private static string? RunSimulation(BallState startBall, List<BlockState> blocks, float dt, int maxTicks, string label)
        {
            var ball = startBall;
            var current = blocks.ToList();

            for (var tick = 0; tick < maxTicks; tick++)
            {
                if (!ball.IsActive) break;
                if (ball.Y - BallRadius > CanvasHeight) break;

                var r = MovementSystem.MoveBallWithCollisions(ball, dt, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                foreach (var fact in r.BlockFacts)
                {
                    for (var i = 0; i < current.Count; i++)
                    {
                        if (current[i].Id == fact.BlockId)
                            current[i] = current[i] with { IsDestroyed = true };
                    }
                }
                ball = ApplyWallReflections(ball);

                foreach (var block in current)
                {
                    if (IsBallCenterInsideBlock(ball, block))
                    {
                        return $"[{label}] tick={tick} ball=({ball.X:F2},{ball.Y:F2}) vx={ball.Vx:F1} vy={ball.Vy:F1} inside {block.Id} at ({block.X},{block.Y})";
                    }
                }
            }
            return null;
        }

        // ─── Suite 1: angles × speeds × dts (3000 tick) ───

        [Test]
        public void Stress_AnglesSpeedsDts_3000Ticks_NoTunneling()
        {
            var anglesRight = new List<float>();
            for (var d = -80f; d <= -30f; d += 5f) anglesRight.Add(d);
            var anglesLeft = new List<float>();
            for (var d = -150f; d <= -100f; d += 5f) anglesLeft.Add(d);
            var angles = anglesRight.Concat(anglesLeft).ToArray();
            var speeds = new[] { 420f, 600f, 900f };
            var dts = new[] { 1f / 60f, 1f / 30f };
            const int maxTicks = 3000;

            var blocks = BuildStage1Blocks();
            var failures = new List<string>();

            foreach (var angleDeg in angles)
            {
                foreach (var speed in speeds)
                {
                    foreach (var dt in dts)
                    {
                        var rad = angleDeg * MathF.PI / 180f;
                        var vx = MathF.Cos(rad) * speed;
                        var vy = MathF.Sin(rad) * speed;
                        var ball = new BallState("ball_0", 480f, 600f, vx, vy, true);
                        var label = $"angle={angleDeg}° speed={speed} dt={dt:F4}";
                        var failure = RunSimulation(ball, blocks, dt, maxTicks, label);
                        if (failure != null) failures.Add(failure);
                    }
                }
            }
            if (failures.Count > 0) Assert.Fail(string.Join("\n", failures));
        }

        // ─── Suite 2: 바 반사 후 barContactX × 속도 (1000 tick) ───

        [Test]
        public void Stress_BarReflectContactX_1000Ticks_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const int maxTicks = 1000;
            var speeds = new[] { 420f, 600f };

            var contactSamples = new List<float>();
            for (var i = 0; i <= 19; i++)
                contactSamples.Add(-0.9f + i / 19f * 1.8f);

            var failures = new List<string>();
            foreach (var speed in speeds)
            {
                foreach (var contactX in contactSamples)
                {
                    var rawVx = contactX * speed * 0.7f;
                    var vyMag = MathF.Sqrt(MathF.Max(speed * speed - rawVx * rawVx, MathF.Pow(speed * 0.3f, 2f)));
                    var vy = -vyMag;
                    var label = $"barContactX={contactX:F3} speed={speed}";

                    var ball = new BallState("ball_0", 480f, 644f, rawVx, vy, true);
                    var failure = RunSimulation(ball, blocks, 1f / 60f, maxTicks, label);
                    if (failure != null) failures.Add(failure);
                }
            }
            if (failures.Count > 0) Assert.Fail(string.Join("\n", failures));
        }

        // ─── Suite 3: 극단적 조건 ───

        [Test]
        public void Extreme_Speed1400_60fps_1000Ticks_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const float speed = 1400f;
            var rad = -60f * MathF.PI / 180f;
            var ball = new BallState("ball_0", 480f, 600f, MathF.Cos(rad) * speed, MathF.Sin(rad) * speed, true);
            var failure = RunSimulation(ball, blocks, 1f / 60f, 1000, "extreme-speed-1400");
            Assert.IsNull(failure, failure);
        }

        [Test]
        public void Extreme_Speed120_30fps_2000Ticks_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const float speed = 120f;
            var rad = -75f * MathF.PI / 180f;
            var ball = new BallState("ball_0", 480f, 600f, MathF.Cos(rad) * speed, MathF.Sin(rad) * speed, true);
            var failure = RunSimulation(ball, blocks, 1f / 30f, 2000, "slow-ball-120");
            Assert.IsNull(failure, failure);
        }

        [Test]
        public void Diagonal_45Deg_CornerApproach_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const float speed = 420f;
            var rad = -45f * MathF.PI / 180f;
            var ball = new BallState("ball_0", 120f, 400f, MathF.Cos(rad) * speed, MathF.Sin(rad) * speed, true);
            var failure = RunSimulation(ball, blocks, 1f / 60f, 2000, "diagonal-corner-approach");
            Assert.IsNull(failure, failure);
        }

        [Test]
        public void NearVertical_80Deg_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const float speed = 420f;
            var rad = -80f * MathF.PI / 180f;
            var ball = new BallState("ball_0", 480f, 500f, MathF.Cos(rad) * speed, MathF.Sin(rad) * speed, true);
            var failure = RunSimulation(ball, blocks, 1f / 60f, 1000, "near-vertical-entry");
            Assert.IsNull(failure, failure);
        }

        [Test]
        public void BetweenBlockGap_NoTunneling()
        {
            var blocks = BuildStage1Blocks();
            const float speed = 420f;
            // gap center between col 0,1: x = 56 + 64 + 2 = 122 (NOTE: LeftMargin=56 — TS test 의 40 과 다름)
            // 일관성 위해 *내 PlayfieldLayout* 의 LeftMargin (56) 사용.
            var ball = new BallState("ball_0", 122f, 600f, 0f, -speed, true);
            var failure = RunSimulation(ball, blocks, 1f / 60f, 500, "between-block-gap");
            Assert.IsNull(failure, failure);
        }
    }
}
