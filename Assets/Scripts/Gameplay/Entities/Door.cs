using Arkanoid.Definitions;
using System;

namespace Arkanoid.Gameplay
{
    public enum DoorSide { Top, Bottom, Left, Right }

    // 상단 테두리 위 게이트. 공은 모든 phase 에서 차단 (스피너만 통과).
    public static class Door
    {
        // opening phase 총 길이 (ms). 디자이너 튜닝 시 config 로 이관 고려.
        public const float OpeningDurationMs = 600f;

        // 공이 항상 차단되므로 phase 무관 풀 사이즈.
        public static Bounds GetBounds(DoorState d) =>
            new(d.X, d.Y, PlayfieldLayout.BorderLength, PlayfieldLayout.BorderThickness);

        // 공 차단 여부 — opened 에서도 공은 차단 (스피너만 통과, 사용자 결정).
        public static bool BlocksBall(DoorState _) => true;

        // opening 진행도 (0..1). closed=0, opened=1.
        public static float OpeningProgress(DoorState d) => d.Phase switch
        {
            DoorPhase.Closed => 0f,
            DoorPhase.Opened => 1f,
            _ => MathF.Min(1f, d.OpeningElapsedMs / OpeningDurationMs),
        };

        // 매 틱 호출. closed → opening (즉시) → opened (elapsed >= duration).
        public static DoorState TickAnimation(DoorState d, float dtMs)
        {
            switch (d.Phase)
            {
                case DoorPhase.Closed:
                    return d with { Phase = DoorPhase.Opening, OpeningElapsedMs = 0f };
                case DoorPhase.Opening:
                    {
                        var elapsed = d.OpeningElapsedMs + dtMs;
                        if (elapsed >= OpeningDurationMs)
                            return d with { Phase = DoorPhase.Opened, OpeningElapsedMs = OpeningDurationMs };
                        return d with { OpeningElapsedMs = elapsed };
                    }
                case DoorPhase.Opened:
                default:
                    return d;
            }
        }

        // 스피너 차단 여부. opening 까지는 차단, opened 부터 통과.
        public static bool BlocksSpinner(DoorState d) => d.Phase != DoorPhase.Opened;

        // 공 충돌 응답. BorderBlock 과 동일 구조 (horizontal AABB).
        public static BallState HandleBallCollision(BallState ball, DoorSide side, DoorState door, PhysicsConfig physics)
        {
            var x = ball.X;
            var y = ball.Y;
            switch (side)
            {
                case DoorSide.Top:
                    y = door.Y - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case DoorSide.Bottom:
                    y = door.Y + PlayfieldLayout.BorderThickness + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
                case DoorSide.Left:
                    x = door.X - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case DoorSide.Right:
                    x = door.X + PlayfieldLayout.BorderLength + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
            }
            var vx = ball.Vx;
            var vy = ball.Vy;
            if (side == DoorSide.Left || side == DoorSide.Right)
                vx = -vx;
            else
                vy = -vy;
            var enforced = CollisionResolutionService.EnforceMinAngle(vx, vy, physics.MinAngleDeg);
            return ball with { X = x, Y = y, Vx = enforced.Vx, Vy = enforced.Vy };
        }
    }
}
