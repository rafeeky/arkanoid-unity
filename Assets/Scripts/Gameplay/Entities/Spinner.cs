using Arkanoid.Definitions;
using Arkanoid.Shared;
using System;
using System.Collections.Generic;

namespace Arkanoid.Gameplay
{
    // 종류:
    //   Cube     — 정사각형 (변 = def.Size). spinner.AngleRad 로 z축 회전.
    //   Triangle — 정삼각형 (외접원 반경 = def.Size/2). spinner.AngleRad 로 z축 회전.
    //
    // 시각 렌더는 pseudo-3D Y축 회전. 콜리전은 *시각 silhouette 과 일치하는 폴리곤* —
    // 원(circle) 보다 형태에 훨씬 가깝다.
    public static class Spinner
    {
        // 시각 outer envelope (edge stroke 2px + glow) 보정 — 콜리전 ~4px 외곽 확장.
        // 시각이 발광으로 더 커 보이는데 콜리전이 fill 만 잡으면 "닿았는데 안 튕김" 느낌.
        private const float VisualOuterPad = 4f;

        // 회전된 콜리전 폴리곤 vertex 목록 (월드 좌표, CCW).
        // Cube     — Y회전 후 2D silhouette = 직사각형 (4 vertex). x range = ±s*(|cos|+|sin|), y = ±s
        // Triangle — tetrahedron Y회전 후 silhouette = top vertex + base 3개 중 minX/maxX (3 vertex)
        public static IReadOnlyList<Vec2> GetCollisionPolygon(SpinnerRuntimeState spinner, SpinnerDefinition def)
        {
            var cx = spinner.X;
            var cy = spinner.Y;
            var cos = MathF.Cos(spinner.AngleRad);
            var sin = MathF.Sin(spinner.AngleRad);

            if (def.Kind == SpinnerKind.Cube)
            {
                var s = def.Size / 2f;
                var halfW = s * (MathF.Abs(cos) + MathF.Abs(sin)) + VisualOuterPad;
                var halfH = s + VisualOuterPad;
                return new[]
                {
                    new Vec2(cx - halfW, cy - halfH),
                    new Vec2(cx + halfW, cy - halfH),
                    new Vec2(cx + halfW, cy + halfH),
                    new Vec2(cx - halfW, cy + halfH),
                };
            }

            // triangle (tetrahedron). renderInGameScreen.ts §triangle 와 동일 vertex.
            //   s = def.Size/2, h = def.Size * √6/3, inv3 = 1/√3
            //   top:   (0,   -h,      0)
            //   base1: (s,   h/3,  -s*inv3)
            //   base2: (-s,  h/3,  -s*inv3)
            //   base3: (0,   h/3,   2*s*inv3)
            // Y회전 후 x' = lx*cos - lz*sin, y' = ly (변하지 않음).
            var sT = def.Size / 2f;
            var h = def.Size * (MathF.Sqrt(6f) / 3f);
            var inv3 = 1f / MathF.Sqrt(3f);
            var baseY = h / 3f;
            var base1X = sT * cos - (-sT * inv3) * sin;     // s*cos + s*inv3*sin
            var base2X = -sT * cos - (-sT * inv3) * sin;    // -s*cos + s*inv3*sin
            var base3X = 0f * cos - (2f * sT * inv3) * sin; // -2*s*inv3*sin
            var minX = MathF.Min(MathF.Min(base1X, base2X), base3X);
            var maxX = MathF.Max(MathF.Max(base1X, base2X), base3X);
            // CCW: top, base-right, base-left (y is down)
            return new[]
            {
                new Vec2(cx, cy - h - VisualOuterPad),
                new Vec2(cx + maxX + VisualOuterPad, cy + baseY + VisualOuterPad),
                new Vec2(cx + minX - VisualOuterPad, cy + baseY + VisualOuterPad),
            };
        }

        // 폴리곤 변 위 점 중 ball center 에 가장 가까운 지점 + outward normal.
        private static (Vec2 Pt, Vec2 EdgeNormal) ClosestPointOnPolygon(IReadOnlyList<Vec2> poly, Vec2 p)
        {
            var bestDistSq = float.PositiveInfinity;
            var bestPt = poly[0];
            var bestNormal = new Vec2(0f, -1f);

            for (var i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                var ex = b.X - a.X;
                var ey = b.Y - a.Y;
                var len2 = ex * ex + ey * ey;
                if (len2 < 1e-12f) continue;
                var t = ((p.X - a.X) * ex + (p.Y - a.Y) * ey) / len2;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
                var cpx = a.X + ex * t;
                var cpy = a.Y + ey * t;
                var dx = p.X - cpx;
                var dy = p.Y - cpy;
                var dist2 = dx * dx + dy * dy;
                if (dist2 < bestDistSq)
                {
                    bestDistSq = dist2;
                    bestPt = new Vec2(cpx, cpy);
                    // CCW outward normal: edge 우측 (ey, -ex) 정규화.
                    var len = MathF.Sqrt(len2);
                    bestNormal = new Vec2(ey / len, -ex / len);
                }
            }
            return (bestPt, bestNormal);
        }

