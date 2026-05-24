using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    // 플레이필드 경계. Unity 매핑: PlayfieldRoot 의 자식 BoxCollider2D 들 (Phase 3).
    // 현재 Phase 1 에선 함수 모듈로 표현.
    public static class Wall
    {
        // 벽 반사 + 경계 안쪽으로 push-out. top wall 만 — floor 는 별도 (BallHitFloor — life lost).
        public static BallState ReflectFromBall(BallState ball, BallHitWallFact fact, PhysicsConfig physics)
        {
            var vx = ball.Vx;
            var vy = ball.Vy;
            var x = ball.X;
            var y = ball.Y;

            switch (fact.Side)
            {
                case WallSide.Left:
                    vx = -vx;
                    x = PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
                case WallSide.Right:
                    vx = -vx;
                    x = PlayfieldLayout.PlayfieldWidth - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case WallSide.Top:
                    vy = -vy;
                    y = PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
            }

            var enforced = CollisionResolutionService.EnforceMinAngle(vx, vy, physics.MinAngleDeg);
            return ball with { X = x, Y = y, Vx = enforced.Vx, Vy = enforced.Vy };
        }
    }
}
