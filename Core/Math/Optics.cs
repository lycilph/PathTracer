namespace Core.Math;

public static class Optics
{
    public static Vec3 Reflect(in Vec3 v, in Vec3 n)
        => v - 2f * Vec3.Dot(v, n) * n;

    /// <summary>
    /// Computes refracted direction using Snell's law.
    /// Returns false if total internal reflection occurs.
    ///
    /// v: incident direction (normalized), pointing into the surface.
    /// n: surface normal (normalized) oriented against incident for front face.
    /// eta: etaI/etaT.
    /// </summary>
    public static bool Refract(in Vec3 v, in Vec3 n, float eta, out Vec3 refracted)
    {
        float cosTheta = float.Min(Vec3.Dot(-v, n), 1f);
        Vec3 rOutPerp = eta * (v + cosTheta * n);
        float k = 1f - rOutPerp.LengthSquared();
        if (k < 0f)
        {
            refracted = default;
            return false;
        }
        Vec3 rOutParallel = -float.Sqrt(k) * n;
        refracted = (rOutPerp + rOutParallel).Normalized();
        return true;
    }

    /// <summary>
    /// Schlick approximation for Fresnel reflectance of a dielectric at an interface.
    /// cosTheta should be in [0,1].
    /// </summary>
    public static float Schlick(float cosTheta, float etaI, float etaT)
    {
        float r0 = (etaI - etaT) / (etaI + etaT);
        r0 *= r0;
        float oneMinus = 1f - cosTheta;
        return r0 + (1f - r0) * oneMinus * oneMinus * oneMinus * oneMinus * oneMinus;
    }
}
