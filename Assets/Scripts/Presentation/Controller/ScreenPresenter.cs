using System;
using System.Collections.Generic;
using Arkanoid.Definitions;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    // flowState + gameplayState → ViewModel 변환. 규칙 계산 X, 읽기 전용 매퍼.
    public sealed class ScreenPresenter
    {
        private static readonly string[] RoundTextIds = { "txt_round_01", "txt_round_02", "txt_round_03" };

        public TitleScreenViewModel BuildTitleViewModel(
            GameSessionState session,
            IReadOnlyList<UITextEntry> uiTexts,
            DifficultyKind selectedDifficulty = DifficultyKind.Normal)
        {
            return new TitleScreenViewModel(
                StartText: LookupText(uiTexts, "txt_title_start"),
                HighScore: session.HighScore,
                SelectedDifficulty: selectedDifficulty);
        }

        public RoundIntroViewModel BuildRoundIntroViewModel(
            GameSessionState session,
            IReadOnlyList<UITextEntry> uiTexts,
            float roundIntroRemainingTime = 0f,
            float roundIntroDurationMs = 1500f)
        {
            var idx = session.CurrentStageIndex >= 0 && session.CurrentStageIndex < RoundTextIds.Length
                ? session.CurrentStageIndex : 0;
            var roundLabel = LookupText(uiTexts, RoundTextIds[idx]);
            var readyLabel = LookupText(uiTexts, "txt_ready");

            var elapsed = roundIntroDurationMs - roundIntroRemainingTime;
            var introProgress = roundIntroDurationMs > 0f
                ? Math.Max(0f, Math.Min(1f, elapsed / roundIntroDurationMs))
                : 1f;
            return new RoundIntroViewModel(roundLabel, readyLabel, introProgress);
        }

        public GameOverViewModel BuildGameOverViewModel(GameSessionState session, IReadOnlyList<UITextEntry> uiTexts)
        {
            var gameOverLabel = LookupText(uiTexts, "txt_gameover");
            var finalTemplate = LookupText(uiTexts, "txt_gameover_final_score");
            var highTemplate = LookupText(uiTexts, "txt_title_highscore");
            var retryText = LookupText(uiTexts, "txt_retry");

            var finalScoreLabel = finalTemplate.Replace("{0}", session.Score.ToString());
            var highScoreLabel = highTemplate.Replace("{0}", session.HighScore.ToString());
            var isNewHighScore = session.Score > 0 && session.Score >= session.HighScore;

            return new GameOverViewModel(gameOverLabel, finalScoreLabel, highScoreLabel, retryText, isNewHighScore);
        }

        public IntroScreenViewModel BuildIntroScreenViewModel(
            int introPageIndex,
            float introTypingProgress,
            IntroPhase introPhase,
            IReadOnlyList<IntroSequenceEntry> introPages)
        {
            if (introPhase == IntroPhase.Done)
                return new IntroScreenViewModel("", false, introPageIndex);

            if (introPageIndex < 0 || introPageIndex >= introPages.Count)
                return new IntroScreenViewModel("", false, introPageIndex);

            var text = introPages[introPageIndex].Text;
            string visibleText = introPhase switch
            {
                IntroPhase.Typing => text.Substring(0, (int)Math.Floor(introTypingProgress * text.Length)),
                IntroPhase.Hold => text,
                IntroPhase.Erasing => text.Substring(0, (int)Math.Floor(introTypingProgress * text.Length)),
                _ => "",
            };

            return new IntroScreenViewModel(visibleText, true, introPageIndex);
        }

        public GameClearViewModel BuildGameClearViewModel(GameSessionState session, IReadOnlyList<UITextEntry> uiTexts)
        {
            var headline = LookupText(uiTexts, "txt_gameclear");
            var finalTemplate = LookupText(uiTexts, "txt_gameclear_final_score");
            var highTemplate = LookupText(uiTexts, "txt_title_highscore");
            var retryText = LookupText(uiTexts, "txt_gameclear_retry");

            var finalScoreLabel = finalTemplate.Replace("{0}", session.Score.ToString());
            var highScoreLabel = highTemplate.Replace("{0}", session.HighScore.ToString());
            var isNewHighScore = session.Score > 0 && session.Score >= session.HighScore;

            return new GameClearViewModel(headline, finalScoreLabel, highScoreLabel, retryText, isNewHighScore);
        }

        private static string LookupText(IReadOnlyList<UITextEntry> uiTexts, string textId)
        {
            foreach (var e in uiTexts)
                if (e.TextId == textId) return e.Value;
            return textId;  // fallback
        }
    }
}
