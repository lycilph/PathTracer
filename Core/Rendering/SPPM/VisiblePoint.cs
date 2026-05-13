
using Core.Materials;
using Core.Math;

namespace Core.Rendering.SPPM;

public struct VisiblePoint
{
    public Vec3 Position;
    public Vec3 Normal;
    public Vec3 Throughput;
    public Vec3 DirectLighting;
    public IMaterial Material;
    public float Radius;
    public int PixelIndex;
    public bool Valid;
}