        // CCW 폴리곤 inside test (ray casting).
        private static bool IsPointInsidePolygon(IReadOnlyList<Vec2> poly, Vec2 p)
        {
            var inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var xi = poly[i].X; var yi = poly[i].Y;
                var xj = poly[j].X; var yj = poly[j].Y;
                var intersect = ((yi > p.Y) != (yj > p.Y))
                    && (p.X < ((xj - xi) * (p.Y - yi)) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        // Swept 충돌 — prev→curr 선분이 폴리곤 변을 가로질러 inside 진입했는지.
        // 빠른 공 터널링 방지. 가장 빠른 (t 최소) 충돌 또는 null.
        private static (float T, float Nx, float Ny)? SweptCheckPolygon(Vec2 prev, Vec2 curr, IReadOnlyList<Vec2> poly)
        {
            var bestT = float.PositiveInfinity;
            var bestNx = 0f;
            var bestNy = 0f;
            var hit = false;

            for (var i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                var ex = b.X - a.X;
                var ey = b.Y - a.Y;
                var lenSq = ex * ex + ey * ey;
                if (lenSq < 1e-12f) continue;
                var len = MathF.Sqrt(lenSq);
                var nx = ey / len;
                var ny = -ex / len;

                // 변 plane 까지 부호 거리 (outward 양수).
                var distPrev = (prev.X - a.X) * nx + (prev.Y - a.Y) * ny;
                var distCurr = (curr.X - a.X) * nx + (curr.Y - a.Y) * ny;

                // 공 surface 가 plane 에 닿는 = dist = BallRadius.
                // prev outside (dist > R) + curr inside-or-touching (dist <= R) → 진입.
                if (distPrev > PlayfieldLayout.BallRadius && distCurr <= PlayfieldLayout.BallRadius)
                {
                    var denom = distPrev - distCurr;
                    if (denom < 1e-9f) continue;
                    var t = (distPrev - PlayfieldLayout.BallRadius) / denom;
                    if (t < 0f || t > 1f) continue;
                    var hitX = prev.X + (curr.X - prev.X) * t;
                    var hitY = prev.Y + (curr.Y - prev.Y) * t;
                    // 그 시점 공 중심이 변 segment 영역 내인지 (투영 0..1).
                    var tEdge = ((hitX - a.X) * ex + (hitY - a.Y) * ey) / lenSq;
                    if (tEdge < 0f || tEdge > 1f) continue;

                    if (t < bestT)
                    {
                        bestT = t;
                        bestNx = nx;
                        bestNy = ny;
                        hit = true;
                    }
                }
            }

            if (!hit) return null;
            return (bestT, bestNx, bestNy);
        }

        // 공 ↔ 스피너 폴리곤 충돌 + 반사.
        // 두 단계:
        //   1) swept (prevBall 있을 때) — 선분 prev→curr 이 폴리곤 변 가로질러 진입? 터널링 방지.
        //   2) 현재 위치 검사 (closest point + inside test).
        public static (BallState NextBall, bool Collided) HandleBallCollision(
            BallState ball,
            SpinnerRuntimeState spinner,
            SpinnerDefinition def,
            BallState? prevBall = null)
        {
            var poly = GetCollisionPolygon(spinner, def);

            // 1) swept 검사
            if (prevBall.HasValue)
            {
                var pb = prevBall.Value;
                var swept = SweptCheckPolygon(new Vec2(pb.X, pb.Y), new Vec2(ball.X, ball.Y), poly);
                if (swept.HasValue)
                {
                    var s = swept.Value;
                    var hitX = pb.X + (ball.X - pb.X) * s.T;
                    var hitY = pb.Y + (ball.Y - pb.Y) * s.T;
                    var dotS = ball.Vx * s.Nx + ball.Vy * s.Ny;
                    // dot < 0 = 변쪽으로 들어오는 중 → 반사.
                    if (dotS < 0f)
                    {
                        return (ball with
                        {
                            X = hitX,
                            Y = hitY,
                            Vx = ball.Vx - 2f * dotS * s.Nx,
                            Vy = ball.Vy - 2f * dotS * s.Ny,
                        }, true);
                    }
                    // 이미 분리 방향 — push-out 만, 반사 안 함.
                    return (ball with { X = hitX, Y = hitY }, true);
                }
            }

            // 2) 옛 로직 — closest + inside test.
            var ballPos = new Vec2(ball.X, ball.Y);
            var (closest, edgeNormal) = ClosestPointOnPolygon(poly, ballPos);

            var dx = ball.X - closest.X;
            var dy = ball.Y - closest.Y;
            var distToBoundary = MathF.Sqrt(dx * dx + dy * dy);
            var inside = IsPointInsidePolygon(poly, ballPos);

            // inside 가 아니면서 거리 > BallRadius → 충돌 없음.
            if (!inside && distToBoundary > PlayfieldLayout.BallRadius)
            {
                return (ball, false);
            }

            // 법선: 공 → 폴리곤 외부 방향.
            float nx, ny;
            if (inside)
            {
                nx = edgeNormal.X;
                ny = edgeNormal.Y;
            }
            else if (distToBoundary > 1e-6f)
            {
                nx = dx / distToBoundary;
                ny = dy / distToBoundary;
            }
            else
            {
                nx = edgeNormal.X;
                ny = edgeNormal.Y;
            }

            // 입사 속도의 법선 성분.
            var dot = ball.Vx * nx + ball.Vy * ny;
            // push-out overlap:
            //   outside: BallRadius - distToBoundary (양수)
            //   inside:  BallRadius + distToBoundary (공을 폴리곤 밖으로 강제)
            var overlap = inside
                ? PlayfieldLayout.BallRadius + distToBoundary
                : PlayfieldLayout.BallRadius - distToBoundary;

            // dot >= 0 이면 이미 분리 방향 → 반사 안 함, push-out 만.
            if (dot >= 0f)
            {
                return (ball with
                {
                    X = ball.X + nx * overlap,
                    Y = ball.Y + ny * overlap,
                }, true);
            }

            // 반사: v' = v - 2(v·n)n
            return (ball with
            {
                X = ball.X + nx * overlap,
                Y = ball.Y + ny * overlap,
                Vx = ball.Vx - 2f * dot * nx,
                Vy = ball.Vy - 2f * dot * ny,
            }, true);
        }
    }
}
