using NUnit.Framework;
using Arkanoid.Flow;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class FlowLifecycleHandlerTests
    {
        [Test]
        public void EnteredTitle_FromGameOver()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.Title, FlowStateKind.GameOver);
            Assert.IsInstanceOf<EnteredTitleEvent>(ev);
            Assert.AreEqual(FlowStateKind.GameOver, ((EnteredTitleEvent)ev).From);
        }

        [Test]
        public void EnteredIntroStory_FromTitle()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.IntroStory, FlowStateKind.Title);
            Assert.IsInstanceOf<EnteredIntroStoryEvent>(ev);
            Assert.AreEqual(FlowStateKind.Title, ((EnteredIntroStoryEvent)ev).From);
        }

        [Test]
        public void EnteredRoundIntro_FromIntroStory()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.RoundIntro, FlowStateKind.IntroStory);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(ev);
            Assert.AreEqual(FlowStateKind.IntroStory, ((EnteredRoundIntroEvent)ev).From);
        }

        [Test]
        public void EnteredRoundIntro_FromInGame()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.RoundIntro, FlowStateKind.InGame);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(ev);
            Assert.AreEqual(FlowStateKind.InGame, ((EnteredRoundIntroEvent)ev).From);
        }

        [Test]
        public void EnteredInGame_FromRoundIntro()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.InGame, FlowStateKind.RoundIntro);
            Assert.IsInstanceOf<EnteredInGameEvent>(ev);
        }

        [Test]
        public void EnteredGameOver_FromInGame()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.GameOver, FlowStateKind.InGame);
            Assert.IsInstanceOf<EnteredGameOverEvent>(ev);
        }

        [Test]
        public void EnteredGameClear_FromInGame()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.GameClear, FlowStateKind.InGame);
            Assert.IsInstanceOf<EnteredGameClearEvent>(ev);
        }

        [Test]
        public void TitleReturn_FromGameClear()
        {
            var ev = FlowLifecycleHandler.OnEnter(FlowStateKind.Title, FlowStateKind.GameClear);
            Assert.IsInstanceOf<EnteredTitleEvent>(ev);
            Assert.AreEqual(FlowStateKind.GameClear, ((EnteredTitleEvent)ev).From);
        }
    }
}
