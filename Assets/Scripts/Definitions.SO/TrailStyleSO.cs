using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    // TRAIL_STYLE_TABLE — TrailStyleId 별 TrailStyle config.
    [CreateAssetMenu(fileName = "TrailStyleTable", menuName = "Arkanoid/Presentation/Trail Style Table")]
    public sealed class TrailStyleSO : ScriptableObject
    {
        [SerializeField] private SerializableEntry goldenSun = new();
        [SerializeField] private SerializableEntry blueMeteor = new();
        [SerializeField] private SerializableEntry sunset = new();

        public IReadOnlyDictionary<TrailStyleId, TrailStyle> Data => new Dictionary<TrailStyleId, TrailStyle>
        {
            [TrailStyleId.GoldenSun] = goldenSun.ToStyle(),
            [TrailStyleId.BlueMeteor] = blueMeteor.ToStyle(),
            [TrailStyleId.Sunset] = sunset.ToStyle(),
        };

        [System.Serializable]
        public class SerializableEntry
        {
            public int headColor;
            public int tailColor;
            public int glowColor;
            public int segmentCount = 12;
            public float headAlpha = 1f;
            public float segmentRadius = 8f;
            public float pushIntervalMs = 16f;

            public TrailStyle ToStyle() =>
                new(headColor, tailColor, glowColor, segmentCount, headAlpha, segmentRadius, pushIntervalMs);
        }
    }
}
