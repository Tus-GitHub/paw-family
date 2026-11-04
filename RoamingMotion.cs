namespace PawFamily;

public readonly record struct RoamStep(Point Location, bool Walking, bool FacingLeft);

// No desktop input injection. The companion only moves its own window.
public sealed class RoamingMotion
{
    readonly Random random;
    double nextStart = 4, walkUntil, remainder;
    int direction = -1;
    bool walking;
    public RoamingMotion(int? seed = null) => random = seed.HasValue ? new Random(seed.Value) : new Random();
    public RoamStep Step(Point location, Rectangle area, Size size, double now, double elapsed, bool enabled, bool interacting, Mood mood)
    {
        int min = area.Left, max = Math.Max(min, area.Right - size.Width);
        var safe = new Point(Math.Clamp(location.X, min, max), Math.Clamp(location.Y, area.Top, Math.Max(area.Top, area.Bottom - size.Height)));
        if (!enabled || interacting || mood is Mood.Attention or Mood.Error or Mood.Done || max == min)
        {
            walking = false; remainder = 0; nextStart = now + 3;
            return new(safe, false, direction < 0);
        }
        if (walking && now >= walkUntil)
        {
            walking = false; remainder = 0; nextStart = now + random.Next(7, 14);
        }
        if (!walking && now >= nextStart)
        {
            if (safe.X <= min + 35) direction = 1;
            else if (safe.X >= max - 35) direction = -1;
            else direction = -direction;
            walking = true; walkUntil = now + random.Next(4, 8);
        }
        if (walking)
        {
            remainder += direction * 34 * Math.Clamp(elapsed, 0, .1);
            int delta = (int)Math.Truncate(remainder); remainder -= delta;
            int x = Math.Clamp(safe.X + delta, min, max);
            safe.X = x;
            if (x == min || x == max) { direction = x == min ? 1 : -1; remainder = 0; }
        }
        return new(safe, walking, direction < 0);
    }
}
