using Core.Materials;
using Core.Math;

namespace Core.Scene;

/// <summary>
/// Triangle mesh built from a list of triangles and internally accelerated by a BVH.
/// </summary>
public sealed class TriangleMesh : IHittable
{
    private readonly BvhNode _bvh;
    private readonly Aabb _box;

    public TriangleMesh(IReadOnlyList<Triangle> triangles)
    {
        if (triangles.Count == 0)
            throw new ArgumentException("Mesh must contain triangles");

        _bvh = new BvhNode(triangles);
        _bvh.BoundingBox(out _box);
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
        => _bvh.Hit(ray, tMin, tMax, out hit);

    public bool BoundingBox(out Aabb box)
    {
        box = _box;
        return true;
    }

    /// <summary>
    /// Utility: creates a unit cube mesh centered at origin.
    /// </summary>
    public static TriangleMesh CreateUnitCube(IMaterial mat)
    {
        // 8 vertices
        Vec3[] v =
        {
            new(-0.5f,-0.5f,-0.5f), new(0.5f,-0.5f,-0.5f), new(0.5f,0.5f,-0.5f), new(-0.5f,0.5f,-0.5f),
            new(-0.5f,-0.5f, 0.5f), new(0.5f,-0.5f, 0.5f), new(0.5f,0.5f, 0.5f), new(-0.5f,0.5f, 0.5f)
        };


        int[] idx =
        {
            // -Z
            0,1,2, 0,2,3,
            // +Z
            4,6,5, 4,7,6,
            // -Y
            0,5,1, 0,4,5,
            // +Y
            3,2,6, 3,6,7,
            // -X
            0,3,7, 0,7,4,
            // +X
            1,5,6, 1,6,2
        };

        var tris = new List<Triangle>(idx.Length / 3);
        for (int i = 0; i < idx.Length; i += 3)
            tris.Add(new Triangle(v[idx[i]], v[idx[i + 1]], v[idx[i + 2]], mat));

        return new TriangleMesh(tris);
    }
}