using System.Collections.Generic;
using System.Linq;

namespace Arkanoid.Gameplay
{
    // StageOutcome — TS discriminated union → C# sealed record hierarchy.
    public abstract record StageOutcome;
    public sealed record StageOutcomeNone() : StageOutcome;
    public sealed record StageOutcomeClear() : StageOutcome;
    public sealed record StageOutcomeLifeLost(int RemainingLives) : StageOutcome;
    public sealed record StageOutcomeGameOver() : StageOutcome;

    // 한 틱의 스테이지 outcome 판정. state mutate X, event 발행 X.
    // Priority: GameOver > LifeLost > Clear > None.
    public static class StageRuleService
    {
        public static StageOutcome JudgeStageOutcome(
            GameplayRuntimeState state,
            IReadOnlyList<GameplayEvent> events)
        {
            var hasLifeLost = events.Any(e => e is LifeLostEvent);
            if (hasLifeLost)
            {
                var remainingLives = state.Session.Lives - 1;
                if (remainingLives <= 0) return new StageOutcomeGameOver();
                return new StageOutcomeLifeLost(remainingLives);
            }

            var allBlocksDestroyed = state.Blocks.Length > 0 && state.Blocks.All(b => b.IsDestroyed);
            if (allBlocksDestroyed) return new StageOutcomeClear();

            return new StageOutcomeNone();
        }
    }
}
