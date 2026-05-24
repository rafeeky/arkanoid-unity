namespace Arkanoid.Gameplay
{
    // Phaser 0의존 POCO. Unity Input System (Phase 4) 가 매 프레임 채워서 GameplayController 에 전달.
    public readonly record struct InputSnapshot(
        bool LeftDown,
        bool RightDown,
        bool SpaceJustPressed,
        // Q 키 edge — 타이틀 복귀 (묶음 F).
        bool QJustPressed = false,
        // ESC 키 edge — InGame 일시정지 토글.
        bool EscJustPressed = false,
        // ← edge — 타이틀 난이도 NORMAL (묶음 E).
        bool LeftJustPressed = false,
        // → edge — 타이틀 난이도 HARD (묶음 E).
        bool RightJustPressed = false,
        // 마우스/터치 드래그 시 바 목표 x 좌표 (TS 좌표, null 면 미사용) (묶음 H1).
        float? TargetBarX = null);
}
