using System;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // 터널링 Root Cause — 블록→벽→블록 재접근 + alreadyInside push-out + free-advance 새 블록.
    [TestFixture]
    public class TunnelingRootCauseTests
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

        // ─── 블록 → 벽 → 블록 재접근 ───

        [Test]
        public void BlockReflect_WallReflect_BlockReapproach_NoTunneling()
        {
            var block = new BlockState("block_A", 460f, 36f, 2, false, "basic");
            var ball = new BallState("ball_0", 492f, 48f, 0f, -3000f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block),
                $"ball.y={r.Ball.Y:F2} inside block(y={block.Y},bottom={block.Y + BlockHeight})");
        }

        [Test]
        public void PreciseDelta1px_HitBlockIdsSkip_NoFreeAdvance()
        {
            // 블록 expanded top = 92, 공 y=93 (1px 안쪽), vy=-750
            var block = new BlockState("precise_block", 460f, 100f, 2, false, "basic");
            var ball = new BallState("ball_0", 492f, 93f, 0f, -750f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block),
                $"ball=({r.Ball.X:F2},{r.Ball.Y:F2}) vy={r.Ball.Vy:F0} block y=[{block.Y},{block.Y + BlockHeight}]");
        }

        [Test]
        public void HalfPxDelta_NoTunneling()
        {
            var block = new BlockState("halfpx_block", 460f, 100f, 2, false, "basic");
            var ball = new BallState("ball_0", 492f, 92.5f, 0f, -750f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block),
                $"ball=({r.Ball.X:F3},{r.Ball.Y:F3}) vy={r.Ball.Vy:F0} blockFacts={r.BlockFacts.Count}");
        }

        // ─── alreadyInside push-out ───

        [Test]
        public void AlreadyInside_TopInsideMovingUp_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 95f, 0f, -300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "top-inside moving up");
        }

        [Test]
        public void AlreadyInside_BottomInsideMovingDown_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 129f, 0f, 300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "bottom-inside moving down");
        }

        [Test]
        public void AlreadyInside_LeftInsideMovingLeft_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 455f, 112f, -300f, 0f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "left-inside moving left");
        }

        [Test]
        public void AlreadyInside_RightInsideMovingRight_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 529f, 112f, 300f, 0f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "right-inside moving right");
        }

        [Test]
        public void AlreadyInside_TopInsideDiagonal_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 95f, 100f, -300f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "top-inside diagonal");
        }

        [Test]
        public void AlreadyInside_CornerDiagonal_PushedOut()
        {
            var block = new BlockState("inside_block", 460f, 100f, 1, false, "basic");
            var ball = new BallState("ball_0", 455f, 95f, -200f, -200f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block), "corner-inside diagonal");
        }

        // ─── free-advance 새 블록 통과 ───

        [Test]
        public void FreeAdvanceNewBlock_OverlappingExpandedAABB_NoTunneling()
        {
            var block1 = new BlockState("blk1", 460f, 100f, 2, false, "basic");
            var block2 = new BlockState("blk2", 460f, 64f, 1, false, "basic");
            var ball = new BallState("ball_0", 492f, 94f, 0f, 200f, true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, new[] { block1, block2 },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block1));
            Assert.IsFalse(IsBallInsideBlock(r.Ball, block2));
        }

        [Test]
        public void HighSpeed_5BlockChain_5thNotPassed()
        {
            var blocks = new BlockState[5];
            for (var i = 0; i < 5; i++)
                blocks[i] = new BlockState($"five_{i}", 460f, 400f - i * (BlockHeight + 20f), 2, false, "basic");

            var ball = new BallState("ball_0", 492f, 450f, 0f, -8000f, true);
            var current = blocks.ToArray();

            for (var tick = 0; tick < 20; tick++)
            {
                var r = MovementSystem.MoveBallWithCollisions(ball, 0.016f, current,
                    Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
                ball = r.Ball;
                foreach (var f in r.BlockFacts)
                {
                    for (var i = 0; i < current.Length; i++)
                    {
                        if (current[i].Id != f.BlockId) continue;
                        var newHits = current[i].RemainingHits - 1;
                        current[i] = current[i] with { RemainingHits = newHits, IsDestroyed = newHits <= 0 };
                    }
                }
                foreach (var b in current)
                    Assert.IsFalse(IsBallInsideBlock(ball, b),
                        $"tick={tick} ball.y={ball.Y:F2} inside {b.Id}(y={b.Y})");
                if (ball.Y < 0f) break;
            }
        }
    }
}
