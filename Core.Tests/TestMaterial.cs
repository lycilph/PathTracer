using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Core.Tests;

internal sealed class TestMaterial : IMaterial
{
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        attenuation = default;
        scattered = default;
        return false;
    }

    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered) => 0.0;

    public Vector3 Evaluate(Ray rayIn, HitRecord hit, Ray scattered) => Vector3.Zero;
}