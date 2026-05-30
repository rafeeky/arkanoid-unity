using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 정적 테두리 — vertical/horizontal sprite 매핑.
    // BorderBlockState.Orientation 에 따라 sprite swap + native size 자동 fit.
    public sealed class BordersRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject borderPrefab;
        [SerializeField] private Transform poolRoot;

        [SerializeField] private Sprite verticalSprite;
        [SerializeField] private Sprite horizontalSprite;

        // PlayfieldLayout: BorderLength=60, BorderThickness=12.
        [SerializeField] private float borderLengthPx = 60f;
        [SerializeField] private float borderThicknessPx = 12f;

        private readonly List<GameObject> _instances = new();
        private readonly List<SpriteRenderer> _sprites = new();

        public void Bind(IReadOnlyList<BorderBlockState> borders)
        {
            EnsureCapacity(borders.Count);
            for (int i = 0; i < borders.Count; i++)
            {
                var b = borders[i];
                _instances[i].SetActive(true);

                var sr = _sprites[i];
                if (sr == null) continue;

                bool vertical = b.Orientation == BorderOrientation.Vertical;
                var sprite = vertical ? verticalSprite : horizontalSprite;
                float targetW = vertical ? borderThicknessPx : borderLengthPx;
                float targetH = vertical ? borderLengthPx : borderThicknessPx;
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    var nw = sprite.rect.width;
                    var nh = sprite.rect.height;
                    _instances[i].transform.localScale = new Vector3(
                        nw > 0f ? targetW / nw : 1f,
                        nh > 0f ? targetH / nh : 1f,
                        1f);
                }
                // TS renderBorders 는 setOrigin(0,0) (좌상단 기준). Unity sprite pivot 은 중앙 →
                // sprite 중앙을 (b.X + w/2, b.Y + h/2) 로 놓아 TS 와 동일한 영역을 차지하게 보정.
                _instances[i].transform.localPosition = new Vector3(b.X + targetW / 2f, b.Y + targetH / 2f, 0f);
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
                _sprites.Add(go.GetComponentInChildren<SpriteRenderer>());
            }
        }
    }
}
