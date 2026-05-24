using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class InputCommandResolverTests
    {
        // --- Fixtures ---
        private static readonly InputSnapshot NoInput = new(LeftDown: false, RightDown: false, SpaceJustPressed: false);
        private static readonly InputSnapshot SpaceInput = new(LeftDown: false, RightDown: false, SpaceJustPressed: true);
        private static readonly InputSnapshot LeftInput = new(LeftDown: true, RightDown: false, SpaceJustPressed: false);
        private static readonly InputSnapshot RightInput = new(LeftDown: false, RightDown: true, SpaceJustPressed: false);

        private static GameplayRuntimeState State(
            BarEffect activeEffect = BarEffect.None,
            IReadOnlyList<string>? attachedBallIds = null,
            float laserCooldownRemaining = 0f,
            bool ballActive = false)
        {
            return MakeState(
                bar: new BarState(480f, 660f, 120f, 420f, activeEffect),
                balls: new[] { Ball(isActive: ballActive) },
                attachedBallIds: attachedBallIds,
                laserCooldownRemaining: laserCooldownRemaining);
        }

        // --- MoveBar 커맨드 ---

        [Test]
        public void NoInput_ReturnsMoveBarDirection0()
        {
            var cmds = InputCommandResolver.ResolveGameplayCommands(NoInput, State());
            var move = cmds.OfType<MoveBarCommand>().FirstOrDefault();
            Assert.IsNotNull(move);
            Assert.AreEqual(0, move!.Direction);
        }

        [Test]
        public void LeftInput_ReturnsMoveBarDirectionMinus1()
        {
            var cmds = InputCommandResolver.ResolveGameplayCommands(LeftInput, State());
            var move = cmds.OfType<MoveBarCommand>().FirstOrDefault();
            Assert.AreEqual(-1, move!.Direction);
        }

        [Test]
        public void RightInput_ReturnsMoveBarDirection1()
        {
            var cmds = InputCommandResolver.ResolveGameplayCommands(RightInput, State());
            var move = cmds.OfType<MoveBarCommand>().FirstOrDefault();
            Assert.AreEqual(1, move!.Direction);
        }

        [Test]
        public void BothInputs_ReturnsMoveBarDirection0()
        {
            var both = new InputSnapshot(LeftDown: true, RightDown: true, SpaceJustPressed: false);
            var cmds = InputCommandResolver.ResolveGameplayCommands(both, State());
            var move = cmds.OfType<MoveBarCommand>().FirstOrDefault();
            Assert.AreEqual(0, move!.Direction);
        }

        // --- spaceJustPressed 분기 ---

        [Test]
        public void Magnet_WithAttachedBalls_Space_ReturnsReleaseAttachedBalls()
        {
            var state = State(BarEffect.Magnet, attachedBallIds: new[] { "ball_0" }, ballActive: true);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsTrue(cmds.Any(c => c is ReleaseAttachedBallsCommand));
            Assert.IsFalse(cmds.Any(c => c is LaunchBallCommand));
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
        }

        [Test]
        public void Magnet_NoAttachedBalls_Space_InactiveBall_ReturnsLaunchBall()
        {
            var state = State(BarEffect.Magnet, attachedBallIds: System.Array.Empty<string>(), ballActive: false);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsTrue(cmds.Any(c => c is LaunchBallCommand));
            Assert.IsFalse(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        // 레이저는 자동 발사 — InputCommandResolver 관여 X.
        [Test]
        public void Laser_CooldownZero_Space_NoFireLaser()
        {
            var state = State(BarEffect.Laser, laserCooldownRemaining: 0f, ballActive: true);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
            Assert.IsFalse(cmds.Any(c => c is LaunchBallCommand));
        }

        [Test]
        public void Laser_CooldownPositive_Space_NoCommand()
        {
            var state = State(BarEffect.Laser, laserCooldownRemaining: 500f, ballActive: true);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
            Assert.IsFalse(cmds.Any(c => c is LaunchBallCommand));
            Assert.IsFalse(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        [Test]
        public void Expand_InactiveBall_Space_ReturnsLaunchBall()
        {
            var state = State(BarEffect.Expand, ballActive: false);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsTrue(cmds.Any(c => c is LaunchBallCommand));
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
            Assert.IsFalse(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        [Test]
        public void None_ActiveBall_Space_NoCommand()
        {
            var state = State(BarEffect.None, ballActive: true);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsFalse(cmds.Any(c => c is LaunchBallCommand));
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
            Assert.IsFalse(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        [Test]
        public void NoSpace_Magnet_NoReleaseCommand()
        {
            var state = State(BarEffect.Magnet, attachedBallIds: new[] { "ball_0" });
            var cmds = InputCommandResolver.ResolveGameplayCommands(NoInput, state);
            Assert.IsFalse(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        [Test]
        public void Magnet_Space_NoFireLaser_ReleasePriority()
        {
            var state = State(BarEffect.Magnet, attachedBallIds: new[] { "ball_0" }, laserCooldownRemaining: 0f);
            var cmds = InputCommandResolver.ResolveGameplayCommands(SpaceInput, state);
            Assert.IsFalse(cmds.Any(c => c is FireLaserCommand));
            Assert.IsTrue(cmds.Any(c => c is ReleaseAttachedBallsCommand));
        }

        [Test]
        public void NoSpace_ReturnsOnlyMoveBar()
        {
            var state = State(BarEffect.None, ballActive: false);
            var cmds = InputCommandResolver.ResolveGameplayCommands(NoInput, state);
            Assert.AreEqual(1, cmds.Count);
            Assert.IsInstanceOf<MoveBarCommand>(cmds[0]);
        }
    }
}
