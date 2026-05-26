using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class CollisionResolutionServiceTests
    {
        private static readonly IReadOnlyDictionary<string, BlockDefinition> BlockDefs = new Dictionary<string, BlockDefinition>
        {
            ["basic"] = new("basic", 1, 10, null, 0xCCCCCC),
            ["basic_drop"] = new("basic_drop", 1, 10, ItemType.Expand, 0xCCCCCC),
            ["tough"] = new("tough", 2, 30, null, 0xCCCCCC),
        };

        private static readonly IReadOnlyDictionary<ItemType, ItemDefinition> ItemDefs = new Dictionary<ItemType, ItemDefinition>
        {
            [ItemType.Expand] = new(ItemType.Expand, "txt_item_expand_name", "txt_item_expand_desc", "icon_item_expand", 160f, ItemType.Expand, ExpandMultiplier: 1.5f),
        };

        private static ResolutionTables Tables => new(BlockDefs, ItemDefs, DefaultConfig);

        private static BallState AB(string id = "ball_0", float x = 480f, float y = 600f, float vx = 100f, float vy = 100f, bool isActive = true) =>
            new(id, x, y, vx, vy, isActive);

        private static BallState First(GameplayRuntimeState s) =>
            s.Balls.Length > 0 ? s.Balls[0] : throw new System.Exception("No ball");

        // ─── Wall reflection ───

        [Test]
        public void WallLeft_VxReversed()
        {
            var ball = AB(vx: -200f, vy: 100f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Left) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.Greater(First(r.NextState).Vx, 0f);
            Assert.AreEqual(ball.Vy, First(r.NextState).Vy);
        }

        [Test]
        public void WallRight_VxReversed()
        {
            var ball = AB(vx: 200f, vy: 100f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Right) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.Less(First(r.NextState).Vx, 0f);
            Assert.AreEqual(ball.Vy, First(r.NextState).Vy);
        }

        [Test]
        public void WallTop_VyReversed()
        {
            var ball = AB(vx: 100f, vy: -200f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.Greater(First(r.NextState).Vy, 0f);
            Assert.AreEqual(ball.Vx, First(r.NextState).Vx);
        }

        // ─── Bar reflection ───

        [Test]
        public void BarCenter_VyUp_MinVxEnforced()
        {
            const float speed = 420f;
            var ball = AB(vx: 0f, vy: speed);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var minVx = speed * MathF.Sin(15f * MathF.PI / 180f);
            Assert.Less(result.Vy, 0f);
            Assert.GreaterOrEqual(MathF.Abs(result.Vx), minVx - 0.01f);
        }

        [Test]
        public void BarLeft_VyUp_VxLeft()
        {
            var ball = AB(vx: 0f, vy: 420f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", -1f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.Less(result.Vy, 0f);
            Assert.Less(result.Vx, 0f);
        }

        [Test]
        public void BarRight_VyUp_VxRight()
        {
            var ball = AB(vx: 0f, vy: 420f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 1f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.Less(result.Vy, 0f);
            Assert.Greater(result.Vx, 0f);
        }

        [Test]
        public void BarHit_SpeedPreserved()
        {
            const float speed = 420f;
            var ball = AB(vx: 200f, vy: speed);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0.3f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var newSpeed = MathF.Sqrt(result.Vx * result.Vx + result.Vy * result.Vy);
            var origSpeed = MathF.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            Assert.That(newSpeed, Is.EqualTo(origSpeed).Within(1f));
        }

        // ─── Block collision ───

        [Test]
        public void BlockTop_VyReversed_BlockHitEvent()
        {
            var block = new BlockState("block_0", 200f, 100f, 2, false, "tough");
            var ball = AB(vy: 100f);
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.Less(First(r.NextState).Vy, 0f);
            Assert.That(r.Events, Has.Member(new BlockHitEvent("block_0", 1)));
            var b0 = r.NextState.Blocks[0];
            Assert.IsFalse(b0.IsDestroyed);
            Assert.AreEqual(1, b0.RemainingHits);
        }

        [Test]
        public void BlockDestroyed_EventScoreEmitted()
        {
            var block = new BlockState("block_0", 200f, 100f, 1, false, "basic");
            var ball = AB(vy: 100f);
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var b0 = r.NextState.Blocks[0];
            Assert.IsTrue(b0.IsDestroyed);
            Assert.IsTrue(r.Events.Any(e => e is BlockDestroyedEvent));
            Assert.AreEqual(10, r.NextState.Session.Score);
        }

        [Test]
        public void DropBlockDestroyed_ItemSpawned()
        {
            var block = new BlockState("block_0", 200f, 100f, 1, false, "basic_drop");
            var ball = AB(vy: 100f);
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block }, itemDrops: System.Array.Empty<ItemDropState>());
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsTrue(r.Events.Any(e => e is ItemSpawnedEvent));
        }

        [Test]
        public void ExistingItem_DropBlockDestroyed_NoItemSpawn()
        {
            var block = new BlockState("block_0", 200f, 100f, 1, false, "basic_drop");
            var existing = new ItemDropState("item_existing", ItemType.Expand, 300f, 300f, 160f, false);
            var ball = AB(vy: 100f);
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block }, itemDrops: new[] { existing });
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsFalse(r.Events.Any(e => e is ItemSpawnedEvent));
        }

        // ─── Floor ───

        [Test]
        public void Floor_BallInactivated_LifeLost()
        {
            var ball = AB();
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitFloorFact("ball_0") };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsFalse(First(r.NextState).IsActive);
            Assert.IsTrue(r.Events.Any(e => e is LifeLostEvent));
        }

        // ─── Item pickup ───

        [Test]
        public void ItemPickedUp_ItemRemoved_BarExpanded()
        {
            var item = new ItemDropState("item_0", ItemType.Expand, 480f, 660f, 160f, false);
            var state = MakeState(itemDrops: new[] { item });
            var facts = new CollisionFact[] { new ItemPickedUpFact("item_0") };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.AreEqual(0, r.NextState.ItemDrops.Length);
            Assert.IsTrue(r.Events.Any(e => e is ItemCollectedEvent));
            Assert.That(r.NextState.Bar.Width, Is.EqualTo(DefaultConfig.BaseBarWidth * DefaultConfig.ExpandMultiplier).Within(0.001f));
            Assert.AreEqual(BarEffect.Expand, r.NextState.Bar.ActiveEffect);
        }

        // ─── ItemFellOff ───

        [Test]
        public void ItemFellOff_ItemRemoved_NoEvent()
        {
            var item = new ItemDropState("item_0", ItemType.Expand, 300f, 950f, 160f, false);
            var state = MakeState(itemDrops: new[] { item });
            var facts = new CollisionFact[] { new ItemFellOffFloorFact("item_0") };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.AreEqual(0, r.NextState.ItemDrops.Length);
            Assert.AreEqual(0, r.Events.Count);
        }

        // ─── Bug C: 벽 반사 후 위치 스냅 ───

        private const float WallPushEps = 0.5f;
        private const float Br = 8f;  // BallRadius

        [Test]
        public void BugC_WallRight_PositionSnapped()
        {
            var ball = AB(x: 725f, y: 300f, vx: 400f, vy: -200f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Right) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.LessOrEqual(result.X, PlayfieldLayout.PlayfieldWidth - Br - WallPushEps + 0.001f);
            Assert.Less(result.Vx, 0f);
        }

        [Test]
        public void BugC_WallLeft_PositionSnapped()
        {
            var ball = AB(x: -3f, y: 300f, vx: -400f, vy: -200f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Left) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.GreaterOrEqual(result.X, Br + WallPushEps - 0.001f);
            Assert.Greater(result.Vx, 0f);
        }

        [Test]
        public void BugC_WallTop_PositionSnapped()
        {
            var ball = AB(x: 300f, y: -2f, vx: 200f, vy: -400f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.GreaterOrEqual(result.Y, Br + WallPushEps - 0.001f);
            Assert.Greater(result.Vy, 0f);
        }

        [Test]
        public void BugC_SnappedPosition_NoReDetect()
        {
            var ball = AB(x: 725f, y: 300f, vx: 400f, vy: -200f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Right) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var snapped = First(r.NextState);
            Assert.Less(snapped.X + Br, PlayfieldLayout.PlayfieldWidth);
        }

        // ─── 자석 부착 예외 ───

        [Test]
        public void MagnetBar_ActiveBall_AttachedNotReflected()
        {
            var ball = AB(x: 480f, y: 650f, vx: 0f, vy: 200f);
            var state = MakeState(
                balls: new[] { ball },
                bar: new BarState(480f, 660f, 120f, 420f, BarEffect.Magnet));
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsFalse(First(r.NextState).IsActive);
            Assert.AreEqual(0f, First(r.NextState).Vx);
            Assert.AreEqual(0f, First(r.NextState).Vy);
            CollectionAssert.Contains(r.NextState.AttachedBallIds, "ball_0");
            Assert.IsTrue(r.Events.Any(e => e is BallAttachedEvent));
            var attached = r.Events.OfType<BallAttachedEvent>().FirstOrDefault();
            Assert.IsNotNull(attached);
            CollectionAssert.Contains(attached!.BallIds, "ball_0");
        }

        [Test]
        public void MagnetBar_InactiveBall_NotAttached()
        {
            var ball = AB(x: 480f, y: 650f, vx: 0f, vy: 0f, isActive: false);
            var state = MakeState(
                balls: new[] { ball },
                bar: new BarState(480f, 660f, 120f, 420f, BarEffect.Magnet));
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsFalse(r.Events.Any(e => e is BallAttachedEvent));
            CollectionAssert.DoesNotContain(r.NextState.AttachedBallIds, "ball_0");
        }

        [Test]
        public void NoneBar_BallReflected()
        {
            var ball = AB(vx: 0f, vy: 200f);
            var state = MakeState(balls: new[] { ball });  // BarEffect.None default
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            Assert.IsTrue(First(r.NextState).IsActive);
            Assert.Less(First(r.NextState).Vy, 0f);
            Assert.IsFalse(r.Events.Any(e => e is BallAttachedEvent));
        }

        [Test]
        public void MagnetBar_AttachedY_BarTopSurface()
        {
            const float barY = 660f;
            const float barHeight = 16f;
            const float ballRadius = 8f;
            var ball = AB(x: 480f, y: barY - 20f, vx: 0f, vy: 200f);
            var state = MakeState(
                balls: new[] { ball },
                bar: new BarState(480f, barY, 120f, 420f, BarEffect.Magnet));
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var expectedY = barY - barHeight / 2f - ballRadius;
            Assert.That(First(r.NextState).Y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void MagnetBar_AttachedOffsetX_Recorded()
        {
            const float barX = 480f;
            const float ballOffset = 30f;
            var ball = AB(x: barX + ballOffset, y: 640f, vx: 0f, vy: 200f);
            var state = MakeState(
                balls: new[] { ball },
                bar: new BarState(barX, 660f, 120f, 420f, BarEffect.Magnet));
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0.5f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var attachedOffset = First(r.NextState).AttachedOffsetX;
            Assert.IsNotNull(attachedOffset);
            Assert.That(attachedOffset!.Value, Is.EqualTo(ballOffset).Within(0.001f));
        }

        // ─── Bug B: enforceMinAngle ───

        [Test]
        public void BugB_BlockTop_NearVerticalSpeed_VxMinEnforced()
        {
            const float speed = 420f;
            var ball = AB(vx: 0.5f, vy: -speed + 0.001f);
            var block = new BlockState("block_0", 460f, 200f, 2, false, "tough");
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var resultSpeed = MathF.Sqrt(result.Vx * result.Vx + result.Vy * result.Vy);
            var minVx = resultSpeed * MathF.Sin(15f * MathF.PI / 180f);
            Assert.GreaterOrEqual(MathF.Abs(result.Vx), minVx - 0.01f);
        }

        [Test]
        public void BugB_WallLeft_NearHorizontalSpeed_VyMinEnforced()
        {
            const float speed = 420f;
            var ball = AB(vx: -speed + 0.001f, vy: 0.5f);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Left) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var resultSpeed = MathF.Sqrt(result.Vx * result.Vx + result.Vy * result.Vy);
            var minVy = resultSpeed * MathF.Sin(15f * MathF.PI / 180f);
            Assert.GreaterOrEqual(MathF.Abs(result.Vy), minVy - 0.01f);
        }

        [Test]
        public void BugB_45Deg_VectorUnchanged()
        {
            const float speed = 420f;
            var vx = speed * MathF.Cos(45f * MathF.PI / 180f);
            var vy = -speed * MathF.Sin(45f * MathF.PI / 180f);
            var ball = AB(vx: vx, vy: vy);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitWallFact("ball_0", WallSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            Assert.That(result.Vx, Is.EqualTo(vx).Within(1f));
            Assert.That(result.Vy, Is.EqualTo(-vy).Within(1f));
        }

        [Test]
        public void BugB_BarCenterContact_VxMinEnforced()
        {
            const float speed = 420f;
            var ball = AB(vx: 0f, vy: speed);
            var state = MakeState(balls: new[] { ball });
            var facts = new CollisionFact[] { new BallHitBarFact("ball_0", 0f) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var resultSpeed = MathF.Sqrt(result.Vx * result.Vx + result.Vy * result.Vy);
            var minVx = resultSpeed * MathF.Sin(15f * MathF.PI / 180f);
            Assert.GreaterOrEqual(MathF.Abs(result.Vx), minVx - 0.01f);
        }

        [Test]
        public void BugB_SpeedPreserved()
        {
            const float speed = 420f;
            var ball = AB(vx: 0.5f, vy: -speed + 0.001f);
            var block = new BlockState("block_0", 460f, 200f, 2, false, "tough");
            var state = MakeState(balls: new[] { ball }, blocks: new[] { block });
            var facts = new CollisionFact[] { new BallHitBlockFact("ball_0", "block_0", BlockSide.Top) };
            var r = CollisionResolutionService.ApplyCollisions(state, facts, Tables);
            var result = First(r.NextState);
            var resultSpeed = MathF.Sqrt(result.Vx * result.Vx + result.Vy * result.Vy);
            Assert.That(resultSpeed, Is.EqualTo(speed).Within(1f));
        }
    }
}
