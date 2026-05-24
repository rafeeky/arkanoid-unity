using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public enum BorderSide { Top, Bottom, Left, Right }

    // 깨지지 않는 벽 — Block 과 유사한 AABB 충돌이지만 HP 감소/파괴 없음.
    // (Border.X, Border.Y) = 좌상단. Orientation 으로 폭/높이 결정.
    public static class BorderBlock
    {
        // Horizontal: BorderLength × BorderThickness
        // Vertical:   BorderThickness × BorderLength
        public static Bounds GetBounds(BorderBlockState b) =>
            b.Orientation == BorderOrientation.Horizontal
                ? new Bounds(b.X, b.Y, PlayfieldLayout.BorderLength, PlayfieldLayout.BorderThickness)
                : new Bounds(b.X, b.Y, PlayfieldLayout.BorderThickness, PlayfieldLayout.BorderLength);

        // Push-out + 반사. HP 감소 없음.
        public static BallState HandleBallCollision(BallState ball, BorderSide side, BorderBlockState border, PhysicsConfig physics)
        {
            var bounds = GetBounds(border);
            var x = ball.X;
            var y = ball.Y;
            switch (side)
            {
                case BorderSide.Top:
                    y = border.Y - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case BorderSide.Bottom:
                    y = border.Y + bounds.Height + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
                case BorderSide.Left:
                    x = border.X - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case BorderSide.Right:
                    x = border.X + bounds.Width + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
            }
            var vx = ball.Vx;
            var vy = ball.Vy;
            if (side == BorderSide.Left || side == BorderSide.Right)
                vx = -vx;
            else
                vy = -vy;
            var enforced = CollisionResolutionService.EnforceMinAngle(vx, vy, physics.MinAngleDeg);
            return ball with { X = x, Y = y, Vx = enforced.Vx, Vy = enforced.Vy };
        }
    }
}
