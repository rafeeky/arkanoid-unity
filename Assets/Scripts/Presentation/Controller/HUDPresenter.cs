using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    // InGame 상단 HUD ViewModel 생성. 규칙 계산 X, mapping 만.
    public sealed class HUDPresenter
    {
        public HudViewModel BuildHudViewModel(GameplayRuntimeState state)
        {
            var s = state.Session;
            return new HudViewModel(
                Score: s.Score,
                HighScore: s.HighScore,
                Lives: s.Lives,
                Round: s.CurrentStageIndex + 1,
                ActiveEffect: state.Bar.ActiveEffect,
                MagnetRemainingMs: state.MagnetRemainingTime,
                MagnetRemainingUses: state.MagnetRemainingUses ?? 0,
                LaserCooldownMs: state.LaserCooldownRemaining,
                LaserRemainingMs: state.LaserRemainingTime ?? 0f);
        }

        // 레거시 호환 (session 만 받음).
        public HudViewModel BuildHudViewModel(GameSessionState session)
        {
            return new HudViewModel(
                Score: session.Score,
                HighScore: session.HighScore,
                Lives: session.Lives,
                Round: session.CurrentStageIndex + 1,
                ActiveEffect: BarEffect.None,
                MagnetRemainingMs: 0f,
                MagnetRemainingUses: 0,
                LaserCooldownMs: 0f,
                LaserRemainingMs: 0f);
        }
    }
}
