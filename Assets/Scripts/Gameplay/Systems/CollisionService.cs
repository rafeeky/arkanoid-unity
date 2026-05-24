using System;
using System.Collections.Generic;

namespace Arkanoid.Gameplay
{
    // 충돌 감지 — state + prevState 비교로 CollisionFact 목록 생성.
    // Circle-AABB 검사. 실제 응답 (반사/score) 은 CollisionResolutionService.ApplyCollisions.
    public static class CollisionService
    {
        // ─── AABB 헬퍼 ───
        private static Bounds RectFromBar(BarState bar) =>
            new(bar.X - bar.Width / 2f,
                bar.Y - PlayfieldLayout.BarHeight / 2f,
                bar.Width,
                PlayfieldLayout.BarHeight);

        private static Bounds RectFromItem(ItemDropState item) =>
            new(item.X - PlayfieldLayout.ItemWidth / 2f,
                item.Y - PlayfieldLayout.ItemHeight / 2f,
                PlayfieldLayout.ItemWidth,
                PlayfieldLayout.ItemHeight);

        private static bool CircleOverlapsRect(float cx, float cy, float radius, Bounds rect)
        {
            var nearestX = MathF.Max(rect.X, MathF.Min(cx, rect.X + rect.Width));
            var nearestY = MathF.Max(rect.Y, MathF.Min(cy, rect.Y + rect.Height));
            var dx = cx - nearestX;
            var dy = cy - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool RectsOverlap(Bounds a, Bounds b) =>
            a.X < b.X + b.Width
            && a.X + a.Width > b.X
            && a.Y < b.Y + b.Height
            && a.Y + a.Height > b.Y;

        // ─── Ball detection ───
        private static void DetectBallWallCollisions(BallState ball, List<CollisionFact> facts)
        {
            if (ball.X - PlayfieldLayout.BallRadius <= 0f)
                facts.Add(new BallHitWallFact(ball.Id, WallSide.Left));
            else if (ball.X + PlayfieldLayout.BallRadius >= PlayfieldLayout.PlayfieldWidth)
                facts.Add(new BallHitWallFact(ball.Id, WallSide.Right));

            if (ball.Y - PlayfieldLayout.BallRadius <= 0f)
                facts.Add(new BallHitWallFact(ball.Id, WallSide.Top));
        }

        private static BallHitFloorFact? DetectBallFloor(BallState ball) =>
            ball.Y - PlayfieldLayout.BallRadius > PlayfieldLayout.PlayfieldHeight
                ? new BallHitFloorFact(ball.Id)
                : null;

        private static BallHitBarFact? DetectBallBarCollision(BallState ball, BallState prevBall, BarState bar)
        {
            var barRect = RectFromBar(bar);
            if (!CircleOverlapsRect(ball.X, ball.Y, PlayfieldLayout.BallRadius, barRect))
                return null;
            // 위에서 내려올 때만 트리거 (prevBall.Vy > 0 — TS Y+ 아래)
            if (prevBall.Vy <= 0f) return null;

            var contactX = (ball.X - bar.X) / (bar.Width / 2f);
            var clamped = MathF.Max(-1f, MathF.Min(1f, contactX));
            return new BallHitBarFact(ball.Id, clamped);
        }

        private static BallHitBlockFact? DetectBallBlockCollisions(BallState ball, BallState prevBall, IReadOnlyList<BlockState> blocks)
        {
            // 가장 가까운 1개 후보.
            BlockState? closest = null;
            var bestDistSq = float.PositiveInfinity;

            foreach (var block in blocks)
            {
                if (block.IsDestroyed) continue;
                var rect = new Bounds(block.X, block.Y, PlayfieldLayout.BlockWidth, PlayfieldLayout.BlockHeight);
                if (!CircleOverlapsRect(ball.X, ball.Y, PlayfieldLayout.BallRadius, rect)) continue;
                var dx = ball.X - (block.X + PlayfieldLayout.BlockWidth / 2f);
                var dy = ball.Y - (block.Y + PlayfieldLayout.BlockHeight / 2f);
                var distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    closest = block;
                }
            }

            if (closest is not { } block2) return null;

            // prev 위치 기준으로 side 결정.
            var rectCx = block2.X + PlayfieldLayout.BlockWidth / 2f;
            var rectCy = block2.Y + PlayfieldLayout.BlockHeight / 2f;
            var pdx = prevBall.X - rectCx;
            var pdy = prevBall.Y - rectCy;
            var nx = pdx / (PlayfieldLayout.BlockWidth / 2f);
            var ny = pdy / (PlayfieldLayout.BlockHeight / 2f);
            var side = MathF.Abs(nx) > MathF.Abs(ny)
                ? (nx > 0f ? BlockSide.Right : BlockSide.Left)
                : (ny > 0f ? BlockSide.Bottom : BlockSide.Top);

            return new BallHitBlockFact(ball.Id, block2.Id, side);
        }

        // ─── Item detection ───
        private static ItemPickedUpFact? DetectItemPickedUp(ItemDropState item, BarState bar)
        {
            if (item.IsCollected) return null;
            return RectsOverlap(RectFromItem(item), RectFromBar(bar))
                ? new ItemPickedUpFact(item.Id)
                : null;
        }

        private static ItemFellOffFloorFact? DetectItemFellOff(ItemDropState item)
        {
            if (item.IsCollected) return null;
            return item.Y > PlayfieldLayout.PlayfieldHeight
                ? new ItemFellOffFloorFact(item.Id)
                : null;
        }

        // ─── Main entry ───
        public static IReadOnlyList<CollisionFact> DetectCollisions(GameplayRuntimeState state, GameplayRuntimeState prevState)
        {
            var facts = new List<CollisionFact>();

            for (var i = 0; i < state.Balls.Length; i++)
            {
                var ball = state.Balls[i];
                var prevBall = i < prevState.Balls.Length ? prevState.Balls[i] : ball;
                if (!ball.IsActive) continue;

                DetectBallWallCollisions(ball, facts);

                var floor = DetectBallFloor(ball);
                if (floor is not null)
                {
                    facts.Add(floor);
                    continue;  // 떨어진 공은 더 이상 처리 X
                }

                var bar = DetectBallBarCollision(ball, prevBall, state.Bar);
                if (bar is not null) facts.Add(bar);

                var block = DetectBallBlockCollisions(ball, prevBall, state.Blocks);
                if (block is not null) facts.Add(block);
            }

            foreach (var item in state.ItemDrops)
            {
                var pick = DetectItemPickedUp(item, state.Bar);
                if (pick is not null)
                {
                    facts.Add(pick);
                    continue;
                }
                var fall = DetectItemFellOff(item);
                if (fall is not null) facts.Add(fall);
            }

            return facts;
        }
    }
}
