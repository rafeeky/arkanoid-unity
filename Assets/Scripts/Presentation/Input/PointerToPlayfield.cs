using UnityEngine;
using UnityEngine.InputSystem;

namespace Arkanoid.Presentation
{
    // Pointer (마우스/터치) 화면 좌표 → playfield 좌표 변환.
    // 카메라 ScreenToWorldPoint 사용. 드래그 중이 아니면 null 반환.
    public sealed class PointerToPlayfield
    {
        private readonly Camera _camera;

        public PointerToPlayfield(Camera camera)
        {
            _camera = camera;
        }

        // 드래그 중인 경우 *playfield x* 반환 (TS 좌표 기준 — Y flip 은 별도 카메라 설정).
        // Phase 3 의 카메라 D3.4 결정: 1 unit = 1 px, world 좌표 == TS 좌표.
        public float? GetPlayfieldX()
        {
            var pointer = Pointer.current;
            if (pointer == null) return null;
            if (!pointer.press.isPressed) return null;  // 드래그 아님

            var screenPos = pointer.position.ReadValue();
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            return world.x;
        }
    }
}
