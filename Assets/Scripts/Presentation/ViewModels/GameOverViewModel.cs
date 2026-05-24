namespace Arkanoid.Presentation
{
    // GameOver 화면 — ScreenPresenter.BuildGameOverViewModel() 생성.
    public readonly record struct GameOverViewModel(
        string GameOverLabel,        // "GAME OVER"
        string FinalScoreLabel,      // "FINAL SCORE 1234"
        string HighScoreLabel,       // "HIGH SCORE 5678"
        string RetryText,            // "PRESS SPACE TO RETRY"
        bool IsNewHighScore);        // 하이라이트 판단
}
