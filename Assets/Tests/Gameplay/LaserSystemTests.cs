using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class LaserSystemTests
    {
        private static BarState MakeBar(float x = 360f, float y = 660f, float width = 120f, BarEffect effect = BarEffect.Laser) =>
            new(x, y, width, 420f, effect);

        private static BlockState MakeBlock(string id = "block_0", float x = 320f, float y = 100f, int hits = 1, bool destroyed = false, string defId = "basic") =>
            new(id, x, y, hits, destroyed, defId);

        private static readonly IReadOnlyDictionary<string, BlockDefinition> BlockDefs = new Dictionary<string, BlockDefinition>
        {
            ["basic"] = new("basic", 1, 10, null, 0xCCCCCC),
            ["tough"] = new("tough", 2, 30, null, 0xCCCCCC),
        };

        private static (LaserSystem System, int[] Counter) MakeSystem()
        {
            var counter = new int[] { 0 };
            var system = new LaserSystem(() => $"laser_{counter[0]++}");
            return (system, counter);
        }

        // ─── FireLaser ───

        [Test]
        public void FireLaser_2Shots()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), 400f);
            Assert.AreEqual(2, result.NewShots.Count);
        }

        [Test]
        public void FireLaser_AddsToExisting()
        {
            var (system, _) = MakeSystem();
            var existing = new[] { new LaserShotState("laser_old", 200f, 300f, -1200f) };
            var result = system.FireLaser(MakeBar(), existing, 400f);
            Assert.AreEqual(3, result.NewShots.Count);
        }

        [Test]
        public void FireLaser_Shot1X_LeftOffset()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(x: 360f, width: 120f), Array.Empty<LaserShotState>(), 400f);
            Assert.That(result.NewShots[0].X, Is.EqualTo(360f - 120f / 3f).Within(0.001f));
        }

        [Test]
        public void FireLaser_Shot2X_RightOffset()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(x: 360f, width: 120f), Array.Empty<LaserShotState>(), 400f);
            Assert.That(result.NewShots[1].X, Is.EqualTo(360f + 120f / 3f).Within(0.001f));
        }

        [Test]
        public void FireLaser_SpawnY_BarMinusHalfHeight()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(y: 660f), Array.Empty<LaserShotState>(), 400f);
            Assert.That(result.NewShots[0].Y, Is.EqualTo(660f - 8f).Within(0.001f));
            Assert.That(result.NewShots[1].Y, Is.EqualTo(660f - 8f).Within(0.001f));
        }

        [Test]
        public void FireLaser_ShotVy_Minus1200()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), 400f);
            Assert.AreEqual(-1200f, result.NewShots[0].Vy);
            Assert.AreEqual(-1200f, result.NewShots[1].Vy);
        }

        [Test]
        public void FireLaser_NextCooldownMs_FromArg()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), 400f);
            Assert.AreEqual(400f, result.NextCooldownMs);
        }

        [Test]
        public void FireLaser_NullCooldown_DefaultsTo400()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), null);
            Assert.AreEqual(400f, result.NextCooldownMs);
        }

        [Test]
        public void FireLaser_LaserFiredEvent()
        {
            var (system, _) = MakeSystem();
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), 400f);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0] as LaserFiredEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual(2, evt!.ShotCount);
        }

        [Test]
        public void FireLaser_IdsDeterministic()
        {
            var ids = new List<string>();
            var system = new LaserSystem(() =>
            {
                var id = $"test_{ids.Count}";
                ids.Add(id);
                return id;
            });
            var result = system.FireLaser(MakeBar(), Array.Empty<LaserShotState>(), 400f);
            Assert.AreEqual("test_0", result.NewShots[0].Id);
            Assert.AreEqual("test_1", result.NewShots[1].Id);
        }

        // ─── Tick ───

        [Test]
        public void Tick_ShotMovesByVyDt()
        {
            var (system, _) = MakeSystem();
            var shots = new[] { new LaserShotState("laser_0", 200f, 400f, -1200f) };
            var result = system.Tick(shots, 0f, 1f / 60f);
            Assert.That(result.NextShots[0].Y, Is.EqualTo(400f + -1200f * (1f / 60f)).Within(0.001f));
        }

        [Test]
        public void Tick_CeilingExceeded_ShotRemoved()
        {
            var (system, _) = MakeSystem();
            var shots = new[]
            {
                new LaserShotState("laser_0", 200f, 5f, -1200f),    // 한 틱 후 y < 0
                new LaserShotState("laser_1", 300f, 400f, -1200f),
            };
            var result = system.Tick(shots, 0f, 1f / 60f);
            Assert.AreEqual(1, result.NextShots.Count);
            Assert.AreEqual("laser_1", result.NextShots[0].Id);
        }

        [Test]
        public void Tick_CeilingEdge_ShotKept()
        {
            var (system, _) = MakeSystem();
            // y=5, LaserHalfH=4 → top edge = 1 >= 0 → 유지
            var shots = new[] { new LaserShotState("laser_0", 200f, 5f, 0f) };
            var result = system.Tick(shots, 0f, 0f);
            Assert.AreEqual(1, result.NextShots.Count);
        }

        [Test]
        public void Tick_CooldownDecreasesByDtMs()
        {
            var (system, _) = MakeSystem();
            var result = system.Tick(Array.Empty<LaserShotState>(), 400f, 1f / 60f);
            Assert.That(result.NextCooldownMs, Is.EqualTo(400f - 1000f / 60f).Within(0.001f));
        }

        [Test]
        public void Tick_CooldownNonNegative()
        {
            var (system, _) = MakeSystem();
            var result = system.Tick(Array.Empty<LaserShotState>(), 10f, 1f);  // 1000ms 감소 시도
            Assert.AreEqual(0f, result.NextCooldownMs);
        }

        [Test]
        public void Tick_EmptyShots_EmptyResult()
        {
            var (system, _) = MakeSystem();
            var result = system.Tick(Array.Empty<LaserShotState>(), 0f, 1f / 60f);
            Assert.AreEqual(0, result.NextShots.Count);
        }

        // ─── HandleBlockCollisions ───

        [Test]
        public void HandleBlockCollisions_NoOverlap_NoChange()
        {
            var (system, _) = MakeSystem();
            var shots = new[] { new LaserShotState("laser_0", 0f, 500f, -1200f) };
            var block = MakeBlock(x: 320f, y: 100f);
            var result = system.HandleBlockCollisions(shots, new[] { block }, BlockDefs);
            Assert.AreEqual(1, result.NextShots.Count);
            Assert.AreEqual(1, result.NextBlocks[0].RemainingHits);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void HandleBlockCollisions_ShotOverlapsBlock_ShotRemoved()
        {
            var (system, _) = MakeSystem();
            var block = MakeBlock(x: 320f, y: 100f, hits: 1);
            var shots = new[] { new LaserShotState("laser_0", 352f, 112f, -1200f) };
            var result = system.HandleBlockCollisions(shots, new[] { block }, BlockDefs);
            Assert.AreEqual(0, result.NextShots.Count);
        }

        [Test]
        public void HandleBlockCollisions_BlockDestroyed_EventEmitted()
        {
            var (system, _) = MakeSystem();
            var block = MakeBlock(x: 320f, y: 100f, hits: 1);
            var shots = new[] { new LaserShotState("laser_0", 352f, 112f, -1200f) };
            var result = system.HandleBlockCollisions(shots, new[] { block }, BlockDefs);
            Assert.IsTrue(result.NextBlocks[0].IsDestroyed);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0] as BlockDestroyedEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual("block_0", evt!.BlockId);
            Assert.AreEqual(10, evt.ScoreDelta);
            CollectionAssert.Contains(result.DestroyedBlockIds, "block_0");
        }

        [Test]
        public void HandleBlockCollisions_2HitBlock_BecomesDamaged()
        {
            var (system, _) = MakeSystem();
            var block = MakeBlock(x: 320f, y: 100f, hits: 2, defId: "tough");
            var shots = new[] { new LaserShotState("laser_0", 352f, 112f, -1200f) };
            var result = system.HandleBlockCollisions(shots, new[] { block }, BlockDefs);
            Assert.AreEqual(1, result.NextBlocks[0].RemainingHits);
            Assert.IsFalse(result.NextBlocks[0].IsDestroyed);
            var evt = result.Events[0] as BlockHitEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual("block_0", evt!.BlockId);
            Assert.AreEqual(1, evt.RemainingHits);
        }

        [Test]
        public void HandleBlockCollisions_DestroyedBlock_Skipped()
        {
            var (system, _) = MakeSystem();
            var block = MakeBlock(x: 320f, y: 100f, destroyed: true);
            var shots = new[] { new LaserShotState("laser_0", 352f, 112f, -1200f) };
            var result = system.HandleBlockCollisions(shots, new[] { block }, BlockDefs);
            Assert.AreEqual(1, result.NextShots.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void HandleBlockCollisions_FirstHitOnly_NoPenetration()
        {
            var (system, _) = MakeSystem();
            var block1 = MakeBlock(id: "b1", x: 320f, y: 100f, hits: 1);
            var block2 = MakeBlock(id: "b2", x: 320f, y: 124f, hits: 1);
            var shots = new[] { new LaserShotState("laser_0", 352f, 112f, -1200f) };
            var result = system.HandleBlockCollisions(shots, new[] { block1, block2 }, BlockDefs);
            Assert.AreEqual(0, result.NextShots.Count);
            var b1 = result.NextBlocks.FirstOrDefault(b => b.Id == "b1");
            Assert.IsTrue(b1.IsDestroyed);
        }

        [Test]
        public void HandleBlockCollisions_MultipleShots_HitDifferentBlocks()
        {
            var (system, _) = MakeSystem();
            var block1 = MakeBlock(id: "b1", x: 320f, y: 100f, hits: 1);
            var block2 = MakeBlock(id: "b2", x: 0f, y: 200f, hits: 1);
            var shots = new[]
            {
                new LaserShotState("laser_0", 352f, 112f, -1200f),
                new LaserShotState("laser_1", 32f, 212f, -1200f),
            };
            var result = system.HandleBlockCollisions(shots, new[] { block1, block2 }, BlockDefs);
            Assert.AreEqual(0, result.NextShots.Count);
            Assert.AreEqual(2, result.NextBlocks.Count(b => b.IsDestroyed));
            Assert.AreEqual(2, result.Events.Count(e => e is BlockDestroyedEvent));
        }

        [Test]
        public void HandleBlockCollisions_ScoreDelta_SumDestroyed()
        {
            var (system, _) = MakeSystem();
            var block1 = MakeBlock(id: "b1", x: 320f, y: 100f, hits: 1, defId: "basic");
            var block2 = MakeBlock(id: "b2", x: 0f, y: 200f, hits: 1, defId: "basic");
            var shots = new[]
            {
                new LaserShotState("laser_0", 352f, 112f, -1200f),
                new LaserShotState("laser_1", 32f, 212f, -1200f),
            };
            var result = system.HandleBlockCollisions(shots, new[] { block1, block2 }, BlockDefs);
            Assert.AreEqual(20, result.ScoreDelta);
        }
    }
}
