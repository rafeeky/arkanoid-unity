using System.Collections.Generic;
using System.Linq;

namespace Arkanoid.Gameplay
{
    // GameplayCommand union — TS discriminated union → C# sealed record hierarchy.
    public abstract record GameplayCommand;
    public sealed record MoveBarCommand(int Direction) : GameplayCommand;  // -1, 0, 1
    public sealed record SetBarTargetXCommand(float X) : GameplayCommand;  // 슬라이더 드래그 — 절대 x 스냅
    public sealed record LaunchBallCommand() : GameplayCommand;
    public sealed record ReleaseAttachedBallsCommand() : GameplayCommand;
    public sealed record FireLaserCommand() : GameplayCommand;

    public static class InputCommandResolver
    {
        public static IReadOnlyList<GameplayCommand> ResolveGameplayCommands(
            InputSnapshot input,
            GameplayRuntimeState state)
        {
            var commands = new List<GameplayCommand>();

            // 포인터 드래그 활성 → 절대 위치 스냅. 키보드 좌우 무시 (드래그 우선).
            if (input.TargetBarX.HasValue)
            {
                commands.Add(new SetBarTargetXCommand(input.TargetBarX.Value));
            }
            else
            {
                var direction = 0;
                if (input.LeftDown && !input.RightDown) direction = -1;
                else if (input.RightDown && !input.LeftDown) direction = 1;
                commands.Add(new MoveBarCommand(direction));
            }

            if (input.SpaceJustPressed)
            {
                // 우선순위 1: 자석 + 부착 공 → 해제
                if (state.Bar.ActiveEffect == BarEffect.Magnet && state.AttachedBallIds.Count > 0)
                {
                    commands.Add(new ReleaseAttachedBallsCommand());
                }
                // 우선순위 2: 비활성 공 있음 → 발사
                else if (state.Balls.Any(b => !b.IsActive))
                {
                    commands.Add(new LaunchBallCommand());
                }
                // (레이저는 자동 발사 — GameplayController.Tick 에서 매 틱 쿨타임 0 시 fire)
            }

            return commands;
        }
    }
}
