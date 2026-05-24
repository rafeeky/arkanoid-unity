using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // Spinner 인스턴스 Pool. Phase 별 ghost(반투명) 처리.
    // AngleRad 는 transform.rotation 으로 변환 (radian → degrees).
    public sealed class SpinnersRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject spinnerPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private Color solidColor = Color.white;
        [SerializeField] private Color ghostColor = new(1f, 1f, 1f, 0.4f);

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();

        public void Bind(IReadOnlyList<SpinnerRuntimeState> spinners)
        {
            EnsureCapacity(spinners.Count);
            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                var go = _instances[i];
                go.SetActive(true);
                go.transform.position = new Vector3(s.X, s.Y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, s.AngleRad * Mathf.Rad2Deg);

                if (_sprites[i] != null)
                {
                    _sprites[i].color = s.Phase == SpinnerPhase.Circling ? solidColor : ghostColor;
                }
            }
            for (int i = spinners.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(spinnerPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }
}
