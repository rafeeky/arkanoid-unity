using System.Collections.Generic;

namespace Arkanoid.Definitions
{
    public enum SpinnerKind { Cube, Triangle }

    public readonly record struct SpinnerDefinition(
        string DefinitionId,
        SpinnerKind Kind,
        // 외접원 지름 (px)
        float Size,
        float RotationSpeedRadPerSec,
        // 블록 충돌 허용 위상 (라디안). 큐브: [0, π/2]. 삼각: [0]
        IReadOnlyList<float> BlockCollisionPhases);
}
