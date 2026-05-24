using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // Door 인스턴스 Pool. Phase 별 시각 표시.
    // Closed → 표시, Opening → x 좌측 슬라이드 보간, Opened → 비표시.
    public sealed class DoorsRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private Transform poolRoot;

        // Opening 애니: 0~400ms 사이 x 가 startX → startX - slideDistance 보간.
        [SerializeField] private float openingDurationMs = 400f;
        [SerializeField] private float slideDistance = 40f;

        private readonly List<GameObject> _instances = new();

        public void Bind(IReadOnlyList<DoorState> doors)
        {
            EnsureCapacity(doors.Count);
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                var visible = d.Phase != DoorPhase.Opened;
                _instances[i].SetActive(visible);
                if (!visible) continue;

                var x = d.X;
                if (d.Phase == DoorPhase.Opening)
                {
                    var t = Mathf.Clamp01(d.OpeningElapsedMs / openingDurationMs);
                    x = d.X - slideDistance * t;
                }
                _instances[i].transform.position = new Vector3(x, d.Y, 0f);
            }
            for (int i = doors.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(doorPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
            }
        }
    }
}
