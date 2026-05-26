namespace Arkanoid.Definitions
{
    // 충돌/반사 물리 튜닝 값. 디자이너가 SO 에서 직접 만질 수 있도록 데이터로 둠.
    public readonly record struct PhysicsConfig(
        // sub-step 1회당 진행 픽셀 수. 작을수록 터널링 안전, 비용 증가.
        float SubStepSize,
        // 한 틱당 sub-step 최대 횟수. 폭주 방지.
        int MaxSubSteps,
        // 충돌 후 push-out 거리 (벽/블럭 공통). strict separation 보장.
        float PushOutEpsilon,
        // 공 반사 후 강제 최소 각도 (수평/수직축에서의). 무한 핑퐁 방지.
        float MinAngleDeg,
        // 바 반사 시 contactX → vx 변환 계수. 1.0 에 가까울수록 가장자리 반사가 가파름.
        float BarContactBias);

    public readonly record struct GameplayConfig(
        int InitialLives,
        float BaseBarWidth,
        float BarMoveSpeed,
        float BallInitialSpeed,
        float BallInitialAngleDeg,
        float RoundIntroDurationMs,
        float BlockHitFlashDurationMs,
        float BarBreakDurationMs,
        float ExpandMultiplier,
        // 라운드 시작 후 N ms 후 자동 발사. 데이터 제어용.
        float AutoLaunchDelayMs,
        PhysicsConfig Physics);
}
