using UnityEngine;
using UnityEngine.InputSystem;
using Arkanoid.Definitions.SO;

namespace Arkanoid.Presentation
{
    // Pointer (마우스/터치) 화면 좌표 → playfield 좌표 변환.
    // gameplay x = world.x - PlayfieldRoot.position.x.
    //   PlayfieldRoot 가 (0,0) 이면 그대로 world.x. LayoutConfig offsetX 를 가정한 옛 동작은
    //   PlayfieldRoot world position 과 다르면 입력이 어긋나므로, Transform 을 직접 사용.
    public sealed class PointerToPlayfield
    {
        private readonly Camera _camera;
        private readonly Transform _playfieldRoot;
        private readonly float _fallbackOffsetX;

        public PointerToPlayfield(Camera camera, Transform playfieldRoot, LayoutConfigSO layoutConfig = null)
        {
            _camera = camera;
            _playfieldRoot = playfieldRoot;
            _fallbackOffsetX = layoutConfig != null ? layoutConfig.Data.Playfield.OffsetX : 0f;
        }

        // 호환 생성자 — playfieldRoot 없이 LayoutConfig 만 (옛 호출부용).
        public PointerToPlayfield(Camera camera, LayoutConfigSO layoutConfig)
            : this(camera, null, layoutConfig) { }
        public PointerToPlayfield(Camera camera) : this(camera, null, null) { }

        public float? GetPlayfieldX()
        {
            var pointer = Pointer.current;
            if (pointer == null) return null;
            if (!pointer.press.isPressed) return null;

            var screenPos = pointer.position.ReadValue();
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            var rootX = _playfieldRoot != null ? _playfieldRoot.position.x : _fallbackOffsetX;
            return world.x - rootX;
        }
    }
}
