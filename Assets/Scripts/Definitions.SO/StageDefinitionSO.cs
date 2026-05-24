using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    // StageDefinition — 한 스테이지의 block/border/door/spinner 배치.
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "Arkanoid/Gameplay/Stage Definition")]
    public sealed class StageDefinitionSO : ScriptableObject
    {
        [SerializeField] private string stageId = "stage_1";
        [SerializeField] private float barSpawnX = 360f;
        [SerializeField] private float barSpawnY = 660f;
        [SerializeField] private List<SerializableBlock> blocks = new();
        [SerializeField] private List<SerializableBorder> borders = new();
        [SerializeField] private List<SerializableDoor> doors = new();
        [SerializeField] private List<SerializableSpinner> spinners = new();
        // TrailStyleId 의 enum 값 — Phase 3 에 정식 사용. 지금은 string fallback.
        [SerializeField] private TrailStyleId trailStyle = TrailStyleId.GoldenSun;
        [SerializeField] private bool useTrailStyle = false;  // false → null

        public StageDefinition Data => new(
            StageId: stageId,
            BarSpawnX: barSpawnX,
            BarSpawnY: barSpawnY,
            Blocks: blocks.Select(b => new BlockPlacement(b.col, b.row, b.definitionId)).ToList(),
            Borders: borders.Count == 0 ? null : borders.Select(b => new BorderPlacement(b.col, b.row, b.orientation)).ToList(),
            Doors: doors.Count == 0 ? null : doors.Select(d => new DoorPlacement(d.col, d.spinnerDefinitionId)).ToList(),
            Spinners: spinners.Count == 0 ? null : spinners.Select(s => new SpinnerPlacement(s.x, s.y, s.definitionId,
                s.hasInitialAngle ? (float?)s.initialAngleRad : null)).ToList(),
            TrailStyle: useTrailStyle ? trailStyle.ToString() : null);

        [System.Serializable] public class SerializableBlock { public int col; public int row; public string definitionId = "basic"; }
        [System.Serializable] public class SerializableBorder { public int col; public int row; public BorderOrientation orientation; }
        [System.Serializable] public class SerializableDoor { public int col; public string spinnerDefinitionId = "spinner_cube"; }
        [System.Serializable] public class SerializableSpinner
        {
            public float x;
            public float y;
            public string definitionId = "spinner_cube";
            public bool hasInitialAngle;
            public float initialAngleRad;
        }
    }
}
