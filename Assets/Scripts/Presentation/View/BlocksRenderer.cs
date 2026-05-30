using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 블록 인스턴스 Pool. BlockDefinition 별 sprite swap (TS 의 PNG 패턴과 동일).
    // Sprite 가 없으면 BaseColor tint 만 적용 (구버전 placeholder fallback).
    // BlockHitFlashBlockIds (ScreenState) 로 hit 시 깜빡임.
    public sealed class BlocksRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private List<BlockDefinitionSO> blockDefinitionSOs = new();

        // definitionId → sprite 매핑 (TS public/assets/blocks/block_<visualId>.png 와 1:1).
        [System.Serializable]
        public struct SpriteEntry { public string DefinitionId; public Sprite Sprite; }
        [SerializeField] private SpriteEntry[] spriteEntries = new SpriteEntry[0];

        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color defaultColor = new(0.6f, 0.6f, 0.6f, 1f);

        // 한 블럭의 표시 크기 (px). Block prefab 의 scale 대신 코드에서 통일 (Pool 인스턴스의 scale 을 강제).
        [SerializeField] private float blockWidthPx = 64f;
        [SerializeField] private float blockHeightPx = 24f;

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();
        private readonly Dictionary<string, BlockDefinition> _defs = new();
        private readonly Dictionary<string, Sprite> _spriteMap = new();
        private bool _defsInitialized;

        public void Bind(IReadOnlyList<BlockState> blocks, IReadOnlyList<string> flashIds)
        {
            EnsureMaps();
            EnsureCapacity(blocks.Count);

            var flashSet = new HashSet<string>(flashIds);

            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                var go = _instances[i];
                var visible = !b.IsDestroyed;
                go.SetActive(visible);
                if (!visible) continue;

                // TS renderBlocks 는 setOrigin(0,0) 좌상단 기준. Unity sprite pivot 은 중앙 →
                // 중앙을 (b.X + w/2, b.Y + h/2) 로 놓아 TS 와 동일 영역 차지.
                go.transform.localPosition = new Vector3(b.X + blockWidthPx / 2f, b.Y + blockHeightPx / 2f, 0f);
                if (_sprites[i] == null) continue;

                // sprite swap — TS 처럼 PNG 색상이 sprite 자체에 들어 있음.
                if (_spriteMap.TryGetValue(b.DefinitionId, out var sp) && sp != null)
                {
                    _sprites[i].sprite = sp;
                    // sprite 자체 색을 그대로 보이게 white tint (flash 가 아니면).
                    if (flashSet.Contains(b.Id)) _sprites[i].color = flashColor;
                    else _sprites[i].color = Color.white;
                    // 표시 크기 — sprite native size 가 750×286 등 다양하므로 prefab scale 로 boxFit.
                    var nw = sp.rect.width;
                    var nh = sp.rect.height;
                    var sx = nw > 0f ? blockWidthPx / nw : 1f;
                    var sy = nh > 0f ? blockHeightPx / nh : 1f;
                    go.transform.localScale = new Vector3(sx, sy, 1f);
                }
                else
                {
                    // sprite 없으면 fallback: 회색 사각 (구버전 동작).
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
            }

            for (int i = blocks.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureMaps()
        {
            if (_defsInitialized) return;
            _defsInitialized = true;
            foreach (var so in blockDefinitionSOs)
            {
                if (so == null) continue;
                var d = so.Data;
                _defs[d.DefinitionId] = d;
            }
            foreach (var e in spriteEntries)
            {
                if (string.IsNullOrEmpty(e.DefinitionId) || e.Sprite == null) continue;
                _spriteMap[e.DefinitionId] = e.Sprite;
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
