namespace Arkanoid.Definitions
{
    // LayoutConfigTable 의 *플레이필드 부분만* — PlayfieldLayout 의 SSOT 의존성 충족 위해.
    // Phase 2 에 LayoutConfigSO 로 확장 (캔버스, UI, 슬라이더 좌표 등 추가).
    public static class LayoutConfig
    {
        public const float PlayfieldWidth = 720f;
        public const float PlayfieldHeight = 720f;
    }
}
