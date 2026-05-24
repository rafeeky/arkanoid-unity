namespace Arkanoid.Presentation
{
    public readonly record struct RoundIntroViewModel(
        string RoundLabel,
        string ReadyLabel,
        // 0.0 (연출 시작) ~ 1.0 (연출 종료). fade-in/out 용.
        float IntroProgress);
}
