using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // Ball 인스턴스 Pool — Bind(state.Balls) 로 풀에서 꺼내 위치 동기화.
    // ballPrefab 은 SpriteRenderer 가 붙은 공 prefab.
    public sealed class BallsRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform poolRoot;

        // poweredTint 는 공이 IsPowered=true 일 때 색 변화.
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color poweredColor = new(1f, 0.9f, 0.4f, 1f);

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();

        public void Bind(IReadOnlyList<BallState> balls)
        {
            EnsureCapacity(balls.Count);

            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                var go = _instances[i];
                go.SetActive(b.IsActive);
                if (b.IsActive)
                {
                    go.transform.position = new Vector3(b.X, b.Y, 0f);
                    if (_sprites[i] != null)
                        _sprites[i].color = (b.IsPowered ?? false) ? poweredColor : normalColor;
                }
            }

            for (int i = balls.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(ballPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }
}
