namespace Core;

/// <summary>
/// A flat list of hittable primitives. Finds the nearest intersection
/// by testing all primitives in order — O(N) per ray (§3.4).
/// Will be replaced by a BVH in Milestone 2 without changing call sites.
/// </summary>
public sealed class SceneList : IHittable
{
    private readonly List<IHittable> _primitives = [];

    /// <summary>Adds a primitive to the scene.</summary>
    public void Add(IHittable primitive) => _primitives.Add(primitive);

    /// <summary>Removes all primitives from the scene.</summary>
    public void Clear() => _primitives.Clear();
    
    /// <summary>Number of primitives in the scene.</summary>
    public int Count => _primitives.Count;

    /// <summary>
    /// Returns the most efficient IHittable for this scene's primitive count.
    /// Uses a BVH for scenes with more than <paramref name="bvhThreshold"/>
    /// primitives, and a flat SceneList below that threshold.
    /// </summary>
    /// <param name="bvhThreshold">
    /// Minimum primitive count to justify BVH overhead. Default 16.
    /// </param>
    public IHittable Build(int bvhThreshold = 128)
    {
        if (_primitives.Count == 0)
            return this;

        if (_primitives.Count <= bvhThreshold)
            return this;

        return new BvhNode([.. _primitives]);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Iterates all primitives, progressively tightening TMax so only
    /// closer hits are accepted. Returns the nearest hit overall.
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;
        var foundAny = false;

        foreach (var primitive in _primitives)
        {
            // Pass a ray with TMax tightened to the closest hit so far
            var clipped = ray with { TMax = foundAny ? hit.T : ray.TMax };

            if (primitive.Hit(clipped, out var candidate))
            {
                hit = candidate;
                foundAny = true;
            }
        }

        return foundAny;
    }
}