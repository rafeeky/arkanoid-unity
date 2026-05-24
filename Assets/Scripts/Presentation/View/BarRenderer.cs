using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 단일 바. SpriteRenderer 의 transform.position + localScale.x 동기화.
    // Effect 색은 Material.color 또는 _effectIndicator SpriteRenderer.color 로.
    public sealed class BarRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer effectIndicator;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color expandColor = new(0.3f, 0.7f, 1f, 1f);
        [SerializeField] private Color magnetColor = new(1f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color laserColor = new(1f, 0.3f, 0.3f, 1f);

        // Sprite native width 가 1 px 단위라고 가정 (D3.4 1 unit = 1 px). 다르면 referenceWidth 로 보정.
        [SerializeField] private float referenceWidthPx = 120f;

        public void Bind(BarState bar)
        {
            transform.position = new Vector3(bar.X, bar.Y, 0f);
            if (body != null)
            {
                var scale = body.transform.localScale;
                scale.x = bar.Width / referenceWidthPx;
                body.transform.localScale = scale;
            }
            if (effectIndicator != null)
            {
                effectIndicator.color = bar.ActiveEffect switch
                {
                    BarEffect.Expand => expandColor,
                    BarEffect.Magnet => magnetColor,
                    BarEffect.Laser => laserColor,
                    _ => baseColor,
                };
            }
        }
    }
}
