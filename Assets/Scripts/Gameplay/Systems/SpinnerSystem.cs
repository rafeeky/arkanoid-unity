using System;
using System.Collections.Generic;
using System.Linq;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public readonly record struct SpinnerBallCollisionResult(
        BallState NextBall,
        bool Collided);

    public readonly record struct SpinnerBlockCollisionResult(
        IReadOnlyList<BlockState> NextBlocks,
        IReadOnlyList<GameplayEvent> Events,
        int ScoreDelta);

    // 회전체 시스템. 3-phase 이동 (spawning→descending→circling) + 자체 회전 + 충돌.
    public sealed class SpinnerSystem
    {
        // 회전체-블록 충돌 phase 허용 오차 (rad). 약 ±5.7°.
        private const float PhaseTolerance = 0.1f;
        // 블록 대각선 반 길이 (px).
        private static readonly float BlockHalfDiag =
            MathF.Sqrt((PlayfieldLayout.BlockWidth / 2f) * (PlayfieldLayout.BlockWidth / 2f)
                     + (PlayfieldLayout.BlockHeight / 2f) * (PlayfieldLayout.BlockHeight / 2f));

        // gate 열림 연출 지속 시간 (ms).
        public const float SpawnDurationMs = 400f;
        // descending 하강 속도 (px/s).
        public const float DescentSpeedPxPerSec = 80f;
        // circling 원 궤도 회전 속도 (rad/s).
        public const float CircleSpeedRadPerSec = 1.5f;
        // door-spawned 스피너 기본 descentEndY.
        private const float DoorSpawnDescentEndY = 50f;

        private readonly IReadOnlyDictionary<string, SpinnerDefinition> _spinnerDefinitions;

        public SpinnerSystem(IReadOnlyDictionary<string, SpinnerDefinition> spinnerDefinitions)
        {
            _spinnerDefinitions = spinnerDefinitions;
        }

        // door 가 opened 로 전이된 시점에 호출. door 위치에서 descending 시작 (spawning skip).
        public SpinnerRuntimeState SpawnFromDoor(
            float doorX,
            float doorY,
            string spinnerDefinitionId,
            string uniqueId)
        {
            var spawnX = doorX + PlayfieldLayout.BorderLength / 2f;
            var descentEndY = DoorSpawnDescentEndY;
            var (centerX, centerY) = PlayfieldLayout.ClampSpinnerCenter(spawnX, descentEndY);
            return new SpinnerRuntimeState(
                Id: uniqueId,
                DefinitionId: spinnerDefinitionId,
                X: spawnX,
                Y: doorY,
                AngleRad: 0f,
                Phase: SpinnerPhase.Descending,
                SpawnElapsedMs: 0f,
                DescentEndY: descentEndY,
                CircleCenterX: centerX,
                CircleCenterY: centerY,
                CircleRadius: PlayfieldLayout.CircleRadius,
                CircleAngleRad: 0f,
                SpawnX: spawnX);
        }

        // 매 틱: 모든 spinnerStates 업데이트. 자체 회전 + phase 분기.
        public IReadOnlyList<SpinnerRuntimeState> Tick(IReadOnlyList<SpinnerRuntimeState> spinnerStates, float dt)
        {
            var result = new SpinnerRuntimeState[spinnerStates.Count];
            for (var i = 0; i < spinnerStates.Count; i++)
            {
                var s = spinnerStates[i];
                if (!_spinnerDefinitions.TryGetValue(s.DefinitionId, out var def))
                {
                    result[i] = s;
                    continue;
                }

                // 자체 회전은 모든 phase 에서 항상 업데이트.
                var newAngleRad = NormalizeAngle(s.AngleRad + def.RotationSpeedRadPerSec * dt);
                var withRotation = s with { AngleRad = newAngleRad };

                result[i] = s.Phase switch
                {
                    SpinnerPhase.Spawning => TickSpawning(withRotation, dt),
                    SpinnerPhase.Descending => TickDescending(withRotation, dt),
                    SpinnerPhase.Circling => TickCircling(withRotation, dt),
                    _ => withRotation,
                };
            }
            return result;
        }

        // 공 ↔ 회전체 충돌. phase=Circling 만 처리. broad-phase 거리 검사 + polygon swept.
        public SpinnerBallCollisionResult HandleBallCollisions(
            BallState ball,
            IReadOnlyList<SpinnerRuntimeState> spinnerStates,
            BallState? prevBall = null)
        {
            if (!ball.IsActive || spinnerStates.Count == 0)
                return new SpinnerBallCollisionResult(ball, false);

            var activeSpinners = spinnerStates.Where(s => s.Phase == SpinnerPhase.Circling).ToList();
            if (activeSpinners.Count == 0)
                return new SpinnerBallCollisionResult(ball, false);

            // broad-phase 반경 = def.Size + BallRadius + travel (선분 통과 케이스 잡기).
            var travel = prevBall.HasValue
                ? Distance(prevBall.Value.X, prevBall.Value.Y, ball.X, ball.Y)
                : 0f;

            var candidates = new List<(SpinnerRuntimeState S, SpinnerDefinition Def, float Dist)>();
            foreach (var s in activeSpinners)
            {
                if (!_spinnerDefinitions.TryGetValue(s.DefinitionId, out var def)) continue;
                var broadR = def.Size + PlayfieldLayout.BallRadius + travel;
                var d = Distance(ball.X, ball.Y, s.X, s.Y);
                var dPrev = prevBall.HasValue ? Distance(prevBall.Value.X, prevBall.Value.Y, s.X, s.Y) : float.PositiveInfinity;
                if (d < broadR || dPrev < broadR)
                {
                    candidates.Add((s, def, MathF.Min(d, dPrev)));
                }
            }
            if (candidates.Count == 0)
                return new SpinnerBallCollisionResult(ball, false);

            candidates.Sort((a, b) => a.Dist.CompareTo(b.Dist));

            // 첫 후보 polygon 정확 충돌 — 통과하면 다음 후보.
            foreach (var (s, def, _) in candidates)
            {
                var r = Spinner.HandleBallCollision(ball, s, def, prevBall);
                if (r.Collided)
                    return new SpinnerBallCollisionResult(r.NextBall, true);
            }
            return new SpinnerBallCollisionResult(ball, false);
        }

        // 회전체 ↔ 블록 phase-gate 충돌. phase=Circling 만. 같은 틱 다중 회전체 독립 처리.
        public SpinnerBlockCollisionResult HandleBlockCollisions(
            IReadOnlyList<SpinnerRuntimeState> spinnerStates,
            IReadOnlyList<BlockState> blocks,
            IReadOnlyDictionary<string, BlockDefinition> blockDefinitions)
        {
            if (spinnerStates.Count == 0 || blocks.Count == 0)
                return new SpinnerBlockCollisionResult(blocks, Array.Empty<GameplayEvent>(), 0);

            var activeSpinners = spinnerStates.Where(s => s.Phase == SpinnerPhase.Circling).ToList();
            if (activeSpinners.Count == 0)
                return new SpinnerBlockCollisionResult(blocks, Array.Empty<GameplayEvent>(), 0);

            var events = new List<GameplayEvent>();
            var totalScoreDelta = 0;
            var blockUpdates = new Dictionary<string, BlockState>();

            foreach (var s in activeSpinners)
            {
                if (!_spinnerDefinitions.TryGetValue(s.DefinitionId, out var def)) continue;
                if (!IsPhaseActive(s.AngleRad, def.BlockCollisionPhases)) continue;

                var reachDist = def.Size / 2f + BlockHalfDiag;

                foreach (var block in blocks)
                {
                    var currentBlock = blockUpdates.TryGetValue(block.Id, out var updated) ? updated : block;
                    if (currentBlock.IsDestroyed) continue;

                    var blockCenterX = currentBlock.X + PlayfieldLayout.BlockWidth / 2f;
                    var blockCenterY = currentBlock.Y + PlayfieldLayout.BlockHeight / 2f;
                    var dist = Distance(s.X, s.Y, blockCenterX, blockCenterY);
                    if (dist > reachDist) continue;

                    var spinnerRadius = def.Size / 2f;
                    if (!CircleOverlapsBlockRect(s.X, s.Y, spinnerRadius, currentBlock)) continue;

                    var newRemainingHits = currentBlock.RemainingHits - 1;
                    if (newRemainingHits <= 0)
                    {
                        blockUpdates[currentBlock.Id] = currentBlock with { RemainingHits = 0, IsDestroyed = true };
                        var scoreDelta = blockDefinitions.TryGetValue(currentBlock.DefinitionId, out var bDef) ? bDef.Score : 0;
                        totalScoreDelta += scoreDelta;
                        events.Add(new BlockDestroyedEvent(currentBlock.Id, scoreDelta));
                    }
                    else
                    {
                        blockUpdates[currentBlock.Id] = currentBlock with { RemainingHits = newRemainingHits };
                        events.Add(new BlockHitEvent(currentBlock.Id, newRemainingHits));
                    }
                }
            }

            var nextBlocks = new BlockState[blocks.Count];
            for (var i = 0; i < blocks.Count; i++)
            {
                nextBlocks[i] = blockUpdates.TryGetValue(blocks[i].Id, out var u) ? u : blocks[i];
            }
            return new SpinnerBlockCollisionResult(nextBlocks, events, totalScoreDelta);
        }

        // ─── Phase tick helpers ───

        private SpinnerRuntimeState TickSpawning(SpinnerRuntimeState s, float dt)
        {
            var newElapsedMs = s.SpawnElapsedMs + dt * 1000f;
            if (newElapsedMs >= SpawnDurationMs)
            {
                return s with
                {
                    Phase = SpinnerPhase.Descending,
                    SpawnElapsedMs = SpawnDurationMs,
                    X = s.SpawnX,
                    Y = 0f,
                };
            }
            return s with
            {
                SpawnElapsedMs = newElapsedMs,
                X = s.SpawnX,
                Y = 0f,
            };
        }

        private SpinnerRuntimeState TickDescending(SpinnerRuntimeState s, float dt)
        {
            var newY = s.Y + DescentSpeedPxPerSec * dt;
            // descent 종료점 = orbit top — circling 첫 프레임 y 점프 방지.
            var orbitTopY = s.CircleCenterY - s.CircleRadius;
            if (newY >= orbitTopY)
            {
                return s with
                {
                    Phase = SpinnerPhase.Circling,
                    X = s.SpawnX,
                    Y = orbitTopY,
                    CircleAngleRad = -MathF.PI / 2f,  // 궤도 상단(위쪽) 시작
                };
            }
            return s with { X = s.SpawnX, Y = newY };
        }

        private SpinnerRuntimeState TickCircling(SpinnerRuntimeState s, float dt)
        {
            var newCircleAngle = s.CircleAngleRad + CircleSpeedRadPerSec * dt;
            var newX = s.CircleCenterX + s.CircleRadius * MathF.Cos(newCircleAngle);
            var newY = s.CircleCenterY + s.CircleRadius * MathF.Sin(newCircleAngle);
            return s with { CircleAngleRad = newCircleAngle, X = newX, Y = newY };
        }

        private static bool IsPhaseActive(float angleRad, IReadOnlyList<float> phases)
        {
            var normalized = NormalizeAngle(angleRad);
            foreach (var phase in phases)
            {
                var normalizedPhase = NormalizeAngle(phase);
                var diff = AngleDiff(normalized, normalizedPhase);
                if (diff <= PhaseTolerance) return true;
            }
            return false;
        }

        // ─── Pure helpers ───

        public static float NormalizeAngle(float rad)
        {
            var twoPi = 2f * MathF.PI;
            return ((rad % twoPi) + twoPi) % twoPi;
        }

        private static float AngleDiff(float a, float b)
        {
            var diff = MathF.Abs(NormalizeAngle(a - b));
            return diff > MathF.PI ? 2f * MathF.PI - diff : diff;
        }

        private static float Distance(float x1, float y1, float x2, float y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        // 원 vs Block AABB 겹침 검사. block.X/Y 좌상단 + BlockWidth/Height 사용.
        private static bool CircleOverlapsBlockRect(float cx, float cy, float radius, BlockState block)
        {
            var blockLeft = block.X;
            var blockRight = block.X + PlayfieldLayout.BlockWidth;
            var blockTop = block.Y;
            var blockBottom = block.Y + PlayfieldLayout.BlockHeight;
            var nearestX = MathF.Max(blockLeft, MathF.Min(cx, blockRight));
            var nearestY = MathF.Max(blockTop, MathF.Min(cy, blockBottom));
            var dx = cx - nearestX;
            var dy = cy - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
