using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class MovementSystemTests
    {
        private static readonly GameplayConfig BaseConfig = DefaultConfig;
        private static readonly PhysicsConfig BasePhysics = DefaultPhysics;
        private static readonly BarState BaseBar = new(480f, 660f, 120f, 420f, BarEffect.None);
        private static readonly BallState BaseBall = new("ball_0", 480f, 600f, 100f, -100f, true);
        private static readonly ItemDropState BaseItem = new("item_0", ItemType.Expand, 300f, 200f, 160f, false);

        // ─── MoveBar ───

        [Test]
        public void MoveBar_Right_Moves()
        {
            var result = MovementSystem.MoveBar(BaseBar, 1, 1f / 60f, BaseConfig);
            Assert.Greater(result.X, BaseBar.X);
        }

        [Test]
        public void MoveBar_Left_Moves()
        {
            var result = MovementSystem.MoveBar(BaseBar, -1, 1f / 60f, BaseConfig);
            Assert.Less(result.X, BaseBar.X);
        }

        [Test]
        public void MoveBar_DirectionZero_NoMove()
        {
            var result = MovementSystem.MoveBar(BaseBar, 0, 1f / 60f, BaseConfig);
            Assert.AreEqual(BaseBar.X, result.X);
        }

        [Test]
        public void MoveBar_LeftClamp()
        {
            var leftBar = BaseBar with { X = 5f };
            var result = MovementSystem.MoveBar(leftBar, -1, 1f / 60f, BaseConfig);
            Assert.GreaterOrEqual(result.X, BaseConfig.BaseBarWidth / 2f);
        }

        [Test]
        public void MoveBar_RightClamp()
        {
            var rightBar = BaseBar with { X = PlayfieldLayout.PlayfieldWidth - 5f };
            var result = MovementSystem.MoveBar(rightBar, 1, 1f / 60f, BaseConfig);
            Assert.LessOrEqual(result.X, PlayfieldLayout.PlayfieldWidth - BaseConfig.BaseBarWidth / 2f);
        }

        [Test]
        public void MoveBar_SpeedProportionalToDt()
        {
            var dt = 1f / 60f;
            var result = MovementSystem.MoveBar(BaseBar, 1, dt, BaseConfig);
            Assert.That(result.X, Is.EqualTo(BaseBar.X + BaseConfig.BarMoveSpeed * dt).Within(0.001f));
        }

        // ─── MoveBall ───

        [Test]
        public void MoveBall_Active_Linear()
        {
            var dt = 1f / 60f;
            var result = MovementSystem.MoveBall(BaseBall, dt);
            Assert.That(result.X, Is.EqualTo(BaseBall.X + BaseBall.Vx * dt).Within(0.001f));
            Assert.That(result.Y, Is.EqualTo(BaseBall.Y + BaseBall.Vy * dt).Within(0.001f));
        }

        [Test]
        public void MoveBall_Inactive_NoMove()
        {
            var inactive = BaseBall with { IsActive = false };
            var result = MovementSystem.MoveBall(inactive, 1f / 60f);
            Assert.AreEqual(inactive.X, result.X);
            Assert.AreEqual(inactive.Y, result.Y);
        }

        [Test]
        public void MoveBall_VelocityPreserved()
        {
            var result = MovementSystem.MoveBall(BaseBall, 1f / 60f);
            Assert.AreEqual(BaseBall.Vx, result.Vx);
            Assert.AreEqual(BaseBall.Vy, result.Vy);
        }

        // ─── MoveItemDrop ───

        [Test]
        public void MoveItemDrop_FallsDown()
        {
            var dt = 1f / 60f;
            var result = MovementSystem.MoveItemDrop(BaseItem, dt);
            Assert.That(result.Y, Is.EqualTo(BaseItem.Y + BaseItem.FallSpeed * dt).Within(0.001f));
        }

        [Test]
        public void MoveItemDrop_XUnchanged()
        {
            var result = MovementSystem.MoveItemDrop(BaseItem, 1f / 60f);
            Assert.AreEqual(BaseItem.X, result.X);
        }

        // ─── MoveAttachedBallToBar ───

        [Test]
        public void Inactive_NoAttach_RightOffset30()
        {
            var inactive = BaseBall with { IsActive = false };
            var result = MovementSystem.MoveAttachedBallToBar(inactive, BaseBar);
            // InitialLaunchOffsetX = 30 (발사 -60° 우측). Y = bar.Y - BarHeight (16).
            Assert.AreEqual(BaseBar.X + 30f, result.X);
            Assert.AreEqual(BaseBar.Y - 16f, result.Y);
        }

        [Test]
        public void Active_NoMove()
        {
            var result = MovementSystem.MoveAttachedBallToBar(BaseBall, BaseBar);
            Assert.AreEqual(BaseBall.X, result.X);
            Assert.AreEqual(BaseBall.Y, result.Y);
        }

        [Test]
        public void AttachedOffset_FollowsBar()
        {
            var attached = BaseBall with { IsActive = false, X = 480f + 30f, AttachedOffsetX = 30f };
            var movedBar = BaseBar with { X = 500f };
            var result = MovementSystem.MoveAttachedBallToBar(attached, movedBar);
            // 바 중심 500 + 오프셋 30 = 530
            Assert.AreEqual(530f, result.X);
        }

        [Test]
        public void AttachedY_BarTopSurface()
        {
            const float barHeight = 16f;
            const float ballRadius = 8f;
            var attached = BaseBall with { IsActive = false, AttachedOffsetX = 0f };
            var result = MovementSystem.MoveAttachedBallToBar(attached, BaseBar);
            Assert.AreEqual(BaseBar.Y - barHeight / 2f - ballRadius, result.Y);
        }

        [Test]
        public void AttachedOffset_FollowsBarOver2Steps()
        {
            const float offsetX = -20f;
            var attached = BaseBall with { IsActive = false, X = BaseBar.X + offsetX, AttachedOffsetX = offsetX };
            var step1Bar = BaseBar with { X = BaseBar.X + 50f };
            var step1Ball = MovementSystem.MoveAttachedBallToBar(attached, step1Bar);
            Assert.AreEqual(step1Bar.X + offsetX, step1Ball.X);

            var step2Bar = BaseBar with { X = step1Bar.X + 50f };
            var step2Ball = MovementSystem.MoveAttachedBallToBar(step1Ball, step2Bar);
            Assert.AreEqual(step2Bar.X + offsetX, step2Ball.X);
        }

        // ─── MoveBallWithCollisions ───

        [Test]
        public void MoveBallWithCollisions_NoBlocks_LinearMove()
        {
            var dt = 1f / 60f;
            var result = MovementSystem.MoveBallWithCollisions(BaseBall, dt,
                System.Array.Empty<BlockState>(),
                System.Array.Empty<BorderBlockState>(),
                System.Array.Empty<DoorState>(),
                BasePhysics);
            Assert.That(result.Ball.X, Is.EqualTo(BaseBall.X + BaseBall.Vx * dt).Within(0.01f));
            Assert.That(result.Ball.Y, Is.EqualTo(BaseBall.Y + BaseBall.Vy * dt).Within(0.01f));
            Assert.AreEqual(0, result.BlockFacts.Count);
        }

        [Test]
        public void MoveBallWithCollisions_Inactive_NoChange()
        {
            var inactive = BaseBall with { IsActive = false };
            var result = MovementSystem.MoveBallWithCollisions(inactive, 1f / 60f,
                System.Array.Empty<BlockState>(),
                System.Array.Empty<BorderBlockState>(),
                System.Array.Empty<DoorState>(),
                BasePhysics);
            Assert.AreEqual(inactive, result.Ball);
            Assert.AreEqual(0, result.BlockFacts.Count);
        }

        [Test]
        public void MoveBallWithCollisions_BlockTopHit_VyReversesDown()
        {
            var block = new BlockState("b", 448f, 300f, 1, false, "basic");
            // 공이 블록 expanded top(300-8=292) 바로 아래에서 빠르게 위로 이동.
            var ball = BaseBall with { X = 480f, Y = 310f, Vx = 0f, Vy = -400f };
            var result = MovementSystem.MoveBallWithCollisions(ball, 1f / 30f,
                new[] { block },
                System.Array.Empty<BorderBlockState>(),
                System.Array.Empty<DoorState>(),
                BasePhysics);
            Assert.GreaterOrEqual(result.BlockFacts.Count, 1);
            Assert.Greater(result.Ball.Vy, 0f);  // 반사 후 아래 방향
        }

        [Test]
        public void MoveBallWithCollisions_DestroyedBlock_NoFact()
        {
            var destroyed = new BlockState("b", 448f, 300f, 0, true, "basic");
            var ball = BaseBall with { X = 480f, Y = 380f, Vx = 0f, Vy = -400f };
            var result = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f,
                new[] { destroyed },
                System.Array.Empty<BorderBlockState>(),
                System.Array.Empty<DoorState>(),
                BasePhysics);
            Assert.AreEqual(0, result.BlockFacts.Count);
        }

        [Test]
        public void MoveBallWithCollisions_FastBall_NoTunneling()
        {
            var block = new BlockState("fast_block", 448f, 300f, 2, false, "basic");
            var ball = BaseBall with { X = 480f, Y = 500f, Vx = 0f, Vy = -1200f };
            var result = MovementSystem.MoveBallWithCollisions(ball, 1f / 60f,
                new[] { block },
                System.Array.Empty<BorderBlockState>(),
                System.Array.Empty<DoorState>(),
                BasePhysics);
            var inside = result.Ball.X > block.X && result.Ball.X < block.X + 64f
                && result.Ball.Y > block.Y && result.Ball.Y < block.Y + 24f;
            Assert.IsFalse(inside);
        }
    }
}
