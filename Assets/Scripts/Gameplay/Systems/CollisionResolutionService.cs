using System;

namespace Arkanoid.Gameplay
{
    // T1.4: enforceMinAngle 만 구현 (entity 들이 사용). 나머지 (resolveWall/Bar/Block 등) 는 T1.5.
    public static class CollisionResolutionService
    {
        // 속도 벡터가 수평/수직축에서 minAngleDeg 보다 가까워지지 않게 강제. magnitude 보존.
        // |vx|/speed < sin(minAngleDeg) 면 vx 를 ±(speed * sin) 으로 클램프 + vy 재계산.
        // 무한 핑퐁 (수평/수직 트래핑) 방지. MinAngleDeg 는 PhysicsConfig 의 data-driven 값.
        public static (float Vx, float Vy) EnforceMinAngle(float vx, float vy, float minAngleDeg)
        {
            var speed = MathF.Sqrt(vx * vx + vy * vy);
            if (speed == 0f) return (vx, vy);

            var minSin = MathF.Sin(minAngleDeg * MathF.PI / 180f);
            var minComponent = speed * minSin;
            var newVx = vx;
            var newVy = vy;

            // 수직에 가까운 궤적 보호 (|vx| 가 너무 작음)
            if (MathF.Abs(newVx) < minComponent)
            {
                newVx = minComponent * (newVx >= 0f ? 1f : -1f);
                var vySign = newVy >= 0f ? 1f : -1f;
                newVy = vySign * MathF.Sqrt(MathF.Max(0f, speed * speed - newVx * newVx));
            }

            // 수평에 가까운 궤적 보호 (|vy| 가 너무 작음)
            if (MathF.Abs(newVy) < minComponent)
            {
                newVy = minComponent * (newVy >= 0f ? 1f : -1f);
                var vxSign = newVx >= 0f ? 1f : -1f;
                newVx = vxSign * MathF.Sqrt(MathF.Max(0f, speed * speed - newVy * newVy));
            }

            return (newVx, newVy);
        }
    }
}
