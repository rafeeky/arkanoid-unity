using Arkanoid.Definitions;

namespace Arkanoid.Presentation
{
    public readonly record struct TitleScreenViewModel(
        string StartText,
        int HighScore,
        DifficultyKind SelectedDifficulty);
}
