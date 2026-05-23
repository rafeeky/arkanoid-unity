namespace Arkanoid.Gameplay
{
    public enum BarEffect { None, Expand, Magnet, Laser }

    public readonly record struct BarState(
        float X,
        float Y,
        float Width,
        float MoveSpeed,
        BarEffect ActiveEffect);
}
