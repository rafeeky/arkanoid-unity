using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "SpinnerDefinition", menuName = "Arkanoid/Gameplay/Spinner Definition")]
    public sealed class SpinnerDefinitionSO : ScriptableObject
    {
        [SerializeField] private string definitionId = "spinner_cube";
        [SerializeField] private SpinnerKind kind = SpinnerKind.Cube;
        [SerializeField] private float size = 48f;
        [SerializeField] private float rotationSpeedRadPerSec = 1.5f;
        // 블록 충돌 허용 위상 (rad). 큐브: [0, π/2]. 삼각: [0].
        [SerializeField] private float[] blockCollisionPhases = new float[] { 0f, Mathf.PI / 2f };

        public SpinnerDefinition Data => new(
            DefinitionId: definitionId,
            Kind: kind,
            Size: size,
            RotationSpeedRadPerSec: rotationSpeedRadPerSec,
            BlockCollisionPhases: blockCollisionPhases);
    }
}
