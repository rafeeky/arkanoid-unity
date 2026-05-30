using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // ItemDrop Pool. ItemType 별 sprite 매핑.
    public sealed class ItemsRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Transform poolRoot;

        [SerializeField] private Sprite expandSprite;
        [SerializeField] private Sprite magnetSprite;
        [SerializeField] private Sprite laserSprite;
        // TS playfieldLayout: ITEM_WIDTH=64, ITEM_HEIGHT=24 (BLOCK 과 동일). sprite native px → 이 값으로 fit.
        [SerializeField] private float itemWidthPx = 64f;
        [SerializeField] private float itemHeightPx = 24f;

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();

        public void Bind(IReadOnlyList<ItemDropState> items)
        {
            EnsureCapacity(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var visible = !it.IsCollected;
                _instances[i].SetActive(visible);
                if (!visible) continue;

                _instances[i].transform.localPosition = new Vector3(it.X, it.Y, 0f);
                if (_sprites[i] != null)
                {
                    var sp = it.ItemType switch
                    {
                        ItemType.Expand => expandSprite,
                        ItemType.Magnet => magnetSprite,
                        ItemType.Laser => laserSprite,
                        _ => null,
                    };
                    _sprites[i].sprite = sp;
                    // sprite native px → ITEM_WIDTH/HEIGHT 로 fit (TS setDisplaySize 동치)
                    if (sp != null)
                    {
                        var nw = sp.rect.width;
                        var nh = sp.rect.height;
                        _instances[i].transform.localScale = new Vector3(
                            nw > 0f ? itemWidthPx / nw : 1f,
                            nh > 0f ? itemHeightPx / nh : 1f,
                            1f);
                    }
                }
            }
            for (int i = items.Count; i < _instances.Count; i++)
                _instances[i].SetActive(false);
        }

        private void EnsureCapacity(int n)
        {
            while (_instances.Count < n)
            {
                var go = Instantiate(itemPrefab, poolRoot != null ? poolRoot : transform);
                _instances.Add(go);
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }
}
