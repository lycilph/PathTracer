
using Core.Math;

namespace Core.Rendering.SPPM;

public struct SppmPixel
{
    public Vec3 Tau;
    public Vec3 Direct;
    public float Radius;
    public float PhotonCount;
    public float IterationPhotonCount;
}
