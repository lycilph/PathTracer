namespace Core;

/// <summary>
/// A sphere whose centre moves linearly between two positions over a
/// time interval, producing motion blur when rendered with shutter time (§3.2.2).
/// </summary>
public sealed class MovingSphere : IHittable, IBoundable
{
    private readonly Vector3 _centre0;
    private readonly Vector3 _centre1;
    private readonly double _time0;
    private readonly double _time1;
    private readonly double _radius;
    private readonly IMaterial _material;

    /// <param name="centre0">Centre position at time <paramref name="time0"/>.</param>
    /// <param name="centre1">Centre position at time <paramref name="time1"/>.</param>
    /// <param name="time0">Start time of the motion interval.</param>
    /// <param name="time1">End time of the motion interval.</param>
    /// <param name="radius">Sphere radius in world units.</param>
    /// <param name="material">Surface material.</param>
    public MovingSphere(
        Vector3 centre0, Vector3 centre1,
        double time0, double time1,
        double radius,
        IMaterial material)
    {
        _centre0 = centre0;
        _centre1 = centre1;
        _time0 = time0;
        _time1 = time1;
        _radius = radius;
        _material = material;
    }

    /// <summary>
    /// Returns the centre of the sphere at the given time by linear interpolation.
    /// </summary>
    /// <remarks>
    /// centre(t) = centre0 + ((t − t0) / (t1 − t0)) · (centre1 − centre0)
    /// </remarks>
    public Vector3 CentreAt(double time)
    {
        if (Math.Abs(_time1 - _time0) < 1e-10)
            return _centre0;

        var t = (time - _time0) / (_time1 - _time0);
        return _centre0 + Math.Clamp(t, 0, 1) * (_centre1 - _centre0);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the ray's Time value to determine the sphere's centre at
    /// intersection time (§3.2.2).
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        // Centre is at the ray's time — this is what produces motion blur
        var centre = CentreAt(ray.Time);
        var oc = ray.Origin - centre;

        var a = ray.Direction.LengthSquared;
        var h = Vector3.Dot(oc, ray.Direction);
        var c = oc.LengthSquared - _radius * _radius;

        var discriminant = h * h - a * c;
        if (discriminant < 0) return false;

        var sqrtD = Math.Sqrt(discriminant);
        var t = (-h - sqrtD) / a;
        if (t < ray.TMin || t > ray.TMax)
        {
            t = (-h + sqrtD) / a;
            if (t < ray.TMin || t > ray.TMax)
                return false;
        }

        var point = ray.At(t);
        var outwardNormal = (point - centre) / _radius;
        hit = HitRecord.Create(t, point, ray, outwardNormal, _material);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The AABB encloses the sphere at both time endpoints — the union
    /// of both positions guarantees all possible positions are contained.
    /// </remarks>
    public Aabb GetBounds()
    {
        var b0 = BoundsAt(_centre0);
        var b1 = BoundsAt(_centre1);
        return b0.ExpandTo(b1);
    }

    private Aabb BoundsAt(Vector3 centre) => new(
        centre - new Vector3(_radius, _radius, _radius),
        centre + new Vector3(_radius, _radius, _radius));
}