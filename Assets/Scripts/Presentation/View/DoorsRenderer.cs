using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // Door 인스턴스 Pool. Phase 별 시각 표시 + sprite swap.
    // Closed → closedSprite, Opening → openingFrames[idx] (elapsed/duration 비율), Opened → 비표시.
    public sealed class DoorsRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private Transform poolRoot;

        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite[] openingFrames = new Sprite[0];

        [SerializeField] private float openingDurationMs = 400f;
        [SerializeField] private float slideDistance = 40f;

        // PlayfieldLayout: BorderLength=60 가로, BorderThickness=12 세로 (door 는 가로 방향).
        [SerializeField] private float doorWidthPx = 60f;
        [SerializeField] private float doorHeightPx = 12f;

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();

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
                Sprite sprite = closedSprite;
                if (d.Phase == DoorPhase.Opening)
                {
                    var t = Mathf.Clamp01(d.OpeningElapsedMs / openingDurationMs);
                    x = d.X - slideDistance * t;
                    if (openingFrames != null && openingFrames.Length > 0)
                    {
                        int idx = Mathf.Clamp((int)(t * openingFrames.Length), 0, openingFrames.Length - 1);
                        sprite = openingFrames[idx];
                    }
                }
                _instances[i].transform.localPosition = new Vector3(x, d.Y, 0f);

                var sr = _sprites[i];
                if (sr != null && sprite != null)
                {
                    sr.sprite = sprite;
                    var nw = sprite.rect.width;
                    var nh = sprite.rect.height;
                    _instances[i].transform.localScale = new Vector3(
                        nw > 0f ? doorWidthPx / nw : 1f,
                        nh > 0f ? doorHeightPx / nh : 1f,
                        1f);
                }
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
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }
}
