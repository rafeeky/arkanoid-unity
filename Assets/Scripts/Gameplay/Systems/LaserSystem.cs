using System;
using System.Collections.Generic;
using System.Linq;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public readonly record struct LaserFireResult(
        IReadOnlyList<LaserShotState> NewShots,
        float NextCooldownMs,
        IReadOnlyList<GameplayEvent> Events);

    public readonly record struct LaserTickResult(
        IReadOnlyList<LaserShotState> NextShots,
        float NextCooldownMs);

    public readonly record struct LaserCollisionResult(
        IReadOnlyList<LaserShotState> NextShots,
        IReadOnlyList<BlockState> NextBlocks,
        IReadOnlyList<GameplayEvent> Events,
        IReadOnlyList<string> DestroyedBlockIds,
        int ScoreDelta);

    // 레이저 효과 전담 시스템. 발사 / 매 틱 이동 / 블록 충돌.
    public sealed class LaserSystem
    {
        // 발사체 AABB 반너비/반높이 (px).
        private const float LaserHalfW = 1f;
        private const float LaserHalfH = 4f;
        // 바 두께 절반 (발사 위치 계산).
        private const float BarHalfHeight = 8f;
        // 레이저 기본 수직 속도 (px/s). TS Y+ 아래 → 음수 = 위쪽.
        private const float LaserVy = -1200f;
        // 레이저 기본 쿨다운 (ms).
        private const float DefaultLaserCooldownMs = 400f;

        private readonly Func<string> _nextId;

        public LaserSystem(Func<string> nextId)
        {
            _nextId = nextId;
        }

        // FireLaser 커맨드: 바 좌우 2발 + 쿨다운 설정 + LaserFired 이벤트.
        public LaserFireResult FireLaser(
            BarState currentBar,
            IReadOnlyList<LaserShotState> currentShots,
            float? laserCooldownMs)
        {
            var spawnY = currentBar.Y - BarHalfHeight;
            var offsetX = currentBar.Width / 3f;

            var shot1 = new LaserShotState(_nextId(), currentBar.X - offsetX, spawnY, LaserVy);
            var shot2 = new LaserShotState(_nextId(), currentBar.X + offsetX, spawnY, LaserVy);
            var cooldown = laserCooldownMs ?? DefaultLaserCooldownMs;

            var newShots = new LaserShotState[currentShots.Count + 2];
            for (var i = 0; i < currentShots.Count; i++) newShots[i] = currentShots[i];
            newShots[currentShots.Count] = shot1;
            newShots[currentShots.Count + 1] = shot2;

            return new LaserFireResult(
                NewShots: newShots,
                NextCooldownMs: cooldown,
                Events: new GameplayEvent[] { new LaserFiredEvent(2) });
        }

        // 매 틱: 발사체 이동 + 천장 통과 제거 + 쿨다운 감소.
        public LaserTickResult Tick(
            IReadOnlyList<LaserShotState> shots,
            float cooldownMs,
            float dt)
        {
            var dtMs = dt * 1000f;
            var nextShots = new List<LaserShotState>(shots.Count);
            foreach (var s in shots)
            {
                var moved = s with { Y = s.Y + s.Vy * dt };
                // 천장 통과 (y < 0): 발사체 상단 (y - LaserHalfH) 이 0 미만이면 제거
                if (moved.Y - LaserHalfH >= 0f) nextShots.Add(moved);
            }
            var nextCooldown = MathF.Max(0f, cooldownMs - dtMs);
            return new LaserTickResult(nextShots, nextCooldown);
        }

        // 레이저 ↔ 블록 AABB 충돌. 첫 hit 에서 shot 소멸 (관통 X). BlockHit/BlockDestroyed 발행.
        public LaserCollisionResult HandleBlockCollisions(
            IReadOnlyList<LaserShotState> shots,
            IReadOnlyList<BlockState> blocks,
            IReadOnlyDictionary<string, BlockDefinition> blockDefinitions)
        {
            var events = new List<GameplayEvent>();
            var destroyedBlockIds = new List<string>();
            var totalScoreDelta = 0;

            // 같은 틱 안의 블록 변경 추적.
            var blockUpdates = new Dictionary<string, BlockState>();
            var removedShotIds = new HashSet<string>();

            foreach (var shot in shots)
            {
                if (removedShotIds.Contains(shot.Id)) continue;

                BlockState? hitBlock = null;
                foreach (var block in blocks)
                {
                    var currentBlock = blockUpdates.TryGetValue(block.Id, out var updated) ? updated : block;
                    if (currentBlock.IsDestroyed) continue;
                    if (ShotOverlapsBlock(shot, currentBlock))
                    {
                        hitBlock = currentBlock;
                        break;
                    }
                }

                if (hitBlock is not { } hit) continue;

                removedShotIds.Add(shot.Id);

                var newRemainingHits = hit.RemainingHits - 1;
                if (newRemainingHits <= 0)
                {
                    blockUpdates[hit.Id] = hit with { RemainingHits = 0, IsDestroyed = true };
                    destroyedBlockIds.Add(hit.Id);

                    var scoreDelta = blockDefinitions.TryGetValue(hit.DefinitionId, out var def) ? def.Score : 0;
                    totalScoreDelta += scoreDelta;
                    events.Add(new BlockDestroyedEvent(hit.Id, scoreDelta));
                }
                else
                {
                    blockUpdates[hit.Id] = hit with { RemainingHits = newRemainingHits };
                    events.Add(new BlockHitEvent(hit.Id, newRemainingHits));
                }
            }

            var nextShots = shots.Where(s => !removedShotIds.Contains(s.Id)).ToArray();
            var nextBlocks = new BlockState[blocks.Count];
            for (var i = 0; i < blocks.Count; i++)
            {
                nextBlocks[i] = blockUpdates.TryGetValue(blocks[i].Id, out var updated) ? updated : blocks[i];
            }

            return new LaserCollisionResult(nextShots, nextBlocks, events, destroyedBlockIds, totalScoreDelta);
        }

        private static bool ShotOverlapsBlock(LaserShotState shot, BlockState block)
        {
            var shotLeft = shot.X - LaserHalfW;
            var shotRight = shot.X + LaserHalfW;
            var shotTop = shot.Y - LaserHalfH;
            var shotBottom = shot.Y + LaserHalfH;

            var blockLeft = block.X;
            var blockRight = block.X + PlayfieldLayout.BlockWidth;
            var blockTop = block.Y;
            var blockBottom = block.Y + PlayfieldLayout.BlockHeight;

            return shotLeft < blockRight && shotRight > blockLeft
                && shotTop < blockBottom && shotBottom > blockTop;
        }
    }
}
