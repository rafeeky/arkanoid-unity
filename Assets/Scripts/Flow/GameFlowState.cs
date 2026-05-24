using Arkanoid.Definitions;

namespace Arkanoid.Flow
{
    public enum FlowStateKind { Title, IntroStory, RoundIntro, InGame, GameOver, GameClear }

    // Flow 의 현재 상태. D1.2 따라 class (mutable). ScreenState 의 CurrentScreen 도 동일 enum.
    public sealed class GameFlowState
    {
        public FlowStateKind Kind { get; set; }
        public int CurrentStageIndex { get; set; }
        // Title 화면에서 커서로 선택된 난이도 (묶음 E). 기본 Normal.
        public DifficultyKind SelectedDifficulty { get; set; }

        public static GameFlowState CreateInitial() =>
            new()
            {
                Kind = FlowStateKind.Title,
                CurrentStageIndex = 0,
                SelectedDifficulty = DifficultyKind.Normal,
            };
    }
}
