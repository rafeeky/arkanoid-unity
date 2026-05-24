using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class BarEffectServiceTests
    {
        private const float BaseBarWidth = 120f;

        private static readonly IReadOnlyDictionary<ItemType, ItemDefinition> ItemDefs = new Dictionary<ItemType, ItemDefinition>
        {
            [ItemType.Expand] = new(ItemType.Expand, "txt_expand", "txt_expand_desc", "icon_expand", 160f, ItemType.Expand, ExpandMultiplier: 1.5f),
            [ItemType.Magnet] = new(ItemType.Magnet, "txt_magnet", "txt_magnet_desc", "icon_magnet", 160f, ItemType.Magnet, MagnetDurationMs: 8000f),
            [ItemType.Laser] = new(ItemType.Laser, "txt_laser", "txt_laser_desc", "icon_laser", 160f, ItemType.Laser, LaserCooldownMs: 500f, LaserShotCount: 2),
        };

        private static BarState MakeBar(BarEffect activeEffect = BarEffect.None, float width = BaseBarWidth) =>
            new(360f, 660f, width, 420f, activeEffect);

        // ─── applyEffect 전환 매트릭스 ───

        [Test]
        public void NoneToExpand_WidthX15()
        {
            var svc = new BarEffectService(ItemDefs);
            var result = svc.ApplyEffect(MakeBar(), 0f, 0f, System.Array.Empty<string>(), ItemType.Expand, BaseBarWidth);
            Assert.AreEqual(BarEffect.Expand, result.NextBar.ActiveEffect);
            Assert.That(result.NextBar.Width, Is.EqualTo(BaseBarWidth * 1.5f).Within(0.001f));
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void NoneToMagnet_Remaining8000_WidthReset()
        {
            var svc = new BarEffectService(ItemDefs);
            var result = svc.ApplyEffect(MakeBar(width: 180f), 0f, 0f, System.Array.Empty<string>(), ItemType.Magnet, BaseBarWidth);
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.AreEqual(BaseBarWidth, result.NextBar.Width);
            Assert.AreEqual(8000f, result.NextMagnetRemaining);
            Assert.AreEqual(0, result.NextAttachedBalls.Count);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void NoneToLaser_CooldownZero_WidthReset()
        {
            var svc = new BarEffectService(ItemDefs);
            var result = svc.ApplyEffect(MakeBar(width: 180f), 0f, 500f, System.Array.Empty<string>(), ItemType.Laser, BaseBarWidth);
            Assert.AreEqual(BarEffect.Laser, result.NextBar.ActiveEffect);
            Assert.AreEqual(BaseBarWidth, result.NextBar.Width);
            Assert.AreEqual(0f, result.NextLaserCooldown);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
        }

        [Test]
        public void ExpandToMagnet_NoCleanup()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Expand, BaseBarWidth * 1.5f);
            var result = svc.ApplyEffect(bar, 0f, 0f, System.Array.Empty<string>(), ItemType.Magnet, BaseBarWidth);
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.AreEqual(BaseBarWidth, result.NextBar.Width);
            Assert.AreEqual(8000f, result.NextMagnetRemaining);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void MagnetToExpand_NoAttached_NoBallsReleasedEvent()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.ApplyEffect(bar, 3000f, 0f, System.Array.Empty<string>(), ItemType.Expand, BaseBarWidth);
            Assert.AreEqual(BarEffect.Expand, result.NextBar.ActiveEffect);
            Assert.AreEqual(0f, result.NextMagnetRemaining);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
        }

        [Test]
        public void ExpandToLaser_WidthReset_NoClearShots()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Expand, BaseBarWidth * 1.5f);
            var result = svc.ApplyEffect(bar, 0f, 0f, System.Array.Empty<string>(), ItemType.Laser, BaseBarWidth);
            Assert.AreEqual(BarEffect.Laser, result.NextBar.ActiveEffect);
            Assert.AreEqual(BaseBarWidth, result.NextBar.Width);
            Assert.AreEqual(0f, result.NextLaserCooldown);
            Assert.IsFalse(result.ClearLaserShots);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void MagnetWithAttached_ToExpand_Released_ReplacedReason()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0" };
            var result = svc.ApplyEffect(bar, 5000f, 0f, attached, ItemType.Expand, BaseBarWidth);
            Assert.AreEqual(BarEffect.Expand, result.NextBar.ActiveEffect);
            Assert.That(result.NextBar.Width, Is.EqualTo(BaseBarWidth * 1.5f).Within(0.001f));
            CollectionAssert.AreEqual(attached, result.ReleasedBallIds);
            Assert.IsFalse(result.ClearLaserShots);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0];
            Assert.IsInstanceOf<BallsReleasedEvent>(evt);
            Assert.AreEqual(BallReleaseReason.Replaced, ((BallsReleasedEvent)evt).ReleaseReason);
        }

        [Test]
        public void LaserInCooldown_ToExpand_ClearShotsTrue()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Laser);
            var result = svc.ApplyEffect(bar, 0f, 300f, System.Array.Empty<string>(), ItemType.Expand, BaseBarWidth);
            Assert.AreEqual(BarEffect.Expand, result.NextBar.ActiveEffect);
            Assert.That(result.NextBar.Width, Is.EqualTo(BaseBarWidth * 1.5f).Within(0.001f));
            Assert.AreEqual(0f, result.NextLaserCooldown);
            Assert.IsTrue(result.ClearLaserShots);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void LaserInCooldown_ToMagnet_ClearShotsTrue()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Laser);
            var result = svc.ApplyEffect(bar, 0f, 300f, System.Array.Empty<string>(), ItemType.Magnet, BaseBarWidth);
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.AreEqual(0f, result.NextLaserCooldown);
            Assert.IsTrue(result.ClearLaserShots);
            Assert.AreEqual(8000f, result.NextMagnetRemaining);
        }

        [Test]
        public void LaserInCooldown_ToLaser_ClearShotsTrue()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Laser);
            var result = svc.ApplyEffect(bar, 0f, 300f, System.Array.Empty<string>(), ItemType.Laser, BaseBarWidth);
            Assert.AreEqual(BarEffect.Laser, result.NextBar.ActiveEffect);
            Assert.AreEqual(0f, result.NextLaserCooldown);
            Assert.IsTrue(result.ClearLaserShots);
        }

        [Test]
        public void MagnetToLaser_WithAttached_Released_ReplacedReason()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0", "ball_1" };
            var result = svc.ApplyEffect(bar, 5000f, 0f, attached, ItemType.Laser, BaseBarWidth);
            Assert.AreEqual(BarEffect.Laser, result.NextBar.ActiveEffect);
            Assert.IsFalse(result.ClearLaserShots);
            CollectionAssert.AreEqual(attached, result.ReleasedBallIds);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0] as BallsReleasedEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual(BallReleaseReason.Replaced, evt!.ReleaseReason);
        }

        [Test]
        public void MagnetToMagnet_TimerReset()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.ApplyEffect(bar, 3000f, 0f, System.Array.Empty<string>(), ItemType.Magnet, BaseBarWidth);
            Assert.AreEqual(8000f, result.NextMagnetRemaining);
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.IsFalse(result.ClearLaserShots);
        }

        // ─── tickMagnet ───

        [Test]
        public void TickMagnet_None_NoOp()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.None);
            var result = svc.TickMagnet(0f, System.Array.Empty<string>(), bar, 100f);
            Assert.AreEqual(0f, result.NextMagnetRemaining);
            Assert.AreEqual(bar, result.NextBar);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void TickMagnet_Decreases()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.TickMagnet(8000f, System.Array.Empty<string>(), bar, 16.7f);
            Assert.That(result.NextMagnetRemaining, Is.EqualTo(7983.3f).Within(0.01f));
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
        }

        [Test]
        public void TickMagnet_Timeout_EndsAndReleases()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0" };
            var result = svc.TickMagnet(50f, attached, bar, 100f);
            Assert.AreEqual(0f, result.NextMagnetRemaining);
            Assert.AreEqual(BarEffect.None, result.NextBar.ActiveEffect);
            CollectionAssert.AreEqual(attached, result.ReleasedBallIds);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0] as BallsReleasedEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual(BallReleaseReason.Timeout, evt!.ReleaseReason);
            CollectionAssert.AreEqual(attached, evt.BallIds);
        }

        [Test]
        public void TickMagnet_Timeout_NoAttached_NoEvent()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.TickMagnet(50f, System.Array.Empty<string>(), bar, 100f);
            Assert.AreEqual(BarEffect.None, result.NextBar.ActiveEffect);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
        }

        [Test]
        public void TickMagnet_StepsUpTo8000_EndsCorrectly()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0" };
            var remaining = 8000f;
            var currentBar = bar;
            IReadOnlyList<string> released = System.Array.Empty<string>();
            var steps = new[] { 100f, 500f, 1000f, 1000f, 2000f, 2000f, 1400f };  // 합계 8000
            foreach (var step in steps)
            {
                var r = svc.TickMagnet(remaining, attached, currentBar, step);
                remaining = r.NextMagnetRemaining;
                currentBar = r.NextBar;
                if (r.ReleasedBallIds.Count > 0) released = r.ReleasedBallIds;
            }
            Assert.AreEqual(0f, remaining);
            Assert.AreEqual(BarEffect.None, currentBar.ActiveEffect);
            CollectionAssert.AreEqual(attached, released);
        }

        // ─── releaseManually ───

        [Test]
        public void ReleaseManually_WithAttached_MagnetKept_SpaceReason()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0", "ball_1" };
            var result = svc.ReleaseManually(bar, attached);
            // 지속형 자석: activeEffect 는 magnet 유지
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            CollectionAssert.AreEqual(attached, result.ReleasedBallIds);
            Assert.AreEqual(1, result.Events.Count);
            var evt = result.Events[0] as BallsReleasedEvent;
            Assert.IsNotNull(evt);
            Assert.AreEqual(BallReleaseReason.Space, evt!.ReleaseReason);
            CollectionAssert.AreEqual(attached, evt.BallIds);
        }

        [Test]
        public void ReleaseManually_NoAttached_NoEvent_MagnetKept()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.ReleaseManually(bar, System.Array.Empty<string>());
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.ReleasedBallIds.Count);
        }

        [Test]
        public void ReleaseManually_BarStateImmutable()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var _ = svc.ReleaseManually(bar, new[] { "ball_0" });
            // BarState 는 struct — 원본 변경 X (값 의미). activeEffect 그대로.
            Assert.AreEqual(BarEffect.Magnet, bar.ActiveEffect);
        }

        [Test]
        public void ReleaseManually_ReleasedBallIdsReturned()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var attached = new[] { "ball_0" };
            var result = svc.ReleaseManually(bar, attached);
            CollectionAssert.AreEqual(attached, result.ReleasedBallIds);
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
        }

        [Test]
        public void ReleaseManually_TimerRemains_MagnetKeptForReattach()
        {
            var svc = new BarEffectService(ItemDefs);
            var bar = MakeBar(BarEffect.Magnet);
            var result = svc.ReleaseManually(bar, new[] { "ball_0" });
            Assert.AreEqual(BarEffect.Magnet, result.NextBar.ActiveEffect);
            // 이후 TickMagnet 이 타이머 감소 → 0 도달 시 None.
        }
    }
}
