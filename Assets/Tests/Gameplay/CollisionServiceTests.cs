using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class CollisionServiceTests
    {
        private static BallState ActiveBall(string id = "ball_0", float x = 480f, float y = 600f, float vx = 0f, float vy = 100f) =>
            new(id, x, y, vx, vy, true);

        private static BarState MakeBar(float x = 480f, float y = 660f, float width = 120f) =>
            new(x, y, width, 420f, BarEffect.None);

        private static BlockState MakeBlock(string id = "block_0", float x = 200f, float y = 100f, bool destroyed = false) =>
            new(id, x, y, 1, destroyed, "basic");

        // ─── Wall collision ───

        [Test]
        public void Ball_AtLeftEdge_DetectsWallLeft()
        {
            var ball = ActiveBall(x: 4f, y: 400f, vx: -100f, vy: 0f);
            var state = MakeState(balls: new[] { ball });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new BallHitWallFact("ball_0", WallSide.Left)));
        }

        [Test]
        public void Ball_AtRightEdge_DetectsWallRight()
        {
            // PlayfieldWidth 720, ball radius 8 → x >= 712 면 right.
            var ball = ActiveBall(x: 716f, y: 400f, vx: 100f, vy: 0f);
            var state = MakeState(balls: new[] { ball });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new BallHitWallFact("ball_0", WallSide.Right)));
        }

        [Test]
        public void Ball_AtTopEdge_DetectsWallTop()
        {
            var ball = ActiveBall(x: 400f, y: 4f, vx: 0f, vy: -100f);
            var state = MakeState(balls: new[] { ball });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new BallHitWallFact("ball_0", WallSide.Top)));
        }

        // ─── Floor ───

        [Test]
        public void Ball_BelowFloor_DetectsFloor()
        {
            // PlayfieldHeight 720, ball radius 8 → y - 8 > 720 → y > 728.
            var ball = ActiveBall(x: 400f, y: 740f);
            var state = MakeState(balls: new[] { ball });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new BallHitFloorFact("ball_0")));
        }

        [Test]
        public void Ball_AboveFloor_DoesNotDetect()
        {
            var ball = ActiveBall(x: 400f, y: 700f);
            var state = MakeState(balls: new[] { ball });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.IsFalse(facts.Any(f => f is BallHitFloorFact));
        }

        // ─── Bar ───

        [Test]
        public void BarCenter_ContactX_NearZero()
        {
            var bar = MakeBar(x: 480f, y: 660f, width: 120f);
            var ball = ActiveBall(x: 480f, y: 652f, vx: 0f, vy: 100f);
            var prevBall = ball with { Y = 640f };
            var state = MakeState(balls: new[] { ball }, bar: bar);
            var prevState = MakeState(balls: new[] { prevBall }, bar: bar);
            var facts = CollisionService.DetectCollisions(state, prevState);
            var barFact = facts.OfType<BallHitBarFact>().FirstOrDefault();
            Assert.IsNotNull(barFact);
            Assert.That(barFact!.BarContactX, Is.EqualTo(0f).Within(0.1f));
        }

        [Test]
        public void BarLeftEdge_ContactXMinus1()
        {
            var bar = MakeBar(x: 480f, y: 660f, width: 120f);
            // Left edge of bar = 480 - 60 = 420
            var ball = ActiveBall(x: 420f, y: 652f, vx: -100f, vy: 100f);
            var prevBall = ball with { Y = 640f, Vy = 100f };
            var state = MakeState(balls: new[] { ball }, bar: bar);
            var prevState = MakeState(balls: new[] { prevBall }, bar: bar);
            var facts = CollisionService.DetectCollisions(state, prevState);
            var barFact = facts.OfType<BallHitBarFact>().FirstOrDefault();
            Assert.IsNotNull(barFact);
            Assert.That(barFact!.BarContactX, Is.EqualTo(-1f).Within(0.1f));
        }

        [Test]
        public void BarRightEdge_ContactX1()
        {
            var bar = MakeBar(x: 480f, y: 660f, width: 120f);
            // Right edge = 540
            var ball = ActiveBall(x: 540f, y: 652f, vx: 100f, vy: 100f);
            var prevBall = ball with { Y = 640f, Vy = 100f };
            var state = MakeState(balls: new[] { ball }, bar: bar);
            var prevState = MakeState(balls: new[] { prevBall }, bar: bar);
            var facts = CollisionService.DetectCollisions(state, prevState);
            var barFact = facts.OfType<BallHitBarFact>().FirstOrDefault();
            Assert.IsNotNull(barFact);
            Assert.That(barFact!.BarContactX, Is.EqualTo(1f).Within(0.1f));
        }

        [Test]
        public void BallMovingUp_DoesNotDetectBar()
        {
            var bar = MakeBar(x: 480f, y: 660f, width: 120f);
            var ball = ActiveBall(x: 480f, y: 652f, vx: 0f, vy: -100f);
            var prevBall = ball with { Vy = -100f };
            var state = MakeState(balls: new[] { ball }, bar: bar);
            var prevState = MakeState(balls: new[] { prevBall }, bar: bar);
            var facts = CollisionService.DetectCollisions(state, prevState);
            Assert.IsFalse(facts.Any(f => f is BallHitBarFact));
        }

        // ─── Block side detection ───

        [Test]
        public void BlockTop_SideTop()
        {
            // Block (200, 100), 64×24. Center (232, 112).
            var block = MakeBlock(x: 200f, y: 100f);
            var ball = ActiveBall(x: 232f, y: 108f, vx: 0f, vy: 100f);
            var prevBall = ball with { Y = 80f };
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var prevState = MakeState(balls: new[] { prevBall }, blocks: new[] { block });
            var facts = CollisionService.DetectCollisions(state, prevState);
            var blockFact = facts.OfType<BallHitBlockFact>().FirstOrDefault();
            Assert.IsNotNull(blockFact);
            Assert.AreEqual(BlockSide.Top, blockFact!.Side);
        }

        [Test]
        public void BlockBottom_SideBottom()
        {
            var block = MakeBlock(x: 200f, y: 100f);
            var ball = ActiveBall(x: 232f, y: 118f, vx: 0f, vy: -100f);
            var prevBall = ball with { Y = 140f };
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var prevState = MakeState(balls: new[] { prevBall }, blocks: new[] { block });
            var facts = CollisionService.DetectCollisions(state, prevState);
            var blockFact = facts.OfType<BallHitBlockFact>().FirstOrDefault();
            Assert.IsNotNull(blockFact);
            Assert.AreEqual(BlockSide.Bottom, blockFact!.Side);
        }

        [Test]
        public void BlockLeft_SideLeft()
        {
            var block = MakeBlock(x: 200f, y: 100f);
            var ball = ActiveBall(x: 204f, y: 112f, vx: 100f, vy: 0f);
            var prevBall = ball with { X = 180f };
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var prevState = MakeState(balls: new[] { prevBall }, blocks: new[] { block });
            var facts = CollisionService.DetectCollisions(state, prevState);
            var blockFact = facts.OfType<BallHitBlockFact>().FirstOrDefault();
            Assert.IsNotNull(blockFact);
            Assert.AreEqual(BlockSide.Left, blockFact!.Side);
        }

        [Test]
        public void BlockRight_SideRight()
        {
            var block = MakeBlock(x: 200f, y: 100f);
            var ball = ActiveBall(x: 258f, y: 112f, vx: -100f, vy: 0f);
            var prevBall = ball with { X = 280f };
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var prevState = MakeState(balls: new[] { prevBall }, blocks: new[] { block });
            var facts = CollisionService.DetectCollisions(state, prevState);
            var blockFact = facts.OfType<BallHitBlockFact>().FirstOrDefault();
            Assert.IsNotNull(blockFact);
            Assert.AreEqual(BlockSide.Right, blockFact!.Side);
        }

        [Test]
        public void MultipleBlockOverlap_ReturnsOnlyClosest()
        {
            var block1 = MakeBlock(id: "block_0", x: 200f, y: 100f);
            var block2 = MakeBlock(id: "block_1", x: 264f, y: 100f);  // 인접
            var ball = ActiveBall(x: 230f, y: 112f, vx: 0f, vy: -100f);
            var prevBall = ball with { Y = 80f };
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block1, block2 });
            var prevState = MakeState(balls: new[] { prevBall }, blocks: new[] { block1, block2 });
            var facts = CollisionService.DetectCollisions(state, prevState);
            var blockFacts = facts.OfType<BallHitBlockFact>().ToList();
            Assert.AreEqual(1, blockFacts.Count);
        }

        [Test]
        public void DestroyedBlock_NoDetection()
        {
            var block = MakeBlock(destroyed: true);
            var ball = ActiveBall(x: 232f, y: 112f, vx: 0f, vy: 100f);
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.IsFalse(facts.Any(f => f is BallHitBlockFact));
        }

        // ─── Item ───

        [Test]
        public void ItemOverlapBar_DetectsPickup()
        {
            var bar = MakeBar(x: 480f, y: 660f, width: 120f);
            var item = new ItemDropState("item_0", ItemType.Expand, X: 480f, Y: 660f, FallSpeed: 160f, IsCollected: false);
            var state = MakeState(bar: bar, itemDrops: new[] { item });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new ItemPickedUpFact("item_0")));
        }

        [Test]
        public void ItemBelowFloor_DetectsFellOff()
        {
            var item = new ItemDropState("item_0", ItemType.Expand, X: 300f, Y: 740f, FallSpeed: 160f, IsCollected: false);
            var state = MakeState(itemDrops: new[] { item });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.That(facts, Has.Member(new ItemFellOffFloorFact("item_0")));
        }

        [Test]
        public void CollectedItem_NoFellOffDetection()
        {
            var item = new ItemDropState("item_0", ItemType.Expand, X: 300f, Y: 740f, FallSpeed: 160f, IsCollected: true);
            var state = MakeState(itemDrops: new[] { item });
            var facts = CollisionService.DetectCollisions(state, state);
            Assert.IsFalse(facts.Any(f => f is ItemFellOffFloorFact));
        }
    }
}
