using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    // BorderOrientation enum 은 Definitions 로 이동 (StageDefinition.BorderPlacement 도 사용).
    // 플레이필드 테두리 (좌/우/상단) 한 셀. (x, y) 는 좌상단. 깨지지 않는 벽.
    public readonly record struct BorderBlockState(
        string Id,
        float X,
        float Y,
        BorderOrientation Orientation);
}
