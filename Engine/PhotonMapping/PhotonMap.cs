using Core.Algebra;

namespace Engine.PhotonMapping;

/// <summary>
/// A kd-tree storing photons for efficient k-nearest-neighbour queries.
/// Build is O(N log N), kNN query is O(log N) expected.
/// </summary>
public sealed class PhotonMap
{
    private readonly PhotonNode? _root;
    private readonly int _count;

    /// <summary>Total number of photons stored in the map.</summary>
    public int Count => _count;

    /// <summary>
    /// Builds a kd-tree from the given list of photons.
    /// </summary>
    /// <param name="photons">The photons to store. May be empty.</param>
    public PhotonMap(IList<Photon> photons)
    {
        _count = photons.Count;
        if (_count > 0)
            _root = Build(photons.ToList(), 0);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private static PhotonNode Build(List<Photon> photons, int depth)
    {
        if (photons.Count == 1)
            return new PhotonNode(photons[0], null, null, 0);

        // Split axis = axis of maximum photon spread
        var axis = ComputeSplitAxis(photons);

        // Sort by chosen axis and split at median
        photons.Sort((a, b) =>
            GetComponent(a.Position, axis)
                .CompareTo(GetComponent(b.Position, axis)));

        var mid = photons.Count / 2;
        var left = photons.Take(mid).ToList();
        var right = photons.Skip(mid + 1).ToList();

        return new PhotonNode(
            photons[mid],
            left.Count > 0 ? Build(left, depth + 1) : null,
            right.Count > 0 ? Build(right, depth + 1) : null,
            axis);
    }

    /// <summary>
    /// Returns the axis (0=X, 1=Y, 2=Z) with the greatest spread
    /// of photon positions.
    /// </summary>
    private static int ComputeSplitAxis(List<Photon> photons)
    {
        var minX = double.MaxValue; var maxX = double.MinValue;
        var minY = double.MaxValue; var maxY = double.MinValue;
        var minZ = double.MaxValue; var maxZ = double.MinValue;

        foreach (var p in photons)
        {
            if (p.Position.X < minX) minX = p.Position.X;
            if (p.Position.X > maxX) maxX = p.Position.X;
            if (p.Position.Y < minY) minY = p.Position.Y;
            if (p.Position.Y > maxY) maxY = p.Position.Y;
            if (p.Position.Z < minZ) minZ = p.Position.Z;
            if (p.Position.Z > maxZ) maxZ = p.Position.Z;
        }

        var spanX = maxX - minX;
        var spanY = maxY - minY;
        var spanZ = maxZ - minZ;

        if (spanX >= spanY && spanX >= spanZ) return 0;
        if (spanY >= spanZ) return 1;
        return 2;
    }

    // ── kNN Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the k nearest photons to <paramref name="queryPoint"/>
    /// within <paramref name="maxRadius"/>.
    /// </summary>
    /// <param name="queryPoint">The world-space point to search around.</param>
    /// <param name="k">Maximum number of photons to return.</param>
    /// <param name="maxRadius">
    /// Maximum search radius. Only photons within this distance are returned.
    /// </param>
    /// <returns>
    /// The nearest photons found, ordered by distance ascending.
    /// May contain fewer than k photons if not enough are within the radius.
    /// </returns>
    public IReadOnlyList<(Photon Photon, double DistanceSq)> FindNearest(
        Vector3 queryPoint,
        int k,
        double maxRadius)
    {
        if (_root is null) return [];

        // Max-heap: keeps the k closest photons found so far
        // We use a sorted list for simplicity — for large k a proper
        // heap would be faster but k is typically small (50-100)
        var heap = new NearestPhotonHeap(k);
        SearchKnn(_root, queryPoint, k, maxRadius * maxRadius, heap);

        return heap.GetResults();
    }

    private static void SearchKnn(
        PhotonNode? node,
        Vector3 query,
        int k,
        double maxDistSq,
        NearestPhotonHeap heap)
    {
        if (node is null) return;

        // Distance from query to this photon
        var diff = query - node.Photon.Position;
        var distSq = diff.LengthSquared;

        // Add this photon to the heap if within radius
        if (distSq <= maxDistSq)
            heap.Add(node.Photon, distSq);

        // Determine which side of the split plane the query is on
        var splitDist = GetComponent(query, node.SplitAxis)
                      - GetComponent(node.Photon.Position, node.SplitAxis);
        var splitDistSq = splitDist * splitDist;

        // Visit the nearer child first
        var nearChild = splitDist < 0 ? node.Left : node.Right;
        var farChild = splitDist < 0 ? node.Right : node.Left;

        SearchKnn(nearChild, query, k, maxDistSq, heap);

        // Only visit far child if it could contain closer photons
        var currentMaxDistSq = heap.Count < k
            ? maxDistSq
            : heap.MaxDistanceSq;

        if (splitDistSq <= currentMaxDistSq)
            SearchKnn(farChild, query, k, maxDistSq, heap);
    }

    private static double GetComponent(Vector3 v, int axis) => axis switch
    {
        0 => v.X,
        1 => v.Y,
        _ => v.Z
    };

    // ── Debug helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all photons in the map. Used for debug visualization.
    /// </summary>
    public IReadOnlyList<Photon> GetAllPhotons()
    {
        var result = new List<Photon>(_count);
        CollectAll(_root, result);
        return result;
    }

    private static void CollectAll(PhotonNode? node, List<Photon> result)
    {
        if (node is null) return;
        result.Add(node.Photon);
        CollectAll(node.Left, result);
        CollectAll(node.Right, result);
    }
}

// ── Internal types ────────────────────────────────────────────────────────────

/// <summary>A node in the photon kd-tree.</summary>
internal sealed class PhotonNode
{
    public Photon Photon { get; }
    public PhotonNode? Left { get; }
    public PhotonNode? Right { get; }

