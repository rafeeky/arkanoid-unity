using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Arkanoid.Definitions;
using Arkanoid.Flow;
using Arkanoid.Gameplay;

namespace Arkanoid.Gameplay.Tests
{
    [TestFixture]
    public class GameFlowControllerTests
    {
        private static readonly InputSnapshot SnapSpace = new(false, false, true);
        private static readonly InputSnapshot SnapNoSpace = new(false, false, false);

        private static (GameFlowController Controller, List<FlowEvent> Events) MakeController(int totalStageCount = 1)
        {
            var events = new List<FlowEvent>();
            var ctrl = new GameFlowController(e => events.Add(e), totalStageCount);
            return (ctrl, events);
        }

        // Title → IntroStory → RoundIntro → InGame 경로 헬퍼.
        private static void AdvanceToInGame(GameFlowController ctrl)
        {
            ctrl.HandleInput(SnapSpace);                                            // Title → IntroStory
            ctrl.HandlePresentationEvent(new IntroSequenceFinishedEvent());          // IntroStory → RoundIntro
            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());             // RoundIntro → InGame
        }

        // ─── 초기 상태 ───

        [Test]
        public void InitialState_TitleStage0Normal()
        {
            var (ctrl, _) = MakeController();
            var s = ctrl.GetState();
            Assert.AreEqual(FlowStateKind.Title, s.Kind);
            Assert.AreEqual(0, s.CurrentStageIndex);
            Assert.AreEqual(DifficultyKind.Normal, s.SelectedDifficulty);
        }

        // ─── 전체 흐름 (Title → ... → GameOver → Title) ───

        [Test]
        public void FullFlow_TitleToGameOverToRoundIntro_TransitionsAndEvents()
        {
            var (ctrl, events) = MakeController();

            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(FlowStateKind.IntroStory, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredIntroStoryEvent>(events[^1]);
            Assert.AreEqual(FlowStateKind.Title, ((EnteredIntroStoryEvent)events[^1]).From);

            ctrl.HandlePresentationEvent(new IntroSequenceFinishedEvent());
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[^1]);

            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            Assert.AreEqual(FlowStateKind.InGame, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredInGameEvent>(events[^1]);

            ctrl.HandleGameplayEvent(new LifeLostEvent(1));
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[^1]);

            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            Assert.AreEqual(FlowStateKind.InGame, ctrl.GetState().Kind);

            ctrl.HandleGameplayEvent(new LifeLostEvent(0));
            Assert.AreEqual(FlowStateKind.GameOver, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredGameOverEvent>(events[^1]);

            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[^1]);
            Assert.AreEqual(FlowStateKind.GameOver, ((EnteredRoundIntroEvent)events[^1]).From);
        }

