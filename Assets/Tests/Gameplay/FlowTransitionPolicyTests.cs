using NUnit.Framework;
using Arkanoid.Flow;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class FlowTransitionPolicyTests
    {
        private static readonly FlowCommand StartGame = new StartGameRequestedCommand();
        private static readonly FlowCommand IntroFinished = new IntroSequenceFinishedCommand();
        private static readonly FlowCommand RoundIntroFinished = new RoundIntroFinishedCommand();
        private static readonly FlowCommand GameOverConditionMet = new GameOverConditionMetCommand();
        private static readonly FlowCommand StageClearedLast = new StageClearedCommand(true);
        private static readonly FlowCommand StageClearedNotLast = new StageClearedCommand(false);
        private static readonly FlowCommand Retry = new RetryRequestedCommand();
        private static FlowCommand LifeLost(int remaining) => new LifeLostCommand(remaining);

        // ─── Title 유효 전이 ───

        [Test]
        public void Title_StartGame_IntroStory() =>
            Assert.AreEqual(FlowStateKind.IntroStory, FlowTransitionPolicy.NextState(FlowStateKind.Title, StartGame));

        // ─── IntroStory ───

        [Test]
        public void IntroStory_IntroFinished_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.IntroStory, IntroFinished));

        // ─── RoundIntro ───

        [Test]
        public void RoundIntro_RoundIntroFinished_InGame() =>
            Assert.AreEqual(FlowStateKind.InGame, FlowTransitionPolicy.NextState(FlowStateKind.RoundIntro, RoundIntroFinished));

        // ─── InGame ───

        [Test]
        public void InGame_LifeLost2_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.InGame, LifeLost(2)));

        [Test]
        public void InGame_LifeLost1_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.InGame, LifeLost(1)));

        [Test]
        public void InGame_GameOverConditionMet_GameOver() =>
            Assert.AreEqual(FlowStateKind.GameOver, FlowTransitionPolicy.NextState(FlowStateKind.InGame, GameOverConditionMet));

        [Test]
        public void InGame_StageClearedLast_GameClear() =>
            Assert.AreEqual(FlowStateKind.GameClear, FlowTransitionPolicy.NextState(FlowStateKind.InGame, StageClearedLast));

        [Test]
        public void InGame_StageClearedNotLast_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.InGame, StageClearedNotLast));

        // ─── GameOver / GameClear ───

        [Test]
        public void GameOver_Retry_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.GameOver, Retry));

        [Test]
        public void GameClear_Retry_RoundIntro() =>
            Assert.AreEqual(FlowStateKind.RoundIntro, FlowTransitionPolicy.NextState(FlowStateKind.GameClear, Retry));

        // ─── 무효 조합 ───

        [Test]
        public void Title_LifeLost_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.Title, LifeLost(2)));

        [Test]
        public void Title_Retry_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.Title, Retry));

        [Test]
        public void Title_RoundIntroFinished_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.Title, RoundIntroFinished));

        [Test]
        public void IntroStory_StartGame_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.IntroStory, StartGame));

        [Test]
        public void IntroStory_RoundIntroFinished_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.IntroStory, RoundIntroFinished));

        [Test]
        public void RoundIntro_StartGame_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.RoundIntro, StartGame));

        [Test]
        public void RoundIntro_LifeLost_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.RoundIntro, LifeLost(2)));

        [Test]
        public void GameOver_LifeLost_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.GameOver, LifeLost(2)));

        [Test]
        public void GameClear_LifeLost_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.GameClear, LifeLost(2)));

        [Test]
        public void GameClear_GameOverConditionMet_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.GameClear, GameOverConditionMet));

        [Test]
        public void InGame_LifeLost0_Null() =>
            Assert.IsNull(FlowTransitionPolicy.NextState(FlowStateKind.InGame, LifeLost(0)));
    }
}
