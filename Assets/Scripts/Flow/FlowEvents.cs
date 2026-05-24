namespace Arkanoid.Flow
{
    // Flow 상태 진입 이벤트. ScreenDirector / AppContext 가 구독.
    public abstract record FlowEvent(FlowStateKind From);

    public sealed record EnteredTitleEvent(FlowStateKind From) : FlowEvent(From);
    public sealed record EnteredIntroStoryEvent(FlowStateKind From) : FlowEvent(From);
    public sealed record EnteredRoundIntroEvent(FlowStateKind From) : FlowEvent(From);
    public sealed record EnteredInGameEvent(FlowStateKind From) : FlowEvent(From);
    public sealed record EnteredGameOverEvent(FlowStateKind From) : FlowEvent(From);
    public sealed record EnteredGameClearEvent(FlowStateKind From) : FlowEvent(From);
}
