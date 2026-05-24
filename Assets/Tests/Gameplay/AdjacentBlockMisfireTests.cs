using System;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 인접 블록 그리드에서 잘못된 블록 hit / 공 내부 진입 회귀.
    // Stage 1 layout: BlockWidth=64, BlockHeight=24, BlockGap=4, BallRadius=8.
    [TestFixture]
    public class AdjacentBlockMisfireTests
    {
        private const float BlockWidth = 64f;
        private const float BlockHeight = 24f;
        // 이 테스트는 옛 stage 1 좌표 (LeftMargin=40) 기준 — PlayfieldLayout (56) 와 다름.
        // TS test 가 자체 const 사용. 일관성 위해 그대로 직역.
        private const float LeftMargin = 40f;
        private const float BlockGap = 4f;
        private const float BlockColStride = BlockWidth + BlockGap;   // 68
        private const float BlockRowStride = BlockHeight + BlockGap;  // 28
        private const float GridStartY = 80f;

        private static BlockState BlockAt(int col, int row, string id) =>
            new(id, LeftMargin + col * BlockColStride, GridStartY + row * BlockRowStride, 1, false, "basic");

        private static bool IsBallCenterInsideBlock(BallState ball, BlockState block)
        {
            if (block.IsDestroyed) return false;
            return ball.X > block.X && ball.X < block.X + BlockWidth
                && ball.Y > block.Y && ball.Y < block.Y + BlockHeight;
        }

        // 2×2 grid:
        //   A(0,0) x=40 y=80  / B(1,0) x=108 y=80
        //   C(0,1) x=40 y=108 / D(1,1) x=108 y=108
        private static readonly BlockState A = BlockAt(0, 0, "A");
        private static readonly BlockState B = BlockAt(1, 0, "B");
        private static readonly BlockState C = BlockAt(0, 1, "C");
        private static readonly BlockState D = BlockAt(1, 1, "D");
        private static readonly BlockState[] All2x2 = { A, B, C, D };

        // ─── Test 1: A top-right 모서리 접근 ───

        [Test]
        public void BallApproachesARight_NotInsideAnyBlock()
        {
            var ball = new BallState("ball_0", 104f, 60f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, All2x2,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);

            foreach (var b in All2x2)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));

            if (r.BlockFacts.Count > 0)
            {
                foreach (var f in r.BlockFacts)
                    Assert.That(new[] { "A", "B" }, Contains.Item(f.BlockId));
            }
        }

        // ─── Test 2: diagonal overlap 구역 ───

        [Test]
        public void DiagonalX103_NoInsideAnyBlock()
        {
            var ball = new BallState("ball_0", 103f, 60f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, All2x2,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in All2x2)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));
        }

        [Test]
        public void DiagonalX109_NoInsideAnyBlock()
        {
            var ball = new BallState("ball_0", 109f, 60f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, All2x2,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in All2x2)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));
        }

        [Test]
        public void DiagonalX106_BottomRowNotEntered()
        {
            var ball = new BallState("ball_0", 106f, 60f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, All2x2,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, C));
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, D));
        }

        // ─── Test 3: 4px 간격 사이 수직 진입 ───

        [Test]
        public void Gap4Center_NoInsideAndNoCDHit()
        {
            var ball = new BallState("ball_0", 106f, 50f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, All2x2,
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            foreach (var b in All2x2)
                Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, b));
            foreach (var f in r.BlockFacts)
            {
                Assert.AreNotEqual("C", f.BlockId);
                Assert.AreNotEqual("D", f.BlockId);
            }
        }

        // ─── Test 4: y축 overlap ───

        [Test]
        public void AcAxisUp_NoInside()
        {
            var ball = new BallState("ball_0", 72f, 130f, 0f, -300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, new[] { A, C },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, A));
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, C));
        }

        [Test]
        public void AcAxisDown_NoInside()
        {
            var ball = new BallState("ball_0", 72f, 95f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.1f, new[] { A, C },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, A));
            Assert.IsFalse(IsBallCenterInsideBlock(r.Ball, C));
        }

        // ─── Test 5: 100 tick 시뮬레이션 ───

        [Test]
        public void Sim100Ticks_NoBlockInsideAnywhere()
        {
            var speeds = new[] { 300f, 600f };
            var angles = new[] { -45f, -60f, -75f, -90f, -120f, -135f };

            foreach (var speed in speeds)
            {
                foreach (var angleDeg in angles)
                {
                    var rad = angleDeg * MathF.PI / 180f;
                    var ball = new BallState("ball_0", 106f, 400f,
                        MathF.Cos(rad) * speed, MathF.Sin(rad) * speed, true);
                    var currentBlocks = All2x2.ToArray();

                    for (var tick = 0; tick < 100; tick++)
                    {
                        if (ball.Y < 0f || ball.Y > 720f) break;
                        var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, currentBlocks,
                            Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                        ball = r.Ball;

                        foreach (var f in r.BlockFacts)
                        {
                            for (var i = 0; i < currentBlocks.Length; i++)
                            {
                                if (currentBlocks[i].Id == f.BlockId)
                                    currentBlocks[i] = currentBlocks[i] with { IsDestroyed = true };
                            }
                        }

                        foreach (var b in currentBlocks)
                        {
                            if (IsBallCenterInsideBlock(ball, b))
                            {
                                Assert.Fail($"[angle={angleDeg}° speed={speed}] tick={tick}: ball inside {b.Id} at ({ball.X:F2},{ball.Y:F2})");
                            }
                        }
                    }
                }
            }
        }
    }
}
