using System;
using System.Collections.Generic;
using System.Linq;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public sealed class GameplayController
    {
        public sealed class Dependencies
        {
            public IReadOnlyDictionary<string, BlockDefinition> BlockDefinitions { get; }
            public IReadOnlyDictionary<ItemType, ItemDefinition> ItemDefinitions { get; }
            public GameplayConfig Config { get; }
            public IReadOnlyDictionary<string, SpinnerDefinition> SpinnerDefinitions { get; }

            public Dependencies(
                IReadOnlyDictionary<string, BlockDefinition> blockDefinitions,
                IReadOnlyDictionary<ItemType, ItemDefinition> itemDefinitions,
                GameplayConfig config,
                IReadOnlyDictionary<string, SpinnerDefinition>? spinnerDefinitions = null)
            {
                BlockDefinitions = blockDefinitions;
                ItemDefinitions = itemDefinitions;
                Config = config;
                SpinnerDefinitions = spinnerDefinitions ?? new Dictionary<string, SpinnerDefinition>();
            }
        }

        private GameplayRuntimeState _state;
        private readonly Dependencies _deps;
        private readonly BarEffectService _barEffectService;
        private readonly LaserSystem _laserSystem;
        private readonly SpinnerSystem _spinnerSystem;
        private int _laserShotCounter = 0;
        // 자동 발사 timer (ms 누적). 비활성 공만 있을 때 누적, 임계 도달 시 자동 LaunchBall.
        private float _autoLaunchTimerMs = 0f;

        public GameplayController(GameplayRuntimeState initialState, Dependencies deps)
        {
            _state = initialState;
            _deps = deps;
            _barEffectService = new BarEffectService(deps.ItemDefinitions);
            _laserSystem = new LaserSystem(() => $"laser_{_laserShotCounter++}");
            _spinnerSystem = new SpinnerSystem(deps.SpinnerDefinitions);
        }

        public GameplayRuntimeState GetState() => _state;
        public void SetState(GameplayRuntimeState newState) => _state = newState;

        // 한 틱 진행. 반환: 이번 틱 발행 이벤트 목록.
        // Tick order (mvp1.md §12): commands → launch → movement → collisions → judge → emit
        public IReadOnlyList<GameplayEvent> Tick(InputSnapshot input, float dt)
        {
            var allEvents = new List<GameplayEvent>();

            // 1. Resolve commands
            var commands = InputCommandResolver.ResolveGameplayCommands(input, _state);

            // 2 & 3. Process commands
            var moveDirection = 0;
            float? barTargetX = null;

            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case LaunchBallCommand:
                        {
                            var (launchState, launchEvent) = ApplyLaunchBall();
                            _state = launchState;
                            allEvents.Add(launchEvent);
                            break;
                        }
                    case MoveBarCommand mb:
                        moveDirection = mb.Direction;
                        break;
                    case SetBarTargetXCommand sbtx:
                        barTargetX = sbtx.X;
                        break;
                    case ReleaseAttachedBallsCommand:
                        {
                            var releaseResult = _barEffectService.ReleaseManually(_state.Bar, _state.AttachedBallIds);
                            var releasedIds = new HashSet<string>(releaseResult.ReleasedBallIds);
                            for (var i = 0; i < _state.Balls.Length; i++)
                            {
                                if (releasedIds.Contains(_state.Balls[i].Id))
                                    _state.Balls[i] = LaunchAttachedBall(_state.Balls[i]);
                            }
                            // 2026-05-18: magnet 5회 — release 1회당 1 차감. 0 도달 시 effect 종료.
                            var prevUses = _state.MagnetRemainingUses ?? 0;
                            var nextUses = Math.Max(0, prevUses - 1);
                            var magnetExhausted = nextUses <= 0 && prevUses > 0;
                            _state.Bar = magnetExhausted
                                ? releaseResult.NextBar with { ActiveEffect = BarEffect.None }
                                : releaseResult.NextBar;
                            _state.AttachedBallIds = Array.Empty<string>();
                            _state.MagnetRemainingUses = nextUses;
                            if (magnetExhausted) _state.MagnetRemainingTime = 0f;
                            allEvents.AddRange(releaseResult.Events);
                            break;
                        }
                    case FireLaserCommand:
                        {
                            var laserItemDef = _deps.ItemDefinitions.TryGetValue(ItemType.Laser, out var def)
                                ? (ItemDefinition?)def : null;
                            var fireResult = _laserSystem.FireLaser(
                                _state.Bar,
                                _state.LaserShots,
                                laserItemDef?.LaserCooldownMs);
                            _state.LaserShots = fireResult.NewShots;
                            _state.LaserCooldownRemaining = fireResult.NextCooldownMs;
                            allEvents.AddRange(fireResult.Events);
                            break;
                        }
                }
            }

            // 3.5. 자동 발사 — 활성 공 0 + 일정 시간 후 자동 LaunchBall.
            var hasActiveBall = _state.Balls.Any(b => b.IsActive);
            if (!hasActiveBall)
            {
                _autoLaunchTimerMs += dt * 1000f;
                if (_autoLaunchTimerMs >= _deps.Config.AutoLaunchDelayMs)
                {
                    var (launchState, launchEvent) = ApplyLaunchBall();
                    _state = launchState;
                    allEvents.Add(launchEvent);
                    _autoLaunchTimerMs = 0f;
                }
            }
            else
            {
                _autoLaunchTimerMs = 0f;
            }

            // 4. Movement — snapshot prev state for collision detection.
            // GameplayRuntimeState 가 class 라 *얕은 복사* 필요 (snapshot).
            var prevState = SnapshotState(_state);

            // 슬라이더 드래그 있으면 즉시 스냅, 없으면 키보드 방향.
            BarState newBar;
            if (barTargetX.HasValue)
            {
                var halfWidth = _state.Bar.Width / 2f;
                var clamped = MathF.Max(halfWidth, MathF.Min(PlayfieldLayout.PlayfieldWidth - halfWidth, barTargetX.Value));
                newBar = _state.Bar with { X = clamped };
            }
            else
            {
                newBar = MovementSystem.MoveBar(_state.Bar, moveDirection, dt, _deps.Config);
            }

            var currentBlocks = _state.Blocks;
            // 회전체 swept 충돌용 — 이번 tick 시작 시점 공 위치 snapshot.
            var preMovementBalls = _state.Balls.ToArray();

            var accumulatedBlockFacts = new List<BallHitBlockFact>();
            var accumulatedSweptWallFacts = new List<BallHitWallFact>();
            var physics = _deps.Config.Physics;
            var currentBorders = _state.Borders;
            var currentDoors = _state.Doors;

            var newBalls = new BallState[_state.Balls.Length];
            for (var i = 0; i < _state.Balls.Length; i++)
            {
                var ball = _state.Balls[i];
                var result = MovementSystem.MoveBallWithCollisions(ball, dt, currentBlocks, currentBorders, currentDoors, physics);
                accumulatedBlockFacts.AddRange(result.BlockFacts);
                accumulatedSweptWallFacts.AddRange(result.WallFacts);

                // Post-tick sanity check — last-resort separation.
                var sanity = MovementSystem.SanityCheckBallBlockSeparation(result.Ball, currentBlocks, physics);
                if (sanity.WasInside && sanity.CollisionFact is { } cf)
                {
                    var alreadyHit = accumulatedBlockFacts.Any(f => f.BlockId == cf.BlockId);
                    if (!alreadyHit) accumulatedBlockFacts.Add(cf);
                }

                newBalls[i] = MovementSystem.MoveAttachedBallToBar(sanity.Ball, newBar);
            }

            var newItemDrops = new ItemDropState[_state.ItemDrops.Length];
            for (var i = 0; i < _state.ItemDrops.Length; i++)
                newItemDrops[i] = MovementSystem.MoveItemDrop(_state.ItemDrops[i], dt);

            _state.Bar = newBar;
            _state.Balls = newBalls;
            _state.ItemDrops = newItemDrops;

            // 5. Detect collisions (bar/item only — block/wall 이미 swept 처리).
            var pipelineCollisions = CollisionService.DetectCollisions(_state, prevState);
            var sweptWallBallIds = new HashSet<string>(accumulatedSweptWallFacts.Select(f => f.BallId));
            var barItemCollisions = pipelineCollisions.Where(f => f switch
            {
                BallHitBlockFact => false,                                  // swept 처리됨
                BallHitWallFact wf => !sweptWallBallIds.Contains(wf.BallId), // swept 처리됨
                _ => true,
            }).ToList();
            var collisions = accumulatedBlockFacts.Cast<CollisionFact>().Concat(barItemCollisions).ToList();

            // 6. Apply collision results — blockReflectionAlreadyApplied=true (swept 가 이미 반사).
            var tables = new ResolutionTables(_deps.BlockDefinitions, _deps.ItemDefinitions, _deps.Config);
            var applyResult = CollisionResolutionService.ApplyCollisions(_state, collisions, tables, new ApplyOptions(BlockReflectionAlreadyApplied: true));
            _state = applyResult.NextState;
            var collisionEvents = applyResult.Events;

            // 6a. BallsReleased(replaced) 가 발행되면 공 활성화.
            foreach (var e in collisionEvents)
            {
                if (e is BallsReleasedEvent br && br.ReleaseReason == BallReleaseReason.Replaced && br.BallIds.Count > 0)
                {
                    var replacedIds = new HashSet<string>(br.BallIds);
                    for (var i = 0; i < _state.Balls.Length; i++)
                    {
                        if (replacedIds.Contains(_state.Balls[i].Id))
                            _state.Balls[i] = LaunchAttachedBall(_state.Balls[i]);
                    }
                }
            }

            allEvents.AddRange(collisionEvents);

            // 6.4. Spinner tick — angleRad/phase 업데이트.
            if (_state.SpinnerStates.Count > 0)
            {
                _state.SpinnerStates = _spinnerSystem.Tick(_state.SpinnerStates, dt);
            }

            // 6.45. Door tick — opened 로 막 전이한 door 는 spinner spawn.
            if (_state.Doors.Count > 0)
            {
                var dtMs = dt * 1000f;
                var prevDoors = _state.Doors;
                var nextDoors = new DoorState[prevDoors.Count];
                for (var i = 0; i < prevDoors.Count; i++)
                    nextDoors[i] = Door.TickAnimation(prevDoors[i], dtMs);

                var nextSpinners = new List<SpinnerRuntimeState>(_state.SpinnerStates);
                var finalDoors = new DoorState[nextDoors.Length];
                for (var i = 0; i < nextDoors.Length; i++)
                {
                    var nd = nextDoors[i];
                    var pd = prevDoors[i];
                    // 이번 틱에 opened 로 전이 + 스피너 없음 → spawn
                    if (pd.Phase != DoorPhase.Opened && nd.Phase == DoorPhase.Opened && nd.SpawnedSpinnerId == null)
                    {
                        var id = $"spinner_{nd.Id}";
                        var spinner = _spinnerSystem.SpawnFromDoor(nd.X, nd.Y, nd.SpinnerDefinitionId, id);
                        nextSpinners.Add(spinner);
                        finalDoors[i] = nd with { SpawnedSpinnerId = id };
                    }
                    else
                    {
                        finalDoors[i] = nd;
                    }
                }
                _state.Doors = finalDoors;
                _state.SpinnerStates = nextSpinners;
            }

            // 6.5. Magnet tick — UseCount 기반이면 skip (5회로 종료).
            var magnetUseCountActive = (_state.MagnetRemainingUses ?? 0) > 0;
            if (!magnetUseCountActive)
            {
                var magnetTick = _barEffectService.TickMagnet(
                    _state.MagnetRemainingTime,
                    _state.AttachedBallIds,
                    _state.Bar,
                    dt * 1000f);
                if (magnetTick.ReleasedBallIds.Count > 0)
                {
                    var timedOutIds = new HashSet<string>(magnetTick.ReleasedBallIds);
                    for (var i = 0; i < _state.Balls.Length; i++)
                    {
                        if (timedOutIds.Contains(_state.Balls[i].Id))
                            _state.Balls[i] = LaunchAttachedBall(_state.Balls[i]);
                    }
                    _state.Bar = magnetTick.NextBar;
                    _state.AttachedBallIds = Array.Empty<string>();
                    _state.MagnetRemainingTime = magnetTick.NextMagnetRemaining;
                }
                else
                {
                    _state.Bar = magnetTick.NextBar;
                    _state.MagnetRemainingTime = magnetTick.NextMagnetRemaining;
                }
                allEvents.AddRange(magnetTick.Events);
            }

            // 6.55. Spinner ↔ Ball 충돌 — swept (prevBall) 로 터널링 방지.
            if (_state.SpinnerStates.Count > 0)
            {
                for (var i = 0; i < _state.Balls.Length; i++)
                {
                    var ball = _state.Balls[i];
                    BallState? prev = null;
                    foreach (var p in preMovementBalls)
                    {
                        if (p.Id == ball.Id) { prev = p; break; }
                    }
                    var result = _spinnerSystem.HandleBallCollisions(ball, _state.SpinnerStates, prev);
                    _state.Balls[i] = result.NextBall;
                }
            }

            // 6.6. Laser ↔ Block 충돌 (이동 전 현재 위치).
            if (_state.LaserShots.Count > 0)
            {
                var laserCollision = _laserSystem.HandleBlockCollisions(_state.LaserShots, _state.Blocks, _deps.BlockDefinitions);
                if (laserCollision.Events.Count > 0 || laserCollision.NextShots.Count != _state.LaserShots.Count)
                {
                    _state.LaserShots = laserCollision.NextShots;
                    _state.Blocks = laserCollision.NextBlocks.ToArray();
                    _state.Session = _state.Session with { Score = _state.Session.Score + laserCollision.ScoreDelta };
                    allEvents.AddRange(laserCollision.Events);
                }
            }

            // 6.7. Laser tick.
            if (_state.LaserShots.Count > 0 || _state.LaserCooldownRemaining > 0f)
            {
                var laserTick = _laserSystem.Tick(_state.LaserShots, _state.LaserCooldownRemaining, dt);
                _state.LaserShots = laserTick.NextShots;
                _state.LaserCooldownRemaining = laserTick.NextCooldownMs;
            }

            // 6.7b. Laser 자동 발사 + duration 감소.
            if (_state.Bar.ActiveEffect == BarEffect.Laser)
            {
                var prevRemain = _state.LaserRemainingTime ?? 0f;
                var nextRemain = MathF.Max(0f, prevRemain - dt * 1000f);
                var laserExpired = prevRemain > 0f && nextRemain <= 0f;
                if (laserExpired)
                {
                    _state.Bar = _state.Bar with { ActiveEffect = BarEffect.None };
                    _state.LaserRemainingTime = 0f;
                    _state.LaserShots = Array.Empty<LaserShotState>();
                }
                else
                {
                    _state.LaserRemainingTime = nextRemain;
                    if (_state.LaserCooldownRemaining <= 0f)
                    {
                        var laserItemDef = _deps.ItemDefinitions.TryGetValue(ItemType.Laser, out var def)
                            ? (ItemDefinition?)def : null;
                        var fireResult = _laserSystem.FireLaser(_state.Bar, _state.LaserShots, laserItemDef?.LaserCooldownMs);
                        _state.LaserShots = fireResult.NewShots;
                        _state.LaserCooldownRemaining = fireResult.NextCooldownMs;
                        allEvents.AddRange(fireResult.Events);
                    }
                }
            }

            // 6.8. Spinner ↔ Block 충돌 (phase-gate).
            if (_state.SpinnerStates.Count > 0)
            {
                var sbResult = _spinnerSystem.HandleBlockCollisions(_state.SpinnerStates, _state.Blocks, _deps.BlockDefinitions);
                if (sbResult.Events.Count > 0)
                {
                    _state.Blocks = sbResult.NextBlocks.ToArray();
                    _state.Session = _state.Session with { Score = _state.Session.Score + sbResult.ScoreDelta };
                    allEvents.AddRange(sbResult.Events);
                }
            }

            // 7. Judge stage outcome
            var outcome = StageRuleService.JudgeStageOutcome(_state, collisionEvents);
            switch (outcome)
            {
                case StageOutcomeLifeLost ll:
                    _state.Session = _state.Session with { Lives = ll.RemainingLives };
                    PatchLifeLostEvents(allEvents, ll.RemainingLives);
                    break;
                case StageOutcomeGameOver:
                    _state.Session = _state.Session with { Lives = 0 };
                    PatchLifeLostEvents(allEvents, 0);
                    allEvents.Add(new GameOverConditionMetEvent());
                    break;
                case StageOutcomeClear:
                    _state.IsStageCleared = true;
                    allEvents.Add(new StageClearedEvent());
                    break;
            }

            return allEvents;
        }

        // Dev 전용: 강제 클리어. StageCleared 이벤트 1건.
        public IReadOnlyList<GameplayEvent> ForceStageCleared()
        {
            _state.IsStageCleared = true;
            return new GameplayEvent[] { new StageClearedEvent() };
        }

        private (GameplayRuntimeState State, GameplayEvent Event) ApplyLaunchBall()
        {
            var config = _deps.Config;
            var angleRad = config.BallInitialAngleDeg * MathF.PI / 180f;
            var speed = config.BallInitialSpeed;
            // angleDeg=-60 → vx = cos(-60°) = 0.5, vy = sin(-60°) = -0.866 (TS Y+ 아래, 음수=위)
            var vx = MathF.Cos(angleRad) * speed;
            var vy = MathF.Sin(angleRad) * speed;

            var launched = false;
            for (var i = 0; i < _state.Balls.Length; i++)
            {
                if (!_state.Balls[i].IsActive && !launched)
                {
                    launched = true;
                    // 새 공 spawn — 파워 상태 명시 reset (바 충돌 이벤트 의존 X).
                    _state.Balls[i] = _state.Balls[i] with
                    {
                        IsActive = true, Vx = vx, Vy = vy,
                        BlocksSincePaddle = 0, IsPowered = false,
                    };
                }
            }
            return (_state, new BallLaunchedEvent());
        }

        // 자석 해제된 공 활성화 + 초기 속도. AttachedOffsetX 제거.
        private BallState LaunchAttachedBall(BallState ball)
        {
            var config = _deps.Config;
            var angleRad = config.BallInitialAngleDeg * MathF.PI / 180f;
            var speed = config.BallInitialSpeed;
            var vx = MathF.Cos(angleRad) * speed;
            var vy = MathF.Sin(angleRad) * speed;
            return ball with
            {
                IsActive = true, Vx = vx, Vy = vy,
                AttachedOffsetX = null,
            };
        }

        // LifeLost 이벤트의 RemainingLives 값 보정 (StageRuleService 가 채울 자리).
        private static void PatchLifeLostEvents(List<GameplayEvent> events, int remainingLives)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] is LifeLostEvent)
                    events[i] = new LifeLostEvent(remainingLives);
            }
        }

        // Balls 재초기화 — RoundIntro 복귀 시 (LifeLost 후).
        public void ResetBallsToBar()
        {
            for (var i = 0; i < _state.Balls.Length; i++)
            {
                _state.Balls[i] = _state.Balls[i] with
                {
                    IsActive = false,
                    Vx = 0f,
                    Vy = 0f,
                    X = _state.Bar.X,
                    Y = _state.Bar.Y - 16f,
                };
            }
        }

        // 얕은 snapshot — collision detection 의 prev 비교용. Balls 배열만 복사.
        private static GameplayRuntimeState SnapshotState(GameplayRuntimeState s)
        {
            return new GameplayRuntimeState
            {
                Session = s.Session,
                Bar = s.Bar,
                Balls = (BallState[])s.Balls.Clone(),
                Blocks = s.Blocks,
                Borders = s.Borders,
                Doors = s.Doors,
                ItemDrops = s.ItemDrops,
                IsStageCleared = s.IsStageCleared,
                MagnetRemainingTime = s.MagnetRemainingTime,
                MagnetRemainingUses = s.MagnetRemainingUses,
                AttachedBallIds = s.AttachedBallIds,
                LaserCooldownRemaining = s.LaserCooldownRemaining,
                LaserRemainingTime = s.LaserRemainingTime,
                LaserShots = s.LaserShots,
                SpinnerStates = s.SpinnerStates,
                CurrentTrailStyle = s.CurrentTrailStyle,
            };
        }
    }
}
