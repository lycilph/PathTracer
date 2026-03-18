namespace Core;

/// <summary>
/// A node in the Bounding Volume Hierarchy (§3.4).
/// Accelerates ray traversal from O(N) to O(log N) by recursively
/// partitioning primitives into axis-aligned bounding boxes.
/// </summary>
public sealed class BvhNode : IHittable
{
    private readonly IHittable _left;
    private readonly IHittable _right;
    private readonly Aabb _bounds;

    /// <summary>
    /// Builds a BVH over <paramref name="primitives"/> using longest-axis
    /// median split (§3.4.1).
    /// </summary>
    /// <param name="primitives">
    /// The list of boundable hittable primitives to partition.
    /// All elements must implement both IHittable and IBoundable.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if primitives is empty or contains non-boundable elements.
    /// </exception>
    public BvhNode(IList<IHittable> primitives)
        : this(primitives, 0, primitives.Count) { }

    private BvhNode(IList<IHittable> primitives, int start, int end)
    {
        var count = end - start;

        if (count == 1)
        {
            // Leaf: both children point to the same primitive
            _left = _right = primitives[start];
        }
        else if (count == 2)
        {
            // Leaf pair: one primitive each side
            _left = primitives[start];
            _right = primitives[start + 1];
        }
        else
        {
            // Interior: split along the longest axis at the centroid median
            var axis = LongestAxis(primitives, start, end);

            // Sort the slice in place by centroid along the chosen axis
            var slice = primitives
                .Skip(start)
                .Take(count)
                .OrderBy(p => GetCentroidComponent(p, axis))
                .ToList();

            // Write sorted order back into the original list
            for (var i = 0; i < count; i++)
                primitives[start + i] = slice[i];

            var mid = start + count / 2;
            _left = new BvhNode(primitives, start, mid);
            _right = new BvhNode(primitives, mid, end);
        }

        // Parent bounds = union of children's bounds
        _bounds = GetBounds(_left).ExpandTo(GetBounds(_right));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Tests the ray against this node's AABB first (§3.4.2).
    /// If the AABB is missed the subtree is skipped entirely.
    /// The nearer child is tested first; TMax is tightened before
    /// testing the second child so only closer hits are accepted.
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        // Early exit: ray misses this node's bounding box
        if (!_bounds.Hit(ray))
            return false;

        // Test left child
        var hitLeft = _left.Hit(ray, out hit);

        // Test right child — tighten TMax if left already found something
        var rightRay = hitLeft ? ray with { TMax = hit.T } : ray;
        var hitRight = _right.Hit(rightRay, out var rightHit);

        if (hitRight)
            hit = rightHit;

        return hitLeft || hitRight;
    }

    /// <summary>
    /// Returns the index (0=X, 1=Y, 2=Z) of the axis with the greatest
    /// spread of primitive centroids in the range [start, end).
    /// </summary>
    private static int LongestAxis(IList<IHittable> primitives, int start, int end)
    {
        var minX = double.MaxValue; var maxX = double.MinValue;
        var minY = double.MaxValue; var maxY = double.MinValue;
        var minZ = double.MaxValue; var maxZ = double.MinValue;

        for (var i = start; i < end; i++)
        {
            var c = GetCentroid(primitives[i]);
            if (c.X < minX) minX = c.X; if (c.X > maxX) maxX = c.X;
            if (c.Y < minY) minY = c.Y; if (c.Y > maxY) maxY = c.Y;
            if (c.Z < minZ) minZ = c.Z; if (c.Z > maxZ) maxZ = c.Z;
        }

        var spanX = maxX - minX;
        var spanY = maxY - minY;
        var spanZ = maxZ - minZ;

        if (spanX >= spanY && spanX >= spanZ) return 0;
        if (spanY >= spanZ) return 1;
        return 2;
    }

    private static double GetCentroidComponent(IHittable primitive, int axis)
    {
        var c = GetCentroid(primitive);
        return axis switch { 0 => c.X, 1 => c.Y, _ => c.Z };
    }

    private static Vector3 GetCentroid(IHittable primitive)
        => GetBounds(primitive).Centroid;

    private static Aabb GetBounds(IHittable primitive)
    {
        if (primitive is IBoundable b)
            return b.GetBounds();
        if (primitive is BvhNode node)
            return node._bounds;
        throw new ArgumentException(
            $"{primitive.GetType().Name} does not implement IBoundable.");
    }
}