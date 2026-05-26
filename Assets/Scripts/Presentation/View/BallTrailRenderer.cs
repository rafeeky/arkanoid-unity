using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 공별 trail. Unity TrailRenderer 1:1 매핑 — IsPowered 일 때만 emitting.
    // BallsRenderer 와 비슷한 Pool. TrailStyleSO (단일) 에서 TrailStyleId enum 으로 lookup.
    public sealed class BallTrailRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject trailPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private TrailStyleSO trailStyleSO;

        private readonly List<GameObject> _instances = new();
        private readonly List<TrailRenderer> _trails = new();

        public void Bind(IReadOnlyList<BallState> balls, string currentStyleId)
        {
            EnsureCapacity(balls.Count);

            TrailStyle? activeStyle = ResolveStyle(currentStyleId);

            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                var powered = b.IsPowered ?? false;
                var go = _instances[i];
                go.SetActive(b.IsActive);
                if (!b.IsActive) continue;

                go.transform.localPosition = new Vector3(b.X, b.Y, 0f);
                if (_trails[i] != null)
                {
                    _trails[i].emitting = powered;
                    if (powered && activeStyle.HasValue)
                    {
                        var st = activeStyle.Value;
                        _trails[i].startColor = HexToColor(st.HeadColor, st.HeadAlpha);
                        _trails[i].endColor = HexToColor(st.TailColor, 0f);
                        _trails[i].time = (st.PushIntervalMs * st.SegmentCount) / 1000f;
                        _trails[i].startWidth = st.SegmentRadius * 2f;
                    }
                }
            }
            for (int i = balls.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private TrailStyle? ResolveStyle(string id)
        {
            if (trailStyleSO == null || string.IsNullOrEmpty(id)) return null;
            if (!System.Enum.TryParse<TrailStyleId>(id, true, out var styleEnum)) return null;
            return trailStyleSO.Data.TryGetValue(styleEnum, out var st) ? st : (TrailStyle?)null;
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(trailPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
                _trails.Add(go.GetComponentInChildren<TrailRenderer>());
            }
        }

        private static Color HexToColor(int hex, float a)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float bl = (hex & 0xFF) / 255f;
            return new Color(r, g, bl, a);
        }
    }
}
