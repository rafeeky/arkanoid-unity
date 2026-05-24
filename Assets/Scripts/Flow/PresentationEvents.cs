namespace Arkanoid.Flow
{
    // Presentation 측에서 발행, Flow 가 받아서 상태 전이에 사용.
    // *Flow asmdef* 에 두는 이유: Presentation 도 Flow 참조 → circular dep 회피.
    public abstract record PresentationEvent;

    public sealed record IntroSequenceFinishedEvent() : PresentationEvent;
    public sealed record RoundIntroFinishedEvent() : PresentationEvent;
    public sealed record LifeLostPresentationFinishedEvent() : PresentationEvent;
}
