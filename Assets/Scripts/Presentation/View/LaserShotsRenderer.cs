using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 레이저 발사체 Pool.
    public sealed class LaserShotsRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private Transform poolRoot;

        private readonly List<GameObject> _instances = new();

        public void Bind(IReadOnlyList<LaserShotState> shots)
        {
            EnsureCapacity(shots.Count);
            for (int i = 0; i < shots.Count; i++)
            {
                _instances[i].SetActive(true);
                _instances[i].transform.localPosition = new Vector3(shots[i].X, shots[i].Y, 0f);
            }
            for (int i = shots.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(laserPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
            }
        }
    }
}
