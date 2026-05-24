namespace Arkanoid.Presentation
{
    // IntroStory 화면 — ScreenPresenter.BuildIntroScreenViewModel() 생성.
    // VisibleText 는 IntroTypingProgress 에 따라 slice 된 값.
    public readonly record struct IntroScreenViewModel(
        string VisibleText,
        bool IsVisible,              // done phase 면 false → 오브젝트 숨김.
        int PageIndex);              // 0..N-1, 페이지별 일러스트 키 결정용.
}