    /// <summary>The axis this node splits on (0=X, 1=Y, 2=Z).</summary>
    public int SplitAxis { get; }

    public PhotonNode(Photon photon, PhotonNode? left,
                      PhotonNode? right, int splitAxis)
    {
        Photon = photon;
        Left = left;
        Right = right;
        SplitAxis = splitAxis;
    }
}

/// <summary>
/// A fixed-capacity max-heap for tracking the k nearest photons.
/// When full, automatically evicts the farthest photon when a closer
/// one is found.
/// </summary>
internal sealed class NearestPhotonHeap
{
    private readonly int _capacity;
    private readonly List<(Photon Photon, double DistanceSq)> _items = [];

    public int Count => _items.Count;

    /// <summary>
    /// The distance squared of the farthest photon in the heap.
    /// Returns double.MaxValue if the heap is empty.
    /// </summary>
    public double MaxDistanceSq => _items.Count == 0
        ? double.MaxValue
        : _items.Max(x => x.DistanceSq);

    public NearestPhotonHeap(int capacity) => _capacity = capacity;

    /// <summary>
    /// Adds a photon to the heap. If the heap is full and this photon
    /// is closer than the farthest current photon, the farthest is evicted.
    /// </summary>
    public void Add(Photon photon, double distanceSq)
    {
        if (_items.Count < _capacity)
        {
            _items.Add((photon, distanceSq));
        }
        else
        {
            // Find and replace the farthest photon if this one is closer
            var maxIdx = 0;
            for (var i = 1; i < _items.Count; i++)
                if (_items[i].DistanceSq > _items[maxIdx].DistanceSq)
                    maxIdx = i;

            if (distanceSq < _items[maxIdx].DistanceSq)
                _items[maxIdx] = (photon, distanceSq);
        }
    }

    /// <summary>
    /// Returns all photons sorted by distance ascending.
    /// </summary>
    public IReadOnlyList<(Photon Photon, double DistanceSq)> GetResults()
        => _items.OrderBy(x => x.DistanceSq).ToList();
}