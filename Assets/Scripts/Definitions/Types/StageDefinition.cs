using System.Collections.Generic;

namespace Arkanoid.Definitions
{
    public readonly record struct BlockPlacement(
        int Col,
        int Row,
        string DefinitionId);

    public readonly record struct BorderPlacement(
        int Col,
        int Row,
        BorderOrientation Orientation);

    public readonly record struct DoorPlacement(
        int Col,
        string SpinnerDefinitionId);

    public readonly record struct SpinnerPlacement(
        float X,
        float Y,
        string DefinitionId,
        float? InitialAngleRad = null);

    public readonly record struct StageDefinition(
        string StageId,
        float BarSpawnX,
        float BarSpawnY,
        IReadOnlyList<BlockPlacement> Blocks,
        IReadOnlyList<BorderPlacement>? Borders = null,
        IReadOnlyList<DoorPlacement>? Doors = null,
        IReadOnlyList<SpinnerPlacement>? Spinners = null,
        // TrailStyleId — Phase 2 에 정식 enum/wrapper. 지금은 string.
        string? TrailStyle = null);
}
