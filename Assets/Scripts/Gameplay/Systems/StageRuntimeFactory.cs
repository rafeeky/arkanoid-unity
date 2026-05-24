using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public static class StageRuntimeFactory
    {
        // StageDefinition → 초기 GameplayRuntimeState. 외부 상태 read X.
        // Difficulty (묶음 E):
        //   - 주어지면 InitialLives 가 인자 override.
        //   - SpinnersEnabled=false 면 스피너 목록 비움 (NORMAL).
        //   - 미지정 시 기본 동작.
        public static GameplayRuntimeState CreateGameplayRuntimeFromStageDefinition(
            StageDefinition def,
            GameplayConfig config,
            IReadOnlyDictionary<string, BlockDefinition> blockDefinitions,
            int initialLives,
            DifficultyConfig? difficulty = null)
        {
            var blocks = new BlockState[def.Blocks.Count];
            for (var i = 0; i < def.Blocks.Count; i++)
            {
                var p = def.Blocks[i];
                var x = PlayfieldLayout.BlockGridLeftMargin + p.Col * (PlayfieldLayout.BlockWidth + PlayfieldLayout.BlockGap);
                var y = PlayfieldLayout.BlockGridStartY + p.Row * (PlayfieldLayout.BlockHeight + PlayfieldLayout.BlockGap);
                var maxHits = blockDefinitions.TryGetValue(p.DefinitionId, out var bDef) ? bDef.MaxHits : 1;
                blocks[i] = new BlockState(
                    Id: $"block_{i}",
                    X: x,
                    Y: y,
                    RemainingHits: maxHits,
                    IsDestroyed: false,
                    DefinitionId: p.DefinitionId);
            }

            var borders = BuildBorderStates(def);
            var doors = BuildDoorStates(def);

            // 난이도 적용: SpinnersEnabled=false → 스피너 목록 비움. InitialLives 우선.
            var spinnerStates = (difficulty.HasValue && !difficulty.Value.SpinnersEnabled)
                ? System.Array.Empty<SpinnerRuntimeState>()
                : BuildSpinnerStates(def);
            var effectiveLives = difficulty?.InitialLives ?? initialLives;

            return new GameplayRuntimeState
            {
                Session = new GameSessionState(
                    CurrentStageIndex: 0,
                    Score: 0,
                    Lives: effectiveLives,
                    HighScore: 0),
                Bar = new BarState(
                    X: def.BarSpawnX,
                    Y: def.BarSpawnY,
                    Width: config.BaseBarWidth,
                    MoveSpeed: config.BarMoveSpeed,
                    ActiveEffect: BarEffect.None),
                Balls = new[]
                {
                    new BallState(
                        Id: "ball_0",
                        // 발사각 -60° 와 시각 일치 — 바 중심에서 우측 30px.
                        X: def.BarSpawnX + PlayfieldLayout.InitialLaunchOffsetX,
                        Y: def.BarSpawnY - PlayfieldLayout.BarHeight,
                        Vx: 0f,
                        Vy: 0f,
                        IsActive: false,
                        // 새 공은 항상 파워 없이 시작.
                        BlocksSincePaddle: 0,
                        IsPowered: false),
                },
                Blocks = blocks,
                Borders = borders,
                Doors = doors,
                ItemDrops = System.Array.Empty<ItemDropState>(),
                IsStageCleared = false,
                MagnetRemainingTime = 0f,
                AttachedBallIds = System.Array.Empty<string>(),
                LaserCooldownRemaining = 0f,
                LaserShots = System.Array.Empty<LaserShotState>(),
                SpinnerStates = spinnerStates,
                CurrentTrailStyle = def.TrailStyle ?? "golden_sun",
            };
        }

        // StageDefinition.Doors → DoorState 배열 (모두 Closed). 위치: 상단 테두리 라인 y=0, col 단위로 BorderLength.
        private static IReadOnlyList<DoorState> BuildDoorStates(StageDefinition def)
        {
            if (def.Doors == null || def.Doors.Count == 0) return System.Array.Empty<DoorState>();
            var result = new DoorState[def.Doors.Count];
            for (var i = 0; i < def.Doors.Count; i++)
            {
                var p = def.Doors[i];
                result[i] = new DoorState(
                    Id: $"door_{i}",
                    X: p.Col * PlayfieldLayout.BorderLength,
                    Y: 0f,
                    Phase: DoorPhase.Closed,
                    OpeningElapsedMs: 0f,
                    SpinnerDefinitionId: p.SpinnerDefinitionId,
                    SpawnedSpinnerId: null);
            }
            return result;
        }

        // StageDefinition.Borders → BorderBlockState 배열. orientation 따라 좌표 계산.
        private static IReadOnlyList<BorderBlockState> BuildBorderStates(StageDefinition def)
        {
            if (def.Borders == null || def.Borders.Count == 0) return System.Array.Empty<BorderBlockState>();
            var result = new BorderBlockState[def.Borders.Count];
            for (var i = 0; i < def.Borders.Count; i++)
            {
                var p = def.Borders[i];
                float x, y;
                if (p.Orientation == BorderOrientation.Horizontal)
                {
                    x = p.Col * PlayfieldLayout.BorderLength;
                    y = p.Row * PlayfieldLayout.BorderThickness;
                }
                else
                {
                    // vertical: col=0 → left edge, col=1 → right edge
                    x = p.Col == 0 ? 0f : PlayfieldLayout.PlayfieldWidth - PlayfieldLayout.BorderThickness;
                    y = p.Row * PlayfieldLayout.BorderLength;
                }
                result[i] = new BorderBlockState(
                    Id: $"border_{i}",
                    X: x,
                    Y: y,
                    Orientation: p.Orientation);
            }
            return result;
        }

        private static IReadOnlyList<SpinnerRuntimeState> BuildSpinnerStates(StageDefinition def)
        {
            if (def.Spinners == null || def.Spinners.Count == 0) return System.Array.Empty<SpinnerRuntimeState>();
            var result = new SpinnerRuntimeState[def.Spinners.Count];
            for (var i = 0; i < def.Spinners.Count; i++)
            {
                var p = def.Spinners[i];
                var spawnX = p.X;
                var descentEndY = p.Y;
                var (centerX, centerY) = PlayfieldLayout.ClampSpinnerCenter(spawnX, descentEndY);

                result[i] = new SpinnerRuntimeState(
                    Id: $"spinner_{i}",
                    DefinitionId: p.DefinitionId,
                    X: spawnX,
                    Y: 0f,
                    AngleRad: p.InitialAngleRad ?? 0f,
                    Phase: SpinnerPhase.Spawning,
                    SpawnElapsedMs: 0f,
                    DescentEndY: descentEndY,
                    CircleCenterX: centerX,
                    CircleCenterY: centerY,
                    CircleRadius: PlayfieldLayout.CircleRadius,
                    CircleAngleRad: 0f,
                    SpawnX: spawnX);
            }
            return result;
        }
    }
}
