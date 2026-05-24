using System;
using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    // Flow 상태 전환에 따라 GameplayRuntimeState 초기화/리셋.
    // - InitializeStage: IntroStory→RoundIntro (새 게임). StageRuntimeFactory 호출.
    // - ResetForRetry: LifeLost→RoundIntro. 블록·점수·라이프 유지, 공/바 재배치, 아이템 제거.
    //   activeEffect 리셋 (None, baseBarWidth).
    // - LoadNextStage: StageCleared→RoundIntro. score/lives 유지, 블록 재구성, 공/바 리셋.
    public sealed class GameplayLifecycleHandler
    {
        private const float BarHeight = 16f;
        private readonly IReadOnlyDictionary<string, BlockDefinition> _blockDefinitions;

        public GameplayLifecycleHandler(IReadOnlyDictionary<string, BlockDefinition> blockDefinitions)
        {
            _blockDefinitions = blockDefinitions;
        }

        public GameplayRuntimeState InitializeStage(
            StageDefinition stage,
            GameplayConfig config,
            int initialLives,
            DifficultyConfig? difficulty = null)
        {
            return StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                stage, config, _blockDefinitions, initialLives, difficulty);
        }

        // 라이프 손실 후 재시도 — 블록/점수/라이프 유지, 공/바 위치 리셋, 아이템 제거.
        // 바 효과 리셋 (None, baseBarWidth).
        public GameplayRuntimeState ResetForRetry(
            GameplayRuntimeState currentState,
            StageDefinition stage,
            GameplayConfig config)
        {
            var resetBar = currentState.Bar with
            {
                X = stage.BarSpawnX,
                Y = stage.BarSpawnY,
                Width = config.BaseBarWidth,
                ActiveEffect = BarEffect.None,
            };

            var resetBalls = new BallState[currentState.Balls.Length];
            for (var i = 0; i < currentState.Balls.Length; i++)
            {
                resetBalls[i] = currentState.Balls[i] with
                {
                    IsActive = false,
                    Vx = 0f,
                    Vy = 0f,
                    // 발사각 -60° 와 시각 일치 — 바 우측 30px 오프셋.
                    X = stage.BarSpawnX + PlayfieldLayout.InitialLaunchOffsetX,
                    Y = stage.BarSpawnY - BarHeight,
                };
            }

            return new GameplayRuntimeState
            {
                Session = currentState.Session,
                Bar = resetBar,
                Balls = resetBalls,
                Blocks = currentState.Blocks,
                Borders = currentState.Borders,
                Doors = currentState.Doors,
                ItemDrops = Array.Empty<ItemDropState>(),
                IsStageCleared = currentState.IsStageCleared,
                MagnetRemainingTime = 0f,
                MagnetRemainingUses = null,
                AttachedBallIds = Array.Empty<string>(),
                LaserCooldownRemaining = 0f,
                LaserRemainingTime = null,
                LaserShots = Array.Empty<LaserShotState>(),
                // SpinnerStates 는 스테이지 내 보존 (LifeLost 후에도 유지).
                SpinnerStates = currentState.SpinnerStates,
                CurrentTrailStyle = currentState.CurrentTrailStyle,
            };
        }

        // 다음 스테이지 로드 — score/lives 유지, 블록 새로 생성, 공/바 리셋.
        public GameplayRuntimeState LoadNextStage(
            GameplayRuntimeState currentState,
            StageDefinition nextStage,
            GameplayConfig config,
            DifficultyConfig? difficulty = null)
        {
            var freshState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                nextStage, config, _blockDefinitions, currentState.Session.Lives, difficulty);

            freshState.Session = freshState.Session with
            {
                Score = currentState.Session.Score,
                Lives = currentState.Session.Lives,
                HighScore = currentState.Session.HighScore,
                // CurrentStageIndex 는 호출자 (AppContext) 가 덮어씀.
            };
            freshState.IsStageCleared = false;
            return freshState;
        }
    }
}
