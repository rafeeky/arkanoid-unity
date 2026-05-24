using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 블록 인스턴스 Pool. BlockDefinition 색/내구도 시각화.
    // BlockHitFlashBlockIds (ScreenState) 로 hit 시 깜빡임.
    public sealed class BlocksRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private List<BlockDefinitionSO> blockDefinitionSOs = new();

        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color defaultColor = new(0.6f, 0.6f, 0.6f, 1f);

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();
        private readonly Dictionary<string, BlockDefinition> _defs = new();
        private bool _defsInitialized;

        public void Bind(IReadOnlyList<BlockState> blocks, IReadOnlyList<string> flashIds)
        {
            EnsureDefs();
            EnsureCapacity(blocks.Count);

            var flashSet = new HashSet<string>(flashIds);

            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                var go = _instances[i];
                var visible = !b.IsDestroyed;
                go.SetActive(visible);
                if (!visible) continue;

                go.transform.position = new Vector3(b.X, b.Y, 0f);
                if (_sprites[i] == null) continue;

                if (flashSet.Contains(b.Id))
                {
                    _sprites[i].color = flashColor;
                }
                else
                {
                    _sprites[i].color = _defs.TryGetValue(b.DefinitionId, out var def)
                        ? HexToColor(def.BaseColor)
                        : defaultColor;
                }
            }

            for (int i = blocks.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureDefs()
        {
            if (_defsInitialized) return;
            _defsInitialized = true;
            foreach (var so in blockDefinitionSOs)
            {
                if (so == null) continue;
                var d = so.Data;
                _defs[d.DefinitionId] = d;
            }
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(blockPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }

        private static Color HexToColor(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
