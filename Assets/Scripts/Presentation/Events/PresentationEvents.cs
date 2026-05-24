namespace Arkanoid.Presentation
{
    public abstract record PresentationEvent;

    public sealed record IntroSequenceFinishedEvent() : PresentationEvent;
    public sealed record RoundIntroFinishedEvent() : PresentationEvent;
    public sealed record LifeLostPresentationFinishedEvent() : PresentationEvent;
}
