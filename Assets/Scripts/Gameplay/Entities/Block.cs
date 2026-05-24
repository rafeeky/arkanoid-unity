using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public enum BlockSide { Top, Bottom, Left, Right }

    // (Block.X, Block.Y) = 블럭 좌상단 (TS prototype 한정).
    // bounds() + reflectFromBall() 이 collision SSOT — 다른 곳에서 inline 반사 금지.
    public static class Block
    {
        public static Bounds GetBounds(BlockState block) =>
            new(block.X, block.Y, PlayfieldLayout.BlockWidth, PlayfieldLayout.BlockHeight);

        // 속도 벡터 flip + EnforceMinAngle. 위치 push-out 은 포함 X (swept 외 fallback 용).
        public static BallState ReflectFromBall(BallState ball, BlockSide side, PhysicsConfig physics)
        {
            var vx = ball.Vx;
            var vy = ball.Vy;
            if (side == BlockSide.Left || side == BlockSide.Right)
                vx = -vx;
            else
                vy = -vy;
            var enforced = CollisionResolutionService.EnforceMinAngle(vx, vy, physics.MinAngleDeg);
            return ball with { Vx = enforced.Vx, Vy = enforced.Vy };
        }

        // 전체 응답: push-out + 반사. swept 루프 (MovementSystem) 가 사용.
        public static BallState HandleBallCollision(BallState ball, BlockSide side, BlockState block, PhysicsConfig physics)
        {
            var x = ball.X;
            var y = ball.Y;
            switch (side)
            {
                case BlockSide.Top:
                    y = block.Y - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case BlockSide.Bottom:
                    y = block.Y + PlayfieldLayout.BlockHeight + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
                case BlockSide.Left:
                    x = block.X - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                    break;
                case BlockSide.Right:
                    x = block.X + PlayfieldLayout.BlockWidth + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                    break;
            }
            return ReflectFromBall(ball with { X = x, Y = y }, side, physics);
        }
    }
}
