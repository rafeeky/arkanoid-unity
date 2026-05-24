using System;
using System.Collections.Generic;
using System.Linq;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public readonly record struct BarEffectApplyResult(
        BarState NextBar,
        float NextMagnetRemaining,
        float NextLaserCooldown,
        // laser → 타 효과 전환 시 true. 호출 측은 LaserShots 배열을 비워야 함.
        bool ClearLaserShots,
        IReadOnlyList<string> NextAttachedBalls,
        IReadOnlyList<string> ReleasedBallIds,
        IReadOnlyList<GameplayEvent> Events);

    public readonly record struct BarEffectTickResult(
        float NextMagnetRemaining,
        BarState NextBar,
        IReadOnlyList<string> ReleasedBallIds,
        IReadOnlyList<GameplayEvent> Events);

    public readonly record struct BarEffectReleaseResult(
        BarState NextBar,
        IReadOnlyList<string> ReleasedBallIds,
        IReadOnlyList<GameplayEvent> Events);

    // 바 효과 전환을 단일 경로로 관리하는 순수 서비스.
    // 책임: 새 효과 적용 (기존 효과 정리) / 자석 타이머 tick / 자석 수동 해제.
    // 금지: 전역 상태, Random/Date.now, Phaser/DOM.
    public sealed class BarEffectService
    {
        // 자석 기본 지속시간 (ms). ItemDefinition.MagnetDurationMs 없을 때 fallback.
        private const float DefaultMagnetDurationMs = 8000f;
        // 확장 배율 기본값.
        private const float DefaultExpandMultiplier = 1.5f;

        private readonly IReadOnlyDictionary<ItemType, ItemDefinition> _itemDefinitions;

        public BarEffectService(IReadOnlyDictionary<ItemType, ItemDefinition> itemDefinitions)
        {
            _itemDefinitions = itemDefinitions;
        }

        // 새 효과 적용 — 기존 효과 정리 포함.
        public BarEffectApplyResult ApplyEffect(
            BarState currentBar,
            float currentMagnetRemaining,
            float currentLaserCooldown,
            IReadOnlyList<string> currentAttachedBalls,
            ItemType newItemType,
            float baseBarWidth)
        {
            var events = new List<GameplayEvent>();
            IReadOnlyList<string> releasedBallIds = Array.Empty<string>();
            var nextMagnetRemaining = currentMagnetRemaining;
            var nextLaserCooldown = currentLaserCooldown;
            IReadOnlyList<string> nextAttachedBalls = currentAttachedBalls;
            var clearLaserShots = false;

            // 1. 기존 효과 정리
            var prevEffect = currentBar.ActiveEffect;
            if (prevEffect == BarEffect.Magnet && currentAttachedBalls.Count > 0)
            {
                releasedBallIds = currentAttachedBalls.ToArray();
                nextAttachedBalls = Array.Empty<string>();
                nextMagnetRemaining = 0f;
                events.Add(new BallsReleasedEvent(releasedBallIds, BallReleaseReason.Replaced));
            }
            else if (prevEffect == BarEffect.Magnet)
            {
                nextMagnetRemaining = 0f;
            }
            else if (prevEffect == BarEffect.Laser)
            {
                // laser → 다른 효과: 쿨다운 + 비행 중인 샷 모두 정리.
                nextLaserCooldown = 0f;
                clearLaserShots = true;
            }

            // 2. 새 효과 적용
            BarState nextBar;
            switch (newItemType)
            {
                case ItemType.Expand:
                    {
                        var multiplier = _itemDefinitions.TryGetValue(ItemType.Expand, out var itemDef)
                                         && itemDef.ExpandMultiplier.HasValue
                            ? itemDef.ExpandMultiplier.Value
                            : DefaultExpandMultiplier;
                        nextBar = currentBar with
                        {
                            Width = baseBarWidth * multiplier,
                            ActiveEffect = BarEffect.Expand,
                        };
                        break;
                    }
                case ItemType.Magnet:
                    {
                        var duration = _itemDefinitions.TryGetValue(ItemType.Magnet, out var itemDef)
                                       && itemDef.MagnetDurationMs.HasValue
                            ? itemDef.MagnetDurationMs.Value
                            : DefaultMagnetDurationMs;
                        nextMagnetRemaining = duration;
                        nextAttachedBalls = Array.Empty<string>();
                        nextBar = currentBar with
                        {
                            Width = baseBarWidth,
                            ActiveEffect = BarEffect.Magnet,
                        };
                        break;
                    }
                case ItemType.Laser:
                    // 새 laser 시작 — 쿨다운 항상 0으로 시작 (prevEffect 무관).
                    nextLaserCooldown = 0f;
                    nextBar = currentBar with
                    {
                        Width = baseBarWidth,
                        ActiveEffect = BarEffect.Laser,
                    };
                    break;
                default:
                    nextBar = currentBar;
                    break;
            }

            return new BarEffectApplyResult(
                NextBar: nextBar,
                NextMagnetRemaining: nextMagnetRemaining,
                NextLaserCooldown: nextLaserCooldown,
                ClearLaserShots: clearLaserShots,
                NextAttachedBalls: nextAttachedBalls,
                ReleasedBallIds: releasedBallIds,
                Events: events);
        }

        // 자석 타이머 dt 감소. 0 이하 → activeEffect=None + BallsReleased (timeout).
        public BarEffectTickResult TickMagnet(
            float currentMagnetRemaining,
            IReadOnlyList<string> attachedBallIds,
            BarState bar,
            float dt)
        {
            if (bar.ActiveEffect != BarEffect.Magnet || currentMagnetRemaining <= 0f)
            {
                return new BarEffectTickResult(
                    currentMagnetRemaining, bar,
                    Array.Empty<string>(), Array.Empty<GameplayEvent>());
            }

            var nextMagnetRemaining = currentMagnetRemaining - dt;
            if (nextMagnetRemaining <= 0f)
            {
                var nextBar = bar with { ActiveEffect = BarEffect.None };
                var releasedBallIds = attachedBallIds.ToArray();
                var events = new List<GameplayEvent>();
                if (releasedBallIds.Length > 0)
                {
                    events.Add(new BallsReleasedEvent(releasedBallIds, BallReleaseReason.Timeout));
                }
                return new BarEffectTickResult(0f, nextBar, releasedBallIds, events);
            }

            return new BarEffectTickResult(nextMagnetRemaining, bar,
                Array.Empty<string>(), Array.Empty<GameplayEvent>());
        }

        // 수동 트리거 (Space) — 부착 공만 해제. activeEffect 와 magnetRemainingTime 유지.
        // 타이머 살아 있는 동안 다음 공이 닿으면 다시 부착 가능.
        public BarEffectReleaseResult ReleaseManually(
            BarState currentBar,
            IReadOnlyList<string> attachedBallIds)
        {
            var nextBar = currentBar;  // activeEffect 유지
            var releasedBallIds = attachedBallIds.ToArray();
            var events = new List<GameplayEvent>();
            if (releasedBallIds.Length > 0)
            {
                events.Add(new BallsReleasedEvent(releasedBallIds, BallReleaseReason.Space));
            }
            return new BarEffectReleaseResult(nextBar, releasedBallIds, events);
        }
    }
}
