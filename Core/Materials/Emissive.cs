namespace Core.Materials;


/// <summary>
/// A light-emitting material (§3.9.1).
/// Terminates the path and returns emitted radiance to the integrator.
/// </summary>
/// <param name="Emission">
/// Emitted radiance per RGB channel. Values above 1 are valid HDR intensities.
/// </param>
public sealed class Emissive(Vector3 Emission) : IMaterial
{
    public Vector3 Emission { get; } = Emission;

    /// <summary>
    /// Returns the emitted radiance at this surface point.
    /// Called by the integrator when a ray hits an emissive surface.
    /// </summary>
    public Vector3 Emit() => Emission;

    /// <inheritdoc/>
    /// <remarks>
    /// Emissive surfaces do not scatter — returning false terminates the path.
    /// The integrator is responsible for calling Emit() before discarding the ray.
    /// </remarks>
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        attenuation = Vector3.Zero;
        scattered = default;
        return false;
    }

    /// <summary>
    /// Returns the PDF of scattering in direction <paramref name="scattered"/>.
    /// </summary>
    /// <remarks>
    /// Emissive surfaces never scatter — <see cref="Scatter"/> always returns
    /// false and the path terminates. The PDF is 0 to reflect that no valid
    /// scatter direction exists.
    /// </remarks>
    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered) => 0.0;
}