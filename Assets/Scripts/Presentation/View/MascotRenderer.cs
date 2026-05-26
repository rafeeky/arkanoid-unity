using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;

namespace Arkanoid.Presentation.View
{
    // 마스코트 — 4 frame 픽셀 애니메이션 (200ms 간격). MascotId 별 sprite 4장 set.
    // TS renderer/inGame/renderMascot.ts 의 mascot.<id>.frame<0..3> 패턴과 1:1.
    public sealed class MascotRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MascotSO mascotSO;

        [System.Serializable]
        public struct Entry
        {
            public string MascotId;
            public Sprite Frame0;
            public Sprite Frame1;
            public Sprite Frame2;
            public Sprite Frame3;
        }

        [SerializeField] private Entry[] mascotSprites;
        [SerializeField] private float frameIntervalSec = 0.2f;

        // 표시 크기 (px). Layout.mascotSize=200 와 동일.
        [SerializeField] private float mascotSizePx = 200f;
        // 좌우 반전 (TS 와 동일 — 캐릭터가 왼쪽 바라봄). PlayfieldRoot scale.y=-1 영향
        // 안 받게 sprite 자체만 flip.
        [SerializeField] private bool flipX = true;

        private string _currentId;
        private Entry? _currentEntry;
        private float _accumTime;
        private int _frameIdx;

        private void Update()
        {
            if (spriteRenderer == null || _currentEntry == null) return;
            _accumTime += Time.deltaTime;
            if (_accumTime >= frameIntervalSec)
            {
                _accumTime = 0f;
                _frameIdx = (_frameIdx + 1) % 4;
                ApplyFrame();
            }
        }

        public void SetMascot(string mascotId)
        {
            if (string.IsNullOrEmpty(mascotId)) return;
            if (mascotId == _currentId) return;
            _currentId = mascotId;

            _currentEntry = null;
            if (spriteRenderer == null || mascotSprites == null) return;
            foreach (var e in mascotSprites)
            {
                if (e.MascotId == mascotId)
                {
                    _currentEntry = e;
                    _frameIdx = 0;
                    _accumTime = 0f;
                    spriteRenderer.flipX = flipX;
                    ApplyFrame();
                    ApplyScale();
                    return;
                }
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null || _currentEntry == null) return;
            var e = _currentEntry.Value;
            Sprite f = _frameIdx switch
            {
                0 => e.Frame0,
                1 => e.Frame1,
                2 => e.Frame2,
                _ => e.Frame3,
            };
            if (f != null) spriteRenderer.sprite = f;
        }

        private void ApplyScale()
        {
            if (spriteRenderer == null || _currentEntry == null) return;
            var sp = spriteRenderer.sprite;
            if (sp == null) return;
            // sprite native size 가 480 등이라 mascotSizePx 에 맞춰 scale.
            var nw = sp.rect.width;
            var nh = sp.rect.height;
            var sx = nw > 0f ? mascotSizePx / nw : 1f;
            var sy = nh > 0f ? mascotSizePx / nh : 1f;
            spriteRenderer.transform.localScale = new Vector3(sx, sy, 1f);
        }
    }
}
