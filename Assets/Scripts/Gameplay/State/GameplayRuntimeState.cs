using System;
using System.Collections.Generic;

namespace Arkanoid.Gameplay
{
    // 한 스테이지의 런타임 전체 상태. D1.2 결정에 따라 class (mutable, 참조 의미).
    public sealed class GameplayRuntimeState
    {
        public GameSessionState Session { get; set; }
        public BarState Bar { get; set; }
        public BallState[] Balls { get; set; } = Array.Empty<BallState>();
        public BlockState[] Blocks { get; set; } = Array.Empty<BlockState>();

        // 플레이필드 테두리 (좌/우/상단). 깨지지 않는 벽.
        public IReadOnlyList<BorderBlockState> Borders { get; set; } = Array.Empty<BorderBlockState>();

        // 상단 테두리 위 문(door). 열리면 스피너 spawn.
        public IReadOnlyList<DoorState> Doors { get; set; } = Array.Empty<DoorState>();

        public ItemDropState[] ItemDrops { get; set; } = Array.Empty<ItemDropState>();
        public bool IsStageCleared { get; set; }

        // [deprecated] 자석 효과 남은 시간 (ms). 호환용 유지 — 새 동작은 MagnetRemainingUses.
        public float MagnetRemainingTime { get; set; }

        // 자석 효과 남은 부착 횟수 (5회). null → 0.
        public int? MagnetRemainingUses { get; set; }

        // 자석 상태에서 바에 붙은 공 ID 목록.
        public IReadOnlyList<string> AttachedBallIds { get; set; } = Array.Empty<string>();

        // 레이저 다음 발사까지 남은 쿨다운 (ms). 0이면 즉시 발사 가능.
        public float LaserCooldownRemaining { get; set; }

        // 레이저 효과 남은 지속 시간 (ms). null → 0.
        public float? LaserRemainingTime { get; set; }

        // 화면에 존재하는 레이저 발사체 목록.
        public IReadOnlyList<LaserShotState> LaserShots { get; set; } = Array.Empty<LaserShotState>();

        // 현재 스테이지의 회전체 런타임 상태 목록.
        public IReadOnlyList<SpinnerRuntimeState> SpinnerStates { get; set; } = Array.Empty<SpinnerRuntimeState>();

        // 현재 스테이지의 공 파워 트레일 스타일. StageRuntimeFactory 에서 세팅.
        // Phase 2 에 정식 TrailStyleId enum/struct 로 변환 예정 (지금은 string).
        public string? CurrentTrailStyle { get; set; }
    }
}
