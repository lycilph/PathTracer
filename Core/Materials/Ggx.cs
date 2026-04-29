using Core.Math;

namespace Core.Materials;

/// <summary>
/// GGX (Trowbridge-Reitz) microfacet utilities.
/// This implementation uses classic NDF sampling (not VNDF).
/// </summary>
public static class Ggx
{
    public static float D_Ggx(float alpha, float cosThetaH)
    {
        // D = a^2 / (pi * ((cos^2)*(a^2 - 1) + 1)^2)
        float a2 = alpha * alpha;
        float cos2 = cosThetaH * cosThetaH;
        float denom = cos2 * (a2 - 1f) + 1f;
        return a2 * MathUtil.InvPi / (denom * denom);
    }

    /// <summary>
    /// Smith G1 term using Schlick-GGX approximation.
    /// k = (alpha+1)^2 / 8 (Disney-style for direct lighting).
    /// </summary>
    public static float G1_SchlickGgx(float alpha, float cosTheta)
    {
        float a = alpha;
        float k = (a + 1f);
        k = (k * k) / 8f;
        return cosTheta / (cosTheta * (1f - k) + k);
    }

    public static float G_Smith(float alpha, float cosThetaI, float cosThetaO)
        => G1_SchlickGgx(alpha, cosThetaI) * G1_SchlickGgx(alpha, cosThetaO);

    public static Vec3 FresnelSchlick(in Vec3 F0, float cosTheta)
    {
        // F = F0 + (1-F0)*(1-cos)^5
        float m = 1f - MathUtil.Clamp(cosTheta, 0f, 1f);
        float m2 = m * m;
        float m5 = m2 * m2 * m;
        return F0 + (Vec3.One - F0) * m5;
    }

    /// <summary>
    /// Samples a GGX half-vector in local coordinates where n=(0,0,1).
    /// Returns a unit vector with z>=0.
    /// </summary>
    public static Vec3 SampleHalfVector(float alpha, float u1, float u2)
    {
        // Sample theta from GGX distribution:
        // tan^2(theta) = a^2 * u / (1-u)
        float a2 = alpha * alpha;
        float t2 = a2 * u1 / (1f - u1 + 1e-8f);
        float cosTheta = 1f / float.Sqrt(1f + t2);
        float sinTheta = float.Sqrt(float.Max(0f, 1f - cosTheta * cosTheta));

        float phi = MathUtil.TwoPi * u2;
        float x = sinTheta * float.Cos(phi);
        float y = sinTheta * float.Sin(phi);
        float z = cosTheta;
        return new Vec3(x, y, z);
    }

    public static (Vec3 u, Vec3 v, Vec3 w) MakeBasis(in Vec3 n)
    {
        Vec3 w = n.Normalized();
        Vec3 a = float.Abs(w.X) > 0.9f ? Vec3.UnitY : Vec3.UnitX;
        Vec3 v = Vec3.Cross(w, a).Normalized();
        Vec3 u = Vec3.Cross(v, w);
        return (u, v, w);
    }

    public static Vec3 ToWorld(in Vec3 local, in Vec3 n)
    {
        var (u, v, w) = MakeBasis(n);
        return (u * local.X + v * local.Y + w * local.Z).Normalized();
    }

    public static float CosTheta(in Vec3 d, in Vec3 n)
        => Vec3.Dot(d, n);
}