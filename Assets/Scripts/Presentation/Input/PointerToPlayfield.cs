using UnityEngine;
using UnityEngine.InputSystem;
using Arkanoid.Definitions.SO;

namespace Arkanoid.Presentation
{
    // Pointer (마우스/터치) 화면 좌표 → playfield 좌표 변환.
    // 카메라 ScreenToWorldPoint 후 playfield offset 만큼 빼서 [0..width] 범위로 보정.
    //
    // World 좌표계 (D3.4 1 unit = 1 px, canvas 1080x1920):
    //   - PlayfieldRoot 가 world (offsetX, canvas.height - offsetY) 에 anchor,
    //     scale.y = -1 로 gameplay (x, y) → world (offsetX + x, canvas.height - offsetY - y).
    //   - 따라서 world.x 에서 offsetX 를 빼면 gameplay X (0..playfield.width) 가 나온다.
    public sealed class PointerToPlayfield
    {
        private readonly Camera _camera;
        private readonly float _offsetX;

        // layoutConfig 미지정 시 기본값 (LayoutConfigTable 와 동일: offsetX=180).
        public PointerToPlayfield(Camera camera, LayoutConfigSO layoutConfig)
        {
            _camera = camera;
            _offsetX = layoutConfig != null ? layoutConfig.Data.Playfield.OffsetX : 180f;
        }

        public PointerToPlayfield(Camera camera) : this(camera, null) { }

        // 드래그 중인 경우 *playfield x* 반환 (gameplay 좌표 0..width).
        // 드래그 안 한 경우 null. (BarState.X 가 0~720 범위이므로 동일 단위.)
        public float? GetPlayfieldX()
        {
            var pointer = Pointer.current;
            if (pointer == null) return null;
            if (!pointer.press.isPressed) return null;  // 드래그 아님

            var screenPos = pointer.position.ReadValue();
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            return world.x - _offsetX;
        }
    }
}
