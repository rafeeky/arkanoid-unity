using Arkanoid.Gameplay;

namespace Arkanoid.Flow
{
    // 비인게임 상태 (Title/GameOver/GameClear) 에서 InputSnapshot → FlowCommand.
    // Q 복귀는 모든 비-Title 상태에서 허용. 우선순위: Q → Title 난이도 커서 → Space.
    public static class FlowInputResolver
    {
        public static FlowCommand? ResolveFlowCommand(FlowStateKind state, InputSnapshot input)
        {
            // 묶음 F: Q 로 어디서든 타이틀 복귀.
            if (input.QJustPressed && state != FlowStateKind.Title)
                return new ReturnToTitleRequestedCommand();

            if (state == FlowStateKind.Title)
            {
                // 묶음 E: 방향키 edge 로 난이도 커서.
                if (input.LeftJustPressed) return new DifficultySelectNormalCommand();
                if (input.RightJustPressed) return new DifficultySelectHardCommand();
                if (input.SpaceJustPressed) return new StartGameRequestedCommand();
                return null;
            }

            if (state == FlowStateKind.GameOver && input.SpaceJustPressed)
                return new RetryRequestedCommand();
            if (state == FlowStateKind.GameClear && input.SpaceJustPressed)
                return new RetryRequestedCommand();

            return null;
        }
    }
}
