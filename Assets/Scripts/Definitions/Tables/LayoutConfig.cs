namespace Arkanoid.Definitions
{
    // LayoutConfigTable 의 *플레이필드 부분만* — PlayfieldLayout 의 SSOT 의존성 충족 위해.
    // Phase 2 에 LayoutConfigSO 로 확장 (캔버스, UI, 슬라이더 좌표 등 추가).
    public static class LayoutConfig
    {
        public const float PlayfieldWidth = 720f;
        // 2026-05-16: playfield height 720→900 (docs/screen-layout.md §3-2).
        // 늘어난 아래 영역(y=720~900)에 bar 배치 — 모바일 한 손 조작 친화.
        public const float PlayfieldHeight = 900f;
    }
}
