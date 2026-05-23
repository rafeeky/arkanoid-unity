namespace Arkanoid.Gameplay
{
    // 'horizontal' = 가로 BORDER_LENGTH × 세로 BORDER_THICKNESS (상단 테두리)
    // 'vertical'   = 가로 BORDER_THICKNESS × 세로 BORDER_LENGTH (좌/우 테두리)
    public enum BorderOrientation { Horizontal, Vertical }

    // 플레이필드 테두리 (좌/우/상단) 한 셀. (x, y) 는 좌상단. 깨지지 않는 벽.
    public readonly record struct BorderBlockState(
        string Id,
        float X,
        float Y,
        BorderOrientation Orientation);
}
