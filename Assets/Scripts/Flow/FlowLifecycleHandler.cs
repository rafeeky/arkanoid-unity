namespace Arkanoid.Flow
{
    // 새 상태 진입 시 발행할 FlowEvent 반환. 순수 함수.
    public static class FlowLifecycleHandler
    {
        public static FlowEvent OnEnter(FlowStateKind newState, FlowStateKind from) => newState switch
        {
            FlowStateKind.Title => new EnteredTitleEvent(from),
            FlowStateKind.IntroStory => new EnteredIntroStoryEvent(from),
            FlowStateKind.RoundIntro => new EnteredRoundIntroEvent(from),
            FlowStateKind.InGame => new EnteredInGameEvent(from),
            FlowStateKind.GameOver => new EnteredGameOverEvent(from),
            FlowStateKind.GameClear => new EnteredGameClearEvent(from),
            _ => new EnteredTitleEvent(from),
        };
    }
}
