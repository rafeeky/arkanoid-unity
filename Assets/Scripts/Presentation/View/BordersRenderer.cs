using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 정적 테두리 — 처음 Bind 호출 시 한 번 인스턴스화. 이후 변화 없음.
    public sealed class BordersRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject borderPrefab;
        [SerializeField] private Transform poolRoot;

        private readonly List<GameObject> _instances = new();
        private int _lastCount = -1;

        public void Bind(IReadOnlyList<BorderBlockState> borders)
        {
            if (borders.Count == _lastCount) return;  // 변화 없음 — early out.
            _lastCount = borders.Count;

            EnsureCapacity(borders.Count);
            for (int i = 0; i < borders.Count; i++)
            {
                var b = borders[i];
                _instances[i].SetActive(true);
                _instances[i].transform.position = new Vector3(b.X, b.Y, 0f);
            }
            for (int i = borders.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(borderPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
            }
        }
    }
}
