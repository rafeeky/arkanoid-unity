namespace Arkanoid.Flow
{
    // FlowCommand union (Flow 의 상태 전이 트리거).
    public abstract record FlowCommand;

    public sealed record StartGameRequestedCommand() : FlowCommand;
    public sealed record IntroSequenceFinishedCommand() : FlowCommand;
    public sealed record RoundIntroFinishedCommand() : FlowCommand;
    public sealed record LifeLostCommand(int RemainingLives) : FlowCommand;
    public sealed record GameOverConditionMetCommand() : FlowCommand;
    public sealed record StageClearedCommand(bool IsLastStage) : FlowCommand;
    public sealed record RetryRequestedCommand() : FlowCommand;
    // Q 키로 어디서든 타이틀 복귀 (묶음 F).
    public sealed record ReturnToTitleRequestedCommand() : FlowCommand;
    // Title 화면 난이도 커서 — intra-state 업데이트.
    public sealed record DifficultySelectNormalCommand() : FlowCommand;
    public sealed record DifficultySelectHardCommand() : FlowCommand;

    // 순수 상태 전이 함수. null = 해당 상태에서 무효한 커맨드.
    // LifeLost: remainingLives > 0 → RoundIntro. == 0 은 Controller 가 GameOverConditionMet 으로 변환.
    // StageCleared: isLastStage → GameClear, !isLastStage → RoundIntro (Controller 가 stageIndex 증가).
    // Q 복귀: 비-Title 모든 상태에서 Title 로 (FlowInputResolver 에서 Title 자체 입력 막음).
    // Difficulty 선택: intra-state — nextState 가 다루지 않음.
    public static class FlowTransitionPolicy
    {
        public static FlowStateKind? NextState(FlowStateKind current, FlowCommand command)
        {
            // Q 복귀: 비-Title 모든 상태에서 허용.
            if (command is ReturnToTitleRequestedCommand && current != FlowStateKind.Title)
                return FlowStateKind.Title;

            return current switch
            {
                FlowStateKind.Title => command is StartGameRequestedCommand
                    ? (FlowStateKind?)FlowStateKind.IntroStory
                    : null,
                FlowStateKind.IntroStory => command is IntroSequenceFinishedCommand
                    ? (FlowStateKind?)FlowStateKind.RoundIntro
                    : null,
                FlowStateKind.RoundIntro => command is RoundIntroFinishedCommand
                    ? (FlowStateKind?)FlowStateKind.InGame
                    : null,
                FlowStateKind.InGame => command switch
                {
                    LifeLostCommand ll when ll.RemainingLives > 0 => FlowStateKind.RoundIntro,
                    GameOverConditionMetCommand => FlowStateKind.GameOver,
                    StageClearedCommand sc => sc.IsLastStage ? FlowStateKind.GameClear : FlowStateKind.RoundIntro,
                    _ => (FlowStateKind?)null,
                },
                FlowStateKind.GameOver => command switch
                {
                    RetryRequestedCommand => FlowStateKind.RoundIntro,
                    ReturnToTitleRequestedCommand => FlowStateKind.Title,
                    _ => (FlowStateKind?)null,
                },
                FlowStateKind.GameClear => command switch
                {
                    RetryRequestedCommand => FlowStateKind.RoundIntro,
                    ReturnToTitleRequestedCommand => FlowStateKind.Title,
                    _ => (FlowStateKind?)null,
                },
                _ => null,
            };
        }
    }
}
