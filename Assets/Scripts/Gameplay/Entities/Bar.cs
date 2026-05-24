using Arkanoid.Definitions;
using System;

namespace Arkanoid.Gameplay
{
    // (Bar.X, Bar.Y) = 바 *중심*. Bar.Width 는 가변 (expand 효과).
    public static class Bar
    {
        // 바의 collision AABB. 중심 → 좌상단 변환.
        public static Bounds GetBounds(BarState bar) =>
            new(bar.X - bar.Width / 2f,
                bar.Y - PlayfieldLayout.BarHeight / 2f,
                bar.Width,
                PlayfieldLayout.BarHeight);

        // 바에 닿은 공 반사.
        // - 어떤 입사각이든 위로 튕김 (vy = -|enforced.vy| — TS Y+ 아래 좌표계, 부호 = 위).
        // - vx = contactX × speed × physics.BarContactBias.
        // - 최소 vy magnitude (speed × 0.3) 보장해 수평 트래핑 방지.
        public static BallState ReflectFromBall(BallState ball, BallHitBarFact fact, PhysicsConfig physics)
        {
            var speed = MathF.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            var rawVx = fact.BarContactX * speed * physics.BarContactBias;
            var minVy = speed * 0.3f;
            var vyMagnitude = MathF.Sqrt(MathF.Max(speed * speed - rawVx * rawVx, minVy * minVy));
            var enforced = CollisionResolutionService.EnforceMinAngle(rawVx, -vyMagnitude, physics.MinAngleDeg);
            return ball with { Vx = enforced.Vx, Vy = -MathF.Abs(enforced.Vy) };
        }

        // 바에 공 부착 (자석 효과). 공은 바 위 표면 바로 위, x 오프셋 저장 (발사 위치 복원).
        public static BallState AttachBall(BallState ball, BarState bar) =>
            ball with
            {
                Y = bar.Y - PlayfieldLayout.BarHeight / 2f - PlayfieldLayout.BallRadius,
                Vx = 0f,
                Vy = 0f,
                IsActive = false,
                AttachedOffsetX = ball.X - bar.X,
            };
    }
}
