namespace Arkanoid.Definitions
{
    // 캔버스 영역 단일 진실 (docs/screen-layout.md). HUD/플레이필드/마스코트/하단 영역 좌표·크기.
    // Unity 매핑: ScriptableObject (LayoutConfigSO) 하나. CanvasScaler + RectTransform anchor.

    public readonly record struct CanvasRegion(float Width, float Height);

    // 플레이필드 — 게임 좌표계 (0..width, 0..height). 캔버스 안 (offsetX, offsetY) 에 배치.
    public readonly record struct PlayfieldRegion(float Width, float Height, float OffsetX, float OffsetY);

    // 위 HUD 영역 (SCORE / HIGH SCORE / ROUND 행). 캔버스 좌표.
    public readonly record struct HudLayout(
        float LabelY, float ValueY,
        float LeftX, float CenterX, float RightX,
        float LabelFontPx, float ValueFontPx);

    // 마스코트 표시 영역. 중심 + 크기.
    public readonly record struct MascotRegion(float CenterX, float CenterY, float Size);

    public readonly record struct LivesBarRegion(
        float StartX, float Y, float Scale, float Gap, int MaxDisplay);

    // 게임바 조작 슬라이더 (모바일/마우스 드래그).
    public readonly record struct BarSliderRegion(
        float CenterY, float TrackHalfWidth, float TrackHeight, float KnobRadius);

    public readonly record struct LayoutConfigData(
        CanvasRegion Canvas,
        PlayfieldRegion Playfield,
        HudLayout Hud,
        MascotRegion Mascot,
        LivesBarRegion LivesBar,
        BarSliderRegion BarSlider);

    // PlayfieldLayout 의 SSOT 의존성 충족용 const — LayoutConfigData 의 playfield.width/height 와 동기화.
    // 2026-05-16: playfield height 720→900 (docs/screen-layout.md §3-2).
    public static class LayoutConfig
    {
        public const float PlayfieldWidth = 720f;
        public const float PlayfieldHeight = 900f;
    }
}
