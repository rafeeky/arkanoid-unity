using System;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    // Position correction + re-entry prevention + destroyed-block ghosting 회귀.
    [TestFixture]
    public class CollisionSweepTests
    {
        private const float BallRadius = 8f;
        private const float PushOutEps = 0.1f;  // matches CollisionService internals

        private static BallState MakeBall(float x = 480f, float y = 600f, float vx = 100f, float vy = -100f) =>
            new("ball_0", x, y, vx, vy, true);

        private static BlockState MakeBlock(string id = "block_0", float x = 90f, float y = 210f, bool destroyed = false) =>
            new(id, x, y, 1, destroyed, "basic");

        // ─── Test 1: position correction (반사 후 epsilon 이상 떨어져야) ───

        [Test]
        public void BlockTopHit_BallSnappedOutside()
        {
            const float blockTopFace = 210f;
            const float expandedTop = blockTopFace - BallRadius;  // 202

            var ball = MakeBall(x: 100f, y: 200f, vx: 0f, vy: 300f);
            var block = MakeBlock(x: 68f, y: 210f);

            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);

            Assert.Less(r.Ball.Vy, 0f);
            Assert.Less(r.Ball.Y, expandedTop - PushOutEps / 2f);
        }

        [Test]
        public void BlockLeftHit_BallSnappedOutside()
        {
            const float blockLeftFace = 200f;
            const float expandedLeft = blockLeftFace - BallRadius;  // 192

            var ball = MakeBall(x: 188f, y: 222f, vx: 300f, vy: 0f);
            var block = MakeBlock(x: 200f, y: 210f);

            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);

            Assert.Less(r.Ball.Vx, 0f);
            Assert.Less(r.Ball.X, expandedLeft - PushOutEps / 2f);
        }

        // ─── Test 2: 연속 2 프레임 재충돌 방지 ───

        [Test]
        public void TwoConsecutiveFrames_NoReHit()
        {
            var ball = MakeBall(x: 100f, y: 200f, vx: 0f, vy: 300f);
            var block = MakeBlock(x: 68f, y: 210f);
            const float dt = 1f / 60f;

            var frame1 = MovementSystem.MoveBallWithCollisions(ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.GreaterOrEqual(frame1.BlockFacts.Count, 1);
            Assert.AreEqual(block.Id, frame1.BlockFacts[0].BlockId);

            var frame2 = MovementSystem.MoveBallWithCollisions(frame1.Ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(frame2.BlockFacts.Any(f => f.BlockId == block.Id));
        }

        // ─── Test 3: 파괴된 블록 ghosting 방지 ───

        [Test]
        public void DestroyedBlock_NoCollision()
        {
            var ball = MakeBall(x: 100f, y: 180f, vx: 0f, vy: 300f);
            var destroyedBlock = MakeBlock(x: 68f, y: 210f, destroyed: true);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f, new[] { destroyedBlock },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.AreEqual(0, r.BlockFacts.Count);
        }

        [Test]
        public void MixedActiveAndDestroyed_DestroyedSkipped()
        {
            var ball = MakeBall(x: 100f, y: 180f, vx: 0f, vy: 300f);
            var destroyed = MakeBlock(id: "destroyed", x: 68f, y: 210f, destroyed: true);
            var active = MakeBlock(id: "active", x: 68f, y: 260f, destroyed: false);
            var r = MovementSystem.MoveBallWithCollisions(ball, 1f / 15f, new[] { destroyed, active },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(r.BlockFacts.Any(f => f.BlockId == "destroyed"));
        }

        // ─── Test 4: t=0 case (공이 블록 내부에서 시작) ───

        [Test]
        public void BallInsideExpandedAABB_NoReHitNextFrame()
        {
            var block = MakeBlock(id: "inner_block", x: 200f, y: 300f);
            var ball = MakeBall(x: 232f, y: 294f, vx: 0f, vy: 300f);  // y=294 inside expanded top (292)
            const float dt = 1f / 60f;

            var frame1 = MovementSystem.MoveBallWithCollisions(ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            var frame2 = MovementSystem.MoveBallWithCollisions(frame1.Ball, dt, new[] { block },
                Array.Empty<BorderBlockState>(), Array.Empty<DoorState>(), DefaultPhysics);
            Assert.IsFalse(frame2.BlockFacts.Any(f => f.BlockId == block.Id));
        }
    }
}
