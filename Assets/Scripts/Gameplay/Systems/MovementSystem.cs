using System;
using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Gameplay
{
    public readonly record struct BallMoveResult(
        BallState Ball,
        IReadOnlyList<BallHitBlockFact> BlockFacts,
        IReadOnlyList<BallHitWallFact> WallFacts);

    public readonly record struct SanityCheckResult(
        BallState Ball,
        bool WasInside,
        BallHitBlockFact? CollisionFact);

    // 공/바/아이템 이동 + 충돌 통합. sub-step sweep 으로 터널링 방지.
    public static class MovementSystem
    {
        // ─── 단순 이동 ───

        // 단순 위치 미리보기용. 충돌 검사 없음 (legacy 테스트 / preview).
        public static BallState MoveBall(BallState ball, float dt)
        {
            if (!ball.IsActive) return ball;
            return ball with { X = ball.X + ball.Vx * dt, Y = ball.Y + ball.Vy * dt };
        }

        public static BarState MoveBar(BarState bar, int direction, float dt, GameplayConfig config)
        {
            if (direction == 0) return bar;
            var newX = bar.X + config.BarMoveSpeed * direction * dt;
            var halfWidth = bar.Width / 2f;
            var clamped = MathF.Max(halfWidth, MathF.Min(PlayfieldLayout.PlayfieldWidth - halfWidth, newX));
            return bar with { X = clamped };
        }

        public static ItemDropState MoveItemDrop(ItemDropState item, float dt)
        {
            return item with { Y = item.Y + item.FallSpeed * dt };
        }

        // 비활성 공 (자석 부착 포함) 을 바에 동기화.
        // AttachedOffsetX 있으면 바 중심 + 오프셋 (자석), 없으면 우측 InitialLaunchOffsetX (발사 -60°).
        public static BallState MoveAttachedBallToBar(BallState ball, BarState bar)
        {
            if (ball.IsActive) return ball;
            if (ball.AttachedOffsetX.HasValue)
            {
                var attachY = bar.Y - PlayfieldLayout.BarHeight / 2f - PlayfieldLayout.BallRadius;
                return ball with
                {
                    X = bar.X + ball.AttachedOffsetX.Value,
                    Y = attachY,
                };
            }
            return ball with
            {
                X = bar.X + PlayfieldLayout.InitialLaunchOffsetX,
                Y = bar.Y - PlayfieldLayout.BarHeight,
            };
        }

        // ─── Circle-AABB overlap ───

        private static bool CircleOverlapsBlock(float cx, float cy, BlockState block)
        {
            var nearestX = MathF.Max(block.X, MathF.Min(cx, block.X + PlayfieldLayout.BlockWidth));
            var nearestY = MathF.Max(block.Y, MathF.Min(cy, block.Y + PlayfieldLayout.BlockHeight));
            var dx = cx - nearestX;
            var dy = cy - nearestY;
            return dx * dx + dy * dy <= PlayfieldLayout.BallRadius * PlayfieldLayout.BallRadius;
        }

        // ─── Swept entry time (slab method) ───

        // prev→curr 선분이 block 의 expanded AABB 에 진입하는 시점. tx, ty 중 MAX (last-axis-to-cross).
        // 동시 overlap 시 tie-breaker: ball 이 *방금* 진입한 block 이 가장 큰 entry t.
        private static float ComputeEntryTime(float prevX, float prevY, float currX, float currY, BlockState block)
        {
            var dx = currX - prevX;
            var dy = currY - prevY;
            var exLeft = block.X - PlayfieldLayout.BallRadius;
            var exRight = block.X + PlayfieldLayout.BlockWidth + PlayfieldLayout.BallRadius;
            var exTop = block.Y - PlayfieldLayout.BallRadius;
            var exBottom = block.Y + PlayfieldLayout.BlockHeight + PlayfieldLayout.BallRadius;

            var tx = float.NegativeInfinity;
            if (dx > 0f) tx = (exLeft - prevX) / dx;
            else if (dx < 0f) tx = (exRight - prevX) / dx;

            var ty = float.NegativeInfinity;
            if (dy > 0f) ty = (exTop - prevY) / dy;
            else if (dy < 0f) ty = (exBottom - prevY) / dy;

            return MathF.Max(tx, ty);
        }

        // overlap 한 후보 중 entry time 가장 큰 (가장 최근 진입) block.
        // skipIds 는 같은 sub-step 안 중복 처리 방지.
        private static BlockState? FindOverlappingBlock(
            float prevX, float prevY, float currX, float currY,
            IReadOnlyList<BlockState> blocks,
            HashSet<string> skipIds)
        {
            BlockState? best = null;
            var bestEntryT = float.NegativeInfinity;
            foreach (var block in blocks)
            {
                if (block.IsDestroyed) continue;
                if (skipIds.Contains(block.Id)) continue;
                if (!CircleOverlapsBlock(currX, currY, block)) continue;

                var t = ComputeEntryTime(prevX, prevY, currX, currY, block);
                if (t > bestEntryT)
                {
                    bestEntryT = t;
                    best = block;
                }
            }
            return best;
        }

        // entry side 결정 — last-entry axis (larger entry time) = constraining face.
        // Ties → y (top/bottom) 우선 (Arkanoid 의 oblique 궤적이 흔함).
        private static BlockSide DetermineEntrySide(float prevX, float prevY, float currX, float currY, BlockState block)
        {
            var dx = currX - prevX;
            var dy = currY - prevY;
            var exLeft = block.X - PlayfieldLayout.BallRadius;
            var exRight = block.X + PlayfieldLayout.BlockWidth + PlayfieldLayout.BallRadius;
            var exTop = block.Y - PlayfieldLayout.BallRadius;
            var exBottom = block.Y + PlayfieldLayout.BlockHeight + PlayfieldLayout.BallRadius;

            var txEntry = float.NegativeInfinity;
            var xSide = BlockSide.Left;
            if (dx > 0f) { txEntry = (exLeft - prevX) / dx; xSide = BlockSide.Left; }
            else if (dx < 0f) { txEntry = (exRight - prevX) / dx; xSide = BlockSide.Right; }

            var tyEntry = float.NegativeInfinity;
            var ySide = BlockSide.Top;
            if (dy > 0f) { tyEntry = (exTop - prevY) / dy; ySide = BlockSide.Top; }
            else if (dy < 0f) { tyEntry = (exBottom - prevY) / dy; ySide = BlockSide.Bottom; }

            return txEntry > tyEntry ? xSide : ySide;
        }

        // ─── Door / Border helpers (orientation-aware bounds) ───

        private static DoorState? FindOverlappingDoor(float cx, float cy, IReadOnlyList<DoorState> doors)
        {
            foreach (var d in doors)
            {
                if (!Door.BlocksBall(d)) continue;
                var bounds = Door.GetBounds(d);
                var nearestX = MathF.Max(bounds.X, MathF.Min(cx, bounds.X + bounds.Width));
                var nearestY = MathF.Max(bounds.Y, MathF.Min(cy, bounds.Y + bounds.Height));
                var dx = cx - nearestX;
                var dy = cy - nearestY;
                if (dx * dx + dy * dy <= PlayfieldLayout.BallRadius * PlayfieldLayout.BallRadius)
                    return d;
            }
            return null;
        }

        private static DoorSide DetermineDoorEntrySide(float prevX, float prevY, float currX, float currY, DoorState door)
        {
            var bounds = Door.GetBounds(door);
            var dx = currX - prevX;
            var dy = currY - prevY;
            var exLeft = bounds.X - PlayfieldLayout.BallRadius;
            var exRight = bounds.X + bounds.Width + PlayfieldLayout.BallRadius;
            var exTop = bounds.Y - PlayfieldLayout.BallRadius;
            var exBottom = bounds.Y + bounds.Height + PlayfieldLayout.BallRadius;

            var txEntry = float.NegativeInfinity;
            var xSide = DoorSide.Left;
            if (dx > 0f) { txEntry = (exLeft - prevX) / dx; xSide = DoorSide.Left; }
            else if (dx < 0f) { txEntry = (exRight - prevX) / dx; xSide = DoorSide.Right; }

            var tyEntry = float.NegativeInfinity;
            var ySide = DoorSide.Top;
            if (dy > 0f) { tyEntry = (exTop - prevY) / dy; ySide = DoorSide.Top; }
            else if (dy < 0f) { tyEntry = (exBottom - prevY) / dy; ySide = DoorSide.Bottom; }

            return txEntry > tyEntry ? xSide : ySide;
        }

        private static BorderBlockState? FindOverlappingBorder(float cx, float cy, IReadOnlyList<BorderBlockState> borders)
        {
            foreach (var b in borders)
            {
                var bounds = BorderBlock.GetBounds(b);
                var nearestX = MathF.Max(bounds.X, MathF.Min(cx, bounds.X + bounds.Width));
                var nearestY = MathF.Max(bounds.Y, MathF.Min(cy, bounds.Y + bounds.Height));
                var dx = cx - nearestX;
                var dy = cy - nearestY;
                if (dx * dx + dy * dy <= PlayfieldLayout.BallRadius * PlayfieldLayout.BallRadius)
                    return b;
            }
            return null;
        }

        private static BorderSide DetermineBorderEntrySide(float prevX, float prevY, float currX, float currY, BorderBlockState border)
        {
            var bounds = BorderBlock.GetBounds(border);
            var dx = currX - prevX;
            var dy = currY - prevY;
            var exLeft = bounds.X - PlayfieldLayout.BallRadius;
            var exRight = bounds.X + bounds.Width + PlayfieldLayout.BallRadius;
            var exTop = bounds.Y - PlayfieldLayout.BallRadius;
            var exBottom = bounds.Y + bounds.Height + PlayfieldLayout.BallRadius;

            var txEntry = float.NegativeInfinity;
            var xSide = BorderSide.Left;
            if (dx > 0f) { txEntry = (exLeft - prevX) / dx; xSide = BorderSide.Left; }
            else if (dx < 0f) { txEntry = (exRight - prevX) / dx; xSide = BorderSide.Right; }

            var tyEntry = float.NegativeInfinity;
            var ySide = BorderSide.Top;
            if (dy > 0f) { tyEntry = (exTop - prevY) / dy; ySide = BorderSide.Top; }
            else if (dy < 0f) { tyEntry = (exBottom - prevY) / dy; ySide = BorderSide.Bottom; }

            return txEntry > tyEntry ? xSide : ySide;
        }

        // ─── moveBallWithCollisions — sub-step AABB sweep ───

        // 알고리즘:
        //   1. dt 를 SubStepSize 픽셀씩 나눔 (MaxSubSteps 캡).
        //   2. 각 sub-step:
        //      a. 위치 진행 (vx * stepDt, vy * stepDt)
        //      b. wall 충돌 (reflect + push)
        //      c. block 충돌 (overlap → entry-side → reflect + push)
        //      d. wall/block 모두 없으면 border/door (한 sub-step 1회)
        //   3. ball + 누적 facts 반환.
        // 보장: ball 중심이 SubStepSize 이상 이동 안 함 → SubStepSize 보다 두꺼운 어떤 block 도 터널링 X.
        public static BallMoveResult MoveBallWithCollisions(
            BallState ball,
            float dt,
            IReadOnlyList<BlockState> blocks,
            IReadOnlyList<BorderBlockState> borders,
            IReadOnlyList<DoorState> doors,
            PhysicsConfig physics)
        {
            if (!ball.IsActive)
                return new BallMoveResult(ball, Array.Empty<BallHitBlockFact>(), Array.Empty<BallHitWallFact>());

            var blockFacts = new List<BallHitBlockFact>();
            var wallFacts = new List<BallHitWallFact>();
            // 같은 tick 안에서 같은 block id 가 fact 중복 emit 안 되게 (collision detection 은 검사 자체가 ground truth).
            var hitBlockIds = new HashSet<string>();

            var speed = MathF.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            var totalDist = speed * dt;
            var steps = Math.Max(1, Math.Min(physics.MaxSubSteps, (int)MathF.Ceiling(totalDist / physics.SubStepSize)));
            var stepDt = dt / steps;

            var current = ball;

            for (var step = 0; step < steps; step++)
            {
                var prev = current;

                // a. Advance
                current = current with
                {
                    X = current.X + current.Vx * stepDt,
                    Y = current.Y + current.Vy * stepDt,
                };

                // b. Wall collisions
                WallSide? wallSide = null;
                if (current.X - PlayfieldLayout.BallRadius < 0f) wallSide = WallSide.Left;
                else if (current.X + PlayfieldLayout.BallRadius > PlayfieldLayout.PlayfieldWidth) wallSide = WallSide.Right;
                else if (current.Y - PlayfieldLayout.BallRadius < 0f) wallSide = WallSide.Top;

                var wallHit = wallSide.HasValue;
                if (wallSide.HasValue)
                {
                    var fact = new BallHitWallFact(ball.Id, wallSide.Value);
                    current = Wall.ReflectFromBall(current, fact, physics);
                    wallFacts.Add(fact);
                }

                // c. Block collision — at most 1 per sub-step.
                var hitThisSubstep = new HashSet<string>();
                var hitBlock = FindOverlappingBlock(prev.X, prev.Y, current.X, current.Y, blocks, hitThisSubstep);
                if (hitBlock is { } block)
                {
                    hitThisSubstep.Add(block.Id);
                    var side = DetermineEntrySide(prev.X, prev.Y, current.X, current.Y, block);

                    current = Block.HandleBallCollision(current, side, block, physics);

                    if (!hitBlockIds.Contains(block.Id))
                    {
                        hitBlockIds.Add(block.Id);
                        blockFacts.Add(new BallHitBlockFact(ball.Id, block.Id, side));
                    }
                }
                else if (!wallHit)
                {
                    // d. Border / Door (wall/block 모두 못 잡은 sub-step 에 한해).
                    var doorHandled = false;
                    if (doors.Count > 0)
                    {
                        var hitDoor = FindOverlappingDoor(current.X, current.Y, doors);
                        if (hitDoor is { } door)
                        {
                            var side = DetermineDoorEntrySide(prev.X, prev.Y, current.X, current.Y, door);
                            current = Door.HandleBallCollision(current, side, door, physics);
                            doorHandled = true;
                        }
                    }
                    if (!doorHandled && borders.Count > 0)
                    {
                        var hitBorder = FindOverlappingBorder(current.X, current.Y, borders);
                        if (hitBorder is { } border)
                        {
                            var side = DetermineBorderEntrySide(prev.X, prev.Y, current.X, current.Y, border);
                            current = BorderBlock.HandleBallCollision(current, side, border, physics);
                        }
                    }
                }
            }

            return new BallMoveResult(current, blockFacts, wallFacts);
        }

        // ─── Post-tick sanity check ───

        // 모든 이동/충돌 적용 후 *최후 방어선* — ball 중심이 active block 안에 있으면 push out + reflect.
        // strict centre-inside check (no radius). 첫 발견 block 만 보정.
        public static SanityCheckResult SanityCheckBallBlockSeparation(
            BallState ball,
            IReadOnlyList<BlockState> blocks,
            PhysicsConfig physics)
        {
            if (!ball.IsActive) return new SanityCheckResult(ball, false, null);

            foreach (var block in blocks)
            {
                if (block.IsDestroyed) continue;

                var bx = block.X;
                var by = block.Y;
                var bRight = bx + PlayfieldLayout.BlockWidth;
                var bBottom = by + PlayfieldLayout.BlockHeight;

                if (ball.X <= bx || ball.X >= bRight) continue;
                if (ball.Y <= by || ball.Y >= bBottom) continue;

                var cx = bx + PlayfieldLayout.BlockWidth / 2f;
                var cy = by + PlayfieldLayout.BlockHeight / 2f;
                var overlapX = PlayfieldLayout.BlockWidth / 2f - MathF.Abs(ball.X - cx);
                var overlapY = PlayfieldLayout.BlockHeight / 2f - MathF.Abs(ball.Y - cy);

                var newVx = ball.Vx;
                var newVy = ball.Vy;
                var newX = ball.X;
                var newY = ball.Y;
                BlockSide side;

                if (overlapX <= overlapY)
                {
                    if (ball.X < cx)
                    {
                        newX = bx - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                        side = BlockSide.Left;
                        if (newVx > 0f) newVx = -newVx;
                    }
                    else
                    {
                        newX = bRight + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                        side = BlockSide.Right;
                        if (newVx < 0f) newVx = -newVx;
                    }
                }
                else
                {
                    if (ball.Y < cy)
                    {
                        newY = by - PlayfieldLayout.BallRadius - physics.PushOutEpsilon;
                        side = BlockSide.Top;
                        if (newVy > 0f) newVy = -newVy;
                    }
                    else
                    {
                        newY = bBottom + PlayfieldLayout.BallRadius + physics.PushOutEpsilon;
                        side = BlockSide.Bottom;
                        if (newVy < 0f) newVy = -newVy;
                    }
                }

                var enforced = CollisionResolutionService.EnforceMinAngle(newVx, newVy, physics.MinAngleDeg);
                var correctedBall = ball with { X = newX, Y = newY, Vx = enforced.Vx, Vy = enforced.Vy };
                var collisionFact = new BallHitBlockFact(ball.Id, block.Id, side);

                return new SanityCheckResult(correctedBall, true, collisionFact);
            }

            return new SanityCheckResult(ball, false, null);
        }
    }
}
