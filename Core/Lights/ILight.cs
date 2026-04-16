using Core.Math;
using Core.Sampling;

namespace Core.Lights;

public readonly struct LightSample
{
    public readonly Vec3 Position;
    public readonly Vec3 Normal;
    public readonly Vec3 Wi;         // direction from reference point to light
    public readonly float Distance;  // distance from reference point
    public readonly float Pdf;       // pdf over solid angle at reference point (NOT including light selection probability)
    public readonly Vec3 Radiance;   // emitted radiance along -Wi

    public LightSample(in Vec3 position, in Vec3 normal, in Vec3 wi, float distance, float pdf, in Vec3 radiance)
        => (Position, Normal, Wi, Distance, Pdf, Radiance) = (position, normal, wi, distance, pdf, radiance);
}

public interface ILight
{
    LightSample Sample(in Vec3 referencePoint, Sampler sampler);

    /// <summary>
    /// Returns the solid-angle PDF for sampling direction wi from referencePoint towards this light.
    /// Returns 0 if the ray does not hit the light.
    /// </summary>
    float Pdf(in Vec3 referencePoint, in Vec3 wi);
}
