using System;
using System.Collections.Generic;
using NUnit.Framework;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class SpinnerSystemTests
    {
        // ─── Fixtures ───

        private static readonly SpinnerDefinition CubeDef = new(
            DefinitionId: "spinner_cube",
            Kind: SpinnerKind.Cube,
            Size: 48f,
            RotationSpeedRadPerSec: 1.5f,
            BlockCollisionPhases: new[] { 0f, MathF.PI / 2f });

        private static readonly SpinnerDefinition TriangleDef = new(
            DefinitionId: "spinner_triangle",
            Kind: SpinnerKind.Triangle,
            Size: 48f,
            RotationSpeedRadPerSec: 1.2f,
            BlockCollisionPhases: new[] { 0f });

        private static readonly IReadOnlyDictionary<string, SpinnerDefinition> Defs = new Dictionary<string, SpinnerDefinition>
        {
            ["spinner_cube"] = CubeDef,
            ["spinner_triangle"] = TriangleDef,
        };

        private static readonly BlockDefinition BlockDef = new("block_normal", 1, 100, null, 0xCCCCCC);
        private static readonly IReadOnlyDictionary<string, BlockDefinition> BlockDefs = new Dictionary<string, BlockDefinition>
        {
            ["block_normal"] = BlockDef,
        };

        private static SpinnerRuntimeState Circling(
            string id, string defId,
            float x = 360f, float y = 300f,
            float angleRad = 0f,
            float? circleCenterX = null, float? circleCenterY = null,
            float circleRadius = 60f,  // PlayfieldLayout.CircleRadius
            float circleAngleRad = -MathF.PI / 2f)
        {
            var ccx = circleCenterX ?? x;
            var ccy = circleCenterY ?? (y + circleRadius);
            return new SpinnerRuntimeState(
                Id: id, DefinitionId: defId,
                X: x, Y: y, AngleRad: angleRad,
                Phase: SpinnerPhase.Circling,
                SpawnElapsedMs: SpinnerSystem.SpawnDurationMs,
                DescentEndY: 300f,
                CircleCenterX: ccx, CircleCenterY: ccy,
                CircleRadius: circleRadius,
                CircleAngleRad: circleAngleRad,
                SpawnX: x);
        }

        private static SpinnerRuntimeState Spawning(
            string id, string defId,
            float x = 360f, float descentEndY = 400f,
            float angleRad = 0f, float spawnElapsedMs = 0f)
        {
            return new SpinnerRuntimeState(
                Id: id, DefinitionId: defId,
                X: x, Y: 0f, AngleRad: angleRad,
                Phase: SpinnerPhase.Spawning,
                SpawnElapsedMs: spawnElapsedMs,
                DescentEndY: descentEndY,
                CircleCenterX: x, CircleCenterY: descentEndY + PlayfieldLayout.CircleRadius,
                CircleRadius: PlayfieldLayout.CircleRadius,
                CircleAngleRad: 0f,
                SpawnX: x);
        }

        private static SpinnerRuntimeState Descending(
            string id, string defId,
            float x = 360f, float y = 0f,
            float descentEndY = 400f, float angleRad = 0f)
        {
            return new SpinnerRuntimeState(
                Id: id, DefinitionId: defId,
                X: x, Y: y, AngleRad: angleRad,
                Phase: SpinnerPhase.Descending,
                SpawnElapsedMs: SpinnerSystem.SpawnDurationMs,
                DescentEndY: descentEndY,
                CircleCenterX: x, CircleCenterY: descentEndY + PlayfieldLayout.CircleRadius,
                CircleRadius: PlayfieldLayout.CircleRadius,
                CircleAngleRad: 0f,
                SpawnX: x);
        }

        private static BallState Ball(float x = 480f, float y = 400f, float vx = 0f, float vy = -300f, bool isActive = true) =>
            new("ball_0", x, y, vx, vy, isActive);

        private static BlockState Block(string id = "block_0", float x = 360f, float y = 280f, int hits = 1, bool destroyed = false) =>
            new(id, x, y, hits, destroyed, "block_normal");

        // ─── NormalizeAngle ───

        [Test]
        public void Normalize_Zero() =>
            Assert.That(SpinnerSystem.NormalizeAngle(0f), Is.EqualTo(0f).Within(0.001f));

        [Test]
        public void Normalize_TwoPi_Zero() =>
            Assert.That(SpinnerSystem.NormalizeAngle(2f * MathF.PI), Is.EqualTo(0f).Within(0.001f));

        [Test]
        public void Normalize_NegativePi_Pi() =>
            Assert.That(SpinnerSystem.NormalizeAngle(-MathF.PI), Is.EqualTo(MathF.PI).Within(0.001f));

        [Test]
        public void Normalize_ThreePi_Pi() =>
            Assert.That(SpinnerSystem.NormalizeAngle(3f * MathF.PI), Is.EqualTo(MathF.PI).Within(0.001f));

        // ─── Tick — 자체 회전 ───

        [Test]
        public void Tick_Circling_AngleRadIncreases()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", angleRad: 0f);
            var r = sys.Tick(new[] { s }, 1f);
            Assert.That(r[0].AngleRad, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void Tick_Spawning_AngleRadIncreases()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", angleRad: 0f);
            var r = sys.Tick(new[] { s }, 0.1f);
            Assert.That(r[0].AngleRad, Is.EqualTo(1.5f * 0.1f).Within(0.001f));
        }

        [Test]
        public void Tick_Descending_AngleRadIncreases()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", angleRad: 0f);
            var r = sys.Tick(new[] { s }, 0.1f);
            Assert.That(r[0].AngleRad, Is.EqualTo(1.5f * 0.1f).Within(0.001f));
        }

        [Test]
        public void Tick_AngleNormalized()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", angleRad: 2f * MathF.PI - 0.1f);
            var r = sys.Tick(new[] { s }, 1f);
            var expected = SpinnerSystem.NormalizeAngle(2f * MathF.PI - 0.1f + 1.5f);
            Assert.That(r[0].AngleRad, Is.EqualTo(expected).Within(0.001f));
            Assert.Less(r[0].AngleRad, 2f * MathF.PI);
            Assert.GreaterOrEqual(r[0].AngleRad, 0f);
        }

        [Test]
        public void Tick_UnknownDef_NoChange()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "unknown", angleRad: 1.0f);
            var r = sys.Tick(new[] { s }, 1f);
            Assert.That(r[0].AngleRad, Is.EqualTo(1.0f).Within(0.001f));
        }

        // ─── Spawning phase ───

        [Test]
        public void Spawning_ElapsedIncreases()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube");
            var r = sys.Tick(new[] { s }, 0.1f);
            Assert.That(r[0].SpawnElapsedMs, Is.EqualTo(100f).Within(0.001f));
            Assert.AreEqual(SpinnerPhase.Spawning, r[0].Phase);
        }

        [Test]
        public void Spawning_StaysIfBeforeDuration()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", spawnElapsedMs: 300f);
            var r = sys.Tick(new[] { s }, 0.05f);
            Assert.AreEqual(SpinnerPhase.Spawning, r[0].Phase);
            Assert.That(r[0].SpawnElapsedMs, Is.EqualTo(350f).Within(0.001f));
        }

        [Test]
        public void Spawning_TransitionsToDescending()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube");
            var dt = SpinnerSystem.SpawnDurationMs / 1000f;
            var r = sys.Tick(new[] { s }, dt);
            Assert.AreEqual(SpinnerPhase.Descending, r[0].Phase);
            Assert.AreEqual(SpinnerSystem.SpawnDurationMs, r[0].SpawnElapsedMs);
        }

        [Test]
        public void Spawning_YStaysZero()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube");
            var r = sys.Tick(new[] { s }, 0.2f);
            Assert.AreEqual(0f, r[0].Y);
        }

        [Test]
        public void Spawning_XStaysSpawnX()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", x: 360f);
            var r = sys.Tick(new[] { s }, 0.2f);
            Assert.AreEqual(360f, r[0].X);
        }

        [Test]
        public void Spawning_LargeDt_CapsAtDuration()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", spawnElapsedMs: 350f);
            var r = sys.Tick(new[] { s }, 1.0f);
            Assert.AreEqual(SpinnerPhase.Descending, r[0].Phase);
            Assert.AreEqual(SpinnerSystem.SpawnDurationMs, r[0].SpawnElapsedMs);
        }

        // ─── Descending phase ───

        [Test]
        public void Descending_YIncreasesByDtSpeed()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", y: 0f);
            var dt = 1f / 60f;
            var r = sys.Tick(new[] { s }, dt);
            Assert.That(r[0].Y, Is.EqualTo(SpinnerSystem.DescentSpeedPxPerSec * dt).Within(0.001f));
            Assert.AreEqual(SpinnerPhase.Descending, r[0].Phase);
        }

        [Test]
        public void Descending_YDtHalfSecond()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", y: 100f);
            var r = sys.Tick(new[] { s }, 0.5f);
            Assert.That(r[0].Y, Is.EqualTo(100f + 80f * 0.5f).Within(0.001f));
        }

        [Test]
        public void Descending_TransitionsToCirclingAtOrbitTop()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", y: 395f, descentEndY: 400f);
            var r = sys.Tick(new[] { s }, 0.1f);
            // orbitTopY = circleCenterY - circleRadius = (400+60) - 60 = 400.
            // descentEndY 도 400 → orbitTopY 도 400. 395 + 80*0.1 = 403 >= 400.
            Assert.AreEqual(SpinnerPhase.Circling, r[0].Phase);
            Assert.AreEqual(400f, r[0].Y);
        }

        [Test]
        public void Descending_CirclingAngleAtTop()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", y: 399f, descentEndY: 400f);
            var r = sys.Tick(new[] { s }, 1.0f);
            Assert.AreEqual(SpinnerPhase.Circling, r[0].Phase);
            Assert.That(r[0].CircleAngleRad, Is.EqualTo(-MathF.PI / 2f).Within(0.001f));
        }

        [Test]
        public void Descending_XStaysSpawnX()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", x: 360f, y: 100f);
            var r = sys.Tick(new[] { s }, 0.5f);
            Assert.AreEqual(360f, r[0].X);
        }

        [Test]
        public void Descending_StaysIfNotReached()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", y: 0f, descentEndY: 400f);
            var r = sys.Tick(new[] { s }, 0.1f);
            Assert.AreEqual(SpinnerPhase.Descending, r[0].Phase);
        }

        // ─── Circling phase ───

        [Test]
        public void Circling_CircleAngleIncreases()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", circleAngleRad: 0f);
            var dt = 1f / 60f;
            var r = sys.Tick(new[] { s }, dt);
            Assert.That(r[0].CircleAngleRad, Is.EqualTo(SpinnerSystem.CircleSpeedRadPerSec * dt).Within(0.001f));
        }

        [Test]
        public void Circling_XYFromCosSin()
        {
            var sys = new SpinnerSystem(Defs);
            float cx = 360f, cy = 550f, radius = PlayfieldLayout.CircleRadius;
            float initial = 0f;
            var s = Circling("s0", "spinner_cube",
                circleCenterX: cx, circleCenterY: cy,
                circleRadius: radius, circleAngleRad: initial,
                x: cx + radius * MathF.Cos(initial),
                y: cy + radius * MathF.Sin(initial));
            var dt = 1f / 60f;
            var r = sys.Tick(new[] { s }, dt);
            var expectedAngle = initial + SpinnerSystem.CircleSpeedRadPerSec * dt;
            Assert.That(r[0].X, Is.EqualTo(cx + radius * MathF.Cos(expectedAngle)).Within(0.001f));
            Assert.That(r[0].Y, Is.EqualTo(cy + radius * MathF.Sin(expectedAngle)).Within(0.001f));
        }

        [Test]
        public void Circling_InitialAtTop()
        {
            float cx = 360f, cy = 550f, radius = PlayfieldLayout.CircleRadius;
            var s = Circling("s0", "spinner_cube",
                circleCenterX: cx, circleCenterY: cy,
                circleRadius: radius, circleAngleRad: -MathF.PI / 2f,
                x: cx + radius * MathF.Cos(-MathF.PI / 2f),
                y: cy + radius * MathF.Sin(-MathF.PI / 2f));
            Assert.That(s.X, Is.EqualTo(cx).Within(0.001f));
            Assert.That(s.Y, Is.EqualTo(cy - radius).Within(0.001f));
        }

        [Test]
        public void Circling_MultipleIndependent()
        {
            var sys = new SpinnerSystem(Defs);
            var s0 = Circling("s0", "spinner_cube", circleAngleRad: 0f);
            var s1 = Circling("s1", "spinner_triangle", circleAngleRad: MathF.PI);
            var r = sys.Tick(new[] { s0, s1 }, 1f);
            Assert.That(r[0].CircleAngleRad, Is.EqualTo(SpinnerSystem.CircleSpeedRadPerSec * 1f).Within(0.001f));
            Assert.That(r[1].CircleAngleRad, Is.EqualTo(MathF.PI + SpinnerSystem.CircleSpeedRadPerSec * 1f).Within(0.001f));
        }

        [Test]
        public void Circling_SelfRotationContinues()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", angleRad: 0f);
            var r = sys.Tick(new[] { s }, 1f);
            Assert.That(r[0].AngleRad, Is.EqualTo(1.5f).Within(0.001f));
        }

        // ─── 전체 phase 전환 흐름 ───

        [Test]
        public void FullFlow_SpawningToDescendingToCircling()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", descentEndY: 400f);

            s = sys.Tick(new[] { s }, SpinnerSystem.SpawnDurationMs / 1000f)[0];
            Assert.AreEqual(SpinnerPhase.Descending, s.Phase);

            s = sys.Tick(new[] { s }, 400f / SpinnerSystem.DescentSpeedPxPerSec)[0];
            Assert.AreEqual(SpinnerPhase.Circling, s.Phase);
            Assert.AreEqual(400f, s.Y);
            Assert.That(s.CircleAngleRad, Is.EqualTo(-MathF.PI / 2f).Within(0.001f));
        }

        // ─── BallCollisions — ghost ───

        [Test]
        public void BallCollisions_Spawning_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", x: 360f);
            var ball = Ball(x: 360f, y: 10f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsFalse(r.Collided);
            Assert.AreEqual(ball, r.NextBall);
        }

        [Test]
        public void BallCollisions_Descending_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", x: 360f, y: 200f);
            var ball = Ball(x: 360f, y: 210f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsFalse(r.Collided);
        }

        [Test]
        public void BallCollisions_AllSpawning_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var spinners = new[]
            {
                Spawning("s0", "spinner_cube", x: 360f),
                Spawning("s1", "spinner_triangle", x: 400f),
            };
            var ball = Ball(x: 380f, y: 10f, vx: 100f, vy: 100f);
            var r = sys.HandleBallCollisions(ball, spinners);
            Assert.IsFalse(r.Collided);
        }

        // ─── BallCollisions — Inactive ball ───

        [Test]
        public void BallCollisions_InactiveBall_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 300f, isActive: false);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsFalse(r.Collided);
            Assert.AreEqual(360f, r.NextBall.X);
        }

        // ─── BallCollisions — Circling (solid) ───

        [Test]
        public void BallCollisions_Circling_Overlap_Collided()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 280f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsTrue(r.Collided);
        }

        [Test]
        public void BallCollisions_VyReversedTowardsSpinner()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 280f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsTrue(r.Collided);
            Assert.Less(r.NextBall.Vy, 0f);
        }

        [Test]
        public void BallCollisions_HorizontalApproach_VxReversed()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 336f, y: 300f, vx: 200f, vy: 0f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsTrue(r.Collided);
            Assert.Less(r.NextBall.Vx, 0f);
        }

        [Test]
        public void BallCollisions_FarAway_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 200f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsFalse(r.Collided);
        }

        [Test]
        public void BallCollisions_MovingAway_NoSpeedReverse()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 280f, vx: 0f, vy: -200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsTrue(r.Collided);
            Assert.AreEqual(-200f, r.NextBall.Vy);
        }

        [Test]
        public void BallCollisions_AfterReflect_BallSeparated()
        {
            var sys = new SpinnerSystem(Defs);
            float sx = 360f, sy = 300f;
            var s = Circling("s0", "spinner_cube", x: sx, y: sy);
            var ball = Ball(x: sx, y: sy - 20f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            var dist = MathF.Sqrt(MathF.Pow(r.NextBall.X - sx, 2f) + MathF.Pow(r.NextBall.Y - sy, 2f));
            Assert.GreaterOrEqual(dist, 32f - 0.001f);  // size/2 + BallRadius = 24 + 8
        }

        [Test]
        public void BallCollisions_TwoSpinners_ClosestReflected()
        {
            var sys = new SpinnerSystem(Defs);
            var sA = Circling("s0", "spinner_cube", x: 360f, y: 300f);
            var sB = Circling("s1", "spinner_cube", x: 400f, y: 300f);
            var ball = Ball(x: 380f, y: 300f, vx: 100f, vy: 0f);
            var r = sys.HandleBallCollisions(ball, new[] { sA, sB });
            Assert.IsTrue(r.Collided);
        }

        [Test]
        public void BallCollisions_EmptySpinners_NoCollision()
        {
            var sys = new SpinnerSystem(Defs);
            var ball = Ball(x: 360f, y: 300f);
            var r = sys.HandleBallCollisions(ball, Array.Empty<SpinnerRuntimeState>());
            Assert.IsFalse(r.Collided);
            Assert.AreEqual(ball, r.NextBall);
        }

        // ─── BallCollisions — Triangle ───

        [Test]
        public void Triangle_Overlap_Collided()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_triangle", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 254f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.IsTrue(r.Collided);
        }

        [Test]
        public void Triangle_TopVertex_VyReversed()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_triangle", x: 360f, y: 300f);
            var ball = Ball(x: 360f, y: 254f, vx: 0f, vy: 200f);
            var r = sys.HandleBallCollisions(ball, new[] { s });
            Assert.Less(r.NextBall.Vy, 0f);
        }

        [Test]
        public void Triangle_SweptFast_Detected()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_triangle", x: 360f, y: 300f);
            var prev = Ball(x: 390f, y: 240f, vx: 0f, vy: 6600f);
            var curr = Ball(x: 390f, y: 350f, vx: 0f, vy: 6600f);
            var r = sys.HandleBallCollisions(curr, new[] { s }, prev);
            Assert.IsTrue(r.Collided);
            Assert.Greater(r.NextBall.Vx, 0f);
        }

        [Test]
        public void Triangle_NoSweptPrev_OldLogicMissesFast()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_triangle", x: 360f, y: 300f);
            var curr = Ball(x: 390f, y: 350f, vx: 0f, vy: 6600f);
            var r = sys.HandleBallCollisions(curr, new[] { s });  // prev 없음
            Assert.IsFalse(r.Collided);
        }

        // ─── BlockCollisions — ghost ───

        [Test]
        public void BlockCollisions_Spawning_NoHit()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", x: 360f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 0f, hits: 2);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(2, r.NextBlocks[0].RemainingHits);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_Descending_NoHit()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Descending("s0", "spinner_cube", x: 360f, y: 288f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 2);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(2, r.NextBlocks[0].RemainingHits);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_AllSpawning_NoChange()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Spawning("s0", "spinner_cube", x: 360f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 0f, hits: 3);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(0, r.Events.Count);
            Assert.AreEqual(0, r.ScoreDelta);
            Assert.AreEqual(3, r.NextBlocks[0].RemainingHits);
        }

        // ─── BlockCollisions — Circling (solid) ───

        [Test]
        public void BlockCollisions_Circling_Angle0_HitsBlock()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 2);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(1, r.NextBlocks[0].RemainingHits);
            Assert.AreEqual(1, r.Events.Count);
            Assert.IsInstanceOf<BlockHitEvent>(r.Events[0]);
        }

        [Test]
        public void BlockCollisions_Circling_DestroyedEvent()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 1);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.IsTrue(r.NextBlocks[0].IsDestroyed);
            Assert.IsInstanceOf<BlockDestroyedEvent>(r.Events[0]);
            Assert.AreEqual(100, r.ScoreDelta);
        }

        [Test]
        public void BlockCollisions_PhaseOutsideTolerance_NoHit()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: MathF.PI / 4f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 2);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(2, r.NextBlocks[0].RemainingHits);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_PhasePi2_Hits()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: MathF.PI / 2f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 1);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.IsInstanceOf<BlockDestroyedEvent>(r.Events[0]);
        }

        [Test]
        public void BlockCollisions_DestroyedBlock_Skipped()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 0, destroyed: true);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_OutOfRange_NoHit()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 600f, y: 500f, hits: 1);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_MultipleSpinners_IndependentHits()
        {
            var sys = new SpinnerSystem(Defs);
            var s0 = Circling("s0", "spinner_cube", x: 360f, y: 300f, angleRad: 0f);
            var s1 = Circling("s1", "spinner_cube", x: 368f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 3);
            var r = sys.HandleBlockCollisions(new[] { s0, s1 }, new[] { b }, BlockDefs);
            Assert.AreEqual(1, r.NextBlocks[0].RemainingHits);
            Assert.AreEqual(2, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_EmptySpinners_NoEvents()
        {
            var sys = new SpinnerSystem(Defs);
            var b = Block(id: "b0", x: 336f, y: 288f);
            var r = sys.HandleBlockCollisions(Array.Empty<SpinnerRuntimeState>(), new[] { b }, BlockDefs);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_EmptyBlocks_NoEvents()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_cube", angleRad: 0f);
            var r = sys.HandleBlockCollisions(new[] { s }, Array.Empty<BlockState>(), BlockDefs);
            Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void BlockCollisions_TrianglePhase0_Hits()
        {
            var sys = new SpinnerSystem(Defs);
            var s = Circling("s0", "spinner_triangle", x: 360f, y: 300f, angleRad: 0f);
            var b = Block(id: "b0", x: 336f, y: 288f, hits: 1);
            var r = sys.HandleBlockCollisions(new[] { s }, new[] { b }, BlockDefs);
            Assert.IsInstanceOf<BlockDestroyedEvent>(r.Events[0]);
        }
    }
}
