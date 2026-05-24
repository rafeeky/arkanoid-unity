using UnityEngine;
using UnityEngine.InputSystem;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    // 매 프레임 Unity Input System 의 Keyboard/Pointer 상태를 읽어 InputSnapshot 생성.
    // Phase 4 의 .inputactions 자산 도입 전 단순 형태. 추후 InputAction 기반으로 교체 가능.
    //
    // 사용: GameManager 가 매 Update() 에서 Build(canvasRect) 호출 → GameplayController.Tick / GameFlowController.HandleInput.
    public sealed class UnityInputSnapshotBuilder
    {
        // edge detection 위한 *직전 프레임 상태*.
        private bool _prevSpace;
        private bool _prevQ;
        private bool _prevEsc;
        private bool _prevLeftArrow;
        private bool _prevRightArrow;

        // 매 프레임 호출. PointerWorldX → 좌표 변환 결과 (null = 드래그 미사용).
        public InputSnapshot Build(float? pointerWorldX = null)
        {
            var kb = Keyboard.current;

            var space = kb?.spaceKey.isPressed ?? false;
            var q = kb?.qKey.isPressed ?? false;
            var esc = kb?.escapeKey.isPressed ?? false;
            var left = kb?.aKey.isPressed == true || kb?.leftArrowKey.isPressed == true;
            var right = kb?.dKey.isPressed == true || kb?.rightArrowKey.isPressed == true;
            var leftArrow = kb?.leftArrowKey.isPressed ?? false;
            var rightArrow = kb?.rightArrowKey.isPressed ?? false;

            // edge: 직전 false → 현재 true.
            var spaceJust = space && !_prevSpace;
            var qJust = q && !_prevQ;
            var escJust = esc && !_prevEsc;
            var leftJust = leftArrow && !_prevLeftArrow;
            var rightJust = rightArrow && !_prevRightArrow;

            _prevSpace = space;
            _prevQ = q;
            _prevEsc = esc;
            _prevLeftArrow = leftArrow;
            _prevRightArrow = rightArrow;

            return new InputSnapshot(
                LeftDown: left,
                RightDown: right,
                SpaceJustPressed: spaceJust,
                QJustPressed: qJust,
                EscJustPressed: escJust,
                LeftJustPressed: leftJust,
                RightJustPressed: rightJust,
                TargetBarX: pointerWorldX);
        }
    }
}
