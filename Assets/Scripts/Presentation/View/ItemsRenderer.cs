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

                _instances[i].transform.position = new Vector3(it.X, it.Y, 0f);
                if (_sprites[i] != null)
                {
                    _sprites[i].sprite = it.ItemType switch
                    {
                        ItemType.Expand => expandSprite,
                        ItemType.Magnet => magnetSprite,
                        ItemType.Laser => laserSprite,
                        _ => null,
                    };
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
