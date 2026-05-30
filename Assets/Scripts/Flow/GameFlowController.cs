using System;
using Arkanoid.Definitions;
using Arkanoid.Gameplay;

namespace Arkanoid.Flow
{
    // Flow 상태기계 orchestration. 입력/Gameplay/Presentation 이벤트 → FlowCommand → 전이.
    // LifeLost: remainingLives > 0 → LifeLost (RoundIntro), == 0 → GameOverConditionMet (GameOver).
    // StageCleared: Gameplay 는 발행만, Flow 가 마지막 스테이지 여부 판정.
    //   currentStageIndex + 1 == totalStageCount → isLastStage=true (GameClear), 아니면 다음 RoundIntro + idx 증가.
    // StartGameRequested → currentStageIndex 리셋 (0).
    public sealed class GameFlowController
    {
        private readonly GameFlowState _state;
        private readonly Action<FlowEvent> _listener;
        private readonly int _totalStageCount;

        public GameFlowController(Action<FlowEvent> listener, int totalStageCount = 1)
        {
            _state = GameFlowState.CreateInitial();
            _listener = listener;
            _totalStageCount = totalStageCount;
        }

        public GameFlowState GetState() => _state;

        public void HandleInput(InputSnapshot input)
        {
            var cmd = FlowInputResolver.ResolveFlowCommand(_state.Kind, input);
            if (cmd != null) ApplyCommand(cmd);
        }

        public void HandleGameplayEvent(GameplayEvent ev)
        {
            var cmd = TranslateGameplayEvent(ev);
            if (cmd != null) ApplyCommand(cmd);
        }

        public void HandlePresentationEvent(PresentationEvent ev)
        {
            var cmd = TranslatePresentationEvent(ev);
            if (cmd != null) ApplyCommand(cmd);
        }

        // UI 직접 호출용 — Title NORMAL/HARD 버튼 onClick → 난이도 설정 + 게임 시작.
        public void RequestStartGame(DifficultyKind difficulty)
        {
            ApplyCommand(difficulty == DifficultyKind.Hard
                ? (FlowCommand)new DifficultySelectHardCommand()
                : new DifficultySelectNormalCommand());
            ApplyCommand(new StartGameRequestedCommand());
        }

        // GameOver/GameClear 의 RETRY 버튼 — RoundIntro 로 리셋
        public void RequestRetry() => ApplyCommand(new RetryRequestedCommand());

        // GameOver/GameClear 의 TITLE/QUIT 버튼 — 강제 Title 전환
        public void RequestQuitToTitle()
        {
            var from = _state.Kind;
            _state.Kind = FlowStateKind.Title;
            _state.CurrentStageIndex = 0;
            _listener(FlowLifecycleHandler.OnEnter(FlowStateKind.Title, from));
        }

        // 인트로 스토리 스킵 (Phase 4 IntroSequenceFinishedCommand 가 RoundIntro 로 전이)
        public void SkipIntroStory() => ApplyCommand(new IntroSequenceFinishedCommand());

        // 디버그용 — 현재 stage 강제 클리어 → 다음 stage (Last 면 GameClear)
        public void DebugForceStageClear() =>
            ApplyCommand(new StageClearedCommand(_state.CurrentStageIndex + 1 >= _totalStageCount));

        private FlowCommand? TranslateGameplayEvent(GameplayEvent ev)
        {
            switch (ev)
            {
                case LifeLostEvent ll:
                    return ll.RemainingLives > 0
                        ? new LifeLostCommand(ll.RemainingLives)
                        : (FlowCommand)new GameOverConditionMetCommand();
                case StageClearedEvent:
                    {
                        var isLastStage = _state.CurrentStageIndex + 1 >= _totalStageCount;
                        return new StageClearedCommand(isLastStage);
                    }
                case GameOverConditionMetEvent:
                    return new GameOverConditionMetCommand();
                default:
                    return null;
            }
        }

        private static FlowCommand? TranslatePresentationEvent(PresentationEvent ev) => ev switch
        {
            IntroSequenceFinishedEvent => new IntroSequenceFinishedCommand(),
            RoundIntroFinishedEvent => new RoundIntroFinishedCommand(),
            // LifeLostPresentationFinished → Flow 가 직접 사용 X (presentation 동기화용).
            _ => null,
        };

        private void ApplyCommand(FlowCommand command)
        {
            // 묶음 E: Title 난이도 커서 — intra-state 업데이트 (FlowTransitionPolicy 미경유).
            if (command is DifficultySelectNormalCommand && _state.Kind == FlowStateKind.Title)
            {
                _state.SelectedDifficulty = DifficultyKind.Normal;
                return;
            }
            if (command is DifficultySelectHardCommand && _state.Kind == FlowStateKind.Title)
            {
                _state.SelectedDifficulty = DifficultyKind.Hard;
                return;
            }

            var from = _state.Kind;
            var next = FlowTransitionPolicy.NextState(from, command);
            if (next is null) return;

            // currentStageIndex 갱신:
            //   StartGameRequested → 0 (새 게임)
            //   RetryRequested (gameOver/gameClear → RoundIntro) → 0 (2026-05-19)
            //   StageCleared (!isLast) → 증가
            switch (command)
            {
                case StartGameRequestedCommand:
                    _state.CurrentStageIndex = 0;
                    break;
                case RetryRequestedCommand when from == FlowStateKind.GameOver || from == FlowStateKind.GameClear:
                    _state.CurrentStageIndex = 0;
                    break;
                case StageClearedCommand sc when !sc.IsLastStage:
                    _state.CurrentStageIndex++;
                    break;
            }

            _state.Kind = next.Value;
            _listener(FlowLifecycleHandler.OnEnter(next.Value, from));
        }
    }
}
