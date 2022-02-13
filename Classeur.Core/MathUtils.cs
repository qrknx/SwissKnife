namespace Classeur.Core;

public static class MathUtils
{
    public static bool Intersects(long value, long min, long max) => Math.Clamp(value, min: min, max: max) == value;
}
