using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;

namespace Arkanoid.Presentation.View
{
    // 마스코트 단일 SpriteRenderer. MascotId 별 sprite swap.
    public sealed class MascotRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MascotSO mascotSO;

        [System.Serializable]
        private struct Entry { public string MascotId; public Sprite Sprite; }

        [SerializeField] private Entry[] mascotSprites;

        private string _currentId;

        public void SetMascot(string mascotId)
        {
            if (mascotId == _currentId) return;
            _currentId = mascotId;

            if (spriteRenderer == null || mascotSprites == null) return;
            foreach (var e in mascotSprites)
            {
                if (e.MascotId == mascotId)
                {
                    spriteRenderer.sprite = e.Sprite;
                    return;
                }
            }
        }
    }
}
