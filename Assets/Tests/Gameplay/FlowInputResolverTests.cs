using NUnit.Framework;
using Arkanoid.Flow;
using Arkanoid.Gameplay;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class FlowInputResolverTests
    {
        private static readonly InputSnapshot SnapSpace = new(false, false, true);
        private static readonly InputSnapshot SnapNoSpace = new(false, false, false);
        private static readonly InputSnapshot SnapLeft = new(true, false, false);

        // Title

        [Test]
        public void Title_Space_StartGameRequested() =>
            Assert.IsInstanceOf<StartGameRequestedCommand>(FlowInputResolver.ResolveFlowCommand(FlowStateKind.Title, SnapSpace));

        [Test]
        public void Title_NoSpace_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.Title, SnapNoSpace));

        [Test]
        public void Title_LeftDownOnly_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.Title, SnapLeft));

        // GameOver

        [Test]
        public void GameOver_Space_RetryRequested() =>
            Assert.IsInstanceOf<RetryRequestedCommand>(FlowInputResolver.ResolveFlowCommand(FlowStateKind.GameOver, SnapSpace));

        [Test]
        public void GameOver_NoSpace_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.GameOver, SnapNoSpace));

        // GameClear

        [Test]
        public void GameClear_Space_RetryRequested() =>
            Assert.IsInstanceOf<RetryRequestedCommand>(FlowInputResolver.ResolveFlowCommand(FlowStateKind.GameClear, SnapSpace));

        [Test]
        public void GameClear_NoSpace_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.GameClear, SnapNoSpace));

        // IntroStory / RoundIntro / InGame — 항상 null

        [Test]
        public void IntroStory_Space_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.IntroStory, SnapSpace));

        [Test]
        public void RoundIntro_Space_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.RoundIntro, SnapSpace));

        [Test]
        public void InGame_Space_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.InGame, SnapSpace));

        [Test]
        public void InGame_NoInput_Null() =>
            Assert.IsNull(FlowInputResolver.ResolveFlowCommand(FlowStateKind.InGame, SnapNoSpace));
    }
}
