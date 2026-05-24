using System;
using NUnit.Framework;
using static Arkanoid.Gameplay.Tests.TestHelpers;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class StageRuleServiceTests
    {
        [Test]
        public void NormalState_ReturnsNone()
        {
            var state = MakeState(blocks: new[] { LivingBlock("b0") });
            var result = StageRuleService.JudgeStageOutcome(state, Array.Empty<GameplayEvent>());
            Assert.IsInstanceOf<StageOutcomeNone>(result);
        }

        [Test]
        public void AllBlocksDestroyed_ReturnsClear()
        {
            var state = MakeState(blocks: new[] { DestroyedBlock("b0"), DestroyedBlock("b1") });
            var result = StageRuleService.JudgeStageOutcome(state, Array.Empty<GameplayEvent>());
            Assert.IsInstanceOf<StageOutcomeClear>(result);
        }

        [Test]
        public void LifeLost_With3Lives_ReturnsLifeLostWithRemaining2()
        {
            var state = MakeState(
                blocks: new[] { LivingBlock("b0") },
                session: new GameSessionState(0, 0, 3, 0));
            var events = new GameplayEvent[] { new LifeLostEvent(0) };
            var result = StageRuleService.JudgeStageOutcome(state, events);
            Assert.IsInstanceOf<StageOutcomeLifeLost>(result);
            Assert.AreEqual(2, ((StageOutcomeLifeLost)result).RemainingLives);
        }

        [Test]
        public void LifeLost_With1Life_ReturnsGameOver()
        {
            var state = MakeState(
                blocks: new[] { LivingBlock("b0") },
                session: new GameSessionState(0, 0, 1, 0));
            var events = new GameplayEvent[] { new LifeLostEvent(0) };
            var result = StageRuleService.JudgeStageOutcome(state, events);
            Assert.IsInstanceOf<StageOutcomeGameOver>(result);
        }

        [Test]
        public void LifeLost_With0Lives_ReturnsGameOver()
        {
            var state = MakeState(
                blocks: new[] { LivingBlock("b0") },
                session: new GameSessionState(0, 0, 0, 0));
            var events = new GameplayEvent[] { new LifeLostEvent(0) };
            var result = StageRuleService.JudgeStageOutcome(state, events);
            Assert.IsInstanceOf<StageOutcomeGameOver>(result);
        }

        // 우선순위: GameOver > LifeLost > Clear > None.
        [Test]
        public void GameOverTakesPriorityOverClear()
        {
            var state = MakeState(
                blocks: new[] { DestroyedBlock("b0") },
                session: new GameSessionState(0, 0, 1, 0));
            var events = new GameplayEvent[] { new LifeLostEvent(0) };
            var result = StageRuleService.JudgeStageOutcome(state, events);
            Assert.IsInstanceOf<StageOutcomeGameOver>(result);
        }

        [Test]
        public void NoBlocks_DoesNotDetectClear()
        {
            var state = MakeState();
            var result = StageRuleService.JudgeStageOutcome(state, Array.Empty<GameplayEvent>());
            Assert.IsInstanceOf<StageOutcomeNone>(result);
        }
    }
}