        [Test]
        public void FullFlow_EventsEmittedInOrder()
        {
            var (ctrl, events) = MakeController();

            ctrl.HandleInput(SnapSpace);
            ctrl.HandlePresentationEvent(new IntroSequenceFinishedEvent());
            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            ctrl.HandleGameplayEvent(new LifeLostEvent(0));
            ctrl.HandleInput(SnapSpace);

            Assert.AreEqual(5, events.Count);
            Assert.IsInstanceOf<EnteredIntroStoryEvent>(events[0]);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[1]);
            Assert.IsInstanceOf<EnteredInGameEvent>(events[2]);
            Assert.IsInstanceOf<EnteredGameOverEvent>(events[3]);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[4]);
        }

        // ─── IntroSequenceFinished → RoundIntro ───

        [Test]
        public void IntroStory_IntroSequenceFinished_RoundIntro()
        {
            var (ctrl, events) = MakeController();
            ctrl.HandleInput(SnapSpace);
            ctrl.HandlePresentationEvent(new IntroSequenceFinishedEvent());
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[^1]);
        }

        // ─── 다중 스테이지 ───

        [Test]
        public void Stage0_StageCleared_StageIndex1_RoundIntro()
        {
            var (ctrl, _) = MakeController(3);
            AdvanceToInGame(ctrl);
            Assert.AreEqual(0, ctrl.GetState().CurrentStageIndex);

            ctrl.HandleGameplayEvent(new StageClearedEvent());
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.AreEqual(1, ctrl.GetState().CurrentStageIndex);
        }

        [Test]
        public void Stage1_StageCleared_StageIndex2()
        {
            var (ctrl, _) = MakeController(3);
            AdvanceToInGame(ctrl);
            ctrl.HandleGameplayEvent(new StageClearedEvent());
            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            ctrl.HandleGameplayEvent(new StageClearedEvent());
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.AreEqual(2, ctrl.GetState().CurrentStageIndex);
        }

        [Test]
        public void LastStage_StageCleared_GameClear_StageIndexKept()
        {
            var (ctrl, events) = MakeController(3);
            AdvanceToInGame(ctrl);
            ctrl.HandleGameplayEvent(new StageClearedEvent());
            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            ctrl.HandleGameplayEvent(new StageClearedEvent());
            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            ctrl.HandleGameplayEvent(new StageClearedEvent());

            Assert.AreEqual(FlowStateKind.GameClear, ctrl.GetState().Kind);
            Assert.AreEqual(2, ctrl.GetState().CurrentStageIndex);
            Assert.IsInstanceOf<EnteredGameClearEvent>(events[^1]);
        }

        // ─── GameClear → SPACE → RoundIntro (stage 0 리셋) ───

        [Test]
        public void GameClear_Space_RoundIntro_StageIndexReset()
        {
            var (ctrl, events) = MakeController(1);
            AdvanceToInGame(ctrl);
            ctrl.HandleGameplayEvent(new StageClearedEvent());
            Assert.AreEqual(FlowStateKind.GameClear, ctrl.GetState().Kind);

            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(FlowStateKind.RoundIntro, ctrl.GetState().Kind);
            Assert.AreEqual(0, ctrl.GetState().CurrentStageIndex);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(events[^1]);
            Assert.AreEqual(FlowStateKind.GameClear, ((EnteredRoundIntroEvent)events[^1]).From);
        }

        [Test]
        public void TitleStartGame_StageIndexZero()
        {
            var (ctrl, _) = MakeController();
            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(0, ctrl.GetState().CurrentStageIndex);
        }

        // ─── 무효 입력 ───

        [Test]
        public void Title_NoSpace_StateUnchanged()
        {
            var (ctrl, events) = MakeController();
            ctrl.HandleInput(SnapNoSpace);
            Assert.AreEqual(FlowStateKind.Title, ctrl.GetState().Kind);
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void InGame_BallLaunched_StateUnchanged()
        {
            var (ctrl, events) = MakeController();
            AdvanceToInGame(ctrl);
            var prevCount = events.Count;
            ctrl.HandleGameplayEvent(new BallLaunchedEvent());
            Assert.AreEqual(FlowStateKind.InGame, ctrl.GetState().Kind);
            Assert.AreEqual(prevCount, events.Count);
        }

        [Test]
        public void GameOver_GameOverConditionMet_StateUnchanged()
        {
            var (ctrl, events) = MakeController();
            AdvanceToInGame(ctrl);
            ctrl.HandleGameplayEvent(new LifeLostEvent(0));  // → GameOver
            var prevCount = events.Count;
            ctrl.HandleGameplayEvent(new GameOverConditionMetEvent());
            Assert.AreEqual(FlowStateKind.GameOver, ctrl.GetState().Kind);
            Assert.AreEqual(prevCount, events.Count);
        }

        [Test]
        public void IntroStory_Space_StateUnchanged()
        {
            var (ctrl, events) = MakeController();
            ctrl.HandleInput(SnapSpace);
            var prevCount = events.Count;
            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(FlowStateKind.IntroStory, ctrl.GetState().Kind);
            Assert.AreEqual(prevCount, events.Count);
        }

        // ─── Listener 호출 횟수 ───

        [Test]
        public void Listener_CalledOncePerTransition()
        {
            var listenerCalls = new List<FlowEvent>();
            var ctrl = new GameFlowController(e => listenerCalls.Add(e), 1);

            ctrl.HandleInput(SnapSpace);
            Assert.AreEqual(1, listenerCalls.Count);
            Assert.IsInstanceOf<EnteredIntroStoryEvent>(listenerCalls[^1]);

            ctrl.HandlePresentationEvent(new IntroSequenceFinishedEvent());
            Assert.AreEqual(2, listenerCalls.Count);
            Assert.IsInstanceOf<EnteredRoundIntroEvent>(listenerCalls[^1]);

            ctrl.HandlePresentationEvent(new RoundIntroFinishedEvent());
            Assert.AreEqual(3, listenerCalls.Count);
            Assert.IsInstanceOf<EnteredInGameEvent>(listenerCalls[^1]);

            ctrl.HandleGameplayEvent(new StageClearedEvent());  // totalStageCount=1, last → GameClear
            Assert.AreEqual(4, listenerCalls.Count);
            Assert.IsInstanceOf<EnteredGameClearEvent>(listenerCalls[^1]);
        }
    }
}
