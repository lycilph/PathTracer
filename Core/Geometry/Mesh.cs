using Core.Acceleration;
using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// A triangle mesh loaded from an OBJ file, accelerated by a BVH (§6.3).
/// Implements IHittable and IBoundable so it can be added directly to a
/// SceneList or nested inside another BVH.
/// </summary>
public sealed class Mesh : IHittable, IBoundable
{
    private readonly BvhNode _bvh;
    private readonly Aabb _bounds;

    /// <summary>Number of triangles in the mesh.</summary>
    public int TriangleCount { get; }

    /// <param name="triangles">
    /// The triangles that make up the mesh. Must not be empty.
    /// </param>
    private Mesh(List<Triangle> triangles)
    {
        TriangleCount = triangles.Count;
        _bvh = new BvhNode(triangles.Cast<IHittable>().ToList());
        _bounds = ComputeBounds(triangles);
    }

    /// <summary>
    /// Loads an OBJ file and builds a mesh with a BVH acceleration structure.
    /// </summary>
    /// <param name="path">Path to the .obj file.</param>
    /// <param name="material">Material applied to all triangles.</param>
    /// <param name="smoothNormals">
    /// If true and the OBJ contains vertex normals, smooth shading is used.
    /// </param>
    /// <returns>A fully built Mesh ready to add to a scene.</returns>
    public static Mesh Load(string path, IMaterial material,
                            bool smoothNormals = false)
    {
        var triangles = ObjLoader.Load(path, material, smoothNormals);
        return new Mesh(triangles);
    }

    /// <inheritdoc/>
    public bool Hit(Ray ray, out HitRecord hit)
        => _bvh.Hit(ray, out hit);

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the precomputed AABB enclosing all triangles.
    /// Used when this mesh is nested inside a parent BVH.
    /// </remarks>
    public Aabb GetBounds() => _bounds;

    /// <summary>
    /// Computes the AABB enclosing all triangles in the mesh.
    /// </summary>
    private static Aabb ComputeBounds(List<Triangle> triangles)
    {
        var bounds = triangles[0].GetBounds();
        for (var i = 1; i < triangles.Count; i++)
            bounds = bounds.ExpandTo(triangles[i].GetBounds());
        return bounds;
    }
}