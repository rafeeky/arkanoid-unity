namespace Arkanoid.Shared
{
    public static class Easing
    {
        // EaseOutCubic: 감속 곡선. t∈[0,1] 클램프 후 1 - (1-t)^3.
        public static float EaseOutCubic(float t)
        {
            var c = t < 0f ? 0f : (t > 1f ? 1f : t);
            var inv = 1f - c;
            return 1f - inv * inv * inv;
        }
    }
}
