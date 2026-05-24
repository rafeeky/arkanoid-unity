using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    // 플레이필드 좌표계의 *단일 진실 소스 (SSOT)*. 게임 런타임 (StageRuntimeFactory,
    // MovementSystem, CollisionService 등) 과 편집기가 모두 여기서 참조.
    //
    // 좌표계: 플레이필드 로컬 (0..720, 0..720). TS 원본의 **Y+ 아래** (좌상단 (0,0)) 유지.
    //   - Y 증가 = 화면 *아래*
    //   - 공이 위로 이동 = vy *음수*
    //   - 게임 캔버스 (1080×1920) 와는 별개. 카메라 zoom 1.5 + centerOn 으로 표시.
    //
    // Unity world 좌표 (Y+ 위, 1 unit = 1 px — D3.4) 로의 변환은 *Phase 3 카메라/렌더* 단계에서
    // Y flip 으로 처리. Phase 1~2 의 알고리즘 코드는 TS 와 비트 단위 동일 (968 테스트 그대로 통과 목표).
    public static class PlayfieldLayout
    {
        // ─── 플레이필드 (논리 좌표계) — LayoutConfig 가 SSOT ───
        public const float PlayfieldWidth = LayoutConfig.PlayfieldWidth;
        public const float PlayfieldHeight = LayoutConfig.PlayfieldHeight;

        // ─── 블록 그리드 ───
        public const float BlockWidth = 64f;
        public const float BlockHeight = 24f;
        public const float BlockGap = 4f;
        public const float BlockGridLeftMargin = 56f;
        public const float BlockGridStartY = 80f;
        // 편집기 그리드 셀 수. 게임은 stage 데이터에 따름이지만 편집기 UI 제약.
        public const int BlockGridCols = 9;
        public const int BlockGridRows = 7;

        // ─── 바 / 공 ───
        public const float BarHeight = 16f;
        public const float BallRadius = 8f;
        // 발사 각도 -60° 와 시각 일치 — 비활성 공이 바 중심에서 우측 30px 위.
        public const float InitialLaunchOffsetX = 30f;

        // ─── 아이템 드랍 ───
        // 2026-05-18: 블록과 동일 사이즈 (64×24). 블록 = 아이템 블록 일치 + 충돌 판정도 같음.
        public const float ItemWidth = 64f;   // = BlockWidth
        public const float ItemHeight = 24f;  // = BlockHeight

        // ─── 테두리 (BorderBlock) ───
        // 테두리 한 셀이 차지하는 길이. 720 의 약수여야 끝이 빔 없이 맞음. 720/60 = 12 셀.
        public const float BorderLength = 60f;
        // 테두리의 짧은 변 (= 일반 블럭 세로의 1/2). 두께.
        public const float BorderThickness = 12f;

        // ─── 스피너 궤도 clamp ───
        public const float CircleRadius = 60f;
        public const float CircleClampMargin = 10f;
        public const float MinCircleCenterY = 380f;
        public const float BarClearance = 80f;

        // 블록 grid (col, row) → 플레이필드 좌표 (블록 좌상단).
        // 편집기 미리보기 / 게임 런타임 둘 다 같은 위치 계산.
        public static (float X, float Y) BlockGridPosition(int col, int row)
        {
            return (
                BlockGridLeftMargin + col * (BlockWidth + BlockGap),
                BlockGridStartY + row * (BlockHeight + BlockGap)
            );
        }

        // 스피너 placement (spawnX, descentEndY) → 게임이 실제 사용하는 원 궤도 중심.
        // Clamp 규칙:
        //   centerX ∈ [CircleRadius + CircleClampMargin,
        //             PlayfieldWidth - CircleRadius - CircleClampMargin]
        //   centerY ∈ [MinCircleCenterY,
        //             PlayfieldHeight - CircleRadius - BarClearance]
        public static (float CenterX, float CenterY) ClampSpinnerCenter(float spawnX, float descentEndY)
        {
            var centerX = Clamp(
                spawnX,
                CircleRadius + CircleClampMargin,
                PlayfieldWidth - CircleRadius - CircleClampMargin);
            var centerY = Clamp(
                descentEndY,
                MinCircleCenterY,
                PlayfieldHeight - CircleRadius - BarClearance);
            return (centerX, centerY);
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
