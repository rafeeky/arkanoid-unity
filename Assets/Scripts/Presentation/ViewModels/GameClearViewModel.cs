namespace Arkanoid.Presentation
{
    public readonly record struct GameClearViewModel(
        string Headline,             // "CONGRATULATIONS"
        string FinalScoreLabel,      // "FINAL SCORE 1234"
        string HighScoreLabel,       // "HIGH SCORE 5678"
        string RetryText,            // "PRESS SPACE TO RETRY"
        bool IsNewHighScore);
}
