using System;
using System.Collections.Generic;
using System.Linq;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public readonly record struct ResolutionTables(
        IReadOnlyDictionary<string, BlockDefinition> BlockDefinitions,
        IReadOnlyDictionary<ItemType, ItemDefinition> ItemDefinitions,
        GameplayConfig Config);

    public readonly record struct ApplyOptions(bool BlockReflectionAlreadyApplied = false);

    public readonly record struct ApplyResult(
        GameplayRuntimeState NextState,
        IReadOnlyList<GameplayEvent> Events);

    // 충돌 fact → 응답 적용. 반사 로직 자체는 entity 모듈 소유 (Wall/Bar/Block.ReflectFromBall).
    // 이 파일은 fact → entity reflect 호출 → state mutate (D1.2: GameplayRuntimeState 는 class, mutable).
    public static class CollisionResolutionService
    {
        // 속도 벡터가 수평/수직축에서 minAngleDeg 보다 가까워지지 않게 강제. magnitude 보존.
        // 무한 핑퐁 (수평/수직 트래핑) 방지. MinAngleDeg 는 PhysicsConfig 의 data-driven 값.
        public static (float Vx, float Vy) EnforceMinAngle(float vx, float vy, float minAngleDeg)
        {
            var speed = MathF.Sqrt(vx * vx + vy * vy);
            if (speed == 0f) return (vx, vy);

            var minSin = MathF.Sin(minAngleDeg * MathF.PI / 180f);
            var minComponent = speed * minSin;
            var newVx = vx;
            var newVy = vy;

            if (MathF.Abs(newVx) < minComponent)
            {
                newVx = minComponent * (newVx >= 0f ? 1f : -1f);
                var vySign = newVy >= 0f ? 1f : -1f;
                newVy = vySign * MathF.Sqrt(MathF.Max(0f, speed * speed - newVx * newVx));
            }

            if (MathF.Abs(newVy) < minComponent)
            {
                newVy = minComponent * (newVy >= 0f ? 1f : -1f);
                var vxSign = newVx >= 0f ? 1f : -1f;
                newVx = vxSign * MathF.Sqrt(MathF.Max(0f, speed * speed - newVy * newVy));
            }

            return (newVx, newVy);
        }

        // ─── Resolve helpers — state mutate, events 반환 ───

        private static IReadOnlyList<GameplayEvent> ResolveWall(
            GameplayRuntimeState state, BallHitWallFact fact, PhysicsConfig physics)
        {
            for (var i = 0; i < state.Balls.Length; i++)
            {
                if (state.Balls[i].Id == fact.BallId)
                    state.Balls[i] = Wall.ReflectFromBall(state.Balls[i], fact, physics);
            }
            return Array.Empty<GameplayEvent>();
        }

        private static IReadOnlyList<GameplayEvent> ResolveBar(
            GameplayRuntimeState state, BallHitBarFact fact, PhysicsConfig physics)
        {
            // 자석 상태 + 활성 공 → 반사 대신 부착.
            if (state.Bar.ActiveEffect == BarEffect.Magnet)
            {
                for (var i = 0; i < state.Balls.Length; i++)
                {
                    if (state.Balls[i].Id != fact.BallId) continue;
                    var target = state.Balls[i];
                    if (!target.IsActive) continue;

                    var attached = Bar.AttachBall(target, state.Bar);
                    // 자석 부착도 paddle 접촉 — 파워 상태 reset.
                    state.Balls[i] = attached with { BlocksSincePaddle = 0, IsPowered = false };

                    var newAttachedIds = state.AttachedBallIds.Concat(new[] { fact.BallId }).ToArray();
                    state.AttachedBallIds = newAttachedIds;

                    return new GameplayEvent[]
                    {
                        new BallAttachedEvent(new[] { fact.BallId }),
                    };
                }
            }

            // 일반 상태: 반사 + 파워 상태 reset.
            for (var i = 0; i < state.Balls.Length; i++)
            {
                if (state.Balls[i].Id != fact.BallId) continue;
                var reflected = Bar.ReflectFromBall(state.Balls[i], fact, physics);
                state.Balls[i] = reflected with { BlocksSincePaddle = 0, IsPowered = false };
            }
            return new GameplayEvent[] { new BallHitBarEvent(fact.BallId) };
        }

        private static IReadOnlyList<GameplayEvent> ResolveBlock(
            GameplayRuntimeState state, BallHitBlockFact fact, ResolutionTables tables, bool skipBallReflection)
        {
            var events = new List<GameplayEvent>();

            // Reflect ball (swept movement 가 이미 반사 적용했으면 skip).
            if (!skipBallReflection)
            {
                for (var i = 0; i < state.Balls.Length; i++)
                {
                    if (state.Balls[i].Id == fact.BallId)
                        state.Balls[i] = Block.ReflectFromBall(state.Balls[i], fact.Side, tables.Config.Physics);
                }
            }

            // Find block.
            var blockIndex = -1;
            for (var i = 0; i < state.Blocks.Length; i++)
            {
                if (state.Blocks[i].Id == fact.BlockId) { blockIndex = i; break; }
            }
            if (blockIndex == -1) return events;

            var block = state.Blocks[blockIndex];
            var newRemainingHits = block.RemainingHits - 1;

            if (newRemainingHits <= 0)
            {
                // Block destroyed.
                tables.BlockDefinitions.TryGetValue(block.DefinitionId, out var def);
                var scoreDelta = def.DefinitionId != null ? def.Score : 0;
                state.Session = state.Session with { Score = state.Session.Score + scoreDelta };

                state.Blocks[blockIndex] = block with { RemainingHits = 0, IsDestroyed = true };
                events.Add(new BlockDestroyedEvent(block.Id, scoreDelta));

                // 공 파워 상태 — destroyed 1회당 BlocksSincePaddle += 1.
                // 2 이상 도달 시 IsPowered=true (시각 트레일만, 게임플레이 영향 X).
                const int powerThreshold = 2;
                for (var i = 0; i < state.Balls.Length; i++)
                {
                    if (state.Balls[i].Id != fact.BallId) continue;
                    var newCount = (state.Balls[i].BlocksSincePaddle ?? 0) + 1;
                    state.Balls[i] = state.Balls[i] with
                    {
                        BlocksSincePaddle = newCount,
                        IsPowered = newCount >= powerThreshold,
                    };
                }

                // Item drop — only if no item currently on screen.
                if (def.DropItemType is { } dropType && state.ItemDrops.Length == 0)
                {
                    var fallSpeed = tables.ItemDefinitions.TryGetValue(dropType, out var itemDef) ? itemDef.FallSpeed : 160f;
                    var newItem = new ItemDropState(
                        Id: $"item_{block.Id}",
                        ItemType: dropType,
                        X: block.X + 32f,  // center of 64px
                        Y: block.Y + 12f,  // center of 24px
                        FallSpeed: fallSpeed,
                        IsCollected: false);
                    var newDrops = new ItemDropState[state.ItemDrops.Length + 1];
                    Array.Copy(state.ItemDrops, newDrops, state.ItemDrops.Length);
                    newDrops[state.ItemDrops.Length] = newItem;
                    state.ItemDrops = newDrops;
                    events.Add(new ItemSpawnedEvent(newItem.Id, dropType, newItem.X, newItem.Y));
                }
            }
            else
            {
                state.Blocks[blockIndex] = block with { RemainingHits = newRemainingHits };
                events.Add(new BlockHitEvent(block.Id, newRemainingHits));
            }

            return events;
        }

        private static IReadOnlyList<GameplayEvent> ResolveFloor(GameplayRuntimeState state, BallHitFloorFact fact)
        {
            for (var i = 0; i < state.Balls.Length; i++)
            {
                if (state.Balls[i].Id == fact.BallId)
                    state.Balls[i] = state.Balls[i] with { IsActive = false };
            }
            // RemainingLives 는 GameplayController 가 StageRuleService 후에 채움.
            return new GameplayEvent[] { new LifeLostEvent(0) };
        }

        private static IReadOnlyList<GameplayEvent> ResolveItemPickedUp(
            GameplayRuntimeState state, ItemPickedUpFact fact, ResolutionTables tables)
        {
            ItemDropState? itemOpt = null;
            foreach (var i in state.ItemDrops)
            {
                if (i.Id == fact.ItemId) { itemOpt = i; break; }
            }
            if (itemOpt is not { } item) return Array.Empty<GameplayEvent>();

            // Remove collected item.
            state.ItemDrops = state.ItemDrops.Where(i => i.Id != fact.ItemId).ToArray();

            var replacedEffect = state.Bar.ActiveEffect;
            var itemType = item.ItemType;

            var barEffectService = new BarEffectService(tables.ItemDefinitions);
            var effectResult = barEffectService.ApplyEffect(
                state.Bar,
                state.MagnetRemainingTime,
                state.LaserCooldownRemaining,
                state.AttachedBallIds,
                itemType,
                tables.Config.BaseBarWidth);

            var events = new List<GameplayEvent>(effectResult.Events)
            {
                new ItemCollectedEvent(itemType, replacedEffect, effectResult.NextBar.ActiveEffect),
            };

            // magnet 5회 / laser 6초 — 새 필드 초기화 (ItemDefinition 우선, 미설정 시 기본값).
            var newEffect = effectResult.NextBar.ActiveEffect;
            var hasDef = tables.ItemDefinitions.TryGetValue(itemType, out var itemDef);
            var magnetUses = newEffect == BarEffect.Magnet
                ? (hasDef && itemDef.MagnetUseCount.HasValue ? itemDef.MagnetUseCount.Value : 5)
                : 0;
            var laserTime = newEffect == BarEffect.Laser
                ? (hasDef && itemDef.LaserDurationMs.HasValue ? itemDef.LaserDurationMs.Value : 6000f)
                : 0f;

            state.Bar = effectResult.NextBar;
            state.MagnetRemainingTime = effectResult.NextMagnetRemaining;
            state.MagnetRemainingUses = magnetUses;
            state.LaserCooldownRemaining = effectResult.NextLaserCooldown;
            state.LaserRemainingTime = laserTime;
            if (effectResult.ClearLaserShots)
                state.LaserShots = Array.Empty<LaserShotState>();
            state.AttachedBallIds = effectResult.NextAttachedBalls;

            return events;
        }

        private static IReadOnlyList<GameplayEvent> ResolveItemFellOff(GameplayRuntimeState state, ItemFellOffFloorFact fact)
        {
            state.ItemDrops = state.ItemDrops.Where(i => i.Id != fact.ItemId).ToArray();
            return Array.Empty<GameplayEvent>();
        }

        // ─── Main entry ───

        public static ApplyResult ApplyCollisions(
            GameplayRuntimeState initialState,
            IReadOnlyList<CollisionFact> collisions,
            ResolutionTables tables,
            ApplyOptions options = default)
        {
            var state = initialState;
            var allEvents = new List<GameplayEvent>();
            var skipBlockReflection = options.BlockReflectionAlreadyApplied;

            foreach (var fact in collisions)
            {
                IReadOnlyList<GameplayEvent> events = fact switch
                {
                    BallHitWallFact f => ResolveWall(state, f, tables.Config.Physics),
                    BallHitBarFact f => ResolveBar(state, f, tables.Config.Physics),
                    BallHitBlockFact f => ResolveBlock(state, f, tables, skipBlockReflection),
                    BallHitFloorFact f => ResolveFloor(state, f),
                    ItemPickedUpFact f => ResolveItemPickedUp(state, f, tables),
                    ItemFellOffFloorFact f => ResolveItemFellOff(state, f),
                    _ => Array.Empty<GameplayEvent>(),
                };
                allEvents.AddRange(events);
            }

            return new ApplyResult(state, allEvents);
        }
    }
}
