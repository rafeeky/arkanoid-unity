using System;
using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // Spinner 인스턴스 Pool. Phase 별 ghost(반투명) 처리.
    // AngleRad 는 transform.rotation 으로 변환 (radian → degrees).
    // DefinitionId (cube / triangle) 별로 sprite swap.
    public sealed class SpinnersRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject spinnerPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private Color solidColor = Color.white;
        [SerializeField] private Color ghostColor = new(1f, 1f, 1f, 0.4f);
        // SpinnerDefinition.size 와 동일 (Stage SO 의 spinner size 48px 매칭). sprite native px → 이 값으로 fit.
        [SerializeField] private float spinnerSizePx = 48f;

        [Serializable]
        public struct SpriteEntry
        {
            public string DefinitionId;
            public Sprite Sprite;
        }
        [SerializeField] private SpriteEntry[] spriteEntries;

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
                go.transform.localPosition = new Vector3(s.X, s.Y, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, s.AngleRad * Mathf.Rad2Deg);

                if (_sprites[i] != null)
                {
                    _sprites[i].color = s.Phase == SpinnerPhase.Circling ? solidColor : ghostColor;
                    var sprite = ResolveSprite(s.DefinitionId);
                    if (sprite != null)
                    {
                        _sprites[i].sprite = sprite;
                        // sprite native px → spinnerSizePx(48) 로 fit. rotation 은 별도 처리됨.
                        var nw = sprite.rect.width;
                        var nh = sprite.rect.height;
                        var sx = nw > 0f ? spinnerSizePx / nw : 1f;
                        var sy = nh > 0f ? spinnerSizePx / nh : 1f;
                        var ls = go.transform.localScale;
                        go.transform.localScale = new Vector3(sx, sy, 1f);
                    }
                }
            }
            for (int i = spinners.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private Sprite ResolveSprite(string definitionId)
        {
            if (spriteEntries == null || string.IsNullOrEmpty(definitionId)) return null;
            for (int i = 0; i < spriteEntries.Length; i++)
                if (spriteEntries[i].DefinitionId == definitionId) return spriteEntries[i].Sprite;
            return null;
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
